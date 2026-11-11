using MediatR;

namespace CrmSuite.Modules.Orders.Application.Notifications;

public sealed record ServiceActivatedNotification(
    Guid OrderId,
    Guid OrderItemId,
    Guid CustomerId,
    Guid SalesExpertId,
    decimal CommissionBase,
    decimal? CommissionPercent,
    string ServiceName) : INotification;
