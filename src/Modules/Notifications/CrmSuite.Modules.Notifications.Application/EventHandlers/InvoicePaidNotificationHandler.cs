using System.Globalization;
using CrmSuite.Modules.Billing.Application.Notifications;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Notifications.Application.EventHandlers;

public sealed class InvoicePaidNotificationHandler
    : INotificationHandler<InvoicePaidNotification>
{
    private readonly ICustomerRepository _customers;
    private readonly INotificationService _notifications;

    public InvoicePaidNotificationHandler(
        ICustomerRepository customers,
        INotificationService notifications)
    {
        _customers = customers;
        _notifications = notifications;
    }

    public async Task Handle(
        InvoicePaidNotification notification,
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
            ["InvoiceNumber"] = notification.InvoiceNumber,
            ["CustomerName"] = customer.DisplayName,
            ["Amount"] = notification.Amount.ToString("N0", CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(customer.Mobile))
        {
            await _notifications.SendAsync(
                new NotificationRequest(
                    "InvoicePaid",
                    customer.Mobile,
                    NotificationChannel.Sms,
                    variables),
                cancellationToken);
        }

        if (customer.UserId.HasValue)
        {
            await _notifications.SendAsync(
                new NotificationRequest(
                    "InvoicePaid",
                    customer.UserId.Value.ToString(),
                    NotificationChannel.InApp,
                    variables),
                cancellationToken);
        }
    }
}
