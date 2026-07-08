using Chet.Admin.Domain.Department;

namespace Chet.Admin.Domain.Role;

/// <summary>
/// 角色-自定义数据权限部门关联实体
/// </summary>
public class RoleDataScopeDeptEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// 导航属性-角色
    /// </summary>
    public RoleEntity Role { get; set; } = null!;

    /// <summary>
    /// 导航属性-部门
    /// </summary>
    public DepartmentEntity Department { get; set; } = null!;
}
