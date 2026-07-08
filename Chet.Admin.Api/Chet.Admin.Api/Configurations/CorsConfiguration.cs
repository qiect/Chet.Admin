namespace Chet.Admin.Api.Configurations;

/// <summary>
/// 跨域资源共享（CORS）配置扩展类
/// <para>
/// 提供CORS策略的配置功能，用于控制跨域HTTP请求。
/// 允许指定的前端应用域名访问API资源，同时保护API免受未授权的跨域访问。
/// </para>
/// </summary>
/// <remarks>
/// <para>什么是CORS？</para>
/// <para>
/// 跨源资源共享（Cross-Origin Resource Sharing）是一种基于HTTP头的机制，
/// 服务器通过这些头告诉浏览器允许哪些Web应用访问其资源。
/// 这是浏览器同源策略（Same-Origin Policy）的放宽。
/// </para>
/// 
/// <para>何时需要配置CORS？</para>
/// <list type="bullet">
///   <item><description>前后端分离架构，前端运行在不同端口/域名</description></item>
///   <item><description>SPA（单页应用）调用后端API</description></item>
///   <item><description>第三方应用集成</description></item>
/// </list>
/// 
/// <para>配置示例（appsettings.json）：</para>
/// <code>
/// {
///   "Cors": {
///     "AllowedOrigins": [
///       "http://localhost:3000",
///       "http://localhost:5173",
///       "https://example.com"
///     ]
///   }
/// }
/// </code>
/// </remarks>
public static class CorsConfiguration
{
    /// <summary>
    /// 配置跨域资源共享（CORS）服务
    /// </summary>
    /// <param name="services">依赖注入服务集合</param>
    /// <param name="configuration">应用程序配置，用于读取允许的源列表</param>
    /// <remarks>
    /// <para>配置的策略名称：<c>DefaultPolicy</c></para>
    /// 
    /// <para>策略特性：</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>配置项</term>
    ///     <description>说明</description>
    ///   </listheader>
    ///   <item>
    ///     <term>AllowedOrigins</term>
    ///     <description>允许的源列表，从appsettings.json读取</description>
    ///   </item>
    ///   <item>
    ///     <term>AllowAnyHeader</term>
    ///     <description>允许所有请求头（Content-Type、Authorization等）</description>
    ///   </item>
    ///   <item>
    ///     <term>AllowAnyMethod</term>
    ///     <description>允许所有HTTP方法（GET、POST、PUT、DELETE等）</description>
    ///   </item>
    ///   <item>
    ///     <term>AllowCredentials</term>
    ///     <description>允许携带凭据（Cookie、Authorization头等）</description>
    ///   </item>
    ///   <item>
    ///     <term>PreflightMaxAge</term>
    ///     <description>预检请求缓存时间1小时，减少OPTIONS请求数量</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>使用方式：</para>
    /// <code>
    /// // 1. 注册服务
    /// builder.Services.ConfigureCors(builder.Configuration);
    /// 
    /// // 2. 在中间件管道中使用
    /// app.UseCors("DefaultPolicy");
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // 配置CORS
    /// builder.Services.ConfigureCors(builder.Configuration);
    /// 
    /// var app = builder.Build();
    /// 
    /// // 应用CORS中间件（必须在UseAuthorization之前）
    /// app.UseCors("DefaultPolicy");
    /// </code>
    /// </example>
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });
    }
}
