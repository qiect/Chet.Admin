# Chet.Admin 工程实践：后端 UTC + 前端北京时间的统一方案 🌐

> 《Chet.Admin 全栈实战》系列第 18 篇

---

## 前言

做全栈项目，**时间时区**是个被严重低估的坑。

- ❌ 后端有时写 `DateTime.Now`，有时写 `DateTime.UtcNow`
- ❌ 数据库存的是"裸时间"，看不出时区
- ❌ 前端 `new Date()` 一解析，差 8 小时
- ❌ 切换数据库类型后，时间显示全乱

**Chet.Admin** 用一套很轻的方案解决了这个问题：**后端统一 UTC 存储 + 自定义 JsonConverter 强制带 `Z` 后缀 + 前端按本地时区展示**。

不靠魔法，只靠纪律。👀

---

## 一、问题背景：为什么会差 8 小时

### 1.1 DateTime.Now vs DateTime.UtcNow 混用

C# 里拿时间有两种方式：

```csharp
var local = DateTime.Now;        // 本地时区，比如北京时间 2026-07-10 15:30:00
var utc   = DateTime.UtcNow;     // UTC 时间 2026-07-10 07:30:00
```

如果代码里**两种混用**，数据库里就会出现：

- 一行记录的 `CreatedAt` 是北京时间
- 另一行的 `UpdatedAt` 是 UTC 时间
- 字段长得一样，根本分不出来 👀

查询、排序、跨时区展示全部翻车。

### 1.2 SQLite 存 TEXT 没有时区

Chet.Admin 默认用 SQLite，DateTime 字段以 **TEXT** 形式存储：

```
2026-07-10 15:30:00
```

注意，**没有 `Z`、没有 `+08:00`**，就是个"裸字符串"。

EF Core 读回来时，`DateTime.Kind` 是 **`Unspecified`** —— 既不是 Local，也不是 UTC，框架不知道该按哪个时区处理。

### 1.3 System.Text.Json 的默认行为

`System.Text.Json` 默认序列化 DateTime 时，**只在 `Kind != Unspecified` 时才带时区标识**。

也就是说，从 SQLite 读出的 `Kind=Unspecified` 时间，序列化后会变成：

```json
"createdAt": "2026-07-10T15:30:00"
```

**没有 `Z` 后缀！**

前端拿到这个字符串，`new Date("2026-07-10T15:30:00")` 会按**浏览器本地时区**解析，结果不可预测，跨时区用户看到的还不一样。

---

## 二、解决方案：四步统一

Chet.Admin 的时间方案就四步：

1. **后端只写 UTC**：所有写入数据库的时间统一用 `DateTime.UtcNow`
2. **自定义 JsonConverter**：序列化时强制带 `Z` 后缀
3. **Program.cs 注册 Converter**：全局生效
4. **前端用 `new Date().toLocaleString()` 自动转本地时区**

下面逐步拆解。

---

## 三、自定义 UtcDateTimeJsonConverter

核心代码位于 `Chet.Admin.Core/Chet.Admin.Shared/Api/UtcDateTimeJsonConverter.cs`：

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chet.Admin.Shared.Api;

