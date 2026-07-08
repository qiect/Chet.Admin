using Chet.Admin.Shared;

namespace Chet.Admin.Contracts;

/// <summary>
/// 通用仓储接口（Generic Repository Interface）
/// <para>
/// 定义了实体数据访问层的标准CRUD操作抽象。
/// 采用泛型设计，可以为任何实体类型提供统一的数据访问接口。
/// 这是仓储模式（Repository Pattern）的核心契约。
/// </para>
/// </summary>
/// <typeparam name="T">
/// 实体类型参数，必须是引用类型（class）。
/// 通常为领域实体（Domain Entity），如UserEntity、OrderEntity等
/// </typeparam>
/// <remarks>
/// <para>设计模式：仓储模式（Repository Pattern）</para>
/// <para>
/// 仓储模式将数据访问逻辑封装在单独的类中，实现业务逻辑层与数据访问层的解耦。
/// 主要优势：
/// <list type="bullet">
///   <item><description>关注点分离：业务逻辑不直接依赖EF Core等ORM框架</description></item>
///   <item><description>可测试性：可以轻松Mock仓储接口进行单元测试</description></item>
///   <item><description>可替换性：可以切换不同的数据存储实现而不影响业务代码</description></item>
/// </list>
/// </para>
/// 
/// <para>使用示例：</para>
/// <code>
/// // 在Service中注入
/// public class UserService : IUserService
/// {
///     private readonly IRepository&lt;UserEntity&gt; _userRepository;
///     
///     public UserService(IRepository&lt;UserEntity&gt; userRepository)
///     {
///         _userRepository = userRepository;
///     }
///     
///     public async Task&lt;UserEntity&gt; GetUserAsync(int id)
///     {
///         return await _userRepository.GetByIdAsync(id);
///     }
/// }
/// </code>
/// 
/// <para>注意事项：</para>
/// <list type="bullet">
///   <item><description>Add/Update/Delete操作不会立即持久化到数据库，需要显式调用SaveChangesAsync()</description></item>
///   <item><description>GetAllAsync()慎用大数据集场景，建议使用分页查询GetPagedAsync()</description></item>
///   <item><description>复杂查询应创建专用的仓储接口扩展，而非滥用FindAsync</description></item>
/// </list>
/// </remarks>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// 根据主键ID获取单个实体
    /// </summary>
    /// <param name="id">实体的主键标识符</param>
    /// <returns>
    /// 匹配的实体对象；如果不存在则返回null
    /// </returns>
    /// <remarks>
    /// <para>内部实现：</para>
    /// <para>通常调用DbContext.FindAsync()方法，支持从变更跟踪器中查找已加载的实体。</para>
    /// </remarks>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// 获取所有实体记录
    /// <para>
    /// ⚠️ 性能警告：此方法会加载表中的所有记录，
    /// 仅适用于数据量较小的配置表或字典表。
    /// 对于大数据集，请使用分页查询 GetPagedAsync()。
    /// </para>
    /// </summary>
    /// <returns>包含所有实体的可枚举集合</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 将新实体添加到数据库上下文
    /// <para>
    /// 注意：此操作仅将实体标记为"Added"状态，
    /// 不会立即写入数据库。需要后续调用 SaveChangesAsync() 才能持久化。
    /// </para>
    /// </summary>
    /// <param name="entity">要添加的新实体对象</param>
    /// <returns>表示异步操作的任务</returns>
    Task AddAsync(T entity);

    /// <summary>
    /// 将实体标记为已修改状态
    /// <para>
    /// EF Core的变更跟踪器会将该实体的所有属性标记为已修改。
    /// 下次SaveChangesAsync()时会生成UPDATE语句。
    /// </para>
    /// </summary>
    /// <param name="entity">要更新的实体对象（必须已存在于数据库中）</param>
    void Update(T entity);

    /// <summary>
    /// 将实体标记为删除状态
    /// <para>
    /// 实体会被添加到Deleted实体列表中。
    /// 下次SaveChangesAsync()时会生成DELETE语句。
    /// 支持级联删除（根据外键配置）。
    /// </para>
    /// </summary>
    /// <param name="entity">要删除的实体对象</param>
    void Delete(T entity);

    /// <summary>
    /// 检查指定ID的实体是否存在于数据库中
    /// </summary>
    /// <param name="id">要检查的主键ID</param>
    /// <returns>
    /// 如果存在返回true；不存在返回false
    /// </returns>
    /// <remarks>
    /// <para>典型用途：</para>
    /// <list type="bullet">
    ///   <item><description>创建前检查唯一性约束</description></item>
    ///   <item><description>更新/删除前验证实体是否存在</description></item>
    /// </list>
    /// </remarks>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// 显式保存所有挂起的更改到数据库
    /// <para>
    /// 将当前上下文中所有待处理的变更（增、删、改）
    /// 以事务方式一次性提交到数据库。
    /// </para>
    /// </summary>
    /// <returns>
    /// 受影响的行数。可用于验证操作是否成功执行
    /// </returns>
    /// <exception cref="DbUpdateException">违反数据库约束时抛出</exception>
    /// <exception cref="DbUpdateConcurrencyException">并发冲突时抛出</exception>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// 分页查询实体列表
    /// <para>
    /// 支持灵活的分页参数配置，自动计算总记录数、总页数等信息。
    /// 返回标准的分页结果对象 PagedResult&lt;T&gt;。
    /// </para>
    /// </summary>
    /// <param name="request">分页请求参数，包括页码、每页大小、排序方式等</param>
    /// <returns>
    /// 包含当前页数据、分页元信息的分页结果对象
    /// </returns>
    /// <remarks>
    /// <para>PagedRequest参数说明：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>属性</term>
    ///     <description>说明</description>
    ///   </listheader>
    ///   <item>
    ///     <term>PageNumber</term>
    ///     <description>页码（从1开始）</description>
    ///   </item>
    ///   <item>
    ///     <term>PageSize</term>
    ///     <description>每页记录数（默认20，最大100）</description>
    ///   </item>
    ///   <item>
    ///     <term>SortBy</term>
    ///     <description>排序字段名</description>
    ///   </item>
    ///   <item>
    ///     <term>SortDirection</term>
    ///     <description>排序方向（Asc/Desc）</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// var result = await repository.GetPagedAsync(new PagedRequest
    /// {
    ///     PageNumber = 1,
    ///     PageSize = 10,
    ///     SortBy = "CreatedAt",
    ///     SortDirection = SortDirection.Descending
    /// });
    /// 
    /// Console.WriteLine($"共 {result.TotalCount} 条记录");
    /// Console.WriteLine($"共 {result.TotalPages} 页");
    /// foreach (var item in result.Items) { ... }
    /// </code>
    /// </remarks>
    Task<PagedResult<T>> GetPagedAsync(PagedRequest request);

    /// <summary>
    /// 根据条件表达式查询实体集合
    /// <para>
    /// 使用LINQ表达式树定义查询条件，支持复杂的筛选逻辑。
    /// 返回所有匹配条件的实体列表。
    /// </para>
    /// </summary>
    /// <param name="predicate">
    /// LINQ谓词表达式，定义筛选条件。
    /// 例如：e => e.IsActive &amp;&amp; e.Age > 18
    /// </param>
    /// <returns>匹配条件的实体集合</returns>
    /// <remarks>
    /// <para>示例：</para>
    /// <code>
    /// // 查询所有活跃用户
    /// var activeUsers = await repository.FindAsync(u => u.IsActive);
    /// 
    /// // 按条件组合查询
    /// var results = await repository.FindAsync(u => 
    ///     u.Email.Contains("@example.com") &amp;&amp; 
    ///     u.CreatedAt > DateTime.UtcNow.AddDays(-30));
    /// </code>
    /// 
    /// <para>性能提示：</para>
    /// <para>对于大数据集，建议结合分页使用。</para>
    /// </remarks>
    Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
}
