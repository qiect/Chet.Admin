using Chet.Admin.Contracts.Department;
using Chet.Admin.Domain.Department;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Department;

public class DepartmentRepository : EfCoreRepository<DepartmentEntity>, IDepartmentRepository
{
    public DepartmentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<DepartmentEntity?> GetByCodeAsync(string code)
    {
        return await _dbContext.Departments.FirstOrDefaultAsync(d => d.Code == code);
    }

    public async Task<IEnumerable<DepartmentEntity>> GetByParentIdAsync(int parentId)
    {
        return await _dbContext.Departments
            .Where(d => d.ParentId == parentId)
            .OrderBy(d => d.Sort)
            .ToListAsync();
    }
}
