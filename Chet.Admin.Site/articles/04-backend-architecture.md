# Chet.Admin 后端架构：Clean Architecture + DDD 落地实践 🏗️

> 《Chet.Admin 全栈实战》系列第 4 篇

---

## 前言

上一篇我们梳理了 Chet.Admin 的目录结构，但可能你还有疑问：

- Clean Architecture 到底"Clean"在哪？
- DDD（领域驱动设计）在代码里是怎么体现的？
- 依赖注入（DI）的生命周期怎么定？
- 泛型仓储 + 工作单元到底解决什么问题？
- 新增一个模块，从 0 到能跑，完整流程是什么？

这篇就**把后端架构彻底讲透**：

- ✅ 分层图解 + 依赖方向
- ✅ DI 约定与生命周期
- ✅ 泛型仓储模式
- ✅ 工作单元模式
- ✅ 新增模块 7 步流程

---

## 一、为什么要分层？

不分层的代码长这样：

```csharp
// 😱 Controller 直接操作 DbContext + 业务逻辑 + 验证 全揉一块
[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    if (string.IsNullOrEmpty(dto.Email)) return BadRequest();
    var user = _dbContext.Users.FirstOrDefault(u => u.Email == dto.Email);
    if (user == null) return Unauthorized();
    // 验证密码
    if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        return Unauthorized();
    // 生成 Token
    var token = GenerateJwt(user);
    // 更新登录时间
    user.LastLoginAt = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();
    return Ok(token);
}
```

**问题**：

- ❌ Controller 又厚又乱，难维护
- ❌ 业务逻辑没法复用
- ❌ 没法单元测试（DbContext 写死了）
- ❌ 改数据库要动 Controller

**分层后**：

```csharp
// ✅ Controller 只做参数接收和响应
[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto)
{
    var token = await _authService.LoginAsync(dto);
    return Ok(ApiResponse.Ok(token, "Login successful"));
}

// ✅ Service 专注业务
public async Task<JwtTokenDto> LoginAsync(LoginDto dto)
{
    var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
    if (user == null || !_passwordService.Verify(dto.Password, user.PasswordHash))
        throw new UnauthorizedAccessException();
    // 生成 Token...
}

// ✅ Repository 专注数据访问
public async Task<UserEntity?> GetByEmailAsync(string email)
    => await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
```

**分层 = 关注点分离 = 可维护 + 可测试 + 可替换**。

---

## 二、四层架构总览

Chet.Admin 后端采用 **Clean Architecture**，分为四层：

```
┌──────────────────────────────────────────────────────┐
│                  表示层（API Layer）                  │
│  Controllers / Middleware / Filters / Configurations │
│  职责：HTTP 路由、参数校验、响应格式化                 │
└──────────────────────┬───────────────────────────────┘
                       │ 调用
┌──────────────────────▼───────────────────────────────┐
│                  应用层（Application Layer）          │
│  Services / DTOs / Mapping / Validators              │
│  职责：业务逻辑编排、对象映射、参数校验               │
└──────────────────────┬───────────────────────────────┘
                       │ 依赖抽象
┌──────────────────────▼───────────────────────────────┐
│                核心层（Core Layer）⭐                │
│  Domain（实体）/ Contracts（接口）/ Shared（工具）   │
│  职责：定义领域模型和契约，不依赖任何其他层           │
└──────────────────────▲───────────────────────────────┘
                       │ 实现接口
┌──────────────────────┴───────────────────────────────┐
│              基础设施层（Infrastructure Layer）       │
│  Data（EF Core）/ Caching / Configuration / Logging │
│  职责：技术实现，实现 Core 定义的接口                │
└──────────────────────────────────────────────────────┘
```

### 依赖方向原则

```
Api ──▶ Application ──▶ Core ◀── Infrastructure
```

- **依赖方向始终向内**指向 Core
- **Core 零依赖**：不引用任何其他层
- **依赖反转**：Infrastructure 实现 Core 的接口，但 Core 不知道 Infrastructure 的存在

> 💡 这就是 Clean Architecture 的精髓：**核心业务逻辑不依赖技术细节**。

---

## 三、各层详解

### 1️⃣ 表示层（Chet.Admin.Api）

**职责**：HTTP 进出

- 接收 HTTP 请求
- 模型绑定 + 参数校验（FluentValidation）
- 调用 Application 层的 Service
- 统一响应格式（`ApiResponse`）
- 异常处理（`ApiExceptionFilter`）

**入口文件 `Program.cs`**：

