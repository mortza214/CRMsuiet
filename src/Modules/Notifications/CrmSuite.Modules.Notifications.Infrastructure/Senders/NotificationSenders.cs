using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Enums;

namespace CrmSuite.Modules.Notifications.Infrastructure.Senders;

public sealed class InAppNotificationSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task SendAsync(string recipient, string content, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class SmsNotificationSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task SendAsync(string recipient, string content, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class BaleNotificationSender : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.Bale;

    public Task SendAsync(string recipient, string content, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
