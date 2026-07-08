namespace Chet.Admin.DTOs.User;

/// <summary>
/// 用户创建数据传输对象（User Create Data Transfer Object）
/// <para>
/// 用于接收创建新用户的HTTP请求数据。
/// 包含用户注册时必须提供的基本信息：用户名、邮箱和密码。
/// </para>
/// </summary>
/// <remarks>
/// <para>使用场景：</para>
/// <list type="bullet">
///   <item><description>管理员通过API创建用户账户</description></item>
///   <item><description>批量导入用户数据</description></item>
///   <item><description>后台管理系统添加用户</description></item>
/// </list>
/// 
/// <para>验证规则：</para>
/// <list type="table">
///   <listheader>
///     <term>字段</term>
///     <description>验证规则</description>
///   </listheader>
///   <item>
///     <term>Name</term>
///     <description>必填，最大长度100字符</description>
///   </item>
///   <item>
///     <term>Email</term>
///     <description>必填，有效邮箱格式，最大长度255字符</description>
///   </item>
///   <item>
///     <term>Password</term>
///     <description>必填，最小长度6字符</description>
///   </item>
/// </list>
/// 
/// <para>请求示例：</para>
/// <code>
/// POST /api/v1/users
/// Content-Type: application/json
/// 
/// {
///   "name": "张三",
///   "email": "zhangsan@example.com",
///   "password": "Secure@123"
/// }
/// </code>
/// 
/// <para>安全提示：</para>
/// <para>
/// 密码字段在传输过程中应始终使用HTTPS加密。
/// 服务端接收到密码后会立即进行BCrypt哈希处理，
/// 明文密码不会被存储到数据库中。
/// </para>
/// </remarks>
public class UserCreateDto
{
    /// <summary>
    /// 用户名，用于标识和显示用户身份
    /// <para>
    /// 建议使用真实姓名或易于识别的昵称。
    /// 此字段会在用户列表和个人资料中显示。
    /// </para>
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 用户邮箱地址
    /// <para>
    /// 作为登录凭证之一，必须是唯一的。
    /// 系统会发送验证邮件、通知等信息到此邮箱。
    /// </para>
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// 用户密码
    /// <para>
    /// 用于登录认证的凭据。
    /// 建议包含大小写字母、数字和特殊字符的组合。
    /// 最小长度6个字符，建议12个字符以上以提高安全性。
    /// </para>
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// 部门ID（可选）
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// 角色ID列表
    /// <para>
    /// 创建用户时同时分配的角色，可选。
    /// </para>
    /// </summary>
    public List<int>? RoleIds { get; set; }
}
