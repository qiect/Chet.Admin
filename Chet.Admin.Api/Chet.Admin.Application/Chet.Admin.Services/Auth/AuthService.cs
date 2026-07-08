using AutoMapper;
using Chet.Admin.Configuration;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Auth;
using Chet.Admin.Contracts.Jwt;
using Chet.Admin.Contracts.Security;
using Chet.Admin.Contracts.User;
using Chet.Admin.Contracts.Role;
using Chet.Admin.Contracts.Permission;
using Chet.Admin.Domain.User;
using Chet.Admin.DTOs.Auth;
using Chet.Admin.DTOs.User;
using Chet.Admin.Shared;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Auth;

/// <summary>
/// 认证服务实现类，实现了 IAuthService 接口
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly AppSettings _appSettings;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IPasswordService passwordService,
        IMapper mapper,
        ILogger<AuthService> logger,
        AppSettings appSettings,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _mapper = mapper;
        _logger = logger;
        _appSettings = appSettings;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="loginDto">登录信息</param>
    /// <returns>JWT令牌对（包含访问令牌和刷新令牌）</returns>
    public async Task<JwtTokenDto> LoginAsync(LoginDto loginDto)
    {
        _logger.LogInformation("User login attempt: {Email}", loginDto.Email);

        var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);
        
        if (user == null || !_passwordService.Verify(loginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid login attempt: {Email}", loginDto.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var accessToken = await _jwtService.GenerateAccessTokenAsync(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            // 使用 UTC 时间计算过期时间，确保跨时区一致性
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_appSettings.Jwt?.RefreshTokenExpirationDays ?? 7);

            _unitOfWork.Users.Update(user);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("User login successful: {Email}", loginDto.Email);

            return new JwtTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for: {Email}", loginDto.Email);
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="registerDto">注册信息</param>
    public async Task RegisterAsync(RegisterDto registerDto)
    {
        _logger.LogInformation("User registration attempt: {Email}", registerDto.Email);

        var existingUser = await _unitOfWork.Users.GetByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("User registration failed: Email already exists: {Email}", registerDto.Email);
            throw new BadRequestException("Email already exists");
        }

        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var user = _mapper.Map<UserEntity>(registerDto);
            user.PasswordHash = _passwordService.Hash(registerDto.Password);
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("User registration successful: {Email}", registerDto.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for: {Email}", registerDto.Email);
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 刷新JWT令牌
    /// </summary>
    /// <param name="refreshTokenDto">刷新令牌请求信息</param>
    /// <returns>新的JWT令牌对</returns>
    public async Task<JwtTokenDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        _logger.LogInformation("Refresh token attempt");

        var token = await _jwtService.RefreshTokenAsync(refreshTokenDto.AccessToken, refreshTokenDto.RefreshToken);
        return token;
    }

    /// <summary>
    /// 获取当前用户信息（包含角色和权限）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户信息数据传输对象</returns>
    public async Task<UserInfoDto> GetUserInfoAsync(int userId)
    {
        _logger.LogInformation("Get user info attempt: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            throw new UnauthorizedAccessException("User not found");
        }

        // 获取角色
        var roles = await _roleRepository.GetRolesByUserIdAsync(userId);
        // 获取权限
        var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(userId);

        var userInfo = new UserInfoDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            DepartmentId = user.DepartmentId,
            Roles = roles.Select(r => r.Code).ToList(),
            Permissions = permissions.Select(p => p.Code).ToList(),
        };

        return userInfo;
    }
}
