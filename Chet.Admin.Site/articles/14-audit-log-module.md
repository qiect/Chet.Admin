# Chet.Admin 模块详解⑧：审计中间件自动记录操作 📜

> 《Chet.Admin 全栈实战》系列第 14 篇

---

## 前言

做后台系统，**操作日志** 是绕不开的话题。

谁删了那条数据？谁改了用户权限？谁批量导出了敏感信息？

没有审计日志，出了事就是一笔糊涂账 📉

**Chet.Admin** 内置了一套 **全局审计中间件**，自动拦截所有写操作（POST/PUT/DELETE），记录操作人、模块、动作、请求参数、耗时等完整信息，**零侵入**、**零改造**。

今天就来拆解这个模块的实现细节 👇

---

## 整体架构

先看一张全景图，了解审计日志模块的完整链路：

<!-- 审计日志架构图 -->
![审计日志架构](/screenshots/audit-log-architecture.svg)

核心链路：

```
请求进来 → AuditLogMiddleware 拦截 → 判断是否写操作
  → 是：捕获请求体 → 放行请求 → 后台异步写入日志
  → 否：直接放行
```

涉及的核心文件：

| 层 | 文件 | 职责 |
| ---- | ---- | ---- |
| 中间件 | `AuditLogMiddleware.cs` | 全局拦截 + 异步写入 |
| 控制器 | `AuditLogsController.cs` | 分页查询 + 清理 |
| 服务 | `AuditLogService.cs` | 业务逻辑 |
| 实体 | `AuditLogEntity.cs` | 数据模型 |
| 前端 | `audit-log/index.vue` | 列表展示 + 清理 |
| API | `audit-log.ts` | 请求封装 |

---

## 一、审计中间件：全局拦截写操作

### 1.1 中间件核心逻辑

审计的核心在 `AuditLogMiddleware`，它注册在中间件管道中，**每个请求都会经过**。

```csharp
public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
{
    var path = context.Request.Path.Value ?? "";

    // Only audit API write operations
    if (!path.StartsWith("/api/v") || context.Request.Method == "GET")
    {
        await _next(context);
        return;
    }

    // ... 记录逻辑
}
```

**关键点**：

- ✅ 只拦截 `/api/v` 开头的 API 请求
- ✅ 只拦截 **写操作**（POST / PUT / DELETE），GET 直接放行
- ✅ 读多写少，不记查询，避免日志爆炸

---

### 1.2 模块 / 动作中文化映射

审计日志要给运营看，不能显示 `users`、`POST` 这种技术术语。

中间件内置了两份映射表：

```csharp
// 模块名映射（控制器名 → 中文模块名）
private static readonly Dictionary<string, string> ModuleMap = new(StringComparer.OrdinalIgnoreCase)
{
    { "users", "用户管理" },
    { "roles", "角色管理" },
    { "menus", "菜单管理" },
    { "departments", "部门管理" },
    { "dictionaries", "字典管理" },
    { "auth", "认证授权" },
    { "auditlogs", "审计日志" },
    { "files", "文件管理" },
    { "notifications", "通知管理" },
    { "dashboard", "仪表盘" },
    { "onlineusers", "在线用户" },
};

// 操作动作映射（HTTP 方法 → 中文动作）
private static readonly Dictionary<string, string> ActionMap = new(StringComparer.OrdinalIgnoreCase)
{
    { "POST", "新增" },
    { "PUT", "修改" },
    { "DELETE", "删除" },
};
```

**提取逻辑**：从 URL 路径解析控制器名，再查映射表。

```csharp
var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
var controllerName = segments.Length >= 3 ? segments[2] : "";
var module = ModuleMap.TryGetValue(controllerName, out var m) ? m : controllerName;
var action = ActionMap.TryGetValue(httpMethod, out var a) ? a : httpMethod;
```

比如请求 `POST /api/v1/users`，解析出来就是：

- **模块**：用户管理
- **动作**：新增
- **描述**：新增用户管理

一目了然 👀

---

### 1.3 请求体捕获

审计要记录用户 **到底提交了什么数据**，所以需要捕获请求体。

```csharp
string? requestData = null;
if (context.Request.Body.CanSeek)
{
    context.Request.Body.Position = 0;
    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
    requestData = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0;
}
```

**注意这个 `CanSeek` 判断**：

- 普通 JSON 请求的流是可 Seek 的，正常读取
- `multipart/form-data`（文件上传）的流 **不可 Seek**，会被跳过
- 避免 **读取后流位置不对导致后续绑定失败**

还有个 **长度截断** 机制，防止超大请求体撑爆数据库：

