# Chet.Admin 模块详解⑪：在线用户追踪 + 强制下线 📡

> 《Chet.Admin 全栈实战》系列第 17 篇

---

## 前言

安全要求高的系统，都需要知道 **当前谁在线**。

- 📊 看看有多少人正在使用系统
- 🔒 发现异常账号，一键 **强制下线**
- 📈 统计用户活跃度

但 JWT 是 **无状态** 的，Token 签发后就无法撤销，这给「强制下线」带来了挑战。

**Chet.Admin** 用 **内存黑名单 + Token 签发时间校验** 解决了这个问题，今天来拆解 👇

---

## 整体架构

先看全景图：

<!-- 在线用户架构图 -->
![在线用户架构](/screenshots/online-user-architecture.svg)

核心设计：

```
两份内存数据：
  _onlineUsers：在线用户列表（展示用）
  _revokedUsers：令牌黑名单（强制下线用）

追踪链路：请求进来 → 中间件更新活跃时间 → 超过30分钟自动清除
强制下线：加入黑名单 → JWT 校验时对比签发时间 → 早于吊销时间则拒绝
```

涉及的核心文件：

| 层 | 文件 | 职责 |
| ---- | ---- | ---- |
| 中间件 | `OnlineUserTrackingMiddleware.cs` | 追踪活跃时间 |
| 控制器 | `OnlineUsersController.cs` | 列表 + 强制下线 |
| 服务 | `OnlineUserService.cs` | 内存存储 + 黑名单 |
| DTO | `OnlineUserDto.cs` | 数据模型 |
| JWT配置 | `JwtConfiguration.cs` | OnTokenValidated 校验 |
| 前端 | `online-user/index.vue` | 列表展示 |
| API | `online-user.ts` | 请求封装 |

---

## 一、追踪中间件：刷新活跃时间

### 1.1 中间件逻辑

`OnlineUserTrackingMiddleware` 注册在管道中，**每个认证请求都会经过**：

```csharp
public async Task InvokeAsync(HttpContext context, IOnlineUserService onlineUserService)
{
    await _next(context);

    // After request completes, update activity
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
    {
        onlineUserService.UpdateActivity(userId);
    }
}
```

**注意**：是在 `await _next(context)` **之后** 更新活跃时间。

为什么？因为要等请求处理完，确认用户确实 **认证通过** 了，才更新。

---

### 1.2 中间件管道位置

```csharp
app.ConfigureAuthMiddleware(appSettings);            // JWT 认证
app.UseMiddleware<AuditLogMiddleware>();             // 操作审计
app.UseMiddleware<OnlineUserTrackingMiddleware>();   // 👈 在线用户追踪
app.UseStaticFiles(...);                             // 静态文件
```

**必须在认证之后**，因为要从 `context.User` 拿用户 ID。

---

## 二、内存存储：ConcurrentDictionary

### 2.1 双字典设计

`OnlineUserService` 维护两份 **静态内存数据**：

```csharp
public class OnlineUserService : IOnlineUserService
{
    // 在线用户列表
    private static readonly ConcurrentDictionary<int, OnlineUserDto> _onlineUsers = new();

    // 令牌黑名单：Key=用户ID，Value=吊销时间（UTC）
    private static readonly ConcurrentDictionary<int, DateTime> _revokedUsers = new();

    // 黑名单条目保留时长
    private static readonly TimeSpan _blacklistRetention = TimeSpan.FromHours(2);
}
```

**为什么要 `static`**？

- `OnlineUserService` 注册为 Scoped，每个请求会 new 一个实例
- 但在线用户列表是 **全局共享** 的，必须用 `static`
- `ConcurrentDictionary` 保证线程安全 ✅

---

### 2.2 OnlineUserDto

```csharp
public class OnlineUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }        // 登录时间
    public DateTime LastActiveTime { get; set; }   // 最后活跃时间
}
```

**两个时间字段**：

- **LoginTime**：登录时记录，不变
- **LastActiveTime**：每次请求都刷新，判断是否活跃

---

### 2.3 标记上线

用户登录时，调用 `UserOnline` 标记上线：

```csharp
public void UserOnline(int userId, string userName, string clientIp)
{
    var info = new OnlineUserDto
    {
        UserId = userId,
        UserName = userName,
        ClientIp = clientIp,
        LoginTime = DateTime.UtcNow,
        LastActiveTime = DateTime.UtcNow,
    };
    _onlineUsers.AddOrUpdate(userId, info, (_, _) => info);

    // 用户重新上线时清除可能存在的历史黑名单记录
    RemoveFromBlacklist(userId);
}
```

