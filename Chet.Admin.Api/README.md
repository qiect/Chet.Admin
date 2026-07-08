# Chet.Admin

一个现代化的后端Web API模板，基于.NET 10构建，支持单体服务和微服务架构。该模板提供了完整的身份认证、缓存、日志等功能，是一个企业级的API开发框架。

## 项目特点

- **现代化技术栈**: 采用最新的.NET 10和C# 12语言特性
- **企业级架构**: 遵循Clean Architecture和领域驱动设计(DDD)
- **灵活架构**: 支持单体服务和微服务架构，可根据业务需求灵活选择
- **安全可靠**: 集成JWT身份认证、数据验证和安全最佳实践
- **高性能**: 支持异步编程、缓存策略和性能优化
- **可扩展性**: 遵循SOLID原则，易于扩展和维护
- **完整测试**: 提供单元测试和集成测试框架

## 技术栈

### 核心技术

- **框架**: .NET 10
- **语言**: C# 12
- **运行时**: .NET Core Runtime

### 数据访问

- **ORM**: Entity Framework Core 8
- **数据库**: 
  - 主数据库: PostgreSQL (生产环境)
  - 开发数据库: SQLite (本地开发)
  - 缓存: Redis
- **数据库迁移**: EF Core Migrations

### 身份认证与安全

- **身份认证**: JWT (JSON Web Tokens) + OAuth 2.0
- **密码哈希**: BCrypt
- **授权**: 基于策略的授权
- **CORS**: 跨域资源共享配置

### 缓存与性能

- **缓存**: Redis + Memory Cache
- **响应缓存**: HTTP响应缓存中间件
- **对象映射**: AutoMapper

### 日志与监控

- **日志**: Serilog + 结构化日志
- **API文档**: Swagger/OpenAPI + NSwag
- **健康检查**: ASP.NET Core Health Checks
- **指标**: Prometheus (可选)

### 测试与质量保证

- **单元测试**: xUnit + Moq
- **集成测试**: xUnit + TestServer + In-Memory Database
- **API测试**: xUnit + HttpClient
- **代码质量**: StyleCop + FxCopAnalyzers

### 开发工具

- **IDE支持**: Visual Studio 2022 / Rider / VS Code
- **包管理**: NuGet
- **构建工具**: MSBuild + .NET CLI
- **依赖注入**: Microsoft.Extensions.DependencyInjection

## 架构设计

项目采用Clean Architecture和Domain-Driven Design (DDD)模式，分为以下几个层次：

```
Chet.Admin/
├── Chet.Admin.Core/           # 核心层 (领域模型和业务规则)
│   ├── Chet.Admin.Contracts/   # 接口定义
│   ├── Chet.Admin.Domain/      # 领域模型 (实体、值对象、聚合根)
│   └── Chet.Admin.Shared/      # 共享类和异常
├── Chet.Admin.Application/    # 应用层 (业务逻辑和用例)
│   ├── Chet.Admin.DTOs/        # 数据传输对象
│   ├── Chet.Admin.Mapping/     # AutoMapper配置
│   ├── Chet.Admin.Services/    # 业务逻辑实现
│   └── Chet.Admin.Abstractions/ # 抽象和契约
├── Chet.Admin.Infrastructure/ # 基础设施层 (外部依赖)
│   ├── Chet.Admin.Caching/     # 缓存实现
│   ├── Chet.Admin.Configuration/ # 配置管理
│   ├── Chet.Admin.Data/        # 数据库访问
│   ├── Chet.Admin.Authentication/ # 认证授权
│   └── Chet.Admin.Logging/     # 日志配置
├── Chet.Admin.Api/            # 表示层 (API控制器)
│   ├── Controllers/                     # API控制器
│   ├── Middleware/                      # 自定义中间件
│   ├── Extensions/                      # 服务扩展
│   └── Program.cs                       # 应用启动配置
└── Chet.Admin.Tests/          # 测试层
    ├── Chet.Admin.UnitTests/    # 单元测试
    └── Chet.Admin.IntegrationTests/ # 集成测试
```

