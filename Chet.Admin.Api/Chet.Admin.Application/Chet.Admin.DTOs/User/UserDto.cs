namespace Chet.Admin.DTOs.User;

/// <summary>
/// 用户数据传输对象（User Data Transfer Object）
/// <para>
/// 用于向客户端返回用户信息的响应DTO。
/// 从领域实体UserEntity映射而来，只包含安全的、需要展示给用户的信息。
/// </para>
/// </summary>
/// <remarks>
/// <para>设计原则：</para>
/// <list type="bullet">
///   <item><description>数据隔离：不包含敏感信息如密码哈希、Refresh Token等</description></item>
///   <item><description>视图定制：只返回前端需要的字段，减少数据传输量</description></item>
///   <item><description>版本兼容：API字段变更不影响内部实体结构</description></item>
///   <item><description>序列化友好：所有属性都适合JSON序列化</description></item>
/// </list>
/// 
/// <para>与UserEntity的区别：</para>
/// <list type="table">
///   <listheader>
///     <term>特性</term>
///     <term>UserEntity</term>
///     <term>UserDto</term>
///   </listheader>
///   <item>
///     <term>用途</term>
///     <description>数据库持久化</description>
///     <description>API响应</description>
///   </item>
///   <item>
///     <term>包含密码哈希</term>
///     <description>✓ 是</description>
///     <description>✗ 否</description>
///   </item>
///   <item>
///     <term>包含RefreshToken</term>
///     <description>✓ 是</description>
///     <description>✗ 否</description>
///   </item>
///   <item>
///     <term>可序列化</term>
///     <description>可能包含循环引用</description>
///     <description>完全支持</description>
///   </item>
/// </list>
/// 
/// <para>使用场景：</para>
/// <list type="bullet">
///   <item><description>GET /api/v1/users - 获取用户列表</description></item>
///   <item><description>GET /api/v1/users/{id} - 获取用户详情</description></item>
///   <item><description>POST /api/v1/users - 创建成功后返回用户信息</description></item>
///   <item><description>PUT /api/v1/users/{id} - 更新成功后返回更新后的信息</description></item>
/// </list>
/// 
/// <para>响应示例：</para>
/// <code>
/// {
///   "id": 1,
///   "name": "张三",
///   "email": "zhangsan@example.com",
///   "createdAt": "2024-01-15T10:30:00Z",
///   "updatedAt": "2024-01-20T14:22:00Z"
/// }
/// </code>
/// </remarks>
public class UserDto
{
    /// <summary>
    /// 用户唯一标识符（主键ID）
    /// <para>
    /// 数据库自增主键，用于唯一标识用户记录。
    /// 在API中用于资源定位和关联查询。
    /// </para>
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户显示名称
    /// <para>
    /// 用户的真实姓名或昵称。
    /// 会显示在用户列表、个人资料页面等位置。
    /// </para>
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 用户邮箱地址
    /// <para>
    /// 用户的联系邮箱，也是登录凭证之一。
    /// 部分场景下可能被脱敏显示（如：z***@example.com）
    /// </para>
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// 用户头像URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<UserRoleInfoDto> Roles { get; set; } = [];

    /// <summary>
    /// 记录创建时间（UTC时间）
    /// <para>
    /// 表示用户账户的注册时间。
    /// 格式为ISO 8601标准：yyyy-MM-ddTHH:mm:ssZ
    /// </para>
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间（UTC时间）
    /// <para>
    /// 表示用户信息最后一次被修改的时间。
    /// 每次更新用户资料时自动更新此字段。
    /// 格式为ISO 8601标准：yyyy-MM-ddTHH:mm:ssZ
    /// </para>
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 用户角色信息
/// </summary>
public class UserRoleInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
