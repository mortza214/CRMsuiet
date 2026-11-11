using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Tickets.Domain.Enums;

namespace CrmSuite.Modules.Tickets.Domain.Entities;

public sealed class TicketMessage : BaseEntity
{
    private TicketMessage()
    {
    }

    public TicketMessage(
        Guid ticketId,
        Guid senderId,
        TicketSenderType senderType,
        string message,
        bool isInternalNote,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainException("متن پیام الزامی است.", "TICKETS_MESSAGE_REQUIRED");
        }

        TicketId = ticketId;
        SenderId = senderId;
        SenderType = senderType;
        Message = message.Trim();
        IsInternalNote = isInternalNote;
        MarkCreated(senderId, createdAtUtc);
    }

    public Guid TicketId { get; private set; }

    public Guid SenderId { get; private set; }

    public TicketSenderType SenderType { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public bool IsInternalNote { get; private set; }
}
