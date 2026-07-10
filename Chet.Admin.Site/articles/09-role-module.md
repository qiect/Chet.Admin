# Chet.Admin 模块详解③：角色与权限这样配最清晰 🛡️

> 《Chet.Admin 全栈实战》系列第 9 篇

---

## 前言

上一篇聊完用户管理，**角色和权限** 紧接着就来了。

**痛点** 直击：

- 🤯 一个角色有十几个菜单，配起来点到手酸
- 🤯 菜单有父子层级，子菜单勾了父菜单没勾，权限不完整
- 🤯 数据权限有 5 种范围，前端表单怎么动态显示字段
- 🤯 Tree 组件 `checkStrictly` 模式下 `checkedKeys` 类型变了的坑

**Chet.Admin** 把这些坑都填好了，今天咱们挨个看。

---

## 一、角色管理：CRUD 设计 📋

### 1.1 RolesController：8 个端点

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供角色管理相关的API接口")]
public class RolesController : ControllerBase
```

| 方法 | 路径 | 作用 |
| ---- | ---- | ---- |
| GET | `/roles` | 所有角色（下拉用） |
| GET | `/roles/paged` | 分页 + 关键字搜索 |
| GET | `/roles/{id}` | 角色详情 |
| POST | `/roles` | 创建角色 |
| PUT | `/roles/{id}` | 更新角色 |
| DELETE | `/roles/{id}` | 删除角色 |
| GET | `/roles/{id}/menus` | **查角色已有菜单** |
| POST | `/roles/{id}/menus` | **分配菜单** |
| PUT | `/roles/{id}/data-scope` | **改数据权限** |

> 💡 角色 Code 创建后不可重复，更新时也得校验唯一性。

### 1.2 创建角色：唯一性校验

```csharp
public async Task<RoleDto> CreateRoleAsync(RoleCreateDto dto)
{
    _logger.LogInformation("Creating role: {Code}", dto.Code);

    // 唯一性校验：Code 不能重复
    var existingRole = await _roleRepository.GetByCodeAsync(dto.Code);
    if (existingRole != null)
        throw new BadRequestException($"Role code '{dto.Code}' already exists");

    var role = _mapper.Map<RoleEntity>(dto);
    await _roleRepository.AddAsync(role);
    await _roleRepository.SaveChangesAsync();

    return _mapper.Map<RoleDto>(role);
}
```

> 🎯 **Code 是机器读的，Name 是人读的**。Code 一旦确定不要轻易改，因为 JWT Claim 里存的就是 Code。

### 1.3 分页查询：Code 或 Name 模糊搜

```csharp
public async Task<PagedResult<RoleDto>> GetPagedRolesAsync(PagedRequest request)
{
    request.Normalize();

    if (!string.IsNullOrWhiteSpace(request.Keyword))
    {
        var dbContext = (AppDbContext)_unitOfWork.DbContext;
        var keyword = request.Keyword.Trim();
        var query = dbContext.Roles.AsNoTracking()
            .Where(r => r.Code.Contains(keyword) || r.Name.Contains(keyword));

        var totalCount = await query.CountAsync();
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync();
        var roleDtos = _mapper.Map<List<RoleDto>>(items);

        return new PagedResult<RoleDto>(roleDtos, request.PageNumber, request.PageSize, totalCount);
    }

    var pagedRoles = await _roleRepository.GetPagedAsync(request);
    var roleDtos2 = _mapper.Map<List<RoleDto>>(pagedRoles.Items);
    return new PagedResult<RoleDto>(roleDtos2, request.PageNumber, request.PageSize, pagedRoles.Metadata.TotalCount);
}
```

### 1.4 更新角色：AutoMapper 自动映射

更新时直接用 AutoMapper 把 DTO 字段映射到实体上：

```csharp
public async Task UpdateRoleAsync(int id, RoleUpdateDto dto)
{
    var role = await _roleRepository.GetByIdAsync(id)
        ?? throw new NotFoundException(nameof(RoleEntity), id);

    _mapper.Map(dto, role); // 把 DTO 字段更新到实体
    _roleRepository.Update(role);
    await _roleRepository.SaveChangesAsync();
}
```

> 💡 `_mapper.Map(dto, role)` 第二个参数表示**更新已有对象**，不会创建新实例。

---

## 二、权限分配：菜单树的核心 🌳

### 2.1 角色-菜单关联表

权限分配通过 `RoleMenus` 关联表实现：

```csharp
public class RoleMenuEntity
{
    public int RoleId { get; set; }
    public int MenuId { get; set; }
    public RoleEntity Role { get; set; } = null!;
    public MenuEntity Menu { get; set; } = null!;
}
```

> 📌 **设计取舍**：用关联表而不是在 Role 上存 `MenuIds` JSON。关联表能建索引、能反查、能加外键约束。

### 2.2 AssignMenusAsync：先删后加

分配菜单的核心逻辑是"**清空旧的，加入新的**"：

```csharp
public async Task AssignMenusAsync(int roleId, List<int> menuIds)
{
    _logger.LogInformation("Assigning menus to role: {RoleId}", roleId);
    var role = await _roleRepository.GetByIdAsync(roleId)
        ?? throw new NotFoundException(nameof(RoleEntity), roleId);

    var dbContext = (AppDbContext)_unitOfWork.DbContext;

    // 1. 删除旧的关联
    var existing = await dbContext.RoleMenus
        .Where(rm => rm.RoleId == roleId).ToListAsync();
    dbContext.RoleMenus.RemoveRange(existing);

    // 2. 添加新的关联
    foreach (var menuId in menuIds)
    {
        await dbContext.RoleMenus.AddAsync(new RoleMenuEntity { RoleId = roleId, MenuId = menuId });
    }

    await _unitOfWork.SaveChangesAsync();
}
```

### 2.3 GetRoleMenusAsync：查角色已有菜单

```csharp
public async Task<IEnumerable<MenuDto>> GetRoleMenusAsync(int roleId)
{
    _logger.LogInformation("Getting menus for role: {RoleId}", roleId);
    var menus = await _menuRepository.GetMenusByRoleIdAsync(roleId);
    return _mapper.Map<IEnumerable<MenuDto>>(menus);
}
```

<!-- 角色菜单分配 -->
![角色菜单分配](/screenshots/role-assign-menu.svg)

---

## 三、数据权限：5 种范围配置 🎚️

### 3.1 5 种 DataScope

| DataScope | 含义 | 适用场景 |
| ---- | ---- | ---- |
| `All` | 全部数据 | 超管、CEO |
| `Dept` | 本部门 | 部门经理 |
| `DeptAndChild` | 本部门及下级 | 大区经理 |
| `Self` | 仅本人 | 普通员工 |
| `Custom` | 自定义部门 | 跨部门负责人 |

> 💡 Custom 范围配合 `RoleDataScopeDeptEntity` 关联表，可以**精确指定**这个角色能看哪几个部门的数据。

### 3.2 UpdateDataScopeAsync：更新数据权限

```csharp
public async Task UpdateDataScopeAsync(int roleId, string dataScope, List<int>? customDeptIds)
{
    _logger.LogInformation("Updating data scope for role: {RoleId}, Scope: {DataScope}", roleId, dataScope);
    var role = await _roleRepository.GetByIdAsync(roleId)
        ?? throw new NotFoundException(nameof(RoleEntity), roleId);

    // 1. 更新角色的 DataScope 字段
    role.DataScope = dataScope;
    _roleRepository.Update(role);

    var dbContext = (AppDbContext)_unitOfWork.DbContext;

    // 2. 删除旧的自定义部门关联
    var existing = await dbContext.RoleDataScopeDepts
        .Where(rd => rd.RoleId == roleId).ToListAsync();
    dbContext.RoleDataScopeDepts.RemoveRange(existing);

    // 3. 仅当 Custom 范围时，添加新的部门关联
    if (dataScope == "Custom" && customDeptIds is { Count: > 0 })
    {
        foreach (var deptId in customDeptIds)
        {
            await dbContext.RoleDataScopeDepts.AddAsync(
                new RoleDataScopeDeptEntity { RoleId = roleId, DepartmentId = deptId });
        }
    }

    await _unitOfWork.SaveChangesAsync();
}
```

> 🎯 **关键设计**：Custom 之外的范围**不存部门关联**，因为不需要。这样表里不会有"陈旧数据"。

### 3.3 RoleDataScopeDeptEntity：自定义关联表

```csharp
public class RoleDataScopeDeptEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int DepartmentId { get; set; }
    public RoleEntity Role { get; set; } = null!;
    public DepartmentEntity Department { get; set; } = null!;
}
```

> 📌 这张表只在 `DataScope == "Custom"` 时有数据，其他范围下是空的。

---

## 四、前端：角色编辑表单 🎨

### 4.1 表单 schema：动态显示自定义部门

前端用 `dependencies` 控制"自定义部门"字段的显隐：

```typescript
const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'code', label: '角色编码', rules: 'required' },
  { component: 'Input', fieldName: 'name', label: '角色名称', rules: 'required' },
  { component: 'Textarea', fieldName: 'description', label: '描述' },
  { component: 'InputNumber', fieldName: 'sort', label: '排序', defaultValue: 0 },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
  {
    component: 'Select',
    fieldName: 'dataScope',
    label: '数据权限',
    defaultValue: 'Self',
    componentProps: {
      options: [
        { label: '全部数据', value: 'All' },
        { label: '本部门数据', value: 'Dept' },
        { label: '本部门及下级部门', value: 'DeptAndChild' },
        { label: '仅本人数据', value: 'Self' },
        { label: '自定义部门', value: 'Custom' },
      ],
    },
  },
  {
    component: 'TreeSelect',
    fieldName: 'customDeptIds',
    label: '自定义部门',
    dependencies: {
      triggerFields: ['dataScope'],
      if(values) { return values.dataScope === 'Custom'; },
    },
    componentProps: {
      treeData: [],
      treeCheckable: true,
      showCheckedStrategy: 'SHOW_ALL',
      placeholder: '选择部门',
    },
  },
];
```

> 🎯 **dependencies 是关键**：监听 `dataScope` 字段变化，只有选了 `Custom` 才显示部门选择树。

### 4.2 提交：分开调两个接口

表单提交时，**先更新角色，再更新数据权限**：

```typescript
const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    const id = values.id;
    if (id) {
      // 1. 更新角色基本信息
      await updateRoleApi(id, values);

      // 2. 单独提交数据权限
      if (values.dataScope) {
        const dataScopeData: { dataScope: string; customDeptIds?: number[] } = {
          dataScope: values.dataScope,
        };
        if (values.dataScope === 'Custom' && values.customDeptIds) {
          dataScopeData.customDeptIds = values.customDeptIds;
        }
        await updateDataScopeApi(id, dataScopeData);
      }
      message.success('更新成功');
    } else {
      await createRoleApi(values);
      message.success('创建成功');
    }
    modalApi.close();
    gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
      if (data) formApi.setValues(data);

      // 加载部门树（用于自定义部门选择）
      try {
        const deptTree = await getDeptTreeApi();
        deptTreeData.value = buildDeptTreeSelectData(deptTree || []);
        formApi.updateSchema([{
          fieldName: 'customDeptIds',
          componentProps: { treeData: deptTreeData.value },
        }]);
      } catch {
        // 部门树加载失败不阻塞表单
      }
    }
  },
});
```

### 4.3 数据权限展示：彩色标签

列表里用 `CellTag` 渲染器把 DataScope 显示成彩色标签：

```typescript
const dataScopeOptions = [
  { color: 'success', label: '全部数据', value: 'All' },
  { color: 'processing', label: '本部门', value: 'Dept' },
  { color: 'cyan', label: '本部门及下级', value: 'DeptAndChild' },
  { color: 'warning', label: '仅本人', value: 'Self' },
  { color: 'purple', label: '自定义', value: 'Custom' },
];

