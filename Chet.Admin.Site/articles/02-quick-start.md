# Chet.Admin 5 分钟启动！前后端联调保姆教程 ⚡

> 《Chet.Admin 全栈实战》系列第 2 篇

---

## 前言

上一篇我们概览了 Chet.Admin 的整体设计，是不是已经跃跃欲试想把它跑起来了？

这篇就是**保姆级启动教程**：

- ✅ 环境要求一表看懂
- ✅ 后端 3 条命令跑起来
- ✅ 前端 3 条命令跑起来
- ✅ Vite 代理配置详解
- ✅ 默认账号 + 常见问题排查

**目标**：5 分钟内浏览器看到登录页，输入账号密码进系统 🚀

---

## 一、环境要求

在动手之前，先确认本机装了下面这些工具：

| 工具 | 最低版本 | 推荐版本 | 验证命令 |
| ---- | ---- | ---- | ---- |
| .NET SDK | 10.0 | 10.0.x | `dotnet --version` |
| Node.js | 22.18 | 22.x LTS | `node --version` |
| pnpm | 11.0 | 11.7.0 | `pnpm --version` |
| Git | 2.30+ | 最新 | `git --version` |
| IDE（可选） | - | VS 2026 / Rider / VSCode | - |

> ⚠️ **Node 版本很关键**：项目 `package.json` 里写死了 `engines.node: "^22.18.0 || ^24.0.0"`，低版本会直接报错。
>
> ⚠️ **pnpm 是必须的**：项目用了 `preinstall: npx only-allow pnpm`，用 npm / yarn 会被拦截。

### 安装 pnpm

如果还没装 pnpm，一行命令搞定：

```bash
# 任选一种
npm install -g pnpm@11.7.0
# 或
corepack enable && corepack prepare pnpm@11.7.0 --activate
```

### 数据库？

**不用装！** 默认用 **SQLite**（一个文件 `Chet.Admin.db`），EF Core 启动时会自动建库建表 + 灌种子数据。

Redis 也**默认关掉**了，开发环境零依赖。

---

## 二、拉代码

```bash
# GitHub
git clone https://github.com/qiect/Chet.Admin.git

# 或 Gitee（国内更快）
git clone https://gitee.com/qiect/Chet.Admin.git
```

项目结构长这样：

```
Chet.Admin/
├── Chet.Admin.Api/      # 后端（.NET 10）
├── Chet.Admin.Web/      # 前端（Vue3 + Vben Admin）
└── docs/                # 文档
```

前后端是**两个独立目录**，可以分别用不同的 IDE 打开。

---

## 三、后端启动

### 1. 进入后端目录

```bash
cd Chet.Admin/Chet.Admin.Api
```

### 2. 还原依赖

```bash
dotnet restore
```

第一次跑会下载 NuGet 包，大概 30 秒到 2 分钟，取决于网速。

### 3. 启动后端

```bash
dotnet run --project Chet.Admin.Api
```

看到这行日志就说明起来了：

```
[INF] Application started successfully. Listening on http://localhost:5000
```

### 4. 验证后端

打开浏览器访问：

- **Swagger 文档**：http://localhost:5000/swagger
- **根路径**：http://localhost:5000/（会自动重定向到 Swagger）

<!-- 后端Swagger界面截图 -->
![Swagger 界面](/screenshots/swagger.svg)

### 后端启动配置说明

后端端口在 `launchSettings.json` 里配置：

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5000;https://localhost:5001;",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- **HTTP 端口**：5000（开发用这个）
- **HTTPS 端口**：5001（开发环境不建议用，Vite 代理会冲突）
- **环境**：Development

### 数据库自动初始化

启动时会自动执行：

1. **应用迁移**：`MigrateAsync()` 增量更新 Schema
2. **种子数据**：`SeedDataAsync()` 首次启动灌入：
   - 2 个部门（总公司、技术部）
   - 2 个角色（admin 超管、user 普通用户）
   - 13+ 个菜单（含按钮权限）
   - 5 个字典类型 + 子项
   - **2 个默认账号**（见下文）
   - 近 7 天模拟登录审计日志（让仪表盘有图可看）

> 💡 想重新初始化？删掉 `Chet.Admin.Api/Chet.Admin.Api/Chet.Admin.db` 文件，重启后端即可。

