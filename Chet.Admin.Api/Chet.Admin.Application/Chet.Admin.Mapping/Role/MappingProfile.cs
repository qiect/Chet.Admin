using AutoMapper;
using Chet.Admin.Domain.Role;
using Chet.Admin.Domain.Menu;
using Chet.Admin.Domain.Department;
using Chet.Admin.Domain.Dictionary;
using Chet.Admin.DTOs.Role;
using Chet.Admin.DTOs.Menu;
using Chet.Admin.DTOs.Department;
using Chet.Admin.DTOs.Dictionary;

namespace Chet.Admin.Mapping.Role;

public class RbacMappingProfile : Profile
{
    public RbacMappingProfile()
    {
        // Role mappings
        CreateMap<RoleEntity, RoleDto>();
        CreateMap<RoleCreateDto, RoleEntity>();
        CreateMap<RoleUpdateDto, RoleEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Menu mappings
        CreateMap<MenuEntity, MenuDto>();
        CreateMap<MenuEntity, MenuTreeDto>();
        CreateMap<MenuCreateDto, MenuEntity>();
        CreateMap<MenuUpdateDto, MenuEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Department mappings
        CreateMap<DepartmentEntity, DepartmentDto>();
        CreateMap<DepartmentEntity, DepartmentTreeDto>();
        CreateMap<DepartmentCreateDto, DepartmentEntity>();
        CreateMap<DepartmentUpdateDto, DepartmentEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Dictionary mappings
        CreateMap<DictionaryEntity, DictionaryDto>();
        CreateMap<DictionaryCreateDto, DictionaryEntity>();
        CreateMap<DictionaryUpdateDto, DictionaryEntity>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
