# Chet Admin 新功能详细设计文档

> **最后更新**: 2026-07-07
> **整体进度**: 9/9 功能已实现，2 项待完善

## 现状分析

### 已实现功能

| 模块 | 状态 | 完成度 |
|------|------|--------|
| 用户管理 | CRUD + 密码强度 + 改密分离 | 95% |
| 角色管理 | CRUD + 权限分配 + 数据权限 | 95% |
| 菜单管理 | CRUD + 树形展示 | 90% |
| 部门管理 | CRUD + 树形展示 | 90% |
| 权限管理 | CRUD + 关联菜单 | 85% |
| 字典管理 | CRUD + 数据联动(useDict) | 85% |
| 仪表盘 | 动态统计 + SVG趋势图 + 最近操作 | 85% |
| 认证登录 | JWT + 限流 + 验证码 + 锁定 | 90% |
| 按钮级权限 | 菜单过滤 + auth 属性 | 90% |
| 个人中心 | 资料修改 + 密码修改(后端对接) | 85% |
| 操作日志 | 中间件自动记录 + 查询/清理 | 90% |
| 通知公告 | CRUD + 全局公告 + 个人通知 + 未读 | 85% |
| 文件上传 | 本地存储 + 上传/下载/删除 | 80% |
| 数据权限 | All/Dept/DeptAndChild/Self/Custom | 80% |
| 在线用户 | ConcurrentDictionary追踪 + 强制下线 | 85% |

### 功能开发完成度

| 优先级 | 功能 | 状态 | 后端 | 前端 | 备注 |
|--------|------|------|------|------|------|
| P0 | 操作日志 | ✅ 已完成 | ✅ | ✅ | 中间件自动记录写操作 |
| P0 | 个人中心对接 | ✅ 已完成 | ✅ | ✅ | 资料+密码修改已对接 |
| P1 | 仪表盘数据可视化 | ✅ 已完成 | ✅ | ✅ | SVG趋势图+6卡片+最近操作 |
| P1 | 字典数据联动 | ✅ 已完成 | ✅ | ✅ | useDict composable + 6种字典 |
| P2 | 通知公告 | ✅ 已完成 | ✅ | ✅ | 全局公告+个人通知+未读计数 |
| P2 | 文件上传 | ✅ 已完成 | ✅ | ✅ | 10MB限制+15种格式 |
| P2 | 登录安全增强 | ⚠️ 部分完成 | ✅ | ❌ | 后端验证码+锁定已完成，前端登录页未加验证码UI |
| P3 | 数据权限 | ✅ 已完成 | ✅ | ✅ | 5种范围+用户列表行级过滤 |
| P3 | 在线用户管理 | ✅ 已完成 | ✅ | ✅ | 登录/登出追踪+强制下线 |

### 已知问题

| 问题 | 严重度 | 说明 |
|------|--------|------|
| 登录页无验证码UI | 中 | 后端支持验证码(captcha API)，连续失败3次返回requireCaptcha，但前端登录页未实现验证码输入和显示 |
| BadRequestException返回500 | 低 | Controller内抛出BadRequestException时，UseExceptionHandler未正确捕获，返回500而非400 |
| 通知铃铛角标 | 低 | 通知未读数API已实现，但顶部导航栏未添加铃铛图标和角标 |
| 密码过期策略 | 低 | 设计中包含但未实现（MustChangePassword、PasswordChangedAt字段） |

---

## P0-1：操作日志

### 功能描述

记录用户在系统中的所有关键操作（登录、增删改、权限变更等），支持查询和导出，用于安全审计和问题追溯。

### 数据模型

```csharp
// Domain/Audit/AuditLogEntity.cs
public class AuditLogEntity
{
    public int Id { get; set; }

    /// <summary>操作人ID</summary>
    public int UserId { get; set; }

    /// <summary>操作人用户名</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>操作类型：Create/Update/Delete/Login/Logout/Assign</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>操作模块：User/Role/Menu/Department/Permission/Dictionary/Auth</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>操作描述（如"创建用户：张三"）</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>操作目标ID</summary>
    public string? TargetId { get; set; }

    /// <summary>请求方法：GET/POST/PUT/DELETE</summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>请求路径</summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>请求参数（JSON）</summary>
    public string? RequestData { get; set; }

    /// <summary>响应状态码</summary>
    public int StatusCode { get; set; }

    /// <summary>客户端IP</summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>User-Agent</summary>
    public string? UserAgent { get; set; }

    /// <summary>执行耗时（ms）</summary>
    public long Duration { get; set; }

    /// <summary>操作时间</summary>
    public DateTime OperatedAt { get; set; } = DateTime.UtcNow;
}
```

