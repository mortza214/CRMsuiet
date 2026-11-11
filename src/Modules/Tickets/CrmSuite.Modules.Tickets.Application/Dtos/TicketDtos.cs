using System.Text.Json.Serialization;

namespace CrmSuite.Modules.Tickets.Application.Dtos;

public sealed record TicketMessageDto(
    Guid Id,
    Guid SenderId,
    string SenderType,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsInternalNote,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record TicketStatusHistoryDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedByUserId,
    DateTime ChangedAt,
    string ChangedAtJalali,
    string? Note);

public sealed record TicketDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string? CustomerName,
    Guid? ServiceId,
    string? ServiceName,
    string Subject,
    Guid DepartmentId,
    string? DepartmentName,
    Guid? AssignedToUserId,
    string? AssignedToName,
    string Priority,
    string Status,
    string Source,
    IReadOnlyList<TicketMessageDto> Messages,
    IReadOnlyList<TicketStatusHistoryDto> StatusHistory,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record TicketListItemDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string? CustomerName,
    Guid DepartmentId,
    string? DepartmentName,
    Guid? AssignedToUserId,
    string? AssignedToName,
    string Subject,
    string Priority,
    string Status,
    string Source,
    int MessageCount,
    DateTime CreatedAt,
    string CreatedAtJalali);