**`AddOrUpdate`** 是 ConcurrentDictionary 的原子操作：

- 不存在 → 添加
- 已存在 → 替换为新值

**重新登录清除黑名单**：用户被强制下线后重新登录，黑名单记录要清掉，否则新 Token 也通不过 ⚠️

---

### 2.4 更新活跃时间

```csharp
public void UpdateActivity(int userId)
{
    if (_onlineUsers.TryGetValue(userId, out var info))
    {
        info.LastActiveTime = DateTime.UtcNow;
    }
}
```

每次请求结束后调用，刷新最后活跃时间。

**只在已存在时更新**，不会新增记录（新增在登录时处理）。

---

## 三、获取在线用户列表

### 3.1 自动清理 30 分钟未活跃用户

```csharp
public List<OnlineUserDto> GetOnlineUsers()
{
    // Remove stale entries (no activity for 30 min)
    var threshold = DateTime.UtcNow.AddMinutes(-30);
    foreach (var kvp in _onlineUsers)
    {
        if (kvp.Value.LastActiveTime < threshold)
        {
            _onlineUsers.TryRemove(kvp.Key, out _);
        }
    }
    return _onlineUsers.Values.OrderByDescending(x => x.LastActiveTime).ToList();
}
```

**懒清理策略**：

- 不需要后台定时任务
- 每次查询时顺手清理
- 超过 30 分钟没活动的用户自动移除

按 **最后活跃时间倒序** 排列，最近活跃的排最前面 📋

---

## 四、强制下线：黑名单机制

### 4.1 核心挑战

JWT 是无状态的，**Token 签发后无法修改**。

强制下线怎么做？

**Chet.Admin 的方案**：

1. 把用户加入黑名单，记录 **吊销时间**
2. JWT 校验时，对比 **Token 签发时间** 和 **吊销时间**
3. 签发时间早于吊销时间 → Token 已失效

---

### 4.2 ForceOffline 方法

```csharp
public void ForceOffline(int userId)
{
    // 移除在线记录
    _onlineUsers.TryRemove(userId, out _);

    // 加入令牌黑名单，记录吊销时间
    _revokedUsers.AddOrUpdate(userId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);

    _logger.LogInformation("User {UserId} has been forced offline and added to token blacklist", userId);
}
```

**两步走**：

1. 从在线列表移除
2. 加入黑名单，记录当前 UTC 时间

---

### 4.3 控制器：同时清除 RefreshToken

光加黑名单还不够，因为用户可能用 **RefreshToken** 换新 Token。

控制器里还要清数据库的 RefreshToken：

```csharp
[HttpDelete("{userId}")]
public async Task<IActionResult> ForceOffline(int userId)
{
    // 1. 加入令牌黑名单并移除在线记录（使已签发的JWT立即失效）
    _onlineUserService.ForceOffline(userId);

    // 2. 清除用户的RefreshToken，防止通过刷新令牌获取新令牌
    var user = await _unitOfWork.Users.GetByIdAsync(userId);
    if (user != null)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("RefreshToken cleared for forced-offline user {UserId}", userId);
    }

    return Ok(ApiResponse.Ok(null, "User forced offline successfully"));
}
```

**三重保险**：

1. ✅ 加入黑名单 → Access Token 失效
2. ✅ 清除 RefreshToken → 无法刷新
3. ✅ 移除在线记录 → 列表不显示

用户只能 **重新登录** 才能继续使用 🚫

---

## 五、JWT 校验：OnTokenValidated

### 5.1 黑名单检查

这是强制下线的 **关键环节**。

在 `JwtConfiguration` 的 `OnTokenValidated` 事件中检查黑名单：

```csharp
options.Events = new JwtBearerEvents
{
    OnTokenValidated = context =>
    {
        var jwtToken = context.SecurityToken as JwtSecurityToken;
        if (jwtToken == null) return Task.CompletedTask;

        // 获取用户ID
        var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)
                      ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Task.CompletedTask;

        // 从DI容器解析在线用户服务
        var onlineUserService = context.HttpContext.RequestServices.GetService<IOnlineUserService>();
        if (onlineUserService == null) return Task.CompletedTask;

        // 检查令牌是否已被吊销（签发时间早于吊销时间则拒绝）
        if (onlineUserService.IsTokenRevoked(userId, jwtToken.ValidFrom))
        {
            context.Fail("Token has been revoked");
        }

        return Task.CompletedTask;
    },
};
```

**校验流程**：

1. 拿到 JWT Token
2. 提取用户 ID
3. 从 DI 解析 `IOnlineUserService`
4. 调用 `IsTokenRevoked` 检查

