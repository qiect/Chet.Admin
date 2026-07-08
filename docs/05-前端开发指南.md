# 前端开发指南

## 1. 概述

前端位于 `Chet.Admin.Web/`，是基于 [Vben Admin v5.7](https://vben.pro) 的 pnpm Monorepo。**业务开发集中在 `apps/web-antd` 应用**（Ant Design Vue 技术栈）。

本指南介绍前端架构、目录规范、新增页面 / 接口对接的标准流程，以及权限控制机制。

## 2. 技术栈

| 技术 | 用途 |
| ---- | ---- |
| Vue 3（Composition API） | UI 框架 |
| TypeScript | 类型安全 |
| Vite | 构建工具 |
| Ant Design Vue | UI 组件库 |
| Pinia | 状态管理 |
| Vue Router | 动态路由 + 权限守卫 |
| VxeTable | 高性能表格 |
| Tailwind CSS v4 | 原子化样式 |
| pnpm + Turbo | Monorepo 管理 |

## 3. 主应用目录结构

```
apps/web-antd/src/
├── api/                     # API 请求层
│   ├── core/                # 核心接口（auth/menu/user）
│   ├── system/              # 业务模块接口（10 个）
│   ├── index.ts             # 统一导出
│   └── request.ts           # Axios 实例 + 拦截器
├── adapter/                 # 组件适配器
│   ├── form.ts              # 表单适配
│   ├── vxe-table.ts         # 表格适配
│   └── component/           # 通用组件适配
├── composables/             # 组合式函数
│   └── useDict.ts           # 字典数据联动
├── layouts/                 # 布局
│   ├── basic.vue            # 主布局
│   ├── auth.vue             # 登录布局
│   └── components/
│       └── notification-bell.vue  # 通知铃铛
├── locales/                 # 国际化
│   └── langs/zh-CN|en-US/
├── router/                  # 路由
│   ├── routes/modules/      # 路由模块
│   ├── access.ts            # 访问权限
│   ├── guard.ts             # 路由守卫
│   └── index.ts
├── store/                   # Pinia 状态
│   └── auth.ts              # 认证状态
├── views/                   # 页面
│   ├── _core/               # 内置页面（登录/注册/个人中心/异常页）
│   ├── dashboard/           # 仪表盘
│   └── system/             # 系统管理（10 个模块）
├── app.vue                  # 根组件
├── bootstrap.ts             # 应用引导
└── main.ts                  # 入口
```

## 4. API 请求层

### 4.1 请求实例

`api/request.ts` 封装了 Axios 实例 `requestClient`，统一处理：

- **请求拦截**：注入 `Authorization: Bearer {token}` 与 `Accept-Language`
- **响应拦截**：解析后端统一格式 `{ success, data, message, statusCode }`
- **Token 续期**：Access Token 过期自动调用 `/auth/refresh-token` 刷新
- **错误处理**：401/403/404/500 统一提示

```ts
export const requestClient = createRequestClient(apiURL, {
  responseReturn: 'data',  // 直接返回 data 字段
});
```

### 4.2 接口定义规范

接口文件按模块组织在 `api/system/` 下，命名 `{module}.ts`：

```ts
// api/system/user.ts
import { requestClient } from '#/api/request';

export async function getUserListApi(params: any) {
  const result = await requestClient.get('/users/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function createUserApi(data: any) {
  return requestClient.post('/users', data);
}

export async function updateUserApi(id: number, data: any) {
  return requestClient.put(`/users/${id}`, data);
}

export async function deleteUserApi(id: number) {
  return requestClient.delete(`/users/${id}`);
}
```

> 所有接口路径以 `/api/v1` 为基础前缀（由 `VITE_GLOB_API_URL` 配置），业务接口无需重复写 `/api/v1`。

## 5. 页面开发规范

### 5.1 页面文件位置

```
src/views/system/{module}/index.vue
```

### 5.2 标准页面结构

业务页面采用「表格 + 表单 + 弹窗」组合，使用 Vben 封装的三个核心 Hook：

- `useVbenVxeGrid`：表格（含搜索栏、分页、工具栏）
- `useVbenForm`：表单（新增 / 编辑）
- `useVbenModal`：弹窗

```vue
<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { useAccess } from '@vben/access';
import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';

// 1. 按钮级权限
const { hasAccessByCodes } = useAccess();

// 2. 搜索栏 schema
const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
];

// 3. 表格列定义
const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 80 },
  { field: 'name', title: '名称', minWidth: 150 },
  { field: 'isEnabled', title: '状态', width: 80, cellRender: { name: 'CellTag' } },
  { align: 'center', field: 'operation', fixed: 'right',
    slots: { default: 'action' }, title: '操作', width: 200 },
];

// 4. 表格（含分页查询）
const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) =>
          await getRoleListApi({
            pageNumber: page.currentPage,  // 注意用 pageNumber
            pageSize: page.pageSize,
            ...formValues,
          }),
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

// 5. 表单 schema
const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'name', label: '名称', rules: 'required' },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
];

// 6. 弹窗 + 表单
const [Modal, modalApi] = useVbenModal({ connectedComponent: EditModal });
</script>

<template>
  <Page>
    <Grid>
      <template #toolbar-tools>
        <Button v-access:code="'system:role:create'" @click="handleAdd">新增</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction :actions="[
          { label: '编辑', code: 'system:role:update', onClick: () => handleEdit(row) },
          { label: '删除', code: 'system:role:delete', onClick: () => handleDelete(row) },
        ]" />
      </template>
    </Grid>
    <Modal />
  </Page>
</template>
```

### 5.3 关键约定

| 约定 | 说明 |
| ---- | ---- |
| 分页参数 | 使用 `pageNumber`（非 `page`） |
| 权限按钮 | 使用 `v-access:code="'模块:资源:操作'"` 或 `hasAccessByCodes` |
| Select 选项 | 通过 `updateSchema` 动态设置，配合 `useDict` 加载字典 |
| 树形表格 | 使用扁平数据 + `treeConfig.transform` |
| 状态列 | 使用 `cellRender: { name: 'CellTag' }` 渲染彩色标签 |
| 时间格式化 | 在列 `slots.default` 中用 `toLocaleString('zh-CN')` |

## 6. 字典数据联动

使用 `useDict` 组合式函数从后端字典接口加载选项：

```ts
// composable: useDict(code)
import { useDict } from '#/composables/useDict';

const { options, loading } = useDict('user_status');
// options → [{ label: '启用', value: '1' }, { label: '禁用', value: '0' }]
```

在表单 schema 中动态注入：

```ts
const formSchema: VbenFormSchema[] = [
  { component: 'Select', fieldName: 'status', label: '状态',
    componentProps: { options: [] } },  // 初始为空
];

// 弹窗打开时动态更新
formApi.updateSchema([
  { fieldName: 'status', componentProps: { options: options.value } },
]);
```

`useDict` 内置缓存，重复调用同字典不会重复请求。

## 7. 权限控制

### 7.1 路由权限

菜单由后端动态返回，前端根据用户角色生成可访问路由。登录后调用 `/auth/user-info` 获取 `permissions` 数组并存入 `accessStore`。

### 7.2 按钮级权限

通过权限码控制按钮显示，权限码格式 `模块:资源:操作`（如 `system:role:create`）：

```vue
<!-- 指令方式 -->
<a-button v-access:code="'system:role:create'">新增</a-button>

<!-- 函数方式 -->
<Button v-if="hasAccessByCodes(['system:role:update'])">编辑</Button>
```

### 7.3 数据权限

后端根据角色 `DataScope`（All/Dept/DeptAndChild/Self/Custom）在 Service 层自动过滤用户列表数据，前端无需处理。

## 8. 认证流程

`store/auth.ts` 管理登录状态：

```
1. 用户输入邮箱密码 → loginApi() → 获取 accessToken + refreshToken
2. 存入 accessStore
3. fetchUserInfo() → 调用 /auth/user-info 获取用户信息 + permissions
4. 跳转至首页
```

Token 过期时由响应拦截器自动调用 `refreshTokenApi` 续期，续期失败则跳转登录页。

## 9. 路由配置

路由模块位于 `router/routes/modules/`，每个文件导出一个路由数组：

```ts
const routes: RouteRecordRaw[] = [
  {
    path: '/dashboard',
    name: 'Dashboard',
    meta: { icon: 'lucide:layout-dashboard', title: $t('page.dashboard.title'), order: -1 },
    children: [
      {
        name: 'Analytics',
        path: 'analytics',
        component: () => import('#/views/dashboard/analytics/index.vue'),
        meta: { icon: 'lucide:area-chart', title: $t('page.dashboard.analytics') },
      },
    ],
  },
];
export default routes;
```

## 10. 环境配置

环境变量文件位于应用根目录：

| 文件 | 用途 |
| ---- | ---- |
| `.env` | 公共配置 |
| `.env.development` | 开发环境 |
| `.env.production` | 生产环境 |
| `.env.analyze` | 构建分析 |

关键变量：

```bash
VITE_PORT=5666                       # 开发端口
VITE_BASE=/
VITE_GLOB_API_URL=/api/v1            # API 基础路径
VITE_NITRO_MOCK=false               # 是否启用 Mock
VITE_DEVTOOLS=false                 # 是否开启 devtools
VITE_INJECT_APP_LOADING=true        # 全局 loading
```

## 11. 国际化

语言文件位于 `locales/langs/`，按 `zh-CN` / `en-US` 组织，页面文案统一通过 `$t('key')` 调用。

## 12. 新增业务模块完整流程

以「文章管理」为例：

```
1. 新增 API 文件：src/api/system/article.ts
2. 在 src/api/index.ts 导出
3. 新增页面：src/views/system/article/index.vue
4. 新增路由模块：src/router/routes/modules/article.ts（或后端菜单动态返回）
5. 后端配置权限码 system:article:create/update/delete
6. 在页面按钮上绑定权限码
```

## 13. 常用命令

```bash
# 开发
pnpm dev:antd                 # 启动主应用
pnpm dev                       # 选择启动应用

# 构建
pnpm build:antd                # 构建主应用
pnpm build                     # 构建所有

# 代码检查
pnpm lint                      # ESLint + oxlint
pnpm check:type                # 类型检查
pnpm check                     # 全量检查（循环依赖 + 类型 + 拼写）

# 测试
pnpm test:unit                 # 单元测试（vitest）
pnpm test:e2e                  # E2E 测试（playwright）
```
