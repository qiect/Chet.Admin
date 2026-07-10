# Chet.Admin 模块详解⑥：字典管理 + useDict 联动表单 📖

> 《Chet.Admin 全栈实战》系列第 12 篇

---

## 前言

写后台系统时，有一类数据特别烦人：

- 用户状态（启用 / 禁用）
- 性别（男 / 女 / 未知）
- 订单状态（待支付 / 已支付 / 已发货 / 已完成 / 已取消）
- 用户类型（管理员 / 普通用户 / VIP）

如果把这些**枚举值写死在代码里**：

- ❌ 加新选项要发版
- ❌ 不同模块对"已支付"的 label 不一致
- ❌ 切换多语言要改一堆代码
- ❌ 业务方无法自助维护

**字典管理** 就是解决这个问题的。把所有可枚举的下拉选项**下沉到数据库 + 后台管理**，前端通过组合式函数 `useDict` 一行代码拿到选项。

Chet.Admin 这套设计很轻量，今天就来拆开看。

---

## 一、字典实体设计：父子同表

字典实体定义在 `Chet.Admin.Domain/Dictionary/DictionaryEntity.cs`：

```csharp
public class DictionaryEntity : BaseEntity
{
    public string DictType { get; set; } = string.Empty;   // 字典类型编码
    public string Name { get; set; } = string.Empty;       // 字典名称
    public string Value { get; set; } = string.Empty;      // 字典值
    public string Label { get; set; } = string.Empty;      // 字典标签
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Remark { get; set; }
    public int ParentId { get; set; }                       // 父级ID
}
```

**关键设计：父子同表**。

- `ParentId == 0` → 字典**类型**（比如「用户性别」）
- `ParentId > 0` → 字典**子项**（比如「男」「女」「未知」）

用一张表存两种数据，靠 `ParentId` 区分。**优点**：

- 不用建两张表 + 外键关联
- 字典类型和子项 CRUD 接口完全统一
- 字段语义清晰

来看下典型数据：

```
ID  DictType       Name        Value   Label   ParentId  Sort
1   user_status    用户状态    0       -       0         0      ← 类型
2   user_status    启用        1       启用    1         1      ← 子项
3   user_status    禁用        0       禁用    1         2      ← 子项
4   user_gender    用户性别    0       -       0         0      ← 类型
5   user_gender    男          1       男      4         1
6   user_gender    女          2       女      4         2
7   user_gender    未知        0       未知    4         3
```

注意 **`Value` 是字符串**，不是数字。这样字典项可以是 `"M"`、`"F"`、`"VIP"` 这种业务编码，灵活性更高。

---

## 二、后端：字典服务

### 2.1 控制器的 8 个接口

`DictionariesController.cs` 接口一览：

```csharp
[HttpGet]                    // 所有字典
[HttpGet("paged")]           // 分页（支持 keyword + dictType 过滤）
[HttpGet("type/{dictType}")] // 按类型查
[HttpGet("{id}")]             // 详情
[HttpPost]                   // 创建
[HttpPut("{id}")]             // 更新
[HttpDelete("{id}")]          // 删除
[HttpGet("code/{code}")]     // 按编码查启用的子项 ⭐
```

**最关键的是最后一个 `/code/{code}`**：

```csharp
[HttpGet("code/{code}")]
public async Task<IActionResult> GetDictionaryByCode(string code)
{
    var items = await _dictionaryService.GetItemsByCodeAsync(code);
    return Ok(ApiResponse.Ok(items, "Dictionary items retrieved successfully"));
}
```

业务表单（比如用户编辑表单）调它，拿到下拉选项。**这是 `useDict` 真正调的接口**。

### 2.2 GetItemsByCodeAsync：双层查询

`DictionaryService.GetItemsByCodeAsync` 是核心方法：