---

### 5.2 IsTokenRevoked 逻辑

```csharp
public bool IsTokenRevoked(int userId, DateTime tokenIssuedAt)
{
    CleanupExpiredBlacklist();

    if (!_revokedUsers.TryGetValue(userId, out var revokedAt))
    {
        return false;  // 不在黑名单中
    }

    // 令牌签发时间早于吊销时间 → 令牌已被吊销
    var issuedUtc = tokenIssuedAt.Kind == DateTimeKind.Utc
        ? tokenIssuedAt
        : tokenIssuedAt.ToUniversalTime();

    return issuedUtc < revokedAt;
}
```

**核心判断**：`tokenIssuedAt < revokedAt`

- Token 签发时间 **早于** 吊销时间 → 被吊销前签发的，**拒绝** ❌
- Token 签发时间 **晚于** 吊销时间 → 吊销后重新登录签发的，**放行** ✅

**时区处理**：统一转 UTC 比较，避免本地时间和 UTC 混用导致判断错误 ⚠️

---

### 5.3 原理图解

```
时间轴 →→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→→

Token A 签发            强制下线              Token B 签发（重新登录）
    ↓                      ↓                      ↓
    ↓                      ↓                      ↓
签发时间 < 吊销时间 → 拒绝    吊销时间        签发时间 > 吊销时间 → 放行
```

- **Token A**：在强制下线之前签发的，被吊销
- **Token B**：重新登录后签发的，有效

---

## 六、黑名单自动清理

### 6.1 为什么要清理

黑名单不能无限增长。

JWT 有过期时间（通常 1-2 小时），过期的 Token 自然就失效了，不需要再查黑名单。

```csharp
private static readonly TimeSpan _blacklistRetention = TimeSpan.FromHours(2);

private void CleanupExpiredBlacklist()
{
    var cutoff = DateTime.UtcNow.Subtract(_blacklistRetention);
    foreach (var kvp in _revokedUsers)
    {
        if (kvp.Value < cutoff)
        {
            _revokedUsers.TryRemove(kvp.Key, out _);
        }
    }
}
```

**保留 2 小时**：超过这个时间的黑名单条目自动清除。

**在 IsTokenRevoked 时顺手清理**，又是懒清理策略，不需要后台任务 🧹

---

## 七、重新登录：清除黑名单

用户被强制下线后，重新登录时要 **清除黑名单**：

```csharp
public void UserOnline(int userId, string userName, string clientIp)
{
    var info = new OnlineUserDto { /* ... */ };
    _onlineUsers.AddOrUpdate(userId, info, (_, _) => info);

    // 用户重新上线时清除可能存在的历史黑名单记录
    RemoveFromBlacklist(userId);
}

public void RemoveFromBlacklist(int userId)
{
    _revokedUsers.TryRemove(userId, out _);
}
```

**为什么必须清除**？

因为新 Token 的签发时间肯定 **晚于** 吊销时间，逻辑上不会被拒绝。

但留着黑名单记录会 **浪费内存**，而且每次请求都要查一次，清除更干净 ✅

---

## 八、数据模型：OnlineUserDto

```csharp
public class OnlineUserDto
{
    public int UserId { get; set; }              // 用户ID
    public string UserName { get; set; }         // 用户名
    public string ClientIp { get; set; }         // 客户端IP
    public DateTime LoginTime { get; set; }      // 登录时间
    public DateTime LastActiveTime { get; set; }  // 最后活跃时间
}
```

**注意**：这个 DTO 不存数据库，纯 **内存数据**。

重启应用后，在线用户列表会清空，用户下次请求时重新加入 🔄

---

## 九、控制器：两个接口

```csharp
[HttpGet]                   // 获取在线用户列表
[HttpDelete("{userId}")]    // 强制下线
```

就两个接口，简单直接。

**强制下线的完整逻辑**（控制器层）：

```csharp
public async Task<IActionResult> ForceOffline(int userId)
{
    // 1. 加入令牌黑名单并移除在线记录
    _onlineUserService.ForceOffline(userId);

    // 2. 清除用户的RefreshToken
    var user = await _unitOfWork.Users.GetByIdAsync(userId);
    if (user != null)
    {
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    return Ok(ApiResponse.Ok(null, "User forced offline successfully"));
}
```

---

## 十、前端：在线用户列表

### 10.1 API 封装

```typescript
// 获取在线用户列表
export async function getOnlineUsersApi() {
  return requestClient.get('/onlineusers');
}

// 强制指定用户下线
export async function forceOfflineApi(userId: number) {
  return requestClient.delete(`/onlineusers/${userId}`);
}
```