```csharp
// ============================================
// 第一阶段：初始化和前置检查
// ============================================
Log.Information("Starting application...");
var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog();

var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
builder.Services.AddSingleton(appSettings!);

// ============================================
// 第二阶段：服务注册（依赖注入配置）
// ============================================
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // UTC 时间统一序列化带 Z 后缀
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
});

builder.Services.ConfigureApiVersioning();    // API 版本
builder.Services.ConfigureSwagger();         // Swagger
builder.Services.ConfigureDatabase(builder.Configuration);  // EF Core
builder.Services.ConfigureRedis(appSettings);  // Redis
builder.Services.AddAutoMapper(typeof(MappingProfile)); // AutoMapper
builder.Services.ConfigureRepositories();    // 仓储注册
builder.Services.ConfigureServices();        // 服务注册
builder.Services.ConfigureFluentValidation(); // 参数校验
builder.Services.ConfigureJwt(appSettings);  // JWT
builder.Services.ConfigureCors(builder.Configuration); // CORS
builder.Services.AddMemoryCache();           // 内存缓存（验证码）

// ============================================
// 第三阶段：构建应用
// ============================================
var app = builder.Build();

// 第四阶段：数据库初始化（自动迁移 + 种子）
await app.InitializeDatabaseAsync();

// 第五阶段：中间件管道
app.ConfigureExceptionHandling();        // 异常处理
app.UseLogContext();                     // 日志上下文
app.UseCors("DefaultPolicy");            // CORS
app.UseRateLimiting();                   // 限流
app.ConfigureSwaggerUI();                // Swagger
app.ConfigureAuthMiddleware(appSettings); // JWT 认证
app.UseMiddleware<AuditLogMiddleware>();  // 审计日志
app.UseMiddleware<OnlineUserTrackingMiddleware>(); // 在线用户
app.UseStaticFiles(...);                  // 静态文件
app.MapControllers();                     // 路由映射

// 第六阶段：启动
app.Run();
```

> 💡 `Program.cs` 是整个应用的**配置中心**，6 个阶段清晰可见。