```csharp
public async Task<List<DictionaryItemDto>> GetItemsByCodeAsync(string code)
{
    _logger.LogInformation("Getting dictionary items by code: {Code}", code);
    var dbContext = (AppDbContext)_unitOfWork.DbContext;

    // Step 1: 找到字典类型（DictType == code，且 ParentId == 0）
    var parent = await dbContext.Dictionaries
        .AsNoTracking()
        .FirstOrDefaultAsync(d => d.DictType == code && d.ParentId == 0);

    if (parent == null) return new List<DictionaryItemDto>();

    // Step 2: 找该类型下所有启用的子项
    var items = await dbContext.Dictionaries
        .AsNoTracking()
        .Where(d => d.ParentId == parent.Id && d.IsEnabled)
        .OrderBy(d => d.Sort)
        .Select(d => new DictionaryItemDto { Value = d.Value, Label = d.Label })
        .ToListAsync();

    return items;
}
```

**两步走的妙处**：

1. **先查父级**：通过 `DictType == code && ParentId == 0` 唯一定位字典类型
2. **再查子项**：通过 `ParentId == parent.Id && IsEnabled` 过滤启用的子项

**为什么不一步到位**？

如果只查 `DictType == code`，会同时返回类型和子项。然后前端再过滤一遍，**多传了无用数据**。

两步走虽然多查一次，但每次都很轻量（索引命中），**响应数据更精炼**。

### 2.3 双维度分页查询

`GetPagedDictionariesAsync` 支持按 **类型 + 关键字** 组合过滤：

```csharp
public async Task<PagedResult<DictionaryDto>> GetPagedDictionariesAsync(PagedRequest request)
{
    var dbContext = (AppDbContext)_unitOfWork.DbContext;
    var query = dbContext.Dictionaries.AsNoTracking();

    // 精确匹配字典类型
    if (!string.IsNullOrWhiteSpace(request.DictType))
    {
        var dictType = request.DictType.Trim();
        query = query.Where(d => d.DictType == dictType);
    }

    // 关键字模糊匹配（多字段 OR）
    if (!string.IsNullOrWhiteSpace(request.Keyword))
    {
        var keyword = request.Keyword.Trim();
        query = query.Where(d => d.DictType.Contains(keyword)
                              || d.Name.Contains(keyword)
                              || d.Label.Contains(keyword)
                              || d.Value.Contains(keyword));
    }

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(d => d.CreatedAt)
        .Skip(request.Skip)
        .Take(request.PageSize)
        .ToListAsync();

    var dictDtos = _mapper.Map<List<DictionaryDto>>(items);
    return new PagedResult<DictionaryDto>(dictDtos, request.PageNumber, request.PageSize, totalCount);
}
```

**两种过滤维度**：

- `DictType`：**精确匹配**，用于"只看某个字典类型下的所有项"
- `Keyword`：**模糊匹配**，跨 4 个字段（DictType / Name / Label / Value）搜索

**为什么 DictType 用精确匹配，而其他字段用模糊匹配**？

- DictType 是结构化字段（用户在搜索框里选择，不输错），精确匹配能命中索引
- 关键字搜索是用户输入的"猜词"，模糊匹配找回更多结果

**倒序 CreatedAt**：字典管理通常是"最近添加的更关心"，所以新数据排在前面。

### 2.4 创建/更新/删除：标准 CRUD

```csharp
public async Task<DictionaryDto> CreateDictionaryAsync(DictionaryCreateDto dto)
{
    var dict = _mapper.Map<DictionaryEntity>(dto);
    await _dictionaryRepository.AddAsync(dict);
    await _dictionaryRepository.SaveChangesAsync();
    return _mapper.Map<DictionaryDto>(dict);
}
```

**没有 Code 唯一校验**，因为字典可以有同 DictType 的多条记录（同一类型下有多个子项）。要校验也是校验 `DictType + Value` 组合唯一，简化起见这里靠业务规范约束。

---

## 三、前端：字典管理页面

### 3.1 分页列表 + 双字段搜索

`views/system/dictionary/index.vue` 跟菜单/部门最大区别：**用分页**。

```typescript
const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
  { component: 'Input', fieldName: 'dictType', label: '字典类型' },
];

const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },  // ⭐ 输入即查询
  gridOptions: {
    columns,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => await getDictListApi({
          pageNumber: page.currentPage,
          pageSize: page.pageSize,
          ...formValues,
        }),
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  },
});
```

**亮点**：

