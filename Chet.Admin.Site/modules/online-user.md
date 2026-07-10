# 在线用户

## 功能特性

- 基于 `ConcurrentDictionary` 追踪在线用户
- 登录成功标记在线，登出 / 超时标记离线
- 每次请求刷新用户活跃时间（`OnlineUserTrackingMiddleware`）
- 在线用户列表（用户名、部门、登录 IP、登录时间、最后活跃时间）
- 强制下线

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/online-users` | 在线用户列表 |
| DELETE | `/online-users/{userId}` | 强制下线 |

## 实现细节

- `OnlineUserService` 注册为单例（`AddSingleton`）
- 登录时通过 `UserOnline(userId, name, ip)` 标记
- 注销时通过 `UserOffline(userId)` 标记
- `OnlineUserTrackingMiddleware` 在每次请求时刷新用户活跃时间
