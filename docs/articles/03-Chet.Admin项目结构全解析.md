# Chet.Admin 项目结构全解析：前后端目录一图看懂 🗂️

> 《Chet.Admin 全栈实战》系列第 3 篇

---

## 前言

上一篇我们把项目跑起来了，但项目一打开可能会懵：

- 后端怎么这么多项目？
- 前端 `packages/` `apps/` `internal/` 都是啥？
- 新增一个模块，代码该放哪？

这篇就**把目录结构彻底讲透**：

- ✅ 后端四层架构目录树
- ✅ 前端 Monorepo 目录树
- ✅ 每层职责 + 依赖方向
- ✅ 关键文件定位
- ✅ 新增模块的目录约定

---

## 一、整体目录结构

Chet.Admin 是**前后端分离**的项目，根目录两个独立工程：

```
Chet.Admin/
├── Chet.Admin.Api/      # 🟢 后端工程（.NET 10 解决方案）
├── Chet.Admin.Web/      # 🔵 前端工程（Vue3 + Vben Admin Monorepo）
├── docs/                # 📚 文档（你正在看的就在这）
│   ├── articles/        #   博客系列文章
│   └── screenshots/     #   截图资源
└── .gitignore
```

**前后端完全独立**：

- 可以用不同 IDE 打开
- 可以独立部署
- 通过 HTTP API 通信

---

## 二、后端目录结构（.NET 10）

### 整体概览

后端是 **Clean Architecture + DDD** 设计，4 个项目 + 1 个测试项目：

```
Chet.Admin.Api/
├── Chet.Admin.Api/              # 1️⃣ 表示层（Web API）
├── Chet.Admin.Application/      # 2️⃣ 应用层（业务逻辑）
├── Chet.Admin.Core/             # 3️⃣ 核心层（领域 + 契约 + 共享）
├── Chet.Admin.Infrastructure/   # 4️⃣ 基础设施层（数据访问 + 缓存 + 日志）
├── Chet.Admin.Tests/            # 单元测试 + 集成测试
├── Chet.Admin.slnx              # 解决方案文件
├── Dockerfile                   # Docker 构建文件
├── docker-compose.yml           # Docker Compose 编排
└── README.md
```

> 💡 用 Visual Studio / Rider 打开 `Chet.Admin.slnx` 即可加载整个解决方案。

### 分层依赖方向

```
        ┌──────────────────┐
        │   Chet.Admin.Api  │  ← 表示层（最外层）
        │   (Controllers)   │
        └────────┬─────────┘
                 │ 依赖
        ┌────────▼─────────┐
        │  Application     │  ← 应用层（业务逻辑）
        │  (Services)      │
        └────────┬─────────┘
                 │ 依赖
        ┌────────▼─────────┐
        │     Core         │  ← 核心层（领域 + 契约）
        │ (Domain/Contracts)│   ⚠️ 不依赖任何其他层
        └────────▲─────────┘
                 │ 实现
        ┌────────┴─────────┐
        │ Infrastructure   │  ← 基础设施层（实现 Core 的接口）
        │ (Data/Caching)   │
        └──────────────────┘
```

**核心原则**：**依赖方向始终向内指向 Core 层**，Core 层零外部依赖。

---

### 1️⃣ 表示层：Chet.Admin.Api

