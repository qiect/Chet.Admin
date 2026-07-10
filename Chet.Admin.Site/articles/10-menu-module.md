# Chet.Admin 模块详解④：菜单树 + 动态路由生成 🌳

> 《Chet.Admin 全栈实战》系列第 10 篇

---

## 前言

权限系统里，**菜单** 是个被低估的模块。

很多人以为菜单就是一串 JSON 配置，写死在前端就行了。但企业级项目里，菜单其实承担了三重身份：

- 🗂️ **导航骨架**：决定侧边栏长什么样
- 🛣️ **路由表**：决定哪些 URL 可访问
- 🔐 **权限载体**：按钮级别的细粒度控制

**Chet.Admin** 把这三件事整合到了一套「菜单实体 + 动态路由」方案里。今天这篇就来拆开看。

---

## 一、菜单实体的三种类型

菜单实体定义在 `Chet.Admin.Domain/Menu/MenuEntity.cs`，关键字段如下：

```csharp
public class MenuEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Component { get; set; }      // 组件路径
    public string? Redirect { get; set; }       // 重定向
    public string? Icon { get; set; }           // 图标
    public int ParentId { get; set; }           // 父菜单ID（0=顶级）
    public string Type { get; set; } = "Menu";  // 类型
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExternal { get; set; }        // 是否外链
    public bool IsCache { get; set; }           // 是否缓存
    public bool IsVisible { get; set; } = true; // 是否显示
    public string? Permission { get; set; }     // 权限标识
    public string? Description { get; set; }
    public List<MenuEntity> Children { get; set; } = [];
    public List<RoleMenuEntity> RoleMenus { get; set; } = [];
}
```

最核心的划分在 **`Type`** 字段，共三种类型：

| 类型 | Type 值 | 作用 | 典型场景 |
| ---- | ---- | ---- | ---- |
| 📁 目录 | `Directory` | 一级分组，不渲染页面 | 「系统管理」「用户中心」 |
| 📄 菜单 | `Menu` | 实际页面，对应路由 | 「用户列表」「角色列表」 |
| 🔘 按钮 | `Button` | 不进路由，只做权限校验 | 「删除用户」「导出 Excel」 |

**为什么要分目录/菜单/按钮？**

因为三种东西在 UI 上的呈现完全不同：

- 目录 = 折叠面板，只渲染标题和图标
- 菜单 = 渲染 `<router-view>`
- 按钮 = 不进侧边栏，但会出现在 `v-access` 指令的校验码里

如果用一张表混存，再写一堆 `if-else` 判断渲染方式，代码会很乱。**用 Type 字段切分后，每条数据职责清晰**。

---

## 二、后端：递归构建菜单树

### 2.1 控制器的 7 个接口

`MenusController.cs` 暴露的接口一览：

```csharp
[HttpGet]            // 所有菜单（扁平）
[HttpGet("tree")]    // 菜单树（全量）
[HttpGet("my-tree")] // 当前用户菜单树（按角色过滤）
[HttpGet("paged")]   // 分页查询
[HttpGet("{id}")]    // 详情
[HttpPost]           // 创建
[HttpPut("{id}")]    // 更新
[HttpDelete("{id}")] // 删除
```

注意 **`my-tree`** 这个接口，它是动态路由的入口。普通用户登录后不会拿到全部菜单，而是按角色过滤后的子集。

### 2.2 递归建树：BuildMenuTree

`MenuService.GetMenuTreeAsync()` 返回全量菜单树，核心是 `BuildMenuTree` 方法：

```csharp
private static IEnumerable<MenuTreeDto> BuildMenuTree(List<MenuTreeDto> allMenus, int parentId)
{
    return allMenus
        .Where(m => m.ParentId == parentId)
        .OrderBy(m => m.Sort)
        .Select(m =>
        {
            m.Children = BuildMenuTree(allMenus, m.Id).ToList();
            return m;
        })
        .ToList();
}
```

**思路**：

- 从扁平列表里挑出 `ParentId == parentId` 的节点
- 对每个节点**递归**调用自己，把结果挂到 `Children`
- 按 `Sort` 排序，保证展示顺序可控

**优点**：简单、不需要建索引；数据库一次性查回扁平列表，无 N+1 问题。

### 2.3 用户菜单：祖先补全算法

最有意思的是 `GetMyMenuTreeAsync`，它要根据用户角色返回"我的菜单树"。

