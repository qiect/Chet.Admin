# Chet.Admin 模块详解②：用户管理与个人中心 👤

> 《Chet.Admin 全栈实战》系列第 8 篇

---

## 前言

**用户管理** 是后台系统的"标配"，但要把细节做到位不简单：

- 📋 CRUD 怎么设计？分页关键字过滤？
- 🎭 用户怎么分配多个角色？
- 🔒 不同角色看到的数据范围不同，怎么自动过滤？
- 🧑 个人中心：头像上传、密码修改怎么做最顺滑？

**Chet.Admin** 把这些都做完了，今天咱们一起拆。

---

## 一、用户实体设计 🗂️

### 1.1 UserEntity 字段一览

```csharp
public class UserEntity : BaseEntity
{
    public string Name { get; set; }
    public string Email { get; set; }  // 唯一登录凭证

    /// BCrypt 哈希值，格式 $2a$12$...
    public string PasswordHash { get; set; }

    /// 刷新令牌（Rotation 时会被覆盖）
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public string? Avatar { get; set; }
    public int? DepartmentId { get; set; }  // 决定数据权限范围

    public List<UserRoleEntity> UserRoles { get; set; } = [];

    /// 连续登录失败次数（达到 5 次锁定）
    public int LoginFailCount { get; set; } = 0;

    /// 锁定截止时间
    public DateTime? LockedUntil { get; set; }

    /// 密码最后修改时间（用于过期策略）
    public DateTime? PasswordChangedAt { get; set; }

    /// 是否需要强制修改密码
    public bool MustChangePassword { get; set; } = false;
}
```

> 📌 **安全提示**：`UserEntity` 包含 `PasswordHash` 和 `RefreshToken` 这种敏感信息，**绝不能直接返回 API 客户端**，必须映射成 `UserDto`。

### 1.2 UserDto：响应 DTO

`UserDto` 删掉了所有敏感字段，只保留展示所需：

```csharp
public class UserDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Avatar { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    /// 角色列表（嵌套对象，不只是 ID）
    public List<UserRoleInfoDto> Roles { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserRoleInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

> 💡 **DTO 设计哲学**：领域实体管持久化，DTO 管传输。两者解耦，前端字段变更不影响数据库结构。

---

## 二、用户 CRUD + 分页 📋

### 2.1 控制器 7 个端点

`UsersController` 提供 **7 个 REST 端点**：

| 方法 | 路径 | 作用 |
| ---- | ---- | ---- |
| GET | `/users` | 获取所有用户（不分页） |
| GET | `/users/paged` | **分页 + 关键字搜索** |
| GET | `/users/{id}` | 获取单个用户详情 |
| POST | `/users` | 创建用户 |
| PUT | `/users/{id}` | 更新用户 |
| DELETE | `/users/{id}` | 删除用户 |
| POST | `/users/{id}/roles` | **分配角色** |

所有端点都加 `[Authorize]`，必须登录才能访问：

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供用户管理相关的API接口")]
public class UsersController : ControllerBase
```

### 2.2 分页查询：带数据权限过滤

分页接口的关键点：**自动按当前用户的 DataScope 过滤**。

控制器把 `currentUserId` 传给 Service：

```csharp
[HttpGet("paged")]
public async Task<IActionResult> GetPagedUsers(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? keyword = null)
{
    var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
    var currentUserId = GetUserId();  // 从 JWT Claim 里取
    var result = await _userService.GetPagedUsersAsync(request, currentUserId);

    return Ok(PaginatedResponse<UserDto>.Ok(
        result.Items, result.Metadata.TotalCount,
        result.Metadata.PageNumber, result.Metadata.PageSize,
        "Users retrieved successfully"));
}
```

### 2.3 UserService.GetPagedUsersAsync

业务层根据当前用户的 `dataScope` 拼不同的 WHERE：

