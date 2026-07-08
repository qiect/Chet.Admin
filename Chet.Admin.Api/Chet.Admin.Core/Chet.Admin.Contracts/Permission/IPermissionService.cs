using Chet.Admin.DTOs.Permission;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Permission
{
    /// <summary>
    /// 权限服务接口
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 根据ID获取权限信息
        /// </summary>
        /// <param name="id">权限ID</param>
        /// <returns>权限DTO</returns>
        Task<PermissionDto> GetPermissionByIdAsync(int id);

        /// <summary>
        /// 获取所有权限
        /// </summary>
        /// <returns>权限DTO集合</returns>
        Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();

        /// <summary>
        /// 分页获取权限列表
        /// </summary>
        /// <param name="request">分页请求参数</param>
        /// <returns>权限分页结果</returns>
        Task<PagedResult<PermissionDto>> GetPagedPermissionsAsync(PagedRequest request);

        /// <summary>
        /// 创建权限
        /// </summary>
        /// <param name="dto">权限创建DTO</param>
        /// <returns>创建的权限DTO</returns>
        Task<PermissionDto> CreatePermissionAsync(PermissionCreateDto dto);

        /// <summary>
        /// 更新权限信息
        /// </summary>
        /// <param name="id">权限ID</param>
        /// <param name="dto">权限更新DTO</param>
        Task UpdatePermissionAsync(int id, PermissionUpdateDto dto);

        /// <summary>
        /// 删除权限
        /// </summary>
        /// <param name="id">权限ID</param>
        Task DeletePermissionAsync(int id);
    }
}
