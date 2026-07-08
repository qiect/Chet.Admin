namespace Chet.Admin.DTOs.Role;

/// <summary>
/// 更新数据权限范围DTO
/// </summary>
public class UpdateDataScopeDto
{
    /// <summary>
    /// 数据权限范围
    /// </summary>
    public required string DataScope { get; set; }

    /// <summary>
    /// 自定义部门ID列表
    /// </summary>
    public List<int>? CustomDeptIds { get; set; }
}
