# 统一响应格式

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

## 3. 错误码

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

## 4. API 版本控制

接口路径包含版本号 `api/v{version}`，当前为 `v1`。版本控制通过 `ConfigureApiVersioning` 配置，支持后续兼容性升级。

## 5. CORS 配置

跨域配置位于 `appsettings.json`：

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  }
}
```

开发环境下前端通过 Vite 代理转发，生产环境需在此配置实际域名或通过反向代理处理。
