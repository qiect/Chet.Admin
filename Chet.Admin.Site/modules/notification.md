# 通知公告

## 功能特性

- 管理员发布全局公告 / 个人通知
- 通知类型：公告（Announcement）、通知（Notification）、待办（Todo）
- 优先级：低 / 普通 / 高 / 紧急
- 未读计数
- 标记已读（单条 / 全部）
- 顶部导航栏铃铛图标 + 通知面板

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| POST | `/notifications` | 发送通知 |
| GET | `/notifications/paged` | 通知列表 |
| GET | `/notifications/my` | 我的通知 |
| GET | `/notifications/unread-count` | 未读数量 |
| PUT | `/notifications/{id}/read` | 标记已读 |
| PUT | `/notifications/read-all` | 全部已读 |
| DELETE | `/notifications/{id}` | 删除通知 |

## 前端组件

- `layouts/components/notification-bell.vue`：铃铛 + 角标 + 下拉面板

## 通知类型与优先级

### 通知类型

| 类型 | 说明 |
| ---- | ---- |
| `Announcement` | 公告（全局） |
| `Notification` | 通知（个人） |
| `Todo` | 待办 |

### 优先级

| 优先级 | 说明 |
| ---- | ---- |
| `Low` | 低 |
| `Normal` | 普通 |
| `High` | 高 |
| `Urgent` | 紧急 |
