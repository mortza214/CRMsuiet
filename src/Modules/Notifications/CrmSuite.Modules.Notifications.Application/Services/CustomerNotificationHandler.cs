using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Notifications.Application.Abstractions;

namespace CrmSuite.Modules.Notifications.Application.Services;

/// <summary>
/// Central customer channel resolver: SMS or Bale is selected from the customer profile.
/// </summary>
public sealed class CustomerNotificationHandler
{
    private readonly ICustomerRepository _customers;
    private readonly INotificationService _notifications;

    public CustomerNotificationHandler(
        ICustomerRepository customers,
        INotificationService notifications)
    {
        _customers = customers;
        _notifications = notifications;
    }

    public async Task<NotificationSendResult> SendAsync(
        Guid customerId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(customerId, false, cancellationToken);
        if (customer is null)
        {
            return new NotificationSendResult(
                Guid.Empty,
                message,
                CrmSuite.Modules.Notifications.Domain.Enums.NotificationStatus.Failed,
                "مشتری یافت نشد.");
        }

        return customer.PreferredNotificationChannel == CustomerNotificationChannel.Bale
            ? await _notifications.SendBale(customer.Mobile, message, cancellationToken)
            : await _notifications.SendSms(customer.Mobile, message, cancellationToken);
    }
}
