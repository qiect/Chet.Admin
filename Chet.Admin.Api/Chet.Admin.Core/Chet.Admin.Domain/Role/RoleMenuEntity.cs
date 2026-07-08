using Chet.Admin.Domain.Menu;

namespace Chet.Admin.Domain.Role
{
    /// <summary>
    /// 角色菜单关联实体
    /// </summary>
    public class RoleMenuEntity
    {
        public int RoleId { get; set; }
        public int MenuId { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public RoleEntity Role { get; set; } = null!;
        public MenuEntity Menu { get; set; } = null!;
    }
}