```csharp
if (requestData != null && requestData.Length > 2000)
    requestData = requestData[..2000] + "...(truncated)";
```

超过 2000 字符就截断，加个 `(truncated)` 标记 ✂️

---

### 1.4 响应状态码捕获

光记请求还不够，**响应成功没**也得记。

中间件用一个 MemoryStream **替换原始响应流**，等请求处理完再拷回去：

```csharp
var originalBodyStream = context.Response.Body;
using var responseBody = new MemoryStream();
context.Response.Body = responseBody;

await _next(context);  // 放行请求

// Restore response body
responseBody.Position = 0;
await responseBody.CopyToAsync(originalBodyStream);
context.Response.Body = originalBodyStream;
```

这样就能拿到 `context.Response.StatusCode`，记录操作是 **成功（200）还是失败（400/500）**。

---

## 二、IServiceScopeFactory：解决后台线程的 DbContext 陷阱

### 2.1 为什么不能直接注入 DbContext

这是整个审计模块 **最容易踩坑** 的地方。

`AppDbContext` 注册为 **Scoped**，生命周期绑定到当前 HTTP 请求。请求结束，DbContext 就被释放。

但审计日志是 **fire-and-forget**（异步后台写入），请求都已经返回了，DbContext 早就没了 ❌

如果直接用注入的 DbContext：

```csharp
// ❌ 错误写法：后台线程访问已释放的 DbContext
_ = Task.Run(async () =>
{
    await _dbContext.AuditLogs.AddAsync(entity); // ObjectDisposedException!
});
```

直接报错 💥

---

### 2.2 正确做法：独立 DI 作用域

解决方案是注入 `IServiceScopeFactory`，在后台线程创建 **独立的 DI 作用域**：

```csharp
// fire-and-forget 写审计，但在独立 DI 作用域内解析服务
_ = Task.Run(async () =>
{
    try
    {
        using var scope = scopeFactory.CreateScope();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

        var auditLog = new AuditLogDto
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            Module = module,
            // ... 其他字段
        };

        await auditLogService.LogAsync(auditLog);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to write audit log for {Method} {Path}", httpMethod, path);
    }
});
```

**三个要点**：

1. ✅ `scopeFactory.CreateScope()` 创建独立作用域，拿到自己的 DbContext
2. ✅ 后台线程的 DbContext 不受请求生命周期影响
3. ✅ 异常被 catch 兜住，**审计失败不影响主业务**

---

### 2.3 数据提前取出

还有个细节容易忽略：**HttpContext 不是线程安全的**，请求结束后访问会出问题。

所以在 `Task.Run` 之前，先 **同步取出所有需要的数据**：

```csharp
// 在请求作用域结束前同步取出所有需要的数据
var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
var userNameClaim = context.User.FindFirst(ClaimTypes.Name);
var userId = int.Parse(userIdClaim.Value);
var userName = userNameClaim?.Value ?? $"用户{userIdClaim.Value}";
var httpMethod = context.Request.Method;
var statusCode = context.Response.StatusCode;
var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "";
var userAgent = context.Request.Headers.UserAgent.ToString();
var duration = stopwatch.ElapsedMilliseconds;
var operatedAt = DateTime.UtcNow;
```

全部转成 **局部变量**，后台线程只读这些变量，不碰 HttpContext 👍

---

## 三、JWT Claim 注入操作人

审计日志必须知道 **谁在操作**。

Chet.Admin 的 JWT Token 里带了两个关键 Claim：

- `ClaimTypes.NameIdentifier` → 用户 ID
- `ClaimTypes.Name` → 用户名

中间件直接从 Claims 取出：

```csharp
var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
var userNameClaim = context.User.FindFirst(ClaimTypes.Name);

if (userIdClaim == null) return; // 跳过未认证请求

var userId = int.Parse(userIdClaim.Value);
var userName = userNameClaim?.Value ?? $"用户{userIdClaim.Value}";
```

**未认证请求直接跳过**，不记录匿名操作。

这样审计日志里的 **操作人** 字段，就是 JWT 里解析出来的真实用户，**无法伪造** 🔒

---

## 四、数据模型：AuditLogEntity

来看审计日志实体，字段很全面：

```csharp
public class AuditLogEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }              // 操作用户ID
    public string UserName { get; set; }         // 操作用户名
    public string Action { get; set; }           // 操作动作（新增/修改/删除）
    public string Module { get; set; }           // 操作模块
    public string Description { get; set; }       // 操作描述
    public string? TargetId { get; set; }         // 目标对象ID
    public string HttpMethod { get; set; }       // HTTP方法
    public string RequestPath { get; set; }      // 请求路径
    public string? RequestData { get; set; }      // 请求数据（JSON）
    public int StatusCode { get; set; }          // 响应状态码
    public string ClientIp { get; set; }         // 客户端IP
    public string? UserAgent { get; set; }       // User-Agent
    public long Duration { get; set; }           // 耗时（ms）
    public DateTime OperatedAt { get; set; }     // 操作时间（UTC）
}
```

