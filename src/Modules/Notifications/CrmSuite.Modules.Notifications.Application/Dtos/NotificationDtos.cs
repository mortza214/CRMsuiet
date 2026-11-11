namespace CrmSuite.Modules.Notifications.Application.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string TemplateCode,
    string Content,
    bool IsRead,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record SendNotificationResultDto(
    Guid LogId,
    string Content,
    string Status,
    string? Error);
