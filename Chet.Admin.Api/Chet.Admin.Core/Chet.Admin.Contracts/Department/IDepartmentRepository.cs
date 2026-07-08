using Chet.Admin.Contracts;
using Chet.Admin.Domain.Department;

namespace Chet.Admin.Contracts.Department
{
    /// <summary>
    /// 部门仓储接口
    /// </summary>
    public interface IDepartmentRepository : IRepository<DepartmentEntity>
    {
        /// <summary>
        /// 根据部门编码获取部门
        /// </summary>
        /// <param name="code">部门编码</param>
        /// <returns>部门实体，不存在则返回null</returns>
        Task<DepartmentEntity?> GetByCodeAsync(string code);

        /// <summary>
        /// 根据父级ID获取子部门列表
        /// </summary>
        /// <param name="parentId">父级部门ID</param>
        /// <returns>子部门实体集合</returns>
        Task<IEnumerable<DepartmentEntity>> GetByParentIdAsync(int parentId);
    }
}
