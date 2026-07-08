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

    public async Task<DictionaryDto> GetDictionaryByIdAsync(int id)
    {
        _logger.LogInformation("Getting dictionary by id: {Id}", id);
        var dict = await _dictionaryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DictionaryEntity), id);
        return _mapper.Map<DictionaryDto>(dict);
    }

    public async Task<IEnumerable<DictionaryDto>> GetAllDictionariesAsync()
    {
        _logger.LogInformation("Getting all dictionaries");
        var dictionaries = await _dictionaryRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<DictionaryDto>>(dictionaries);
    }

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

    public async Task<IEnumerable<DictionaryDto>> GetByDictTypeAsync(string dictType)
    {
        _logger.LogInformation("Getting dictionaries by type: {DictType}", dictType);
        var dictionaries = await _dictionaryRepository.GetByDictTypeAsync(dictType);
        return _mapper.Map<IEnumerable<DictionaryDto>>(dictionaries);
    }

    public async Task<DictionaryDto> CreateDictionaryAsync(DictionaryCreateDto dto)
    {
        _logger.LogInformation("Creating dictionary: {DictType}", dto.DictType);
        var dict = _mapper.Map<DictionaryEntity>(dto);
        await _dictionaryRepository.AddAsync(dict);
        await _dictionaryRepository.SaveChangesAsync();
        return _mapper.Map<DictionaryDto>(dict);
    }

    public async Task UpdateDictionaryAsync(int id, DictionaryUpdateDto dto)
    {
        _logger.LogInformation("Updating dictionary: {Id}", id);
        var dict = await _dictionaryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DictionaryEntity), id);
        _mapper.Map(dto, dict);
        _dictionaryRepository.Update(dict);
        await _dictionaryRepository.SaveChangesAsync();
    }

    public async Task DeleteDictionaryAsync(int id)
    {
        _logger.LogInformation("Deleting dictionary: {Id}", id);
        var dict = await _dictionaryRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DictionaryEntity), id);
        _dictionaryRepository.Delete(dict);
        await _dictionaryRepository.SaveChangesAsync();
    }

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