---

## 四、前端启动

### 1. 进入前端目录

```bash
cd Chet.Admin/Chet.Admin.Web
```

### 2. 安装依赖

```bash
pnpm install
```

> ⏱ 第一次会装很久（Vben Admin 是 Monorepo，依赖多），大概 2-5 分钟。

### 3. 启动前端

```bash
pnpm dev:antd
```

`dev:antd` 是启动 `apps/web-antd` 这个 Ant Design Vue 应用的快捷命令，等价于：

```bash
pnpm -F @vben/web-antd run dev
```

启动成功后控制台会显示：

```
  VITE v7.x.x  ready in 800 ms

  ➜  Local:   http://localhost:5666/
```

### 4. 验证前端

浏览器打开 **http://localhost:5666**，看到登录页就成功了 🎉

<!-- 前端登录页截图 -->
![登录页](/screenshots/login.png)

---

## 五、Vite 代理配置详解

前后端要联调，靠的是 **Vite 开发服务器代理**。配置在 `apps/web-antd/vite.config.ts`：

```typescript
import { defineConfig } from '@vben/vite-config';

export default defineConfig(async () => {
  return {
    application: {},
    vite: {
      server: {
        proxy: {
          '/api': {
            changeOrigin: true,
            // 后端 API 地址
            target: 'http://localhost:5000',
            ws: true,
          },
          // 静态资源（上传的文件）代理
          '/uploads': {
            changeOrigin: true,
            target: 'http://localhost:5000',
          },
        },
      },
    },
  };
});
```

### 代理原理

```
浏览器                    Vite Dev Server              后端
  │  http://localhost:5666      │                      │
  │  /api/v1/auth/login  ──────▶│                      │
  │                              │  http://localhost:5000  │
  │                              │  /api/v1/auth/login ──▶│
  │                              │                      │
  │                              │  ◀── JWT Token ──────│
  │  ◀── Response ──────────────│                      │
```

- 前端所有 `/api/**` 请求会被转发到 `http://localhost:5000`
- 前端所有 `/uploads/**` 请求（图片、文件）也会转发
- **同源访问**，没有跨域问题，Cookie 也能正常带

### 前端环境变量

`apps/web-antd/.env.development` 关键配置：

```bash
# 前端端口
VITE_PORT=5666

# 接口地址前缀
VITE_GLOB_API_URL=/api/v1

# 关闭 Mock 服务
VITE_NITRO_MOCK=false
```

> 💡 如果后端端口改了（比如改成 6000），记得同步改 `vite.config.ts` 里的 `target`。

---

## 六、默认账号

种子数据内置了两个账号，开箱即用：

| 角色 | 邮箱 | 密码 | 权限 |
| ---- | ---- | ---- | ---- |
| 超级管理员 | `admin@example.com` | `Admin@123` | 所有菜单 + 所有按钮 |
| 普通用户 | `user@example.com` | `User@123` | 仅菜单查看，无增删改按钮 |

**登录后效果对比**：

- 用 `admin` 登录：看到所有菜单，按钮可点
- 用 `user` 登录：只看到菜单，"新增/编辑/删除"按钮**不会渲染**

这就是 Chet.Admin 的**按钮级权限**，不是简单的前端隐藏，而是后端不返回权限码 + 前端不渲染。

<!-- 仪表盘截图 -->
![仪表盘](/screenshots/dashboard.png)

---

## 七、CORS 配置（可选）

后端 `appsettings.json` 里有 CORS 白名单：

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173"
  ]
}
```

> ⚠️ 注意：默认白名单里**没有 5666**！

**为什么还能跑？** 因为开发环境用的是 Vite 代理（同源），根本走不到 CORS。如果你前端直连后端（不走代理），需要加上 `http://localhost:5666`。

---

## 八、Redis 配置（可选）

默认**关闭**，开发环境不需要：

```json
"Redis": {
  "Enabled": false,
  "ConnectionString": "localhost:6379",
  "InstanceName": "ChetAdmin:"
}
```

想启用？三步：

1. 本地装个 Redis（Docker 最快：`docker run -d -p 6379:6379 redis`）
2. 把 `Enabled` 改成 `true`
3. 重启后端

启用后：缓存查询走 Redis；不启用：自动降级到 **NoOp 缓存**（每次都查 DB），功能不受影响。

