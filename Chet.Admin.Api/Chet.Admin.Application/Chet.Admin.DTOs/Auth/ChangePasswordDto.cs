using System.ComponentModel.DataAnnotations;

namespace Chet.Admin.DTOs.Auth;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "旧密码不能为空")]
    public required string OldPassword { get; set; }

    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码至少6位")]
    public required string NewPassword { get; set; }
}
