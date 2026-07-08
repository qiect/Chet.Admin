using Chet.Admin.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Chet.Admin.Api.Filters;

/// <summary>
/// 全局API异常过滤器，在MVC管道内捕获业务异常并返回正确的HTTP状态码
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;

        var result = exception switch
        {
            BadRequestException => new ObjectResult(ApiResponse.Error(exception.Message, 400))
            {
                StatusCode = 400,
            },
            NotFoundException => new ObjectResult(ApiResponse.Error(exception.Message, 404))
            {
                StatusCode = 404,
            },
            UnauthorizedAccessException => new ObjectResult(ApiResponse.Error(exception.Message, 401))
            {
                StatusCode = 401,
            },
            _ => null,
        };

        if (result != null)
        {
            context.Result = result;
            context.ExceptionHandled = true;
        }
    }
}