### 后端设计

**1. 审计日志中间件**

```
AuditLogMiddleware.cs
├── 拦截所有 /api/v1/* 的写操作（POST/PUT/DELETE）
├── 记录请求参数、响应状态码、耗时
├── 异步写入数据库（不阻塞请求响应）
└── 登录/登出通过 AuthController 手动记录
```

**2. API 端点**

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/audit-logs/paged` | 分页查询日志（支持时间范围、用户、模块、操作类型筛选） |
| DELETE | `/api/v1/audit-logs/clear` | 清理指定日期之前的日志 |

**3. 查询参数**

```json
{
  "pageNumber": 1,
  "pageSize": 20,
  "keyword": "",
  "userId": null,
  "module": null,
  "action": null,
  "startTime": "2026-01-01",
  "endTime": "2026-12-31"
}
```

### 前端设计

**页面**：系统管理 > 操作日志

```
┌──────────────────────────────────────────────────┐
│ 筛选栏：[时间范围] [操作模块▼] [操作类型▼] [关键字] [查询] │
├──────────────────────────────────────────────────┤
│ ID │ 操作人 │ 模块 │ 操作 │ 描述 │ IP │ 耗时 │ 时间 │
│ 1  │ Admin  │ 用户 │ 创建 │ 创建用户：张三 │ 192… │ 45ms │ 07-06 │
│ 2  │ Admin  │ 认证 │ 登录 │ Admin登录成功 │ 192… │ 12ms │ 07-06 │
└──────────────────────────────────────────────────┘
```

- 操作类型彩色 Tag：创建(蓝)、更新(橙)、删除(红)、登录(绿)、登出(灰)
- 点击行展开查看完整请求参数 JSON
- 仅支持查看和清理，不支持编辑/删除单条

---

## P0-2：个人中心对接

### 功能描述

将框架自带的个人中心页面与后端 API 对接，实现真正的资料修改、密码修改功能。

### 当前问题

| Tab | 现状 | 问题 |
|-----|------|------|
| 基本设置 | 有表单但无提交逻辑 | 表单数据未对接后端 |
| 修改密码 | 有表单但无提交逻辑 | 未调用后端改密接口 |
| 安全设置 | 纯静态展示 | 密保手机/邮箱/MFA 未实现 |
| 通知设置 | 纯静态展示 | 通知偏好未持久化 |

### 后端新增接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/auth/profile` | 获取当前用户详细资料（含角色、部门、头像） |
| PUT | `/api/v1/auth/profile` | 更新当前用户资料（姓名、头像） |
| PUT | `/api/v1/auth/change-password` | 修改当前用户密码（需验证旧密码） |

**ChangePasswordDto**：

```csharp
public class ChangePasswordDto
{
    /// <summary>旧密码</summary>
    public required string OldPassword { get; set; }

    /// <summary>新密码</summary>
    [MinLength(6, ErrorMessage = "密码至少6位")]
    public required string NewPassword { get; set; }
}
```

### 前端改造

1. **基本设置**：调用 `GET /api/v1/auth/profile` 加载数据，提交调用 `PUT /api/v1/auth/profile`，新增头像上传区域
2. **修改密码**：调用 `PUT /api/v1/auth/change-password`，添加密码强度条
3. **安全设置**：暂时保持静态展示，后续迭代增加密保手机绑定
4. **通知设置**：暂时保持静态展示，后续迭代增加通知偏好持久化

---

## P1-1：仪表盘数据可视化

### 功能描述

将仪表盘从静态展示升级为动态数据可视化，增加趋势图表和实时信息。

### 当前问题

- 统计卡片从 API 获取了数据，但只有数字
- 无趋势图表
- 无最近操作记录
- 无待办/通知提醒

### 后端新增接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/dashboard/stats` | 获取统计数据（用户数、角色数、菜单数、部门数、今日登录数） |
| GET | `/api/v1/dashboard/trend?days=7` | 获取近N天用户注册趋势 |
| GET | `/api/v1/dashboard/recent-logs?count=10` | 获取最近操作日志（依赖 P0-1） |

**DashboardStatsDto**：

```csharp
public class DashboardStatsDto
{
    public int UserCount { get; set; }
    public int RoleCount { get; set; }
    public int MenuCount { get; set; }
    public int DepartmentCount { get; set; }
    public int TodayLoginCount { get; set; }
    public int ActiveUserCount { get; set; }    // 近7天有登录
}
```

**DashboardTrendDto**：

