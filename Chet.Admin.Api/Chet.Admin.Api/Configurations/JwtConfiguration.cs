using Chet.Admin.Configuration;
using Chet.Admin.Contracts.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// JWT认证配置类
/// </summary>
public static class JwtConfiguration
{
    /// <summary>
    /// 配置JWT认证
    /// </summary>
    /// <param name="services">IServiceCollection实例</param>
    /// <param name="appSettings">应用程序配置实例</param>
    public static void ConfigureJwt(this IServiceCollection services, AppSettings appSettings)
    {
        if (appSettings?.Jwt != null && appSettings.Jwt.Enabled)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = appSettings.Jwt.Issuer,
                        ValidAudience = appSettings.Jwt.Audience,
                        // 使用配置中的SecretKey，确保与生成令牌时使用相同的密钥
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Jwt.SecretKey ?? "DefaultJwtSecretKey"))
                    };

                    // JWT过期或无效时返回401而非500
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            // 令牌验证通过后，检查是否在强制下线黑名单中
                            var jwtToken = context.SecurityToken as JwtSecurityToken;
                            if (jwtToken == null)
                            {
                                return Task.CompletedTask;
                            }

                            // 获取用户ID（Sub声明可能被映射为NameIdentifier）
                            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)
                                          ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub);
                            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                            {
                                return Task.CompletedTask;
                            }

                            // 从DI容器解析在线用户服务
                            var onlineUserService = context.HttpContext.RequestServices.GetService<IOnlineUserService>();
                            if (onlineUserService == null)
                            {
                                return Task.CompletedTask;
                            }

                            // 检查令牌是否已被吊销（签发时间早于吊销时间则拒绝）
                            if (onlineUserService.IsTokenRevoked(userId, jwtToken.ValidFrom))
                            {
                                context.Fail("Token has been revoked");
                            }

                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            // Token过期
                            if (context.Exception is SecurityTokenExpiredException)
                            {
                                context.Response.StatusCode = 401;
                                context.Response.ContentType = "application/json";
                                var response = new { success = false, message = "登录已过期，请重新登录", statusCode = 401 };
                                return context.Response.WriteAsJsonAsync(response);
                            }
                            // Token无效
                            if (context.Exception is SecurityTokenException)
                            {
                                context.Response.StatusCode = 401;
                                context.Response.ContentType = "application/json";
                                var response = new { success = false, message = "认证失败，请重新登录", statusCode = 401 };
                                return context.Response.WriteAsJsonAsync(response);
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            // 未携带Token时返回401
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var response = new { success = false, message = "未授权访问，请先登录", statusCode = 401 };
                            return context.Response.WriteAsJsonAsync(response);
                        }
                    };
                });
        }
        else
        {
            // 当JWT禁用时，注册一个允许所有请求的认证方案
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "AllowAll";
            })
            .AddScheme<AuthenticationSchemeOptions, AllowAllAuthenticationHandler>("AllowAll", null);
        }
    }

    /// <summary>
    /// 配置认证和授权中间件
    /// </summary>
    /// <param name="app">WebApplication实例</param>
    /// <param name="appSettings">应用程序配置实例</param>
    public static void ConfigureAuthMiddleware(this WebApplication app, AppSettings appSettings)
    {
        if (appSettings?.Jwt != null && appSettings.Jwt.Enabled)
        {
            // 添加身份认证中间件
            app.UseAuthentication();
            // 添加授权中间件
            app.UseAuthorization();
        }
    }
}

/// <summary>
/// 允许所有请求的认证处理程序，当JWT禁用时使用
/// </summary>
public class AllowAllAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="optionsMonitor">选项监视器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="encoder">URL编码器</param>
    public AllowAllAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> optionsMonitor, ILoggerFactory logger, UrlEncoder encoder) : base(optionsMonitor, logger, encoder)
    { }

    /// <summary>
    /// 处理认证请求，允许所有请求通过
    /// </summary>
    /// <returns>认证结果</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 创建一个包含默认声明的身份
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Anonymous"),
            new Claim(ClaimTypes.Role, "Guest")
        };

        // 创建身份和认证票据
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // 返回成功的认证结果
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
