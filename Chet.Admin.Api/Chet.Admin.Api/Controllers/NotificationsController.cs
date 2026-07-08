using Chet.Admin.Contracts.Notification;
using Chet.Admin.DTOs.Notification;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 通知公告控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("通知公告管理")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    /// <summary>
    /// 初始化通知公告控制器的新实例
    /// </summary>
    /// <param name="notificationService">通知服务接口</param>
    /// <param name="logger">日志记录器</param>
    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 创建通知
    /// </summary>
    /// <param name="dto">通知创建数据传输对象</param>
    /// <returns>创建的通知信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNotification(CreateNotificationDto dto)
    {
        var senderId = GetUserId();
        var notification = await _notificationService.CreateNotificationAsync(dto, senderId);
        return Ok(ApiResponse.Ok(notification, "Notification created successfully", StatusCodes.Status201Created));
    }

    /// <summary>
    /// 分页获取通知列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>分页通知列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _notificationService.GetPagedNotificationsAsync(request);
        return Ok(PaginatedResponse<NotificationDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Notifications retrieved successfully"));
    }

    /// <summary>
    /// 分页获取当前用户的通知列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>当前用户的分页通知列表</returns>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PaginatedResponse<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var userId = GetUserId();
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _notificationService.GetMyNotificationsAsync(userId, request);
        return Ok(PaginatedResponse<NotificationDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "My notifications retrieved successfully"));
    }

    /// <summary>
    /// 获取当前用户的未读通知数量
    /// </summary>
    /// <returns>未读通知数量</returns>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        var result = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(ApiResponse.Ok(result, "Unread count retrieved successfully"));
    }

    /// <summary>
    /// 将指定通知标记为已读
    /// </summary>
    /// <param name="id">通知ID</param>
    /// <returns>标记结果</returns>
    [HttpPut("{id}/read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse.Ok(null, "Notification marked as read"));
    }

    /// <summary>
    /// 将当前用户的所有未读通知标记为已读
    /// </summary>
    /// <returns>标记结果</returns>
    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse.Ok(null, "All notifications marked as read"));
    }

    /// <summary>
    /// 删除指定通知
    /// </summary>
    /// <param name="id">通知ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return Ok(ApiResponse.NoContent("Notification deleted successfully"));
    }

    /// <summary>
    /// 从JWT Claims中获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID</returns>
    private int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            throw new UnauthorizedAccessException();
        }
        return userId;
    }
}
