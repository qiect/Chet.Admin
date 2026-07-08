# Chet Admin 管理系统使用说明

## 概述

Chet Admin 是一套基于 **Vben Admin v5.7 + .NET Core WebAPI** 的全栈 RBAC 权限管理系统，采用前后端分离架构：

- **后端**：.NET Core WebAPI + EF Core (SQLite) + JWT 认证 + AutoMapper
- **前端**：Vue 3 + Vite + Ant Design Vue + TypeScript + Pinia

核心功能包括：用户管理、角色管理、菜单管理、部门管理、权限管理、字典管理，以及完整的按钮级权限控制。

---

## 前置条件

| 环境 | 版本要求 |
|------|---------|
| .NET SDK | 8.0+ |
| Node.js | 18.x+ |
| pnpm | 8.x+ |

---

## 快速开始

### 1. 克隆项目

```bash
# 后端
git clone <backend-repo-url> Chet.Admin
cd Chet.Admin

# 前端
git clone <frontend-repo-url> Chet.Admin.Web
cd Chet.Admin.Web
```

### 2. 启动后端

```bash
cd Chet.Admin\Chet.Admin.Api
dotnet restore
dotnet run
```

后端默认运行在 **http://localhost:5000**，首次启动会自动创建 SQLite 数据库并初始化种子数据。

### 3. 启动前端

```bash
cd Chet.Admin.Web
pnpm install
pnpm dev
```

前端默认运行在 **http://localhost:5666**，Vite 开发服务器自动将 `/api` 请求代理到后端。

### 4. 登录系统

浏览器打开 http://localhost:5666，使用默认管理员账号登录：

| 项目 | 值 |
|------|-----|
| 邮箱 | `admin@example.com` |
| 密码 | `Admin@123` |

> 首次登录建议按 `Ctrl+Shift+Delete` 清除浏览器 localStorage，确保最新配置生效。

---

## 系统架构

### 项目结构

```
Chet.Admin/                    # 后端项目根目录
├── Chet.Admin.Api/            # API 层（控制器、配置、启动入口）
│   ├── Controllers/                     # API 控制器
│   ├── Configurations/                  # JWT、数据库、CORS、Swagger 等配置
│   └── Program.cs                       # 应用启动入口
├── Chet.Admin.Application/    # 应用层（DTO、服务、验证器）
│   ├── Chet.Admin.DTOs/       # 数据传输对象
│   ├── Chet.Admin.Services/   # 业务服务实现
│   └── Chet.Admin.Contracts/  # 服务接口定义
├── Chet.Admin.Core/           # 核心层（实体、异常、公共类型）
│   └── Chet.Admin.Domain/     # 领域实体
└── Chet.Admin.Infrastructure/ # 基础设施层（数据访问、缓存）
    ├── Chet.Admin.Data/       # EF Core DbContext、仓储实现
    └── Chet.Admin.Caching/    # 缓存服务（内存/Redis）

Chet.Admin.Web/                          # 前端项目根目录（pnpm monorepo）
└── apps/web-antd/                       # Ant Design Vue 应用
    └── src/
        ├── api/                         # API 请求定义
        │   ├── core/                    # 认证相关 API
        │   └── system/                  # 系统管理 API
        ├── views/                       # 页面组件
        │   ├── dashboard/               # 仪表盘/欢迎页
        │   └── system/                  # 系统管理页面
        ├── adapter/                     # Vben 组件适配器
        ├── store/                       # Pinia 状态管理
        └── preferences.ts              # 全局偏好配置
```

### RBAC 权限模型

```
用户(User) ←N:N→ 角色(Role) ←N:N→ 权限(Permission)
                    ↕
              角色(Role) ←N:N→ 菜单(Menu)
                    
用户(User) → 部门(Department)
菜单(Menu) → 权限(Permission)
```

- **用户**：可分配多个角色，隶属于一个部门
- **角色**：可分配多个菜单和权限
- **菜单**：树形结构，支持目录/菜单/按钮三种类型
- **权限**：细粒度权限码（如 `system:user:create`），关联到菜单
- **部门**：树形组织架构

---

## 功能模块说明

### 仪表盘

登录后默认进入欢迎页，包含：
- 实时时钟显示
- 4 个统计卡片（用户数、角色数、菜单数、部门数）
- 6 个快捷入口（各管理模块直达）
- 系统信息展示

