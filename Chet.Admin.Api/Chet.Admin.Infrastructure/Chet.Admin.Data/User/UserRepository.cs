using Chet.Admin.Contracts.User;
using Chet.Admin.Domain.User;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data.User;

/// <summary>
/// 用户仓储实现类（User Repository Implementation）
/// <para>
/// 继承EfCoreRepository&lt;UserEntity&gt;通用仓储实现，
/// 并实现IUserRepository接口中定义的用户特定查询方法。
/// </para>
/// </summary>
/// <remarks>
/// <para>职责范围：</para>
/// <list type="bullet">
///   <item><description>继承通用CRUD操作：GetById、GetAll、Add、Update、Delete等</description></item>
///   <item><description>实现用户特定查询：按邮箱查询用户</description></item>
///   <item><description>可扩展：未来可添加更多用户相关的查询方法</description></item>
/// </list>
/// 
/// <para>设计模式：</para>
/// <para>
/// 采用"具体仓储继承通用仓储"的模式：
/// - 通用仓储（EfCoreRepository）处理标准CRUD
/// - 具体仓储（UserRepository）处理领域特定的查询
/// - 这种设计遵循开闭原则，便于扩展而不修改现有代码
/// </para>
/// 
/// <para>依赖注入配置：</para>
/// <code>
/// // 在DI容器中注册
/// services.AddScoped&lt;IUserRepository, UserRepository&gt;();
/// 
/// // 使用Scoped生命周期，确保每个请求使用相同的DbContext实例
/// // 这对于事务管理和变更跟踪至关重要
/// </code>
/// </remarks>
public class UserRepository : EfCoreRepository<UserEntity>, IUserRepository
{
    /// <summary>
    /// 初始化用户仓储实例
    /// </summary>
    /// <param name="dbContext">
    /// 数据库上下文，提供对Users表的访问。
    /// 通过构造函数注入传递给基类EfCoreRepository
    /// </param>
    public UserRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 根据邮箱地址查询用户实体
    /// <para>
    /// 使用FirstOrDefaultAsync执行查询，如果找到匹配的用户则返回该实体，
    /// 否则返回null。此方法主要用于用户登录认证场景。
    /// </para>
    /// </summary>
    /// <param name="email">要查找的邮箱地址（区分大小写取决于数据库配置）</param>
    /// <returns>匹配的用户实体；如果未找到则返回null</returns>
    public async Task<UserEntity> GetByEmailAsync(string email)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}
