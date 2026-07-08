using AutoMapper;
using Chet.Admin.Domain.Audit;
using Chet.Admin.DTOs.Audit;

namespace Chet.Admin.Mapping.Audit;

public class AuditLogMappingProfile : Profile
{
    public AuditLogMappingProfile()
    {
        CreateMap<AuditLogEntity, AuditLogDto>().ReverseMap();
    }
}
