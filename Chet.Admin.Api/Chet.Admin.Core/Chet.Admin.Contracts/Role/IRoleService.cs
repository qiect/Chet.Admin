using Chet.Admin.DTOs.Menu;
using Chet.Admin.DTOs.Permission;
using Chet.Admin.DTOs.Role;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Role
{
    /// <summary>
    /// 角色服务接口
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// 根据ID获取角色信息
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns>角色DTO</returns>
        Task<RoleDto> GetRoleByIdAsync(int id);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        /// <returns>角色DTO集合</returns>
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();

        /// <summary>
        /// 分页获取角色列表
        /// </summary>
        /// <param name="request">分页请求参数</param>
        /// <returns>角色分页结果</returns>
        Task<PagedResult<RoleDto>> GetPagedRolesAsync(PagedRequest request);

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="dto">角色创建DTO</param>
        /// <returns>创建的角色DTO</returns>
        Task<RoleDto> CreateRoleAsync(RoleCreateDto dto);

        /// <summary>
        /// 更新角色信息
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <param name="dto">角色更新DTO</param>
        Task UpdateRoleAsync(int id, RoleUpdateDto dto);

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="id">角色ID</param>
        Task DeleteRoleAsync(int id);

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionIds">权限ID列表</param>
        Task AssignPermissionsAsync(int roleId, List<int> permissionIds);

        /// <summary>
        /// 为角色分配菜单
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="menuIds">菜单ID列表</param>
        Task AssignMenusAsync(int roleId, List<int> menuIds);

        /// <summary>
        /// 获取角色拥有的权限列表
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>权限DTO集合</returns>
        Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(int roleId);

        /// <summary>
        /// 获取角色拥有的菜单列表
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>菜单DTO集合</returns>
        Task<IEnumerable<MenuDto>> GetRoleMenusAsync(int roleId);

        /// <summary>
        /// 更新角色数据权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="dataScope">数据权限范围</param>
        /// <param name="customDeptIds">自定义部门ID列表</param>
        Task UpdateDataScopeAsync(int roleId, string dataScope, List<int>? customDeptIds);
    }
}
