namespace Chet.Admin.DTOs.Notification;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int? SenderId { get; set; }
    public bool IsGlobal { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationDto
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public string Type { get; set; } = "Notification";
    public string Priority { get; set; } = "Normal";
    public bool IsGlobal { get; set; }
    public List<int>? RecipientUserIds { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}
