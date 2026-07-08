using Chet.Admin.DTOs.Permission;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Permission
{
    public interface IPermissionService
    {
        Task<PermissionDto> GetPermissionByIdAsync(int id);
        Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();
        Task<PagedResult<PermissionDto>> GetPagedPermissionsAsync(PagedRequest request);
        Task<PermissionDto> CreatePermissionAsync(PermissionCreateDto dto);
        Task UpdatePermissionAsync(int id, PermissionUpdateDto dto);
        Task DeletePermissionAsync(int id);
    }
}
