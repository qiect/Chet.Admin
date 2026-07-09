using Chet.Admin.Contracts.Dictionary;
using Chet.Admin.DTOs.Dictionary;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 字典管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供字典管理相关的API接口")]
public class DictionariesController : ControllerBase
{
    private readonly IDictionaryService _dictionaryService;
    private readonly ILogger<DictionariesController> _logger;

    /// <summary>
    /// 初始化字典控制器的新实例
    /// </summary>
    /// <param name="dictionaryService">字典服务接口</param>
    /// <param name="logger">日志记录器</param>
    public DictionariesController(IDictionaryService dictionaryService, ILogger<DictionariesController> logger)
    {
        _dictionaryService = dictionaryService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有字典
    /// </summary>
    /// <returns>字典列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDictionaries()
    {
        var dictionaries = await _dictionaryService.GetAllDictionariesAsync();
        return Ok(ApiResponse.Ok(dictionaries, "Dictionaries retrieved successfully"));
    }

    /// <summary>
    /// 分页获取字典列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="dictType">字典类型（精确匹配）</param>
    /// <returns>分页字典列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<DictionaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedDictionaries([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null, [FromQuery] string? dictType = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword, DictType = dictType };
        var result = await _dictionaryService.GetPagedDictionariesAsync(request);
        return Ok(PaginatedResponse<DictionaryDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Dictionaries retrieved successfully"));
    }

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    /// <param name="dictType">字典类型</param>
    /// <returns>字典列表</returns>
    [HttpGet("type/{dictType}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDictType(string dictType)
    {
        var dictionaries = await _dictionaryService.GetByDictTypeAsync(dictType);
        return Ok(ApiResponse.Ok(dictionaries, "Dictionaries retrieved successfully"));
    }

    /// <summary>
    /// 根据ID获取字典详情
    /// </summary>
    /// <param name="id">字典ID</param>
    /// <returns>字典详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDictionaryById(int id)
    {
        var dict = await _dictionaryService.GetDictionaryByIdAsync(id);
        return Ok(ApiResponse.Ok(dict, "Dictionary retrieved successfully"));
    }

    /// <summary>
    /// 创建新字典
    /// </summary>
    /// <param name="dto">字典创建数据传输对象</param>
    /// <returns>创建的字典信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDictionary(DictionaryCreateDto dto)
    {
        var dict = await _dictionaryService.CreateDictionaryAsync(dto);
        return CreatedAtAction(nameof(GetDictionaryById), new { id = dict.Id }, ApiResponse.Ok(dict, "Dictionary created successfully", StatusCodes.Status201Created));
    }

    /// <summary>
    /// 更新字典信息
    /// </summary>
    /// <param name="id">字典ID</param>
    /// <param name="dto">字典更新数据传输对象</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDictionary(int id, DictionaryUpdateDto dto)
    {
        await _dictionaryService.UpdateDictionaryAsync(id, dto);
        return Ok(ApiResponse.NoContent("Dictionary updated successfully"));
    }

    /// <summary>
    /// 删除字典
    /// </summary>
    /// <param name="id">字典ID</param>
    /// <returns>删除结果</returns>
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
    /// <param name="code">字典编码</param>
    /// <returns>字典项列表</returns>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDictionaryByCode(string code)
    {
        var items = await _dictionaryService.GetItemsByCodeAsync(code);
        return Ok(ApiResponse.Ok(items, "Dictionary items retrieved successfully"));
    }
}
