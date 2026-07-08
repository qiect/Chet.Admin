using Chet.Admin.Shared;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// FluentValidation 配置类
/// <para>
/// 负责配置 FluentValidation 验证框架，包括：
/// - 自动验证功能
/// - 验证器注册
/// - 自定义错误响应格式
/// </para>
/// </summary>
public static class FluentValidationConfiguration
{
    /// <summary>
    /// 配置 FluentValidation 验证框架
    /// </summary>
    /// <param name="services">IServiceCollection 实例</param>
    /// <remarks>
    /// <para>配置内容包括：</para>
    /// <list type="bullet">
    ///   <item><description>启用自动验证：在模型绑定时自动执行验证</description></item>
    ///   <item><description>禁用 DataAnnotations：确保只使用 FluentValidation</description></item>
    ///   <item><description>注册验证器：从 DTOs 程序集自动扫描并注册所有验证器</description></item>
    ///   <item><description>自定义错误响应：将验证错误转换为统一的 ApiResponse 格式</description></item>
    /// </list>
    /// </remarks>
    public static void ConfigureFluentValidation(this IServiceCollection services)
    {
        // 启用 FluentValidation 自动验证
        services.AddFluentValidationAutoValidation(options =>
        {
            // 禁用 DataAnnotations 验证，避免与 FluentValidation 冲突
            options.DisableDataAnnotationsValidation = true;
        });
        
        // 从 DTOs 程序集中扫描并注册所有验证器
        services.AddValidatorsFromAssembly(Assembly.Load("Chet.Admin.DTOs"));
        
        // 配置自定义的模型验证错误响应
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // 自定义无效模型状态的响应工厂
            options.InvalidModelStateResponseFactory = context =>
            {
                // 提取所有验证错误，按字段名分组
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray()
                    );

                // 构造统一的验证错误响应格式
                var response = new ValidationErrorResponse
                {
                    StatusCode = 400,
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors,
                    // 使用 UTC 时间确保时间一致性
                    Timestamp = DateTime.UtcNow
                };

                // 返回 400 Bad Request 响应
                return new BadRequestObjectResult(response)
                {
                    ContentTypes = { "application/json" }
                };
            };
        });
    }
}