```
Chet.Admin.Api/
├── Controllers/               # 12 个 API 控制器
│   ├── AuthController.cs       #   认证（登录/注册/刷新令牌）
│   ├── UsersController.cs      #   用户管理
│   ├── RolesController.cs      #   角色管理
│   ├── MenusController.cs      #   菜单管理
│   ├── DepartmentsController.cs #   部门管理
│   ├── DictionariesController.cs #  字典管理
│   ├── AuditLogsController.cs  #   操作日志
│   ├── NotificationsController.cs # 通知公告
│   ├── FilesController.cs      #   文件管理
│   ├── OnlineUsersController.cs #  在线用户
│   ├── DashboardController.cs  #   仪表盘
│   └── HealthController.cs     #   健康检查
│
├── Middleware/                 # 中间件
│   ├── AuditLogMiddleware.cs       # 操作审计日志
│   ├── LogContextMiddleware.cs     # 日志上下文
│   ├── OnlineUserTrackingMiddleware.cs # 在线用户追踪
│   └── RateLimitingMiddleware.cs   # 限流
│
├── Filters/
│   └── ApiExceptionFilter.cs   # 全局异常过滤器
│
├── Configurations/            # DI 注册扩展（11 个配置类）
│   ├── ApiVersionConfiguration.cs   #   API 版本控制
│   ├── CorsConfiguration.cs        #   跨域配置
│   ├── DatabaseConfiguration.cs    #   数据库配置
│   ├── ExceptionHandlingConfiguration.cs # 异常处理
│   ├── FluentValidationConfiguration.cs # 参数校验
│   ├── JwtConfiguration.cs         #   JWT 认证
│   ├── RedisConfiguration.cs       #   Redis 缓存
│   ├── RepositoryConfiguration.cs  #   仓储注册
│   ├── SerilogConfiguration.cs     #   日志配置
│   ├── ServiceConfiguration.cs     #   业务服务注册
│   └── SwaggerConfiguration.cs     #   Swagger 文档
│
├── Properties/
│   └── launchSettings.json     # 启动配置（端口等）
│
├── Program.cs                  # 🎯 应用入口
├── appsettings.json            # 主配置
├── appsettings.Development.json # 开发环境配置
└── Chet.Admin.Api.csproj        # 项目文件
```

**职责**：

- 接收 HTTP 请求，路由到 Controller
- 通过中间件管道处理：异常 → 日志 → CORS → 限流 → 认证 → 审计 → 业务
- **不包含业务逻辑**，只做参数校验和调用 Service

---

### 2️⃣ 应用层：Chet.Admin.Application

```
Chet.Admin.Application/
├── Chet.Admin.Services/        # 业务服务实现
│   ├── Auth/
│   │   ├── AuthService.cs      #   认证服务（登录/注册/刷新）
│   │   └── CaptchaService.cs   #   验证码服务
│   ├── Jwt/
│   │   └── JwtService.cs       #   JWT 令牌服务
│   ├── User/
│   │   ├── UserService.cs      #   用户服务
│   │   └── OnlineUserService.cs #  在线用户服务
│   ├── Role/
│   │   ├── RoleService.cs      #   角色服务
│   │   └── DataScopeService.cs #   数据权限服务
│   ├── Menu/
│   │   └── MenuService.cs      #   菜单服务
│   ├── Department/
│   │   └── DepartmentService.cs #  部门服务
│   ├── Dictionary/
│   │   └── DictionaryService.cs #  字典服务
│   ├── Audit/
│   │   └── AuditLogService.cs  #   审计日志服务
│   ├── Notification/
│   │   └── NotificationService.cs # 通知服务
│   ├── File/
│   │   └── FileService.cs      #   文件服务
│   ├── Dashboard/
│   │   └── DashboardService.cs  #   仪表盘服务
│   └── Security/
│       └── PasswordService.cs  #   密码哈希服务
│
├── Chet.Admin.DTOs/            # 数据传输对象
│   ├── Auth/                   #   认证相关 DTO
│   │   ├── LoginDto.cs
│   │   ├── JwtTokenDto.cs
│   │   ├── CaptchaDto.cs
│   │   └── ...
│   ├── User/                  #   用户相关 DTO
│   │   ├── UserDto.cs
│   │   ├── UserCreateDto.cs
│   │   ├── Validators/        #     FluentValidation 校验器
│   │   │   ├── LoginDtoValidator.cs
│   │   │   └── ...
│   │   └── ...
│   ├── Role/ Menu/ Department/ ... # 各模块 DTO
│   └── Chet.Admin.DTOs.csproj
│
└── Chet.Admin.Mapping/         # AutoMapper 映射配置
    ├── User/
    │   └── MappingProfile.cs
    ├── Role/
    │   └── MappingProfile.cs
    └── ...
```

**职责**：

