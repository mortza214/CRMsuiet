using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Notifications.Domain.Enums;

namespace CrmSuite.Modules.Notifications.Domain.Entities;

public sealed class NotificationLog : BaseEntity
{
    private NotificationLog()
    {
    }

    public NotificationLog(
        string recipient,
        NotificationChannel channel,
        string templateCode,
        string content,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        Recipient = recipient;
        Channel = channel;
        TemplateCode = templateCode;
        Content = content;
        Status = NotificationStatus.Pending;
        IsRead = false;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Recipient { get; private set; } = string.Empty;

    public NotificationChannel Channel { get; private set; }

    public string TemplateCode { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public NotificationStatus Status { get; private set; }

    public string? ErrorMessage { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime? SentAt { get; private set; }

    public void MarkSent(DateTime utcNow)
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        ErrorMessage = null;
    }

    public void MarkFailed(string? error, DateTime utcNow)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = error;
        MarkUpdated(null, utcNow);
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}
