using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Tickets.Domain.Enums;

namespace CrmSuite.Modules.Tickets.Domain.Entities;

public sealed class TicketStatusHistory : BaseEntity
{
    private TicketStatusHistory()
    {
    }

    public TicketStatusHistory(
        Guid ticketId,
        TicketStatus? fromStatus,
        TicketStatus toStatus,
        Guid? changedByUserId,
        DateTime changedAtUtc,
        string? note)
        : base(Guid.NewGuid())
    {
        TicketId = ticketId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        ChangedAt = DateTime.SpecifyKind(changedAtUtc, DateTimeKind.Utc);
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid TicketId { get; private set; }

    public TicketStatus? FromStatus { get; private set; }

    public TicketStatus ToStatus { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public DateTime ChangedAt { get; private set; }

    public string? Note { get; private set; }
}
