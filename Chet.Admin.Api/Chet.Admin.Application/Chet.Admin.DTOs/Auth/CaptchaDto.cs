namespace Chet.Admin.DTOs.Auth;

/// <summary>
/// 验证码DTO
/// </summary>
public class CaptchaDto
{
    /// <summary>
    /// 验证码ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 验证码SVG图片
    /// </summary>
    public string Svg { get; set; } = string.Empty;
}