const columns: VxeTableGridColumns = [
  // ... 其他列
  {
    field: 'dataScope', title: '数据权限', width: 130,
    cellRender: { name: 'CellTag', options: dataScopeOptions },
  },
];
```

---

## 五、菜单分配：Tree 的 checkStrictly 坑 🕳️

### 5.1 为什么用 checkStrictly

Ant Design Vue 的 Tree 默认是**父子联动**——勾父节点会自动勾所有子节点。

但 Chet.Admin 的菜单有 **4 种类型**（目录/菜单/按钮/接口），**只想精确控制每一项**：

- 父子节点独立勾选
- 可单独勾按钮权限
- 不会因为勾父菜单"误带"全部子按钮

所以必须 `checkStrictly: true`。

### 5.2 隐藏的坑：checkedKeys 类型变了

开启 `checkStrictly` 后，`v-model:checkedKeys` 的值会变成对象：

```typescript
// 期望的是 number[]
const checkedKeys = ref<number[]>([]);

// 实际拿到的是：
{
  checked: number[],     // 已勾选的节点
  halfChecked: number[], // 半选的父节点
}
```

### 5.3 兼容处理：类型联合 + 解构

Chet.Admin 用**联合类型 + 运行时判断**兼容两种情况：

```typescript
const [AssignModal, assignModalApi] = useVbenModal({
  onConfirm: async () => {
    // checkStrictly 模式下，checkedKeys 可能是 number[] 或对象
    const val = checkedKeys.value as
      | number[]
      | { checked: number[]; halfChecked: number[] };

    // 关键：统一解构成 number[]
    const menuIds = Array.isArray(val) ? val : (val?.checked ?? []);

    await assignRoleMenusApi(assignRoleId.value, menuIds);
    message.success('菜单分配成功');
    assignModalApi.close();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      const data = assignModalApi.getData<any>();
      assignRoleId.value = data.roleId;
      assignLoading.value = true;
      try {
        // 并行拉菜单树 + 角色已有菜单
        const [menus, roleMenuList] = await Promise.all([
          getMenuTreeApi(),
          getRoleMenusApi(data.roleId),
        ]);

        treeData.value = buildMenuTree(menus || []);

        // 回显：把已有菜单 ID 塞进 checkedKeys
        checkedKeys.value = (roleMenuList || []).map((m: any) => m.id);
      } finally {
        assignLoading.value = false;
      }
    }
  },
});
```

> 🎯 **关键代码**就这一句：
> ```typescript
> const menuIds = Array.isArray(val) ? val : (val?.checked ?? []);
> ```

### 5.4 菜单树构建：按钮特殊标记

构建菜单树时，按钮节点加 `[按钮]` 后缀，方便识别：

```typescript
function buildMenuTree(menus: any[]): TreeNode[] {
  return (menus || []).map((m: any) => ({
    key: m.id,
    title: m.type === 'Button' ? `${m.name} [按钮]` : m.name,
    value: m.id,
    children: m.children && m.children.length > 0 ? buildMenuTree(m.children) : undefined,
    selectable: false,
  }));
}
```

### 5.5 模板：Tree 组件

```vue
<AssignModal title="分配菜单" class="w-[600px]">
  <Spin :spinning="assignLoading">
    <Alert type="info" show-icon class="mb-3">
      <template #message>
        <span class="text-xs">
          勾选菜单分配访问权限。父子节点独立勾选，可单独分配按钮权限。
        </span>
      </template>
    </Alert>
    <Tree
      v-model:checkedKeys="checkedKeys"
      :tree-data="treeData"
      checkable
      :check-strictly="true"
      default-expand-all
      :selectable="false"
      :field-names="{ key: 'key', title: 'title', children: 'children' }"
    />
  </Spin>
