using Chet.Admin.Contracts;
using Chet.Admin.Domain.Menu;

namespace Chet.Admin.Contracts.Menu
{
    /// <summary>
    /// 菜单仓储接口
    /// </summary>
    public interface IMenuRepository : IRepository<MenuEntity>
    {
        /// <summary>
        /// 根据角色ID获取菜单列表
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>菜单实体集合</returns>
        Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId);

        /// <summary>
        /// 根据用户ID获取菜单列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>菜单实体集合</returns>
        Task<IEnumerable<MenuEntity>> GetMenusByUserIdAsync(int userId);

        /// <summary>
        /// 根据父级ID获取子菜单列表
        /// </summary>
        /// <param name="parentId">父级菜单ID</param>
        /// <returns>菜单实体集合</returns>
        Task<IEnumerable<MenuEntity>> GetMenusByParentIdAsync(int parentId);
    }
}
