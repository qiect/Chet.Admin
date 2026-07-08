using Serilog;
using Serilog.Events;
using Chet.Admin.Logging;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// Serilog 配置类
/// <para>
/// 提供灵活的日志配置，支持：
/// - 从配置文件读取
/// - 环境区分
/// - 动态日志级别
/// - 结构化日志
/// </para>
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// 配置 Serilog 日志系统
    /// </summary>
    /// <param name="builder">WebApplicationBuilder 实例</param>
    /// <remarks>
    /// <para>配置内容包括：</para>
    /// <list type="bullet">
    ///   <item><description>从 appsettings.json 读取配置</description></item>
    ///   <item><description>根据环境变量调整日志级别</description></item>
    ///   <item><description>添加丰富的上下文信息</description></item>
    ///   <item><description>支持多种输出目标（控制台、文件等）</description></item>
    /// </list>
    /// </remarks>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        // 清除默认的日志提供程序，避免重复输出
        builder.Logging.ClearProviders();
        
        builder.Host.UseSerilog((context, configuration) =>
        {
            // 从配置文件读取基础配置
            configuration.ReadFrom.Configuration(context.Configuration);
            
            // 添加丰富的上下文信息
            configuration.Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithEnvironmentUserName()
                        .Enrich.WithProperty("Application", "Chet.Admin")
                        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
            
            // 添加敏感信息过滤器（可选）
            // 自动过滤包含密码、令牌等敏感信息的日志
            // configuration.Filter.With<SensitiveDataLogFilter>();
            
            // 开发环境特殊配置
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.MinimumLevel.Debug()
                           .WriteTo.Console(
                               outputTemplate: 
                                   "[{Timestamp:HH:mm:ss}] [{Level:u4}] {Message:lj}{NewLine}" +
                                   "    └─ Properties: {Properties}{NewLine}" +
                                   "{Exception}",
                               theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code);
            }
            
            // 生产环境特殊配置
            if (context.HostingEnvironment.IsProduction())
            {
                configuration.MinimumLevel.Information()
                           .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
            }
        });
    }
}
