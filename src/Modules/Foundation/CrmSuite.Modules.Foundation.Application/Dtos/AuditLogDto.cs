namespace CrmSuite.Modules.Foundation.Application.Dtos;

public sealed record AuditLogDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    Guid? UserId,
    string? UserIp,
    DateTime Timestamp,
    string TimestampJalali);