一条记录包含 **15 个字段**，从操作人到请求参数到响应状态，**全链路覆盖**。

---

## 五、服务层：查询 + 清理

### 5.1 分页查询

支持多维筛选：**用户、模块、操作、时间范围、关键词**。

```csharp
public async Task<PagedResult<AuditLogDto>> GetPagedAuditLogsAsync(AuditLogPagedRequest request)
{
    request.Normalize();
    var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

    if (request.UserId.HasValue)
        query = query.Where(x => x.UserId == request.UserId.Value);
    if (!string.IsNullOrWhiteSpace(request.Module))
        query = query.Where(x => x.Module == request.Module);
    if (!string.IsNullOrWhiteSpace(request.Action))
        query = query.Where(x => x.Action == request.Action);
    if (request.StartTime.HasValue)
        query = query.Where(x => x.OperatedAt >= request.StartTime.Value);
    if (request.EndTime.HasValue)
        query = query.Where(x => x.OperatedAt <= request.EndTime.Value);

    // 关键词模糊搜索（用户名 / 描述 / 请求路径）
    if (!string.IsNullOrWhiteSpace(request.Keyword))
    {
        var keyword = request.Keyword.Trim();
        query = query.Where(x => x.UserName.Contains(keyword)
            || x.Description.Contains(keyword)
            || x.RequestPath.Contains(keyword));
    }

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(x => x.OperatedAt)
        .Skip(request.Skip)
        .Take(request.PageSize)
        .ToListAsync();

    var dtos = _mapper.Map<List<AuditLogDto>>(items);
    return new PagedResult<AuditLogDto>(dtos, request.PageNumber, request.PageSize, totalCount);
}
```

**`AsNoTracking`** 提升查询性能，审计日志只读不改 ✅

---

### 5.2 日志清理策略

日志不能无限存，需要有清理机制：

```csharp
public async Task ClearBeforeAsync(DateTime before)
{
    var count = await _dbContext.AuditLogs.Where(x => x.OperatedAt < before).CountAsync();
    if (count > 0)
    {
        _dbContext.AuditLogs.RemoveRange(
            _dbContext.AuditLogs.Where(x => x.OperatedAt < before));
        await _dbContext.SaveChangesAsync();
    }
}
```

**按时间清理**：删除指定时间之前的所有日志。

前端提供了 **日期选择器**，选个日期一键清理 🗑️

---

## 六、控制器：两个接口

```csharp
[HttpGet("paged")]
public async Task<IActionResult> GetPagedAuditLogs(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? keyword = null,
    [FromQuery] int? userId = null,
    [FromQuery] string? module = null,
    [FromQuery] string? action = null,
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null)
{
    // 分页查询
}

[HttpDelete("clear")]
public async Task<IActionResult> ClearAuditLogs([FromQuery] DateTime before)
{
    await _auditLogService.ClearBeforeAsync(before);
    return Ok(ApiResponse.Ok(null, "Audit logs cleared successfully"));
}
```

就两个接口：**查** 和 **清**，简单直接。

---

## 七、前端：列表 + 彩色标签

### 7.1 API 封装

```typescript
// 分页查询审计日志列表
export async function getAuditLogListApi(params: {
  pageNumber: number;
  pageSize: number;
  keyword?: string;
  userId?: number;
  module?: string;
  action?: string;
  startTime?: string;
  endTime?: string;
}) {
  const result = await requestClient.get('/auditlogs/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

// 清除指定时间之前的审计日志
export async function clearAuditLogsApi(before: string) {
  return requestClient.delete('/auditlogs/clear', { params: { before } });
}
```

---

### 7.2 列表页面

前端用 VxeTable 展示，**模块和操作用彩色 Tag** 区分：

```typescript
const actionColorMap: Record<string, string> = {
  Create: 'blue',
  Update: 'orange',
  Delete: 'red',
  Login: 'green',
  Logout: 'default',
  Assign: 'purple',
};

const moduleColorMap: Record<string, string> = {
  User: 'blue',
  Role: 'purple',
  Menu: 'cyan',
  Department: 'green',
  Permission: 'orange',
  Dictionary: 'default',
  Auth: 'geekblue',
};
```

删除操作是 **红色 Tag**，创建是 **蓝色 Tag**，一眼就能分辨操作类型 🎨

