using Chet.Admin.Contracts;
using Chet.Admin.Domain.Department;

namespace Chet.Admin.Contracts.Department
{
    public interface IDepartmentRepository : IRepository<DepartmentEntity>
    {
        Task<DepartmentEntity?> GetByCodeAsync(string code);
        Task<IEnumerable<DepartmentEntity>> GetByParentIdAsync(int parentId);
    }
}