/// <summary>
/// DateTime JSON 转换器：统一把 DateTime 当作 UTC 输出，确保序列化结果带 "Z" 后缀。
/// </summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        // 输入带 Z 或时区偏移的按 UTC 解析；裸时间字符串视为 UTC
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // 统一输出为 UTC 带 Z 后缀
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        writer.WriteStringValue(utc);
    }
}
```

### 3.1 关键点：Write 方法

序列化时，无论 `DateTime.Kind` 是什么：

- `Utc` → 直接输出，自动带 `Z`
- `Local` → `ToUniversalTime()` 转成 UTC 再输出
- **`Unspecified` → `SpecifyKind` 标成 UTC**，再输出

最终前端拿到的 JSON 永远是这种格式：

```json
"createdAt": "2026-07-10T07:30:00Z",
"updatedAt": "2026-07-10T07:30:00Z"
```

**带 `Z` 后缀**，明确告诉前端："我是 UTC 时间"。

### 3.2 关键点：Read 方法

反序列化时（前端传时间到后端），统一保证 `Kind=Utc`：

- 带 `Z` 或偏移量的 → 按 UTC 解析
- 裸字符串 → 当作 UTC 处理

这样后端代码里拿到的 `DateTime` 永远是 `Kind=Utc`，不会再出现 `Unspecified` 这种"四不像"。

### 3.3 为什么放在 Shared 层

`UtcDateTimeJsonConverter` 位于 **`Chet.Admin.Shared`** 层：

- **Shared 层零依赖**，任何层都能引用
- Api 层（注册 Converter）和 Application 层（DTO 序列化）都能用到
- 符合 Clean Architecture **依赖方向向内**的原则

<!-- UtcDateTimeJsonConverter 工作流程图 -->
![UtcDateTimeJsonConverter 工作流程](/screenshots/utc-converter.svg)

---

## 四、Program.cs 注册 Converter

光写 Converter 没用，还得**注册到全局 JSON 选项**。在 `Program.cs` 中：

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // SQLite 存储的 DateTime 读回时 Kind=Unspecified，System.Text.Json 默认序列化时不带 Z 后缀
    // 这里统一指定 Kind=Utc，使输出带 Z，前端 new Date() 可正确按 UTC 解析并转本地时区显示
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
});
```

**一行 `Converters.Add`**，全局所有 DateTime 字段都走这个 Converter。

### 4.1 为什么不用 JsonSerializerDefaults

`System.Text.Json` 提供了 `JsonSerializerDefaults.Web`，但**它不会处理 `Unspecified` 类型**，对 SQLite 场景无效。

自定义 Converter 是最干净、最可控的方式。

### 4.2 为什么不用 Newtonsoft.Json

`Newtonsoft.Json` 自带的 `DateFormatHandling.UtcDateTime` 能解决部分问题，但：

- 多引一个包，依赖更重
- ASP.NET Core 8/10 默认用 `System.Text.Json`，没必要换
- 自己写 Converter **代码不到 30 行**，维护成本几乎为 0

---

## 五、AppDbContext 统一 UtcNow 写入

Converter 解决了"读出来怎么显示"，那"写进去用什么时间"呢？

