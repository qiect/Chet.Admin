# 操作日志

## 功能特性

- `AuditLogMiddleware` 自动记录所有写操作（POST/PUT/DELETE）
- 记录操作人、模块、操作类型、请求参数、响应状态码、IP、耗时
- 分页查询（支持时间范围、用户、模块、操作类型筛选）
- 日志清理（按日期清理）
- 操作类型彩色标签展示

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/audit-logs/paged` | 分页查询 |
| DELETE | `/audit-logs/clear` | 清理日志 |

**查询参数：** `pageNumber`、`pageSize`、`keyword`、`userId`、`module`、`action`、`startTime`、`endTime`

## 实现细节

- 中间件异步写入数据库，不阻塞请求响应
- 登录/登出通过 `AuthController` 手动记录
- 请求参数以 JSON 存储，支持详情展开查看

## 日志字段

| 字段 | 说明 |
| ---- | ---- |
| UserId | 操作人 ID |
| UserName | 操作人用户名 |
| Action | 操作类型（Create/Update/Delete/Login/Logout/Assign） |
| Module | 模块（User/Role/Menu/Auth 等） |
| Description | 操作描述 |
| TargetId | 操作目标 ID |
| HttpMethod | 请求方法 |
| RequestPath | 请求路径 |
| RequestData | 请求参数（JSON） |
| StatusCode | 响应状态码 |
| ClientIp | 客户端 IP |
| UserAgent | User-Agent |
| Duration | 执行耗时（ms） |
| OperatedAt | 操作时间（UTC） |
