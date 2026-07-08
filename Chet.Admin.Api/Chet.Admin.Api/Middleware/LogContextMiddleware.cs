using Chet.Admin.Logging;
using System.Diagnostics;

namespace Chet.Admin.Api.Middleware;

/// <summary>
/// 日志上下文中间件
/// <para>
/// 自动为每个请求添加上下文信息到日志中，包括：
/// - 请求ID（用于追踪整个请求链路）
/// - HTTP方法和路径
/// - 用户信息（如果已认证）
/// </para>
/// </summary>
/// <remarks>
/// <para>使用场景：</para>
/// <list type="bullet">
///   <item><description>追踪请求链路，便于问题排查</description></item>
///   <item><description>审计日志，记录谁在什么时候做了什么</description></item>
///   <item><description>性能监控，分析请求处理时间</description></item>
/// </list>
/// 
/// <para>使用示例：</para>
/// <code>
/// // 在 Program.cs 中注册中间件
/// app.UseMiddleware&lt;LogContextMiddleware&gt;();
/// </code>
/// 
/// <para>日志输出效果：</para>
/// <code>
/// [23:46:10] [INFO] 用户登录成功
///     └─ Properties: {
///         "RequestId": "0HN4J5Q0000001",
///         "Method": "POST",
///         "Path": "/api/v1/auth/login",
///         "UserId": "123",
///         "UserName": "张三"
///     }
/// </code>
/// </remarks>
public class LogContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LogContextMiddleware> _logger;

    /// <summary>
    /// 初始化日志上下文中间件
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    /// <param name="logger">日志记录器</param>
    public LogContextMiddleware(RequestDelegate next, ILogger<LogContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 处理HTTP请求
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // 记录请求开始时间
        var stopwatch = Stopwatch.StartNew();
        
        // 添加请求上下文信息到日志
        using (LogContextHelper.WithRequest(
            context.TraceIdentifier,
            context.Request.Method,
            context.Request.Path.Value ?? ""))
        {
            // 如果用户已认证，添加用户信息
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value 
                            ?? context.User.FindFirst("id")?.Value 
                            ?? "unknown";
                var userName = context.User.Identity.Name ?? "unknown";
                
                // 使用 LogContextHelper 添加用户上下文
                using (LogContextHelper.WithUser(userId, userName))
                {
                    _logger.LogInformation(
                        "请求开始: {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path.Value);
                    
                    await _next(context);
                }
            }
            else
            {
                _logger.LogInformation(
                    "请求开始: {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path.Value);
                
                await _next(context);
            }
        }
        
        // 记录请求处理时间
        stopwatch.Stop();
        _logger.LogInformation(
            "请求完成: {Method} {Path} - {StatusCode} ({ElapsedMilliseconds}ms)",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}

/// <summary>
/// 中间件扩展方法
/// </summary>
public static class LogContextMiddlewareExtensions
{
    /// <summary>
    /// 使用日志上下文中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseLogContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogContextMiddleware>();
    }
}
