using Chet.Admin.Contracts;
using Chet.Admin.Contracts.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Chet.Admin.Contracts;

/// <summary>
/// 工作单元接口（Unit of Work Interface）
/// <para>
/// 协调多个Repository之间的操作，确保它们在同一个事务中执行。
/// 实现工作单元模式（Unit of Work Pattern），保证数据一致性。
/// </para>
/// </summary>
/// <remarks>
/// <para>设计模式：工作单元模式（Unit of Work Pattern）</para>
/// <para>
/// 工作单元模式维护一个受业务事务影响的对象列表，
/// 并协调写入变更和解决并发问题的方式。
/// 它确保多个Repository的操作要么全部成功，要么全部回滚。
/// </para>
/// 
/// <para>何时使用UnitOfWork？</para>
/// <list type="bullet">
///   <item><description>需要在一个事务中操作多个实体类型</description></item>
///   <item><description>需要确保多个操作的原子性（全成功或全失败）</description></item>
///   <item><description>需要在Service层协调多个Repository的调用</description></item>
/// </list>
/// 
/// <para>使用示例：</para>
/// <code>
/// public class OrderService : IOrderService
/// {
///     private readonly IUnitOfWork _unitOfWork;
///     private readonly IUserRepository _userRepo;
///     private readonly IOrderRepository _orderRepo;
///     
///     public async Task CreateOrderAsync(OrderDto dto)
///     {
///         // 开始事务
///         await _unitOfWork.BeginTransactionAsync();
///         
///         try
///         {
///             // 扣减用户余额
///             var user = await _userRepo.GetByIdAsync(dto.UserId);
///             user.Balance -= dto.Amount;
///             _userRepo.Update(user);
///             
///             // 创建订单
///             var order = new Order { ... };
///             await _orderRepo.AddAsync(order);
///             
///             // 提交事务（两个操作同时生效）
///             await _unitOfWork.CommitAsync();
///         }
///         catch
///         {
///             // 回滚事务（两个操作都撤销）
///             await _unitOfWork.RollbackAsync();
///             throw;
///         }
///     }
/// }
/// </code>
/// 
/// <para>与Repository的关系：</para>
/// <para>
/// UnitOfWork聚合了多个Repository实例，共享同一个DbContext。
/// 通过UnitOfWork访问Repository可以确保它们使用相同的上下文和事务范围。
/// </para>
/// </remarks>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 用户仓储实例
    /// <para>
    /// 提供对UserEntity实体的CRUD操作以及用户特定的查询方法。
    /// 所有通过此属性执行的操作都共享同一事务上下文。
    /// </para>
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// 数据库上下文实例
    /// </summary>
    DbContext DbContext { get; }

    /// <summary>
    /// 保存所有挂起的更改到数据库
    /// <para>
    /// 将当前工作单元中所有Repository的待处理变更
    /// （包括添加、修改、删除）一次性提交到数据库。
    /// </para>
    /// </summary>
    /// <returns>受影响的行数总和</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// 开始一个数据库事务
    /// <para>
    /// 创建新的事务作用域，后续的所有数据库操作
    /// 都将在该事务中执行，直到显式提交或回滚。
    /// </para>
    /// </summary>
    /// <param name="isolationLevel">
    /// 事务隔离级别，控制事务之间的隔离程度。
    /// 默认为 ReadCommitted（读取已提交）。
    /// </param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>
    /// 事务对象，可用于手动管理事务生命周期
    /// </returns>
    /// <remarks>
    /// <para>隔离级别说明：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>级别</term>
    ///     <description>说明</description>
    ///   </listheader>
    ///   <item>
    ///     <term>ReadUncommitted</term>
    ///     <description>可以读取未提交的数据（脏读），性能最高但最不安全</description>
    ///   </item>
    ///   <item>
    ///     <term>ReadCommitted（默认）</term>
    ///     <description>只能读取已提交的数据，防止脏读</description>
    ///   </item>
    ///   <item>
    ///     <term>RepeatableRead</term>
    ///     <description>可重复读，防止脏读和不可重复读</description>
    ///   </item>
    ///   <item>
    ///     <term>Serializable</term>
    ///     <description>串行化，完全隔离，防止所有并发问题但性能最低</description>
    ///   </item>
    /// </list>
    /// </remarks>
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交当前事务
    /// <para>
    /// 将事务中的所有更改永久保存到数据库。
    /// 此方法内部会先调用SaveChangesAsync()，然后提交事务。
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <exception cref="InvalidOperationException">当前没有活动的事务时抛出</exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚当前事务
    /// <para>
    /// 撤销事务中的所有未提交更改，使数据库恢复到事务开始前的状态。
    /// 通常在异常处理块中调用。
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <exception cref="InvalidOperationException">当前没有活动的事务时抛出</exception>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
