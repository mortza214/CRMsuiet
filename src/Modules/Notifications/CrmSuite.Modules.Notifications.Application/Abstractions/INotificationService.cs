using CrmSuite.Modules.Notifications.Domain.Enums;

namespace CrmSuite.Modules.Notifications.Application.Abstractions;

public sealed record NotificationRequest(
    string TemplateCode,
    string Recipient,
    NotificationChannel Channel,
    IReadOnlyDictionary<string, string> Variables,
    Guid? CreatedByUserId = null);

public sealed record NotificationSendResult(
    Guid LogId,
    string Content,
    NotificationStatus Status,
    string? Error);

public interface INotificationService
{
    Task<NotificationSendResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken);
}

public interface INotificationSender
{
    NotificationChannel Channel { get; }

    Task SendAsync(string recipient, string content, CancellationToken cancellationToken);
}
