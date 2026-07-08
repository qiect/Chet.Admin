using Chet.Admin.DTOs.Audit;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Audit;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAuditLogsAsync(AuditLogPagedRequest request);
    Task LogAsync(AuditLogDto auditLog);
    Task ClearBeforeAsync(DateTime before);
}

public class AuditLogPagedRequest : PagedRequest
{
    public int? UserId { get; set; }
    public string? Module { get; set; }
    public string? Action { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