### Controller 长什么样

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto);
        return Ok(ApiResponse.Ok(token, "Login successful"));
    }
}
```

**特点**：

- 只做参数接收 + 调用 Service + 包装响应
- 不直接操作 DbContext
- 不写业务逻辑（if/else 业务判断都在 Service）

---

### 2️⃣ 应用层（Chet.Admin.Application）

**职责**：业务逻辑

- 实现 Service（业务用例）
- 定义 DTO（数据传输对象）
- 用 AutoMapper 做实体 ↔ DTO 转换
- 用 FluentValidation 做参数校验

**Service 示例**：

```csharp
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;

    public async Task<JwtTokenDto> LoginAsync(LoginDto dto)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);

        if (user == null || !_passwordService.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // 事务保护登录操作
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CommitAsync();

            return new JwtTokenDto { AccessToken = accessToken, RefreshToken = refreshToken };
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
```

**关键点**：

- Service 依赖的是 **接口**（`IUnitOfWork`、`IJwtService`），不是具体实现
- 业务异常通过 `throw` 抛出，由全局异常过滤器处理
- 事务边界由 Service 控制（`BeginTransactionAsync` / `CommitAsync`）

---

### 3️⃣ 核心层（Chet.Admin.Core）⭐ 最重要

**职责**：定义领域模型和契约

Core 层包含三个子项目：

| 子项目 | 内容 | 作用 |
| ---- | ---- | ---- |
| `Domain` | 实体类（`UserEntity` 等） | 纯领域模型 |
| `Contracts` | 接口（`IRepository`、`IUserService`） | 抽象契约 |
| `Shared` | 工具类（`ApiResponse`、异常） | 跨层共享 |

#### 实体基类

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

所有实体继承它，统一有主键 + 时间戳。

#### 泛型仓储接口

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(int id);
    Task<int> SaveChangesAsync();
    Task<PagedResult<T>> GetPagedAsync(PagedRequest request);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}
```

> 💡 这就是**依赖反转**：Service 依赖 `IRepository<T>`，不依赖 EF Core。

#### 工作单元接口

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    DbContext DbContext { get; }
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

> 💡 `IUnitOfWork` 协调多个 Repository，确保它们共享同一个事务。

#### 共享响应格式

```csharp
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public object? Data { get; set; }
    public int StatusCode { get; set; }
}
```

所有 API 返回统一格式，前端好处理。

#### 业务异常

```csharp
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

Service 抛业务异常，全局过滤器转成 HTTP 响应。

---

### 4️⃣ 基础设施层（Chet.Admin.Infrastructure）

**职责**：技术实现

- 用 EF Core 实现 Core 的仓储接口
- 用 Redis 实现缓存接口
- 配置 Serilog 日志
- 读取 appsettings.json

#### 数据库上下文

```csharp
public class AppDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<RoleEntity> Roles { get; set; }
    public DbSet<MenuEntity> Menus { get; set; }
    // ... 其他表

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 自动应用所有 IEntityTypeConfiguration<T> 配置
        modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自动更新 CreatedAt / UpdatedAt（UTC）
        var entities = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity
                && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entities)
        {
            var entity = (BaseEntity)entry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

**亮点**：

- `ApplyConfigurationsFromAssembly` 自动加载所有 EF 配置，新增表不用改 DbContext
- 重写 `SaveChangesAsync` 自动维护时间戳

#### 泛型仓储实现

```csharp
public class EfCoreRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _dbContext;
    protected readonly DbSet<T> _dbSet;

    public EfCoreRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.AsNoTracking().ToListAsync();  // 只读查询不跟踪

    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Delete(T entity)
        => _dbSet.Remove(entity);

    public async Task<PagedResult<T>> GetPagedAsync(PagedRequest request)
    {
        request.Normalize();
        var query = _dbSet.AsNoTracking();
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();
        return new PagedResult<T>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
```

**设计要点**：

- ✅ `AsNoTracking()` 只读查询提升性能
- ✅ Add/Update/Delete 不立即保存，由 UnitOfWork 统一提交
- ✅ 泛型支持，任意实体都能用

#### 工作单元实现

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;
    private IUserRepository? _users;  // 懒加载

    public UnitOfWork(AppDbContext context) => _context = context;

    // 懒加载：首次访问时创建
    public IUserRepository Users => _users ??= new UserRepository(_context);

    public DbContext DbContext => _context;

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return _transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _disposed = true;
        }
    }
}
```

**亮点**：

- ✅ 懒加载 Repository（`??=`），按需创建
- ✅ `CommitAsync` 内部先 `SaveChanges` 再 `Commit`，失败自动回滚
- ✅ 实现 `IDisposable`，随请求释放

---

## 四、DI 约定与生命周期

依赖注入在 `Configurations/` 下分两个文件管理：

### 仓储注册（RepositoryConfiguration.cs）

```csharp
public static class RepositoryConfiguration
{
    public static void ConfigureRepositories(this IServiceCollection services)
    {
        // 泛型仓储
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        // 具体仓储
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDictionaryRepository, DictionaryRepository>();
    }
}
```

### 服务注册（ServiceConfiguration.cs）

```csharp
public static class ServiceConfiguration
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, Data.UnitOfWork>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        // ... 13 个服务
        services.AddSingleton<IOnlineUserService, OnlineUserService>();  // 在线用户（单例）
        services.AddSingleton<CaptchaService>();  // 验证码（单例）
    }
}
```

### 生命周期约定

| 生命周期 | 用途 | 示例 | 原因 |
| ---- | ---- | ---- | ---- |
| **Scoped** | 大部分服务 | `IUserService`、`IRepository` | 每请求一个实例，与 DbContext 同步 |
| **Singleton** | 全局状态 | `IOnlineUserService`、`CaptchaService` | 跨请求共享内存数据 |
| **Transient** | 轻量无状态 | （本项目未用） | 每次注入新实例 |

**为什么 DbContext 是 Scoped？**

EF Core 的 DbContext **不是线程安全**的，必须每个请求一个实例。所有依赖它的 Repository、UnitOfWork 都必须是 Scoped。

**为什么 OnlineUserService 是 Singleton？**

它维护一个**全局在线用户列表**（`ConcurrentDictionary`），跨请求共享。如果是 Scoped，每次请求都新建一个，就丢失数据了。

> ⚠️ **Singleton 内不能注入 Scoped！** 这是 DI 容器的**捕获陷阱**。如果 Singleton 需要用 Scoped 服务，要用 `IServiceScopeFactory` 手动创建 Scope。

---

## 五、泛型仓储模式

### 为什么用泛型仓储？

**不用泛型**：每个实体都要写一遍 CRUD

```csharp
// 😱 重复代码
public class UserRepository
{
    public Task<User?> GetByIdAsync(int id) { ... }
    public Task<IEnumerable<User>> GetAllAsync() { ... }
    public Task AddAsync(User entity) { ... }
    // ...
}

