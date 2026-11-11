using MediatR;

namespace CrmSuite.Modules.Billing.Application.Notifications;

public sealed record InvoicePaidNotification(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    decimal Amount) : INotification;
