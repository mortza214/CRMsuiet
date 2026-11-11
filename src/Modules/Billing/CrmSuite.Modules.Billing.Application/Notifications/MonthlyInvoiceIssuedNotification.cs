using MediatR;

namespace CrmSuite.Modules.Billing.Application.Notifications;

public sealed record MonthlyInvoiceIssuedNotification(
    Guid InvoiceId,
    Guid CustomerId,
    string InvoiceNumber,
    decimal Amount) : INotification;