public class RoleRepository  // 又抄一遍
{
    public Task<Role?> GetByIdAsync(int id) { ... }
    // ...
}
```

**用泛型**：一份代码所有实体复用

```csharp
// ✅ 一份代码搞定
public class EfCoreRepository<T> : IRepository<T> where T : class
{
    // 通用 CRUD 实现
}

// 具体仓储只写特殊查询
public class UserRepository : EfCoreRepository<UserEntity>, IUserRepository
{
    public Task<UserEntity?> GetByEmailAsync(string email)
        => _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> IsEmailUniqueAsync(string email)
        => !await _dbSet.AnyAsync(u => u.Email == email);
}
```

### 继承体系

```
IRepository<T>              （接口 - 定义契约）
    └── EfCoreRepository<T> （抽象实现 - 通用 CRUD）
            └── UserRepository  （具体实现 - 用户特定查询）
```

### 何时用泛型？何时写具体仓储？

| 场景 | 选择 |
| ---- | ---- |
| 标准 CRUD（增删改查） | 直接用 `IRepository<T>` |
| 需要特殊查询（如按邮箱查） | 写具体仓储接口 + 实现 |
| 复杂查询（多表关联） | 在具体仓储里加方法 |

---

## 六、工作单元模式

### 解决什么问题？

**场景**：转账操作要扣减 A 账户 + 增加 B 账户，必须**原子性**（要么都成功，要么都失败）。

**不用工作单元**：

```csharp
// 😱 两次 SaveChanges，中间崩了就数据不一致
await _userRepository.UpdateAsync(fromUser);
await _dbContext.SaveChangesAsync();  // 第一次保存
// 💥 这里崩了
await _userRepository.UpdateAsync(toUser);
await _dbContext.SaveChangesAsync();  // 第二次保存（不会执行）
```

**用工作单元**：

```csharp
// ✅ 一次事务，要么都成功要么都失败
using var transaction = await _unitOfWork.BeginTransactionAsync();
try
{
    _unitOfWork.Users.Update(fromUser);
    _unitOfWork.Users.Update(toUser);
    await _unitOfWork.CommitAsync();  // 统一提交
}
catch
{
    await _unitOfWork.RollbackAsync();  // 统一回滚
    throw;
}
```

### UnitOfWork 的核心价值

1. **事务一致性**：多个 Repository 操作在同一事务
2. **延迟持久化**：Add/Update/Delete 不立即写库，统一 Commit
3. **共享 DbContext**：所有 Repository 用同一个上下文

---

## 七、中间件管道

请求处理顺序（`Program.cs`）：

```csharp
app.ConfigureExceptionHandling();                    // 1. 异常处理（最外层）
app.UseLogContext();                                  // 2. 日志上下文
app.UseCors("DefaultPolicy");                         // 3. 跨域
app.UseRateLimiting();                                // 4. 限流
app.ConfigureSwaggerUI();                             // 5. Swagger
app.ConfigureAuthMiddleware(appSettings);             // 6. JWT 认证
app.UseMiddleware<AuditLogMiddleware>();               // 7. 审计日志
app.UseMiddleware<OnlineUserTrackingMiddleware>();    // 8. 在线用户追踪
app.UseStaticFiles(...);                               // 9. 静态文件
app.MapControllers();                                  // 10. 路由映射
```

**顺序很重要**：

- 异常处理必须在最外层（捕获所有异常）
- 认证必须在授权前
- 审计日志在认证后（需要知道是谁在操作）

---

## 八、新增模块完整流程（7 步）

假设要新增一个 **文章管理** 模块：

### Step 1：创建实体

`Core/Domain/Article/ArticleEntity.cs`

```csharp
public class ArticleEntity : BaseEntity
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int AuthorId { get; set; }
}
```

### Step 2：创建仓储接口（如果有特殊查询）

`Core/Contracts/Article/IArticleRepository.cs`

```csharp
public interface IArticleRepository : IRepository<ArticleEntity>
{
    Task<IEnumerable<ArticleEntity>> GetByAuthorAsync(int authorId);
}
```

`Core/Contracts/Article/IArticleService.cs`

```csharp
public interface IArticleService
{
    Task<ArticleDto> GetByIdAsync(int id);
    Task<PagedResult<ArticleDto>> GetPagedAsync(PagedRequest request);
    Task CreateAsync(ArticleCreateDto dto);
    Task UpdateAsync(int id, ArticleUpdateDto dto);
    Task DeleteAsync(int id);
}
```

### Step 3：创建 EF 配置

`Infrastructure/Data/Article/ArticleConfig.cs`

```csharp
public class ArticleConfig : IEntityTypeConfiguration<ArticleEntity>
{
    public void Configure(EntityTypeBuilder<ArticleEntity> builder)
    {
        builder.ToTable("Articles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Content).HasColumnType("TEXT");
    }
}
```

### Step 4：创建仓储实现

`Infrastructure/Data/Article/ArticleRepository.cs`

```csharp
public class ArticleRepository : EfCoreRepository<ArticleEntity>, IArticleRepository
{
    public ArticleRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ArticleEntity>> GetByAuthorAsync(int authorId)
        => await _dbSet.Where(a => a.AuthorId == authorId).ToListAsync();
}
```

### Step 5：创建 DTO 和映射

`Application/DTOs/Article/ArticleDto.cs`

```csharp
public class ArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ArticleCreateDto
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}
```

### Step 6：创建 Service 实现

`Application/Services/Article/ArticleService.cs`

```csharp
public class ArticleService : IArticleService
{
    private readonly IRepository<ArticleEntity> _repository;
    private readonly IMapper _mapper;

