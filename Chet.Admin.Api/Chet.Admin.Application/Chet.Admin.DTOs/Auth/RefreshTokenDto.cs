namespace Chet.Admin.DTOs.Auth;

/// <summary>
/// 刷新令牌数据传输对象，用于接收令牌刷新请求
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// 当前过期的访问令牌
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// 用于获取新访问令牌的刷新令牌
    /// </summary>
    public required string RefreshToken { get; set; }
}
