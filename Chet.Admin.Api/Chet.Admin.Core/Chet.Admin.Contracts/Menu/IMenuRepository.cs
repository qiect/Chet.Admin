using Chet.Admin.Contracts;
using Chet.Admin.Domain.Menu;

namespace Chet.Admin.Contracts.Menu
{
    public interface IMenuRepository : IRepository<MenuEntity>
    {
        Task<IEnumerable<MenuEntity>> GetMenusByRoleIdAsync(int roleId);
        Task<IEnumerable<MenuEntity>> GetMenusByUserIdAsync(int userId);
        Task<IEnumerable<MenuEntity>> GetMenusByParentIdAsync(int parentId);
    }
}