    public async Task<ArticleDto> GetByIdAsync(int id)
    {
        var article = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException("Article not found");
        return _mapper.Map<ArticleDto>(article);
    }

    public async Task CreateAsync(ArticleCreateDto dto)
    {
        var article = _mapper.Map<ArticleEntity>(dto);
        await _repository.AddAsync(article);
        await _repository.SaveChangesAsync();
    }
}
```

### Step 7：注册 + 创建控制器

在 `RepositoryConfiguration.cs` 加：

```csharp
services.AddScoped<IArticleRepository, ArticleRepository>();
```

在 `ServiceConfiguration.cs` 加：

```csharp
services.AddScoped<IArticleService, ArticleService>();
```

在 `AppDbContext.cs` 加：

```csharp
public DbSet<ArticleEntity> Articles { get; set; }
```

创建 `Api/Controllers/ArticlesController.cs`：

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _service;

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
        => Ok(ApiResponse.Ok(await _service.GetByIdAsync(id)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ArticleCreateDto dto)
    {
        await _service.CreateAsync(dto);
        return Ok(ApiResponse.Ok(null, "Created", 201));
    }
}
```

最后生成迁移：

```bash
dotnet ef migrations add AddArticle --project Chet.Admin.Infrastructure --startup-project Chet.Admin.Api
dotnet ef database update --project Chet.Admin.Infrastructure --startup-project Chet.Admin.Api
```

> 💡 因为 `ApplyConfigurationsFromAssembly` 会自动加载 `ArticleConfig`，不用手动注册。

---

## 九、架构优势总结

| 优势 | 体现 |
| ---- | ---- |
| **关注点分离** | Controller 不管业务，Service 不管数据访问 |
| **可测试性** | Service 依赖接口，可 Mock 仓储做单元测试 |
| **可替换性** | 换数据库只改 Infrastructure，业务代码不动 |
| **复用性** | 泛型仓储一份代码所有实体用 |
| **事务安全** | 工作单元保证多操作原子性 |
| **扩展性** | 新增模块 7 步搞定，不动现有代码 |

---

## 十、与 DDD 的对应关系

| DDD 概念 | Chet.Admin 实现 |
| ---- | ---- |
| 实体（Entity） | `Domain/User/UserEntity.cs` |
| 聚合根（Aggregate Root） | `UserEntity`（聚合 User/Role 关联） |
| 仓储（Repository） | `IRepository<T>` + `EfCoreRepository<T>` |
| 工作单元（Unit of Work） | `IUnitOfWork` + `UnitOfWork` |
| 领域服务（Domain Service） | `AuthService`、`UserService` |
| 值对象（Value Object） | DTO（`UserDto`、`LoginDto`） |
| 领域事件（Domain Event） | （暂未实现，可扩展） |

> 💡 Chet.Admin 是**务实版 DDD**，没有完整 DDD 那么重（如没有聚合根边界、领域事件），但保留了核心思想。

---

## 十一、下一步

架构搞懂了，下一篇深入**安全机制**：

- 📖 第 5 篇：**JWT 认证与安全**，双令牌 + 刷新 + 登录锁定

---

## 互动

你们团队用 Clean Architecture 吗？还是传统的三层架构？评论区聊聊～

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#CleanArchitecture` `#DDD` `#泛型仓储` `#工作单元` `#.NET10`
