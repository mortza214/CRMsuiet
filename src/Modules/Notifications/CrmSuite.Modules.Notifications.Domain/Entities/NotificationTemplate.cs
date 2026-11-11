using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Notifications.Domain.Entities;

public sealed class NotificationTemplate : BaseEntity
{
    private NotificationTemplate()
    {
    }

    public NotificationTemplate(
        string code,
        string title,
        string body,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد قالب اعلان الزامی است.", "NOTIFICATIONS_CODE_REQUIRED");
        }

        Code = code.Trim();
        Title = title.Trim();
        Body = body;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;
}
