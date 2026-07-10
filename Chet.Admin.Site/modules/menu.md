# 菜单管理

## 功能特性

- 菜单 CRUD（树形结构）
- 四种菜单类型：目录（Directory）、菜单（Menu）、按钮（Button）、接口（Api）
- 按钮权限码管理（Type=Button/Api 节点定义权限码，如 `system:user:create`）
- 动态路由生成（前端根据后端返回菜单构建路由）
- 图标配置（Lucide 图标库）
- 排序、启用/禁用
- 外链支持

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/menus` | 菜单树 |
| POST | `/menus` | 创建 |
| PUT | `/menus/{id}` | 更新 |
| DELETE | `/menus/{id}` | 删除 |

## 前端页面

`views/system/menu/index.vue`：树形表格展示，使用 `treeConfig.transform` 转换扁平数据。

## 菜单类型

| 类型 | 说明 |
| ---- | ---- |
| Directory | 目录（容器节点） |
| Menu | 菜单（可访问页面） |
| Button | 按钮（权限码节点） |
| Api | 接口（权限码节点） |
