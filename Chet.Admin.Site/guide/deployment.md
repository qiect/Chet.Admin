# 部署指南

## 1. 概述

系统支持多种部署方式：

| 方式 | 适用场景 | 说明 |
| ---- | ---- | ---- |
| Docker Compose | 推荐 | 一键启动后端 + Redis，开箱即用 |
| Docker 单容器 | 快速验证 | 仅启动后端 API |
| 本地发布 | 自托管 / IIS | `dotnet publish` 后部署 |
| 前后端独立部署 | 生产环境 | 前端静态资源 + Nginx 反代后端 |

## 2. Docker Compose 部署（推荐）

后端已提供 `docker-compose.yml`，位于 `Chet.Admin.Api/docker-compose.yml`。

### 2.1 启动服务

```bash
cd Chet.Admin.Api

# 启动后端 API（默认不启动 Redis）
docker-compose up -d --build

# 同时启动 Redis
docker-compose --profile with-redis up -d --build
```

### 2.2 服务说明

| 服务 | 容器端口 | 主机端口 | 说明 |
| ---- | ---- | ---- | ---- |
| `api` | 8080 / 8443 | 8080 / 8443 | 后端 API |
| `redis` | 6379 | 6379 | 缓存（需 `--profile with-redis`） |

### 2.3 数据卷

| 卷名 | 挂载点 | 说明 |
| ---- | ---- | ---- |
| `app-data` | `/data` | SQLite 数据库文件 |
| `logs-data` | `/app/logs` | Serilog 日志文件 |
| `redis-data` | `/data` | Redis 持久化数据 |

### 2.4 环境变量配置

可通过环境变量覆盖 `appsettings.json`，采用双下划线 `__` 分隔层级：

```bash
# 数据库连接
ConnectionStrings__DefaultConnection=Data Source=/data/Chet.Admin.db

# JWT 密钥（生产环境必须修改）
JWT__SECRETKEY=YourVeryStrongSecretKeyHere

# 启用 Redis
Redis__Enabled=true
Redis__ConnectionString=redis:6379
```

### 2.5 健康检查

容器内置健康检查（每 30 秒探测 `http://localhost:8080/api/v1/health`），可通过 `docker ps` 查看健康状态：

```bash
docker ps --format "table {{.Names}}\t{{.Status}}"
```

## 3. Docker 单容器部署

### 3.1 构建镜像

```bash
cd Chet.Admin.Api
docker build -t chet-admin-api .
```

### 3.2 运行容器

```bash
docker run -d \
  --name chet-api \
  -p 8080:8080 \
  -v chet-data:/data \
  -v chet-logs:/app/logs \
  -e JWT__SECRETKEY=YourVeryStrongSecretKeyHere \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --restart unless-stopped \
  chet-admin-api
```

访问：`http://localhost:8080/swagger`

## 4. 本地发布部署

### 4.1 发布应用

```bash
cd Chet.Admin.Api
dotnet publish -c Release -o ./publish
```

### 4.2 自托管运行

```bash
cd publish
dotnet Chet.Admin.Api.dll
```

### 4.3 部署到 IIS

1. 在 IIS 创建应用程序池（.NET CLR 版本选择「无托管代码」）
2. 创建网站，物理路径指向 `publish` 目录
3. 确保进程模型标识有 `publish` 目录的读写权限
4. 配置 `web.config`（发布时自动生成）

## 5. 前端部署

### 5.1 构建生产包

```bash
cd Chet.Admin.Web

# 安装依赖
pnpm install

# 构建主应用（Ant Design Vue）
pnpm build:antd
```

构建产物位于 `apps/web-antd/dist`。

### 5.2 部署到 Nginx

将 `dist` 目录内容部署到 Nginx 静态目录，并配置 API 反向代理：