```csharp
public async Task<PagedResult<UserDto>> GetPagedUsersAsync(PagedRequest request, int? currentUserId)
{
    if (!currentUserId.HasValue)
        return await GetPagedUsersAsync(request);

    var dataScope = await _dataScopeService.GetDataScopeAsync(currentUserId.Value);

    // All 范围不过滤，直接返回全部
    if (dataScope == "All")
        return await GetPagedUsersAsync(request);

    request.Normalize();
    var dbContext = (AppDbContext)_unitOfWork.DbContext;
    IQueryable<UserEntity> query = dbContext.Users.AsNoTracking()
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    // 关键字过滤（姓名 + 邮箱）
    if (!string.IsNullOrWhiteSpace(request.Keyword))
    {
        var keyword = request.Keyword.Trim();
        query = query.Where(u => u.Name.Contains(keyword) || u.Email.Contains(keyword));
    }

    // 按数据权限范围动态拼接 WHERE
    switch (dataScope)
    {
        case "Self":
            query = query.Where(u => u.Id == currentUserId.Value);
            break;

        case "Dept":
            var currentUserDept = await dbContext.Users.AsNoTracking()
                .Where(u => u.Id == currentUserId.Value)
                .Select(u => u.DepartmentId).FirstOrDefaultAsync();
            query = query.Where(u => u.DepartmentId == currentUserDept);
            break;

        case "DeptAndChild":
        case "Custom":
            var accessibleDeptIds = await _dataScopeService.GetAccessibleDeptIdsAsync(currentUserId.Value);
            if (accessibleDeptIds.Count > 0)
                query = query.Where(u => u.DepartmentId.HasValue && accessibleDeptIds.Contains(u.DepartmentId.Value));
            else
                query = query.Where(u => u.Id == currentUserId.Value);
            break;
    }

    var totalCount = await query.CountAsync();
    var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync();
    var userDtos = _mapper.Map<List<UserDto>>(items);

    return new PagedResult<UserDto>(userDtos, request.PageNumber, request.PageSize, totalCount);
}
```

> 🎯 **关键设计**：Service 不知道当前用户是谁，由 Controller 把 `currentUserId` 传进来。这样 Service 易测试，权限逻辑清晰可追溯。

<!-- 用户分页查询流程 -->
![用户分页流程](/screenshots/user-paged.svg)

### 2.4 创建用户：自动分配角色

`CreateUserAsync` 同时支持"创建用户"和"创建并分配角色"：

```csharp
public async Task<UserDto> CreateUserAsync(UserCreateDto userCreateDto)
{
    var user = _mapper.Map<UserEntity>(userCreateDto);
    user.PasswordHash = _passwordService.Hash(userCreateDto.Password);
    user.PasswordChangedAt = DateTime.UtcNow;
    user.MustChangePassword = false;

    if (userCreateDto.DepartmentId.HasValue)
        user.DepartmentId = userCreateDto.DepartmentId;

    await _userRepository.AddAsync(user);
    await _userRepository.SaveChangesAsync();

    // 关键：如果传了 RoleIds，一并创建关联
    if (userCreateDto.RoleIds is { Count: > 0 })
    {
        var dbContext = (AppDbContext)_unitOfWork.DbContext;
        foreach (var roleId in userCreateDto.RoleIds)
        {
            await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = user.Id, RoleId = roleId });
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // 清缓存（避免脏读）
    await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);

    return _mapper.Map<UserDto>(user);
}
```

### 2.5 更新用户：邮箱不可改

更新有几个**细节**要注意：

