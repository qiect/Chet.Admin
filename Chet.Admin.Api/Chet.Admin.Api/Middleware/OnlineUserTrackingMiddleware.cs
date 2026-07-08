using Chet.Admin.Contracts.User;
using System.Security.Claims;

namespace Chet.Admin.Api.Middleware;

public class OnlineUserTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public OnlineUserTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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
