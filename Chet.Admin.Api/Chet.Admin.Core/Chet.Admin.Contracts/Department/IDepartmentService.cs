using Chet.Admin.DTOs.Department;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Department
{
    /// <summary>
    /// 部门服务接口
    /// </summary>
    public interface IDepartmentService
    {
        /// <summary>
        /// 根据ID获取部门信息
        /// </summary>
        /// <param name="id">部门ID</param>
        /// <returns>部门DTO</returns>
        Task<DepartmentDto> GetDepartmentByIdAsync(int id);

        /// <summary>
        /// 获取所有部门
        /// </summary>
        /// <returns>部门DTO集合</returns>
        Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync();

        /// <summary>
        /// 获取部门树形结构
        /// </summary>
        /// <returns>部门树DTO集合</returns>
        Task<IEnumerable<DepartmentTreeDto>> GetDepartmentTreeAsync();

        /// <summary>
        /// 分页获取部门列表
        /// </summary>
        /// <param name="request">分页请求参数</param>
        /// <returns>部门分页结果</returns>
        Task<PagedResult<DepartmentDto>> GetPagedDepartmentsAsync(PagedRequest request);

        /// <summary>
        /// 创建部门
        /// </summary>
        /// <param name="dto">部门创建DTO</param>
        /// <returns>创建的部门DTO</returns>
        Task<DepartmentDto> CreateDepartmentAsync(DepartmentCreateDto dto);

        /// <summary>
        /// 更新部门信息
        /// </summary>
        /// <param name="id">部门ID</param>
        /// <param name="dto">部门更新DTO</param>
        Task UpdateDepartmentAsync(int id, DepartmentUpdateDto dto);

        /// <summary>
        /// 删除部门
        /// </summary>
        /// <param name="id">部门ID</param>
        Task DeleteDepartmentAsync(int id);
    }
}