</AssignModal>
```

> 💡 **Alert 提示**很贴心：告诉用户"父子独立勾选"，避免误以为勾父节点会带子节点。

<!-- 菜单分配弹窗 -->
![菜单分配弹窗](/screenshots/assign-menu-modal.svg)

---

## 六、API 封装一览 🔌

`api/system/role.ts` 一行一个方法：

```typescript
// 分页查询
export async function getRoleListApi(params: any) {
  const result = await requestClient.get('/roles/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

// 所有角色（下拉用）
export async function getRoleAllApi() {
  return requestClient.get('/roles');
}

// CRUD
export async function createRoleApi(data: any) {
  return requestClient.post('/roles', data);
}
export async function updateRoleApi(id: number, data: any) {
  return requestClient.put(`/roles/${id}`, data);
}
export async function deleteRoleApi(id: number) {
  return requestClient.delete(`/roles/${id}`);
}

// 角色已有菜单
export async function getRoleMenusApi(id: number) {
  return requestClient.get(`/roles/${id}/menus`);
}

// 分配菜单
export async function assignRoleMenusApi(id: number, menuIds: number[]) {
  return requestClient.post(`/roles/${id}/menus`, menuIds);
}

// 菜单树（角色模块用）
export async function getMenuTreeApi() {
  return requestClient.get('/menus/tree');
}

// 更新数据权限
export async function updateDataScopeApi(id: number, data: { dataScope: string; customDeptIds?: number[] }) {
  return requestClient.put(`/api/v1/roles/${id}/data-scope`, data);
}
```

---

## 七、操作列：权限码驱动 🎯

和用户管理一样，每个按钮挂权限码：

```vue
<template #toolbar-tools>
  <Button v-if="hasAccessByCodes(['system:role:create'])" type="primary" @click="onCreate">
    <Plus class="mr-2 size-4" />新增
  </Button>
</template>

<template #action="{ row }">
  <VbenTableAction
    :actions="[
      { text: '编辑', auth: 'system:role:update', onClick: () => onEdit(row) },
      { text: '分配菜单', auth: 'system:role:update', onClick: () => onAssign(row) },
    ]"
    :dropdown-actions="[
      { text: '删除', auth: 'system:role:delete', danger: true,
        popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }
    ]"
  />
