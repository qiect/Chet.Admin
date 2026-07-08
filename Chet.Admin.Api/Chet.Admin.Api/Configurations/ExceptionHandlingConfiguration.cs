using Chet.Admin.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.Net;

namespace Chet.Admin.Api.Configurations;

/// <summary>
/// 异常处理配置类
/// </summary>
public static class ExceptionHandlingConfiguration
{
    /// <summary>
    /// 配置异常处理中间件
    /// </summary>
    /// <param name="app">WebApplication实例</param>
    public static void ConfigureExceptionHandling(this WebApplication app)
    {
        // 添加自定义异常处理中间件
        app.UseExceptionHandler(options =>
        {
            options.Run(async context =>
            {
                // 获取异常信息
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception == null)
                {
                    return;
                }

                // 解包内部异常（ASP.NET 有时会将实际异常包装在内部异常中）
                var actualException = exception is System.Reflection.TargetInvocationException tie
                    ? tie.InnerException ?? exception
                    : exception;

                // 记录异常日志
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(actualException, "An unhandled exception occurred");

                // 设置默认错误状态码和消息
                var statusCode = HttpStatusCode.InternalServerError;
                var message = "An unexpected error occurred";

                // 根据异常类型设置不同的状态码和消息（使用 is 模式匹配替代 GetType().Name 字符串比较）
                if (actualException is NotFoundException)
                {
                    statusCode = HttpStatusCode.NotFound;
                    message = actualException.Message;
                }
                else if (actualException is BadRequestException)
                {
                    statusCode = HttpStatusCode.BadRequest;
                    message = actualException.Message;
                }
                else if (actualException is UnauthorizedAccessException)
                {
                    statusCode = HttpStatusCode.Unauthorized;
                    message = actualException.Message;
                }
                else if (actualException is SecurityTokenException)
                {
                    statusCode = HttpStatusCode.Unauthorized;
                    message = actualException.Message;
                }

                // 构造统一格式的错误响应
                context.Response.StatusCode = (int)statusCode;
                context.Response.ContentType = "application/json";

                var errorResponse = ApiResponse.Error(message, (int)statusCode);

                // 返回错误响应
                await context.Response.WriteAsJsonAsync(errorResponse);
            });
        });
    }
}
