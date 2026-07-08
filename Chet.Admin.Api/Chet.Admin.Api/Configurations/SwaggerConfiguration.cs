using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// Swagger配置扩展类
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// 配置Swagger服务，支持API版本控制
    /// </summary>
    /// <param name="services">依赖注入服务集合</param>
    /// <remarks>
    /// 必须在 ConfigureApiVersioning() 之后调用。
    /// 通过 IConfigureOptions 动态为每个API版本生成独立的Swagger文档。
    /// </remarks>
    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
    }

    /// <summary>
    /// 配置Swagger UI中间件
    /// </summary>
    /// <param name="app">WebApplication实例</param>
    public static void ConfigureSwaggerUI(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"API {description.GroupName.ToUpperInvariant()}");
                }
            });
        }
    }
}

/// <summary>
/// Swagger生成选项配置器，根据API版本动态创建Swagger文档
/// </summary>
internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    /// <summary>
    /// 初始化 ConfigureSwaggerOptions 的新实例
    /// </summary>
    /// <param name="provider">API版本描述提供者</param>
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    /// <summary>
    /// 配置Swagger生成选项，为每个API版本创建文档并添加JWT认证支持
    /// </summary>
    /// <param name="options">Swagger生成选项</param>
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        options.EnableAnnotations();
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "Chet.Admin",
            Version = description.ApiVersion.ToString(),
            Description = "基于.NET 10的WebAPI模板框架，提供用户认证和管理功能"
        };

        if (description.IsDeprecated)
        {
            info.Description += " (已弃用)";
        }

        return info;
    }
}
