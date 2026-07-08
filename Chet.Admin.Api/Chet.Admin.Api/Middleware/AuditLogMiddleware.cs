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

    // Module mapping from controller name
    private static readonly Dictionary<string, string> ModuleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "users", "User" },
        { "roles", "Role" },
        { "menus", "Menu" },
        { "departments", "Department" },
        { "permissions", "Permission" },
        { "dictionaries", "Dictionary" },
        { "auth", "Auth" },
        { "auditlogs", "AuditLog" },
    };

    private static readonly Dictionary<string, string> ActionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "POST", "Create" },
        { "PUT", "Update" },
        { "DELETE", "Delete" },
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
    /// <param name="auditLogService">审计日志服务</param>
    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        var path = context.Request.Path.Value ?? "";

        // Only audit API write operations
        if (!path.StartsWith("/api/v") || context.Request.Method == "GET")
        {
            await _next(context);
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Capture request body
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

        // Write audit log asynchronously (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                var userNameClaim = context.User.FindFirst(ClaimTypes.Name);

                if (userIdClaim == null) return; // Skip unauthenticated requests

                // Extract module from path: /api/v1/users/... → "users"
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var controllerName = segments.Length >= 3 ? segments[2] : "";
                var module = ModuleMap.TryGetValue(controllerName, out var m) ? m : controllerName;
                var action = ActionMap.TryGetValue(context.Request.Method, out var a) ? a : context.Request.Method;

                // Truncate request data to prevent oversized logs
                if (requestData != null && requestData.Length > 2000)
                    requestData = requestData[..2000] + "...(truncated)";

                var auditLog = new AuditLogDto
                {
                    UserId = int.Parse(userIdClaim.Value),
                    UserName = userNameClaim?.Value ?? "",
                    Action = action,
                    Module = module,
                    Description = $"{action} {module}",
                    HttpMethod = context.Request.Method,
                    RequestPath = path,
                    RequestData = requestData,
                    StatusCode = context.Response.StatusCode,
                    ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    Duration = stopwatch.ElapsedMilliseconds,
                    OperatedAt = DateTime.UtcNow,
                };

                await auditLogService.LogAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log");
            }
        });
    }
}
