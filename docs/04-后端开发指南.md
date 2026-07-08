# 后端开发指南

## 1. 概述

后端解决方案位于 `Chet.Admin.Api/`，采用 Clean Architecture + DDD 分层架构。本指南介绍分层职责、依赖注入约定、以及新增业务模块的标准流程。

## 2. 分层与职责

| 层 | 项目 | 职责 |
| ---- | ---- | ---- |
| 表示层 | `Chet.Admin.Api` | 控制器、中间件、过滤器、DI 注册、启动配置 |
| 应用层 | `Chet.Admin.Services` / `Chet.Admin.Mapping` | 业务逻辑实现、对象映射 |
| 核心层 | `Chet.Admin.Domain` / `Contracts` / `Shared` | 领域实体、接口契约、共享类型 |
| 基础设施层 | `Chet.Admin.Data` / `Caching` / `Configuration` / `Logging` | 数据访问、缓存、配置、日志 |
| 测试层 | `Chet.Admin.UnitTests` / `IntegrationTests` | 单元测试、集成测试 |

依赖方向：**表示层 → 应用层 → 核心层 ← 基础设施层**（核心层不依赖任何外层）。

## 3. 依赖注入约定

DI 注册通过扩展方法组织，集中在 `Chet.Admin.Api/Configurations/` 目录，每个扩展方法对应一个功能域：

| 扩展方法 | 文件 | 作用 |
| ---- | ---- | ---- |
| `ConfigureRepositories` | `RepositoryConfiguration.cs` | 注册仓储（Scoped） |
| `ConfigureServices` | `ServiceConfiguration.cs` | 注册业务服务（Scoped） |
| `ConfigureDatabase` | `DatabaseConfiguration.cs` | EF Core + 数据库初始化 |
| `ConfigureRedis` | `RedisConfiguration.cs` | Redis 缓存（含 NoOp 降级） |
| `ConfigureJwt` | `JwtConfiguration.cs` | JWT 认证 |
| `ConfigureCors` | `CorsConfiguration.cs` | 跨域策略 |
| `ConfigureSwagger` | `SwaggerConfiguration.cs` | OpenAPI 文档 |
| `ConfigureApiVersioning` | `ApiVersionConfiguration.cs` | API 版本控制 |
| `ConfigureFluentValidation` | `FluentValidationConfiguration.cs` | 参数校验 |
| `ConfigureSerilog` | `SerilogConfiguration.cs` | 日志系统 |

### 3.1 注册示例

仓储注册（`RepositoryConfiguration.cs`）：

```csharp
// 通用仓储
services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
// 各模块仓储
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IRoleRepository, RoleRepository>();
```

服务注册（`ServiceConfiguration.cs`）：

```csharp
services.AddScoped<IUserService, UserService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddSingleton<IOnlineUserService, OnlineUserService>();  // 全局单例
services.AddSingleton<CaptchaService>();
```

## 4. 领域实体规范

### 4.1 BaseEntity 基类

所有实体继承 `BaseEntity`，自动获得基础字段：

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }            // 主键（自增）
    public DateTime CreatedAt { get; set; } // 创建时间（UTC）
    public DateTime UpdatedAt { get; set; } // 更新时间（UTC）
}
```

### 4.2 实体定义示例

以 `UserEntity` 为例：

```csharp
public class UserEntity : BaseEntity
{
    public string Name { get; set; }
    public string Email { get; set; }        // 唯一约束
    public string PasswordHash { get; set; } // BCrypt 哈希
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? Avatar { get; set; }
    public int? DepartmentId { get; set; }
    public List<UserRoleEntity> UserRoles { get; set; } = [];
    public int LoginFailCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool MustChangePassword { get; set; }
}
```

> 安全提示：包含敏感字段（`PasswordHash`、`RefreshToken`）的实体**禁止直接返回客户端**，必须通过 AutoMapper 映射为 DTO。

## 5. 接口契约规范

接口定义在 `Chet.Admin.Contracts` 项目中，按模块组织：

```
Contracts/
├── IRepository.cs            # 通用仓储接口
├── IUnitOfWork.cs           # 工作单元接口
├── User/
│   ├── IUserRepository.cs   # 用户仓储接口
│   └── IUserService.cs      # 用户服务接口
├── Role/
│   ├── IRoleRepository.cs
│   └── IRoleService.cs
└── ...
```

通用仓储接口：

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

工作单元接口：

```csharp
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    // ... 各模块仓储
    Task<int> SaveChangesAsync();
}
```

## 6. 业务服务规范

服务实现位于 `Chet.Admin.Services`，通过依赖注入获取仓储：

```csharp
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public UserService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id)
            ?? throw new NotFoundException($"User {id} not found");
        return _mapper.Map<UserDto>(user);  // 通过 AutoMapper 转换
    }
}
```

## 7. 控制器规范

控制器位于 `Chet.Admin.Api/Controllers`，遵循 RESTful 风格：

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]  // 统一路由前缀 + 版本
[Authorize]                                          // 默认需要认证
[SwaggerTag("模块描述")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(ApiResponse.Ok(users, "Users retrieved successfully"));
    }
}
```

