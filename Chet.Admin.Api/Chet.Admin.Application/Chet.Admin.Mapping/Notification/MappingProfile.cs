using AutoMapper;
using Chet.Admin.Domain.Notification;
using Chet.Admin.DTOs.Notification;

namespace Chet.Admin.Mapping.Notification;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<NotificationEntity, NotificationDto>();
        CreateMap<CreateNotificationDto, NotificationEntity>();
    }
}