```csharp
public async Task UpdateUserAsync(int id, UserUpdateDto userUpdateDto)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null) throw new NotFoundException(nameof(UserEntity), id);

    // 1. 邮箱不可改（唯一凭证）
    if (!string.IsNullOrWhiteSpace(userUpdateDto.Name))
        user.Name = userUpdateDto.Name;

    // 2. 改密码要重新哈希 + 重置过期时间
    if (!string.IsNullOrWhiteSpace(userUpdateDto.Password))
    {
        user.PasswordHash = _passwordService.Hash(userUpdateDto.Password);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;
    }

    // 3. 部门可改
    if (userUpdateDto.DepartmentId.HasValue)
        user.DepartmentId = userUpdateDto.DepartmentId;

    _userRepository.Update(user);
    await _userRepository.SaveChangesAsync();

    // 4. 角色重新分配（先删后加）
    if (userUpdateDto.RoleIds != null)
    {
        var dbContext = (AppDbContext)_unitOfWork.DbContext;
        var existing = await dbContext.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
        dbContext.UserRoles.RemoveRange(existing);

        foreach (var roleId in userUpdateDto.RoleIds)
        {
            await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = id, RoleId = roleId });
        }
        await _unitOfWork.SaveChangesAsync();
    }

    // 5. 清缓存
    await _cacheService.RemoveAsync(CacheKeys.Users.ById(id));
    await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
}
```

> 💡 **关键约束**：邮箱是登录唯一凭证，**编辑时不允许修改**，前端表单会把邮箱字段 disabled。

### 2.6 分配角色接口

独立的"分配角色"接口，**先删旧关联再添新关联**：

```csharp
[HttpPost("{id}/roles")]
public async Task<IActionResult> AssignRoles(int id, [FromBody] List<int> roleIds)
{
    await _userService.AssignRolesAsync(id, roleIds);
    return Ok(ApiResponse.Ok(null, "Roles assigned successfully"));
}
```

```csharp
public async Task AssignRolesAsync(int userId, List<int> roleIds)
{
    var user = await _userRepository.GetByIdAsync(userId)
        ?? throw new NotFoundException(nameof(UserEntity), userId);

    var dbContext = (AppDbContext)_unitOfWork.DbContext;

    // 删除旧关联
    var existing = await dbContext.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
    dbContext.UserRoles.RemoveRange(existing);

    // 添加新关联
    foreach (var roleId in roleIds)
    {
        await dbContext.UserRoles.AddAsync(new UserRoleEntity { UserId = userId, RoleId = roleId });
    }

    await _unitOfWork.SaveChangesAsync();
    await _cacheService.RemoveAsync(CacheKeys.Users.ById(userId));
    await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
}
```

---

## 三、缓存策略：GetOrCreateAsync 🚀

用户查询**默认走缓存**，减少 DB 压力：

```csharp
public async Task<UserDto> GetUserByIdAsync(int id)
{
    var cacheKey = CacheKeys.Users.ById(id);

    return await _cacheService.GetOrCreateAsync(cacheKey, async () =>
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException(nameof(UserEntity), id);
        return _mapper.Map<UserDto>(user);
    }, CacheKeys.Expiry.Medium);
}
```

> 🎯 **缓存一致性**：每次写操作（增删改）后，都调 `RemoveByPatternAsync(CacheKeys.Users.Pattern)` 清掉所有用户缓存。**先写库再清缓存**，避免读到旧数据。

---

## 四、前端：用户列表页 🎨

### 4.1 表格列定义

`views/system/user/index.vue` 用 VxeTable 渲染列表：

```typescript
const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 70 },
  { field: 'name', title: '用户名', minWidth: 120 },
  { field: 'email', title: '邮箱', minWidth: 200 },
  {
    field: 'departmentId', title: '部门', minWidth: 120,
    slots: {
      default: ({ row }) => {
        const name = deptNameMap.value.get(row.departmentId);
        return name || '-';
      },
    },
  },
  {
    field: 'roles', title: '角色', minWidth: 160,
    slots: {
      default: ({ row }) => {
        const roles = row.roles || [];
        if (!roles.length) return '-';
        return roles.map((r: any) => h(Tag, { color: 'blue', class: 'mr-1' }, () => r.name));
      },
    },
  },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: '操作', width: 180 },
];
```

