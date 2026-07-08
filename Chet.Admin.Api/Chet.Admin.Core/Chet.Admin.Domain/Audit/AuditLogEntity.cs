namespace Chet.Admin.Domain.Audit;

/// <summary>
/// 操作日志实体
/// </summary>
public class AuditLogEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string? RequestData { get; set; }
    public int StatusCode { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public long Duration { get; set; }
    public DateTime OperatedAt { get; set; } = DateTime.UtcNow;
}
