# 字典管理

## 功能特性

- 字典 CRUD（类型 + 字典项，树形结构）
- 前端 `useDict` 组合式函数联动业务表单
- 内置缓存（重复请求不重复调用）
- 预置字典：`user_status`、`menu_type`、`gender`、`yes_no`

## 后端接口

| 方法 | 路径 | 说明 |
| ---- | ---- | ---- |
| GET | `/dictionaries` | 字典列表 |
| GET | `/dictionaries/code/{code}` | 按编码获取字典项 |
| POST | `/dictionaries` | 创建 |
| PUT | `/dictionaries/{id}` | 更新 |
| DELETE | `/dictionaries/{id}` | 删除 |

## 前端使用

使用 `useDict` 组合式函数从后端字典接口加载选项：

```ts
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

## 预置字典

| 字典编码 | 说明 |
| ---- | ---- |
| `user_status` | 用户状态 |
| `menu_type` | 菜单类型 |
| `gender` | 性别 |
| `yes_no` | 是否 |
