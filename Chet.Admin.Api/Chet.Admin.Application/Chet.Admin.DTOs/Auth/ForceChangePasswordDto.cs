using System.ComponentModel.DataAnnotations;

namespace Chet.Admin.DTOs.Auth;

/// <summary>
/// 强制修改密码数据传输对象（密码过期时使用）
/// </summary>
public class ForceChangePasswordDto
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessage = "旧密码不能为空")]
    public required string OldPassword { get; set; }

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码至少6位")]
    public required string NewPassword { get; set; }
}
