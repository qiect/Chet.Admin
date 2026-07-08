using Chet.Admin.Contracts.User;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("在线用户管理")]
public class OnlineUsersController : ControllerBase
{
    private readonly IOnlineUserService _onlineUserService;
    private readonly ILogger<OnlineUsersController> _logger;

    public OnlineUsersController(IOnlineUserService onlineUserService, ILogger<OnlineUsersController> logger)
    {
        _onlineUserService = onlineUserService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult GetOnlineUsers()
    {
        var users = _onlineUserService.GetOnlineUsers();
        return Ok(ApiResponse.Ok(users, "Online users retrieved successfully"));
    }

    [HttpDelete("{userId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult ForceOffline(int userId)
    {
        _onlineUserService.UserOffline(userId);
        return Ok(ApiResponse.Ok(null, "User forced offline successfully"));
    }
}
