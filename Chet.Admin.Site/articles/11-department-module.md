# Chet.Admin 模块详解⑤：部门树形管理与组织架构 🏢

> 《Chet.Admin 全栈实战》系列第 11 篇

---

## 前言

上一篇讲了菜单树，这一篇继续讲另一个树形结构：**部门**。

很多人会把「部门」和「菜单」用同一套方案糊过去，但两者的语义其实差别很大：

| 维度 | 菜单 | 部门 |
| ---- | ---- | ---- |
| 用途 | 决定能访问哪些页面 | 决定能看哪些数据 |
| 类型 | 目录/菜单/按钮三态 | 单一实体 |
| 编码 | 无强制编码 | 业务编码（HR、TECH） |
| 路由 | 直接驱动路由生成 | 不进路由表 |
| 权限 | 控制按钮显隐 | 控制数据范围 |

**Chet.Admin** 把部门作为**数据权限**的载体：用户的部门归属 + 数据权限策略 → 决定能查到哪些数据。

---

## 一、部门实体设计

部门实体位于 `Chet.Admin.Domain/Department/DepartmentEntity.cs`：

```csharp
public class DepartmentEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;       // 部门名称
    public string Code { get; set; } = string.Empty;       // 部门编码
    public string? Leader { get; set; }                    // 负责人
    public string? Phone { get; set; }                     // 联系电话
    public string? Email { get; set; }                    // 邮箱
    public int ParentId { get; set; }                     // 父部门ID（0=顶级）
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<DepartmentEntity> Children { get; set; } = [];
}
```

**字段设计思路**：

- **`Code`** 业务编码（如 `TECH`、`HR`、`FIN`），用于跨系统对接
- **`Leader` / `Phone` / `Email`** 部门联系信息，方便业务流程审批
- **`ParentId`** 父子关系，0 表示顶级部门
- **`Children`** 导航属性，EF Core 自动装配

**与菜单的区别**：

- 没有类型字段（部门不需要分类）
- 没有 `Path` / `Component` / `Icon`（部门不进路由）
- 多了 `Code` / `Leader` / `Phone` / `Email`（业务属性）

---

## 二、后端：树形 CRUD + 编码唯一校验

### 2.1 控制器的 7 个接口

`DepartmentsController.cs` 的接口列表：

```csharp
[HttpGet]            // 所有部门（扁平）
[HttpGet("tree")]    // 部门树（全量）
[HttpGet("paged")]   // 分页查询
[HttpGet("{id}")]    // 详情
[HttpPost]           // 创建
[HttpPut("{id}")]    // 更新
[HttpDelete("{id}")] // 删除
```

注意这里**没有**菜单那种 `my-tree` 接口。部门权限的过滤是在**业务查询层**做的，比如查用户列表时根据数据权限策略自动 join 部门表。

### 2.2 编码唯一校验

`CreateDepartmentAsync` 里有重要的前置校验：

```csharp
public async Task<DepartmentDto> CreateDepartmentAsync(DepartmentCreateDto dto)
{
    _logger.LogInformation("Creating department: {Code}", dto.Code);
    var existing = await _departmentRepository.GetByCodeAsync(dto.Code);
    if (existing != null)
        throw new BadRequestException($"Department code '{dto.Code}' already exists");

    var dept = _mapper.Map<DepartmentEntity>(dto);
    await _departmentRepository.AddAsync(dept);
    await _departmentRepository.SaveChangesAsync();
    return _mapper.Map<DepartmentDto>(dept);
}
```

**为什么不直接靠数据库唯一索引**？

数据库唯一索引报错时是个 `DbUpdateException`，**信息不友好**，前端拿到的是"违反 UNIQUE 约束"这种数据库方言。

Chet.Admin 选择在业务层**主动校验**，抛 `BadRequestException`，统一返回给前端：

```json
{
  "code": 400,
  "message": "Department code 'TECH' already exists"
}
```

数据库的唯一索引作为**兜底**，防止并发请求绕过业务校验。

### 2.3 关键字搜索：多字段 OR 匹配

`GetPagedDepartmentsAsync` 支持按编码/名称/负责人模糊搜索：