支持浅色/深色主题切换，图标使用 Lucide 图标库。

### 用户管理

**路径**：系统管理 > 用户管理

| 操作 | 说明 |
|------|------|
| 新增用户 | 填写用户名、邮箱、密码（含强度提示）、确认密码、部门、角色 |
| 编辑用户 | 仅修改用户名、邮箱、部门、角色（不涉及密码） |
| 修改密码 | 独立弹窗，输入新密码+确认密码，含强度提示 |
| 删除用户 | 二次确认后删除 |

密码强度实时检测规则：

| 评分条件 | 强度 |
|----------|------|
| 长度≥6 且 长度<10 | 弱（红色） |
| 长度≥6 且 含大小写 或 数字 | 中（黄色） |
| 长度≥10 且 含大小写+数字+特殊字符 | 强（绿色） |

### 角色管理

**路径**：系统管理 > 角色管理

| 操作 | 说明 |
|------|------|
| 新增/编辑/删除角色 | 基础 CRUD |
| 分配权限 | 弹窗中展示菜单-权限统一树，勾选即分配 |

角色分配权限时，菜单作为父节点、权限作为子节点，勾选后自动提取 `menuIds` 和 `permissionIds`。

### 菜单管理

**路径**：系统管理 > 菜单管理

菜单支持三种类型：

| 类型 | 说明 | 示例 |
|------|------|------|
| 目录 | 仅作为分组容器 | 系统管理 |
| 菜单 | 可点击的页面路由 | 用户管理 |
| 按钮 | 页面内的操作按钮 | 新增用户 |

菜单列表使用扁平数据 + `parentId` 通过 vxe-table `treeConfig.transform` 自动构建树形显示。

### 部门管理

**路径**：系统管理 > 部门管理

树形组织架构管理，支持上级部门选择（TreeSelect），编辑时排除自身节点避免循环引用。

### 权限管理

**路径**：系统管理 > 权限管理

管理细粒度权限码，每个权限关联一个菜单。权限码命名规范：

```
{模块}:{资源}:{操作}
```

示例：`system:user:create`、`system:role:update`、`system:menu:delete`

### 字典管理

**路径**：系统管理 > 字典管理

管理系统中使用的各类字典数据（如状态、类型等枚举值）。

---

## 权限控制

### 后端认证流程

```
1. 用户登录 → AuthController.Login()
2. 验证邮箱/密码 → 生成 JWT AccessToken + RefreshToken
3. JWT 包含 ClaimTypes.NameIdentifier(userId) + ClaimTypes.Role(roleName) + "permission" claims
4. 后续请求携带 Authorization: Bearer {token}
5. 过期/无效 → 返回 401，前端跳转登录页
```

### 前端权限控制

```
1. 登录成功 → 调用 getUserInfoApi()
2. 后端返回用户信息 + permissions 权限码列表
3. accessStore.setAccessCodes(permissions) 存入状态
4. 菜单按权限过滤（后端模式：只返回有权限的菜单）
5. 按钮按权限码控制可见性
```

### 按钮级权限

| 控制方式 | 用法 | 场景 |
|---------|------|------|
| `v-if` | `v-if="hasAccessByCodes(['system:user:create'])"` | 工具栏按钮 |
| `auth` 属性 | `{ text: '编辑', auth: 'system:user:update', onClick }` | 表格操作列 |
| 后端菜单过滤 | 按钮类型菜单按权限返回 | 菜单可见性 |

**默认权限码清单**：

| 模块 | 权限码 |
|------|--------|
| 用户 | `system:user:list` / `create` / `update` / `delete` |
| 角色 | `system:role:list` / `create` / `update` / `delete` |
| 菜单 | `system:menu:list` / `create` / `update` / `delete` |
| 部门 | `system:dept:list` / `create` / `update` / `delete` |
| 字典 | `system:dict:list` / `create` / `update` / `delete` |
| 权限 | `system:permission:list` / `create` / `update` / `delete` |

---

## API 接口一览

所有 API 基础路径：`/api/v1/`，需在请求头携带 `Authorization: Bearer {token}`。

### 认证接口

