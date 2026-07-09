using System.Security.Claims;
using System.Text.Json;
using Chet.Admin.Contracts.Audit;
using Chet.Admin.DTOs.Audit;

namespace Chet.Admin.Api.Middleware;

/// <summary>
/// 审计日志中间件
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    // 模块名映射（控制器名 → 中文模块名）
    private static readonly Dictionary<string, string> ModuleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "users", "用户管理" },
        { "roles", "角色管理" },
        { "menus", "菜单管理" },
        { "departments", "部门管理" },
        { "dictionaries", "字典管理" },
        { "auth", "认证授权" },
        { "auditlogs", "审计日志" },
        { "files", "文件管理" },
        { "notifications", "通知管理" },
        { "dashboard", "仪表盘" },
        { "onlineusers", "在线用户" },
    };

    // 操作动作映射（HTTP 方法 → 中文动作）
    private static readonly Dictionary<string, string> ActionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "POST", "新增" },
        { "PUT", "修改" },
        { "DELETE", "删除" },
    };

    /// <summary>
    /// 初始化审计日志中间件的新实例
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    /// <param name="logger">日志记录器</param>
    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 处理HTTP请求，对API写操作进行审计日志记录
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="scopeFactory">用于在后台线程创建独立DI作用域的服务工厂</param>
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        var path = context.Request.Path.Value ?? "";

        // Only audit API write operations
        if (!path.StartsWith("/api/v") || context.Request.Method == "GET")
        {
            await _next(context);
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Capture request body（multipart/form-data 等不可 Seek 的流会被跳过，避免影响上传）
        string? requestData = null;
        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestData = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        stopwatch.Stop();

        // Restore response body
        responseBody.Position = 0;
        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;

        // 在请求作用域结束前同步取出所有需要的数据，避免在后台线程访问已释放的 HttpContext
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        var userNameClaim = context.User.FindFirst(ClaimTypes.Name);

        if (userIdClaim == null) return; // 跳过未认证请求

        var userId = int.Parse(userIdClaim.Value);
        var userName = userNameClaim?.Value ?? $"用户{userIdClaim.Value}";
        var httpMethod = context.Request.Method;
        var statusCode = context.Response.StatusCode;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "";
        var userAgent = context.Request.Headers.UserAgent.ToString();

        // 提取模块名和动作（中文化）
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var controllerName = segments.Length >= 3 ? segments[2] : "";
        var module = ModuleMap.TryGetValue(controllerName, out var m) ? m : controllerName;
        var action = ActionMap.TryGetValue(httpMethod, out var a) ? a : httpMethod;

        // 截断过长的请求数据
        if (requestData != null && requestData.Length > 2000)
            requestData = requestData[..2000] + "...(truncated)";

        var duration = stopwatch.ElapsedMilliseconds;
        var operatedAt = DateTime.UtcNow;

        // fire-and-forget 写审计，但在独立 DI 作用域内解析服务，避免使用已释放的 scoped DbContext
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

                var auditLog = new AuditLogDto
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    Module = module,
                    Description = $"{action}{module}",
                    HttpMethod = httpMethod,
                    RequestPath = path,
                    RequestData = requestData,
                    StatusCode = statusCode,
                    ClientIp = clientIp,
                    UserAgent = userAgent,
                    Duration = duration,
                    OperatedAt = operatedAt,
                };

                await auditLogService.LogAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for {Method} {Path}", httpMethod, path);
            }
        });
    }
}
