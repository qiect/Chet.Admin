namespace Chet.Admin.DTOs.Auth;

/// <summary>
/// JWT令牌数据传输对象（JWT Token Data Transfer Object）
/// <para>
/// 用于在客户端和服务端之间传输JWT令牌对。
/// 包含访问令牌（Access Token）和刷新令牌（Refresh Token），
/// 是用户登录或令牌刷新成功后的标准响应格式。
/// </para>
/// </summary>
/// <remarks>
/// <para>令牌说明：</para>
/// <list type="table">
///   <listheader>
///     <term>令牌类型</term>
///     <description>用途</description>
///     <term>有效期</term>
///     <term>存储位置</term>
///   </listheader>
///   <item>
///     <term>AccessToken</term>
///     <description>每次API请求时携带在Authorization头中</description>
///     <term>30分钟（可配置）</description>
///     <term>内存/SessionStorage</term>
///   </item>
///   <item>
///     <term>RefreshToken</term>
///     <description>当Access Token过期后用于获取新令牌对</description>
///     <term>7天（可配置）</description>
///     <term>HttpOnly Cookie / 安全存储</term>
///   </item>
/// </list>
/// 
/// <para>响应示例：</para>
/// <code>
/// {
///   "success": true,
///   "message": "Login successful",
///   "data": {
///     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
///     "refreshToken": "rt_abc123def456...",
///     "expiresIn": 1800
///   },
///   "statusCode": 200
/// }
/// </code>
/// 
/// <para>使用流程：</para>
/// <list type="number">
///   <item><description>登录成功后，客户端保存两个Token到安全存储</description></item>
///   <item><description>后续API请求在Header中携带：Authorization: Bearer {accessToken}</description></item>
///   <item><description>当收到401 Unauthorized且Token过期时，使用refreshToken调用刷新接口</description></item>
///   <item><description>刷新成功后，更新本地存储的两个Token，重新发送原请求</description></item>
/// </list>
/// 
/// <para>安全建议：</para>
/// <list type="bullet">
///   <item><description>Refresh Token应存储在HttpOnly Cookie中防止XSS攻击</description></item>
///   <item><description>不要将Token存储在localStorage中（易受XSS攻击）</description></item>
///   <item><description>实现Token黑名单机制支持主动注销</description></item>
/// </list>
/// </remarks>
public class JwtTokenDto
{
    /// <summary>
    /// JWT访问令牌（Access Token）
    /// <para>
    /// 短期有效的JWT令牌，包含用户身份信息（Sub、Email等声明）。
    /// 每次调用需要认证的API时都需要在HTTP Header中携带：
    /// <code>Authorization: Bearer {accessToken}</code>
    /// </para>
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// 刷新令牌（Refresh Token）
    /// <para>
    /// 长期有效的随机令牌，用于在Access Token过期后获取新的令牌对。
    /// 应该安全存储（如HttpOnly Cookie），不应暴露给JavaScript代码。
    /// </para>
    /// </summary>
    public string? RefreshToken { get; set; }
}
