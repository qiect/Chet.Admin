namespace Chet.Admin.Shared;

/// <summary>
/// 通用API响应包装器基类
/// </summary>
public abstract class ApiResponseBase
{
    /// <summary>
    /// 响应状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 响应状态，true表示成功，false表示失败
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 响应时间戳（UTC时间）
    /// <para>
    /// 使用 UTC 时间确保：
    /// - 跨时区一致性
    /// - 避免夏令时问题
    /// - 便于日志追踪和问题排查
    /// </para>
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    protected ApiResponseBase()
    {
    }

    protected ApiResponseBase(int statusCode, bool success, string? message = null)
    {
        StatusCode = statusCode;
        Success = success;
        Message = message;
    }
}

/// <summary>
/// 非泛型API响应包装器，用于简单场景
/// </summary>
public class ApiResponse : ApiResponseBase
{
    /// <summary>
    /// 响应数据
    /// </summary>
    public object? Data { get; set; }

    private ApiResponse()
    {
    }

    private ApiResponse(int statusCode, bool success, object? data, string? message = null)
        : base(statusCode, success, message)
    {
        Data = data;
    }

    /// <summary>
    /// 成功响应静态工厂方法
    /// </summary>
    public static ApiResponse Ok(object? data = null, string? message = null, int statusCode = 200)
    {
        return new ApiResponse(statusCode, true, data, message);
    }

    /// <summary>
    /// 错误响应静态工厂方法
    /// </summary>
    public static ApiResponse Error(string? message = null, int statusCode = 500)
    {
        return new ApiResponse(statusCode, false, null, message);
    }

    /// <summary>
    /// 无内容成功响应静态工厂方法
    /// </summary>
    public static ApiResponse NoContent(string? message = null, int statusCode = 204)
    {
        return new ApiResponse(statusCode, true, null, message);
    }
}

/// <summary>
/// 泛型API响应包装器，提供类型安全的数据封装
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T> : ApiResponseBase
{
    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }

    protected ApiResponse()
    {
    }

    protected ApiResponse(int statusCode, bool success, T? data, string? message = null)
        : base(statusCode, success, message)
    {
        Data = data;
    }

    /// <summary>
    /// 成功响应静态工厂方法
    /// </summary>
    public static ApiResponse<T> Ok(T? data = default, string? message = null, int statusCode = 200)
    {
        return new ApiResponse<T>(statusCode, true, data, message);
    }

    /// <summary>
    /// 错误响应静态工厂方法
    /// </summary>
    public static ApiResponse<T> Error(string? message = null, int statusCode = 500)
    {
        return new ApiResponse<T>(statusCode, false, default, message);
    }
}

/// <summary>
/// 分页请求参数
/// </summary>
public class PagedRequest
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSizeLimit = 100;

    /// <summary>
    /// 页码，从1开始，默认为1
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 每页大小，默认为20
    /// </summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// 搜索关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 跳过的记录数
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// 校验并修正分页参数
    /// </summary>
    /// <param name="maxPageSize">最大每页大小限制</param>
    public void Normalize(int maxPageSize = MaxPageSizeLimit)
    {
        if (PageNumber < 1) PageNumber = 1;
        if (PageSize < 1) PageSize = DefaultPageSize;
        if (PageSize > maxPageSize) PageSize = maxPageSize;
    }
}

/// <summary>
/// 分页元数据
/// </summary>
public class PagedMetadata
{
    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedMetadata(int pageNumber, int pageSize, int totalCount)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 当前页数据列表
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// 分页元数据
    /// </summary>
    public PagedMetadata Metadata { get; set; } = null!;

    public PagedResult()
    {
    }

    public PagedResult(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        Metadata = new PagedMetadata(pageNumber, pageSize, totalCount);
    }
}

/// <summary>
/// 分页API响应包装器
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class PaginatedResponse<T> : ApiResponse<PagedResult<T>>
{
    private PaginatedResponse()
    {
    }

    /// <summary>
    /// 成功分页响应静态工厂方法
    /// </summary>
    /// <param name="items">当前页数据</param>
    /// <param name="totalCount">总记录数</param>
    /// <param name="pageNumber">当前页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="message">响应消息</param>
    /// <param name="statusCode">响应状态码，默认200</param>
    /// <returns>PaginatedResponse实例</returns>
    public static PaginatedResponse<T> Ok(List<T>? items, int totalCount, int pageNumber, int pageSize, string? message = null, int statusCode = 200)
    {
        var result = new PagedResult<T>(items ?? [], pageNumber, pageSize, totalCount);
        return new PaginatedResponse<T>
        {
            StatusCode = statusCode,
            Success = true,
            Message = message,
            Data = result
        };
    }
}

/// <summary>
/// 验证错误响应类，用于返回模型验证失败的详细信息
/// </summary>
public class ValidationErrorResponse : ApiResponseBase
{
    /// <summary>
    /// 验证错误字典，键为字段名，值为该字段的错误信息数组
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }
}
