# Chet.Admin 模块详解⑩：文件上传下载全流程 📁

> 《Chet.Admin 全栈实战》系列第 16 篇

---

## 前言

文件上传是后台系统的 **基础设施**。

头像、附件、文档、图片…… 哪哪都需要。

但文件上传的坑可不少：

- ❌ 文件大小限制怎么配？
- ❌ 文件类型怎么校验？
- ❌ 下载时怎么带 Token 鉴权？
- ❌ 前端怎么绕过拦截器拿二进制流？
- ❌ 删除时物理文件和数据库记录怎么同步？

**Chet.Admin** 内置了一套完整的文件管理模块，本地存储 + 鉴权下载，今天来拆解 👇

---

## 整体架构

先看全景图：

<!-- 文件上传架构图 -->
![文件上传架构](/screenshots/file-architecture.svg)

核心链路：

```
上传：前端 → customRequest 带 Token → 校验大小/类型 → GUID 重命名 → 存 uploads → 写数据库
下载：前端 → 带 Token 请求 → 后端读文件 → 返回文件流 → Blob → createObjectURL → <a> 下载
```

涉及的核心文件：

| 层 | 文件 | 职责 |
| ---- | ---- | ---- |
| 控制器 | `FilesController.cs` | 5 个接口 |
| 服务 | `FileService.cs` | 上传/下载/删除逻辑 |
| 实体 | `FileEntity.cs` | 数据模型 |
| 入口 | `Program.cs` | 静态文件中间件 |
| 前端 | `file/index.vue` | 列表 + 上传 + 下载 |
| API | `file.ts` | 请求封装 |

---

## 一、本地存储方案

### 1.1 uploads 目录

Chet.Admin 采用 **本地存储** 方案，文件放在项目根目录的 `uploads` 文件夹下。

```csharp
public class FileService : IFileService
{
    private readonly string _uploadDir;

    public FileService(AppDbContext dbContext, ILogger<FileService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(_uploadDir))
        {
            Directory.CreateDirectory(_uploadDir);
        }
    }
}
```

**构造函数里自动创建目录**，不存在就建，避免首次上传报错。

**为什么选本地存储**？

- ✅ 简单，不需要额外的对象存储服务
- ✅ 适合中小项目，上手快
- ✅ 后续可平滑迁移到 OSS / MinIO

---

### 1.2 静态文件中间件

uploads 目录还要配置 **静态文件中间件**，才能通过 URL 直接访问：

```csharp
// Program.cs
var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});
```

**配置解读**：

- `PhysicalFileProvider`：指定物理目录
- `RequestPath = "/uploads"`：URL 前缀映射

配置后，`http://localhost:5000/uploads/abc.jpg` 就能直接访问图片 🖼️

---

## 二、数据模型：FileEntity