</template>
```

> 📌 "分配菜单"和"编辑"共用 `system:role:update` 权限码，简化配置。

---

## 八、设计要点总结 ✨

### 8.1 后端设计

| 设计 | 价值 |
| ---- | ---- |
| 角色 Code 唯一性校验 | 防止 JWT Claim 冲突 |
| 菜单分配先删后加 | 简化增量更新逻辑 |
| Custom 范围才存部门关联 | 避免表里有陈旧数据 |
| DataScope 字段挂在角色上 | 解耦菜单权限和数据权限 |
| DTO ↔ Entity 用 AutoMapper | 减少手动赋值 |

### 8.2 前端设计

| 设计 | 价值 |
| ---- | ---- |
| `dependencies` 联动显隐 | Custom 范围才显示部门树 |
| `checkStrictly: true` | 父子节点独立勾选 |
| 联合类型兼容 checkedKeys | 防止 TS 类型坑 |
| 按钮节点加 `[按钮]` 标记 | 区分菜单和按钮 |
| Alert 提示交互规则 | 降低用户认知负担 |

### 8.3 易踩坑 Top 3

| 坑 | 现象 | 解决 |
| ---- | ---- | ---- |
| `checkedKeys` 类型变了 | 提交时 menuIds 是对象不是数组 | `Array.isArray(val) ? val : val?.checked` |
| DataScope 选 Custom 不显示部门树 | 字段依赖没配对 | `dependencies.triggerFields: ['dataScope']` |
| 改了角色权限用户没生效 | JWT 里还是旧权限 | 让用户重新登录，或用强制下线 |

---

## 九、完整流程：从配角色到用户生效 🔄

```
管理员创建角色
  ↓
