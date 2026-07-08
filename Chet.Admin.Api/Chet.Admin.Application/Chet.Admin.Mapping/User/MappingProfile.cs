using AutoMapper;
using Chet.Admin.Domain.Role;
using Chet.Admin.Domain.User;
using Chet.Admin.DTOs.User;

namespace Chet.Admin.Mapping.User;

/// <summary>
/// AutoMapper配置类，用于定义实体和DTO之间的映射关系
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// 初始化映射配置，定义所有实体和DTO之间的映射规则
    /// </summary>
    public MappingProfile()
    {
        // 用户实体到用户DTO的映射
        CreateMap<UserEntity, UserDto>()
            .ForMember(d => d.DepartmentName, opt => opt.Ignore()) // 由前端通过 deptNameMap 渲染
            .ForMember(d => d.Roles, opt => opt.MapFrom(s =>
                s.UserRoles.Select(ur => new UserRoleInfoDto
                {
                    Id = ur.RoleId,
                    Name = ur.Role != null ? ur.Role.Name : string.Empty
                }).ToList()));

        // 用户创建DTO到用户实体的映射
        CreateMap<UserCreateDto, UserEntity>();

        // 用户更新DTO到用户实体的映射
        CreateMap<UserUpdateDto, UserEntity>();

        // 注册DTO到用户实体的映射
        CreateMap<RegisterDto, UserEntity>();
    }
}
