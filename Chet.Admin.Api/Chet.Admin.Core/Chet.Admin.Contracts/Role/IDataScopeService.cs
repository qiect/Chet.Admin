namespace Chet.Admin.Contracts.Role;

/// <summary>
/// 数据权限范围服务接口
/// </summary>
public interface IDataScopeService
{
    /// <summary>
    /// 获取用户可访问的部门ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>可访问的部门ID列表</returns>
    Task<List<int>> GetAccessibleDeptIdsAsync(int userId);

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>数据权限范围标识</returns>
    Task<string> GetDataScopeAsync(int userId);
}
