using System.Collections.Concurrent;
using System.Net;
using Chet.Admin.Shared;

namespace Chet.Admin.Api.Middleware;

/// <summary>
/// 请求限流中间件
/// <para>
/// 基于IP地址的请求频率限制，用于防止暴力破解攻击和API滥用。
/// 使用滑动窗口算法（Sliding Window）跟踪每个客户端的请求次数，
/// 在超过阈值时返回429 Too Many Requests状态码。
/// </para>
/// </summary>
/// <remarks>
/// <para>为什么需要限流？</para>
/// <list type="bullet">
///   <item><description>防止暴力破解：限制登录接口的请求频率，增加攻击成本</description></item>
///   <item><description>保护资源：防止恶意用户通过大量请求耗尽服务器资源</description></item>
///   <item><description>保障服务稳定性：在流量高峰时保护后端服务不被压垮</description></item>
/// </list>
/// 
/// <para>限流规则：</para>
/// <list type="table">
///   <listheader>
///     <term>端点</term>
///     <description>限制</description>
///     <term>时间窗口</term>
///   </listheader>
///   <item>
///     <term>/login</term>
///     <description>5次/分钟</description>
///     <term>60秒</term>
///   </item>
///   <item>
///     <term>/register</term>
///     <description>10次/分钟</description>
///     <term>60秒</term>
///   </item>
/// </list>
/// 
/// <para>响应头：</para>
/// <list type="table">
///   <listheader>
///     <term>头名称</term>
///     <description>说明</description>
///   </listheader>
///   <item>
///     <term>Retry-After</term>
///     <description>建议客户端等待的秒数，直到可以再次发送请求</description>
///   </item>
/// </list>
/// 
/// <para>使用方式：</para>
/// <code>
/// // 在Program.cs中注册
/// app.UseRateLimiting();
/// </code>
/// </remarks>
public class RateLimitingMiddleware
{
    /// <summary>
    /// 下一个中间件委托
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// 日志记录器实例
    /// </summary>
    private readonly ILogger<RateLimitingMiddleware> _logger;

    /// <summary>
    /// 请求记录字典
    /// <para>
    /// 键：客户端标识（IP地址 + 请求路径）
    /// 值：请求记录（包含计数和时间窗口起始时间）
    /// 使用ConcurrentDictionary保证线程安全
    /// </para>
    /// </summary>
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requestLog = new();

    /// <summary>
    /// 定时器，用于定期清理过期的请求记录
    /// <para>每5分钟执行一次清理操作，防止内存泄漏</para>
    /// </summary>
    private static readonly Timer _cleanupTimer = new(CleanupExpiredRecords, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

    /// <summary>
    /// 初始化限流中间件的新实例
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    /// <param name="logger">日志记录器，用于记录限流事件和警告</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 处理HTTP请求并应用限流逻辑
    /// </summary>
    /// <param name="context">当前HTTP请求上下文</param>
    /// <returns>表示异步操作的任务</returns>
    /// <remarks>
    /// <para>处理流程：</para>
    /// <list type="number">
    ///   <item><description>检查请求路径是否匹配需要限流的端点</description></item>
    ///   <item><description>获取客户端真实IP地址</description></item>
    ///   <item><description>更新或创建该客户端的请求记录</description></item>
    ///   <item><description>判断是否超过限制阈值</description></item>
    ///   <item><description>如果超限：返回429 + Retry-After头</description></item>
    ///   <item><description>如果未超限：放行到下一个中间件</description></item>
    /// </list>
    /// 
    /// <para>IP地址获取优先级：</para>
    /// <list type="number">
    ///   <item><description>X-Forwarded-For 头（反向代理场景）</description></item>
   ///   <item><description>X-Real-IP 头（Nginx等）</description></item>
    ///   <item><description>Connection.RemoteIpAddress（直连场景）</description></item>
    /// </list>
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        if (!path.Contains("/login") && !path.Contains("/register"))
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIpAddress(context);
        var key = $"{clientIp}:{path}";
        // 使用 UTC 时间进行限流计算，避免时区问题
        var now = DateTime.UtcNow;
        
        var windowMinutes = 1;
        var maxRequestsPerWindow = path.Contains("/login") ? 5 : 10;

        _requestLog.AddOrUpdate(key,
            addValue: (1, now),
            updateValueFactory: (_, existing) =>
                (now - existing.WindowStart).TotalMinutes < windowMinutes
                    ? (existing.Count + 1, existing.WindowStart)
                    : (1, now));

        if (_requestLog.TryGetValue(key, out var record) && record.Count > maxRequestsPerWindow)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {ClientIp} on {Path}. Count: {Count}, Max: {Max}",
                clientIp, path, record.Count, maxRequestsPerWindow);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = (60 - (int)(now - record.WindowStart).TotalSeconds).ToString();

