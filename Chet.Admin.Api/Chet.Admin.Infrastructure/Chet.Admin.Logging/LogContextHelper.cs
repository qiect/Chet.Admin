using Serilog.Context;

namespace Chet.Admin.Logging;

/// <summary>
/// 日志上下文帮助类
/// <para>
/// 提供便捷的方法来添加日志上下文信息，用于在日志中记录额外的结构化数据。
/// 使用 using 语句确保上下文在作用域结束时自动清理。
/// </para>
/// </summary>
/// <remarks>
/// <para>使用示例：</para>
/// <code>
/// using (LogContextHelper.WithUser("123", "张三"))
/// {
///     _logger.LogInformation("用户执行了操作"); // 日志会自动包含 UserId 和 UserName
/// }
/// </code>
/// 
/// <para>最佳实践：</para>
/// <list type="bullet">
///   <item><description>在中间件中添加请求上下文</description></item>
///   <item><description>在服务中添加用户上下文</description></item>
///   <item><description>在业务逻辑中添加操作上下文</description></item>
/// </list>
/// </remarks>
public static class LogContextHelper
{
    /// <summary>
    /// 添加用户信息到日志上下文
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <returns>IDisposable 对象，用于在 using 语句中自动清理上下文</returns>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item><description>用户登录后记录用户信息</description></item>
    ///   <item><description>用户操作时追踪用户行为</description></item>
    ///   <item><description>审计日志中标识操作用户</description></item>
    /// </list>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// using (LogContextHelper.WithUser(user.Id.ToString(), user.Name))
    /// {
    ///     _logger.LogInformation("用户创建了订单");
    /// }
    /// </code>
    /// </remarks>
    public static IDisposable WithUser(string userId, string userName)
    {
        var properties = new Dictionary<string, object>
        {
            ["UserId"] = userId,
            ["UserName"] = userName
        };
        return WithProperties(properties);
    }
    
    /// <summary>
    /// 添加请求信息到日志上下文
    /// </summary>
    /// <param name="requestId">请求ID（用于追踪整个请求链路）</param>
    /// <param name="method">HTTP方法（GET、POST、PUT、DELETE等）</param>
    /// <param name="path">请求路径</param>
    /// <returns>IDisposable 对象，用于在 using 语句中自动清理上下文</returns>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item><description>在中间件中记录请求信息</description></item>
    ///   <item><description>追踪API调用链路</description></item>
    ///   <item><description>性能监控和问题排查</description></item>
    /// </list>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// using (LogContextHelper.WithRequest(
    ///     HttpContext.TraceIdentifier,
    ///     HttpContext.Request.Method,
    ///     HttpContext.Request.Path))
    /// {
    ///     await _next(context);
    /// }
    /// </code>
    /// </remarks>
    public static IDisposable WithRequest(string requestId, string method, string path)
    {
        var properties = new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["Method"] = method,
            ["Path"] = path
        };
        return WithProperties(properties);
    }
    
    /// <summary>
    /// 添加自定义属性到日志上下文
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <param name="value">属性值</param>
    /// <returns>IDisposable 对象，用于在 using 语句中自动清理上下文</returns>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item><description>添加业务相关的上下文信息</description></item>
    ///   <item><description>记录订单ID、产品ID等业务标识</description></item>
    ///   <item><description>添加自定义追踪信息</description></item>
    /// </list>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// using (LogContextHelper.WithProperty("OrderId", orderId))
    /// {
    ///     _logger.LogInformation("订单处理完成");
    /// }
    /// </code>
    /// </remarks>
    public static IDisposable WithProperty(string propertyName, object value)
    {
        return LogContext.PushProperty(propertyName, value);
    }
    
    /// <summary>
    /// 添加多个自定义属性到日志上下文
    /// </summary>
    /// <param name="properties">属性字典，键为属性名，值为属性值</param>
    /// <returns>IDisposable 对象，用于在 using 语句中自动清理上下文</returns>
    /// <remarks>
    /// <para>使用场景：</para>
    /// <list type="bullet">
    ///   <item><description>批量添加多个上下文属性</description></item>
    ///   <item><description>从字典或配置中加载上下文信息</description></item>
    /// </list>
    /// 
    /// <para>示例：</para>
    /// <code>
    /// var properties = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["OrderId"] = orderId,
    ///     ["CustomerId"] = customerId,
    ///     ["Amount"] = amount
    /// };
    /// using (LogContextHelper.WithProperties(properties))
    /// {
    ///     _logger.LogInformation("订单创建成功");
    /// }
    /// </code>
    /// </remarks>
    public static IDisposable WithProperties(Dictionary<string, object> properties)
    {
        var stack = new Stack<IDisposable>();
        foreach (var property in properties)
        {
            stack.Push(LogContext.PushProperty(property.Key, property.Value));
        }
        return new DisposableStack(stack);
    }
    
    /// <summary>
    /// 内部类：用于管理多个 IDisposable 对象的生命周期
    /// </summary>
    private class DisposableStack : IDisposable
    {
        private readonly Stack<IDisposable> _disposables;
        private bool _disposed;
        
        public DisposableStack(Stack<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        /// <summary>
        /// 释放所有托管的 IDisposable 对象
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                while (_disposables.Count > 0)
                {
                    _disposables.Pop()?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