```csharp
if (!string.IsNullOrWhiteSpace(request.Keyword))
{
    var dbContext = (AppDbContext)_unitOfWork.DbContext;
    var keyword = request.Keyword.Trim();
    var query = dbContext.Departments.AsNoTracking()
        .Where(d => d.Code.Contains(keyword)
                 || d.Name.Contains(keyword)
                 || (d.Leader != null && d.Leader.Contains(keyword)));

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip(request.Skip)
        .Take(request.PageSize)
        .ToListAsync();
    // ...
}
```

**注意 `Leader` 字段的判空**：

```csharp
d.Leader != null && d.Leader.Contains(keyword)
```

因为 `Leader` 可空，直接 `Contains` 会 NRE。EF Core 翻译成 SQL 时其实是安全的，但**显式判空**让代码意图更清晰，也能在 LINQ to Objects 调试时不翻车。

### 2.4 递归建树

跟菜单一模一样的模式：

```csharp
private static IEnumerable<DepartmentTreeDto> BuildDepartmentTree(
    List<DepartmentTreeDto> allDepts, int parentId)
{
    return allDepts
        .Where(d => d.ParentId == parentId)
        .OrderBy(d => d.Sort)
        .Select(d =>
        {
            d.Children = BuildDepartmentTree(allDepts, d.Id).ToList();
            return d;
        })
        .ToList();
}
```

**为什么不抽公共方法**？

- 树节点类型不同（`MenuTreeDto` vs `DepartmentTreeDto`）
- 抽象成本 > 重复成本
- 这两个方法都很短，独立维护反而更清晰

**不过度抽象** = 工程上的克制。

---

## 三、前端：部门管理页面

### 3.1 树形表格 + 自动建树

`views/system/department/index.vue` 跟菜单页结构类似：

```typescript
const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns,
    proxyConfig: {
      ajax: {
        query: async () => {
          const list = await getAllDeptsApi();
          return { items: list || [], total: list?.length || 0 };
        },
      },
    },
    rowConfig: { keyField: 'id' },
    treeConfig: {
      parentField: 'parentId',
      rowField: 'id',
      transform: true,        // 扁平 → 树，自动处理
      expandAll: true,
      indent: 20,
    },
    pagerConfig: { enabled: false }, // 部门数据量小，不分页
  },
});
```

**亮点**：

- 后端返回**扁平数组**，前端靠 `transform: true` 自动建树
- `expandAll: true` 默认全部展开，方便查看整体组织架构
- 关闭分页器，避免父子节点被分页拆散

### 3.2 父级选择器：基于树形数据递归建树

跟菜单不一样的地方：部门父级选择器**基于已经建好的树**再递归，而不是基于扁平数据。

```typescript
function buildTreeSelect(items: any[], excludeId?: number): any[] {
  return items
    .filter((item: any) => item.id !== excludeId)
    .map((item: any) => ({
      label: item.name,
      value: item.id,
      children: item.children ? buildTreeSelect(item.children, excludeId) : undefined,
    }));
}
```

**为什么部门可以这么做**？

因为后端 `/departments/tree` 接口已经返回了带 `children` 的树结构。**直接复用**这棵树即可，不需要把扁平数据再建一次。

**对比菜单的写法**（菜单是基于扁平数据建树）：

```typescript
// 菜单：基于扁平数组
function buildMenuTreeSelect(flatMenus: any[], excludeId?: number): any[] {
  const filtered = flatMenus.filter((m: any) => m.id !== excludeId);
  const build = (parentId: number): any[] => { ... };
  return build(0);
}
```

两者都是正确的，但**部门写法更简洁**，因为后端已经建好树了。

**`excludeId`** 是关键：编辑某部门时，**自己不能当自己的父级**，也不应该把后代当父级（会形成环）。最简单稳妥的做法是**把自己从候选列表里过滤掉**。

### 3.3 editingId 模式：与菜单一致

```typescript
const editingId = ref(0);

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    if (editingId.value) {
      await updateDeptApi(editingId.value, values);
      message.success('更新成功');
    } else {
      await createDeptApi(values);
      message.success('创建成功');
    }
    modalApi.close();
    gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
      editingId.value = data?.id || 0;
      // 关键：每次打开都重新拉取部门树，排除当前编辑节点
      const deptTree: any[] = await getDeptTreeApi() || [];
      const excludeId = data?.id;
      formApi.updateSchema([{
        fieldName: 'parentId',
        componentProps: { treeData: buildTreeSelect(deptTree, excludeId) },
      }]);
      if (data) formApi.setValues(data);
    }
  },
});
```