- 实现 **业务逻辑**（Service 层）
- 定义 **DTO**（Data Transfer Object）用于接口数据交换
- 用 **AutoMapper** 做实体 ↔ DTO 转换
- 用 **FluentValidation** 做参数校验

> 💡 Service 依赖的是 Core 层的**接口**（`IUserService`），不直接依赖 Infrastructure 的实现。

---

### 3️⃣ 核心层：Chet.Admin.Core

```
Chet.Admin.Core/
├── Chet.Admin.Domain/          # 领域实体（最纯粹的业务模型）
│   ├── BaseEntity.cs           #   实体基类（Id/CreatedAt/UpdatedAt）
│   ├── User/
│   │   └── UserEntity.cs       #   用户实体
│   ├── Role/
│   │   ├── RoleEntity.cs       #   角色实体
│   │   ├── UserRoleEntity.cs   #   用户-角色关联
│   │   └── RoleMenuEntity.cs   #   角色-菜单关联
│   ├── Menu/
│   │   └── MenuEntity.cs       #   菜单实体
│   ├── Department/
│   │   └── DepartmentEntity.cs  #   部门实体
│   ├── Dictionary/
│   │   └── DictionaryEntity.cs  #   字典实体
│   ├── Audit/
│   │   └── AuditLogEntity.cs   #   审计日志实体
│   ├── Notification/
│   │   └── NotificationEntity.cs # 通知实体
│   └── File/
│       └── FileEntity.cs       #   文件实体
│
├── Chet.Admin.Contracts/       # 接口契约（Service + Repository）
│   ├── IRepository.cs          #   🎯 泛型仓储接口
│   ├── IUnitOfWork.cs          #   🎯 工作单元接口
│   ├── User/
│   │   ├── IUserService.cs     #     用户服务接口
│   │   ├── IUserRepository.cs  #     用户仓储接口
│   │   └── IOnlineUserService.cs #   在线用户接口
│   ├── Role/
│   │   ├── IRoleService.cs
│   │   ├── IRoleRepository.cs
│   │   └── IDataScopeService.cs
│   ├── Auth/                   #   认证接口
│   ├── Jwt/                    #   JWT 接口
│   ├── Menu/ Department/ ... # 其他模块接口
│   └── Chet.Admin.Contracts.csproj
│
└── Chet.Admin.Shared/          # 共享工具类
    ├── Api/
    │   ├── ApiResponse.cs          #   统一响应格式
    │   └── UtcDateTimeJsonConverter.cs # UTC 时间转换器
    ├── Caching/
    │   └── CacheKeys.cs            #   缓存键定义
    ├── Exception/
    │   ├── BadRequestException.cs #   业务异常
    │   └── NotFoundException.cs
    └── Chet.Admin.Shared.csproj
```

**职责**：

- **Domain**：纯领域模型，无任何外部依赖
- **Contracts**：定义接口契约，Service 和 Repository 的抽象
- **Shared**：跨层共享的工具类（异常、响应格式等）

> ⚠️ Core 层**不引用任何其他项目**，是整个架构的中心。

---

### 4️⃣ 基础设施层：Chet.Admin.Infrastructure

```
Chet.Admin.Infrastructure/
├── Chet.Admin.Data/            # EF Core 数据访问
│   ├── AppDbContext.cs         #   🎯 数据库上下文
│   ├── EfCoreRepository.cs     #   🎯 泛型仓储实现
│   ├── UnitOfWork.cs           #   🎯 工作单元实现
│   ├── User/
│   │   ├── UserConfig.cs       #     用户表 EF 配置
│   │   └── UserRepository.cs   #     用户仓储实现
│   ├── Role/
│   │   ├── RoleConfig.cs
│   │   ├── RoleRepository.cs
│   │   └── RoleMenuConfig.cs
│   ├── Menu/ Department/ ... # 各模块配置 + 仓储
│   └── Chet.Admin.Data.csproj
│
├── Chet.Admin.Caching/         # 缓存实现
│   ├── RedisCacheService.cs    #   Redis 缓存
│   ├── NoOpCacheService.cs     #   空实现（降级用）
│   └── Chet.Admin.Caching.csproj
│
├── Chet.Admin.Configuration/   # 强类型配置
│   ├── AppSettings.cs          #   读取 appsettings.json
│   └── Chet.Admin.Configuration.csproj
│
└── Chet.Admin.Logging/         # 日志配置
    ├── LogContextHelper.cs
    ├── SensitiveDataLogFilter.cs # 敏感数据过滤
    └── Chet.Admin.Logging.csproj
```

