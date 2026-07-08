namespace Chet.Admin.DTOs.Auth;

/// <summary>
/// 更新个人资料DTO
/// </summary>
public class UpdateProfileDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }
}