```csharp
public class FileEntity
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;      // 原始文件名
    public string StoredName { get; set; } = string.Empty;   // 存储文件名（GUID）
    public string FilePath { get; set; } = string.Empty;     // 相对路径
    public string ContentType { get; set; } = string.Empty;  // MIME类型
    public long FileSize { get; set; }                        // 文件大小（字节）
    public string? Description { get; set; }                  // 文件描述
    public int? UploaderId { get; set; }                     // 上传者ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**为什么要两个文件名字段**？

- **FileName**：用户上传时的原始名字（展示用）
- **StoredName**：服务器存储的重命名（避免冲突）

比如用户上传了「测试报告.pdf」，服务器存成 `a1b2c3d4...pdf`，互不影响 👍

---

## 三、上传 API

### 3.1 控制器接口

```csharp
[HttpPost("upload")]
[RequestSizeLimit(10 * 1024 * 1024)] // 10MB
public async Task<IActionResult> Upload(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest(ApiResponse.Error("请选择文件"));

    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    int? userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

    var result = await _fileService.UploadAsync(file, userId);
    return Ok(ApiResponse.Ok(result, "文件上传成功"));
}
```

**`[RequestSizeLimit(10 * 1024 * 1024)]`** 限制请求体大小为 10MB，超大会被 **中间件直接拒绝**，连控制器都进不来。

**`IFormFile`** 是 ASP.NET Core 的标准文件接收方式。

---

### 3.2 上传服务逻辑

```csharp
public async Task<FileDto> UploadAsync(IFormFile file, int? uploaderId)
{
    if (file.Length > MaxFileSize)
        throw new BadRequestException("文件大小不能超过10MB");

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!AllowedExtensions.Contains(extension))
        throw new BadRequestException("不支持的文件类型");

    var storedName = $"{Guid.NewGuid():N}{extension}";
    var filePath = Path.Combine(_uploadDir, storedName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var entity = new FileEntity
    {
        FileName = file.FileName,
        StoredName = storedName,
        FilePath = $"uploads/{storedName}",
        ContentType = file.ContentType,
        FileSize = file.Length,
        UploaderId = uploaderId,
    };

    _dbContext.Files.Add(entity);
    await _dbContext.SaveChangesAsync();

    return new FileDto { /* ... 映射字段 */ };
}
```

**完整流程**：

1. ✅ **大小校验**：超过 10MB 抛异常
2. ✅ **类型校验**：扩展名不在白名单内抛异常
3. ✅ **GUID 重命名**：`Guid.NewGuid():N` 生成无连字符的 GUID
4. ✅ **写入磁盘**：`FileCopyToAsync` 流式写入
5. ✅ **写数据库**：记录文件元信息

---

### 3.3 类型白名单

```csharp
private static readonly string[] AllowedExtensions =
    { ".jpg", ".jpeg", ".png", ".gif", ".bmp",          // 图片
      ".pdf", ".doc", ".docx", ".xls", ".xlsx",          // 文档
      ".ppt", ".pptx", ".txt", ".zip", ".rar" };         // 其他