<!-- 操作日志列表界面 -->
![操作日志列表](/screenshots/audit-log.png)

---

### 7.3 清理功能

清理按钮有 **权限控制**，只有 `system:audit:clear` 权限才显示：

```vue
<template #toolbar-tools>
  <div v-if="hasAccessByCodes(['system:audit:clear'])" class="flex items-center gap-2">
    <DatePicker
      :placeholder="'清理此日期之前'"
      format="YYYY-MM-DD"
      @change="onClearDateChange"
    />
    <Button danger size="small" @click="onClearLogs">清理</Button>
  </div>
</template>
```

选个日期 → 点清理 → 确认，搞定 ✅

---

## 八、耗时统计：Stopwatch

审计日志还记录了 **每个操作耗时多少毫秒**：

```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

// ... 请求处理

stopwatch.Stop();
var duration = stopwatch.ElapsedMilliseconds;
```

这个数据很有价值：

- 📊 哪个接口慢，一目了然
- 📊 慢操作可以关联性能优化
- 📊 异常耗时可能是攻击或异常

前端展示也很直观：

```typescript
{
  field: 'duration',
  title: '耗时',
  width: 80,
  slots: { default: ({ row }) => `${row.duration}ms` },
},
```

---

## 九、中间件管道位置

回顾一下审计中间件在管道中的位置：

```csharp
app.ConfigureExceptionHandling();                    // 异常处理
app.UseLogContext();                                // 日志上下文
app.UseCors("DefaultPolicy");                       // 跨域
app.UseRateLimiting();                               // 限流
app.ConfigureSwaggerUI();                            // Swagger
app.ConfigureAuthMiddleware(appSettings);            // JWT 认证
app.UseMiddleware<AuditLogMiddleware>();             // 👈 操作审计
app.UseMiddleware<OnlineUserTrackingMiddleware>();   // 在线用户追踪
app.UseStaticFiles(...);                             // 静态文件
app.MapControllers();                                // 路由映射
```

**为什么放在认证之后**？

因为审计需要从 JWT 拿用户信息，必须等认证中间件把 `context.User` 填充好才行。

**为什么放在路由映射之前**？

因为中间件要在请求进入控制器之前拦截，路由之后就来不及了。

---

## 十、完整流程总结

一张图梳理审计日志的完整流程：

```
请求进来
  ↓
判断是否 /api/v 开头 + 非 GET？
  ├─ 否 → 直接放行
  └─ 是 ↓
     启动 Stopwatch 计时
     ↓
     捕获请求体（CanSeek 判断）
     ↓
     替换响应流（MemoryStream）
     ↓
     放行请求 → _next(context)
     ↓
     恢复响应流
     ↓
     从 JWT Claim 取 userId / userName
     ↓
     解析模块名 + 动作（中文化）
     ↓
     截断过长请求体（>2000 字符）
     ↓
     Task.Run 后台异步写入
       └─ CreateScope → 拿独立 DbContext → 写入
       └─ 异常 catch 兜底
     ↓
     请求返回
```

---

## 设计亮点总结

| 特性 | 说明 |
| ---- | ---- |
| **零侵入** | 中间件全局拦截，业务代码无感知 |
| **只记写操作** | GET 不记，避免日志爆炸 |
| **中文化映射** | 模块/动作自动转中文，运营可读 |
| **fire-and-forget** | 后台异步写入，不影响响应速度 |
| **IServiceScopeFactory** | 解决后台线程 DbContext 释放问题 |
| **数据提前取出** | 避免 HttpContext 跨线程问题 |
| **耗时统计** | 每个操作记录毫秒级耗时 |
| **长度截断** | 请求体超 2000 字符自动截断 |
| **异常兜底** | 审计失败不影响主业务 |
| **分页清理** | 支持按时间清理历史日志 |

---

## 小结

审计日志模块看着简单，但细节很多：

- 🔑 **IServiceScopeFactory** 解决 scoped 服务在后台线程的生命周期问题
- 🔑 **数据提前取出** 避免 HttpContext 跨线程访问
- 🔑 **fire-and-forget** 保证主请求响应速度
- 🔑 **中文化映射** 让日志对业务人员可读

这些模式在任何需要 **异步后台任务 + DI 服务** 的场景都通用，值得收藏 ⭐

---

> 🔗 **GitHub**：https://github.com/qiect/Chet.Admin
> 🔗 **Gitee**：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

---

**下篇预告**：「Chet.Admin 模块详解⑨：全局公告 + 个人通知 + 未读计数 🔔」

---

`#ChetAdmin` `#全栈开发` `#.NET10` `#Vue3` `#审计日志` `#中间件` `#开源项目`
