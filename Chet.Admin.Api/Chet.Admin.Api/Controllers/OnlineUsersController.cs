using Chet.Admin.Contracts.User;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 在线用户控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("在线用户管理")]
public class OnlineUsersController : ControllerBase
{
    private readonly IOnlineUserService _onlineUserService;
    private readonly ILogger<OnlineUsersController> _logger;

    /// <summary>
    /// 初始化在线用户控制器的新实例
    /// </summary>
    /// <param name="onlineUserService">在线用户服务接口</param>
    /// <param name="logger">日志记录器</param>
    public OnlineUsersController(IOnlineUserService onlineUserService, ILogger<OnlineUsersController> logger)
    {
        _onlineUserService = onlineUserService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有在线用户列表
    /// </summary>
    /// <returns>在线用户列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult GetOnlineUsers()
    {
        var users = _onlineUserService.GetOnlineUsers();
        return Ok(ApiResponse.Ok(users, "Online users retrieved successfully"));
    }

    /// <summary>
    /// 强制指定用户下线
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>强制下线结果</returns>
    [HttpDelete("{userId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult ForceOffline(int userId)
    {
        _onlineUserService.UserOffline(userId);
        return Ok(ApiResponse.Ok(null, "User forced offline successfully"));
    }
}
