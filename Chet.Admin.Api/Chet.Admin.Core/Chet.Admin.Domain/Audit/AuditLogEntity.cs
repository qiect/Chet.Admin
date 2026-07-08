namespace Chet.Admin.Domain.Audit;

/// <summary>
/// 操作日志实体
/// </summary>
public class AuditLogEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 操作用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 操作用户名称
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 操作动作（如 Create/Update/Delete）
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 操作模块
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 操作目标对象ID
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// HTTP请求方法（GET/POST/PUT/DELETE等）
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// 请求路径
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 请求数据（JSON格式）
    /// </summary>
    public string? RequestData { get; set; }

    /// <summary>
    /// HTTP响应状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// 客户端User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 操作耗时（毫秒）
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// 操作时间（UTC）
    /// </summary>
    public DateTime OperatedAt { get; set; } = DateTime.UtcNow;
}
