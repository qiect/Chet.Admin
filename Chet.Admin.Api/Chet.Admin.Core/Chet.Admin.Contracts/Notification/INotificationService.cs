using Chet.Admin.DTOs.Notification;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Notification;

/// <summary>
/// 通知服务接口
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 创建通知
    /// </summary>
    /// <param name="dto">创建通知DTO</param>
    /// <param name="senderId">发送者ID</param>
    /// <returns>通知DTO</returns>
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, int? senderId = null);

    /// <summary>
    /// 分页获取通知列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>通知分页结果</returns>
    Task<PagedResult<NotificationDto>> GetPagedNotificationsAsync(PagedRequest request);

    /// <summary>
    /// 分页获取当前用户的通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">分页请求参数</param>
    /// <returns>通知分页结果</returns>
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int userId, PagedRequest request);

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>未读数量DTO</returns>
    Task<UnreadCountDto> GetUnreadCountAsync(int userId);

    /// <summary>
    /// 将指定通知标记为已读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="userId">用户ID</param>
    Task MarkAsReadAsync(int notificationId, int userId);

    /// <summary>
    /// 将用户所有通知标记为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task MarkAllAsReadAsync(int userId);

    /// <summary>
    /// 删除通知
    /// </summary>
    /// <param name="id">通知ID</param>
    Task DeleteNotificationAsync(int id);
}
