using Chet.Admin.DTOs.Department;
using Chet.Admin.Shared;

namespace Chet.Admin.Contracts.Department
{
    public interface IDepartmentService
    {
        Task<DepartmentDto> GetDepartmentByIdAsync(int id);
        Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync();
        Task<IEnumerable<DepartmentTreeDto>> GetDepartmentTreeAsync();
        Task<PagedResult<DepartmentDto>> GetPagedDepartmentsAsync(PagedRequest request);
        Task<DepartmentDto> CreateDepartmentAsync(DepartmentCreateDto dto);
        Task UpdateDepartmentAsync(int id, DepartmentUpdateDto dto);
        Task DeleteDepartmentAsync(int id);
    }
}