### 部署架构

- **单体架构**: 所有功能模块部署在单一应用中，适合中小规模应用
- **微服务架构**: 功能模块独立部署，通过API网关通信，适合大规模分布式应用
- **容器化部署**: 使用Docker容器化部署，支持Kubernetes编排

## 快速开始

### 环境要求

- **.NET 10 SDK**: 最新稳定版
- **SQLite**: 本地开发数据库 (自动嵌入)
- **Redis**: 可选，用于缓存 (应用会在Redis不可用时自动降级)
- **Git**: 版本控制

### 本地开发设置

1. **克隆仓库**
   ```bash
   git clone https://github.com/qiect/Chet.Admin.git
   cd Chet.Admin
   ```

2. **配置数据库连接**
   修改 `Chet.Admin.Api/appsettings.json` 中的数据库连接字符串：
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=ChetWebApiTemplate.db"
     },
     "AppSettings": {
       "Jwt": {
         "SecretKey": "YourSuperSecretKeyForJWTAuthentication1234567890123",
         "Issuer": "Chet.Admin",
         "Audience": "Chet.Admin",
         "AccessTokenExpirationInMinutes": 30,
         "RefreshTokenExpirationInDays": 7
       },
       "Redis": {
         "ConnectionString": "localhost:6379",
         "InstanceName": "ChetWebApiTemplate:"
       }
     }
   }
   ```

3. **安装依赖**
   ```bash
   dotnet restore
   ```

4. **运行数据库迁移**
   ```bash
   dotnet ef database update --project Chet.Admin.Data --startup-project Chet.Admin.Api
   ```

5. **启动应用**
   ```bash
   dotnet run --project Chet.Admin.Api
   ```

6. **访问API文档**
   打开浏览器访问：http://localhost:5021/swagger
   应用会自动重定向到Swagger UI页面

### Docker部署

1. **构建并运行服务**
   ```bash
   docker-compose up --build
   ```

2. **单独构建镜像**
   ```bash
   docker build -t chet-webapi-template -f src/Chet.Admin.Api/Dockerfile .
   ```

## 功能模块

### 用户管理模块

- **用户认证**: JWT身份认证，支持登录和注销
- **用户授权**: 基于角色和策略的权限控制
- **用户管理**: 用户的增删改查功能
- **密码管理**: 安全的密码哈希和验证
- **刷新令牌**: 支持JWT刷新令牌机制

### 身份认证模块

- **JWT Token**: 生成和验证JWT访问令牌
- **刷新机制**: 自动刷新过期的访问令牌
- **多因子认证**: 可扩展的多因子认证支持
- **OAuth 2.0**: 支持第三方登录集成

### 缓存管理模块

- **分布式缓存**: Redis缓存实现
- **内存缓存**: 本地内存缓存
- **缓存策略**: 自动过期和缓存键管理
- **缓存穿透防护**: 防止缓存穿透的安全措施

### 数据访问模块

- **Repository模式**: 统一的数据访问接口
- **UnitOfWork**: 事务管理
- **查询过滤**: 全局查询过滤器
- **审计日志**: 自动记录数据变更日志

### 日志管理模块

- **结构化日志**: 使用Serilog进行结构化日志记录
- **多目标输出**: 支持控制台、文件等多种输出
- **日志级别控制**: 支持不同环境的日志级别配置
- **敏感数据过滤**: 自动过滤敏感信息

## 配置说明

### 应用配置

主要配置文件位于 `Chet.Admin.Api/appsettings.json`：

- **数据库连接**: `ConnectionStrings.DefaultConnection`
- **JWT配置**: `AppSettings.Jwt`
- **Redis配置**: `AppSettings.Redis`
- **Serilog配置**: `Serilog`
- **CORS配置**: `AllowedHosts`

### JWT配置详解

```json
{
  "AppSettings": {
    "Jwt": {
      "SecretKey": "YourSuperSecretKeyForJWTAuthentication1234567890123",
      "Issuer": "Chet.Admin",
      "Audience": "Chet.Admin",
      "AccessTokenExpirationInMinutes": 30,
      "RefreshTokenExpirationInDays": 7,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "ValidateLifetime": true,
      "ClockSkew": "00:05:00"
    }
  }
}
```

### Redis配置详解

```json
{
  "AppSettings": {
    "Redis": {
      "ConnectionString": "localhost:6379,password=yourpassword,ssl=false,abortConnect=false",
      "InstanceName": "ChetWebApiTemplate:",
      "DefaultExpireTimeInMinutes": 60
    }
  }
}
```

### CORS配置

```json
{
  "AllowedHosts": "*",
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://yourdomain.com"
    ],
    "AllowedMethods": [ "GET", "POST", "PUT", "DELETE", "OPTIONS" ],
    "AllowedHeaders": [ "Content-Type", "Authorization", "X-Requested-With" ]
  }
}
```

## 部署说明

### 本地部署

1. **发布应用**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **部署到IIS**
   - 创建应用程序池 (.NET CLR版本: No Managed Code)
   - 创建网站，指向发布目录
   - 配置web.config

3. **自托管**
   ```bash
   cd publish
   dotnet Chet.Admin.Api.dll
   ```

### Docker部署

1. **使用Docker Compose**
   ```yaml
   version: '3.8'
   
   services:
     api:
       build: ./src/Chet.Admin.Api
       ports:
         - "5021:80"
       environment:
         - ConnectionStrings__DefaultConnection=Host=db;Database=chet_webapi;Username=postgres;Password=password
       depends_on:
         - db
         - redis
         
     db:
       image: postgres:15
       environment:
         POSTGRES_DB: chet_webapi
         POSTGRES_USER: postgres
         POSTGRES_PASSWORD: password
       volumes:
         - postgres_data:/var/lib/postgresql/data
         
     redis:
       image: redis:7-alpine
       ports:
         - "6379:6379"
   
   volumes:
     postgres_data:
   ```

2. **单独运行容器**
   ```bash
   docker run -d -p 5021:80 --name chet-api chet-webapi-template
   ```

### 云平台部署

#### Azure部署

1. **部署到Azure App Service**
   - 创建App Service
   - 配置部署源 (GitHub Actions, ZIP Deploy等)
   - 设置应用设置 (连接字符串、环境变量)

2. **使用Azure Container Instances**
   ```bash
   az container create \
     --resource-group myResourceGroup \
     --name chet-webapi-container \
     --image chet-webapi-template \
     --dns-name-label my-app \
     --ports 80
   ```

## 开发指南

### 添加新功能模块

1. **设计领域模型** (Core.Domain)
   - 定义实体 (Entity)
   - 定义值对象 (Value Object)
   - 定义聚合根 (Aggregate Root)

2. **定义接口契约** (Core.Contracts)
   - 定义仓储接口 (IRepository<T>)
   - 定义服务接口 (IService)

3. **实现基础设施** (Infrastructure)
   - 实现仓储 (Repository)
   - 配置实体映射 (Entity Configuration)

4. **实现业务逻辑** (Application)
   - 实现服务 (Service)
   - 添加DTO和映射配置 (DTOs + Mapping Profiles)

5. **添加API端点** (Api)
   - 添加控制器 (Controller)
   - 配置路由和授权

### 数据库迁移

```bash
# 添加新迁移
dotnet ef migrations add MigrationName --project Chet.Admin.Data --startup-project Chet.Admin.Api

