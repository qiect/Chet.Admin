using Chet.Admin.DTOs.Menu;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Menu
{
    /// <summary>
    /// 菜单服务接口
    /// </summary>
    public interface IMenuService
    {
        /// <summary>
        /// 根据ID获取菜单信息
        /// </summary>
        /// <param name="id">菜单ID</param>
        /// <returns>菜单DTO</returns>
        Task<MenuDto> GetMenuByIdAsync(int id);

        /// <summary>
        /// 获取所有菜单
        /// </summary>
        /// <returns>菜单DTO集合</returns>
        Task<IEnumerable<MenuDto>> GetAllMenusAsync();

        /// <summary>
        /// 获取菜单树形结构
        /// </summary>
        /// <returns>菜单树DTO集合</returns>
        Task<IEnumerable<MenuTreeDto>> GetMenuTreeAsync();

        /// <summary>
        /// 分页获取菜单列表
        /// </summary>
        /// <param name="request">分页请求参数</param>
        /// <returns>菜单分页结果</returns>
        Task<PagedResult<MenuDto>> GetPagedMenusAsync(PagedRequest request);

        /// <summary>
        /// 创建菜单
        /// </summary>
        /// <param name="dto">菜单创建DTO</param>
        /// <returns>创建的菜单DTO</returns>
        Task<MenuDto> CreateMenuAsync(MenuCreateDto dto);

        /// <summary>
        /// 更新菜单信息
        /// </summary>
        /// <param name="id">菜单ID</param>
        /// <param name="dto">菜单更新DTO</param>
        Task UpdateMenuAsync(int id, MenuUpdateDto dto);

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <param name="id">菜单ID</param>
        Task DeleteMenuAsync(int id);
    }
}