---

### 10.2 列表展示

```typescript
const columns: VxeTableGridColumns<any> = [
  { field: 'userId', title: 'ID', width: 70 },
  { field: 'userName', title: '用户名', minWidth: 120 },
  { field: 'clientIp', title: '登录IP', minWidth: 140 },
  {
    field: 'loginTime',
    title: '登录时间',
    minWidth: 180,
    slots: { default: ({ row }) => formatTime(row.loginTime) },
  },
  {
    field: 'lastActiveTime',
    title: '最后活跃',
    minWidth: 180,
    slots: { default: ({ row }) => formatTime(row.lastActiveTime) },
  },
  { field: 'operation', title: '操作', width: 120 },
];
```

<!-- 在线用户列表界面 -->
![在线用户列表](/screenshots/online-user.svg)

**注意**：这个列表 **不分页**，因为内存数据量不大：

```typescript
const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns,
    height: 'auto',
    pagerConfig: { enabled: false },   // 👈 关闭分页
    proxyConfig: {
      ajax: {
        query: async () => {
          const res = await getOnlineUsersApi();
          return { items: res || [], total: (res || []).length };
        },
      },
    },
  },
});
```

---

### 10.3 强制下线按钮

```vue
<template #action="{ row }">
  <VbenTableAction
    v-if="hasAccessByCodes(['system:online:force-offline'])"
    :actions="[
      { text: '强制下线', danger: true, onClick: () => onForceOffline(row.userId) },
    ]"
  />
</template>
```

**红色危险按钮** + **权限控制**，只有 `system:online:force-offline` 权限的管理员才能操作 🔐

```typescript
async function onForceOffline(userId: number) {
  try {
    await forceOfflineApi(userId);
    message.success('已强制下线');
    gridApi.query();
  } catch {
    message.error('操作失败');
  }
}
```

---

## 十一、完整流程总结

### 在线追踪流程

```
用户登录
  → UserOnline() → 加入 _onlineUsers + 清除黑名单
  ↓
每次请求
  → OnlineUserTrackingMiddleware → UpdateActivity() → 刷新活跃时间
  ↓
查看列表
  → GetOnlineUsers() → 清理30分钟未活跃 → 返回列表
```

### 强制下线流程

```
管理员点击「强制下线」
  → ForceOffline() → 移除 _onlineUsers + 加入 _revokedUsers（记录吊销时间）
  → 清除数据库 RefreshToken
  ↓
被下线用户下次请求
  → JWT 校验 → OnTokenValidated
  → IsTokenRevoked() → token.ValidFrom < revokedAt → 拒绝
  → 返回 401
  ↓
用户重新登录
  → UserOnline() → 清除黑名单 + 新 Token 签发时间晚于吊销时间 → 正常使用
```

---

## 设计亮点总结

| 特性 | 说明 |
| ---- | ---- |
| **ConcurrentDictionary** | 线程安全的内存存储，无需数据库 |
| **双字典设计** | 在线列表 + 黑名单，职责分离 |
| **static 字段** | 跨请求共享内存数据 |
| **30分钟自动清理** | 懒清理，无需后台任务 |
| **黑名单 2 小时保留** | 过期自动清理，防内存泄漏 |
| **Token 签发时间对比** | 解决 JWT 无状态无法撤销的问题 |
| **三重保险** | 黑名单 + 清 RefreshToken + 移除在线记录 |
| **重新登录清除黑名单** | 避免误判新 Token |
| **时区统一 UTC** | 避免时间比较错误 |
| **权限控制** | 强制下线需要专门权限 |

---

## 小结

在线用户 + 强制下线是 **JWT 无状态架构** 下的经典难题：

- 🔑 **ConcurrentDictionary** 做内存存储，简单高效
- 🔑 **Token 签发时间对比** 是撤销 JWT 的通用方案
- 🔑 **三重保险** 确保强制下线无遗漏
- 🔑 **懒清理策略** 避免后台定时任务的复杂性

这套方案在单机部署下完美工作；如果是 **集群部署**，可以把黑名单迁移到 Redis，实现跨节点共享 ⭐

---

> 🔗 **GitHub**：https://github.com/qiect/Chet.Admin
> 🔗 **Gitee**：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

---

**下篇预告**：「Chet.Admin 时间时区统一方案：前后端 UTC 协作 ⏰」

---

`#ChetAdmin` `#全栈开发` `#.NET10` `#Vue3` `#JWT` `#强制下线` `#开源项目`
