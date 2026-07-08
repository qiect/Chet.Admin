using Chet.Admin.Contracts.Cache;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Chet.Admin.Caching
{
    /// <summary>
    /// Redis缓存服务实现类（Redis Cache Service Implementation）
    /// <para>
    /// 基于StackExchange.Redis客户端实现的分布式缓存服务。
    /// 提供高性能的数据缓存能力，支持数据持久化、集群部署和高可用性。
    /// 适用于生产环境的分布式系统架构。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>技术特性：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>特性</term>
    ///     <description>说明</description>
    ///   </listheader>
    ///   <item>
    ///     <term>高性能</term>
    ///     <description>基于内存操作，支持每秒10万+次读写</description>
    ///   </item>
    ///   <item>
    ///     <term>数据持久化</term>
    ///     <description>支持RDB快照和AOF日志，保证数据安全</description>
    ///   </item>
    ///   <item>
    ///     <term>集群支持</term>
    ///     <description>支持主从复制、哨兵模式和Cluster集群</description>
    ///   </item>
    ///   <item>
    ///     <term>丰富的数据结构</term>
    ///     <description>支持String、Hash、List、Set、Sorted Set等</description>
    ///   </item>
    ///   <item>
    ///     <term>原子操作</term>
    ///     <description>支持事务、Lua脚本、分布式锁</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>配置要求：</para>
    /// <list type="bullet">
    ///   <item><description>需要在appsettings.json中配置Redis连接字符串</description></item>
    ///   <item><description>建议配置连接池大小和超时时间</description></item>
    ///   <item><description>生产环境建议启用TLS加密传输</description></item>
    /// </list>
    /// 
    /// <para>错误处理策略：</para>
    /// <para>
    /// 所有方法都包含try-catch异常处理，确保Redis故障时不会影响主业务流程。
    /// 异常会被记录到日志，并返回安全的默认值（如null、false、空数组）。
    /// 这种"降级容错"设计保证了系统的可用性。
    /// </para>
    /// 
    /// <para>序列化方案：</para>
    /// <para>
    /// 使用System.Text.Json进行JSON序列化，相比BinaryFormatter：
    /// <list type="bullet">
    ///   <item><description>更安全：不存在反序列化漏洞风险</description></item>
    ///   <item><description>更高效：性能优于Newtonsoft.Json</description></item>
    ///   <item><description>跨平台：完全兼容.NET Core/.NET 5+</description></item>
    ///   <item><description>可读性好：便于调试和排查问题</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;

        /// <summary>
        /// 初始化Redis缓存服务实例
        /// </summary>
        /// <param name="connectionMultiplexer">
        /// Redis连接多路复用器，由DI容器注入。
        /// 使用单例模式管理连接，避免频繁创建和销毁连接的开销。
        /// </param>
        /// <param name="logger">日志记录器，用于记录操作日志和异常信息</param>
        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
        {
            _database = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        /// <summary>
        /// 从Redis缓存中获取指定键的值
        /// </summary>
        /// <typeparam name="T">缓存值的类型，需要能够被JSON反序列化</typeparam>
        /// <param name="key">缓存键，用于唯一标识缓存项</param>
        /// <returns>
        /// 反序列化后的对象实例；如果键不存在、已过期或发生异常，返回默认值（null）
        /// </returns>
        public async Task<T> GetAsync<T>(string key)
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                if (value.IsNull)
                {
                    return default;
                }
                return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from Redis cache for key: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// 将值写入Redis缓存
        /// </summary>
        /// <typeparam name="T">缓存值的类型，需要能够被JSON序列化</typeparam>
        /// <param name="key">缓存键，用于唯一标识缓存项</param>
        /// <param name="value">要缓存的值，会被序列化为JSON字符串存储</param>
        /// <param name="expiry">
        /// 可选的过期时间。如果指定，则键会在该时间后自动删除。
        /// 建议始终设置合理的过期时间以避免内存无限增长。
        /// </param>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, json, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value to Redis cache for key: {Key}", key);
            }
        }

        /// <summary>
        /// 从Redis缓存中移除指定的键
        /// </summary>
        /// <param name="key">要删除的缓存键</param>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing value from Redis cache for key: {Key}", key);
            }
        }

        /// <summary>
        /// 根据通配符模式批量删除Redis缓存键
        /// <para>
        /// 遍历所有Redis节点，使用KEYS命令查找匹配的键并删除。
        /// 支持Redis通配符：*（匹配任意字符）、?（匹配单个字符）
        /// </para>
        /// </summary>
        /// <param name="pattern">
        /// 键匹配模式。例如：
        /// <list type="bullet">
        ///   <item><description>"user:*" - 所有用户相关缓存</description></item>
        ///   <item><description>"api:users:list:*" - 所有用户列表缓存</description></item>
        ///   <item><description>"*:permissions" - 所有权限缓存</description></item>
        /// </list>
        /// </param>
        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var endpoints = _database.Multiplexer.GetEndPoints();

                foreach (var endpoint in endpoints)
                {
                    var server = _database.Multiplexer.GetServer(endpoint);
                    var keys = server.Keys(pattern: pattern).ToArray();

                    if (keys.Length > 0)
                    {
                        await _database.KeyDeleteAsync(keys);
                        _logger.LogInformation("Removed {Count} keys matching pattern: {Pattern}", keys.Length, pattern);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing keys from Redis cache for pattern: {Pattern}", pattern);
            }
        }

        /// <summary>
        /// 检查指定的缓存键是否存在于Redis中
        /// </summary>
        /// <param name="key">要检查的缓存键</param>
        /// <returns>如果键存在返回true；否则返回false（包括键不存在或发生异常的情况）</returns>
        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists in Redis cache: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 获取所有匹配指定模式的Redis缓存键
        /// <para>
        /// 遍历所有Redis节点，使用KEYS命令查找匹配的键。
        /// 适用于缓存管理、监控和批量操作场景。
        /// </para>
        /// </summary>
        /// <param name="pattern">键匹配模式，支持通配符</param>
        /// <returns>匹配的键字符串数组；如果没有匹配或发生异常，返回空数组</returns>
        public async Task<string[]> GetKeysByPatternAsync(string pattern)
        {
            try
            {
                var endpoints = _database.Multiplexer.GetEndPoints();
                var allKeys = new List<string>();

                foreach (var endpoint in endpoints)
                {
                    var server = _database.Multiplexer.GetServer(endpoint);
                    var keys = server.Keys(pattern: pattern).Select(k => k.ToString());
                    allKeys.AddRange(keys);
                }

                return allKeys.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keys by pattern from Redis cache: {Pattern}", pattern);
                return [];
            }
        }

        /// <summary>
        /// 从缓存获取值，如果不存在则通过工厂方法创建并缓存
        /// <para>
        /// 实现经典的"Cache-Aside"模式（旁路缓存模式）：
        /// 1. 先尝试从缓存获取
        /// 2. 如果缓存未命中，调用工厂方法从数据源加载
        /// 3. 将加载的数据写入缓存，供后续请求使用
        /// </para>
        /// </summary>
        /// <typeparam name="T">缓存值的类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">
        /// 数据工厂函数，当缓存未命中时调用来获取数据。
        /// 通常是从数据库查询或调用外部API的异步方法。
        /// </param>
        /// <param name="expiry">
        /// 可选的过期时间。仅在新创建缓存项时应用。
        /// </param>
        /// <returns>缓存中的值或新创建的值</returns>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            var value = await GetAsync<T>(key);
            if (value != null)
            {
                return value;
            }

            value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiry);
            }

            return value;
        }

        /// <summary>
        /// 检测Redis连接是否可用
        /// <para>
        /// 通过发送PING命令测试与Redis服务器的连接状态。
        /// 用于健康检查和监控场景，判断缓存服务是否正常工作。
        /// </para>
        /// </summary>
        /// <returns>
        /// 如果Redis响应时间小于5秒返回true；否则或发生异常时返回false
        /// </returns>
        public async Task<bool> PingAsync()
        {
            try
            {
                return await _database.PingAsync() < TimeSpan.FromSeconds(5);
            }
            catch
            {
                return false;
            }
        }
    }
}