如果直接拿 `userMenus` 建树，会出问题：**叶子节点的父级不在集合里**，树就断了。所以必须把祖先菜单**补全**进来：

```csharp
public async Task<IEnumerable<MenuTreeDto>> GetMyMenuTreeAsync(int userId)
{
    var allMenus = (await _menuRepository.GetAllAsync()).ToList();
    var userMenus = (await _menuRepository.GetMenusByUserIdAsync(userId)).ToList();

    if (userMenus.Count == 0) return [];

    // 收集需要包含的菜单ID（被分配的菜单 + 其所有祖先菜单）
    var includedIds = new HashSet<int>();
    var menuById = allMenus.ToDictionary(m => m.Id);

    foreach (var menu in userMenus)
    {
        var current = menu;
        while (current != null && includedIds.Add(current.Id))
        {
            if (current.ParentId > 0
                && menuById.TryGetValue(current.ParentId, out var parent))
            {
                current = parent;
            }
            else
            {
                break;
            }
        }
    }

    var filteredMenus = allMenus.Where(m => includedIds.Contains(m.Id)).ToList();
    var menuDtos = _mapper.Map<List<MenuTreeDto>>(filteredMenus);
    return BuildMenuTree(menuDtos, 0);
}
```

**关键点**：

- 用 `HashSet<int>` 记录要包含的 ID，自动去重
- `includedIds.Add(...)` 返回 `false` 说明遇到环，跳出避免死循环
- 从叶子节点**向上回溯**，直到 `ParentId == 0`
- 最后用同一套 `BuildMenuTree` 重新建树

这样返回给前端的菜单树**层级完整**，目录结构不会断。

---

## 三、前端：动态路由注册

### 3.1 路由加载流程

前端的路由分三类，定义在 `router/routes/index.ts`：

```typescript
const dynamicRouteFiles = import.meta.glob('./modules/**/*.ts', { eager: true });

/** 动态路由 */
const dynamicRoutes: RouteRecordRaw[] = mergeRouteModules(dynamicRouteFiles);

/** 路由列表：基本路由 + 外部路由 + 404 兜底 */
const routes: RouteRecordRaw[] = [
  ...coreRoutes,
  ...externalRoutes,
  fallbackNotFoundRoute,
];
```

- **coreRoutes**：登录页、404 页等基础路由，**不走权限校验**
- **dynamicRoutes**：通过 `import.meta.glob` 收集 `modules/` 下的静态路由模块
- **菜单路由**：登录后从后端 `my-tree` 接口拉取，**运行时通过 `addRoute` 动态注册**

### 3.2 generateAccess：菜单转路由

核心逻辑在 `router/access.ts`：

```typescript
async function generateAccess(options: GenerateMenuAndRoutesOptions) {
  const pageMap: ComponentRecordType = import.meta.glob('../views/**/*.vue');

  const layoutMap: ComponentRecordType = {
    BasicLayout,
    IFrameView,
  };

  return await generateAccessible(preferences.app.accessMode, {
    ...options,
    fetchMenuListAsync: async () => {
      message.loading({ content: `${$t('common.loadingMenu')}...`, duration: 1.5 });
      return await getMyMenusApi();
    },
    forbiddenComponent,
    layoutMap,
    pageMap,
  });
}
```

**三个关键点**：

1. **`pageMap`**：通过 `import.meta.glob` 把 `views/**/*.vue` 全部预扫描，建立「组件路径 → 异步组件」映射
2. **`fetchMenuListAsync`**：调用 `getMyMenusApi()` 拿到后端菜单树
3. **`generateAccessible`**：Vben Admin 的工具函数，把菜单树**自动转成路由配置**，内部调用 `router.addRoute` 注入

这样后端只要返回标准的菜单结构，前端不需要任何改动就能注册新页面。**新增功能模块时只动后端菜单表**就行。

### 3.3 菜单字段到路由字段的映射

后端菜单字段 → 前端路由字段的对应关系：

| 菜单字段 | 路由字段 | 说明 |
| ---- | ---- | ---- |
| `path` | `route.path` | 路由路径 |
| `component` | `route.component` | 组件路径，到 `pageMap` 里查 |
| `redirect` | `route.redirect` | 进入目录时的默认跳转 |
| `name` | `route.meta.title` | 菜单显示名 |
| `icon` | `route.meta.icon` | 图标 |
| `sort` | `route.meta.order` | 排序 |
| `isVisible` | `route.meta.hideInMenu` | 反向控制 |
| `isCache` | `route.meta.keepAlive` | 页面缓存 |
| `isExternal` | `route.meta.link` | 外链 |

