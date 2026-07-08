using AutoMapper;
using Chet.Admin.Contracts;
using Chet.Admin.Contracts.Department;
using Chet.Admin.Data;
using Chet.Admin.Domain.Department;
using Chet.Admin.DTOs.Department;
using Chet.Admin.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chet.Admin.Services.Department;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<DepartmentService> logger)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DepartmentDto> GetDepartmentByIdAsync(int id)
    {
        _logger.LogInformation("Getting department by id: {Id}", id);
        var dept = await _departmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DepartmentEntity), id);
        return _mapper.Map<DepartmentDto>(dept);
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
    {
        _logger.LogInformation("Getting all departments");
        var departments = await _departmentRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
    }

    public async Task<IEnumerable<DepartmentTreeDto>> GetDepartmentTreeAsync()
    {
        _logger.LogInformation("Getting department tree");
        var departments = await _departmentRepository.GetAllAsync();
        var deptDtos = _mapper.Map<List<DepartmentTreeDto>>(departments);
        return BuildDepartmentTree(deptDtos, 0);
    }

    public async Task<PagedResult<DepartmentDto>> GetPagedDepartmentsAsync(PagedRequest request)
    {
        _logger.LogInformation("Getting paged departments: Page {PageNumber}, Size {PageSize}", request.PageNumber, request.PageSize);
        request.Normalize();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var dbContext = (AppDbContext)_unitOfWork.DbContext;
            var keyword = request.Keyword.Trim();
            var query = dbContext.Departments.AsNoTracking()
                .Where(d => d.Code.Contains(keyword) || d.Name.Contains(keyword) || (d.Leader != null && d.Leader.Contains(keyword)));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            var deptDtos = _mapper.Map<List<DepartmentDto>>(items);
            return new PagedResult<DepartmentDto>(deptDtos, request.PageNumber, request.PageSize, totalCount);
        }

        var pagedDepts = await _departmentRepository.GetPagedAsync(request);
        var deptDtos2 = _mapper.Map<List<DepartmentDto>>(pagedDepts.Items);
        return new PagedResult<DepartmentDto>(deptDtos2, request.PageNumber, request.PageSize, pagedDepts.Metadata.TotalCount);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(DepartmentCreateDto dto)
    {
        _logger.LogInformation("Creating department: {Code}", dto.Code);
        var existing = await _departmentRepository.GetByCodeAsync(dto.Code);
        if (existing != null)
            throw new BadRequestException($"Department code '{dto.Code}' already exists");

        var dept = _mapper.Map<DepartmentEntity>(dto);
        await _departmentRepository.AddAsync(dept);
        await _departmentRepository.SaveChangesAsync();
        return _mapper.Map<DepartmentDto>(dept);
    }

    public async Task UpdateDepartmentAsync(int id, DepartmentUpdateDto dto)
    {
        _logger.LogInformation("Updating department: {Id}", id);
        var dept = await _departmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DepartmentEntity), id);
        _mapper.Map(dto, dept);
        _departmentRepository.Update(dept);
        await _departmentRepository.SaveChangesAsync();
    }

    public async Task DeleteDepartmentAsync(int id)
    {
        _logger.LogInformation("Deleting department: {Id}", id);
        var dept = await _departmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(DepartmentEntity), id);
        _departmentRepository.Delete(dept);
        await _departmentRepository.SaveChangesAsync();
    }

    private static IEnumerable<DepartmentTreeDto> BuildDepartmentTree(List<DepartmentTreeDto> allDepts, int parentId)
    {
        return allDepts
            .Where(d => d.ParentId == parentId)
            .OrderBy(d => d.Sort)
            .Select(d =>
            {
                d.Children = BuildDepartmentTree(allDepts, d.Id).ToList();
                return d;
            })
            .ToList();
    }
}
