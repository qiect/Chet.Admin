using Chet.Admin.Contracts;
using Chet.Admin.Contracts.User;
using Chet.Admin.Data.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Chet.Admin.Data
{
    /// <summary>
    /// 工作单元实现类（Unit of Work Implementation）
    /// <para>
    /// 实现IUnitOfWork接口，协调多个Repository实例之间的操作，
    /// 确保它们在同一个数据库事务中执行，保证数据一致性。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>核心职责：</para>
    /// <list type="number">
    ///   <item><description>仓储管理：创建和管理Repository实例的生命周期</description></item>
    ///   <item><description>事务协调：提供事务的开启、提交和回滚操作</description></item>
    ///   <item><description>变更保存：统一将所有挂起的变更持久化到数据库</description></item>
    ///   <item><description>资源释放：实现IDisposable模式，确保资源正确释放</description></item>
    /// </list>
    /// 
    /// <para>设计模式：</para>
    /// <list type="bullet">
    ///   <item><description>工作单元模式（Unit of Work）：管理业务事务边界</description></item>
    ///   <item><description>仓储模式（Repository）：封装数据访问逻辑</description></item>
    ///   <item><description>延迟初始化（Lazy Initialization）：Repository按需创建</description></item>
    ///   <item><description>Dispose模式：确保非托管资源的释放</description></item>
    /// </list>
    /// 
    /// <para>线程安全性：</para>
    /// <para>
    /// 此类不是线程安全的，设计为每个请求使用独立实例。
    /// 通过依赖注入容器的Scoped生命周期管理，确保每个HTTP请求拥有独立的UnitOfWork。
    /// </para>
    /// 
    /// <para>使用示例：</para>
    /// <code>
    /// public class OrderService
    /// {
    ///     private readonly IUnitOfWork _unitOfWork;
    ///     
    ///     public async Task&lt;Order&gt; CreateOrderAsync(CreateOrderDto dto)
    ///     {
    ///         // 开始事务
    ///         await _unitOfWork.BeginTransactionAsync();
    ///         
    ///         try
    ///         {
    ///             // 通过UnitOfWork访问Repository（共享同一DbContext）
    ///             var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
    ///             
    ///             // 执行业务逻辑...
    ///             user.UpdateBalance(-dto.TotalAmount);
    ///             _unitOfWork.Users.Update(user);
    ///             
    ///             var order = new Order { ... };
    ///             await _unitOfWork.Orders.AddAsync(order);
    ///             
    ///             // 提交所有变更
    ///             await _unitOfWork.CommitAsync();
    ///             
    ///             return order;
    ///         }
    ///         catch (Exception ex)
    ///         {
    ///             // 发生异常时回滚所有变更
    ///             await _unitOfWork.RollbackAsync();
    ///             throw new BusinessException("创建订单失败", ex);
    ///         }
    ///     }
    /// }
    /// </code>
    /// 
    /// <para>事务隔离级别说明：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>级别</term>
    ///     <description>说明</description>
    ///     <term>适用场景</term>
    ///   </listheader>
    ///   <item>
    ///     <term>ReadUncommitted</term>
    ///     <description>允许读取未提交的数据（脏读）</description>
    ///     <term>极少使用，性能要求极高时</term>
    ///   </item>
    ///   <item>
    ///     <term>ReadCommitted（默认）</term>
    ///     <description>只允许读取已提交的数据</description>
    ///     <term>大多数业务场景</term>
    ///   </item>
    ///   <item>
    ///     <term>RepeatableRead</term>
    ///     <description>确保同一事务内多次读取结果一致</description>
    ///     <term>财务、报表等数据一致性要求高的场景</term>
    ///   </item>
    ///   <item>
    ///     <term>Serializable</term>
    ///     <description>最高隔离级别，完全串行化</description>
    ///     <term>极少使用，严重影响并发性能</term>
    ///   </item>
    /// </list>
    /// </remarks>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        /// <summary>
        /// 用户仓储实例（延迟初始化）
        /// <para>
        /// 使用??=运算符实现懒加载，
        /// 只有在首次访问时才会创建UserRepository实例。
        /// </para>
        /// </summary>
        private IUserRepository? _users;

        /// <summary>
        /// 初始化工作单元实例
        /// </summary>
        /// <param name="context">
        /// 数据库上下文实例，由依赖注入容器注入。
        /// 所有通过此UnitOfWork访问的Repository都共享此上下文，
        /// 确保它们在同一事务范围内工作。
        /// </param>
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
    /// 获取用户仓储实例（延迟初始化）
    /// <para>
    /// 使用空合并赋值运算符(??=)实现懒加载模式。
    /// 首次访问时创建UserRepository实例，后续调用直接返回已创建的实例。
    /// 所有Repository共享同一个DbContext实例。
    /// </para>
    /// </summary>
    public IUserRepository Users => _users ??= new UserRepository(_context);

    /// <summary>
    /// 数据库上下文实例
    /// </summary>
    public DbContext DbContext => _context;

        /// <summary>
        /// 保存所有挂起的变更到数据库
        /// <para>
        /// 调用DbContext.SaveChangesAsync()将当前上下文中所有待处理的变更
        /// （包括添加、修改、删除操作）一次性提交到数据库。
        /// </para>
        /// </summary>
        /// <returns>受影响的行数总和</returns>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 开始一个数据库事务
        /// <para>
        /// 创建新的事务作用域，后续的所有数据库操作都将在该事务中执行。
        /// 必须显式调用CommitAsync()或RollbackAsync()来结束事务。
        /// </para>
        /// </summary>
        /// <param name="isolationLevel">
        /// 事务隔离级别，控制并发事务之间的隔离程度。
        /// 默认为ReadCommitted（读取已提交），适用于大多数业务场景。
        /// </param>
        /// <param name="cancellationToken">用于取消异步操作的令牌</param>
        /// <returns>数据库事务对象，可用于手动管理事务生命周期</returns>
        public async Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return _transaction;
        }

        /// <summary>
        /// 提交当前事务并保存所有变更
        /// <para>
        /// 执行两步操作：
        /// 1. 调用SaveChangesAsync()将所有挂起的变更写入数据库
        /// 2. 如果存在活动事务，则提交事务使变更永久生效
        /// </para>
        /// </summary>
        /// <param name="cancellationToken">用于取消异步操作的令牌</param>
        /// <exception cref="Exception">
        /// 如果保存或提交过程中发生异常，会自动调用RollbackAsync()回滚事务，
        /// 然后重新抛出原始异常供调用方处理。
        /// </exception>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (_transaction != null)
                {
                    await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// 回滚当前事务
        /// <para>
        /// 撤销自BeginTransactionAsync()以来所有未提交的数据库操作。
        /// 如果没有活动的事务，此方法不执行任何操作。
        /// </para>
        /// </summary>
        /// <param name="cancellationToken">用于取消异步操作的令牌</param>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 释放工作单元占用的资源
        /// <para>
        /// 实现IDisposable接口，确保非托管资源（如数据库事务）被正确释放。
        /// 使用_disposed标志防止重复释放。
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>调用时机：</para>
        /// <list type="bullet">
        ///   <item><description>当使用using语句块时，在块结束时自动调用</description></item>
        ///   <item><description>当通过依赖注入容器管理时，在Scope结束时自动调用</description></item>
        ///   <item><description>也可以手动调用，但通常不推荐</description></item>
        /// </list>
        /// 
        /// <para>注意：</para>
        /// <para>
        /// 此方法不会释放DbContext实例，因为DbContext由DI容器管理其生命周期。
        /// 只释放由此UnitOfWork创建的事务对象。
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _disposed = true;
            }
        }
    }
}