答案是：**在 `AppDbContext.SaveChangesAsync` 中统一处理**。

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // 自动设置创建和更新时间（统一用 UTC 存储，前端按本地时区显示）
    var entities = ChangeTracker.Entries()
        .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

    foreach (var entityEntry in entities)
    {
        var entity = (BaseEntity)entityEntry.Entity;
        entity.UpdatedAt = DateTime.UtcNow;

        if (entityEntry.State == EntityState.Added)
        {
            entity.CreatedAt = DateTime.UtcNow;
        }
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

### 5.1 设计要点

- **重写 `SaveChangesAsync`**：拦截所有写操作
- **基于 `ChangeTracker`**：只处理新增/修改的实体
- **统一 `DateTime.UtcNow`**：所有时间字段一律 UTC
- **基于 `BaseEntity`**：约定所有领域实体都继承 `BaseEntity`，统一有 `CreatedAt` / `UpdatedAt`

### 5.2 为什么不在业务代码里写

如果每个 Service 都手动 `entity.CreatedAt = DateTime.UtcNow`：

- 容易**忘写**，字段为 null
- 容易**写错**，混入 `DateTime.Now`
- 代码冗余

放在 DbContext 里，**业务代码完全不用关心时间字段**，专注业务逻辑。

### 5.3 业务代码里的时间使用

业务代码中所有时间相关操作也统一用 `UtcNow`。比如 `JwtService` 里：

```csharp
var token = new JwtSecurityToken(
    issuer: jwtSettings.Issuer,
    audience: jwtSettings.Audience,
    claims: claims,
    notBefore: DateTime.UtcNow,  // UTC
    expires: DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes),  // UTC
    signingCredentials: creds);

// 校验 Refresh Token 是否过期
if (user.RefreshTokenExpiryTime < DateTime.UtcNow)  // UTC
{
    throw new SecurityTokenException("Refresh token expired");
}
```

**全链路 UTC**，前后比较不会出错。

---

## 六、前端：自动转本地时区

前端这边**几乎不用做任何特殊处理**。

因为后端返回的 JSON 都带 `Z` 后缀，前端 `new Date("2026-07-10T07:30:00Z")` 会自动：

1. 按 UTC 解析
2. 转换为浏览器本地时区的时间对象

显示时调用 `toLocaleString`：

```typescript
const date = new Date('2026-07-10T07:30:00Z');

// 北京时间（UTC+8）
date.toLocaleString('zh-CN');  // "2026/7/10 15:30:00"

// 美东时间（UTC-4）
date.toLocaleString('en-US', { timeZone: 'America/New_York' });  // "7/10/2026, 3:30:00 AM"
```

### 6.1 封装一个 formatDateTime

实际项目里通常会封装一个工具函数：

```typescript
// src/utils/date.ts
export function formatDateTime(value: string | Date): string {
  if (!value) return '';
  const date = typeof value === 'string' ? new Date(value) : value;
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  });
}

export function formatDate(value: string | Date): string {
  if (!value) return '';
  const date = typeof value === 'string' ? new Date(value) : value;
  return date.toLocaleDateString('zh-CN');
}
```

在表格里用：

```vue
<vxe-column field="createdAt" title="创建时间">
  <template #default="{ row }">
    {{ formatDateTime(row.createdAt) }}
  </template>
</vxe-column>
```

### 6.2 关键优势：跨时区用户

这套方案天然支持**多时区用户**：

- 北京用户看到 `15:30`
- 纽约用户看到 `03:30`
- 伦敦用户看到 `07:30`

**后端只存一份 UTC，前端各自展示**，不需要根据用户时区动态计算。

<!-- 前端时间显示对比图 -->
![前端时间显示](/screenshots/frontend-time.svg)

---

## 七、跨数据库兼容性分析

Chet.Admin 支持 **SQLite / MySQL / PostgreSQL** 三种数据库，时间方案是否兼容？

### 7.1 SQLite

- 存储类型：**TEXT**
- 存储格式：`2026-07-10 07:30:00.0000000`（无时区）
- EF Core 读回：`Kind=Unspecified`
- **方案适用**：✅ Converter 处理 Unspecified，完美

### 7.2 MySQL

- 推荐列类型：**`datetime`**（不带时区）
- 存储格式：`2026-07-10 07:30:00`
- EF Core 读回：`Kind=Unspecified`
- **方案适用**：✅ 同 SQLite

> ⚠️ 注意：MySQL 还有 `timestamp` 类型，会自动转 UTC 存储并按当前会话时区读取。**不建议用**，会和我们的方案冲突，统一用 `datetime` 最简单。

### 7.3 PostgreSQL

- 推荐列类型：**`timestamptz`**（带时区）
- 存储格式：内部统一存 UTC，读取时按会话时区返回
- EF Core 读回：`Kind=Utc` ✅
- **方案适用**：✅ Converter 对 `Utc` 类型直接输出，带 `Z`

> 💡 PostgreSQL 的 `timestamptz` 是最规范的选择，存进去自动转 UTC，读出来 `Kind=Utc`，**和方案完全契合**。

### 7.4 对比表

| 数据库 | 列类型 | 存储时区 | 读回 Kind | 是否兼容方案 |
| ------ | ------ | -------- | ---------- | ------------ |
| SQLite | TEXT | 无 | Unspecified | ✅ |
| MySQL | `datetime` | 无 | Unspecified | ✅ |
| MySQL | `timestamp` | 自动 UTC | Local | ⚠️ 不推荐 |
| PostgreSQL | `timestamptz` | UTC | Utc | ✅ |
| PostgreSQL | `timestamp` | 无 | Unspecified | ✅ |

**结论**：本方案对三种数据库完全兼容，迁移时**无需改任何业务代码**，只改连接字符串和 EF Core 包。

---

## 八、迁移数据库示例

从 SQLite 切到 PostgreSQL，改动很小：

### 8.1 改 csproj 引用

```xml
<!-- 原来 -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />

<!-- 改成 -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
```

### 8.2 改连接字符串

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=chet_admin;Username=postgres;Password=yourpassword"
  }
}
```

### 8.3 改 UseNpgsql

```csharp
// DatabaseConfiguration.cs
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### 8.4 时间字段无需任何改动

