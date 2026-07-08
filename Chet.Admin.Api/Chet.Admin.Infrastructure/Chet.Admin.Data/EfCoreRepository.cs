using Chet.Admin.Contracts;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;

namespace Chet.Admin.Data
{
    /// <summary>
    /// Entity Framework Core通用仓储实现类（EF Core Generic Repository Implementation）
    /// <para>
    /// 实现了IRepository&lt;T&gt;接口，提供基于EF Core的标准CRUD数据访问操作。
    /// 作为所有具体仓储实现的基础类，封装了通用的数据访问逻辑。
    /// </para>
    /// </summary>
    /// <typeparam name="T">
    /// 实体类型参数，必须是引用类型且映射到数据库表。
    /// 通常继承自基类实体或直接使用领域实体
    /// </typeparam>
    /// <remarks>
    /// <para>设计特点：</para>
    /// <list type="bullet">
    ///   <item><description>泛型支持：通过泛型参数T支持任意实体类型的仓储</description></item>
    ///   <item><description>延迟持久化：Add/Update/Delete操作不立即保存，由UnitOfWork统一控制</description></item>
    ///   <item><description>变更跟踪：利用EF Core的变更跟踪器自动检测实体状态变化</description></item>
    ///   <item><description>查询优化：只读查询使用AsNoTracking()提升性能</description></item>
    ///   <item><description>分页支持：内置GetPagedAsync()方法支持服务端分页</description></item>
    /// </list>
    /// 
    /// <para>线程安全性：</para>
    /// <para>
    /// 此类不是线程安全的。DbContext本身不是线程安全的，
    /// 因此每个请求应该使用独立的仓储实例（通过Scoped依赖注入）。
    /// </para>
    /// 
    /// <para>事务管理：</para>
    /// <para>
    /// SaveChangesAsync()方法会将所有挂起的变更一次性提交到数据库。
    /// 在需要事务的场景下，应通过UnitOfWork.BeginTransactionAsync()管理事务边界。
    /// </para>
    /// 
    /// <para>继承体系：</para>
    /// <code>
    /// IRepository&lt;T&gt;          (接口 - 定义契约)
    ///     └── EfCoreRepository&lt;T&gt;  (抽象实现 - 通用CRUD)
    ///         └── UserRepository      (具体实现 - 用户特定查询)
    /// </code>
    /// </remarks>
    public class EfCoreRepository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _dbContext;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// 初始化EF Core通用仓储实例
        /// </summary>
        /// <param name="dbContext">
        /// 数据库上下文实例，提供对数据库的访问和变更跟踪功能。
        /// 通过依赖注入容器注入，生命周期为Scoped（每个请求一个实例）
        /// </param>
        public EfCoreRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = dbContext.Set<T>();
        }

        /// <summary>
        /// 根据主键ID获取单个实体
        /// </summary>
        /// <param name="id">实体的主键标识符</param>
        /// <returns>匹配的实体对象；如果不存在则返回null</returns>
        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// 获取所有实体记录（只读查询，不跟踪变更）
        /// </summary>
        /// <returns>包含所有实体的列表</returns>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// 将新实体添加到数据库上下文（标记为Added状态）
        /// </summary>
        /// <param name="entity">要添加的新实体对象</param>
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        /// <summary>
        /// 将实体标记为已修改状态（触发变更跟踪）
        /// </summary>
        /// <param name="entity">要更新的实体对象</param>
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        /// <summary>
        /// 将实体标记为删除状态
        /// </summary>
        /// <param name="entity">要删除的实体对象</param>
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        /// <summary>
        /// 检查指定ID的实体是否存在
        /// </summary>
        /// <param name="id">要检查的主键ID</param>
        /// <returns>如果存在返回true；否则返回false</returns>
        public async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(e => EF.Property<int>(e, "Id") == id);
        }

        /// <summary>
        /// 将所有挂起的变更保存到数据库
        /// </summary>
        /// <returns>受影响的行数</returns>
        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 分页查询实体列表
        /// <para>
        /// 执行服务端分页，只返回当前页的数据和总数信息。
        /// 使用Skip/Take实现高效分页，避免加载全部数据到内存。
        /// </para>
        /// </summary>
        /// <param name="request">分页请求参数（页码、每页大小、排序等）</param>
        /// <returns>包含当前页数据和分页元信息的PagedResult对象</returns>
        public async Task<PagedResult<T>> GetPagedAsync(PagedRequest request)
        {
            request.Normalize();

            var query = _dbSet.AsNoTracking();
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<T>(items, request.PageNumber, request.PageSize, totalCount);
        }

        /// <summary>
        /// 根据条件表达式查询实体列表
        /// <para>
        /// 支持LINQ表达式作为过滤条件，提供灵活的查询能力。
        /// 查询结果为只读（AsNoTracking），不会跟踪实体变更。
        /// </para>
        /// </summary>
        /// <param name="predicate">LINQ表达式谓词，定义过滤条件</param>
        /// <returns>匹配条件的实体列表</returns>
        public async Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).AsNoTracking().ToListAsync();
        }
    }
}
