using CrmSuite.Modules.Orders.Application.Notifications;
using MediatR;

namespace CrmSuite.Modules.Wallet.Application.EventHandlers;

/// <summary>
/// Commission is intentionally calculated only after service activation, never on order creation.
/// Kept as a no-op so existing event registrations remain stable.
/// </summary>
public sealed class OrderCreatedCommissionHandler : INotificationHandler<OrderCreatedNotification>
{
    public Task Handle(
        OrderCreatedNotification notification,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