- `BaseEntity.CreatedAt` / `UpdatedAt` 仍然是 `DateTime`
- `AppDbContext.SaveChangesAsync` 仍然写 `DateTime.UtcNow`
- `UtcDateTimeJsonConverter` 仍然有效
- 前端代码**一行不用改**

这就是统一方案的威力 💪

---

## 九、常见问题

### Q1: 为什么不存 DateTimeOffset

`DateTimeOffset` 自带时区信息，看起来更"正确"。但：

- EF Core 对 `DateTimeOffset` 在 SQLite 上支持一般
- 老项目迁移成本高
- 业务代码用 `DateTime` 更顺手
- 我们用 Converter 已经解决了"无时区标识"问题

够用就行，不为了"政治正确"过度设计。

### Q2: JWT 里的 exp 字段怎么办

JWT 规范规定 `exp` 必须是 **UTC 秒数**，和我们的方案天然契合。

`JwtSecurityToken` 在序列化时会自动转 UTC，不用我们管。

### Q3: 前端传时间给后端怎么处理

比如查询条件 "2026-07-10 到 2026-07-11 创建的用户"：

- 前端传：`?startDate=2026-07-10T00:00:00&endDate=2026-07-11T00:00:00`（无 Z）
- Converter Read 时会把它当作 UTC 处理
- 如果前端想要"北京时间 0 点"作为查询条件，应该在前端先转 UTC 再传

```typescript
// 北京时间 2026-07-10 00:00:00 转 UTC
const startDate = new Date('2026-07-10T00:00:00+08:00').toISOString();
// 结果：2026-07-09T16:00:00.000Z
```

### Q4: 已经混用了 Now 和 UtcNow 怎么补救

如果你接手的老项目代码里混用了两种：

1. **先统一成 UtcNow**：全局替换 `DateTime.Now` → `DateTime.UtcNow`（注意排除日志类等需要本地时间的场景）
2. **加 Converter**：参考本文实现
3. **数据迁移**：把数据库里 `Kind=Unspecified` 的"裸时间"统一加 8 小时（如果是北京时间存的）

---

## 十、方案总结

| 环节 | 做法 | 文件 |
| ---- | ---- | ---- |
| 写入 | `DateTime.UtcNow` | `AppDbContext.SaveChangesAsync` |
| 读取 | EF Core 读回（可能 Unspecified） | - |
| 序列化 | Converter 强制带 `Z` | `UtcDateTimeJsonConverter` |
| 注册 | 全局 JsonOptions | `Program.cs` |
| 前端解析 | `new Date(str)` 自动按 UTC 解析 | 前端代码 |
| 前端显示 | `toLocaleString('zh-CN')` 转本地时区 | `formatDateTime` 工具函数 |

**核心代码不到 50 行**，解决了：

- ✅ 后端时区混乱
- ✅ 跨时区用户显示
- ✅ 跨数据库兼容
- ✅ 序列化格式统一

---

## 下篇预告

下一篇我们聊**项目重命名**：fork 二开后，如何一键把 `Chet.Admin` 改成你自己的品牌名。

> 📌 「Chet.Admin 实用技巧：一键把项目重命名成你的品牌 ✏️」

---

## 开源地址

- **GitHub**：https://github.com/qiect/Chet.Admin
- **Gitee**：https://gitee.com/qiect/Chet.Admin

觉得有帮助的话，**点个 Star ⭐** 支持一下吧！你的 Star 是我持续更新的动力～

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#全栈开发` `#.NET10` `#Vue3` `#时区处理` `#SQLite` `#PostgreSQL`
