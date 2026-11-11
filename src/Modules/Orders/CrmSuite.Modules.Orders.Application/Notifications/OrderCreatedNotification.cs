using MediatR;

namespace CrmSuite.Modules.Orders.Application.Notifications;

public sealed record OrderCreatedNotification(Guid OrderId) : INotification;
