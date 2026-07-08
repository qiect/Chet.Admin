# API 接口文档

## 1. 概述

后端提供 RESTful API，所有接口基础路径为 `/api/v1`，遵循统一响应格式。完整的交互式文档可在开发环境通过 **Swagger UI** 访问：`http://localhost:5000/swagger`。

## 2. 统一响应格式

所有接口返回 `ApiResponse` 包装结构：

```json
{
  "success": true,
  "message": "操作描述",
  "data": { },
  "statusCode": 200
}
```

| 字段 | 类型 | 说明 |
| ---- | ---- | ---- |
| `success` | boolean | 业务是否成功（前端据此判断） |
| `message` | string | 提示消息 |
| `data` | object/array/null | 业务数据 |
| `statusCode` | number | HTTP 状态码 |

分页接口的 `data` 结构：

```json
{
  "items": [],
  "metadata": { "totalCount": 100, "pageNumber": 1, "pageSize": 20 }
}
```

## 3. 认证机制

### 3.1 JWT 双令牌

- **Access Token**：短期有效（默认 30 分钟），用于 API 调用
- **Refresh Token**：长期有效（默认 7 天），用于续期

所有受保护接口需在请求头携带：

```
Authorization: Bearer {accessToken}
```

### 3.2 认证流程

```
1. POST /api/v1/auth/login        → 获取 accessToken + refreshToken
2. 后续请求携带 Authorization 头
3. accessToken 过期 → POST /api/v1/auth/refresh-token 换取新令牌对
4. POST /api/v1/auth/logout       → 注销
```

### 3.3 安全特性

- 登录限流：每 IP 每分钟最多 5 次请求
- 注册限流：每 IP 每分钟最多 10 次请求
- 连续失败 5 次锁定账户 15 分钟
- 连续失败 3 次返回 `requireCaptcha: true`（需验证码）
- 密码 BCrypt 哈希存储
- 密码过期策略（默认 90 天）

## 4. 接口清单

### 4.1 认证模块（Auth）

| 方法 | 路径 | 说明 | 认证 |
| ---- | ---- | ---- | ---- |
| POST | `/auth/register` | 注册新用户 | 否 |
| GET | `/auth/captcha` | 获取图形验证码（SVG） | 否 |
| POST | `/auth/login` | 登录获取令牌 | 否 |
| POST | `/auth/refresh-token` | 刷新令牌 | 否 |
| POST | `/auth/logout` | 退出登录 | 是 |
| GET | `/auth/user-info` | 获取当前用户信息 + 权限 | 是 |
| GET | `/auth/profile` | 获取个人资料 | 是 |
| PUT | `/auth/profile` | 更新个人资料 | 是 |
| PUT | `/auth/change-password` | 修改密码 | 是 |
| PUT | `/auth/force-change-password` | 强制修改密码（密码过期） | 是 |

**登录请求示例：**

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "Admin@123",
  "captchaId": "可选",
  "captchaCode": "可选"
}
```

**登录成功响应：**

```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "rt_xxxxx...",
    "requireCaptcha": false,
    "lockedUntil": null,
    "mustChangePassword": false
  },
  "statusCode": 200
}
```

### 4.2 用户模块（Users）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/users` | 获取所有用户 |
| GET | `/users/paged` | 分页获取用户（支持数据权限过滤） |
| GET | `/users/{id}` | 获取用户详情 |
| POST | `/users` | 创建用户 |
| PUT | `/users/{id}` | 更新用户 |
| DELETE | `/users/{id}` | 删除用户 |

**分页查询参数：** `pageNumber`、`pageSize`、`keyword`

### 4.3 角色模块（Roles）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/roles` | 获取所有角色 |
| GET | `/roles/paged` | 分页获取角色 |
| GET | `/roles/{id}` | 获取角色详情 |
| POST | `/roles` | 创建角色 |
| PUT | `/roles/{id}` | 更新角色 |
| DELETE | `/roles/{id}` | 删除角色 |
| GET | `/roles/{id}/menus` | 获取角色菜单 |
| PUT | `/roles/{id}/menus` | 分配角色菜单 |
| PUT | `/roles/{id}/data-scope` | 设置数据权限范围 |

**数据权限范围（DataScope）枚举：**

| 值 | 说明 |
| ---- | ---- |
| `All` | 全部数据 |
| `Dept` | 本部门数据 |
| `DeptAndChild` | 本部门及下级部门 |
| `Self` | 仅本人数据 |
| `Custom` | 自定义部门 |

### 4.4 菜单模块（Menus）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/menus` | 获取菜单树 |
| GET | `/menus/{id}` | 获取菜单详情 |
| POST | `/menus` | 创建菜单 |
| PUT | `/menus/{id}` | 更新菜单 |
| DELETE | `/menus/{id}` | 删除菜单 |

