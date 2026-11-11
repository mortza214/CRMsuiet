using MediatR;

namespace CrmSuite.Modules.Orders.Application.Notifications;

/// <summary>
/// Published once, after the final invoice is paid and the customer service is activated.
/// Commission logic consumes this event; sales commands must not calculate commission.
/// </summary>
public sealed record ServiceActivatedEvent(
    Guid OrderId,
    Guid OrderItemId,
    Guid CustomerId,
    Guid CustomerServiceId,
    Guid SalesExpertId,
    decimal FinalInvoiceAmount,
    decimal? CommissionPercent,
    string ServiceName) : INotification;
