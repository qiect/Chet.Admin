using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// API版本控制配置扩展类
/// <para>
/// 提供API版本控制相关的服务注册和配置功能。
/// 支持通过URL路径段、请求头或查询参数来指定API版本。
/// </para>
/// </summary>
/// <remarks>
/// <para>使用示例：</para>
/// <code>
/// // 在Program.cs中调用
/// builder.Services.ConfigureApiVersioning();
/// </code>
/// 
/// <para>支持的版本识别方式（按优先级）：</para>
/// <list type="number">
///   <item><description>URL段：/api/v1/users</description></item>
///   <item><description>请求头：X-API-Version: 1.0</description></item>
///   <item><description>查询参数：?api-version=1.0</description></item>
/// </list>
/// </remarks>
public static class ApiVersionConfiguration
{
    /// <summary>
    /// 配置API版本控制服务
    /// </summary>
    /// <param name="services">依赖注入服务集合</param>
    /// <remarks>
    /// <para>配置内容：</para>
    /// <list type="bullet">
    ///   <item><description>默认API版本：v1.0</description></item>
    ///   <item><description>未指定版本时使用默认版本</description></item>
    ///   <item><description>响应中包含API版本信息</description></item>
    ///   <item><description>支持URL段、Header、Query三种版本识别方式</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.ConfigureApiVersioning();
    /// </code>
    /// </example>
    public static void ConfigureApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-API-Version"),
                new QueryStringApiVersionReader("api-version")
            );
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
    }
}
