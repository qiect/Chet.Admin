namespace Chet.Admin.Domain.Notification;

public class NotificationEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "Notification"; // Announcement/Notification/Todo
    public string Priority { get; set; } = "Normal";   // Low/Normal/High/Urgent
    public int? SenderId { get; set; }
    public bool IsGlobal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class NotificationRecipientEntity
{
    public int Id { get; set; }
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
