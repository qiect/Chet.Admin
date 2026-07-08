using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Dictionary;
using Chet.Admin.Data;
using Chet.Admin.Domain.Dictionary;
using Chet.Admin.DTOs.Dictionary;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Dictionary;

/// <summary>
/// 字典服务实现
/// </summary>
public class DictionaryService : IDictionaryService
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DictionaryService> _logger;

    public DictionaryService(
        IDictionaryRepository dictionaryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<DictionaryService> logger)
    {
        _dictionaryRepository = dictionaryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 根据ID获取字典信息
    /// </summary>
    /// <param name="id">字典ID</param>
    /// <returns>字典数据传输对象</returns>
    public async Task<DictionaryDto> GetDictionaryByIdAsync(int id)
    {
        _logger.LogInformation("Getting dictionary by id: {Id}", id);
        var dict = await _dictionaryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DictionaryEntity), id);
        return _mapper.Map<DictionaryDto>(dict);
    }

    /// <summary>
    /// 获取所有字典列表
    /// </summary>
    /// <returns>字典数据传输对象集合</returns>
    public async Task<IEnumerable<DictionaryDto>> GetAllDictionariesAsync()
    {
        _logger.LogInformation("Getting all dictionaries");
        var dictionaries = await _dictionaryRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<DictionaryDto>>(dictionaries);
    }

    /// <summary>
    /// 分页查询字典列表
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页字典列表</returns>
    public async Task<PagedResult<DictionaryDto>> GetPagedDictionariesAsync(PagedRequest request)
    {
        _logger.LogInformation("Getting paged dictionaries: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);
        request.Normalize();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;
            var keyword = request.Keyword.Trim();
            var query = dbContext.Dictionaries.AsNoTracking()
                .Where(d => d.DictType.Contains(keyword) || d.Name.Contains(keyword) || d.Label.Contains(keyword) || d.Value.Contains(keyword));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            var dictDtos = _mapper.Map<List<DictionaryDto>>(items);
            return new PagedResult<DictionaryDto>(dictDtos, request.PageNumber, request.PageSize, totalCount);
        }

        var pagedDicts = await _dictionaryRepository.GetPagedAsync(request);
        var dictDtos2 = _mapper.Map<List<DictionaryDto>>(pagedDicts.Items);
        return new PagedResult<DictionaryDto>(dictDtos2, request.PageNumber, request.PageSize, pagedDicts.Metadata.TotalCount);
    }

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    /// <param name="dictType">字典类型</param>
    /// <returns>字典数据传输对象集合</returns>
    public async Task<IEnumerable<DictionaryDto>> GetByDictTypeAsync(string dictType)
    {
        _logger.LogInformation("Getting dictionaries by type: {DictType}", dictType);
        var dictionaries = await _dictionaryRepository.GetByDictTypeAsync(dictType);
        return _mapper.Map<IEnumerable<DictionaryDto>>(dictionaries);
    }

    /// <summary>
    /// 创建字典
    /// </summary>
    /// <param name="dto">字典创建信息</param>
    /// <returns>创建后的字典数据传输对象</returns>
    public async Task<DictionaryDto> CreateDictionaryAsync(DictionaryCreateDto dto)
    {
        _logger.LogInformation("Creating dictionary: {DictType}", dto.DictType);
        var dict = _mapper.Map<DictionaryEntity>(dto);
        await _dictionaryRepository.AddAsync(dict);
        await _dictionaryRepository.SaveChangesAsync();
        return _mapper.Map<DictionaryDto>(dict);
    }

    /// <summary>
    /// 更新字典信息
    /// </summary>
    /// <param name="id">字典ID</param>
    /// <param name="dto">字典更新信息</param>
    public async Task UpdateDictionaryAsync(int id, DictionaryUpdateDto dto)
    {
        _logger.LogInformation("Updating dictionary: {Id}", id);
        var dict = await _dictionaryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DictionaryEntity), id);
        _mapper.Map(dto, dict);
        _dictionaryRepository.Update(dict);
        await _dictionaryRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 删除字典
    /// </summary>
    /// <param name="id">字典ID</param>
    public async Task DeleteDictionaryAsync(int id)
    {
        _logger.LogInformation("Deleting dictionary: {Id}", id);
        var dict = await _dictionaryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DictionaryEntity), id);
        _dictionaryRepository.Delete(dict);
        await _dictionaryRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 根据字典编码获取启用的字典子项列表
    /// </summary>
    /// <param name="code">字典编码</param>
    /// <returns>字典子项列表</returns>
    public async Task<List<DictionaryItemDto>> GetItemsByCodeAsync(string code)
    {
        _logger.LogInformation("Getting dictionary items by code: {Code}", code);
        var dbContext = (AppDbContext)_unitOfWork.DbContext;

        // Find the parent dictionary by DictType (code)
        var parent = await dbContext.Dictionaries
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DictType == code && d.ParentId == 0);

        if (parent == null) return new List<DictionaryItemDto>();

        // Get all children (items) of this dictionary type
        var items = await dbContext.Dictionaries
            .AsNoTracking()
            .Where(d => d.ParentId == parent.Id && d.IsEnabled)
            .OrderBy(d => d.Sort)
            .Select(d => new DictionaryItemDto { Value = d.Value, Label = d.Label })
            .ToListAsync();

        return items;
    }
}