> 💡 部门名通过 `deptNameMap` 缓存映射，避免每个单元格都查一次。

### 4.2 数据加载：proxyConfig

VxeTable 通过 `proxyConfig.ajax.query` 自动拉数据：

```typescript
const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => {
          return await getUserListApi({
            pageNumber: page.currentPage,
            pageSize: page.pageSize,
            ...formValues,
          });
        },
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});
```

### 4.3 API 封装

`api/system/user.ts` 一行一个方法，简单清晰：

```typescript
export async function getUserListApi(params: any) {
  const result = await requestClient.get('/users/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

export async function createUserApi(data: any) {
  return requestClient.post('/users', data);
}

export async function updateUserApi(id: number, data: any) {
  return requestClient.put(`/users/${id}`, data);
}

export async function deleteUserApi(id: number) {
  return requestClient.delete(`/users/${id}`);
}
```

<!-- 用户列表页 -->
![用户列表页](/screenshots/user-list.svg)

---

## 五、前端：editingId 模式 🆔

### 5.1 什么是 editingId 模式

Chet.Admin 用了一个**轻量模式**管理"新增 vs 编辑"：

```typescript
const isEdit = ref(false);
const editingId = ref(0);

const [EditModal, editModalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await editFormApi.getValues();

    if (isEdit.value && editingId.value) {
      // 编辑模式：调 update
      await updateUserApi(editingId.value, {
        name: values.name,
        departmentId: values.departmentId,
        roleIds: values.roleIds,
      });
      message.success('更新成功');
    } else {
      // 新增模式：调 create
      await createUserApi(values);
      message.success('创建成功');
    }
    editModalApi.close();
    gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      editFormApi.resetForm();

      // 拉部门和角色数据
      const [deptTree, roles] = await Promise.all([
        loadDeptNameMap(),
        getRoleListAllApi(),
      ]);

      const data = editModalApi.getData<Record<string, any>>();
      isEdit.value = !!data?.id;
      if (data?.id) editingId.value = data.id;

      // 后端返回 roles: [{id, name}]，前端字段是 roleIds: number[]
      const roleIds = Array.isArray(data.roles) ? data.roles.map((r: any) => r.id) : [];

      editFormApi.setValues({
        name: data.name,
        email: data.email,
        departmentId: data.departmentId,
        roleIds,
      });
    }
  },
});

function onCreate() { createModalApi.open(); }
function onEdit(row: any) { editModalApi.setData(row).open(); }
```

**editingId 模式的精髓**：

- ✅ 新增和编辑**共用一个 Modal + Form**
- ✅ 通过 `isEdit` 和 `editingId` 区分模式
- ✅ 比维护两套表单代码量少一半
- ✅ 字段格式不一致时（如 `roles` vs `roleIds`）在打开时转换

### 5.2 邮箱字段：编辑时 disabled

```typescript
const editFormSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'name', label: '用户名', rules: 'required' },
  {
    component: 'Input', fieldName: 'email', label: '邮箱', rules: 'required',
    componentProps: { disabled: true, placeholder: '邮箱为唯一凭证，不可修改' },
    help: '邮箱为用户唯一登录凭证，不支持修改',
  },
  // ... 部门、角色
];
```

提交时**不传 email** 字段：

```typescript
await updateUserApi(editingId.value, {
  name: values.name,
  departmentId: values.departmentId,
  roleIds: values.roleIds,
});
```

### 5.3 修改密码：单独 Modal

修改密码用独立的 Modal，**不复用编辑表单**：