**和菜单的差异**：

- 菜单的父级选择器在 `try-catch` 里加载（容错）
- 部门直接 await，因为父级选择器是**必填**的，加载失败要直接抛错

两者设计取向不同：菜单的父级可以不选（顶级），部门也允许留空，但部门数据量小，加载失败的概率可以忽略。

### 3.4 行操作：新增子部门

```typescript
function onCreate(parentId = 0) { modalApi.setData({ parentId }).open(); }

// 表格行模板
<VbenTableAction
  :actions="[
    { text: '新增', auth: 'system:dept:create', onClick: () => onCreate(row.id) },
    { text: '编辑', auth: 'system:dept:update', onClick: () => onEdit(row) },
  ]"
  :dropdown-actions="[
    { text: '删除', auth: 'system:dept:delete', danger: true,
      popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }
  ]"
/>
```

**每个部门都能直接「新增」子部门**，自动把当前行 ID 作为 `parentId` 传给 Modal。这是树形管理模块的标配体验。

### 3.5 表单 Schema

```typescript
const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'name', label: '部门名称', rules: 'required' },
  { component: 'Input', fieldName: 'code', label: '部门编码', rules: 'required',
    help: '如 TECH, HR, FIN' },
  { component: 'TreeSelect', fieldName: 'parentId', label: '上级部门',
    componentProps: {
      treeData: [],
      placeholder: '留空为顶级部门',
      allowClear: true,
      showSearch: true,
      treeNodeFilterProp: 'label',
      treeLine: true,
      treeDefaultExpandAll: true,
      dropdownStyle: { maxHeight: '400px' },
      style: { width: '100%' },
    },
  },
  { component: 'Input', fieldName: 'leader', label: '负责人' },
  { component: 'Input', fieldName: 'phone', label: '联系电话' },
  { component: 'Input', fieldName: 'email', label: '邮箱' },
  { component: 'InputNumber', fieldName: 'sort', label: '排序', defaultValue: 0,
    componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
];
```

**注意 `treeLine: true`** 这个属性：

- 在 TreeSelect 里显示**树形连接线**
- 视觉上更清晰地看出父子关系
- 配合 `treeDefaultExpandAll`，打开下拉就能看到完整组织架构

跟菜单表单的对比：

- **没有 `dependencies.if`**：所有字段都常驻显示，没有动态显隐
- **没有 `IconPicker`**：部门不需要图标
- **`TreeSelect` 比 `Select` 更适合**：父级本身是树形数据

### 3.6 微交互：树节点 hover 样式

部门页面在 `<style>` 里加了一些细节：

```scss
:deep(.vxe-tree--btn-wrapper) {
  .vxe-tree-icon {
    color: hsl(var(--muted-foreground));
    transition: all 0.2s ease;

    &:hover {
      color: hsl(var(--primary));
    }
  }
}

:deep(.vxe-tree-cell) {
  .vxe-tree-wrapper {
    align-items: center;
  }
}

/* 树形节点 hover 效果 */
:deep(.vxe-body--row) {
  transition: background-color 0.2s ease;
}
```

**细节**：

- 展开/折叠图标 hover 时变主题色
- 行背景过渡 0.2s，鼠标划过有"呼吸感"

这些细节单看没什么，**累积起来就是产品的精致度**。

<!-- 部门管理界面截图 -->
![部门管理](/screenshots/department.png)

---

## 四、API 层：扁平 + 树形双接口

`api/system/department.ts` 提供两种接口：

```typescript
// 扁平接口：用于表格渲染（前端 transform 自动建树）
export async function getAllDeptsApi() {
  return requestClient.get('/departments');
}

// 树形接口：用于父级选择器（直接复用后端建好的树）
export async function getDeptTreeApi() {
  return requestClient.get('/departments/tree');
}

// 分页接口：用于按关键字搜索场景
export async function getDeptListApi(params: any) {
  const result = await requestClient.get('/departments/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function createDeptApi(data: any) { ... }
export async function updateDeptApi(id: number, data: any) { ... }
export async function deleteDeptApi(id: number) { ... }
```

**为什么同时要 `getAllDeptsApi` 和 `getDeptTreeApi`**？

- 表格用扁平数据：靠 VxeTable 的 `transform` 建树，**渲染层和后端解耦**
- TreeSelect 用树形数据：Ant Design Vue 的 `TreeSelect` 直接接受 `treeData`，**省一层转换**