private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
```

**双重校验**：

- 大小：`10MB` 上限
- 类型：`14` 种扩展名白名单

**为什么用白名单而不是黑名单**？白名单更安全，只允许已知安全的类型，防止上传 `.exe`、`.sh` 等危险文件 🔒

---

## 四、下载流程

### 4.1 后端接口

```csharp
[HttpGet("{id}/download")]
public async Task<IActionResult> Download(int id)
{
    var result = await _fileService.DownloadAsync(id);
    if (result == null) return NotFound(ApiResponse.Error("文件不存在"));

    var (data, contentType, fileName) = result.Value;
    return File(data, contentType, fileName);
}
```

返回的是 `File()` 结果，即 **二进制文件流**，不是 JSON。

---

### 4.2 下载服务逻辑

```csharp
public async Task<(byte[] Data, string ContentType, string FileName)?> DownloadAsync(int id)
{
    var entity = await _dbContext.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
    if (entity == null) return null;

    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), entity.FilePath);
    if (!System.IO.File.Exists(fullPath)) return null;

    var data = await System.IO.File.ReadAllBytesAsync(fullPath);
    return (data, entity.ContentType, entity.FileName);
}
```

**流程**：

1. 查数据库拿文件信息
2. 拼接完整路径
3. 检查物理文件是否存在
4. 读取全部字节返回

返回的 `FileName` 是原始文件名，浏览器下载时自动用这个名字 📥

---

### 4.3 前端下载：绕过拦截器

这是文件下载的 **核心难点**。

普通 API 请求返回 JSON，前端拦截器会统一处理 `{ success, data, message }` 格式。

但下载返回的是 **二进制流**，不是 JSON，如果走拦截器会 **解析失败** 💥

解决方案是 `responseReturn: 'body'`：

```typescript
export async function downloadFileApi(id: number) {
  return requestClient.get<Blob>(`/files/${id}/download`, {
    responseType: 'blob',          // 声明返回类型为 Blob
    responseReturn: 'body',        // 👈 直接返回 body，跳过 success 校验
  });
}
```

**两个关键参数**：

- `responseType: 'blob'`：告诉 axios 返回二进制 Blob
- `responseReturn: 'body'`：**跳过拦截器的业务逻辑**，直接返回原始 body

---

### 4.4 前端下载实现

拿到 Blob 后，用 `createObjectURL` + `<a>` 标签触发下载：

```typescript
async function onDownload(row: any) {
  try {
    const blob = await downloadFileApi(row.id);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = row.fileName || `file-${row.id}`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch {
    message.error('下载失败');
  }
}
```

**流程**：

1. 请求接口拿到 Blob
2. `createObjectURL` 生成临时 URL
3. 创建隐藏的 `<a>` 标签
4. 设置 `download` 属性为文件名
5. 模拟点击触发下载
6. 清理 DOM 和 URL

**`URL.revokeObjectURL`** 很重要，释放内存，否则会内存泄漏 ⚠️

---

## 五、删除文件

删除要 **同时删物理文件和数据库记录**：

```csharp
public async Task DeleteAsync(int id)
{
    var entity = await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
    if (entity == null) throw new NotFoundException(nameof(FileEntity), id);

    // 1. 删除物理文件
    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), entity.FilePath);
    if (System.IO.File.Exists(fullPath))
    {
        System.IO.File.Delete(fullPath);
    }

    // 2. 删除数据库记录
    _dbContext.Files.Remove(entity);
    await _dbContext.SaveChangesAsync();
}
```

**先删文件，再删记录**：

- 物理文件删了，即使数据库删除失败也不会有孤儿记录
- 反过来如果先删数据库记录，物理文件就成了 **垃圾文件** 占磁盘

---

## 六、分页查询

```csharp
public async Task<(List<FileDto> items, int total)> GetListAsync(int pageNumber, int pageSize, string? keyword)
{
    var query = _dbContext.Files.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(f => f.FileName.Contains(keyword));
    }

    var total = await query.CountAsync();
    var entities = await query
        .OrderByDescending(f => f.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var items = entities.Select(entity => new FileDto
    {
        Id = entity.Id,
        FileName = entity.FileName,
        FilePath = entity.FilePath,
        ContentType = entity.ContentType,
        FileSize = entity.FileSize,
        UploaderId = entity.UploaderId,
        CreatedAt = entity.CreatedAt,
    }).ToList();

    return (items, total);
}
```

**手动映射 DTO**，没用 AutoMapper，因为字段一一对应，直接写更清晰。

---

## 七、前端：上传组件

### 7.1 customRequest 携带 Token

Ant Design Vue 的 `Upload` 组件默认用 XMLHttpRequest 上传，**不会走 requestClient**，也就 **不会带 Authorization header**。

解决方案是用 `customRequest` 自定义上传：

```typescript
const customUpload: UploadProps['customRequest'] = async (options) => {
  const { file, onSuccess, onError } = options;
  uploading.value = true;
  try {
    await uploadFileApi(file as File);
    onSuccess?.({}, file);
    message.success(`${(file as File).name} 上传成功`);
    gridApi.query();
  } catch (error) {
    onError?.(error as Error);
    message.error(`${(file as File).name} 上传失败`);
  } finally {
    uploading.value = false;
  }
};
```

**`uploadFileApi`** 用 requestClient 发请求，自动带 Token：

```typescript
export async function uploadFileApi(file: File) {
  const formData = new FormData();
  formData.append('file', file);
  return requestClient.post('/files/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
}
```

**FormData** 包装文件，`Content-Type: multipart/form-data` 声明上传类型 ✅

---

### 7.2 模板部分

```vue
<Upload
  v-if="hasAccessByCodes(['system:file:upload'])"
  :custom-request="customUpload"
  :show-upload-list="false"
  :show-button="false"
>
  <Button type="primary" :loading="uploading">
    <Plus class="mr-2 size-4" />上传文件
  </Button>
</Upload>
```

**`show-upload-list="false"`** 隐藏默认的文件列表，因为用 VxeTable 展示。

**权限控制**：只有 `system:file:upload` 权限才显示上传按钮。

---

## 八、前端：列表展示

### 8.1 文件大小格式化

字节对人来说不直观，要格式化成 KB / MB：

```typescript
function formatFileSize(size: number) {
  if (!size) return '-';
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / (1024 * 1024)).toFixed(2)} MB`;
}
```

