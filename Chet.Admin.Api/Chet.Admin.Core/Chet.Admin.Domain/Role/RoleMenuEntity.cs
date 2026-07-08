using Chet.Admin.Domain.Menu;

namespace Chet.Admin.Domain.Role
{
    /// <summary>
    /// 角色菜单关联实体
    /// </summary>
    public class RoleMenuEntity
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// 菜单ID
        /// </summary>
        public int MenuId { get; set; }

        /// <summary>
        /// 导航属性-角色
        /// </summary>
        public RoleEntity Role { get; set; } = null!;

        /// <summary>
        /// 导航属性-菜单
        /// </summary>
        public MenuEntity Menu { get; set; } = null!;
    }
}
