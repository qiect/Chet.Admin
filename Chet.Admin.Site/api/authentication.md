# 认证机制

## 1. JWT 双令牌

- **Access Token**：短期有效（默认 30 分钟），用于 API 调用
- **Refresh Token**：长期有效（默认 7 天），用于续期

所有受保护接口需在请求头携带：

```
Authorization: Bearer {accessToken}
```

## 2. 认证流程

```
1. POST /api/v1/auth/login        → 获取 accessToken + refreshToken
2. 后续请求携带 Authorization 头
3. accessToken 过期 → POST /api/v1/auth/refresh-token 换取新令牌对
4. POST /api/v1/auth/logout       → 注销
```

### 2.1 登录请求示例

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

### 2.2 登录成功响应

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

## 3. 安全特性

- 登录限流：每 IP 每分钟最多 5 次请求
- 注册限流：每 IP 每分钟最多 10 次请求
- 连续失败 5 次锁定账户 15 分钟
- 连续失败 3 次返回 `requireCaptcha: true`（需验证码）
- 密码 BCrypt 哈希存储
- 密码过期策略（默认 90 天）

## 4. 限流规则

`RateLimitingMiddleware` 对敏感接口实施限流：

| 接口 | 限制 |
| ---- | ---- |
| `/auth/login` | 每 IP 每分钟 5 次 |
| `/auth/register` | 每 IP 每分钟 10 次 |

超限返回 `429 Too Many Requests`。

## 5. 认证相关接口

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

更多接口详见 [接口清单](/api/endpoints)。
