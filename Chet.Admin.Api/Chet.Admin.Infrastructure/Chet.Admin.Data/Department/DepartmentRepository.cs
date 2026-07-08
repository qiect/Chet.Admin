using Chet.Admin.Contracts.Department;
using Chet.Admin.Domain.Department;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.Department;

/// <summary>
/// 部门仓储实现类
/// </summary>
public class DepartmentRepository : EfCoreRepository<DepartmentEntity>, IDepartmentRepository
{
    /// <summary>
    /// 初始化部门仓储实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public DepartmentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据部门编码查询部门
    /// </summary>
    /// <param name="code">部门编码</param>
    /// <returns>部门实体，不存在返回null</returns>
    public async Task<DepartmentEntity?> GetByCodeAsync(string code)
    {
        return await _dbContext.Departments.FirstOrDefaultAsync(d => d.Code == code);
    }

    /// <summary>
    /// 根据父部门ID查询子部门列表
    /// </summary>
    /// <param name="parentId">父部门ID</param>
    /// <returns>子部门实体列表（按排序号排序）</returns>
    public async Task<IEnumerable<DepartmentEntity>> GetByParentIdAsync(int parentId)
    {
        return await _dbContext.Departments
            .Where(d => d.ParentId == parentId)
            .OrderBy(d => d.Sort)
            .ToListAsync();
    }
}
