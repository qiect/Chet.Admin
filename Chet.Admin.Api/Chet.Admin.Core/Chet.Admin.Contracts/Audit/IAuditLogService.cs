using Chet.Admin.DTOs.Audit;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Audit;

/// <summary>
/// 审计日志服务接口
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// 分页获取审计日志
    /// </summary>
    /// <param name="request">审计日志分页请求参数</param>
    /// <returns>审计日志分页结果</returns>
    Task<PagedResult<AuditLogDto>> GetPagedAuditLogsAsync(AuditLogPagedRequest request);

    /// <summary>
    /// 记录审计日志
    /// </summary>
    /// <param name="auditLog">审计日志DTO</param>
    Task LogAsync(AuditLogDto auditLog);

    /// <summary>
    /// 清除指定时间之前的审计日志
    /// </summary>
    /// <param name="before">截止时间，早于此时间的日志将被清除</param>
    Task ClearBeforeAsync(DateTime before);
}

/// <summary>
/// 审计日志分页请求参数
/// </summary>
public class AuditLogPagedRequest : PagedRequest
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// 起始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
}
