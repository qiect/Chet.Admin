using Chet.Admin.Contracts.Department;
using Chet.Admin.DTOs.Department;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

/// <summary>
/// 部门管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供部门管理相关的API接口")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentsController> _logger;

    /// <summary>
    /// 初始化部门控制器的新实例
    /// </summary>
    /// <param name="departmentService">部门服务接口</param>
    /// <param name="logger">日志记录器</param>
    public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有部门
    /// </summary>
    /// <returns>部门列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDepartments()
    {
        var departments = await _departmentService.GetAllDepartmentsAsync();
        return Ok(ApiResponse.Ok(departments, "Departments retrieved successfully"));
    }

    /// <summary>
    /// 获取部门树形结构
    /// </summary>
    /// <returns>部门树</returns>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentTree()
    {
        var tree = await _departmentService.GetDepartmentTreeAsync();
        return Ok(ApiResponse.Ok(tree, "Department tree retrieved successfully"));
    }

    /// <summary>
    /// 分页获取部门列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页条数</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>分页部门列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedDepartments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _departmentService.GetPagedDepartmentsAsync(request);
        return Ok(PaginatedResponse<DepartmentDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Departments retrieved successfully"));
    }

    /// <summary>
    /// 根据ID获取部门详情
    /// </summary>
    /// <param name="id">部门ID</param>
    /// <returns>部门详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentById(int id)
    {
        var dept = await _departmentService.GetDepartmentByIdAsync(id);
        return Ok(ApiResponse.Ok(dept, "Department retrieved successfully"));
    }

    /// <summary>
    /// 创建新部门
    /// </summary>
    /// <param name="dto">部门创建数据传输对象</param>
    /// <returns>创建的部门信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDepartment(DepartmentCreateDto dto)
    {
        var dept = await _departmentService.CreateDepartmentAsync(dto);
        return CreatedAtAction(nameof(GetDepartmentById), new { id = dept.Id }, ApiResponse.Ok(dept, "Department created successfully", StatusCodes.Status201Created));
    }

    /// <summary>
    /// 更新部门信息
    /// </summary>
    /// <param name="id">部门ID</param>
    /// <param name="dto">部门更新数据传输对象</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDepartment(int id, DepartmentUpdateDto dto)
    {
        await _departmentService.UpdateDepartmentAsync(id, dto);
        return Ok(ApiResponse.NoContent("Department updated successfully"));
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    /// <param name="id">部门ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        await _departmentService.DeleteDepartmentAsync(id);
        return Ok(ApiResponse.NoContent("Department deleted successfully"));
    }
}