- **`submitOnChange: true`**：搜索框输入即时触发查询，**无需点搜索按钮**
- **`toolbarConfig.search: true`**：表格工具栏内置搜索区
- 不用 `treeConfig`：字典是扁平的（虽然物理上有父子关系，但管理界面**按行展示更直观**）

**为什么字典不分树形展示**？

- 字典子项数量通常较少（5-20 个），不值得建树
- 用户更关心"按字典类型筛选"，而不是看父子层级
- 分页 + 类型筛选 = 最高效的管理方式

### 3.2 列定义

```typescript
const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 80 },
  { field: 'dictType', title: '字典类型', minWidth: 120 },
  { field: 'name', title: '名称', minWidth: 150 },
  { field: 'value', title: '值', minWidth: 100 },
  { field: 'label', title: '标签', minWidth: 120 },
  { field: 'sort', title: '排序', width: 80 },
  { field: 'isEnabled', title: '状态', width: 80, cellRender: { name: 'CellTag' } },
  { field: 'remark', title: '备注', minWidth: 150 },
  { align: 'center', field: 'operation', fixed: 'right',
    slots: { default: 'action' }, title: '操作', width: 160 },
];
```

**`cellRender: { name: 'CellTag' }`** 是 VxeTable 的内置渲染器，根据布尔值显示「启用 / 禁用」Tag。**零代码自定义渲染**。

### 3.3 表单 Schema：所有字段平铺

```typescript
const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'dictType', label: '字典类型', rules: 'required' },
  { component: 'Input', fieldName: 'name', label: '名称', rules: 'required' },
  { component: 'Input', fieldName: 'value', label: '字典值', rules: 'required' },
  { component: 'Input', fieldName: 'label', label: '标签', rules: 'required' },
  { component: 'InputNumber', fieldName: 'sort', label: '排序', defaultValue: 0,
    componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
  { component: 'Textarea', fieldName: 'remark', label: '备注' },
  { component: 'InputNumber', fieldName: 'parentId', label: '父级ID', defaultValue: 0,
    componentProps: { style: { width: '100%' } } },
];
```

**注意 `parentId` 用 `InputNumber`**：

- 创建字典类型时：`parentId = 0`
- 创建字典子项时：`parentId = 字典类型的 id`

这里没有用 TreeSelect，因为字典管理是"扁平 + 类型筛选"模式，用户更多是**按类型分批维护子项**。

### 3.4 editingId 模式：和菜单/部门一致

```typescript
const editingId = ref(0);

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    if (editingId.value) {
      await updateDictApi(editingId.value, values);
      message.success('更新成功');
    } else {
      await createDictApi(values);
      message.success('创建成功');
    }
    modalApi.close();
    gridApi.query();
  },
  onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
      editingId.value = data?.id || 0;
      if (data) formApi.setValues(data);
    }
  },
});
```

**全项目统一的 Modal 模式**，写多了闭眼也能写对。

<!-- 字典管理界面截图 -->
![字典管理](/screenshots/dictionary.png)

---

## 四、useDict：组合式函数设计

字典管理的真正威力在于 **`useDict`** 这个组合式函数。它让业务表单**一行代码**就能联动字典数据。

### 4.1 完整实现

`composables/useDict.ts` 只有 35 行，但信息密度极高：

```typescript
import { ref } from 'vue';
import { requestClient } from '#/api/request';

// 模块级缓存：所有 useDict 实例共享
const dictCache = new Map<string, { label: string; value: string }[]>();

export function useDict(code: string) {
  const options = ref<{ label: string; value: string }[]>([]);
  const loading = ref(false);

  async function load() {
    // 命中缓存直接返回
    if (dictCache.has(code)) {
      options.value = dictCache.get(code)!;
      return;
    }
    loading.value = true;
    try {
      const res = await requestClient.get(`/dictionaries/code/${code}`);
      const items = res?.items || res || [];
      const mapped = items.map((item: any) => ({
        label: item.label,
        value: item.value,
      }));
      dictCache.set(code, mapped);
      options.value = mapped;
    } catch {
      options.value = [];
    } finally {
      loading.value = false;
    }
  }

  load();  // ⭐ 实例化即加载

  return { options, loading, refresh: load };
}
```