| 方法 | 路径 | 说明 | 是否需认证 |
|------|------|------|-----------|
| POST | `/api/v1/auth/register` | 用户注册 | 否 |
| POST | `/api/v1/auth/login` | 用户登录 | 否 |
| POST | `/api/v1/auth/refresh-token` | 刷新令牌 | 否 |
| POST | `/api/v1/auth/logout` | 退出登录 | 是 |
| GET | `/api/v1/auth/user-info` | 获取当前用户信息+权限 | 是 |

### 用户接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/users` | 获取所有用户 |
| GET | `/api/v1/users/paged?pageNumber=1&pageSize=20&keyword=` | 分页查询用户 |
| GET | `/api/v1/users/{id}` | 获取用户详情 |
| POST | `/api/v1/users` | 创建用户 |
| PUT | `/api/v1/users/{id}` | 更新用户（仅传需要修改的字段） |
| DELETE | `/api/v1/users/{id}` | 删除用户 |
| POST | `/api/v1/users/{id}/roles` | 分配角色 |

### 角色接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/roles` | 获取所有角色 |
| GET | `/api/v1/roles/paged` | 分页查询角色 |
| POST | `/api/v1/roles` | 创建角色 |
| PUT | `/api/v1/roles/{id}` | 更新角色 |
| DELETE | `/api/v1/roles/{id}` | 删除角色 |
| GET | `/api/v1/roles/{id}/permissions` | 获取角色权限 |
| POST | `/api/v1/roles/{id}/permissions` | 分配角色权限 |
| GET | `/api/v1/roles/{id}/menus` | 获取角色菜单 |
| POST | `/api/v1/roles/{id}/menus` | 分配角色菜单 |

### 菜单 / 部门 / 权限 / 字典接口

各模块均提供标准 CRUD 接口：`GET`（列表/分页/详情）、`POST`（创建）、`PUT`（更新）、`DELETE`（删除）。

菜单和部门额外提供：
- `GET /api/v1/menus/all` — 获取扁平菜单列表
- `GET /api/v1/menus/tree` — 获取菜单树
- `GET /api/v1/departments/all` — 获取扁平部门列表
- `GET /api/v1/departments/tree` — 获取部门树

---

## 配置说明

### 后端配置

**appsettings.json** 关键配置项：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Chet.Admin.db"   // SQLite 数据库文件
  },
  "AppSettings": {
    "Jwt": {
      "Enabled": true,                                         // 启用 JWT 认证
      "SecretKey": "YourSecretKeyForJWT...",                    // 签名密钥（生产环境务必更换）
      "AccessTokenExpirationInMinutes": 30,                     // Access Token 有效期（分钟）
      "RefreshTokenExpirationDays": 7                           // Refresh Token 有效期（天）
    },
    "Redis": {
      "Enabled": false,                                         // 启用 Redis 缓存（默认关闭，使用内存缓存）
      "ConnectionString": "localhost:6379"
    }
  }
}
```

> **安全提示**：生产环境请务必更换 `Jwt.SecretKey`，建议至少 32 位随机字符串。

### 前端配置

**环境变量** (.env / .env.development)：

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `VITE_APP_TITLE` | `Chet Admin` | 应用标题 |
| `VITE_PORT` | `5666` | 开发服务器端口 |
| `VITE_GLOB_API_URL` | `/api/v1` | API 基础路径 |

**偏好配置** (preferences.ts)：

| 配置项 | 值 | 说明 |
|--------|-----|------|
| `accessMode` | `'backend'` | 后端权限模式，菜单从 API 获取 |
| `enableRefreshToken` | `true` | 启用 Refresh Token |
| `defaultHomePath` | `'/dashboard'` | 登录后默认首页 |

---

## 默认种子数据

系统首次启动时自动初始化以下数据：

### 管理员账号

| 字段 | 值 |
|------|-----|
| 用户名 | Admin |
| 邮箱 | admin@example.com |
| 密码 | Admin@123 |
| 角色 | admin |

### 预置角色

| 角色 | 说明 |
|------|------|
| admin | 管理员，拥有全部权限 |
| user | 普通用户，仅有列表查看权限 |

### 预置菜单

```
系统管理（目录）
├── 用户管理（菜单）
│   ├── 新增（按钮）
│   ├── 编辑（按钮）
│   └── 删除（按钮）
├── 角色管理（菜单）
├── 菜单管理（菜单）
├── 部门管理（菜单）
├── 字典管理（菜单）
└── 权限管理（菜单）
```

---

## 开发指南

### 新增一个管理模块

以"日志管理"为例：

**后端**：

1. 在 `Domain/` 下创建实体 `LogEntity.cs`
2. 在 `Data/` 下创建仓储 `LogRepository.cs` 并注册到 `ServiceConfiguration`
3. 在 `DTOs/` 下创建 `LogDto.cs`、`LogCreateDto.cs`、`LogUpdateDto.cs`
4. 在 `Services/` 下创建 `LogService.cs` 实现业务逻辑
5. 在 `Controllers/` 下创建 `LogsController.cs` 暴露 API 端点
6. 在 `DatabaseConfiguration.cs` 种子数据中添加菜单和权限

**前端**：

1. 在 `src/api/system/` 下创建 `log.ts` 定义 API 请求函数
2. 在 `src/views/system/` 下创建 `log/index.vue` 页面组件
3. 使用 `useVbenVxeGrid` + `useVbenForm` + `useVbenModal` 组合实现 CRUD
4. 操作按钮通过 `auth` 属性绑定权限码

### VbenForm 动态选项最佳实践

Select / TreeSelect 的选项**不要**在 `componentProps` 中使用响应式 ref，因为不会自动更新。正确做法：

```ts
// 初始设空
{ component: 'Select', fieldName: 'roleIds', componentProps: { options: [] } }