# 更新数据库
dotnet ef database update --project Chet.Admin.Data --startup-project Chet.Admin.Api

# 移除最后的迁移
dotnet ef migrations remove --project Chet.Admin.Data --startup-project Chet.Admin.Api
```

### 代码规范

- **C#**: 遵循Microsoft C#编码约定
- **命名空间**: 使用PascalCase，层级清晰
- **类命名**: 使用PascalCase，名词形式
- **方法命名**: 使用PascalCase，动词开头
- **变量命名**: 使用camelCase
- **常量命名**: 使用PascalCase
- **异步方法**: 以Async结尾

### 安全最佳实践

- 输入验证和输出编码
- 参数化查询防止SQL注入
- JWT令牌安全存储和传输
- HTTPS强制使用
- CORS策略限制
- 敏感信息加密存储

## 测试说明

### 单元测试

单元测试使用xUnit和Moq框架，位于 `Chet.Admin.Tests/Chet.Admin.UnitTests` 目录下：

```bash
# 运行所有单元测试
dotnet test --filter "Category=Unit" --project Chet.Admin.Tests/Chet.Admin.UnitTests

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~UserServiceTests" --project Chet.Admin.Tests/Chet.Admin.UnitTests
```

### 集成测试

集成测试使用TestServer和In-Memory Database，位于 `Chet.Admin.Tests/Chet.Admin.IntegrationTests` 目录下：

```bash
# 运行所有集成测试
dotnet test --filter "Category=Integration" --project Chet.Admin.Tests/Chet.Admin.IntegrationTests

