namespace Chet.Admin.Contracts.Role;

public interface IDataScopeService
{
    /// <summary>
    /// 获取用户可访问的部门ID列表
    /// </summary>
    Task<List<int>> GetAccessibleDeptIdsAsync(int userId);

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    Task<string> GetDataScopeAsync(int userId);
}
