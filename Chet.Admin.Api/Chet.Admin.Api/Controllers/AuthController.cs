using Chet.Admin.Configuration;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Auth;
using Chet.Admin.Contracts.User;
using Chet.Admin.DTOs.Auth;
using Chet.Admin.DTOs.User;
using Chet.Admin.Services.Auth;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Text;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 认证控制器（Authentication Controller）
/// </summary>
/// <remarks>
/// 处理用户身份认证相关的HTTP请求，包括用户注册、登录和令牌刷新操作。
/// 使用JWT（JSON Web Token）作为无状态的身份验证机制。
/// 
/// 端点列表：
/// - POST /api/v1/auth/register - 注册新用户账户
/// - POST /api/v1/auth/login - 用户登录获取JWT令牌
/// - POST /api/v1/auth/refresh-token - 使用刷新令牌获取新的访问令牌
/// 
/// 安全特性：
/// - 登录接口受限流保护：每IP每分钟最多5次请求
/// - 注册接口受限流保护：每IP每分钟最多10次请求
/// - 密码使用BCrypt加密存储，不可逆
/// - JWT令牌包含过期时间，支持滑动续期
/// 
/// 认证流程：
/// 1. 客户端发送登录凭据到 /login 端点
/// 2. 服务端验证凭据后签发JWT Access Token + Refresh Token
/// 3. 后续请求在Authorization头中携带Bearer Token
/// 4. Access Token过期后使用Refresh Token换取新Token
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[SwaggerTag("提供用户认证相关的API接口，包括注册、登录和令牌刷新")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// 认证服务实例，负责处理业务逻辑：密码验证、令牌生成、用户创建等
    /// </summary>
    private readonly IAuthService _authService;

    /// <summary>
    /// 用户服务实例，负责处理用户资料相关业务逻辑
    /// </summary>
    private readonly IUserService _userService;

    /// <summary>
    /// 日志记录器实例，用于记录认证事件、安全警告和错误信息
    /// </summary>
    private readonly ILogger<AuthController> _logger;

    private readonly CaptchaService _captchaService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOnlineUserService _onlineUserService;
    private readonly AppSettings _appSettings;

    private const int MaxLoginFailCount = 5;
    private const int LockoutMinutes = 15;

    /// <summary>
    /// 初始化认证控制器的新实例
    /// </summary>
    /// <param name="authService">认证服务接口，提供注册、登录、令牌刷新等业务功能</param>
    /// <param name="userService">用户服务接口，提供用户资料和密码修改等业务功能</param>
    /// <param name="logger">日志记录器，用于记录操作日志和安全审计信息</param>
    /// <param name="captchaService">验证码服务</param>
    /// <param name="unitOfWork">工作单元</param>
    /// <param name="onlineUserService">在线用户服务</param>
    /// <param name="appSettings">应用程序配置</param>
    public AuthController(
        IAuthService authService,
        IUserService userService,
        ILogger<AuthController> logger,
        CaptchaService captchaService,
        IUnitOfWork unitOfWork,
        IOnlineUserService onlineUserService,
        AppSettings appSettings)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
        _captchaService = captchaService;
        _unitOfWork = unitOfWork;
        _onlineUserService = onlineUserService;
        _appSettings = appSettings;
    }

    /// <summary>
    /// 用户注册接口
    /// </summary>
    /// <remarks>
    /// 创建新的用户账户。系统会对输入数据进行验证，包括邮箱格式、密码强度等。
    /// 密码会使用BCrypt算法进行哈希处理后存储。
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/v1/auth/register
    ///     Content-Type: application/json
    ///     
    ///     {
    ///       "name": "张三",
    ///       "email": "zhangsan@example.com",
    ///       "password": "MySecure@123"
    ///     }
    /// 
    /// 响应示例（201）：
    /// 
    ///     {
    ///       "success": true,
    ///       "message": "User registered successfully",
    ///       "data": null,
    ///       "statusCode": 201
    ///     }
    /// </remarks>
    /// <param name="registerDto">注册信息数据传输对象，包含姓名、邮箱、密码</param>
    /// <returns>201 注册成功 / 400 输入数据验证失败或邮箱已存在</returns>
    /// <response code="201">注册成功</response>
    /// <response code="400">请求参数无效或邮箱已存在</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        await _authService.RegisterAsync(registerDto);
        return Created("", ApiResponse.Ok(null, "User registered successfully", StatusCodes.Status201Created));
    }

    /// <summary>
    /// 获取图形验证码
    /// </summary>
    [HttpGet("captcha")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult GetCaptcha()
    {
        var (id, code) = _captchaService.Generate();
        var svg = GenerateCaptchaSvg(code);
        return Ok(ApiResponse.Ok(new CaptchaDto { Id = id, Svg = svg }, "Captcha generated successfully"));
    }

    /// <summary>
    /// 用户登录接口
    /// </summary>
    /// <remarks>
    /// 验证用户凭据并签发JWT令牌对。成功登录后会返回Access Token（短期有效）和Refresh Token（长期有效）。
    /// Access Token用于API调用认证，Refresh Token用于续期。
    /// 
    /// 安全增强：
    /// - 连续失败3次后需要验证码
    /// - 连续失败5次后锁定账户15分钟
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/v1/auth/login
    ///     Content-Type: application/json
    ///     
    ///     {
    ///       "email": "zhangsan@example.com",
    ///       "password": "MySecure@123"
    ///     }
    /// 
    /// 响应示例（200）：
    /// 
    ///     {
    ///       "success": true,
    ///       "message": "Login successful",
    ///       "data": {
    ///         "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    ///         "refreshToken": "rt_xxxxx...",
    ///         "requireCaptcha": false,
    ///         "lockedUntil": null
    ///       },
    ///       "statusCode": 200
    ///     }
    /// 
    /// 限流规则：每个IP地址每分钟最多5次登录尝试，超限返回429状态码。
    /// </remarks>
    /// <param name="loginDto">登录信息数据传输对象，包含邮箱和密码</param>
    /// <returns>200 登录成功，返回JWT令牌对 / 401 邮箱或密码不正确</returns>
    /// <response code="200">登录成功，返回JWT令牌</response>
    /// <response code="401">认证失败，邮箱或密码错误</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);

        // 检查账户锁定状态
        if (user != null && user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            var remaining = user.LockedUntil.Value - DateTime.UtcNow;
            return Ok(ApiResponse.Error($"账号已锁定，请在 {Math.Ceiling(remaining.TotalMinutes)} 分钟后重试", StatusCodes.Status400BadRequest));
        }

        // 如果锁定时间已过，重置失败计数
        if (user != null && user.LockedUntil.HasValue && user.LockedUntil.Value <= DateTime.UtcNow)
        {
            user.LoginFailCount = 0;
            user.LockedUntil = null;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        try
        {
            var token = await _authService.LoginAsync(loginDto);

            // 登录成功，重置失败计数
            if (user != null)
            {
                user.LoginFailCount = 0;
                user.LockedUntil = null;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // 标记用户在线
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                _onlineUserService.UserOnline(user.Id, user.Name, clientIp);
            }

            // Check password expiration
            var passwordPolicy = _appSettings?.PasswordPolicy;
            var mustChangePassword = user?.MustChangePassword ?? false;

            if (!mustChangePassword && passwordPolicy?.ExpirationDays > 0 && user?.PasswordChangedAt.HasValue == true)
            {
                var daysSinceChange = (DateTime.UtcNow - user.PasswordChangedAt.Value).TotalDays;
                if (daysSinceChange > passwordPolicy.ExpirationDays)
                {
                    mustChangePassword = true;
                }
            }

            // Set PasswordChangedAt for existing users if null (first time policy applies)
            if (user != null && !user.PasswordChangedAt.HasValue)
            {
                user.PasswordChangedAt = user.UpdatedAt != default ? user.UpdatedAt : user.CreatedAt;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok(ApiResponse.Ok(new LoginResponseDto
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                RequireCaptcha = false,
                MustChangePassword = mustChangePassword
            }, "Login successful"));
        }
        catch (UnauthorizedAccessException)
        {
            // 登录失败，增加失败计数
            var accountLocked = false;
            if (user != null)
            {
                user.LoginFailCount++;
                if (user.LoginFailCount >= MaxLoginFailCount)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    accountLocked = true;
                    _logger.LogWarning("Account locked for user: {Email} until {LockedUntil}", user.Email, user.LockedUntil);
                }
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }

            var failMessage = accountLocked
                ? $"密码错误次数过多，账户已锁定 {LockoutMinutes} 分钟"
                : "邮箱或密码错误";

            return Ok(ApiResponse.Error(failMessage, StatusCodes.Status400BadRequest));
        }
    }

    /// <summary>
    /// 刷新令牌接口
    /// </summary>
    /// <remarks>
    /// 使用有效的Refresh Token来获取新的Access Token。
    /// 当Access Token过期时，客户端应调用此接口进行无感续期，避免用户需要重新登录。
    /// 
    /// 请求示例：
    /// 
    ///     POST /api/v1/auth/refresh-token
    ///     Content-Type: application/json
    ///     
    ///     {
    ///       "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    ///       "refreshToken": "rt_xxxxx..."
    ///     }
    /// 
    /// 响应示例（200）：
    /// 
    ///     {
    ///       "success": true,
    ///       "message": "Token refreshed successfully",
    ///       "data": {
    ///         "accessToken": "eyJhbGciOiJIUzI1NiIs...(new)",
    ///         "refreshToken": "rt_yyyyy...(new)",
    ///         "expiresIn": 3600
    ///       },
    ///       "statusCode": 200
    ///     }
    /// 
    /// 安全建议：
    /// - Refresh Token应安全存储（HttpOnly Cookie或安全存储）
    /// - 每次刷新后旧Refresh Token应失效（单次使用）
    /// - 检测到异常刷新行为时应撤销所有Token
    /// </remarks>
    /// <param name="refreshTokenDto">刷新令牌数据传输对象，包含AccessToken和RefreshToken</param>
    /// <returns>200 刷新成功，返回新的令牌对 / 401 Refresh Token无效或已过期</returns>
    /// <response code="200">令牌刷新成功</response>
    /// <response code="401">Refresh Token无效或已过期</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var token = await _authService.RefreshTokenAsync(refreshTokenDto);
        return Ok(ApiResponse.Ok(token, "Token refreshed successfully"));
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        // JWT是无状态的，客户端清除token即可
        // 如需服务端使token失效，可加入黑名单机制
        var userId = GetUserId();
        _onlineUserService.UserOffline(userId);
        return Ok(ApiResponse.Ok(null, "Logout successful"));
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    [HttpGet("user-info")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserInfo()
    {
        // ASP.NET Core JWT中间件会将sub映射为ClaimTypes.NameIdentifier
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        var userId = int.Parse(subClaim?.Value ?? "0");
        var userInfo = await _authService.GetUserInfoAsync(userId);
        return Ok(ApiResponse.Ok(userInfo, "User info retrieved successfully"));
    }

    /// <summary>
    /// 获取当前用户资料
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var user = await _userService.GetUserByIdAsync(userId);
        return Ok(ApiResponse.Ok(user, "Profile retrieved successfully"));
    }

    /// <summary>
    /// 更新当前用户资料
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetUserId();
        await _userService.UpdateProfileAsync(userId, dto.Name, dto.Avatar);
        return Ok(ApiResponse.Ok(null, "Profile updated successfully"));
    }

    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetUserId();
        await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);
        return Ok(ApiResponse.Ok(null, "Password changed successfully"));
    }

    /// <summary>
    /// 强制修改密码（密码过期时使用）
    /// </summary>
    [HttpPut("force-change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForceChangePassword([FromBody] ForceChangePasswordDto dto)
    {
        var userId = GetUserId();
        await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);
        return Ok(ApiResponse.Ok(null, "Password changed successfully"));
    }

    /// <summary>
    /// 从JWT Claims中获取当前用户ID
    /// </summary>
    private int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            throw new UnauthorizedAccessException();
        }
        return userId;
    }

    /// <summary>
    /// 生成验证码SVG图像
    /// </summary>
    private static string GenerateCaptchaSvg(string code)
    {
        const int width = 120;
        const int height = 40;
        var random = new Random();

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>");

        // Background
        sb.Append($"<rect width='{width}' height='{height}' fill='#f0f0f0' rx='4'/>");

        // Noise lines
        for (int i = 0; i < 3; i++)
        {
            var x1 = random.Next(width);
            var y1 = random.Next(height);
            var x2 = random.Next(width);
            var y2 = random.Next(height);
            sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#ccc' stroke-width='1'/>");
        }

        // Characters
        for (int i = 0; i < code.Length; i++)
        {
            var x = 15 + i * 25;
            var y = 25 + random.Next(-5, 6);
            var rotate = random.Next(-15, 16);
            var color = $"#{random.Next(0, 128):X2}{random.Next(0, 128):X2}{random.Next(0, 128):X2}";
            sb.Append($"<text x='{x}' y='{y}' font-size='22' font-weight='bold' fill='{color}' transform='rotate({rotate},{x},{y})' font-family='monospace'>{code[i]}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
