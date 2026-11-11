using MediatR;

namespace CrmSuite.Modules.Tickets.Application.Notifications;

public sealed record TicketCreatedNotification(
    Guid TicketId,
    string TicketNumber,
    Guid CustomerId,
    Guid DepartmentId,
    string Subject) : INotification;
