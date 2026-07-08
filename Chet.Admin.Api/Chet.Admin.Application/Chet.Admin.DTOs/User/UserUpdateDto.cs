using System.ComponentModel.DataAnnotations;

namespace Chet.Admin.DTOs.User;

/// <summary>
/// 用户更新数据传输对象，用于接收更新用户信息的请求
/// 所有字段均为可选，仅更新提供了值的字段
/// </summary>
public class UserUpdateDto
{
    /// <summary>
    /// 用户名（可选），如果提供则更新
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 用户邮箱（可选），如果提供则更新
    /// </summary>
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    /// <summary>
    /// 密码（可选），如果提供则更新密码
    /// </summary>
    [MinLength(6, ErrorMessage = "密码至少6位")]
    public string? Password { get; set; }

    /// <summary>
    /// 部门ID（可选）
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// 角色ID列表（可选），如果提供则重新分配角色
    /// </summary>
    public List<int>? RoleIds { get; set; }
}