### 4.2 设计拆解

**① 模块级缓存 `dictCache`**

```typescript
const dictCache = new Map<string, { label: string; value: string }[]>();
```

这是**模块作用域的 Map**，**所有 `useDict` 实例共享**。

如果放在函数内部：

```typescript
export function useDict(code: string) {
  const dictCache = new Map();  // ❌ 每个实例都有自己的缓存
  // ...
}
```

每次调用 `useDict('user_status')` 都会新建一个 Map，缓存失效。

放在模块顶层则**全局共享**：A 页面拉过 `user_status`，B 页面再调 `useDict('user_status')` 直接命中缓存，**不重复请求**。

**② 即时加载**

```typescript
load();  // 函数体最后一行
return { options, loading, refresh: load };
```

调用 `useDict('user_gender')` 时**立即触发加载**，不需要业务方手动调 `load()`。

**③ 三态返回**

```typescript
return { options, loading, refresh: load };
```

- `options`：响应式选项数组，直接绑定到 `<Select :options="options" />`
- `loading`：加载状态，用于骨架屏 / loading 效果
- `refresh`：强制刷新函数，**绕过缓存**

**④ 兼容多种响应格式**

```typescript
const res = await requestClient.get(`/dictionaries/code/${code}`);
const items = res?.items || res || [];  // ⭐ 双兜底
```

为什么写 `res?.items || res || []`？

- 有些接口返回 `{ items: [...] }` 结构
- 有些直接返回数组
- 加双兜底后，**后端格式调整不影响 useDict**

**⑤ 异常降级**

```typescript
} catch {
  options.value = [];  // 出错时给空数组，不抛异常
}
```

字典加载失败不应该让整个表单挂掉。**降级为空数组**，用户至少能看到表单其他部分。

### 4.3 业务表单使用示例

在用户管理表单里，性别字段这样用：

```typescript
import { useDict } from '@/composables/useDict';

const { options: genderOptions } = useDict('user_gender');
const { options: statusOptions } = useDict('user_status');
```

表单 Schema 里直接绑定：

```typescript
{
  component: 'Select',
  fieldName: 'gender',
  label: '性别',
  componentProps: {
    options: genderOptions,  // ⭐ 响应式自动更新
  },
},
{
  component: 'Select',
  fieldName: 'status',
  label: '状态',
  componentProps: {
    options: statusOptions,
  },
},
```

**业务代码只需关注：**

- 字典编码是什么（`user_gender`）
- 绑定到哪个字段（`gender`）

**完全不需要关心：**

- 数据从哪来
- 怎么调 API
- 怎么处理 loading
- 怎么处理错误
- 怎么处理缓存

---

## 五、API 层

`api/system/dictionary.ts` 提供 6 个函数：

```typescript
// 分页查询（管理页面用）
export async function getDictListApi(params: any) {
  const result = await requestClient.get('/dictionaries/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

// 按类型查询
export async function getDictByTypeApi(dictType: string) {
  return requestClient.get(`/dictionaries/type/${dictType}`);
}

// CRUD
export async function createDictApi(data: any) { ... }
export async function updateDictApi(id: number, data: any) { ... }
export async function deleteDictApi(id: number) { ... }

// ⭐ useDict 直接调这个
export async function getDictItemsByCodeApi(code: string) {
  return requestClient.get(`/dictionaries/code/${code}`);
}
```

**注意**：`useDict` 里**没有用 `getDictItemsByCodeApi`**，而是直接调 `requestClient.get`。

这是有意为之的**分层**：

- `api/` 目录给**业务页面**用
- `composables/` 直接调 `requestClient`，绕开 `api/` 层

为什么？因为 `useDict` 是**通用组合式函数**，不应该耦合业务 API 文件。它只关心"调哪个 URL"，不关心业务封装。

如果未来要换一个 HTTP 库，**只改 `requestClient` 一处即可**。

---

## 六、种子数据分析

Chet.Admin 在数据库种子数据里预置了常用字典。来看下典型用法：

### 6.1 用户性别字典

