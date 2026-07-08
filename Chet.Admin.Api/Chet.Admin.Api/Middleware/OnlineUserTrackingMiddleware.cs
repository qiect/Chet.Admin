using Chet.Admin.Contracts.User;
using System.Security.Claims;

namespace Chet.Admin.Api.Middleware;

/// <summary>
/// 在线用户跟踪中间件
/// </summary>
public class OnlineUserTrackingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 初始化在线用户跟踪中间件的新实例
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    public OnlineUserTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 处理HTTP请求，并在请求完成后更新用户活动时间
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="onlineUserService">在线用户服务</param>
    public async Task InvokeAsync(HttpContext context, IOnlineUserService onlineUserService)
    {
        await _next(context);

        // After request completes, update activity
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            onlineUserService.UpdateActivity(userId);
        }
    }
}
