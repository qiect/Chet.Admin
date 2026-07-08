using Chet.Admin.DTOs.Auth;
using Chet.Admin.DTOs.User;

namespace Chet.Admin.Contracts.Auth
{
    /// <summary>
    /// 认证服务接口，定义了用户认证相关的业务逻辑操作
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="loginDto">登录DTO，包含邮箱和密码</param>
        /// <returns>JWT令牌DTO，包含访问令牌和刷新令牌</returns>
        Task<JwtTokenDto> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="registerDto">注册DTO，包含用户名、邮箱和密码</param>
        Task RegisterAsync(RegisterDto registerDto);

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        /// <param name="refreshTokenDto">刷新令牌DTO，包含当前访问令牌和刷新令牌</param>
        /// <returns>新的JWT令牌DTO</returns>
        Task<JwtTokenDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息DTO</returns>
        Task<UserInfoDto> GetUserInfoAsync(int userId);
    }
}
