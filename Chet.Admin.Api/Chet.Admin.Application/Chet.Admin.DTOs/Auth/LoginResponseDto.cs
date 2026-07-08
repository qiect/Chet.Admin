namespace Chet.Admin.DTOs.Auth;

/// <summary>
/// 登录响应数据传输对象，包含令牌和锁定状态信息
/// </summary>
public class LoginResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    /// <summary>是否需要验证码（连续失败次数 >= 3 时需要）</summary>
    public bool RequireCaptcha { get; set; }

    /// <summary>锁定截止时间</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>是否需要强制修改密码（密码过期时为true）</summary>
    public bool MustChangePassword { get; set; }
}
