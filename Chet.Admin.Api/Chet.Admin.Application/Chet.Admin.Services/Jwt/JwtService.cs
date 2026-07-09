using Chet.Admin.Configuration;
using Chet.Admin.Contracts.Jwt;
using Chet.Admin.Contracts.User;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Menu;
using Chet.Admin.Domain.User;
using Chet.Admin.DTOs.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Chet.Admin.Services.Jwt;

/// <summary>
/// JWT服务实现类（JWT Service Implementation）
/// <para>
/// 实现IJwtService接口，提供JWT令牌的生成、验证和刷新功能。
/// 使用HMAC-SHA256对称加密算法对令牌进行签名，确保令牌的完整性和真实性。
/// </para>
/// </summary>
/// <remarks>
/// <para>核心功能：</para>
/// <list type="number">
///   <item><description>Access Token生成：包含用户身份信息的短期令牌</description></item>
///   <item><description>Refresh Token生成：使用密码学安全随机数生成的长期令牌</description></item>
///   <item><description>令牌刷新：验证旧令牌并签发新的令牌对</description></item>
/// </list>
/// 
/// <para>安全特性：</para>
/// <list type="bullet">
///   <item><description>使用HMAC-SHA256算法进行数字签名</description></item>
///   <item><description>Refresh Token使用加密安全的随机数生成器（CSPRNG）</description></item>
///   <item><description>每次刷新都会更换新的Refresh Token（Rotation机制）</description></item>
///   <item><description>支持自定义过期时间和密钥配置</description></item>
/// </list>
/// 
/// <para>配置说明：</para>
/// <para>通过AppSettings.Jwt配置节设置：</para>
/// <code>
/// {
///   "Jwt": {
///     "SecretKey": "your-256-bit-secret-key",
///     "Issuer": "YourApp",
///     "Audience": "YourAudience",
///     "AccessTokenExpirationMinutes": 30,
///     "RefreshTokenExpirationDays": 7
///   }
/// }
/// </code>
/// 
/// <para>依赖项：</para>
/// <list type="bullet">
///   <item><description>AppSettings：读取JWT配置参数</description></item>
///   <item><description>IUserRepository：查询和更新用户的Refresh Token</description></item>
///   <item><description>ILogger：记录令牌操作日志用于审计</description></item>
/// </list>
/// </remarks>
public class JwtService : IJwtService
{
    private readonly AppSettings _appSettings;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<JwtService> _logger;

    /// <summary>
    /// 初始化JWT服务实例
    /// </summary>
    /// <param name="appSettings">应用程序配置对象，包含JWT相关的配置参数</param>
    /// <param name="userRepository">用户仓储实例，用于查询用户和更新Refresh Token</param>
    /// <param name="roleRepository">角色仓储实例，用于查询用户角色</param>
    /// <param name="menuRepository">菜单仓储实例，用于查询用户权限</param>
    /// <param name="logger">日志记录器，用于记录令牌操作和异常信息</param>
    public JwtService(AppSettings appSettings, IUserRepository userRepository, IRoleRepository roleRepository, IMenuRepository menuRepository, ILogger<JwtService> logger)
    {
        _appSettings = appSettings;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _logger = logger;
    }

    /// <summary>
    /// 生成访问令牌（Access Token）
    /// <para>
    /// 根据用户信息创建包含标准声明的JWT令牌。
    /// 令牌包含以下声明（Claims）：
    /// <list type="bullet">
    ///   <item><description>Sub：用户ID，用于标识用户身份</description></item>
    ///   <item><description>Email：用户邮箱地址</description></item>
    ///   <item><description>Jti：JWT唯一标识符，用于防止重放攻击</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="user">用户实体对象，包含ID和邮箱等信息</param>
    /// <returns>
    /// Base64URL编码的JWT字符串，
    /// 有效期由配置中的AccessTokenExpirationMinutes决定（默认30分钟）
    /// </returns>
    public async Task<string> GenerateAccessTokenAsync(UserEntity user)
    {
        _logger.LogInformation("Generating access token for user: {Email}", user.Email);

        // 定义 JWT 声明
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // 主题，用户ID
            new Claim(JwtRegisteredClaimNames.Email, user.Email), // 邮箱
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID，唯一标识符
            new Claim(ClaimTypes.Name, user.Name), // 用户名，供审计中间件读取
        };

