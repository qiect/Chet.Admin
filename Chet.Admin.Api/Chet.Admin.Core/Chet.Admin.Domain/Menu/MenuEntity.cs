using Chet.Admin.Domain.Role;

namespace Chet.Admin.Domain.Menu
{
    /// <summary>
    /// 菜单实体
    /// </summary>
    public class MenuEntity : BaseEntity
    {
        /// <summary>
        /// 菜单名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 菜单路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 组件路径
        /// </summary>
        public string? Component { get; set; }

        /// <summary>
        /// 重定向路径
        /// </summary>
        public string? Redirect { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 父菜单ID（0表示顶级菜单）
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// 菜单类型（Directory=目录, Menu=菜单, Button=按钮, Api=接口）
        /// </summary>
        public string Type { get; set; } = "Menu";

        /// <summary>
        /// 排序号
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 是否外链
        /// </summary>
        public bool IsExternal { get; set; }

        /// <summary>
        /// 是否缓存
        /// </summary>
        public bool IsCache { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 权限标识
        /// </summary>
        public string? Permission { get; set; }

        /// <summary>
        /// 权限/按钮描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 子菜单
        /// </summary>
        public List<MenuEntity> Children { get; set; } = [];

        /// <summary>
        /// 角色菜单关联
        /// </summary>
        public List<RoleMenuEntity> RoleMenus { get; set; } = [];
    }
}
