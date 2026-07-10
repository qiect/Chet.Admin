# 认证登录

## 功能特性

- 用户注册（邮箱 + 密码，密码 BCrypt 哈希）
- 用户登录获取 JWT 双令牌
- 图形验证码（SVG 格式，连续失败 3 次触发）
- 登录失败锁定（连续 5 次失败锁定 15 分钟）
- 密码过期策略（默认 90 天，过期后强制改密）
- 请求限流（登录 5 次/分钟，注册 10 次/分钟）

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| POST | `/auth/register` | 注册 |
| GET | `/auth/captcha` | 获取验证码 |
| POST | `/auth/login` | 登录 |
| POST | `/auth/refresh-token` | 刷新令牌 |
| POST | `/auth/logout` | 注销 |
| GET | `/auth/user-info` | 当前用户信息 + 权限 |
| GET/PUT | `/auth/profile` | 个人资料 |
| PUT | `/auth/change-password` | 修改密码 |
| PUT | `/auth/force-change-password` | 强制改密 |

## 实现细节

- `AuthController` 协调 `IAuthService`、`IUserService`、`CaptchaService`、`IOnlineUserService`
- 登录成功后标记用户在线（记录 IP）
- JWT Claims 中 `sub` 存储用户 ID
- 验证码存储在内存缓存中，5 分钟有效

## 已知问题

- 前端登录页尚未实现验证码输入 UI（后端已支持 `requireCaptcha` 返回）

更多认证机制说明详见 [认证机制](/api/authentication)。