---

## 九、JWT 配置

开发环境默认配置（`appsettings.json`）：

```json
"Jwt": {
  "Enabled": true,
  "SecretKey": "YourSecretKeyForJWTAuthentication1234567890",
  "Issuer": "Chet.Admin",
  "Audience": "Chet.Admin",
  "AccessTokenExpirationInMinutes": 30,
  "RefreshTokenExpirationDays": 7
}
```

| 配置项 | 说明 | 默认值 |
| ---- | ---- | ---- |
| Enabled | 是否启用 JWT | true |
| SecretKey | 签名密钥（≥32 字节） | 内置开发密钥 |
| Issuer | 发行者 | Chet.Admin |
| Audience | 受众 | Chet.Admin |
| AccessTokenExpirationInMinutes | Access Token 有效期 | 30 分钟 |
| RefreshTokenExpirationDays | Refresh Token 有效期 | 7 天 |

> 🚨 **生产环境务必修改 SecretKey！** 这个密钥在 GitHub 上是公开的。

---

## 十、常见问题排查

### Q1：后端启动报错 "端口被占用"

```
Failed to bind to http://localhost:5000: address already in use
```

**解决**：

```bash
# 查 5000 端口被谁占了
netstat -ano | findstr :5000

# 杀掉进程（替换 PID）
taskkill /PID 12345 /F
```

或者改 `launchSettings.json` 里的端口（同步改前端 vite.config.ts 的 target）。

### Q2：前端启动报 "pnpm not found"

```
ERR_PNPM_NOT_FOUND
```

**原因**：没装 pnpm，或版本太低。

**解决**：

```bash
# 检查
pnpm --version

# 没装就装
npm install -g pnpm@11.7.0
```

### Q3：前端 `pnpm install` 报 "node version not satisfied"

```
ERR_PNPM_UNSUPPORTED_ENGINE
```

**原因**：Node 版本不够。

**解决**：升级到 Node 22 LTS 或 24。

```bash
node --version
# 必须是 v22.18+ 或 v24+
```

