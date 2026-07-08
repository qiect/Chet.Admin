# API设计

## 1. 设计目标
- 提供一致的API响应格式，方便客户端处理
- 包含成功/失败状态标识，明确响应结果
- 支持数据、消息、状态码等必要字段
- 提供分页响应支持
- 保持与现有代码的兼容性
- 简化开发人员使用，提供便捷的静态工厂方法

## 2. 核心模型设计

### 2.1 通用响应包装器 `ApiResponse<T>`
```csharp
namespace Chet.Admin.Shared;

/// <summary>
/// 通用API响应包装器
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 响应状态码
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// 响应状态，true表示成功，false表示失败
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 响应消息
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }
    
    /// <summary>
    /// 响应时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// 创建成功的响应
    /// </summary>
    /// <param name="data">响应数据</param>
    /// <param name="message">响应消息</param>
    /// <returns>ApiResponse实例</returns>
    public static ApiResponse<T> SuccessResponse(T? data = default, string? message = "操作成功")
    {
        return new ApiResponse<T>
        {
            StatusCode = 200,
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// 创建失败的响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="statusCode">状态码</param>
    /// <returns>ApiResponse实例</returns>
    public static ApiResponse<T> ErrorResponse(string message, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            StatusCode = statusCode,
            Success = false,
            Message = message,
            Data = default(T),
            Timestamp = DateTime.UtcNow
        };
    }
}
```

### 2.2 无数据响应包装器 `ApiResponse`
```csharp
namespace Chet.Admin.Shared;

/// <summary>
/// 无数据的API响应包装器
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 响应状态码
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// 响应状态，true表示成功，false表示失败
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 响应消息
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// 响应时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// 创建成功的响应
    /// </summary>
    /// <param name="message">响应消息</param>
    /// <returns>ApiResponse实例</returns>
    public static ApiResponse SuccessResponse(string? message = "操作成功")
    {
        return new ApiResponse
        {
            StatusCode = 200,
            Success = true,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// 创建失败的响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="statusCode">状态码</param>
    /// <returns>ApiResponse实例</returns>
    public static ApiResponse ErrorResponse(string message, int statusCode = 400)
    {
        return new ApiResponse
        {
            StatusCode = statusCode,
            Success = false,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

### 2.3 分页响应模型 `PaginatedResponse<T>`
```csharp
namespace Chet.Admin.Shared;

/// <summary>
/// 分页API响应包装器
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class PaginatedResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    
    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
    
    /// <summary>
    /// 创建分页响应
    /// </summary>
    /// <param name="data">分页数据</param>
    /// <param name="pageNumber">当前页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="totalCount">总记录数</param>
    /// <param name="message">响应消息</param>
    /// <returns>PaginatedResponse实例</returns>
    public static PaginatedResponse<T> CreateResponse(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalCount,
        string? message = "操作成功")
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        return new PaginatedResponse<T>
        {
            StatusCode = 200,
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
```

### 2.4 错误响应模型 `ErrorResponse`
```csharp
namespace Chet.Admin.Shared;

/// <summary>
/// 错误响应模型
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// 详细错误信息（仅在开发环境下显示）
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="details">详细错误信息</param>
    public ErrorResponse(string message, string? errorCode = null, string? details = null)
    {
        Message = message;
        ErrorCode = errorCode;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }
}
```

## 3. 实现步骤

### 3.1 创建 Shared 项目并添加响应模型
- 在 `Chet.Admin.Shared` 项目中创建响应模型类
- 确保所有项目都能引用 Shared 项目

### 3.2 配置控制器基类（可选）
```csharp
namespace Chet.Admin.Api.Controllers;

/// <summary>
/// API控制器基类，提供统一的响应格式
/// </summary>
[ApiController]
public class BaseController : ControllerBase
{
    protected ApiResponse<T> Success<T>(T data, string message = "操作成功") =>
        ApiResponse<T>.SuccessResponse(data, message);
    
    protected ApiResponse Success(string message = "操作成功") =>
        ApiResponse.SuccessResponse(message);
    
    protected ApiResponse<T> Error<T>(string message, int statusCode = 400) =>
        ApiResponse<T>.ErrorResponse(message, statusCode);
    
    protected ApiResponse Error(string message, int statusCode = 400) =>
        ApiResponse.ErrorResponse(message, statusCode);
}
```

### 3.3 在控制器中使用统一响应
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(Error<UserDto>("用户不存在", 404));
            }
            
            return Ok(Success(user, "获取用户成功"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Error<UserDto>($"获取用户失败: {ex.Message}", 500));
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var users = await _userService.GetAllUsersAsync(page, pageSize);
            var totalCount = await _userService.GetTotalCountAsync();
            
            var paginatedResponse = PaginatedResponse<UserDto>.CreateResponse(
                users, page, pageSize, totalCount, "获取用户列表成功");
            
            return Ok(paginatedResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, Error<IEnumerable<UserDto>>($"获取用户列表失败: {ex.Message}", 500));
        }
    }
}
```

## 4. 使用示例

### 4.1 成功响应示例
```json
{
  "statusCode": 200,
  "success": true,
  "message": "获取用户成功",
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "john.doe@example.com"
  },
  "timestamp": "2023-10-27T10:30:00Z"
}
```

### 4.2 分页响应示例
```json
{
  "statusCode": 200,
  "success": true,
  "message": "获取用户列表成功",
  "data": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john.doe@example.com"
    }
  ],
  "timestamp": "2023-10-27T10:30:00Z",
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 100,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### 4.3 错误响应示例
```json
{
  "statusCode": 404,
  "success": false,
  "message": "用户不存在",
  "data": null,
  "timestamp": "2023-10-27T10:30:00Z"
}
```

## 5. 最佳实践

### 5.1 在中间件中处理全局异常
```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = new ErrorResponse(
            message: "内部服务器错误",
            errorCode: "INTERNAL_ERROR",
            details: exception.Message
        );

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
```

### 5.2 在服务层返回统一格式
```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserDto>.ErrorResponse("用户不存在", 404);
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.SuccessResponse(userDto, "获取用户成功");
        }
        catch (Exception ex)
        {
            return ApiResponse<UserDto>.ErrorResponse($"获取用户失败: {ex.Message}", 500);
        }
    }
}
```

## 6. 扩展性考虑

### 6.1 支持国际化
可以为响应消息添加多语言支持：
```csharp
public class ApiResponse<T>
{
    // ... 其他属性
    
    /// <summary>
    /// 本地化消息（可选）
    /// </summary>
    public Dictionary<string, string>? LocalizedMessages { get; set; }
}
```

### 6.2 添加追踪ID
为了便于调试和监控，可以添加请求追踪ID：
```csharp
public class ApiResponse<T>
{
    // ... 其他属性
    
    /// <summary>
    /// 请求追踪ID
    /// </summary>
    public string? TraceId { get; set; }
}
```

这种统一API响应模型设计有助于：
- 提供一致的API体验
- 简化客户端处理逻辑
- 提高错误处理的一致性
- 便于API版本管理和监控