分配菜单（checkStrictly 模式精确勾选）
  ↓
配置数据权限（All/Dept/DeptAndChild/Self/Custom）
  ↓
把角色分配给用户（POST /users/{id}/roles）
  ↓
用户登录，JWT 里塞入 roles + permissions
  ↓
前端拿 Token 调 /auth/user-info，拿到 roles 和 permissions
  ↓
accessStore.setAccessCodes(permissions)
  ↓
v-access 指令按权限码显示/隐藏按钮
  ↓
DataScopeService 自动过滤列表数据
  ↓
✅ 完整的权限闭环
```

---

## 下篇预告

到这里，**RBAC + 认证 + 用户 + 角色** 四篇连成完整的权限闭环。接下来我们继续拆其他模块：菜单管理、部门管理、字典管理、审计日志等 🚀

---

## 开源地址

- **GitHub**：https://github.com/qiect/Chet.Admin
- **Gitee**：https://gitee.com/qiect/Chet.Admin

觉得有帮助的话，**点个 Star ⭐** 支持一下吧！你的 Star 是我持续更新的动力～

---

## 互动

你项目里菜单分配用的是父子联动还是独立勾选？checkStrictly 这个坑踩过吗？评论区聊聊～👇

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#角色管理` `#权限分配` `#数据权限` `#Tree` `#checkStrictly` `#.NET10`
