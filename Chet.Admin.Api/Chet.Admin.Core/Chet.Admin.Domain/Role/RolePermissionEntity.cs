using Chet.Admin.Domain.Permission;

namespace Chet.Admin.Domain.Role
{
    /// <summary>
    /// 角色权限关联实体
    /// </summary>
    public class RolePermissionEntity
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// 权限ID
        /// </summary>
        public int PermissionId { get; set; }

        /// <summary>
        /// 导航属性-角色
        /// </summary>
        public RoleEntity Role { get; set; } = null!;

        /// <summary>
        /// 导航属性-权限
        /// </summary>
        public PermissionEntity Permission { get; set; } = null!;
    }
}