            await context.Response.WriteAsJsonAsync(
                ApiResponse.Error("请求过于频繁，请稍后再试", 429));
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// 获取客户端的真实IP地址
    /// </summary>
    /// <param name="context">当前HTTP请求上下文</param>
    /// <returns>
    /// 客户端的IP地址字符串。
    /// 如果无法确定，则返回 "unknown"
    /// </returns>
    /// <remarks>
    /// <para>IP地址解析优先级：</para>
    /// <list type="ordered">
    ///   <item>
    ///     <term>X-Forwarded-For</term>
    ///     <description>
    ///       标准的反向代理转发的原始IP头。
    ///       格式可能为 "client, proxy1, proxy2"，取第一个有效IP。
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>X-Real-IP</term>
    ///     <description>Nginx常用的真实IP头</description>
    ///   </item>
    ///   <item>
    ///     <term>RemoteIpAddress</term>
    ///     <description>TCP连接的直接远程IP（无代理时使用）</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>注意：</para>
    /// <para>
    /// 在生产环境中部署在反向代理（如Nginx、CloudFlare、Azure Front Door）后面时，
    /// 必须配置代理正确传递这些头部信息，否则可能导致所有请求被视为来自同一IP。
    /// </para>
    /// </remarks>
    private static string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-Ip"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 清理过期的请求记录
    /// <para>
    /// 由定时器周期性调用，移除超过5分钟的旧记录，
    /// 防止内存中积累过多无用的数据。
    /// </para>
    /// </summary>
    /// <param name="state">
    /// 定时器状态参数（未使用，保留以符合TimerCallback签名要求）
    /// </param>
    /// <remarks>
    /// <para>清理条件：</para>
    /// <para>移除窗口开始时间距今超过5分钟的所有记录。</para>
    /// 
    /// <para>调用频率：</para>
    /// <para>每5分钟自动执行一次。</para>
    /// </remarks>
    private static void CleanupExpiredRecords(object? state)
    {
        // 使用 UTC 时间计算截止时间，确保与限流逻辑时间一致
        var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

        foreach (var key in _requestLog.Keys.Where(k => _requestLog.TryGetValue(k, out var record) && record.WindowStart < cutoffTime).ToList())
        {
            _requestLog.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// 限流中间件扩展方法类
/// <para>
/// 提供便捷的扩展方法用于将限流中间件注册到ASP.NET Core管道中。
/// </para>
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// 将请求限流中间件添加到应用程序管道
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器，支持链式调用</returns>
    /// <remarks>
    /// <para>推荐位置：</para>
    /// <para>
    /// 应该在以下中间件之后注册：
    /// <list type="bullet">
    ///   <item><description>UseHttpsRedirection</description></item>
    ///   <item><description>UseCors</description></item>
    /// </list>
    /// 应该在以下中间件之前注册：
    /// <list type="bullet">
    ///   <item><description>UseAuthentication</description></item>
    ///   <item><description>UseAuthorization</description></item>
    ///   <item><description>MapControllers</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// var app = builder.Build();
    /// app.UseCors("DefaultPolicy");
    /// app.UseRateLimiting();  // &lt;-- 在这里添加
    /// app.UseAuthentication();
    /// app.MapControllers();
    /// </code>
    /// </remarks>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