# 运行特定集成测试
dotnet test --filter "FullyQualifiedName~UserServiceIntegrationTests" --project Chet.Admin.Tests/Chet.Admin.IntegrationTests
```

### 测试覆盖率

```bash
# 使用coverlet生成测试覆盖率报告
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## API文档

应用启动后，通过以下URL访问API文档：

- **Swagger UI**: http://localhost:5000/swagger
- **OpenAPI JSON**: http://localhost:5000/swagger/v1/swagger.json
- **API参考**: http://localhost:5000/api-docs

## 性能优化

### 缓存策略

- **Redis缓存**: 使用Redis进行分布式缓存
- **响应缓存**: 对GET请求启用HTTP响应缓存
- **内存缓存**: 对频繁访问的数据使用内存缓存
- **缓存预热**: 应用启动时预加载常用数据

### 数据库优化

- **索引优化**: 为经常查询的字段添加数据库索引
- **查询优化**: 使用投影减少不必要的数据传输
- **批量操作**: 对大量数据操作使用批量处理
- **连接池**: 配置合适的数据库连接池大小

### 异步编程

- **异步API**: 所有API端点使用异步编程模型
- **并行处理**: 对独立操作使用并行处理
- **非阻塞I/O**: 使用非阻塞I/O操作提高吞吐量

## 监控与日志

### 日志记录

使用Serilog进行结构化日志记录：

```csharp
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    
    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        _logger.LogInformation("Getting user with ID {UserId}", id);
        
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return NotFound();
            }
            
            _logger.LogInformation("Successfully retrieved user {UserId}", id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting user with ID {UserId}", id);
            throw;
        }
    }
}
```

### 健康检查

```csharp
// 在Program.cs中配置
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"))
    .AddCheck<CustomHealthCheck>("custom_check");
```

## 贡献指南

### 开发流程

1. Fork项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建Pull Request

### 代码审查

- 所有PR必须经过至少一人审查
- 确保所有测试通过
- 遵循代码规范
- 添加适当注释和文档

## 许可证

本项目采用 MIT 许可证 - 请参阅 [LICENSE](LICENSE) 文件了解详情。

## 支持与联系

- **问题跟踪**: [Issues](https://github.com/qiect/Chet.Admin/issues)
- **讨论区**: [Discussions](https://github.com/qiect/Chet.Admin/discussions)
- **贡献者**: 查看 [贡献者列表](https://github.com/qiect/Chet.Admin/graphs/contributors)