```csharp
public class DashboardTrendDto
{
    public List<TrendItem> Items { get; set; }
}

public class TrendItem
{
    public string Date { get; set; }      // "2026-07-01"
    public int RegisterCount { get; set; }
    public int LoginCount { get; set; }
}
```

### 前端设计

```
┌──────────────────────────────────────────────────────────┐
│  欢迎回来，Admin                          2026-07-06 19:00  │
├──────────┬──────────┬──────────┬──────────┬──────────────┤
│  👥 用户   │  🎭 角色   │  📋 菜单   │  🏢 部门   │  🔑 今日登录  │
│    12     │    5     │    28    │    8    │     23      │
├──────────┴──────────┴──────────┴──────────┴──────────────┤
│  📈 近7天趋势                                          │
│  ┌─────────────────────────────────────────────────┐    │
│  │  [折线图] 注册人数 / 登录次数                      │    │
│  └─────────────────────────────────────────────────┘    │
├─────────────────────────┬────────────────────────────────┤
│  📋 最近操作            │  🚀 快捷入口                   │
│  • Admin 创建了用户 张三  │  用户管理  角色管理             │
│  • Admin 修改了角色 user  │  菜单管理  部门管理             │
│  • Admin 登录系统        │  权限管理  字典管理             │
└─────────────────────────┴────────────────────────────────┘
```

技术选型：使用 **ECharts**（通过 `vue-echarts` 封装）渲染折线图，轻量且与 Ant Design Vue 兼容好。

---

## P1-2：字典数据联动

### 功能描述

将字典管理模块与业务表单联动，使 Select/Radio 等选项从字典数据动态加载，而非硬编码。

### 当前问题

- 字典管理仅独立 CRUD，数据未被任何业务模块引用
- 用户性别、状态等字段无法通过字典配置

### 数据模型调整

```csharp
// 现有 DictionaryEntity 需确认结构
// 需要支持：字典类型（如 user_status）+ 字典项（启用/禁用）

public class DictionaryEntity
{
    public int Id { get; set; }
    public string Code { get; set; }          // 字典编码：user_status
    public string Name { get; set; }          // 字典名称：用户状态
    public int? ParentId { get; set; }        // 父级ID（类型项为null，字典项指向类型）
    public string? Value { get; set; }        // 字典值（类型项为null，字典项有值）
    public string? Label { get; set; }        // 显示标签
    public int Sort { get; set; }             // 排序
    public bool IsEnabled { get; set; }       // 是否启用
}
```

### 后端新增接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/dictionaries/type/{code}` | 根据字典编码获取字典项列表 |

返回格式：
```json
{
  "code": "user_status",
  "name": "用户状态",
  "items": [
    { "value": "1", "label": "启用" },
    { "value": "0", "label": "禁用" }
  ]
}
```

### 前端使用方式

```ts
// composable: useDict(code)
const { options, loading } = useDict('user_status');
// options → [{ label: '启用', value: '1' }, { label: '禁用', value: '0' }]

// 在表单 schema 中使用
{ component: 'Select', fieldName: 'status', label: '状态',
  componentProps: { options: [] }  // 初始空
}
// 弹窗打开时
formApi.updateSchema([
  { fieldName: 'status', componentProps: { options } }
]);
```

### 预置字典数据

| 编码 | 名称 | 字典项 |
|------|------|--------|
| `user_status` | 用户状态 | 启用(1) / 禁用(0) |
| `menu_type` | 菜单类型 | 目录(1) / 菜单(2) / 按钮(3) |
| `gender` | 性别 | 男(1) / 女(2) / 未知(0) |
| `yes_no` | 是否 | 是(1) / 否(0) |

---

## P2-1：通知公告

### 功能描述

系统内部通知和公告功能，支持管理员发布全局公告，用户接收个人通知。

### 数据模型

```csharp
// Domain/Notification/NotificationEntity.cs
public class NotificationEntity
{
    public int Id { get; set; }

    /// <summary>通知标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>通知内容（支持HTML）</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>类型：Announcement(公告) / Notification(通知) / Todo(待办)</summary>
    public string Type { get; set; } = "Notification";

    /// <summary>优先级：Low / Normal / High / Urgent</summary>
    public string Priority { get; set; } = "Normal";

    /// <summary>发送人ID（null为系统）</summary>
    public int? SenderId { get; set; }

    /// <summary>是否全局公告</summary>
    public bool IsGlobal { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Domain/Notification/NotificationRecipientEntity.cs
public class NotificationRecipientEntity
{
    public int Id { get; set; }
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
```

