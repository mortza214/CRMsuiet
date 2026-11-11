using System.Globalization;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Enums;
using CrmSuite.Modules.Tickets.Application.Notifications;
using MediatR;

namespace CrmSuite.Modules.Notifications.Application.EventHandlers;

public sealed class TicketCreatedNotificationHandler
    : INotificationHandler<TicketCreatedNotification>
{
    private readonly ICustomerRepository _customers;
    private readonly INotificationService _notifications;

    public TicketCreatedNotificationHandler(
        ICustomerRepository customers,
        INotificationService notifications)
    {
        _customers = customers;
        _notifications = notifications;
    }

    public async Task Handle(
        TicketCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(
            notification.CustomerId,
            false,
            cancellationToken);

        if (customer is null)
        {
            return;
        }

        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["TicketNumber"] = notification.TicketNumber,
            ["CustomerName"] = customer.DisplayName,
            ["Subject"] = notification.Subject,
        };

        if (!string.IsNullOrWhiteSpace(customer.Mobile))
        {
            await _notifications.SendAsync(
                new NotificationRequest(
                    "TicketCreated",
                    customer.Mobile,
                    NotificationChannel.Sms,
                    variables),
                cancellationToken);
        }

        if (customer.UserId.HasValue)
        {
            await _notifications.SendAsync(
                new NotificationRequest(
                    "TicketCreated",
                    customer.UserId.Value.ToString(),
                    NotificationChannel.InApp,
                    variables),
                cancellationToken);
        }
    }
}