**三级展示**：B / KB / MB，根据大小自动选择合适的单位 📏

---

### 8.2 表格列

```typescript
const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 80 },
  { field: 'fileName', title: '文件名', minWidth: 200 },
  { field: 'fileSize', title: '大小', width: 120,
    slots: { default: ({ row }) => formatFileSize(row.fileSize) } },
  { field: 'contentType', title: '类型', width: 150 },
  { field: 'createdAt', title: '上传时间', minWidth: 180,
    slots: { default: ({ row }) =>
      row.createdAt ? new Date(row.createdAt).toLocaleString('zh-CN') : '-' } },
  { field: 'operation', title: '操作', width: 180 },
];
```

<!-- 文件列表界面 -->
![文件列表](/screenshots/file.svg)

---

### 8.3 操作按钮

```vue
<template #action="{ row }">
  <VbenTableAction
    :actions="[
      { text: '下载', onClick: () => onDownload(row) },
    ]"
    :dropdown-actions="[
      { text: '删除', auth: 'system:file:delete', danger: true,
        popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } },
    ]"
  />
</template>
```

- **下载**：所有人可用
- **删除**：需要 `system:file:delete` 权限，红色按钮 + 二次确认

---

## 九、控制器全貌

5 个接口，职责清晰：

```csharp
[HttpGet]                  // 分页列表
[HttpPost("upload")]       // 上传
[HttpGet("{id}")]          // 详情
[HttpGet("{id}/download")] // 下载
[HttpDelete("{id}")]        // 删除
```

---

## 十、完整流程图

```
上传：
  前端选文件 → customRequest → requestClient.post（带Token）
    → 后端校验大小（10MB）→ 校验类型（白名单）
    → GUID 重命名 → 写入 uploads 目录 → 写数据库 → 返回文件信息

下载：
  前端点击下载 → requestClient.get（responseType: blob, responseReturn: body）
    → 后端查数据库 → 读物理文件 → 返回 File 流
    → 前端拿 Blob → createObjectURL → <a> download → 触发下载 → revokeObjectURL

删除：
  前端点击删除 → 二次确认 → requestClient.delete
    → 后端删物理文件 → 删数据库记录 → 返回成功
```

---

## 设计亮点总结

| 特性 | 说明 |
| ---- | ---- |
| **本地存储** | uploads 目录，简单易上手 |
| **GUID 重命名** | 避免文件名冲突和中文乱码 |
| **双重校验** | 大小（10MB）+ 类型（白名单）|
| **RequestSizeLimit** | 中间件层拦截超大请求 |
| **customRequest** | 前端自定义上传，携带 Token |
| **responseReturn: body** | 下载绕过拦截器，拿原始 Blob |
| **createObjectURL** | 前端触发文件下载 |
| **revokeObjectURL** | 释放内存，防泄漏 |
| **先删文件再删记录** | 避免孤儿文件 |
| **静态文件中间件** | uploads 目录可 URL 直接访问 |

---

## 小结

文件上传看着简单，但前后端配合的细节不少：

- 🔑 **customRequest** 解决 Upload 组件不带 Token 的问题
- 🔑 **responseReturn: 'body'** 解决下载绕过拦截器的问题
- 🔑 **GUID 重命名** 解决文件名冲突和安全问题
- 🔑 **白名单校验** 比黑名单更安全
- 🔑 **先删文件再删记录** 避免垃圾文件

这套方案适合中小项目，后续可平滑迁移到 OSS / MinIO ⭐

---

> 🔗 **GitHub**：https://github.com/qiect/Chet.Admin
> 🔗 **Gitee**：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

---

**下篇预告**：「Chet.Admin 模块详解⑪：在线用户追踪 + 强制下线 📡」

---

`#ChetAdmin` `#全栈开发` `#.NET10` `#Vue3` `#文件上传` `#Blob下载` `#开源项目`
