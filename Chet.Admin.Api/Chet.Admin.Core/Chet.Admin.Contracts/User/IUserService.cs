using Chet.Admin.DTOs.User;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.User
{
    /// <summary>
    /// 用户服务接口，定义了用户相关的业务逻辑操作
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 根据用户ID获取用户信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>用户DTO</returns>
        Task<UserDto> GetUserByIdAsync(int id);

        /// <summary>
        /// 获取所有用户信息
        /// </summary>
        /// <returns>用户DTO列表</returns>
        Task<IEnumerable<UserDto>> GetAllUsersAsync();

        /// <summary>
        /// 分页获取用户信息
        /// </summary>
        /// <param name="request">分页请求参数</param>
        /// <returns>分页用户结果</returns>
        Task<PagedResult<UserDto>> GetPagedUsersAsync(PagedRequest request);

        /// <summary>
        /// 分页获取用户信息（应用数据权限过滤）
        /// </summary>
        /// <param name="request">分页请求参数</param>
        /// <param name="currentUserId">当前用户ID，用于数据权限过滤</param>
        /// <returns>分页用户结果</returns>
        Task<PagedResult<UserDto>> GetPagedUsersAsync(PagedRequest request, int? currentUserId);

        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="userCreateDto">用户创建DTO</param>
        /// <returns>创建的用户DTO</returns>
        Task<UserDto> CreateUserAsync(UserCreateDto userCreateDto);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="userUpdateDto">用户更新DTO</param>
        Task UpdateUserAsync(int id, UserUpdateDto userUpdateDto);

        /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户ID</param>
    Task DeleteUserAsync(int id);

    /// <summary>
    /// 分配角色给用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    Task AssignRolesAsync(int userId, List<int> roleIds);

    /// <summary>
    /// 创建用户并分配角色
    /// </summary>
    /// <param name="dto">用户创建DTO</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <returns>创建的用户DTO</returns>
    Task<UserDto> CreateUserWithRolesAsync(UserCreateDto dto, List<int> roleIds);

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="oldPassword">旧密码</param>
    /// <param name="newPassword">新密码</param>
    Task ChangePasswordAsync(int userId, string oldPassword, string newPassword);

    /// <summary>
    /// 更新用户个人资料
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="name">用户名</param>
    /// <param name="avatar">头像URL</param>
    Task UpdateProfileAsync(int userId, string? name, string? avatar);
    }
}