菜单类型：目录(1) / 菜单(2) / 按钮(3)

### 4.5 部门模块（Departments）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/departments` | 获取部门树 |
| GET | `/departments/{id}` | 获取部门详情 |
| POST | `/departments` | 创建部门 |
| PUT | `/departments/{id}` | 更新部门 |
| DELETE | `/departments/{id}` | 删除部门 |

### 4.6 字典模块（Dictionaries）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/dictionaries` | 获取字典列表 |
| GET | `/dictionaries/code/{code}` | 根据编码获取字典项（前端 `useDict` 调用） |
| GET | `/dictionaries/{id}` | 获取字典详情 |
| POST | `/dictionaries` | 创建字典 |
| PUT | `/dictionaries/{id}` | 更新字典 |
| DELETE | `/dictionaries/{id}` | 删除字典 |

预置字典：`user_status`（用户状态）、`menu_type`（菜单类型）、`gender`（性别）、`yes_no`（是否）

### 4.7 仪表盘模块（Dashboard）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/dashboard/stats` | 获取统计数据（用户数、角色数、菜单数等） |
| GET | `/dashboard/trend?days=7` | 获取近 N 天注册 / 登录趋势 |
| GET | `/dashboard/recent-logs?count=10` | 获取最近操作日志 |

### 4.8 操作日志模块（AuditLogs）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/audit-logs/paged` | 分页查询日志（支持时间、用户、模块筛选） |
| DELETE | `/audit-logs/clear` | 清理指定日期之前的日志 |

**查询参数：** `pageNumber`、`pageSize`、`keyword`、`userId`、`module`、`action`、`startTime`、`endTime`

### 4.9 通知公告模块（Notifications）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| POST | `/notifications` | 发送通知 / 公告 |
| GET | `/notifications/paged` | 分页查询通知列表 |
| GET | `/notifications/my` | 获取我的通知 |
| GET | `/notifications/unread-count` | 获取未读数量 |
| PUT | `/notifications/{id}/read` | 标记单条已读 |
| PUT | `/notifications/read-all` | 全部标记已读 |
| DELETE | `/notifications/{id}` | 删除通知 |

通知类型：`Announcement`（公告）/ `Notification`（通知）/ `Todo`（待办）
优先级：`Low` / `Normal` / `High` / `Urgent`

### 4.10 文件模块（Files）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| POST | `/files/upload` | 上传文件（multipart/form-data） |
| GET | `/files/{id}` | 获取文件信息 |
| GET | `/files/{id}/download` | 下载文件 |
| DELETE | `/files/{id}` | 删除文件 |

**上传限制：** 单文件最大 10MB，支持 `.jpg` `.jpeg` `.png` `.gif` `.pdf` `.doc` `.docx` `.xls` `.xlsx` 等 15 种格式。静态文件通过 `/uploads/{filename}` 访问。

### 4.11 在线用户模块（OnlineUsers）

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/online-users` | 获取在线用户列表 |
| DELETE | `/online-users/{userId}` | 强制下线 |

### 4.12 健康检查模块（Health）

| 方法 | 路径 | 说明 | 认证 |
| ---- | ---- | ---- | ---- |
| GET | `/health` | 健康检查（Docker 健康探测） | 否 |

## 5. 错误码

| 状态码 | 说明 | 触发场景 |
| ---- | ---- | ---- |
| 200 | 成功 | 正常请求 |
| 201 | 创建成功 | POST 创建资源 |
| 400 | 请求参数错误 | 参数校验失败 / 邮箱已存在 |
| 401 | 未认证 | Token 无效 / 过期 / 未登录 |
| 403 | 无权限 | 已登录但无操作权限 |
| 404 | 资源不存在 | 资源 ID 无效 |
| 429 | 请求过多 | 触发限流（登录 / 注册） |
| 500 | 服务器错误 | 未捕获异常 |

> 业务错误（如登录失败）也会返回 HTTP 200 + `success: false`，前端通过 `success` 字段判断业务是否成功，通过 `message` 提示错误。

## 6. API 版本控制

接口路径包含版本号 `api/v{version}`，当前为 `v1`。版本控制通过 `ConfigureApiVersioning` 配置，支持后续兼容性升级。

## 7. CORS 配置

跨域配置位于 `appsettings.json`：

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  }
}
```

开发环境下前端通过 Vite 代理转发，生产环境需在此配置实际域名或通过反向代理处理。

## 8. 限流规则

`RateLimitingMiddleware` 对敏感接口实施限流：

| 接口 | 限制 |
| ---- | ---- |
| `/auth/login` | 每 IP 每分钟 5 次 |
| `/auth/register` | 每 IP 每分钟 10 次 |

超限返回 `429 Too Many Requests`。
