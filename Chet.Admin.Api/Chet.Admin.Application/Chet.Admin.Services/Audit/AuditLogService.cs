using AutoMapper;
using Chet.Admin.Contracts.Audit;
using Chet.Admin.Data;
using Chet.Admin.Domain.Audit;
using Chet.Admin.DTOs.Audit;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Audit;

/// <summary>
/// 审计日志服务实现
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext dbContext, IMapper mapper, ILogger<AuditLogService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询审计日志列表
    /// </summary>
    /// <param name="request">审计日志分页请求参数（支持按用户、模块、操作、时间范围、关键词过滤）</param>
    /// <returns>分页审计日志列表</returns>
    public async Task<PagedResult<AuditLogDto>> GetPagedAuditLogsAsync(AuditLogPagedRequest request)
    {
        request.Normalize();

        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(x => x.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(x => x.Module == request.Module);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(x => x.Action == request.Action);

        if (request.StartTime.HasValue)
            query = query.Where(x => x.OperatedAt >= request.StartTime.Value);

        if (request.EndTime.HasValue)
            query = query.Where(x => x.OperatedAt <= request.EndTime.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(x => x.UserName.Contains(keyword) || x.Description.Contains(keyword) || x.RequestPath.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.OperatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<AuditLogDto>>(items);
        return new PagedResult<AuditLogDto>(dtos, request.PageNumber, request.PageSize, totalCount);
    }

    /// <summary>
    /// 记录审计日志
    /// </summary>
    /// <param name="auditLog">审计日志信息</param>
    public async Task LogAsync(AuditLogDto auditLog)
    {
        try
        {
            var entity = _mapper.Map<AuditLogEntity>(auditLog);
            _dbContext.AuditLogs.Add(entity);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log");
        }
    }

    /// <summary>
    /// 清理指定时间之前的审计日志
    /// </summary>
    /// <param name="before">截止时间，早于此时间的日志将被删除</param>
    public async Task ClearBeforeAsync(DateTime before)
    {
        var count = await _dbContext.AuditLogs.Where(x => x.OperatedAt < before).CountAsync();
        if (count > 0)
        {
            _dbContext.AuditLogs.RemoveRange(_dbContext.AuditLogs.Where(x => x.OperatedAt < before));
            await _dbContext.SaveChangesAsync();
        }
    }
}