两种数据格式各自服务不同组件，**让接口贴合使用方**，比强行统一格式更优雅。

---

## 五、与用户/数据权限的联动

部门表的真正价值不在 CRUD，而在**数据权限**。

### 5.1 用户表关联部门

每个用户都有 `DepartmentId` 字段，决定他属于哪个部门。一个用户**只属于一个部门**（简化模型）。

### 5.2 数据权限策略

Chet.Admin 的角色表里有数据权限策略：

- **All** 看全部数据
- **Department** 只看本部门
- **DepartmentAndSub** 看本部门及子部门
- **Custom** 自定义部门集合

执行查询时，**根据当前用户部门 + 角色策略**，动态拼接 `Where` 条件：

```csharp
// 伪代码示意
if (policy == DataPermissionPolicy.Department)
{
    query = query.Where(u => u.DepartmentId == currentUserId);
}
else if (policy == DataPermissionPolicy.DepartmentAndSub)
{
    var deptIds = GetDeptAndSubIds(user.DepartmentId);
    query = query.Where(u => deptIds.Contains(u.DepartmentId.Value));
}
```

**这里有个隐含约束**：

- 部门表必须有**完整的层级关系**
- 删除部门时要检查是否还有用户挂在该部门下
- 否则会出现"孤儿用户"，数据权限失效

Chet.Admin 目前简化了这层校验，但**生产环境**里建议补上：

```csharp
public async Task DeleteDepartmentAsync(int id)
{
    var dept = await _departmentRepository.GetByIdAsync(id)
        ?? throw new NotFoundException(nameof(DepartmentEntity), id);

    // 建议补充：检查是否有子部门和关联用户
    var hasChildren = await _dbContext.Departments.AnyAsync(d => d.ParentId == id);
    if (hasChildren)
        throw new BadRequestException("该部门下有子部门，无法删除");

    var hasUsers = await _dbContext.Users.AnyAsync(u => u.DepartmentId == id);
    if (hasUsers)
        throw new BadRequestException("该部门下有用户，无法删除");

    _departmentRepository.Delete(dept);
    await _departmentRepository.SaveChangesAsync();
}
```

> 💡 这是个**留给使用者按需扩展**的点，不破坏现有 CRUD 体验。

---

## 六、设计回顾

- ✅ **字段精简**：只保留部门必备字段，没有冗余
- ✅ **Code 唯一校验**：业务层主动校验，返回友好错误
- ✅ **多字段搜索**：Code/Name/Leader 任一匹配
- ✅ **扁平 + 树形双接口**：贴合不同使用方
- ✅ **TreeSelect 复用后端树**：减少前端转换
- ✅ **excludeId 防环**：编辑时排除当前节点
- ✅ **editingId 模式**：和菜单等模块保持一致
- ✅ **数据权限载体**：作为用户数据范围的判定依据
- ✅ **微交互**：树节点 hover 状态、过渡动画

---

## 七、和菜单模块的对比

放一张完整对比表，方便理解两个模块的"和而不同"：

| 维度 | 菜单 | 部门 |
| ---- | ---- | ---- |
| 实体字段 | 多（Type/Path/Component/Icon/Permission...） | 少（Name/Code/Leader/Phone/Email） |
| 类型分类 | 三态（目录/菜单/按钮） | 单态 |
| 路由生成 | ✅ 是 | ❌ 否 |
| 唯一校验 | Path 隐式唯一 | Code 显式校验 |
| 树形数据接口 | `/menus/tree` | `/departments/tree` |
| 用户关联 | 多对多（角色-菜单） | 一对一（用户-部门） |
| 权限用途 | 控制 UI 显隐 | 控制数据范围 |
| IconPicker | ✅ 使用 | ❌ 不用 |
| 表单动态显隐 | ✅ dependencies.if | ❌ 全部常驻 |

**核心差异一句话**：菜单管"能不能点"，部门管"能看哪些数据"。

---

## 下篇预告

下篇讲 **字典管理 + useDict**：字典类型 + 字典项 CRUD、组合式函数 useDict 的设计、业务表单如何自动联动字典数据。

> **「Chet.Admin 模块详解⑥：字典管理 + useDict 联动表单 📖」** 敬请期待 👀

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#全栈开发` `#.NET10` `#Vue3` `#组织架构` `#RBAC`
