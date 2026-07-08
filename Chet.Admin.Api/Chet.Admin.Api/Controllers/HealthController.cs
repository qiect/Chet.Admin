using Microsoft.AspNetCore.Mvc;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
/// <remarks>
/// 提供应用健康状态检查端点，用于监控和运维场景。
/// 支持Kubernetes/Docker的存活探针（Liveness）和就绪探针（Readiness）。
/// 
/// 端点说明：
/// - GET /api/v1/health - 存活探针，检查应用进程是否运行
/// - GET /api/v1/health/ready - 就绪探针，检查应用是否准备好接收流量
/// 
/// 使用场景：
/// 1. Kubernetes/Docker容器健康检查
/// 2. 负载均衡器后端服务探测
/// 3. 监控系统（Prometheus、Zabbix等）
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 日志记录器实例
    /// </summary>
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// 初始化健康检查控制器的新实例
    /// </summary>
    /// <param name="logger">日志记录器，用于记录健康检查过程中的信息</param>
    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 存活探针（Liveness Probe）
    /// </summary>
    /// <remarks>
    /// 检查应用进程是否正常运行。此端点仅验证应用是否在运行，不检查外部依赖（数据库、Redis等）的状态。
    /// 
    /// 典型用途：
    /// - Docker HEALTHCHECK指令
    /// - Kubernetes livenessProbe配置
    /// - 负载均衡器心跳检测
    /// 
    /// 响应示例：
    /// 
    ///     {
    ///         "status": "Healthy",
    ///         "timestamp": "2026-04-29T10:30:00Z",
    ///         "version": "v1"
    ///     }
    /// </remarks>
    /// <returns>始终返回200 OK，包含应用状态、时间戳和版本信息</returns>
    /// <response code="200">应用运行正常</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Liveness()
    {
        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "v1"
        });
    }

    /// <summary>
    /// 就绪探针（Readiness Probe）
    /// </summary>
    /// <remarks>
    /// 检查应用是否已完全启动并准备好接收请求。此端点会验证所有关键依赖项的连接状态。
    /// 只有当所有依赖都正常时，才返回健康状态。
    /// 
    /// 检查项目：
    /// - Database: 验证数据库连接是否可用
    /// - Redis: 验证Redis连接是否可用（如已启用）
    /// 
    /// 响应示例（健康）：
    /// 
    ///     {
    ///         "status": "Healthy",
    ///         "timestamp": "2026-04-29T10:30:00Z",
    ///         "checks": { "database": true, "redis": true }
    ///     }
    /// 
    /// 响应示例（不健康）：
    /// 
    ///     {
    ///         "status": "Unhealthy",
    ///         "timestamp": "2026-04-29T10:30:00Z",
    ///         "checks": { "database": false, "redis": true }
    ///     }
    /// </remarks>
    /// <param name="serviceProvider">依赖注入服务提供者，用于获取数据库上下文和缓存服务的实例</param>
    /// <returns>200 所有依赖正常，应用就绪 / 503 存在依赖故障，应用未就绪</returns>
    /// <response code="200">所有依赖正常，应用就绪</response>
    /// <response code="503">存在依赖故障，应用未就绪</response>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(HealthReadyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthReadyResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Readiness([FromServices] IServiceProvider serviceProvider)
    {
        var checks = new Dictionary<string, bool>();

        try
        {
            await CheckDatabaseAsync(serviceProvider, checks);
            await CheckRedisAsync(serviceProvider, checks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
        }

        var isHealthy = checks.Values.All(v => v);
        var response = new HealthReadyResponse
        {
            Status = isHealthy ? "Healthy" : "Unhealthy",
            Timestamp = DateTime.UtcNow,
            Checks = checks
        };

        return isHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    /// <summary>
    /// 检查数据库连接是否可用
    /// </summary>
    /// <remarks>
    /// 通过调用 DbContext.Database.CanConnectAsync() 来验证数据库连接。
    /// 如果无法连接或超时，会将 database 标记为不可用。
    /// </remarks>
    /// <param name="serviceProvider">依赖注入服务提供者</param>
    /// <param name="checks">检查结果字典，用于存储各组件的健康状态</param>
    /// <returns>表示异步操作的任务</returns>
    private static async Task CheckDatabaseAsync(IServiceProvider serviceProvider, Dictionary<string, bool> checks)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<Chet.Admin.Data.AppDbContext>();
            if (context != null)
            {
                await context.Database.CanConnectAsync();
                checks["database"] = true;
            }
            else
            {
                checks["database"] = false;
            }
        }
        catch
        {
            checks["database"] = false;
        }
    }

    /// <summary>
    /// 检查Redis缓存连接是否可用
    /// </summary>
    /// <remarks>
    /// 通过调用 ICacheService.PingAsync() 来验证Redis连接。
    /// 如果使用的是 NoOpCacheService（未启用Redis），则始终返回健康状态。
    /// </remarks>
    /// <param name="serviceProvider">依赖注入服务提供者</param>
    /// <param name="checks">检查结果字典，用于存储各组件的健康状态</param>
    /// <returns>表示异步操作的任务</returns>
    private static async Task CheckRedisAsync(IServiceProvider serviceProvider, Dictionary<string, bool> checks)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<Chet.Admin.Contracts.Cache.ICacheService>();
            if (cacheService != null)
            {
                await cacheService.PingAsync();
                checks["redis"] = true;
            }
            else
            {
                checks["redis"] = true;
            }
        }
        catch
        {
            checks["redis"] = false;
        }
    }
}

/// <summary>
/// 存活探针响应模型
/// </summary>
/// <remarks>
/// 用于表示应用的基本运行状态信息，包含状态、时间戳和API版本号。
/// </remarks>
public class HealthResponse
{
    /// <summary>
    /// 健康状态，可能的值：Healthy（健康）、Unhealthy（不健康）
    /// </summary>
    public string Status { get; set; } = "Healthy";

    /// <summary>
    /// 检查时间戳（UTC时间）
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// API版本号，例如：v1、v2
    /// </summary>
    public string Version { get; set; } = "v1";
}

/// <summary>
/// 就绪探针响应模型
/// </summary>
/// <remarks>
/// 用于表示应用的完整就绪状态信息，包含状态、时间戳以及各个依赖组件的详细检查结果。
/// </remarks>
public class HealthReadyResponse
{
    /// <summary>
    /// 整体健康状态，只有当所有Checks中的值都为true时，状态才为Healthy
    /// </summary>
    public string Status { get; set; } = "Healthy";

    /// <summary>
    /// 检查时间戳（UTC时间）
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 各依赖组件的检查结果，键为组件名称（如 database、redis），值为该组件是否可用
    /// </summary>
    public Dictionary<string, bool> Checks { get; set; } = new();
}
