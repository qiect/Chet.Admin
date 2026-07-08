using Chet.Admin.Domain.User;
using Chet.Admin.DTOs.Auth;
using System.Security.Claims;

namespace Chet.Admin.Contracts.Jwt
{
    /// <summary>
    /// JWT服务接口（JWT Service Interface）
    /// <para>
    /// 定义了JSON Web Token（JWT）令牌的生成、验证和刷新操作。
    /// JWT是一种无状态的身份认证机制，用于在客户端和服务端之间安全地传输用户身份信息。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>JWT令牌类型：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>令牌类型</term>
    ///     <description>说明</description>
    ///     <term>有效期</term>
    ///   </listheader>
    ///   <item>
    ///     <term>Access Token（访问令牌）</term>
    ///     <description>用于API调用的短期令牌，包含用户身份信息</description>
    ///     <term>通常30分钟-2小时</term>
    ///   </item>
    ///   <item>
    ///     <term>Refresh Token（刷新令牌）</term>
    ///     <description>用于获取新Access Token的长期令牌，存储在服务端或HttpOnly Cookie中</description>
    ///     <term>通常7天-30天</term>
    ///   </item>
    /// </list>
    /// 
    /// <para>设计原则：</para>
    /// <list type="bullet">
    ///   <item><description>无状态：服务端不存储Session，便于水平扩展</description></item>
    ///   <item><description>跨域支持：适合前后端分离架构和微服务架构</description></item>
    ///   <item><description>安全性：使用HMAC-SHA256签名算法防止篡改</description></item>
    ///   <item><description>双令牌机制：Access Token短期有效+Refresh Token长期有效，平衡安全性和用户体验</description></item>
    /// </list>
    /// 
    /// <para>JWT结构：</para>
    /// <code>
    /// {Header}.{Payload}.{Signature}
    /// 
    /// Header: {"alg": "HS256", "typ": "JWT"}
    /// Payload: {"sub": "123", "email": "user@example.com", "exp": 1234567890}
    /// Signature: HMACSHA256(base64Url(header) + "." + base64Url(payload), secret)
    /// </code>
    /// 
    /// <para>使用场景：</para>
    /// <list type="number">
    ///   <item><description>用户登录后签发令牌对（Access Token + Refresh Token）</description></item>
    ///   <item><description>API请求时携带Access Token进行身份认证</description></item>
    ///   <item><description>Access Token过期后使用Refresh Token获取新的令牌对</description></item>
    ///   <item><description>用户登出时使Refresh Token失效</description></item>
    /// </list>
    /// </remarks>
    public interface IJwtService
    {
        /// <summary>
        /// 生成访问令牌（Access Token）
        /// <para>
        /// 根据用户信息创建包含用户身份声明的JWT令牌。
        /// Access Token有效期较短（通常30分钟），用于每次API请求的身份验证。
        /// </para>
        /// </summary>
        /// <param name="user">用户实体对象，用于提取用户ID和邮箱等信息</param>
        /// <returns>
        /// Base64URL编码的JWT字符串，格式为：{Header}.{Payload}.{Signature}
        /// </returns>
        Task<string> GenerateAccessTokenAsync(UserEntity user);

        /// <summary>
        /// 生成刷新令牌（Refresh Token）
        /// <para>
        /// 使用加密安全的随机数生成器创建唯一的刷新令牌。
        /// Refresh Token有效期较长（通常7天），用于在Access Token过期后获取新的令牌对。
        /// </para>
        /// </summary>
        /// <returns>
        /// 随机生成的Base64编码字符串（32字节随机数）
        /// </returns>
        string GenerateRefreshToken();

        /// <summary>
        /// 从过期的访问令牌中提取声明主体
        /// <para>
        /// 解析并验证过期的JWT令牌，但不检查其过期时间。
        /// 主要用于Refresh Token流程中从旧Access Token恢复用户身份信息。
        /// </para>
        /// </summary>
        /// <param name="token">
        /// 已过期的访问令牌字符串。
        /// 虽然已过期，但签名必须有效且算法必须是HMAC-SHA256
        /// </param>
        /// <returns>
        /// 包含用户身份声明的ClaimsPrincipal对象，
        /// 可通过FindFirst()方法提取特定声明（如Sub、Email等）
        /// </returns>
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

        /// <summary>
        /// 使用刷新令牌获取新的令牌对
        /// <para>
        /// 完整的令牌刷新流程：
        /// 1. 从过期的Access Token中解析用户身份
        /// 2. 验证Refresh Token的有效性（存在、未过期、匹配）
        /// 3. 生成新的Access Token和Refresh Token
        /// 4. 更新数据库中的Refresh Token记录
        /// 5. 返回新的令牌对给客户端
        /// </para>
        /// </summary>
        /// <param name="accessToken">
        /// 当前（可能已过期）的访问令牌，
        /// 用于从中提取用户身份信息
        /// </param>
        /// <param name="refreshToken">
        /// 刷新令牌，必须与数据库中存储的值匹配且未过期
        /// </param>
        /// <returns>
        /// 包含新的Access Token和Refresh Token的数据传输对象
        /// </returns>
        Task<JwtTokenDto> RefreshTokenAsync(string accessToken, string refreshToken);
    }
}