```typescript
const pwdFormSchema: VbenFormSchema[] = [
  {
    component: 'VbenInputPassword', fieldName: 'newPassword',
    label: '新密码', rules: 'required',
    componentProps: { placeholder: '请输入新密码', passwordStrength: true },
  },
  {
    component: 'VbenInputPassword', fieldName: 'confirmPassword',
    label: '确认密码', rules: 'required',
    componentProps: { placeholder: '再次输入新密码', passwordStrength: true },
  },
];

const [PwdModal, pwdModalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await pwdFormApi.getValues();
    if (!values.newPassword || values.newPassword.length < 6) {
      message.warning('密码至少6位'); return;
    }
    if (values.newPassword !== values.confirmPassword) {
      message.warning('两次密码不一致'); return;
    }
    await updateUserApi(pwdUserId.value, { password: values.newPassword });
    message.success('密码修改成功');
    pwdModalApi.close();
  },
});
```

> 💡 `passwordStrength: true` 会显示密码强度条，前端实时给用户视觉反馈。

### 5.4 操作列权限控制

每个按钮挂权限码，没权限自动隐藏：

```vue
<template #action="{ row }">
  <VbenTableAction
    :actions="[
      { text: '编辑', auth: 'system:user:update', onClick: () => onEdit(row) },
      { text: '修改密码', auth: 'system:user:update', onClick: () => onChangePwd(row) },
    ]"
    :dropdown-actions="[
      { text: '删除', auth: 'system:user:delete', danger: true,
        popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }
    ]"
  />
</template>
```

---

## 六、个人中心 👤

### 6.1 个人中心入口

`views/_core/profile/index.vue` 用 Tabs 切换"基本设置"和"修改密码"：

```vue
<template>
  <Profile
    v-model:model-value="tabsValue"
    title="个人中心"
    :user-info="userStore.userInfo"
    :tabs="tabs"
  >
    <template #content>
      <ProfileBaseSetting v-if="tabsValue === 'base'" />
      <ProfilePasswordSetting v-else-if="tabsValue === 'password'" />
    </template>
  </Profile>
</template>
```

### 6.2 基本设置：资料 + 头像

`base-setting.vue` 包含三部分：**头像上传、姓名修改、邮箱展示**。

**头像上传**用自定义 `customRequest`，先上传文件再保存到用户资料：

```typescript
async function handleCustomUpload(options: { file: File }) {
  const { file } = options;
  uploading.value = true;
  try {
    // 1. 调文件上传接口
    const res: any = await uploadFileApi(file);
    const filePath = res?.filePath || res?.data?.filePath;

    // 2. 把返回的路径写到个人资料
    await updateProfileApi({ avatar: filePath });
    avatarUrl.value = filePath;

    // 3. 同步更新 userStore（顶部导航立即刷新）
    if (userStore.userInfo) {
      userStore.setUserInfo({ ...userStore.userInfo, avatar: filePath });
    }
    message.success('头像更新成功');
  } catch (error) {
    message.error('头像上传失败');
  } finally {
    uploading.value = false;
  }
}
```

**上传前校验**：格式 + 大小：

```typescript
function beforeUpload(file: File) {
  const isImage = /^image\/(jpeg|png|gif|webp|bmp)$/i.test(file.type);
  if (!isImage) {
    message.error('只能上传 JPG/PNG/GIF/WEBP/BMP 格式的图片');
    return false;
  }
  const isLt2M = file.size / 1024 / 1024 < 2;
  if (!isLt2M) {
    message.error('头像图片大小不能超过 2MB');
    return false;
  }
  return true;
}
```

### 6.3 后端：UpdateProfile 接口

```csharp
[HttpPut("profile")]
[Authorize]
public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
{
    var userId = GetUserId();
    await _userService.UpdateProfileAsync(userId, dto.Name, dto.Avatar);
    return Ok(ApiResponse.Ok(null, "Profile updated successfully"));
}
```

业务层按需更新字段：

