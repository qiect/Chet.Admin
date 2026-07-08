namespace Chet.Admin.Contracts.Cache;

/// <summary>
/// 缓存服务接口（Cache Service Interface）
/// <para>
/// 定义缓存操作的统一抽象层，支持多种缓存后端实现。
/// 当前提供Redis和NoOp（空操作）两种实现，可根据配置动态切换。
/// </para>
/// </summary>
/// <remarks>
/// <para>设计模式：</para>
/// <para>采用策略模式（Strategy Pattern），通过接口抽象缓存行为，
/// 具体实现可在运行时通过依赖注入进行切换。</para>
/// 
/// <para>可用实现：</para>
/// <list type="table">
///   <listheader>
///     <term>实现类</term>
///     <description>说明</description>
///   </listheader>
///   <item>
///     <term>RedisCacheService</term>
///     <description>基于Redis的分布式缓存实现，支持集群和持久化</description>
///   </item>
///   <item>
///     <term>NoOpCacheService</term>
///     <description>空操作实现，不实际缓存数据，用于开发和测试环境</description>
///   </item>
/// </list>
/// 
/// <para>使用场景：</para>
/// <list type="bullet">
///   <item><description>会话数据缓存（用户登录状态、购物车等）</description></item>
///   <item><description>热点数据缓存（频繁查询的配置、字典数据）</description></item>
///   <item><description>API响应缓存（减少数据库压力）</description></item>
///   <item><description>分布式锁（防止并发重复操作）</description></item>
/// </list>
/// 
/// <para>键命名规范：</para>
/// <para>
/// 建议使用冒号分隔的层级结构：
/// <code>{模块}:{实体}:{标识符}</code>
/// 例如：<c>user:profile:123</c>、<c>api:users:list</c>
/// </para>
/// </remarks>
public interface ICacheService
{
    /// <summary>
    /// 从缓存中获取指定键的值
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键，用于唯一标识缓存项</param>
    /// <returns>
    /// 缓存中存储的值。如果键不存在或已过期，返回类型的默认值（如null）
    /// </returns>
    /// <remarks>
    /// <para>示例：</para>
    /// <code>
    /// var user = await cacheService.GetAsync&lt;UserDto&gt;("user:1");
    /// if (user != null)
    /// {
    ///     // 使用缓存的数据
    /// }
    /// else
    /// {
    ///     // 从数据库加载并写入缓存
    /// }
    /// </code>
    /// </remarks>
    Task<T> GetAsync<T>(string key);

    /// <summary>
    /// 将值写入缓存
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键，用于唯一标识缓存项</param>
    /// <param name="value">要缓存的值</param>
    /// <param name="expiry">
    /// 可选的过期时间。如果为null，则使用默认过期时间或永不过期（取决于实现）。
    /// 建议始终设置合理的过期时间以避免内存泄漏。
    /// </param>
    /// <returns>表示异步写入操作的任务</returns>
    /// <remarks>
    /// <para>示例：</para>
    /// <code>
    /// // 设置30分钟过期
    /// await cacheService.SetAsync("user:1", userDto, TimeSpan.FromMinutes(30));
    /// 
    /// // 设置1小时过期
    /// await cacheService.SetAsync("config:settings", settings, TimeSpan.FromHours(1));
    /// </code>
    /// 
    /// <para>常见过期时间建议：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>数据类型</term>
    ///     <description>建议过期时间</description>
    ///   </listheader>
    ///   <item>
    ///     <term>会话数据</term>
    ///     <description>15-30分钟</description>
    ///   </item>
    ///   <item>
    ///     <term>用户资料</term>
    ///     <description>1-2小时</description>
    ///   </item>
    ///   <item>
    ///     <term>系统配置</term>
    ///     <description>5-10分钟（便于热更新）</description>
    ///   </item>
    ///   <item>
    ///     <term>字典/枚举数据</term>
    ///     <description>24小时或更长</description>
    ///   </item>
    /// </list>
    /// </remarks>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// 移除指定的缓存项
    /// </summary>
    /// <param name="key">要移除的缓存键</param>
    /// <returns>表示异步删除操作的任务</returns>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item><description>数据更新后清除旧缓存</description></item>
    ///   <item><description>用户登出时清除会话</description></item>
    ///   <item><description>手动失效特定缓存项</description></item>
    /// </list>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// // 更新用户后清除缓存
    /// await userService.UpdateUser(id, dto);
    /// await cacheService.RemoveAsync($"user:{id}");
    /// </code>
    /// </remarks>
    Task RemoveAsync(string key);

