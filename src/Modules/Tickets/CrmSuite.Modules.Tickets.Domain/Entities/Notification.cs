using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Tickets.Domain.Entities;

public sealed class Notification : BaseEntity
{
    private Notification()
    {
    }

    public Notification(
        Guid? departmentId,
        Guid? userId,
        string title,
        string body,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        DepartmentId = departmentId;
        UserId = userId;
        Title = title;
        Body = body;
        IsRead = false;
        MarkCreated(null, createdAtUtc);
    }

    public Guid? DepartmentId { get; private set; }

    public Guid? UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public bool IsRead { get; private set; }

    public void MarkRead()
    {
        IsRead = true;
    }
}