```csharp
public async Task UpdateProfileAsync(int userId, string? name, string? avatar)
{
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null) throw new NotFoundException(nameof(UserEntity), userId);

    if (!string.IsNullOrWhiteSpace(name)) user.Name = name;
    if (avatar != null) user.Avatar = avatar;

    _userRepository.Update(user);
    await _userRepository.SaveChangesAsync();

    await _cacheService.RemoveAsync(CacheKeys.Users.ById(userId));
    await _cacheService.RemoveByPatternAsync(CacheKeys.Users.Pattern);
}
```

### 6.4 修改密码：个人中心版

`password-setting.vue` 用 zod 校验两次密码一致：

```typescript
const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      fieldName: 'oldPassword', label: '旧密码',
      component: 'VbenInputPassword',
      componentProps: { placeholder: '请输入旧密码' },
    },
    {
      fieldName: 'newPassword', label: '新密码',
      component: 'VbenInputPassword',
      componentProps: { passwordStrength: true, placeholder: '请输入新密码' },
    },
    {
      fieldName: 'confirmPassword', label: '确认密码',
      component: 'VbenInputPassword',
      componentProps: { passwordStrength: true, placeholder: '请再次输入新密码' },
      dependencies: {
        rules(values) {
          const { newPassword } = values;
          return z.string({ required_error: '请再次输入新密码' })
            .min(1, { message: '请再次输入新密码' })
            .refine((value) => value === newPassword, {
              message: '两次输入的密码不一致',
            });
        },
        triggerFields: ['newPassword'],
      },
    },
  ];
});
```

后端业务层校验旧密码 + 更新：

```csharp
public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword)
{
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null) throw new NotFoundException(nameof(UserEntity), userId);

    // 验证旧密码
    if (!_passwordService.Verify(oldPassword, user.PasswordHash))
    {
        throw new BadRequestException("旧密码不正确");
    }

    user.PasswordHash = _passwordService.Hash(newPassword);
    user.PasswordChangedAt = DateTime.UtcNow;
    user.MustChangePassword = false;

    _userRepository.Update(user);
    await _userRepository.SaveChangesAsync();
}
```

<!-- 个人中心 -->
![个人中心](/screenshots/profile.svg)

---

## 七、设计要点总结 ✨

### 7.1 用户管理 API 设计

| 设计 | 取舍 |
| ---- | ---- |
| 邮箱不可改 | 防止账号被换邮箱顶替 |
| 数据权限自动过滤 | Controller 传 currentUserId，Service 拼 WHERE |
| 角色 DTO 嵌套返回 | 减少前端二次查询 |
| 创建/分配角色合并 | 一次 POST 完成用户初始化 |
| 缓存 + 写后清除 | GetOrCreateAsync + RemoveByPattern |

### 7.2 前端设计模式

| 模式 | 用途 |
| ---- | ---- |
| editingId | 新增和编辑共用一个 Modal |
| deptNameMap | 部门 ID → 名称缓存映射 |
| passwordStrength | 密码强度可视化反馈 |
| auth 字段 | 按钮级权限自动隐藏 |
| customRequest | 头像上传自定义流程 |

### 7.3 个人中心安全细节

- ✅ 头像格式 + 大小校验
- ✅ 改密需要旧密码
- ✅ 改密后重置 `PasswordChangedAt`（重新计算过期）
- ✅ 改密后清 `MustChangePassword` 标记

---

## 下篇预告

下一篇我们看 **角色管理 + 权限管理**：角色 CRUD、菜单树权限分配、5 种数据权限配置、前端 Tree 的 checkStrictly 兼容处理 🛡️

---

## 开源地址

- **GitHub**：https://github.com/qiect/Chet.Admin
- **Gitee**：https://gitee.com/qiect/Chet.Admin

觉得有帮助的话，**点个 Star ⭐** 支持一下吧！你的 Star 是我持续更新的动力～

---

## 互动

你的项目里用户管理的"邮箱不可改"是怎么做的？头像上传走的是 OSS 还是本地？评论区聊聊～👇

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#用户管理` `#个人中心` `#数据权限` `#editingId` `#.NET10` `#Vue3`
