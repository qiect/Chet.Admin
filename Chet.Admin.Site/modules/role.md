# 角色管理

## 功能特性

- 角色 CRUD
- 菜单分配（树形勾选菜单，含按钮权限）
- 数据权限范围设置（5 种：All/Dept/DeptAndChild/Self/Custom）
- 自定义数据权限（选择部门树）

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/roles` | 所有角色 |
| GET | `/roles/paged` | 分页查询 |
| POST | `/roles` | 创建 |
| PUT | `/roles/{id}` | 更新 |
| DELETE | `/roles/{id}` | 删除 |
| GET/PUT | `/roles/{id}/menus` | 角色-菜单 |
| PUT | `/roles/{id}/data-scope` | 数据权限范围 |

## 实现细节

- `DataScopeService` 负责根据用户角色计算数据可见范围
- 多角色时使用 `DataScope` 取最宽松的范围

### 数据权限范围（DataScope）

| 值 | 说明 |
| ---- | ---- |
| `All` | 全部数据 |
| `Dept` | 本部门数据 |
| `DeptAndChild` | 本部门及下级部门 |
| `Self` | 仅本人数据 |
| `Custom` | 自定义部门 |