**字段统一** = 后端配置即所见，不需要写路由文件。

---

## 四、前端：菜单管理页面

### 4.1 VxeTable 树形渲染

`views/system/menu/index.vue` 用 VxeTable 展示菜单树，关键配置：

```typescript
const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns,
    proxyConfig: {
      ajax: {
        query: async () => {
          // 使用扁平数据，让 vxe-table 通过 treeConfig.transform 自动构建树
          const list = await getAllMenusApi();
          return { items: list || [], total: list?.length || 0 };
        },
      },
    },
    rowConfig: { keyField: 'id' },
    treeConfig: {
      parentField: 'parentId',
      rowField: 'id',
      transform: true,        // 自动扁平 → 树
      expandAll: true,
      indent: 20,
    },
    pagerConfig: { enabled: false }, // 树形结构不分页
  },
});
```

**亮点**：

- 后端返回**扁平数组**，不需要预先建树
- VxeTable 通过 `treeConfig.transform=true` + `parentField` **自动构建树**
- 关闭分页器：菜单数据量小，一次加载全部

这样后端可以同时给扁平接口（管理列表用）和树形接口（前端路由用），各取所需。

### 4.2 三种类型的 Tag 渲染

类型字段用颜色 Tag 区分：

```typescript
const typeMap: Record<string, { color: string; label: string }> = {
  Directory: { color: 'processing', label: '目录' },  // 蓝色
  Menu:      { color: 'success',    label: '菜单' },  // 绿色
  Button:    { color: 'warning',    label: '按钮' },  // 橙色
};

// 列定义里用 slots 自定义渲染
{
  field: 'type', title: '类型', width: 80,
  slots: {
    default: ({ row }) => {
      const t = typeMap[row.type];
      return h(Tag, { color: t?.color || 'default' }, () => t?.label || row.type);
    },
  },
},
```

一眼就能看出哪行是目录、哪行是菜单、哪行是按钮。

### 4.3 表单字段的联动显隐

最有意思的是表单 Schema。不同类型需要填的字段不一样：

- 目录：要 `redirect`，不要 `component`
- 菜单：要 `component`、`isCache`、`isExternal`
- 按钮：只要 `permission` 和 `description`

Vben Form 的 `dependencies.if` 字段完美解决：

```typescript
const formSchema: VbenFormSchema[] = [
  {
    component: 'Select', fieldName: 'type', label: '类型',
    defaultValue: 'Menu', rules: 'required',
    componentProps: {
      options: [
        { label: '目录', value: 'Directory' },
        { label: '菜单', value: 'Menu' },
        { label: '按钮', value: 'Button' },
      ],
    },
    help: '目录=一级分组, 菜单=页面, 按钮=操作权限',
  },
  {
    component: 'Input', fieldName: 'permission', label: '权限标识',
    help: '如 system:user:list',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu' || values.type === 'Button'; },
    },
  },
  {
    component: 'Input', fieldName: 'component', label: '组件路径',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu'; },
    },
  },
  {
    component: 'Input', fieldName: 'redirect', label: '重定向',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Directory'; },
    },
  },
  // ... 其他字段
];
```

**机制**：

- `triggerFields` 声明依赖的表单字段
- 当 `type` 变化时，框架会重新计算 `if(values)`
- 返回 `false` 的字段会从表单里**卸载**

切到「按钮」类型时，路由路径、组件路径、图标全部消失，表单瞬间清爽。**无需手写 `v-if`**。

### 4.4 editingId 模式：追踪编辑状态

Chet.Admin 全项目统一用 `editingId` 区分新增和编辑：

```typescript
const editingId = ref(0);

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    if (editingId.value) {
      await updateMenuApi(editingId.value, values);
      message.success('更新成功');
    } else {
      await createMenuApi(values);
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
      // ... 加载父级菜单树
      if (data) formApi.setValues(data);
    }
  },
});

function onCreate(parentId = 0) { modalApi.setData({ parentId }).open(); }
function onEdit(row: any) { modalApi.setData(row).open(); }
```

**好处**：

- 同一个 Modal 同时承担新增和编辑，**减少代码重复**
- `editingId === 0` 一眼看出是新增模式
- `onCreate(parentId)` 支持从某行点击「新增子菜单」，自动填入父级

