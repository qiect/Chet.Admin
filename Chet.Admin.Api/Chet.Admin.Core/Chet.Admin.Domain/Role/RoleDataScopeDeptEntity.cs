using Chet.Admin.Domain.Department;

namespace Chet.Admin.Domain.Role;

/// <summary>
/// 角色-自定义数据权限部门关联
/// </summary>
public class RoleDataScopeDeptEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int DepartmentId { get; set; }

    /// <summary>
    /// 导航属性
    /// </summary>
    public RoleEntity Role { get; set; } = null!;
    public DepartmentEntity Department { get; set; } = null!;
}
