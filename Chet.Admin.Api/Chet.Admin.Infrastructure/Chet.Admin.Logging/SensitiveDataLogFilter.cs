using Serilog.Core;
using Serilog.Events;

namespace Chet.Admin.Logging;

/// <summary>
/// 敏感信息日志过滤器
/// <para>
/// 自动过滤日志中的敏感信息（如密码、令牌等），防止敏感数据泄露到日志文件中。
/// 实现了 Serilog 的 ILogEventFilter 接口，可以在日志管道中自动应用。
/// </para>
/// </summary>
/// <remarks>
/// <para>过滤规则：</para>
/// <list type="bullet">
///   <item><description>检查所有日志属性名称</description></item>
///   <item><description>匹配预定义的敏感关键词</description></item>
///   <item><description>不区分大小写进行匹配</description></item>
///   <item><description>匹配成功则过滤该日志事件</description></item>
/// </list>
/// 
/// <para>默认过滤的敏感关键词：</para>
/// <list type="bullet">
///   <item><description>password - 密码</description></item>
///   <item><description>token - 令牌</description></item>
///   <item><description>secret - 密钥</description></item>
///   <item><description>apikey - API密钥</description></item>
///   <item><description>authorization - 授权信息</description></item>
///   <item><description>creditcard - 信用卡号</description></item>
///   <item><description>ssn - 社会安全号</description></item>
/// </list>
/// 
/// <para>使用示例：</para>
/// <code>
/// // 在 Serilog 配置中添加过滤器
/// builder.Host.UseSerilog((context, configuration) =>
/// {
///     configuration
///         .ReadFrom.Configuration(context.Configuration)
///         .Filter.With&lt;SensitiveDataLogFilter&gt;();
/// });
/// </code>
/// </remarks>
public class SensitiveDataLogFilter : ILogEventFilter
{
    /// <summary>
    /// 敏感关键词列表
    /// <para>
    /// 包含所有需要过滤的敏感信息关键词，不区分大小写。
    /// </para>
    /// </summary>
    private static readonly string[] SensitiveKeys = 
    {
        "password",
        "token",
        "secret",
        "apikey",
        "api_key",
        "authorization",
        "creditcard",
        "credit_card",
        "ssn",
        "social_security",
        "privatekey",
        "private_key"
    };
    
    /// <summary>
    /// 判断日志事件是否应该被记录
    /// </summary>
    /// <param name="logEvent">日志事件对象</param>
    /// <returns>
    /// true: 允许记录该日志
    /// false: 过滤该日志（包含敏感信息）
    /// </returns>
    /// <remarks>
    /// <para>过滤逻辑：</para>
    /// <list type="number">
    ///   <item><description>遍历日志事件的所有属性</description></item>
    ///   <item><description>检查属性名称是否包含敏感关键词</description></item>
    ///   <item><description>如果匹配，返回 false 过滤该日志</description></item>
    ///   <item><description>如果所有属性都不匹配，返回 true 记录该日志</description></item>
    /// </list>
    /// </remarks>
    public bool IsEnabled(LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties)
        {
            if (ContainsSensitiveKey(property.Key))
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 检查属性名称是否包含敏感关键词
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <returns>true: 包含敏感关键词; false: 不包含</returns>
    private static bool ContainsSensitiveKey(string propertyName)
    {
        return SensitiveKeys.Any(key => 
            propertyName.Contains(key, StringComparison.OrdinalIgnoreCase));
    }
}