---

## 五、IconPicker：图标选择器组件

菜单管理里有个看起来不起眼但很关键的组件：**IconPicker**。

Chet.Admin 用的是 [Iconify](https://iconify.design/) 体系，集成了 **15 万 +** 个图标。但怎么让用户选？写个 Input 让用户手动敲图标名？显然不行。

### 5.1 全局注册

在 `adapter/component/index.ts` 里全局注册：

```typescript
async function initComponentAdapter() {
  const components: Partial<Record<ComponentType, Component>> = {
    // ... 其他组件
    IconPicker: withDefaultPlaceholder(IconPicker, 'select', {
      iconSlot: 'addonAfter',       // 图标显示在输入框右侧
      inputComponent: Input,
      modelValueProp: 'value',
    }),
  };
  globalShareState.setComponents(components);
}
```

### 5.2 在 Schema 里使用

菜单表单里直接当字段类型用：

```typescript
{
  component: 'IconPicker', fieldName: 'icon', label: '图标',
  componentProps: {
    prefix: 'lucide',      // 默认 lucide 图标集
    autoFetchApi: false,   // 不自动拉取，避免首屏卡顿
  },
  dependencies: {
    triggerFields: ['type'],
    if(values) { return values.type !== 'Button'; },  // 按钮不选图标
  },
},
```

**设计点**：

- `prefix: 'lucide'` 限定图标集，避免 15 万图标全展开
- `autoFetchApi: false` 按需加载，打开弹窗时才请求
- 通过 `withDefaultPlaceholder` 包装后，统一了 placeholder 体验

<!-- 菜单管理界面截图 -->
![菜单管理](/screenshots/menu.png)

---

## 六、API 层的极简封装

`api/system/menu.ts` 只有 5 个函数，**零冗余**：

```typescript
import { requestClient } from '#/api/request';

export async function getAllMenusApi() {
  return requestClient.get('/menus');
}

export async function getMenuTreeApi() {
  return requestClient.get('/menus/tree');
}

export async function createMenuApi(data: any) {
  return requestClient.post('/menus', data);
}

export async function updateMenuApi(id: number, data: any) {
  return requestClient.put(`/menus/${id}`, data);
}

export async function deleteMenuApi(id: number) {
  return requestClient.delete(`/menus/${id}`);
}
```

**没有手写 try-catch、没有手写 loading**，这些通用逻辑都被 `requestClient` 封装掉了。**业务代码只关心"调哪个接口"**。

---

## 七、权限标识的妙用

菜单的 `Permission` 字段（如 `system:user:list`）有两个用途：

### 7.1 控制按钮显隐

```vue
<Button v-if="hasAccessByCodes(['system:menu:create'])" type="primary" @click="onCreate(0)">
  <Plus class="mr-2 size-4" />新增
</Button>
```

### 7.2 控制表格行操作

```vue
<VbenTableAction
  :actions="[
    { text: '新增', auth: 'system:menu:create', onClick: () => onCreate(row.id) },
    { text: '编辑', auth: 'system:menu:update', onClick: () => onEdit(row) },
  ]"
  :dropdown-actions="[
    { text: '删除', auth: 'system:menu:delete', danger: true,
      popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }
  ]"
/>
```

`auth` 字段就是权限码。**用户没权限 → 按钮/操作自动隐藏**，无需手写 `v-if`。

---

## 八、设计回顾

总结一下这套方案的几个亮点：

- ✅ **菜单即路由**：后端配置菜单即配置路由，无需改前端代码
- ✅ **祖先补全算法**：保证用户菜单树层级完整
- ✅ **类型字段切分**：目录/菜单/按钮职责清晰，表单动态显隐
- ✅ **VxeTable transform**：扁平数据自动建树，前后端都简单
- ✅ **IconPicker 全局注册**：15 万图标即选即用
- ✅ **editingId 模式**：新增编辑共用 Modal，减少代码重复
- ✅ **权限码统一**：菜单表里的 `permission` 字段直接驱动前端按钮显隐

---

## 下篇预告

下篇讲 **部门管理**：树形 CRUD + 父子关系 + 部门编码唯一校验 + 与用户数据权限联动。

> **「Chet.Admin 模块详解⑤：部门树形管理与组织架构 🏢」** 敬请期待 👀

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#全栈开发` `#.NET10` `#Vue3` `#动态路由` `#RBAC`
