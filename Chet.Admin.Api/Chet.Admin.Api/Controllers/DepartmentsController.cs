using Chet.Admin.Contracts.Department;
using Chet.Admin.DTOs.Department;
using Chet.Admin.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Chet.Admin.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[SwaggerTag("提供部门管理相关的API接口")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDepartments()
    {
        var departments = await _departmentService.GetAllDepartmentsAsync();
        return Ok(ApiResponse.Ok(departments, "Departments retrieved successfully"));
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentTree()
    {
        var tree = await _departmentService.GetDepartmentTreeAsync();
        return Ok(ApiResponse.Ok(tree, "Department tree retrieved successfully"));
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PaginatedResponse<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedDepartments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? keyword = null)
    {
        var request = new PagedRequest { PageNumber = pageNumber, PageSize = pageSize, Keyword = keyword };
        var result = await _departmentService.GetPagedDepartmentsAsync(request);
        return Ok(PaginatedResponse<DepartmentDto>.Ok(result.Items, result.Metadata.TotalCount, result.Metadata.PageNumber, result.Metadata.PageSize, "Departments retrieved successfully"));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentById(int id)
    {
        var dept = await _departmentService.GetDepartmentByIdAsync(id);
        return Ok(ApiResponse.Ok(dept, "Department retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDepartment(DepartmentCreateDto dto)
    {
        var dept = await _departmentService.CreateDepartmentAsync(dto);
        return CreatedAtAction(nameof(GetDepartmentById), new { id = dept.Id }, ApiResponse.Ok(dept, "Department created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDepartment(int id, DepartmentUpdateDto dto)
    {
        await _departmentService.UpdateDepartmentAsync(id, dto);
        return Ok(ApiResponse.NoContent("Department updated successfully"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        await _departmentService.DeleteDepartmentAsync(id);
        return Ok(ApiResponse.NoContent("Department deleted successfully"));
    }
}
