using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Domain.Entities;

namespace CrmSuite.Modules.Tickets.Application.Mapping;

public sealed class TicketDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public TicketDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public TicketDto ToDto(
        Ticket ticket,
        string? customerName,
        string? serviceName,
        string? departmentName,
        string? assignedToName,
        bool includeInternalNotes)
    {
        var createdAt = AsUtc(ticket.CreatedAt);

        return new TicketDto(
            ticket.Id,
            ticket.TicketNumber,
            ticket.CustomerId,
            customerName,
            ticket.ServiceId,
            serviceName,
            ticket.Subject,
            ticket.DepartmentId,
            departmentName,
            ticket.AssignedToUserId,
            assignedToName,
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.Source.ToString(),
            ticket.Messages
                .Where(message => includeInternalNotes || !message.IsInternalNote)
                .OrderBy(message => message.CreatedAt)
                .Select(message =>
                {
                    var messageCreatedAt = AsUtc(message.CreatedAt);
                    return new TicketMessageDto(
                        message.Id,
                        message.SenderId,
                        message.SenderType.ToString(),
                        message.Message,
                        includeInternalNotes ? message.IsInternalNote : null,
                        messageCreatedAt,
                        _jalaliDate.ToJalali(messageCreatedAt));
                })
                .ToArray(),
            ticket.StatusHistory
                .OrderBy(history => history.ChangedAt)
                .Select(history =>
                {
                    var changedAt = AsUtc(history.ChangedAt);
                    return new TicketStatusHistoryDto(
                        history.Id,
                        history.FromStatus?.ToString(),
                        history.ToStatus.ToString(),
                        history.ChangedByUserId,
                        changedAt,
                        _jalaliDate.ToJalali(changedAt),
                        history.Note);
                })
                .ToArray(),
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
