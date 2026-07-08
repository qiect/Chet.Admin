using Chet.Admin.Domain.Role;

namespace Chet.Admin.Domain.Permission
{
    /// <summary>
    /// 权限实体
    /// </summary>
    public class PermissionEntity : BaseEntity
    {
        /// <summary>
        /// 权限编码（如 user:create, user:delete）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限类型（Menu=菜单, Button=按钮, Api=接口）
        /// </summary>
        public string Type { get; set; } = "Button";

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 关联菜单ID
        /// </summary>
        public int? MenuId { get; set; }

        /// <summary>
        /// 角色权限关联
        /// </summary>
        public List<RolePermissionEntity> RolePermissions { get; set; } = [];
    }
}
