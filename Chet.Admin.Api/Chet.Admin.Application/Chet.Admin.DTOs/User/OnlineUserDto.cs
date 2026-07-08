namespace Chet.Admin.DTOs.User;

/// <summary>
/// 在线用户DTO
/// </summary>
public class OnlineUserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTime LoginTime { get; set; }

    /// <summary>
    /// 最后活跃时间
    /// </summary>
    public DateTime LastActiveTime { get; set; }
}
