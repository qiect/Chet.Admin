namespace Chet.Admin.DTOs.User;

/// <summary>
/// 用户注册数据传输对象（Register Data Transfer Object）
/// <para>
/// 用于接收新用户注册的HTTP请求数据。
/// 包含创建新账户所需的必要信息：用户名、邮箱和密码。
/// </para>
/// </summary>
/// <remarks>
/// <para>使用场景：</para>
/// <list type="bullet">
///   <item><description>用户自助注册新账户</description></item>
///   <item><description>移动端App注册流程</description></item>
///   <item><description>Web端注册页面提交</description></item>
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
///     <description>必填，有效邮箱格式，最大长度255字符，全局唯一</description>
///   </item>
///   <item>
///     <term>Password</term>
///     <description>必填，最小长度6字符（建议12+字符）</description>
///   </item>
/// </list>
/// 
/// <para>请求示例：</para>
/// <code>
/// POST /api/v1/auth/register
/// Content-Type: application/json
/// 
/// {
///   "name": "张三",
///   "email": "zhangsan@example.com",
///   "password": "Secure@123"
/// }
/// 
/// 成功响应（201 Created）：
/// {
///   "success": true,
///   "message": "User registered successfully",
///   "data": {
///     "id": 1,
///     "name": "张三",
///     "email": "zhangsan@example.com",
///     "createdAt": "2024-01-15T10:30:00Z",
///     "updatedAt": "2024-01-15T10:30:00Z"
///   },
///   "statusCode": 201
/// }
/// 
/// 错误响应（400 Bad Request）：
/// {
///   "success": false,
///   "message": "Validation failed",
///   "errors": {
///     "Email": ["邮箱已被注册"]
///   },
///   "statusCode": 400
/// }
/// </code>
/// 
/// <para>业务逻辑：</para>
/// <list type="number">
///   <item><description>检查邮箱是否已被注册（唯一性约束）</description></item>
///   <item><description>对密码进行强度校验（可选）</description></item>
///   <item><description>使用BCrypt算法哈希密码（工作因子=12）</description></item>
///   <item><description>创建用户记录并保存到数据库</description></item>
///   <item><description>返回用户信息（不包含密码哈希）</description></item>
/// </list>
/// </remarks>
public class RegisterDto
{
    /// <summary>
    /// 用户显示名称
    /// <para>
    /// 用户在系统中的显示名称。
    /// 可以是真实姓名、昵称或任意标识符。
    /// 此字段会在个人资料、评论、消息等场景中显示。
    /// </para>
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 用户邮箱地址（登录凭证）
    /// <para>
    /// 作为主要登录标识符，必须是唯一的。
    /// 注册时会检查此邮箱是否已被其他账户使用。
    /// 建议使用真实有效的邮箱地址以便接收通知和找回密码。
    /// </para>
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// 用户登录密码
    /// <para>
    /// 账户的安全凭据。
    /// 服务端会使用BCrypt算法进行单向哈希处理，
    /// 原始密码不会被明文存储到数据库中。
    /// </para>
    /// 
    /// <para>密码强度建议：</para>
    /// <list type="bullet">
    ///   <item><description>最小长度：6个字符（强制要求）</description></item>
    ///   <item><description>推荐长度：12个字符以上</description></item>
    ///   <item><description>包含大小写字母、数字和特殊字符</description></item>
    ///   <item><description>避免使用常见密码或个人信息</description></item>
    /// </list>
    /// </summary>
    public required string Password { get; set; }
}
