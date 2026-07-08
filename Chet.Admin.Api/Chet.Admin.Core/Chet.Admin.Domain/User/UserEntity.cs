using Chet.Admin.Domain.Role;

namespace Chet.Admin.Domain.User;

/// <summary>
/// 用户领域实体类（User Domain Entity）
/// <para>
/// 表示系统中的用户账户，包含用户的身份信息、认证凭据和令牌数据。
/// 继承自BaseEntity基类，获得通用的Id、CreatedAt、UpdatedAt字段。
/// </para>
/// </summary>
/// <remarks>
/// <para>设计模式：领域驱动设计（DDD）</para>
/// <para>
/// UserEntity是核心领域实体之一，封装了用户相关的业务规则和数据。
/// 作为聚合根（Aggregate Root），负责维护用户状态的一致性。
/// </para>
/// 
/// <para>数据库映射：</para>
/// <list type="table">
///   <listheader>
///     <term>属性</term>
///     <description>数据库列</description>
///     <term>约束</term>
///   </listheader>
///   <item>
///     <term>Id (继承)</term>
///     <description>Id (INT, PK, IDENTITY)</description>
///     <term>主键，自增</term>
///   </item>
///   <item>
///     <term>Name</term>
///     <description>Name (NVARCHAR(100))</description>
///     <term>NOT NULL</term>
///   </item>
///   <item>
///     <term>Email</term>
///     <description>Email (NVARCHAR(255))</description>
///     <term>NOT NULL, UNIQUE</term>
///   </item>
///   <item>
///     <term>PasswordHash</term>
///     <description>PasswordHash (NVARCHAR(MAX))</description>
///     <term>NOT NULL（BCrypt哈希值）</term>
///   </item>
///   <item>
///     <term>RefreshToken</term>
///     <description>RefreshToken (NVARCHAR(MAX))</description>
///     <term>NULLABLE</term>
///   </item>
///   <item>
///     <term>RefreshTokenExpiryTime</term>
///     <description>RefreshTokenExpiryTime (DATETIME2)</description>
///     <term>NULLABLE</term>
///   </item>
///   <item>
///     <term>CeatedAt/UpdatedAt</term>
///     <description>DATETIME2</description>
///     <term>自动填充</term>
///   </item>
/// </list>
/// 
/// <para>安全注意事项：</para>
/// <list type="bullet">
///   <item><description>此实体包含敏感信息（PasswordHash、RefreshToken）</description></item>
///   <item><description>永远不要将此实体直接返回给API客户端</description></item>
///   <item><description>应该通过AutoMapper映射到UserDto后再返回</description></item>
///   <item><description>PasswordHash使用BCrypt算法存储，不可逆</description></item>
/// </list>
/// 
/// <para>使用示例：</para>
/// <code>
/// // 创建新用户
/// var user = new UserEntity
/// {
///     Name = "张三",
///     Email = "zhangsan@example.com",
///     PasswordHash = _passwordService.Hash("Secure@123")
/// };
/// 
/// await _userRepository.AddAsync(user);
/// await _unitOfWork.SaveChangesAsync();
/// </code>
/// </remarks>
public class UserEntity : BaseEntity
{
    /// <summary>
    /// 用户显示名称
    /// <para>
    /// 用户的真实姓名或昵称，用于在系统中标识和展示用户身份。
    /// 此字段没有唯一性要求，多个用户可以使用相同的名称。
    /// </para>
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 用户邮箱地址
    /// <para>
    /// 用户的主要登录凭证和联系方式。
    /// 在数据库中配置了UNIQUE约束，确保每个邮箱只能注册一个账户。
    /// 用于登录认证、密码重置、通知推送等场景。
    /// </para>
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 密码哈希值（BCrypt格式）
    /// <para>
    /// 存储用户密码的BCrypt哈希值，而非明文密码。
    /// 格式为：$2a$12$...
    /// 其中：
    /// - $2a$：BCrypt算法标识符
    /// - 12：工作因子（cost factor）
    /// - 后续22字符：盐值（salt）
    /// - 剩余31字符：密码哈希
    /// </para>
    /// 
    /// <para>安全特性：</para>
    /// <list type="bullet">
    ///   <item><description>单向哈希：无法从哈希值还原原始密码</description></item>
    ///   <item><description>内置盐值：每次哈希自动生成唯一盐值，防止彩虹表攻击</description></item>
    ///   <item><description>自适应成本：可调整计算复杂度以抵抗硬件性能提升</description></item>
    /// </list>
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// 刷新令牌（Refresh Token）
    /// <para>
    /// 存储用户当前的刷新令牌值。
    /// 当Access Token过期后，客户端可使用此令牌获取新的令牌对。
    /// 每次成功刷新后会生成新的Refresh Token并更新此字段（Rotation机制）。
    /// </para>
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// <para>
    /// 当前Refresh Token的有效期截止时间。
    /// 超过此时间后，Refresh Token将失效，用户需要重新登录。
    /// 默认有效期为7天（可通过配置调整）。
    /// </para>
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// 用户头像URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// 用户角色关联
    /// </summary>
    public List<UserRoleEntity> UserRoles { get; set; } = [];

    /// <summary>连续登录失败次数</summary>
    public int LoginFailCount { get; set; } = 0;

    /// <summary>锁定截止时间</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>密码最后修改时间</summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>是否需要强制修改密码</summary>
    public bool MustChangePassword { get; set; } = false;
}