        // 添加角色Claims
        var roles = await _roleRepository.GetRolesByUserIdAsync(user.Id);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Code));
        }

        // 添加权限Claims
        var permissions = await _menuRepository.GetPermissionCodesByUserIdAsync(user.Id);
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // 使用框架中设计好的AppSettings配置
        var jwtSettings = _appSettings.Jwt ?? new JwtSettings();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey ?? "DefaultJwtSecretKeyForJWTAuthentication1234567890"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 创建 JWT 令牌
        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer, // 发行者
            audience: jwtSettings.Audience, // 受众
            claims: claims, // 声明
            notBefore: DateTime.UtcNow, // 生效时间（UTC），供强制下线黑名单比较使用
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes > 0 ? jwtSettings.AccessTokenExpirationMinutes : 30), // 过期时间（UTC）
            signingCredentials: creds); // 签名凭据

        // 生成 JWT 字符串
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 生成刷新令牌（Refresh Token）
    /// <para>
    /// 使用密码学安全伪随机数生成器（CSPRNG）生成32字节的随机数，
    /// 并转换为Base64编码字符串。
    /// 每次调用都会生成唯一的、不可预测的令牌值。
    /// </para>
    /// </summary>
    /// <returns>
    /// 44字符的Base64编码字符串（32字节随机数），
    /// 具有足够的熵来抵御暴力破解和猜测攻击
    /// </returns>
    public string GenerateRefreshToken()
    {
        _logger.LogInformation("Generating refresh token");

        // 使用加密安全的随机数生成器生成 32 字节的随机数
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber); // 转换为 Base64 字符串
        }
    }

    /// <summary>
    /// 从过期的访问令牌中提取声明主体（ClaimsPrincipal）
    /// <para>
    /// 解析并验证JWT令牌的签名，但不验证其过期时间（ValidateLifetime = false）。
    /// 主要用于令牌刷新流程中从旧的Access Token恢复用户身份信息。
    /// </para>
    /// </summary>
    /// <param name="token">
    /// 已过期或即将过期的访问令牌字符串。
    /// 令牌必须具有有效的签名且使用HMAC-SHA256算法
    /// </param>
    /// <returns>
    /// 包含用户身份声明的ClaimsPrincipal对象，
    /// 可通过principal.FindFirst(ClaimTypes.NameIdentifier)获取用户ID
    /// </returns>
    /// <exception cref="SecurityTokenException">
    /// 当令牌签名无效、算法不匹配或格式错误时抛出
    /// </exception>
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        _logger.LogInformation("Getting principal from expired token");

        // 使用框架中设计好的AppSettings配置
        var jwtSettings = _appSettings.Jwt ?? new JwtSettings();

        // 配置令牌验证参数，不验证过期时间
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey ?? "DefaultJwtSecretKeyForJWTAuthentication1234567890")),
            ValidateLifetime = false // 不验证令牌过期时间
        };

        var tokenHandler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        // 验证令牌算法是否为 HmacSha256
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    /// <summary>
    /// 使用刷新令牌获取新的令牌对（Token Refresh Flow）
    /// <para>
    /// 完整的令牌刷新流程：
    /// <list type="number">
    ///   <item><description>从过期的Access Token中解析用户身份（不验证过期时间）</description></item>
    ///   <item><description>根据用户ID查询数据库获取用户信息</description></item>
    ///   <item><description>验证Refresh Token的有效性：
    ///     <list type="bullet">
    ///       <item>用户必须存在</item>
    ///       <item>Refresh Token必须与数据库中的值匹配</item>
    ///       <item>Refresh Token未过期</item>
    ///     </list>
    ///   </description></item>
    ///   <item><description>生成全新的Access Token和Refresh Token</description></item>
    ///   <item><description>将新Refresh Token持久化到数据库</description></item>
    ///   <item><description>返回新的令牌对给客户端</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>安全机制 - Refresh Token Rotation：</para>
    /// <para>
    /// 每次成功刷新后都会生成新的Refresh Token并使旧的失效。
    /// 这可以防止Refresh Token被窃取后的长期滥用，
    /// 因为窃取的Token只能使用一次就会被替换掉。
    /// </para>
    /// </summary>
    /// <param name="accessToken">
    /// 当前（可能已过期）的访问令牌，
    /// 用于从中提取用户身份信息（Sub声明包含用户ID）
    /// </param>
    /// <param name="refreshToken">
    /// 刷新令牌，必须与数据库中存储的值完全匹配且未过期
    /// </param>
    /// <returns>
    /// 包含全新Access Token和Refresh Token的数据传输对象
    /// </returns>
    /// <exception cref="SecurityTokenException">
    /// 当以下情况之一发生时抛出：
    /// <list type="bullet">
    ///   <item><description>Access Token无效或签名错误</description></item>
    ///   <item><description>用户不存在或已被删除</description></item>
    ///   <item><description>Refresh Token与数据库记录不匹配</description></item>
    ///   <item><description>Refresh Token已过期</description></item>
    /// </list>
    /// </exception>
    public async Task<JwtTokenDto> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        _logger.LogInformation("Refreshing token");

        ClaimsPrincipal principal;
        try
        {
            // 从过期令牌获取声明主体
            principal = GetPrincipalFromExpiredToken(accessToken);
        }
        catch (SecurityTokenException)
        {
            _logger.LogWarning("Refresh token failed: invalid access token");
            throw new SecurityTokenException("Invalid access token");
        }

        // 获取用户ID
        var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId) || userId <= 0)
        {
            _logger.LogWarning("Refresh token failed: invalid user id in access token");
            throw new SecurityTokenException("Invalid access token");
        }

        // 获取用户信息
        var user = await _userRepository.GetByIdAsync(userId);

        // 验证刷新令牌
        if (user == null)
        {
            _logger.LogWarning("Refresh token failed: user not found (userId: {UserId})", userId);
            throw new SecurityTokenException("User not found");
        }

        if (user.RefreshToken != refreshToken)
        {
            _logger.LogWarning("Refresh token failed: refresh token mismatch (userId: {UserId})", userId);
            throw new SecurityTokenException("Invalid refresh token");
        }

        if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token failed: refresh token expired (userId: {UserId})", userId);
            throw new SecurityTokenException("Refresh token expired");
        }

        // 生成新的访问令牌和刷新令牌
        var newAccessToken = await GenerateAccessTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();

        // 更新用户的刷新令牌和过期时间
        user.RefreshToken = newRefreshToken;
        var jwtSettings = _appSettings.Jwt ?? new JwtSettings();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(jwtSettings.RefreshTokenExpirationDays > 0 ? jwtSettings.RefreshTokenExpirationDays : 7);
        
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        // 返回新的令牌对
        return new JwtTokenDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }
}