**职责**：

- **Data**：EF Core 实现，继承 Core 的仓储接口
- **Caching**：Redis 实现 + NoOp 降级
- **Configuration**：强类型配置对象
- **Logging**：Serilog 配置 + 敏感数据脱敏

> 💡 Infrastructure 实现 Core 定义的接口，**依赖方向指向 Core**。

---

### 测试项目

```
Chet.Admin.Tests/
├── Chet.Admin.UnitTests/       # 单元测试
│   ├── UserServiceTests.cs
│   └── Chet.Admin.UnitTests.csproj
└── Chet.Admin.IntegrationTests/ # 集成测试
    ├── UserServiceIntegrationTests.cs
    └── Chet.Admin.IntegrationTests.csproj
```

---

## 三、前端目录结构（Vben Admin Monorepo）

### 整体概览

前端基于 [Vben Admin v5.7](https://vben.pro)，采用 **pnpm Monorepo** 架构：

```
Chet.Admin.Web/
├── apps/           # 1️⃣ 应用入口（可部署的成品）
├── packages/       # 2️⃣ 核心包（被应用依赖）
├── internal/       # 3️⃣ 内部工具（构建/规范，不发布）
├── package.json    # 根 package.json（定义脚本）
├── pnpm-workspace.yaml
├── turbo.json
└── .npmrc
```

### Monorepo 三大模块

```
┌─────────────────────────────────────────┐
│              apps/                      │ ← 应用层（最终产物）
│  └── web-antd（Ant Design Vue 应用）    │
└────────────────┬────────────────────────┘
                 │ 依赖
┌────────────────▼────────────────────────┐
│           packages/                     │ ← 核心包（复用能力）
│  ├── @core/（基础/组件/UI Kit）         │
│  ├── constants/（常量）                  │
│  └── effects/（布局/权限/Hooks）        │
└────────────────┬────────────────────────┘
                 │ 依赖
┌────────────────▼────────────────────────┐
│           internal/                     │ ← 内部工具（不发布）
│  ├── lint-configs/（代码规范）         │
│  ├── vite-config/（构建配置）           │
│  ├── tsconfig/（TS 配置）               │
│  └── tailwind-config/（CSS 配置）      │
└─────────────────────────────────────────┘
```

---

### 1️⃣ 应用层：apps/

```
apps/
└── web-antd/                    # 主应用（Ant Design Vue）
    ├── public/
    │   └── favicon.ico
    ├── src/
    │   ├── api/                 # 🎯 API 请求层
    │   │   ├── core/            #   核心接口
    │   │   │   ├── auth.ts      #     认证 API
    │   │   │   ├── user.ts      #     用户 API
    │   │   │   └── menu.ts      #     菜单 API
    │   │   ├── system/          #   业务接口
    │   │   │   ├── user.ts      #     用户管理
    │   │   │   ├── role.ts      #     角色管理
    │   │   │   ├── menu.ts      #     菜单管理
    │   │   │   ├── department.ts #    部门管理
    │   │   │   ├── dictionary.ts #    字典管理
    │   │   │   ├── audit-log.ts #     操作日志
    │   │   │   ├── notification.ts #   通知公告
    │   │   │   ├── online-user.ts #   在线用户
    │   │   │   ├── file.ts      #     文件管理
    │   │   │   └── dashboard.ts #     仪表盘
    │   │   ├── request.ts       #     Axios 封装
    │   │   └── index.ts
    │   │
    │   ├── views/               # 🎯 页面
    │   │   ├── _core/           #   框架自带页面
    │   │   │   ├── authentication/
    │   │   │   │   └── login.vue #     登录页
    │   │   │   ├── fallback/    #     404/403/500 等
    │   │   │   └── profile/     #     个人中心
    │   │   ├── dashboard/       #   仪表盘
    │   │   │   ├── analytics/    #     分析页
    │   │   │   └── workspace/   #     工作台
    │   │   └── system/         #   系统管理（业务页）
    │   │       ├── user/        #     用户管理
    │   │       ├── role/        #     角色管理
    │   │       ├── menu/        #     菜单管理
    │   │       ├── department/  #     部门管理
    │   │       ├── dictionary/  #    字典管理
    │   │       ├── audit-log/   #     操作日志
    │   │       ├── notification/ #    通知管理
    │   │       ├── file/        #     文件管理
    │   │       └── online-user/ #     在线用户
    │   │
    │   ├── router/              # 🎯 路由
    │   │   ├── routes/
    │   │   │   ├── modules/     #     路由模块
    │   │   │   │   └── dashboard.ts
    │   │   │   ├── core.ts      #     核心路由
    │   │   │   └── index.ts
    │   │   ├── access.ts        #     权限路由
    │   │   ├── guard.ts         #     路由守卫
    │   │   └── index.ts
    │   │
    │   ├── store/               # 🎯 状态管理（Pinia）
    │   │   ├── auth.ts          #     认证状态
    │   │   └── index.ts
    │   │
    │   ├── composables/        # 组合式函数
    │   │   └── useDict.ts       #     字典 Hook
    │   │
    │   ├── layouts/            # 布局组件
    │   │   ├── basic.vue        #     基础布局
    │   │   ├── auth.vue        #     认证布局
    │   │   └── components/
    │   │       └── notification-bell.vue # 通知铃铛
    │   │
    │   ├── adapter/            # 组件适配器
    │   │   ├── form.ts          #     表单适配
    │   │   └── vxe-table.ts    #     表格适配
    │   │
    │   ├── locales/            # 国际化
    │   │   └── langs/
    │   │       ├── zh-CN/
    │   │       └── en-US/
    │   │
    │   ├── app.vue             # 根组件
    │   ├── main.ts             # 🎯 入口
    │   ├── bootstrap.ts        # 应用引导
    │   └── preferences.ts     # 偏好设置
    │
    ├── .env                    # 通用环境变量
    ├── .env.development        # 开发环境
    ├── .env.production         # 生产环境
    ├── vite.config.ts          # 🎯 Vite 配置
    ├── tsconfig.json
    └── package.json
```

**职责**：

- `api/`：封装后端接口调用
- `views/`：业务页面（每个文件夹对应一个菜单）
- `router/`：路由 + 权限守卫
- `store/`：Pinia 状态管理

---

### 2️⃣ 核心包：packages/

```
packages/
├── @core/                      # 核心能力
│   ├── base/                   #   基础能力
│   │   ├── design/             #     设计令牌 + 全局样式
│   │   ├── icons/              #     图标库（Lucide）
│   │   ├── shared/             #     工具函数（cache/color/utils）
│   │   └── typings/           #     类型定义
│   ├── composables/           #   组合式函数（useIsMobile 等）
│   ├── preferences/           #   偏好设置系统
│   └── ui-kit/                #   UI 组件库
│       ├── form-ui/           #     表单组件
│       ├── layout-ui/         #     布局组件
│       ├── menu-ui/           #     菜单组件
│       ├── popup-ui/          #     弹窗组件（Modal/Drawer/Alert）
│       ├── shadcn-ui/         #     Shadcn 组件（Button/Card/Dialog...）
│       └── tabs-ui/           #     标签页组件
│
├── constants/                 # 常量定义
└── effects/                   # 效果层
    ├── access/                #   权限控制（指令 + 组件）
    ├── common-ui/             #   通用 UI（about/fallback/profile）
    ├── hooks/                 #   业务 Hooks（watermark/pagination/tabs）
    ├── layouts/               #   布局（basic/authentication）
    └── plugins/               #   插件（echarts）
```

**职责**：

- `@core/`：UI 组件库 + 基础能力（可被多个 app 复用）
- `effects/`：业务效果（权限、布局、Hook）
- `constants/`：全局常量

> 💡 这些包通过 `workspace:*` 在 `apps/web-antd/package.json` 中被引用。

---

### 3️⃣ 内部工具：internal/

```
internal/
├── lint-configs/               # 代码规范
│   ├── commitlint-config/      #   提交规范
│   ├── eslint-config/          #   ESLint 配置
│   ├── oxlint-config/          #   OxLint 配置
│   ├── oxfmt-config/           #   代码格式化
│   └── stylelint-config/       #   样式规范
│
├── vite-config/                # Vite 构建配置
│   └── src/
│       ├── config/             #   应用/库配置
│       └── plugins/            #   Vite 插件
│
├── tsconfig/                  # TypeScript 配置
│   ├── base.json
│   ├── web.json
│   └── node.json
│
├── tailwind-config/           # Tailwind CSS 配置
└── node-utils/                # Node 工具函数
```

**职责**：

- 只在**开发时**用，不会被发布
- 统一所有子包的 lint / build / tsconfig 规则

---

## 四、关键文件速查表

### 后端关键文件

| 文件 | 路径 | 作用 |
| ---- | ---- | ---- |
| 应用入口 | `Api/Program.cs` | 启动配置中心 |
| DI 配置 | `Api/Configurations/*.cs` | 11 个注册扩展 |
| 数据库上下文 | `Infrastructure/Data/AppDbContext.cs` | EF Core 上下文 |
| 泛型仓储 | `Infrastructure/Data/EfCoreRepository.cs` | CRUD 基类 |
| 仓储接口 | `Core/Contracts/IRepository.cs` | 数据访问契约 |
| 工作单元 | `Core/Contracts/IUnitOfWork.cs` | 事务管理契约 |
| 实体基类 | `Core/Domain/BaseEntity.cs` | Id + 时间戳 |
| 业务服务 | `Application/Services/*` | 13 个服务 |
| 配置文件 | `Api/appsettings.json` | 数据库/JWT/Redis |

### 前端关键文件

| 文件 | 路径 | 作用 |
| ---- | ---- | ---- |
| 应用入口 | `apps/web-antd/src/main.ts` | Vue 挂载 |
| 路由配置 | `apps/web-antd/src/router/` | 动态路由 + 守卫 |
| 状态管理 | `apps/web-antd/src/store/auth.ts` | 登录状态 |
| API 封装 | `apps/web-antd/src/api/request.ts` | Axios 拦截器 |
| 业务页面 | `apps/web-antd/src/views/system/` | 9 个管理页面 |
| Vite 配置 | `apps/web-antd/vite.config.ts` | 代理 + 构建 |
| 环境变量 | `apps/web-antd/.env.development` | 端口 + API 地址 |

---

## 五、新增模块目录约定

假设要新增一个**订单管理**模块，代码该放哪？

### 后端：7 个文件

```
1. Core/Domain/Order/OrderEntity.cs          # 实体
2. Core/Contracts/Order/IOrderRepository.cs   # 仓储接口
3. Core/Contracts/Order/IOrderService.cs      # 服务接口
4. Infrastructure/Data/Order/OrderConfig.cs   # EF 配置
5. Infrastructure/Data/Order/OrderRepository.cs # 仓储实现
6. Application/Services/Order/OrderService.cs # 服务实现
7. Application/DTOs/Order/OrderDto.cs        # DTO
   + Api/Controllers/OrdersController.cs      # 控制器
```

然后别忘了：

- 在 `RepositoryConfiguration.cs` 注册：`services.AddScoped<IOrderRepository, OrderRepository>();`
- 在 `ServiceConfiguration.cs` 注册：`services.AddScoped<IOrderService, OrderService>();`
- 在 `AppDbContext.cs` 加：`public DbSet<OrderEntity> Orders { get; set; }`
- 创建 EF 迁移：`dotnet ef migrations add AddOrder`

### 前端：3 个文件

```
1. apps/web-antd/src/api/system/order.ts     # API 封装
2. apps/web-antd/src/views/system/order/index.vue # 页面
3. apps/web-antd/src/router/routes/modules/order.ts # 路由
```

> 💡 完整的新增模块流程会在第 4 篇《后端分层架构》里详细展开。

---

## 六、命名规范

### 后端

| 类型 | 命名规则 | 示例 |
| ---- | ---- | ---- |
| 实体 | `{Name}Entity` | `UserEntity` |
| 接口 | `I{Name}` | `IUserService` |
| 服务 | `{Name}Service` | `UserService` |
| 仓储 | `{Name}Repository` | `UserRepository` |
| DTO | `{Name}Dto` | `UserDto` / `UserCreateDto` |
| 配置 | `{Name}Config` | `UserConfig` |
| 控制器 | `{Name}sController` | `UsersController` |

### 前端

| 类型 | 命名规则 | 示例 |
| ---- | ---- | ---- |
| 页面 | `views/{module}/index.vue` | `views/system/user/index.vue` |
| API | `api/{module}/{name}.ts` | `api/system/user.ts` |
| 组件 | `PascalCase.vue` | `NotificationBell.vue` |
| Hook | `use{Name}.ts` | `useDict.ts` |
| Store | `{name}.ts` | `auth.ts` |

---

## 七、配置文件一览

### 后端配置文件

```
Chet.Admin.Api/Chet.Admin.Api/
├── appsettings.json            # 主配置
├── appsettings.Development.json # 开发环境覆盖
└── Properties/launchSettings.json # 启动配置
```

### 前端配置文件

```
Chet.Admin.Web/
├── package.json                 # 根脚本
├── pnpm-workspace.yaml          # Monorepo 工作区
├── turbo.json                   # Turbo 配置
├── .npmrc                       # pnpm 配置
└── apps/web-antd/
    ├── .env                     # 通用
    ├── .env.development         # 开发环境
    ├── .env.production          # 生产环境
    └── vite.config.ts           # Vite 配置
```

---

## 八、依赖关系图

### 后端项目引用

```
Chet.Admin.Api  ──▶ Chet.Admin.Application ──▶ Chet.Admin.Core ◀── Chet.Admin.Infrastructure
     │                                          ▲
     └──────────────────────────────────────────┘
                       │
                       └──▶ Chet.Admin.Infrastructure（直接引用，配置 DI）
```

| 项目 | 引用 |
| ---- | ---- |
| Api | Application + Infrastructure（用于 DI 注册） |
| Application | Core |
| Infrastructure | Core |
| Core | 无（零依赖） |

### 前端包依赖

```
apps/web-antd
  ├── @vben/access (workspace:*)
  ├── @vben/common-ui (workspace:*)
  ├── @vben/hooks (workspace:*)
  ├── @vben/layouts (workspace:*)
  ├── @vben/request (workspace:*)
  ├── @vben/stores (workspace:*)
  ├── ant-design-vue (catalog:*)
  └── vue (catalog:*)
```

> 💡 `workspace:*` 表示 Monorepo 内部包，`catalog:*` 表示版本统一管理。

---

## 九、调试技巧

### 后端

- 用 Rider / VS 打开 `Chet.Admin.slnx`
- F5 启动调试，断点直接打在 Service 里
- 配合 `dotnet watch run` 实现热重载

### 前端

- 用 VSCode 打开 `Chet.Admin.Web`
- 装 Vue Official 插件
- F5 启动 Chrome 调试
- Vue DevTools 浏览器插件看组件树

### 同时调试前后端

VSCode 工作区文件（`.code-workspace`）：

```json
{
  "folders": [
    { "path": "Chet.Admin.Api" },
    { "path": "Chet.Admin.Web" }
  ]
}
```

---

## 十、下一步

搞懂目录结构后，接下来：

- 📖 第 4 篇：**后端分层架构**，深入 Clean Architecture + DDD 落地
- 📖 第 5 篇：**JWT 认证与安全**，双令牌怎么实现

---

## 互动

你们团队的项目结构是怎么组织的？分几层？评论区聊聊～

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#项目结构` `#CleanArchitecture` `#Monorepo` `#.NET10` `#Vue3`
