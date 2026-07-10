# 权限管理

本文档介绍 Chet.Admin 的按钮级权限实现机制。系统的权限体系包含三个层级：

- **菜单级路由权限**：通过菜单表动态生成前端路由
- **按钮级操作权限**：通过权限码控制页面按钮显隐
- **行级数据权限**：通过角色 `DataScope` 过滤数据

## 按钮级权限实现机制

1. 后端 `Menus` 表中 Type=Button/Api 的节点定义权限码（如 `system:user:create`）
2. 角色分配菜单（`RoleMenu` 关联表，按钮节点随父菜单分配）
3. 用户登录后 `/auth/user-info` 返回 `permissions` 数组（从菜单表派生）
4. 前端存入 `accessStore`
5. 页面按钮通过 `v-access:code` 或 `hasAccessByCodes` 控制显示

## 权限码命名规范

```
模块:资源:操作
```

示例：

| 权限码 | 说明 |
| ---- | ---- |
| `system:user:create` | 创建用户 |
| `system:user:update` | 更新用户 |
| `system:user:delete` | 删除用户 |
| `system:role:assign` | 分配角色菜单 |

## 前端使用

### 指令方式

```vue
<a-button v-access:code="'system:role:create'">新增</a-button>
```

### 函数方式

```vue
<Button v-if="hasAccessByCodes(['system:role:update'])">编辑</Button>
```

## 数据权限

数据权限通过角色 `DataScope` 字段控制，在 Service 层自动过滤用户列表数据，前端无需处理。

| 值 | 说明 |
| ---- | ---- |
| `All` | 全部数据 |
| `Dept` | 本部门数据 |
| `DeptAndChild` | 本部门及下级部门 |
| `Self` | 仅本人数据 |
| `Custom` | 自定义部门 |

更多数据权限相关说明详见 [角色管理](/modules/role)。
