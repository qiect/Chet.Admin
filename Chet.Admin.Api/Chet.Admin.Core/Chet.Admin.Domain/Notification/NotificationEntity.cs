namespace Chet.Admin.Domain.Notification;

/// <summary>
/// 通知公告实体
/// </summary>
public class NotificationEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 通知类型（Announcement=公告/Notification=通知/Todo=待办）
    /// </summary>
    public string Type { get; set; } = "Notification";

    /// <summary>
    /// 优先级（Low/Normal/High/Urgent）
    /// </summary>
    public string Priority { get; set; } = "Normal";

    /// <summary>
    /// 发送人用户ID（null表示系统发送）
    /// </summary>
    public int? SenderId { get; set; }

    /// <summary>
    /// 是否全局通知（true表示所有用户可见）
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 通知接收人关联实体
/// </summary>
public class NotificationRecipientEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 通知ID
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