```nginx
server {
    listen 80;
    server_name yourdomain.com;

    # 前端静态资源
    location / {
        root /usr/share/nginx/html;
        index index.html;
        try_files $uri $uri/ /index.html;  # SPA 路由回退
    }

    # API 反向代理到后端
    location /api/ {
        proxy_pass http://backend:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 上传文件大小限制（根据需要调整）
    client_max_body_size 10m;
}
```

### 5.3 环境变量配置

构建前修改 `apps/web-antd/.env.production`：

```bash
# 生产环境 API 地址
# 跨域部署时填写后端实际地址；同域部署（Nginx 反代）保持相对路径
VITE_GLOB_API_URL=/api/v1
```

## 6. 配置项详解

### 6.1 后端配置（appsettings.json）

| 配置项 | 默认值 | 说明 |
| ---- | ---- | ---- |
| `ConnectionStrings.DefaultConnection` | `Data Source=Chet.Admin.db` | 数据库连接 |
| `AppSettings.Jwt.Enabled` | `true` | 是否启用 JWT |
| `AppSettings.Jwt.SecretKey` | 内置默认值 | **生产环境必须修改** |
| `AppSettings.Jwt.AccessTokenExpirationInMinutes` | `30` | Access Token 有效期（分钟） |
| `AppSettings.Jwt.RefreshTokenExpirationDays` | `7` | Refresh Token 有效期（天） |
| `AppSettings.Redis.Enabled` | `false` | 是否启用 Redis |
| `AppSettings.Redis.ConnectionString` | `localhost:6379` | Redis 连接串 |
| `AppSettings.PasswordPolicy.ExpirationDays` | `90` | 密码过期天数 |
| `AppSettings.PasswordPolicy.MinLength` | `6` | 密码最小长度 |
| `AppSettings.PasswordPolicy.RequireUppercase` | `false` | 是否需要大写字母 |
| `Cors.AllowedOrigins` | localhost:3000/5173 | 允许的跨域来源 |
| `Serilog.MinimumLevel.Default` | `Information` | 日志级别 |

### 6.2 前端配置

| 配置项 | 默认值 | 说明 |
| ---- | ---- | ---- |
| `VITE_PORT` | `5666` | 开发端口 |
| `VITE_GLOB_API_URL` | `/api/v1` | API 基础路径 |
| `VITE_NITRO_MOCK` | `false` | 是否启用 Mock |
| `VITE_DEVTOOLS` | `false` | 是否启用 devtools |
| `VITE_INJECT_APP_LOADING` | `true` | 全局 loading |

## 7. 生产环境清单

部署前请逐项确认：

- [ ] **修改 JWT SecretKey**：使用 32 位以上的强随机字符串
- [ ] **关闭 Swagger**：生产环境设置 `ASPNETCORE_ENVIRONMENT=Production`
- [ ] **启用 HTTPS**：配置 SSL 证书
- [ ] **配置 CORS**：`AllowedOrigins` 仅允许实际前端域名
- [ ] **切换数据库**：使用 PostgreSQL 替代 SQLite
- [ ] **启用 Redis**：设置 `Redis.Enabled=true`
- [ ] **配置日志持久化**：挂载 `logs-data` 数据卷
- [ ] **数据备份**：定期备份数据库与上传文件
- [ ] **修改默认管理员密码**：首次登录后立即修改 `admin@example.com` 的密码
- [ ] **配置反向代理**：Nginx 转发前端静态资源与 API 请求

## 8. 监控与运维

### 8.1 健康检查接口

```bash
curl http://localhost:8080/api/v1/health
```

### 8.2 日志查看

- **文件日志**：Serilog 输出到 `logs/log-{date}.txt`（按天滚动，保留 7 天）
- **容器日志**：`docker logs -f chet-webapi`

### 8.3 在线用户监控

通过「系统管理 > 在线用户」页面查看当前在线用户，支持强制下线。

### 8.4 操作审计

通过「系统管理 > 操作日志」页面查询所有写操作记录，支持按时间、用户、模块筛选。
