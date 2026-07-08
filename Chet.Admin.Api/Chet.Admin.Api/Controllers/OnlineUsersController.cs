using Chet.Admin.Contracts;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OnlineUsersController> _logger;

    /// <summary>
    /// 初始化在线用户控制器的新实例
    /// </summary>
    /// <param name="onlineUserService">在线用户服务接口</param>
    /// <param name="unitOfWork">工作单元，用于数据持久化操作</param>
    /// <param name="logger">日志记录器</param>
    public OnlineUsersController(
        IOnlineUserService onlineUserService,
        IUnitOfWork unitOfWork,
        ILogger<OnlineUsersController> logger)
    {
        _onlineUserService = onlineUserService;
        _unitOfWork = unitOfWork;
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
    /// <remarks>
    /// 执行以下操作使指定用户立即下线：
    /// 1. 从在线用户列表中移除
    /// 2. 将用户加入令牌黑名单，使其已签发的JWT立即失效
    /// 3. 清除数据库中用户的RefreshToken，防止通过刷新令牌获取新令牌
    /// 用户需要重新登录才能获得新的有效令牌。
    /// </remarks>
    /// <param name="userId">用户ID</param>
    /// <returns>强制下线结果</returns>
    [HttpDelete("{userId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForceOffline(int userId)
    {
        // 1. 加入令牌黑名单并移除在线记录（使已签发的JWT立即失效）
        _onlineUserService.ForceOffline(userId);

        // 2. 清除用户的RefreshToken，防止通过刷新令牌获取新令牌
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("RefreshToken cleared for forced-offline user {UserId}", userId);
        }

        return Ok(ApiResponse.Ok(null, "User forced offline successfully"));
    }
}
