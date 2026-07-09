namespace Chet.Admin.DTOs.Notification;

/// <summary>
/// 通知DTO
/// </summary>
public class NotificationDto
{
    /// <summary>
    /// 通知ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 通知类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 优先级
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// 发送者ID
    /// </summary>
    public int? SenderId { get; set; }

    /// <summary>
    /// 发送者用户名（null表示系统发送）
    /// </summary>
    public string? SenderName { get; set; }

    /// <summary>
    /// 是否全局通知
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建通知DTO
/// </summary>
public class CreateNotificationDto
{
    /// <summary>
    /// 标题
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public string Type { get; set; } = "Notification";

    /// <summary>
    /// 优先级
    /// </summary>
    public string Priority { get; set; } = "Normal";

    /// <summary>
    /// 是否全局通知
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 接收用户ID列表
    /// </summary>
    public List<int>? RecipientUserIds { get; set; }
}

/// <summary>
/// 未读数量DTO
/// </summary>
public class UnreadCountDto
{
    /// <summary>
    /// 未读数量
    /// </summary>
    public int Count { get; set; }
}
