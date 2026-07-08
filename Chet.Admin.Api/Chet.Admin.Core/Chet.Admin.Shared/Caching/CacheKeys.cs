namespace Chet.Admin.Shared.Caching;

/// <summary>
/// 缓存键常量定义类（Cache Keys Constants）
/// <para>
/// 集中管理所有缓存键的定义，确保缓存键命名的一致性和可维护性。
/// 采用层级结构组织缓存键，支持按模块、实体、操作分类管理。
/// 所有键都带有统一的应用前缀，避免与其他应用冲突。
/// </para>
/// </summary>
/// <remarks>
/// <para>设计原则：</para>
/// <list type="number">
///   <item><description>集中定义：所有缓存键在一个地方定义，便于查找和管理</description></item>
///   <item><description>层级结构：使用冒号分隔的层级结构，清晰表达数据关系</description></item>
///   <item><description>类型安全：通过方法参数而非字符串拼接构建键名</description></item>
///   <item><description>统一前缀：全局前缀区分不同应用/环境的缓存</description></item>
/// </list>
/// 
/// <para>键命名规范：</para>
/// <code>{Prefix}:{Module}:{Entity}:{Identifier}:{Operation}</code>
/// 
/// <para>示例：</para>
/// <list type="table">
///   <listheader>
///     <term>键</term>
///     <description>说明</description>
///   </listheader>
///   <item>
///     <term>ChetApp:users:1</term>
///     <description>ID为1的用户详情</description>
///   </item>
///   <item>
///     <term>ChetApp:users:all</term>
///     <description>所有用户的列表</description>
///   </item>
///   <item>
///     <term>ChetApp:auth:blacklist:abc123</term>
///     <description>Token黑名单条目</description>
///   </item>
///   <item>
///     <term>ChetApp:auth:permissions:5</term>
///     <description>用户ID=5的权限缓存</description>
///   </item>
/// </list>
/// 
/// <para>使用示例：</para>
/// <code>
/// // 设置用户缓存
/// await cache.SetAsync(CacheKeys.Users.ById(1), userDto, CacheKeys.Expiry.Medium);
/// 
/// // 获取用户列表缓存
/// var users = await cache.GetAsync&lt;List&lt;UserDto&gt;&gt;(CacheKeys.Users.All());
/// 
/// // 清除所有用户相关缓存
/// await cache.RemoveByPatternAsync(CacheKeys.Users.Pattern);
/// </code>
/// </remarks>
public static class CacheKeys
{
    /// <summary>
    /// 全局缓存键前缀
    /// <para>用于区分不同应用的缓存数据，避免键冲突</para>
    /// </summary>
    private const string Prefix = "ChetApp:";

    /// <summary>
    /// 层级分隔符
    /// <para>使用冒号作为各层级的分隔符，符合Redis社区惯例</para>
    /// </summary>
    private const string Separator = ":";

    /// <summary>
    /// 默认过期时间配置
    /// <para>
    /// 提供四种标准的过期时间常量，根据数据特性选择合适的过期策略：
    /// <list type="table">
    ///   <listheader>
    ///     <term>级别</term>
    ///     <description>时间</description>
    ///     <term>适用场景</description>
    ///   </listheader>
    ///   <item>
    ///     <term>Short</term>
    ///     <description>5分钟</description>
    ///     <description>实时性要求高的数据（如在线状态）</description>
    ///   </item>
    ///   <item>
    ///     <term>Medium</term>
    ///     <description>30分钟</description>
    ///     <description>一般业务数据（如用户资料）</description>
    ///   </item>
    ///   <item>
    ///     <term>Long</term>
    ///     <description>2小时</description>
    ///     <description>变化较少的数据（如系统配置）</description>
    ///   </item>
    ///   <item>
    ///     <term>VeryLong</term>
    ///     <description>1天</description>
    ///     <description>几乎不变的数据（如字典表）</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    public static class Expiry
    {
        /// <summary>短期缓存 - 5分钟</summary>
        public static TimeSpan Short => TimeSpan.FromMinutes(5);

        /// <summary>中期缓存 - 30分钟</summary>
        public static TimeSpan Medium => TimeSpan.FromMinutes(30);

        /// <summary>长期缓存 - 2小时</summary>
        public static TimeSpan Long => TimeSpan.FromHours(2);

        /// <summary>超长缓存 - 1天</summary>
        public static TimeSpan VeryLong => TimeSpan.FromDays(1);
    }

    /// <summary>
    /// 用户相关缓存键定义
    /// <para>
    /// 包含用户实体的各种缓存场景：
    /// 单个用户查询、列表查询等。
    /// </para>
    /// </summary>
    public static class Users
    {
        /// <summary>用户模块的基础路径</summary>
        private const string Base = $"{Prefix}users";

        /// <summary>
        /// 通配符模式，用于批量删除所有用户相关缓存
        /// <para>配合 ICacheService.RemoveByPatternAsync() 使用</para>
        /// </summary>
        public static readonly string Pattern = $"{Base}:*";

        /// <summary>
        /// 根据用户ID获取单个用户缓存的键
        /// </summary>
        /// <param name="id">用户唯一标识符</param>
        /// <returns>格式化后的缓存键，如 "ChetApp:users:1"</returns>
        /// <example>
        /// <code>
        /// var key = CacheKeys.Users.ById(123); // 返回 "ChetApp:users:123"
        /// </code>
        /// </example>
        public static string ById(int id) => $"{Base}{Separator}{id}";

        /// <summary>
        /// 获取所有用户列表缓存的键
        /// <para>通常用于缓存分页的用户列表或完整列表</para>
        /// </summary>
        /// <returns>格式化后的缓存键，如 "ChetApp:users:all"</returns>
        public static string All() => $"{Base}{Separator}all";
    }

    /// <summary>
    /// 认证相关缓存键定义
    /// <para>
    /// 包含认证和授权相关的缓存场景：
    /// Token黑名单、用户权限等。
    /// </para>
    /// </summary>
    public static class Auth
    {
        /// <summary>认证模块的基础路径</summary>
        private const string Base = $"{Prefix}auth";

        /// <summary>
        /// Token黑名单缓存键
        /// <para>
        /// 用于JWT令牌注销功能。当用户主动登出或管理员撤销令牌时，
        /// 将令牌哈希加入黑名单，后续请求会被拒绝。
        /// </para>
        /// </summary>
        /// <param name="tokenHash">JWT令牌的SHA256哈希值</param>
        /// <returns>格式化后的缓存键</returns>
        /// <remarks>
        /// <para>建议过期时间：</para>
        /// <para>应设置为等于或大于JWT的过期时间</para>
        /// </remarks>
        public static string TokenBlacklist(string tokenHash) => $"{Base}{Separator}blacklist:{tokenHash}";

        /// <summary>
        /// 用户权限缓存键
        /// <para>
        /// 缓存用户的权限集合，避免每次请求都查询数据库。
        /// 权限变更时应主动清除该缓存。
        /// </para>
        /// </summary>
        /// <param name="userId">用户唯一标识符</param>
        /// <returns>格式化后的缓存键</returns>
        /// <remarks>
        /// <para>建议过期时间：</para>
        /// <para>Medium（30分钟），平衡性能和数据新鲜度</para>
        /// </remarks>
        public static string UserPermissions(int userId) => $"{Base}{Separator}permissions:{userId}";
    }
}
