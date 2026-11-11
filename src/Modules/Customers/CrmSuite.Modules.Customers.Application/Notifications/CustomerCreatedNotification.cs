using MediatR;

namespace CrmSuite.Modules.Customers.Application.Notifications;

public sealed record CustomerCreatedNotification(Guid CustomerId) : INotification;
