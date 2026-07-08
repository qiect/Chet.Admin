using AutoMapper;
using Chet.Admin.Contracts.Notification;
using Chet.Admin.Data;
using Chet.Admin.Domain.Notification;
using Chet.Admin.DTOs.Notification;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Notification;

/// <summary>
/// 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext dbContext, IMapper mapper, ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 创建通知
    /// </summary>
    /// <param name="dto">通知创建信息</param>
    /// <param name="senderId">发送者ID，可为空</param>
    /// <returns>创建后的通知数据传输对象</returns>
    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, int? senderId = null)
    {
        _logger.LogInformation("Creating notification: {Title}", dto.Title);

        var entity = new NotificationEntity
        {
            Title = dto.Title,
            Content = dto.Content,
            Type = dto.Type,
            Priority = dto.Priority,
            IsGlobal = dto.IsGlobal,
            SenderId = senderId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(entity);
        await _dbContext.SaveChangesAsync();

        // If not global, create recipient records
        if (!dto.IsGlobal && dto.RecipientUserIds is { Count: > 0 })
        {
            foreach (var userId in dto.RecipientUserIds)
            {
                _dbContext.NotificationRecipients.Add(new NotificationRecipientEntity
                {
                    NotificationId = entity.Id,
                    UserId = userId,
                    IsRead = false
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        return _mapper.Map<NotificationDto>(entity);
    }

    /// <summary>
    /// 分页查询通知列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页通知列表</returns>
    public async Task<PagedResult<NotificationDto>> GetPagedNotificationsAsync(PagedRequest request)
    {
        _logger.LogInformation("Getting paged notifications: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);
        request.Normalize();

        var query = _dbContext.Notifications.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<NotificationDto>>(items);
        return new PagedResult<NotificationDto>(dtos, request.PageNumber, request.PageSize, totalCount);
    }

    /// <summary>
    /// 获取当前用户的通知列表（包含全局通知和指定接收者的通知）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页通知列表，包含已读状态</returns>
    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(int userId, PagedRequest request)
    {
        _logger.LogInformation("Getting my notifications for user: {UserId}", userId);
        request.Normalize();

        // Global notifications + notifications where user is a recipient
        var query = _dbContext.Notifications.AsNoTracking()
            .Where(n => n.IsGlobal || _dbContext.NotificationRecipients.Any(r => r.NotificationId == n.Id && r.UserId == userId));

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(n => n.Title.Contains(keyword) || n.Content.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<NotificationDto>>(items);

        // Set IsRead for each notification
        foreach (var dto in dtos)
        {
            if (dto.IsGlobal)
            {
                // For global notifications, check if there's a recipient record marked as read
                var recipient = await _dbContext.NotificationRecipients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.NotificationId == dto.Id && r.UserId == userId);
                dto.IsRead = recipient?.IsRead ?? false;
            }
            else
            {
                var recipient = await _dbContext.NotificationRecipients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.NotificationId == dto.Id && r.UserId == userId);
                dto.IsRead = recipient?.IsRead ?? true;
            }
        }

        return new PagedResult<NotificationDto>(dtos, request.PageNumber, request.PageSize, totalCount);
    }

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>未读通知数量</returns>
    public async Task<UnreadCountDto> GetUnreadCountAsync(int userId)
    {
        _logger.LogInformation("Getting unread count for user: {UserId}", userId);

        // Count global notifications that are unread (no recipient record or not marked as read)
        var globalUnread = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.IsGlobal && !_dbContext.NotificationRecipients.Any(r => r.NotificationId == n.Id && r.UserId == userId && r.IsRead))
            .CountAsync();

        // Count non-global notifications where user is a recipient and not read
        var personalUnread = await _dbContext.NotificationRecipients
            .AsNoTracking()
            .Where(r => r.UserId == userId && !r.IsRead && !_dbContext.Notifications.Any(n => n.Id == r.NotificationId && n.IsGlobal))
            .CountAsync();

        return new UnreadCountDto { Count = globalUnread + personalUnread };
    }

    /// <summary>
    /// 将指定通知标记为已读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="userId">用户ID</param>
    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}", notificationId, userId);

        var notification = await _dbContext.Notifications.FindAsync(notificationId)
            ?? throw new NotFoundException(nameof(NotificationEntity), notificationId);

        var recipient = await _dbContext.NotificationRecipients
            .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.UserId == userId);

        if (recipient == null)
        {
            // Create a recipient record (for global notifications first read)
            _dbContext.NotificationRecipients.Add(new NotificationRecipientEntity
            {
                NotificationId = notificationId,
                UserId = userId,
                IsRead = true,
                ReadAt = DateTime.UtcNow
            });
        }
        else if (!recipient.IsRead)
        {
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 将用户所有未读通知标记为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    public async Task MarkAllAsReadAsync(int userId)
    {
        _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);

        // Mark all personal unread notifications as read
        var unreadRecipients = await _dbContext.NotificationRecipients
            .Where(r => r.UserId == userId && !r.IsRead)
            .ToListAsync();

        foreach (var recipient in unreadRecipients)
        {
            recipient.IsRead = true;
            recipient.ReadAt = DateTime.UtcNow;
        }

        // For global notifications without recipient record, create one
        var globalNotificationIds = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.IsGlobal)
            .Select(n => n.Id)
            .ToListAsync();

        var existingRecipientNotificationIds = await _dbContext.NotificationRecipients
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => r.NotificationId)
            .ToListAsync();

        var missingGlobalIds = globalNotificationIds.Except(existingRecipientNotificationIds).ToList();

        foreach (var notificationId in missingGlobalIds)
        {
            _dbContext.NotificationRecipients.Add(new NotificationRecipientEntity
            {
                NotificationId = notificationId,
                UserId = userId,
                IsRead = true,
                ReadAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 删除通知及其关联的接收者记录
    /// </summary>
    /// <param name="id">通知ID</param>
    public async Task DeleteNotificationAsync(int id)
    {
        _logger.LogInformation("Deleting notification: {Id}", id);

        var notification = await _dbContext.Notifications.FindAsync(id)
            ?? throw new NotFoundException(nameof(NotificationEntity), id);

        // Delete related recipients
        var recipients = await _dbContext.NotificationRecipients
            .Where(r => r.NotificationId == id)
            .ToListAsync();
        _dbContext.NotificationRecipients.RemoveRange(recipients);

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync();
    }
}
