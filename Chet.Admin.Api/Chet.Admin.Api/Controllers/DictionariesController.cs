using Chet.Admin.Contracts.Dictionary;
using Chet.Admin.DTOs.Dictionary;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供字典管理相关的API接口")]
public class DictionariesController : ControllerBase
{
    private readonly IDictionaryService _dictionaryService;
    private readonly ILogger<DictionariesController> _logger;

    public DictionariesController(IDictionaryService dictionaryService, ILogger<DictionariesController> logger)
    {
        _dictionaryService = dictionaryService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDictionaries()
    {
        var dictionaries = await _dictionaryService.GetAllDictionariesAsync();
        return Ok(ApiResponse.Ok(dictionaries, "Dictionaries retrieved successfully"));
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<DictionaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedDictionaries([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _dictionaryService.GetPagedDictionariesAsync(request);
        return Ok(PaginatedResponse<DictionaryDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Dictionaries retrieved successfully"));
    }

    [HttpGet("type/{dictType}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDictType(string dictType)
    {
        var dictionaries = await _dictionaryService.GetByDictTypeAsync(dictType);
        return Ok(ApiResponse.Ok(dictionaries, "Dictionaries retrieved successfully"));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDictionaryById(int id)
    {
        var dict = await _dictionaryService.GetDictionaryByIdAsync(id);
        return Ok(ApiResponse.Ok(dict, "Dictionary retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDictionary(DictionaryCreateDto dto)
    {
        var dict = await _dictionaryService.CreateDictionaryAsync(dto);
        return CreatedAtAction(nameof(GetDictionaryById), new { id = dict.Id }, ApiResponse.Ok(dict, "Dictionary created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDictionary(int id, DictionaryUpdateDto dto)
    {
        await _dictionaryService.UpdateDictionaryAsync(id, dto);
        return Ok(ApiResponse.NoContent("Dictionary updated successfully"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDictionary(int id)
    {
        await _dictionaryService.DeleteDictionaryAsync(id);
        return Ok(ApiResponse.NoContent("Dictionary deleted successfully"));
    }

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDictionaryByCode(string code)
    {
        var items = await _dictionaryService.GetItemsByCodeAsync(code);
        return Ok(ApiResponse.Ok(items, "Dictionary items retrieved successfully"));
    }
}