### 后端接口

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/v1/notifications` | 发送通知/公告 |
| GET | `/api/v1/notifications/paged` | 分页查询通知列表 |
| GET | `/api/v1/notifications/my` | 获取我的通知列表 |
| GET | `/api/v1/notifications/unread-count` | 获取未读数量 |
| PUT | `/api/v1/notifications/{id}/read` | 标记已读 |
| PUT | `/api/v1/notifications/read-all` | 全部标记已读 |
| DELETE | `/api/v1/notifications/{id}` | 删除通知 |

### 前端设计

**1. 顶部导航栏铃铛图标**

```
🔔 3   ← 未读数量角标，点击展开通知面板
```

**2. 通知面板（Dropdown）**

```
┌─────────────────────────────┐
│ 通知(3)              全部已读 │
├─────────────────────────────┤
│ 🔴 系统维护通知     10分钟前  │
│ 🔴 您被分配了新角色  1小时前  │
│ 🔵 系统版本已更新    昨天     │
├─────────────────────────────┤
│                    查看全部 → │
└─────────────────────────────┘
```

**3. 通知管理页面**（系统管理 > 通知管理）

- 管理员可发送公告（全局）和通知（指定用户）
- 富文本编辑器（TinyMCE / Wangeditor）
- 通知列表 + 已读/未读统计

---

## P2-2：文件上传

### 功能描述

通用文件上传和存储服务，支持用户头像上传、附件管理等场景。

### 数据模型

```csharp
// Domain/File/FileEntity.cs
public class FileEntity
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;       // 原始文件名
    public string StoredName { get; set; } = string.Empty;     // 存储文件名（GUID）
    public string FilePath { get; set; } = string.Empty;       // 相对路径
    public string ContentType { get; set; } = string.Empty;    // MIME类型
    public long FileSize { get; set; }                         // 文件大小(bytes)
    public string? Description { get; set; }                   // 描述
    public int? UploaderId { get; set; }                       // 上传人
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 后端设计

**1. 存储策略**

```json
// appsettings.json
{
  "FileStorage": {
    "Provider": "Local",           // Local / OSS
    "LocalPath": "uploads",        // 本地存储目录
    "MaxFileSize": 10485760,       // 10MB
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx"]
  }
}
```

**2. 接口**

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/v1/files/upload` | 上传文件（multipart/form-data） |
| GET | `/api/v1/files/{id}` | 获取文件信息 |
| GET | `/api/v1/files/{id}/download` | 下载文件 |
| DELETE | `/api/v1/files/{id}` | 删除文件 |

### 前端使用

```ts
// Ant Design Vue Upload 组件
<Upload
  :action="`${apiUrl}/api/v1/files/upload`"
  :headers="{ Authorization: `Bearer ${token}` }"
  @change="onUploadChange"
>
  <Button><UploadOutlined /> 上传文件</Button>
</Upload>
```

---

## P2-3：登录安全增强

### 功能描述

增强登录安全性，包括图形验证码、登录失败锁定、密码过期策略。

### 后端设计

**1. 图形验证码**

```csharp
// 登录请求增加验证码字段
public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? CaptchaId { get; set; }      // 验证码ID
    public string? CaptchaCode { get; set; }    // 用户输入的验证码
}
```

- 使用 `SkiaSharp` 生成图形验证码
- 连续失败 3 次后强制要求验证码
- 验证码 5 分钟有效，使用后即失效

**接口**：

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/auth/captcha` | 获取图形验证码（返回 id + base64 图片） |

**2. 登录失败锁定**

```
UserEntity 新增字段：
- LoginFailCount (int)    连续失败次数
- LockedUntil (DateTime?) 锁定截止时间

逻辑：
- 连续失败5次 → 锁定30分钟
- 成功登录 → 重置失败计数
- 锁定期间 → 返回 "账户已锁定，请30分钟后重试"
```

**3. 密码过期策略**

```
UserEntity 新增字段：
- PasswordChangedAt (DateTime?)  密码最后修改时间
- MustChangePassword (bool)      是否强制修改密码

appsettings.json 配置：
"PasswordPolicy": {
  "ExpirationDays": 90,        // 90天过期
  "MinLength": 6,
  "RequireUppercase": false,
  "RequireLowercase": false,
  "RequireDigit": false,
  "RequireSpecialChar": false
}

逻辑：
- 登录成功后检查密码是否过期
- 过期 → 返回 mustChangePassword: true
- 前端 → 跳转强制修改密码页面
```

### 前端改造

**1. 登录页增加验证码**