- 所有响应通过 `ApiResponse` 统一包装
- 使用 `[ProducesResponseType]` 标注响应类型，供 Swagger 生成文档
- 控制器不写业务逻辑，仅转发到 Service

## 8. 新增业务模块流程

以新增「文章管理」模块为例，按层依次添加：

### 8.1 核心层 - 定义实体与接口

```
1. Domain/Article/ArticleEntity.cs       // 实体
2. Contracts/Article/
   ├── IArticleRepository.cs              // 仓储接口
   └── IArticleService.cs                 // 服务接口
```

### 8.2 基础设施层 - 实现数据访问

```
3. Data/Article/
   ├── ArticleConfig.cs                   // EF Core 实体配置
   └── ArticleRepository.cs               // 仓储实现
4. Data/AppDbContext.cs                  // 添加 DbSet<ArticleEntity>
```

EF Core 实体配置示例：

```csharp
public class ArticleConfig : IEntityTypeConfiguration<ArticleEntity>
{
    public void Configure(EntityTypeBuilder<ArticleEntity> builder)
    {
        builder.ToTable("Articles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
    }
}
```

### 8.3 应用层 - 实现业务逻辑

```
5. Services/Article/ArticleService.cs     // 服务实现
6. Mapping/Article/MappingProfile.cs      // AutoMapper 配置
```

### 8.4 表示层 - 添加控制器并注册

```
7. Controllers/ArticlesController.cs       // API 控制器
```

注册到 DI 容器：

```csharp
// RepositoryConfiguration.cs
services.AddScoped<IArticleRepository, ArticleRepository>();

// ServiceConfiguration.cs
services.AddScoped<IArticleService, ArticleService>();

// IUnitOfWork.cs（添加仓储属性）
IArticleRepository Articles { get; }

// UnitOfWork.cs（实现属性）
public IArticleRepository Articles => new ArticleRepository(_context);
```

### 8.5 数据库迁移

```bash
cd Chet.Admin.Api
dotnet ef migrations add AddArticles \
  --project ../Chet.Admin.Infrastructure/Chet.Admin.Data \
  --startup-project .
dotnet ef database update \
  --project ../Chet.Admin.Infrastructure/Chet.Admin.Data \
  --startup-project .
```

## 9. 配置管理

应用配置使用强类型 `AppSettings` 模型（位于 `Chet.Admin.Configuration`）：

```csharp
// Program.cs 中注册
var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
builder.Services.AddSingleton(appSettings!);
```

配置结构（`appsettings.json`）：

```json
{
  "ConnectionStrings": { "DefaultConnection": "Data Source=Chet.Admin.db" },
  "AppSettings": {
    "Jwt": {
      "Enabled": true,
      "SecretKey": "...",
      "Issuer": "Chet.Admin",
      "AccessTokenExpirationInMinutes": 30,
      "RefreshTokenExpirationDays": 7
    },
    "Redis": { "Enabled": false, "ConnectionString": "localhost:6379" },
    "PasswordPolicy": {
      "ExpirationDays": 90,
      "MinLength": 6,
      "RequireUppercase": false
    }
  }
}
```

## 10. 中间件管道

中间件按以下顺序执行（`Program.cs`）：

```
异常处理 → 日志上下文 → CORS → 限流 → SwaggerUI
→ 认证 → 授权 → 审计日志 → 在线用户追踪 → 静态文件 → 控制器
```

自定义中间件示例可参考 `AuditLogMiddleware.cs`、`OnlineUserTrackingMiddleware.cs`。

## 11. 测试规范

### 11.1 单元测试

位于 `Chet.Admin.UnitTests`，使用 xUnit + Moq：

```csharp
public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_unitOfWork.Object, ...);
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUserDto()
    {
        // Arrange / Act / Assert
    }
}
```

运行测试：

```bash
# 所有单元测试
dotnet test --filter "Category=Unit" --project Chet.Admin.Tests/Chet.Admin.UnitTests

# 测试覆盖率
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### 11.2 集成测试

位于 `Chet.Admin.IntegrationTests`，使用 TestServer + In-Memory Database。

## 12. 代码规范

- **命名空间**：PascalCase，与目录结构一致
- **类 / 方法**：PascalCase（方法动词开头）
- **变量**：camelCase
- **异步方法**：以 `Async` 结尾
- **接口**：以 `I` 开头
- **遵循** Microsoft C# 编码约定
