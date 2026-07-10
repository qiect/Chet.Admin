# 用户管理

## 功能特性

- 用户 CRUD（创建、查询、更新、删除）
- 分页查询（支持关键字搜索）
- 密码强度校验
- 分配角色（多角色）
- 隶属部门
- 数据权限过滤（根据角色 DataScope 自动过滤可见数据）
- 头像上传
- 修改密码（验证旧密码）

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/users` | 所有用户 |
| GET | `/users/paged` | 分页查询 |
| GET | `/users/{id}` | 用户详情 |
| POST | `/users` | 创建 |
| PUT | `/users/{id}` | 更新 |
| DELETE | `/users/{id}` | 删除 |

## 前端页面

`views/system/user/index.vue`：使用 `useVbenVxeGrid` 表格 + `useVbenModal` 弹窗表单，支持角色分配、部门选择。
