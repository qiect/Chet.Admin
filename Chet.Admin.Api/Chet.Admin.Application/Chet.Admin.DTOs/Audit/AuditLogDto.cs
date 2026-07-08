namespace Chet.Admin.DTOs.Audit;

/// <summary>
/// 审计日志DTO
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// 审计日志ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 操作
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 模块
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 目标ID
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// HTTP方法
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// 请求路径
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 请求数据
    /// </summary>
    public string? RequestData { get; set; }

    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperatedAt { get; set; }
}
