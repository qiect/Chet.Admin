using Chet.Admin.DTOs.Menu;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Menu
{
    public interface IMenuService
    {
        Task<MenuDto> GetMenuByIdAsync(int id);
        Task<IEnumerable<MenuDto>> GetAllMenusAsync();
        Task<IEnumerable<MenuTreeDto>> GetMenuTreeAsync();
        Task<PagedResult<MenuDto>> GetPagedMenusAsync(PagedRequest request);
        Task<MenuDto> CreateMenuAsync(MenuCreateDto dto);
        Task UpdateMenuAsync(int id, MenuUpdateDto dto);
        Task DeleteMenuAsync(int id);
    }
}
