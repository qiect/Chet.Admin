using Chet.Admin.Domain.Permission;

namespace Chet.Admin.Domain.Role
{
    /// <summary>
    /// 角色权限关联实体
    /// </summary>
    public class RolePermissionEntity
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public RoleEntity Role { get; set; } = null!;
        public PermissionEntity Permission { get; set; } = null!;
    }
}
