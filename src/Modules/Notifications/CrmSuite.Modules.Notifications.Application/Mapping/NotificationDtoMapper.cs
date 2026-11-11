using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Dtos;
using CrmSuite.Modules.Notifications.Domain.Entities;

namespace CrmSuite.Modules.Notifications.Application.Mapping;

public sealed class NotificationDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public NotificationDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public NotificationDto ToDto(NotificationLog log)
    {
        var createdAt = AsUtc(log.CreatedAt);
        return new NotificationDto(
            log.Id,
            log.TemplateCode,
            log.Content,
            log.IsRead,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
