using Chet.Admin.DTOs.Notification;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Notification;

public interface INotificationService
{
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, int? senderId = null);
    Task<PagedResult<NotificationDto>> GetPagedNotificationsAsync(PagedRequest request);
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int userId, PagedRequest request);
    Task<UnreadCountDto> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task DeleteNotificationAsync(int id);
}