```
┌──────────────────────────┐
│  邮箱：[____________]     │
│  密码：[____________]     │
│  验证码：[______] [🎨A3xK] │  ← 连续失败3次后显示
│         [  登录  ]        │
└──────────────────────────┘
```

**2. 登录响应处理**

```ts
// 登录成功后检查
if (result.mustChangePassword) {
  // 跳转强制修改密码页面
  router.push('/force-change-password');
}
```

---

## P3-1：数据权限

### 功能描述

实现行级数据权限控制，使用户只能看到自己部门（及下级部门）的数据。

### 数据模型调整

```csharp
// RoleEntity 新增
public string? DataScope { get; set; }
// 枚举值：
// All      - 全部数据
// Dept     - 本部门数据
// DeptAndChild - 本部门及下级部门
// Self     - 仅本人数据
// Custom   - 自定义部门
```

```csharp
// 新增：角色-自定义数据权限部门关联
public class RoleDataScopeDeptEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int DepartmentId { get; set; }
}
```

### 实现方案

**1. 查询过滤器**

在 Service 层根据当前用户的角色 DataScope，自动追加查询条件：

```csharp
// 伪代码
IQueryable<UserEntity> ApplyDataScope(IQueryable<UserEntity> query, int userId)
{
    var user = GetUser(userId);
    var roles = GetRoles(userId);
    var scope = roles.Max(r => r.DataScope);  // 取最宽松的

    return scope switch
    {
        "All" => query,
        "Dept" => query.Where(u => u.DepartmentId == user.DepartmentId),
        "DeptAndChild" => query.Where(u => deptTree.Contains(u.DepartmentId)),
        "Self" => query.Where(u => u.Id == userId),
        "Custom" => query.Where(u => customDepts.Contains(u.DepartmentId)),
    };
}
```

**2. 角色分配界面**

在角色管理"分配权限"弹窗中增加数据权限选择：

```
┌────────────────────────────────┐
│ 数据权限：                      │
│ ○ 全部数据                      │
│ ○ 本部门数据                    │
│ ○ 本部门及下级部门数据           │
│ ○ 仅本人数据                    │
│ ○ 自定义部门  [选择部门...]      │
└────────────────────────────────┘
```

---

## P3-2：在线用户管理

### 功能描述

查看当前在线用户列表，支持强制下线。

### 实现方案

**1. 在线用户追踪**

```
方案A（推荐）：基于 Redis/内存缓存追踪
- 登录成功 → 缓存 Set("online:{userId}", sessionId, TTL=30min)
- 每次请求 → 刷新 TTL
- 获取在线用户 → 查询缓存 keys matching "online:*"
- 强制下线 → 删除对应用户的 JWT RefreshToken + 缓存标记
```

**2. 接口**

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/online-users` | 获取在线用户列表 |
| DELETE | `/api/v1/online-users/{userId}` | 强制下线 |

**3. 前端**

系统管理 > 在线用户，展示：用户名、部门、登录IP、登录时间、最后活跃时间，操作列有"强制下线"按钮。

---

## 开发路线图

```
第一阶段（1-2周）── 基础体验完善
├── P0-1 操作日志
├── P0-2 个人中心对接
└── P1-2 字典数据联动

第二阶段（2-3周）── 功能增强
├── P1-1 仪表盘数据可视化
└── P2-3 登录安全增强

第三阶段（3-4周）── 业务扩展
├── P2-1 通知公告
├── P2-2 文件上传
└── P3-2 在线用户管理

第四阶段（按需）── 高级特性
└── P3-1 数据权限
```

---

## 附录：通用技术规范

### 后端新增 Entity 规范

```csharp
// 1. 实体继承无基类（保持一致）
// 2. 必须在 AppDbContext 中添加 DbSet
// 3. 必须在 OnModelCreating 中配置表名和关系
// 4. 字符串字段设置 MaxLength
// 5. 日期字段统一使用 UTC
```

### 前端新增页面规范

```ts
// 1. API 文件放在 src/api/system/ 下
// 2. 页面文件放在 src/views/system/{module}/index.vue
// 3. 使用 useVbenVxeGrid + useVbenForm + useVbenModal 组合
// 4. 操作按钮必须绑定 auth 权限码
// 5. Select/TreeSelect 选项通过 updateSchema 动态设置
// 6. 树形表格使用扁平数据 + treeConfig.transform
// 7. 分页参数使用 pageNumber（非 page）
```

### 数据库迁移

```bash
# 新增 Entity 后执行
cd Chet.Admin.Api
dotnet ef migrations add AddAuditLogs \
  --project ../Chet.Admin.Infrastructure/Chet.Admin.Data \
  --startup-project .
dotnet ef database update
```