推荐用 [nvm](https://github.com/nvm-sh/nvm) 或 [fnm](https://github.com/Schniz/fnm) 管理多版本。

### Q4：前端 `pnpm install` 卡住不动

**原因**：网络问题（Vben Admin 依赖很多）。

**解决**：换镜像源

```bash
# .npmrc 文件加上
registry=https://registry.npmmirror.com
```

或者直接：

```bash
pnpm config set registry https://registry.npmmirror.com
pnpm install
```

### Q5：前端 pnpm workspace 链接报错

```
ERR_PNPM_WORKSPACE_CONFIG_ERROR
```

**原因**：Monorepo 内部包没链接上。

**解决**：

```bash
# 清掉重来
pnpm run clean
pnpm install
```

`clean` 脚本会删掉所有 `node_modules` 和 `dist`，然后重新安装。

### Q6：登录提示 "邮箱或密码错误"

**检查清单**：

- ✅ 邮箱拼写：`admin@example.com`（不是 admin@chet.com）
- ✅ 密码大小写：`Admin@123`（A 大写，@ 是 at 符号）
- ✅ 数据库有没有种子数据？删掉 `Chet.Admin.db` 重启后端

### Q7：前端页面空白，控制台报 401

**原因**：Token 过期或没带。

**解决**：

1. 清浏览器 localStorage
2. 重新登录

### Q8：登录成功但接口都报 500

**原因**：数据库没初始化成功。

**解决**：

```bash
# 看后端日志
# 找 "Initializing database..." 后面的报错

# 最常见：db 文件权限问题
# 删掉重启
del Chet.Admin.Api\Chet.Admin.Api\Chet.Admin.db
dotnet run --project Chet.Admin.Api
```

### Q9：Swagger 打不开

**原因**：环境不是 Development。

**解决**：

```bash
# 设置环境变量
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project Chet.Admin.Api
```

### Q10：想用 HTTPS

后端默认同时监听 HTTP(5000) + HTTPS(5001)，但开发环境**不建议用** HTTPS：

- Vite 代理配的是 `http://localhost:5000`
- HTTPS 会触发证书信任问题

生产环境用 Nginx 反代 + Let's Encrypt 即可，后端不需要自己处理 HTTPS。

---

## 十一、IDE 推荐配置

### 后端（.NET）

- **Visual Studio 2026**（Windows）
- **JetBrains Rider**（跨平台，推荐）
- **VSCode + C# Dev Kit**

打开 `Chet.Admin.Api/Chet.Admin.slnx` 解决方案文件。

### 前端（Vue3）

- **VSCode + Volar**
- **WebStorm**
- **Cursor**

打开 `Chet.Admin.Web` 目录即可，IDE 会自动识别 Monorepo。

### 同时开发前后端

推荐用 **VSCode 工作区**：

1. 创建一个 `.code-workspace` 文件
2. 添加 `Chet.Admin.Api` 和 `Chet.Admin.Web` 两个文件夹
3. 一个窗口搞定

---

## 十二、开发调试技巧

### 后端热重载

```bash
dotnet watch run --project Chet.Admin.Api
```

改 C# 代码自动重启，比 `dotnet run` 爽多了。

### 前端热更新

`pnpm dev:antd` 默认就开了 HMR（Hot Module Replacement），改 Vue 文件秒级生效。

### 联调后端 API

前端请求都带 `/api/v1` 前缀，比如登录：

```
POST http://localhost:5666/api/v1/auth/login
```

实际上经过 Vite 代理转发到：

```
POST http://localhost:5000/api/v1/auth/login
```

你可以在后端 Swagger 直接测，也可以用 Postman / Apifox 测 `http://localhost:5000/api/v1/...`。

### 查看后端日志

日志文件在：

```
Chet.Admin.Api/Chet.Admin.Api/logs/log-2026-07-10.txt
```

格式是 **Serilog Compact JSON**，方便结构化检索。

---

## 十三、一键启动脚本（可选）

如果你嫌每次敲两遍命令麻烦，可以写个脚本：

### Windows PowerShell

```powershell
# start-dev.ps1
Start-Process powershell -ArgumentList "-Command","cd Chet.Admin.Api; dotnet run --project Chet.Admin.Api"
Start-Process powershell -ArgumentList "-Command","cd Chet.Admin.Web; pnpm dev:antd"
```

### 或用 concurrently（推荐）

根目录装个工具：

```bash
npm install -g concurrently
```

然后：

```bash
concurrently "cd Chet.Admin.Api && dotnet run --project Chet.Admin.Api" "cd Chet.Admin.Web && pnpm dev:antd"
```

---

## 十四、启动检查清单

启动完成后，对照这张表逐项确认：

| 检查项 | 期望结果 | 命令/操作 |
| ---- | ---- | ---- |
| .NET SDK | 10.0.x | `dotnet --version` |
| Node.js | v22.18+ | `node --version` |
| pnpm | 11.7+ | `pnpm --version` |
| 后端运行 | http://localhost:5000/swagger 可访问 | 浏览器打开 |
| 前端运行 | http://localhost:5666 可访问 | 浏览器打开 |
| 数据库文件 | Chet.Admin.db 存在 | 看后端目录 |
| admin 登录 | 成功进仪表盘 | `admin@example.com / Admin@123` |
| user 登录 | 进系统，但无按钮 | `user@example.com / User@123` |

全部 ✅ 就说明环境搭好了，可以开撸代码了！

---

## 十五、Docker 启动（可选）

项目根目录有 `Dockerfile` 和 `docker-compose.yml`，也可以用 Docker：

```bash
cd Chet.Admin.Api
docker build -t chet-admin-api .
docker run -d -p 5000:5000 chet-admin-api
```

或者用 compose：

```bash
docker-compose up -d
```

> 💡 Docker 启动适合**演示**，开发调试还是用本地 `dotnet run` + `pnpm dev:antd`。

---

## 十六、下一步

环境搭好了，接下来：

- 📖 第 3 篇：**项目结构全解析**，搞懂目录怎么组织的
- 📖 第 4 篇：**后端分层架构**，理解 Clean Architecture 怎么落地
- 📖 第 5 篇：**JWT 认证与安全**，看看双令牌怎么玩

---

## 互动

启动过程中遇到什么坑？评论区贴出来，我帮你排！👇

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#快速上手` `#.NET10` `#Vue3` `#VbenAdmin` `#全栈开发`
