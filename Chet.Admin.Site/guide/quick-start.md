# 快速开始

## 1. 环境要求

开发前请确保已安装以下环境：

| 环境 | 版本要求 | 说明 |
| ---- | ---- | ---- |
| .NET SDK | 10.0+ | 后端运行时 |
| Node.js | 22.18+ 或 24+ | 前端运行时 |
| pnpm | 11.0+ | 前端包管理器 |
| Git | 任意版本 | 版本控制 |
| IDE（可选） | VS 2022 / Rider / VS Code | 推荐 Rider 或 VS Code |

> 后端数据库默认使用 SQLite（自动创建，无需安装），Redis 为可选项（默认关闭）。

### 1.1 安装 pnpm

```bash
# 通过 corepack 启用 pnpm
npm i -g corepack
corepack enable
```

## 2. 获取代码

```bash
git clone <仓库地址> Chet.Admin
cd Chet.Admin
```

项目根目录下包含两个子项目：

```
Chet.Admin/
├── Chet.Admin.Api/    # 后端
└── Chet.Admin.Web/    # 前端
```

## 3. 启动后端

### 3.1 还原依赖

```bash
cd Chet.Admin.Api
dotnet restore
```

### 3.2 启动服务

```bash
dotnet run --project Chet.Admin.Api
```

启动成功后：

- API 服务运行在 **http://localhost:5000**
- Swagger UI：访问 **http://localhost:5000/swagger**（根路径 `/` 会自动重定向到此）
- 首次启动会自动创建 SQLite 数据库 `Chet.Admin.db` 并写入种子数据

> 后端代码内置 JWT 配置，开发环境无需额外配置即可运行。

### 3.3 数据库迁移（可选）

默认情况下数据库会在启动时自动创建。如需手动管理迁移：

```bash
# 生成新迁移
dotnet ef migrations add MigrationName \
  --project Chet.Admin.Infrastructure/Chet.Admin.Data \
  --startup-project Chet.Admin.Api

# 更新数据库
dotnet ef database update \
  --project Chet.Admin.Infrastructure/Chet.Admin.Data \
  --startup-project Chet.Admin.Api
```

## 4. 启动前端

### 4.1 安装依赖

```bash
cd Chet.Admin.Web
pnpm install
```

### 4.2 启动开发服务器

```bash
# 启动主应用（Ant Design Vue）
pnpm dev:antd
```

启动成功后：

- 前端运行在 **http://localhost:5666**
- Vite 开发服务器会自动把 `/api` 请求代理到后端 `http://localhost:5000`

### 4.3 构建生产包

```bash
# 构建主应用
pnpm build:antd

# 预览构建结果
pnpm preview
```

## 5. 访问系统

### 5.1 默认管理员账号

浏览器打开 **http://localhost:5666**，使用默认账号登录：

| 项目 | 值 |
| ---- | ---- |
| 邮箱 | `admin@example.com` |
| 密码 | `Admin@123` |

> 首次登录如遇异常，建议按 `Ctrl+Shift+Delete` 清除浏览器 localStorage 后重试。

### 5.2 访问 API 文档

直接访问后端 Swagger UI：**http://localhost:5000/swagger**

可在 Swagger 中直接调试所有 API 接口（需先调用 `/api/v1/auth/login` 获取 Token，再点击右上角 Authorize 填入）。

## 6. 前后端联调说明

### 6.1 代理配置

前端通过 Vite 代理转发 API 请求，配置位于 `Chet.Admin.Web/apps/web-antd/vite.config.ts`：

```ts
server: {
  proxy: {
    '/api': {
      changeOrigin: true,
      target: 'http://localhost:5000',  // 后端地址
      ws: true,
    },
  },
},
```

如需修改后端端口，同步更新此处的 `target`。

### 6.2 API 基础路径

前端请求基础路径由 `.env.development` 中的 `VITE_GLOB_API_URL=/api/v1` 决定，所有业务接口都以 `/api/v1` 开头。

## 7. 常见问题

### Q: 后端启动报错 "无法连接数据库"？

A: 开发环境使用 SQLite，数据库文件 `Chet.Admin.Api/Chet.Admin.Api/Chet.Admin.db` 会在首次启动自动创建。如遇异常，删除该文件及 `-shm`、`-wal` 后缀文件后重启。

### Q: 前端登录提示 401 或 "网络错误"？

A: 请确认后端已启动且监听在 `http://localhost:5000`。检查 `vite.config.ts` 中的代理 target 是否与后端端口一致。

### Q: Swagger 无法访问？

A: Swagger 仅在开发环境启用。确认 `ASPNETCORE_ENVIRONMENT` 环境变量为 `Development`（`launchSettings.json` 已默认配置）。

### Q: pnpm install 失败？

A: 前端要求 Node.js 22.18+ 与 pnpm 11+。可通过 `corepack enable` 启用 pnpm，或运行 `pnpm env use --global 22` 切换 Node 版本。

### Q: 如何启用 Redis 缓存？

A: 修改 `appsettings.json` 中 `AppSettings.Redis.Enabled` 为 `true`，并配置 `ConnectionString`。后端在 Redis 不可用时会自动降级为 NoOp 缓存，不影响运行。
