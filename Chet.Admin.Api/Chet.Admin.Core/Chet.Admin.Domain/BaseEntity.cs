namespace Chet.Admin.Domain
{
    /// <summary>
    /// 实体基类（Base Entity）
    /// <para>
    /// 所有领域实体的抽象基类，定义了实体共有的基础属性。
    /// 提供统一的主键标识和时间戳跟踪功能。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>设计目的：</para>
    /// <list type="bullet">
    ///   <item><description>代码复用：避免在每个实体中重复定义通用字段</description></item>
    ///   <item><description>一致性：确保所有实体具有相同的身份标识和审计字段</description></item>
    ///   <item><description>可扩展：便于在基类中添加全局行为（如软删除、并发控制等）</description></item>
    ///   <item><description>多态支持：可通过基类引用操作任何具体实体类型</description></item>
    /// </list>
    /// 
    /// <para>继承体系：</para>
    /// <code>
    /// BaseEntity (抽象基类)
    ///     ├── UserEntity (用户实体)
    ///     ├── OrderEntity (订单实体) - 未来扩展
    ///     ├── ProductEntity (产品实体) - 未来扩展
    ///     └── ... 其他领域实体
    /// </code>
    /// 
    /// <para>数据库映射：</para>
    /// <para>
    /// EF Core会将这些属性映射到每个实体表的对应列。
    /// 建议使用EF Core的配置（Fluent API或Data Annotations）来：
    /// <list type="bullet">
    ///   <item><description>设置Id为主键且自增</description></item>
    ///   <item><description>设置CreatedAt默认值为当前时间</description></item>
    ///   <item><description>设置UpdatedAt在每次更新时自动刷新</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>最佳实践：</para>
    /// <list type="number">
    ///   <item><description>不要直接实例化BaseEntity，它只是作为其他实体的基类</description></item>
    ///   <item><description>CreatedAt应该在创建记录时设置，之后不应修改</description></item>
    ///   <item><description>UpdatedAt应该在每次更新记录时自动刷新为当前时间</description></item>
    ///   <item><description>可在EF Core的SaveChanges拦截器中自动处理时间戳更新</description></item>
    /// </list>
    /// 
    /// <para>未来可能的扩展：</para>
    /// <list type="bullet">
    ///   <item><description>IsDeleted：支持软删除（逻辑删除）</description></item>
    ///   <item><description>Version/RowVersion：乐观并发控制</description></item>
    ///   <item><description>CreatedBy/UpdatedBy：审计追踪（创建人/修改人）</description></item>
    ///   <item><description>TenantId：多租户隔离</description></item>
    /// </list>
    /// </remarks>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 实体唯一标识符（主键ID）
        /// <para>
        /// 数据库主键，通常为自增整数（INT IDENTITY）。
        /// 用于唯一标识每条记录，建立表之间的关联关系。
        /// </para>
        /// 
        /// <para>设计考虑：</para>
        /// <list type="table">
        ///   <listheader>
        ///     <term>方案</term>
        ///     <description>优点</description>
        ///     <term>缺点</term>
        ///   </listheader>
        ///   <item>
        ///     <term>自增INT（当前）</term>
        ///     <description>简单、高效、占用空间小</description>
        ///     <term>分布式系统可能冲突</term>
        ///   </item>
        ///   <item>
        ///     <term>GUID/UUID</term>
        ///     <description>全局唯一，适合分布式</description>
        ///     <term>占用空间大、索引效率低</term>
        ///   </item>
        ///   <item>
        ///     <term>Snowflake ID</term>
        ///     <description>有序、分布式友好</description>
        ///     <term>实现复杂度较高</term>
        ///   </item>
        /// </list>
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 记录创建时间（UTC时间戳）
        /// <para>
        /// 表示该记录首次插入数据库的时间点。
        /// 格式为ISO 8601标准：yyyy-MM-ddTHH:mm:ss.fffffffZ
        /// </para>
        /// 
        /// <para>用途：</para>
        /// <list type="bullet">
        ///   <item><description>数据排序：按创建时间排列记录</description></item>
        ///   <item><description>数据分析：统计某时间段内的新增数据量</description></item>
        ///   <item><description>业务规则：判断数据是否过期或需要归档</description></item>
        /// </list>
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 记录最后更新时间（UTC时间戳）
        /// <para>
        /// 表示该记录最后一次被修改的时间点。
        /// 每次执行UPDATE操作时都应该更新此字段为当前服务器时间。
        /// </para>
        /// 
        /// <para>用途：</para>
        /// <list type="bullet">
        ///   <item><description>缓存失效：检测数据是否被其他请求修改</description></item>
        ///   <item><description>并发控制：配合版本号实现乐观锁</description></item>
        ///   <item><description>审计追踪：了解数据的变更历史</description></item>
        ///   <item><description>同步机制：增量数据同步的依据</description></item>
        /// </list>
        /// 
        /// <para>自动更新建议：</para>
        /// <code>
        /// // 在DbContext中重写SaveChangesAsync
        /// public override Task&lt;int&gt; SaveChangesAsync(...)
        /// {
        ///     var entries = ChangeTracker.Entries()
        ///         .Where(e => e.State == EntityState.Modified);
        ///     
        ///     foreach (var entry in entries)
        ///     {
        ///         ((BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;
        ///     }
        ///     
        ///     return base.SaveChangesAsync(...);
        /// }
        /// </code>
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