// 弹窗打开时通过 updateSchema 动态设置
async onOpenChange(isOpen) {
  if (isOpen) {
    const roles = await getRoleListAllApi();
    formApi.updateSchema([
      { fieldName: 'roleIds', componentProps: { options: roles.map(r => ({ label: r.name, value: r.id })) } }
    ]);
  }
}
```

### 树形表格数据要求

vxe-table `treeConfig.transform: true` 需要**扁平数据**（每行含 `parentId`），不要传嵌套 `children` 结构。

---

## 常见问题

### Q: 登录后默认跳转到系统管理而非仪表盘？

A: 浏览器 localStorage 中缓存了旧的 `preferences`，按 `Ctrl+Shift+Delete` 清除后重新登录即可。

### Q: 修改代码后页面没有变化？

A: 前端 Vite HMR 通常自动生效。如果修改了 `preferences.ts` 等配置文件，需要清除 localStorage 后刷新。后端修改需要重启 `dotnet run`。

### Q: JWT 过期后出现"内部服务器错误"？

A: 已优化为返回 401 状态码，前端自动跳转登录页。如果仍出现 500，检查后端 `JwtConfiguration.cs` 是否包含 `OnAuthenticationFailed` 和 `OnChallenge` 事件处理。

### Q: Select / TreeSelect 下拉选项为空？

A: 这是 VbenForm 的已知限制，`componentProps` 中的响应式 ref 不会自动更新。解决方案见上方"动态选项最佳实践"。

### Q: 编辑用户后出现 Validation Failed？

A: 确保后端 `UserUpdateDtoValidator` 中各字段的验证规则使用了 `.When()` 条件，因为编辑用户时所有字段均为可选。

### Q: 表格分页不生效？

A: 前端分页参数必须使用 `pageNumber`（而非 `page`）以匹配后端 `PagedRequest` 的字段名。

### Q: 如何启用 Redis 缓存？

A: 修改 `appsettings.json` 中 `Redis.Enabled` 为 `true`，并配置 `ConnectionString`，确保 Redis 服务已启动。

---

## 技术栈详情

| 层级 | 技术 |
|------|------|
| 后端框架 | .NET Core WebAPI |
| ORM | Entity Framework Core |
| 数据库 | SQLite（开发）/ 可切换 PostgreSQL/SQL Server |
| 认证 | JWT Bearer Token |
| 密码加密 | BCrypt |
| 对象映射 | AutoMapper |
| 缓存 | 内存缓存 / Redis（可选） |
| 日志 | Serilog |
| API文档 | Swagger |
| 前端框架 | Vue 3 + TypeScript |
| 构建工具 | Vite |
| UI组件库 | Ant Design Vue |
| 状态管理 | Pinia |
| 表格组件 | vxe-table |
| 管理框架 | Vben Admin v5.7 |
| 图标库 | Lucide (Iconify) |
