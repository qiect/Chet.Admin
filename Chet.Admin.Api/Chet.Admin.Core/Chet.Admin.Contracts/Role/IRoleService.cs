using Chet.Admin.DTOs.Menu;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.DTOs.Role;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Role
{
    public interface IRoleService
    {
        Task<RoleDto> GetRoleByIdAsync(int id);
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<PagedResult<RoleDto>> GetPagedRolesAsync(PagedRequest request);
        Task<RoleDto> CreateRoleAsync(RoleCreateDto dto);
        Task UpdateRoleAsync(int id, RoleUpdateDto dto);
        Task DeleteRoleAsync(int id);
        Task AssignPermissionsAsync(int roleId, List<int> permissionIds);
        Task AssignMenusAsync(int roleId, List<int> menuIds);
        Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(int roleId);
        Task<IEnumerable<MenuDto>> GetRoleMenusAsync(int roleId);

        /// <summary>
        /// 更新角色数据权限
        /// </summary>
        Task UpdateDataScopeAsync(int roleId, string dataScope, List<int>? customDeptIds);
    }
}
