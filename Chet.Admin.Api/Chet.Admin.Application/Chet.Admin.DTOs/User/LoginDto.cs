namespace Chet.Admin.DTOs.User;

/// <summary>
/// 用户登录数据传输对象（Login Data Transfer Object）
/// <para>
/// 用于接收用户登录认证的HTTP请求数据。
/// 包含登录所需的邮箱和密码凭据。
/// </para>
/// </summary>
/// <remarks>
/// <para>使用场景：</para>
/// <list type="bullet">
///   <item><description>用户通过邮箱和密码进行身份认证</description></item>
///   <item><description>获取JWT访问令牌和刷新令牌</description></item>
///   <item><description>移动端、Web端、桌面客户端的统一登录接口</description></item>
/// </list>
/// 
/// <para>验证规则：</para>
/// <list type="table">
///   <listheader>
///     <term>字段</term>
///     <description>验证规则</description>
///   </listheader>
///   <item>
///     <term>Email</term>
///     <description>必填，有效邮箱格式</description>
///   </item>
///   <item>
///     <term>Password</term>
///     <description>必填，无长度限制（由前端控制）</description>
///   </item>
/// </list>
/// 
/// <para>请求示例：</para>
/// <code>
/// POST /api/v1/auth/login
/// Content-Type: application/json
/// 
/// {
///   "email": "zhangsan@example.com",
///   "password": "MySecure@123"
/// }
/// 
/// 响应示例（200 OK）：
/// {
///   "success": true,
///   "message": "Login successful",
///   "data": {
///     "accessToken": "eyJhbGciOiJIUzI1NiIs...",
///     "refreshToken": "rt_xxxxx...",
///     "expiresIn": 3600
///   },
///   "statusCode": 200
/// }
/// </code>
/// 
/// <para>安全机制：</para>
/// <list type="number">
///   <item><description>密码使用BCrypt算法进行哈希比对</description></item>
///   <item><description>登录失败次数限制（防暴力破解）</description></item>
///   <item><description>成功登录后签发短期Access Token + 长期Refresh Token</description></item>
/// </list>
/// </remarks>
public class LoginDto
{
    /// <summary>
    /// 用户邮箱地址
    /// <para>
    /// 作为主要登录标识符。
    /// 系统会根据此邮箱在数据库中查找对应用户，
    /// 然后验证密码哈希是否匹配。
    /// </para>
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// 用户密码
    /// <para>
    /// 登录凭据的一部分。
    /// 服务端会使用BCrypt.Verify()方法与数据库中的密码哈希进行安全比对。
    /// 明文密码不会被记录或存储。
    /// </para>
    /// </summary>
    public required string Password { get; set; }

    /// <summary>验证码ID</summary>
    public string? CaptchaId { get; set; }

    /// <summary>验证码内容</summary>
    public string? CaptchaCode { get; set; }
}
