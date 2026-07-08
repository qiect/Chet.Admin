namespace Chet.Admin.Domain.Role
{
    /// <summary>
    /// 角色实体
    /// </summary>
    public class RoleEntity : BaseEntity
    {
        /// <summary>
        /// 角色编码（如 admin, user）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 数据权限范围：All/Dept/DeptAndChild/Self/Custom
        /// </summary>
        public string DataScope { get; set; } = "All";

        /// <summary>
        /// 用户角色关联
        /// </summary>
        public List<UserRoleEntity> UserRoles { get; set; } = [];

        /// <summary>
        /// 角色权限关联
        /// </summary>
        public List<RolePermissionEntity> RolePermissions { get; set; } = [];

        /// <summary>
        /// 角色菜单关联
        /// </summary>
        public List<RoleMenuEntity> RoleMenus { get; set; } = [];

        /// <summary>
        /// 角色自定义数据权限部门关联
        /// </summary>
        public List<RoleDataScopeDeptEntity> RoleDataScopeDepts { get; set; } = [];
    }
}