```
DictType: user_gender
├── 男 (value=1, sort=1)
├── 女 (value=2, sort=2)
└── 未知 (value=0, sort=3)
```

### 6.2 用户状态字典

```
DictType: user_status
├── 启用 (value=1, sort=1)
└── 禁用 (value=0, sort=2)
```

### 6.3 角色类型字典

```
DictType: role_type
├── 内置角色 (value=builtin, sort=1)
└── 自定义角色 (value=custom, sort=2)
```

**注意 Value 的设计**：

- 性别用数字（1/2/0）→ 便于数据库存储为整型
- 角色类型用字符串（builtin/custom）→ 便于代码可读

**字典的 Value 是字符串** = 业务方自由决定编码方案。

### 6.4 业务表存储的是 Value

用户表里：

| ID | UserName | Gender | Status |
| ---- | ---- | ---- | ---- |
| 1 | admin | 1 | 1 |
| 2 | alice | 2 | 1 |
| 3 | bob | 0 | 0 |

- `Gender=1` 表示男
- `Status=1` 表示启用

数据库存的是**Value**（数字），不是 Label（"男"）。**好处**：

- 改 Label 不影响存量数据（把"男"改成"男性"，老数据不受影响）
- 节省存储空间（数字比字符串小）
- 便于排序和索引

---

## 七、缓存策略的取舍

`useDict` 的缓存策略是**永久缓存**（Map 一旦写入，刷新页面才会清空）。

| 策略 | 优点 | 缺点 |
| ---- | ---- | ---- |
| 永久缓存 | 加载最快，零重复请求 | 字典更新后前端不感知 |
| TTL 缓存 | 自动失效，相对实时 | 实现稍复杂 |
| 每次请求 | 最实时 | 性能差，体验差 |

Chet.Admin 选择**永久缓存 + 手动 refresh**：

```typescript
const { options, refresh } = useDict('user_status');

// 字典管理页更新后，业务页可以手动调 refresh
function onDictUpdated() {
  refresh();  // 绕过缓存，重新拉取
}
```

**为什么这样设计**？

- 字典数据**变更频率极低**（一年改不了几次）
- 业务页面通常打开后用一会就关，**没必要实时同步**
- 真有更新，用户刷新页面即可

如果业务对实时性有要求，可以监听字典更新事件，调用 `refresh()` 即可。**API 留好了，按需使用**。

---

## 八、设计回顾

- ✅ **父子同表**：一张表搞定类型和子项，简化结构
- ✅ **GetItemsByCodeAsync 双层查询**：精炼响应数据
- ✅ **双维度过滤**：DictType 精确匹配 + Keyword 模糊搜索
- ✅ **useDict 模块级缓存**：全局共享，零重复请求
- ✅ **即时加载 + 三态返回**：options / loading / refresh
- ✅ **异常降级**：出错给空数组，不让业务挂掉
- ✅ **submitOnChange: true**：搜索输入即时查询
- ✅ **全项目统一 editingId 模式**：减少心智负担
- ✅ **Value 字符串设计**：业务方自由决定编码方案

---

## 九、和其他模块的协同

字典管理不是孤立的，它服务整个系统：

```
┌─────────────────┐
│  字典管理页面    │  ← 管理员维护字典
└────────┬────────┘
         │ /dictionaries/code/{code}
         ▼
┌─────────────────┐
│   useDict       │  ← 业务表单调用
└────────┬────────┘
         │ options
         ▼
┌─────────────────────────────────┐
│  用户表单 / 角色表单 / 订单表单  │  ← 自动联动
└─────────────────────────────────┘
```

**一个字典编码**贯穿管理后台和业务表单，**单一数据源**，永远不会出现"同一个字典在不同页面有不同选项"的问题。

---

## 下篇预告

下篇讲 **仪表盘**：6 项核心统计指标、纯 SVG 折线图实现（不依赖图表库）、纵坐标自动刻度计算、数据点 hover tooltip。

> **「Chet.Admin 模块详解⑦：不依赖图表库的 SVG 趋势图 📊」** 敬请期待 👀

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#全栈开发` `#.NET10` `#Vue3` `#字典管理` `#组合式函数`