    /// <summary>
    /// 根据模式批量移除匹配的缓存项
    /// <para>
    /// 支持通配符模式匹配，可以一次性删除多个相关缓存项。
    /// 适用于清理某个实体的所有关联缓存。
    /// </para>
    /// </summary>
    /// <param name="pattern">
    /// 匹配模式，支持通配符。例如：
    /// <list type="bullet">
    ///   <item><description><c>user:*</c> - 删除所有用户相关的缓存</description></item>
    ///   <item><description><c>api:users:*</c> - 删除所有用户列表缓存</description></item>
    ///   <item><description><c>*:permission:*</c> - 删除所有权限相关缓存</description></item>
    /// </list>
    /// </param>
    /// <returns>表示异步删除操作的任务</returns>
    /// <remarks>
    /// <para>性能警告：</para>
        /// <para>
    /// 此操作可能需要扫描大量键，在高并发环境下应谨慎使用。
    /// 建议在非高峰期执行或在后台任务中处理。
    /// </para>
    /// 
    /// <para>典型用例：</para>
    /// <code>
    /// // 用户权限变更时清除所有权限缓存
    /// await permissionService.UpdatePermissions(userId, permissions);
    /// await cacheService.RemoveByPatternAsync($"*:{userId}:permissions");
    /// </code>
    /// </remarks>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// 检查指定的缓存键是否存在
    /// </summary>
    /// <param name="key">要检查的缓存键</param>
    /// <returns>
    /// 如果键存在且未过期返回true；否则返回false
    /// </returns>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item><description>判断是否需要从数据源加载数据</description></item>
    ///   <item><description>实现缓存穿透保护</description></item>
    ///   <item><description>监控特定缓存项的状态</description></item>
    /// </list>
    /// </remarks>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 获取所有匹配指定模式的缓存键
    /// </summary>
    /// <param name="pattern">键匹配模式，支持通配符</param>
    /// <returns>
    /// 匹配的键数组。如果没有匹配项，返回空数组
    /// </returns>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item><description>调试和监控：查看某类缓存有哪些键</description></item>
    ///   <item><description>批量操作前预览影响的范围</description></item>
    ///   <item><description>统计分析：统计某类缓存的数量</description></item>
    /// </list>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// // 查看所有用户缓存键
    /// var keys = await cacheService.GetKeysByPatternAsync("user:*");
    /// Console.WriteLine($"共有 {keys.Length} 个用户缓存");
    /// </code>
    /// </remarks>
    Task<string[]> GetKeysByPatternAsync(string pattern);

    /// <summary>
    /// 获取缓存值，如果不存在则创建并缓存
    /// <para>
    /// 原子性的"获取或创建"操作，常被称为"缓存穿透"（Cache-Aside）模式的简化版。
    /// 当缓存未命中时，自动调用工厂函数生成数据并写入缓存。
    /// </para>
    /// </summary>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">
    /// 数据工厂函数，当缓存未命中时调用此函数生成数据。
    /// 该函数只在缓存不存在时执行一次。
    /// </param>
    /// <param name="expiry">可选的过期时间</param>
    /// <returns>
    /// 返回缓存中的值或新创建的值
    /// </returns>
    /// <remarks>
    /// <para>线程安全说明：</para>
    /// <para>
    /// 在高并发场景下，可能会有多个线程同时发现缓存未命中并调用factory。
    /// 实现应考虑使用锁或其他机制确保factory只被调用一次，
    /// 或者接受短暂的重复计算以保证可用性。
    /// </para>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// var users = await cacheService.GetOrCreateAsync(
    ///     "users:all",
    ///     async () => {
    ///         // 只有在缓存未命中时才执行此查询
    ///         return await userRepository.GetAllAsync();
    ///     },
    ///     TimeSpan.FromMinutes(10)
    /// );
    /// </code>
    /// </remarks>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// 检查缓存服务连接是否正常
    /// <para>
    /// 用于健康检查探针，验证缓存服务的可用性。
    /// 在健康检查端点中调用以报告缓存组件状态。
    /// </para>
    /// </summary>
    /// <returns>
    /// 如果连接正常返回true；连接失败或超时返回false
    /// </returns>
    /// <remarks>
    /// <para>实现差异：</para>
    /// <list type="table">
    ///   <listheader>
        ///     <term>实现类</term>
        ///     <description>Ping行为</description>
    ///   </listheader>
    ///   <item>
    ///     <term>RedisCacheService</term>
    ///     <description>发送PING命令到Redis服务器，验证网络连通性</description>
    ///   </item>
    ///   <item>
    ///     <term>NoOpCacheService</term>
    ///     <description>始终返回true（无实际连接需要检查）</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>使用位置：</para>
    /// <para>
    /// 主要在HealthController的就绪探针中使用：
    /// <code>
    /// public async Task&lt;IActionResult&gt; Readiness()
    /// {
    ///     var isHealthy = await _cacheService.PingAsync();
    ///     // ...
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    Task<bool> PingAsync();
}
