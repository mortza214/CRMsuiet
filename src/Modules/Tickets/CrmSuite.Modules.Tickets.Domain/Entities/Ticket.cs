using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Tickets.Domain.Enums;

namespace CrmSuite.Modules.Tickets.Domain.Entities;

public sealed class Ticket : BaseEntity
{
    private Ticket()
    {
    }

    public Ticket(
        string ticketNumber,
        Guid customerId,
        Guid? serviceId,
        string subject,
        Guid departmentId,
        Guid? assignedToUserId,
        TicketPriority priority,
        TicketSource source,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            throw new DomainException("شماره تیکت الزامی است.", "TICKETS_NUMBER_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException("موضوع تیکت الزامی است.", "TICKETS_SUBJECT_REQUIRED");
        }

        TicketNumber = ticketNumber.Trim();
        CustomerId = customerId;
        ServiceId = serviceId;
        Subject = subject.Trim();
        DepartmentId = departmentId;
        AssignedToUserId = assignedToUserId;
        Priority = priority;
        Status = TicketStatus.Open;
        Source = source;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string TicketNumber { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public Guid? ServiceId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public Guid DepartmentId { get; private set; }

    public Guid? WorkflowInstanceId { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public TicketPriority Priority { get; private set; }

    public TicketStatus Status { get; private set; }

    public TicketSource Source { get; private set; }

    public ICollection<TicketMessage> Messages { get; private set; } = new List<TicketMessage>();

    public ICollection<TicketStatusHistory> StatusHistory { get; private set; } = new List<TicketStatusHistory>();

    public void AddMessage(TicketMessage message)
    {
        if (message.TicketId != Id)
        {
            throw new DomainException("پیام به تیکت جاری تعلق ندارد.", "TICKETS_MESSAGE_MISMATCH");
        }

        if (message.IsInternalNote && message.SenderType == TicketSenderType.Customer)
        {
            throw new DomainException("مشتری نمی‌تواند یادداشت داخلی ثبت کند.", "TICKETS_INTERNAL_NOTE_FORBIDDEN");
        }

        Messages.Add(message);

        if (message.IsInternalNote)
        {
            return;
        }

        if (message.SenderType == TicketSenderType.Customer
            && Status is TicketStatus.Open or TicketStatus.PendingCustomer or TicketStatus.Resolved)
        {
            if (Status != TicketStatus.Open)
            {
                AddHistory(Status, TicketStatus.Open, message.SenderId, message.CreatedAt, "پاسخ مشتری");
            }

            Status = TicketStatus.Open;
        }
        else if (message.SenderType == TicketSenderType.Employee
                 && Status is TicketStatus.Open or TicketStatus.PendingCustomer)
        {
            AddHistory(Status, TicketStatus.InProgress, message.SenderId, message.CreatedAt, "پاسخ کارمند");
            Status = TicketStatus.InProgress;
        }

        MarkUpdated(message.SenderId, message.CreatedAt);
    }

    public void ChangeStatus(
        TicketStatus newStatus,
        Guid? userId,
        DateTime utcNow,
        string? note)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (Status == TicketStatus.Closed && newStatus != TicketStatus.Open)
        {
            throw new DomainException("تیکت بسته‌شده فقط قابل بازگشایی است.", "TICKETS_STATUS_INVALID");
        }

        AddHistory(Status, newStatus, userId, utcNow, note);
        Status = newStatus;
        MarkUpdated(userId, utcNow);
    }

    public void Assign(Guid? userId, DateTime utcNow)
    {
        AssignedToUserId = userId;
        MarkUpdated(userId, utcNow);
    }

    public void AttachWorkflowInstance(Guid instanceId, DateTime utcNow, Guid? userId = null)
    {
        if (instanceId == Guid.Empty)
        {
            throw new DomainException("شناسه گردش کار معتبر نیست.", "TICKETS_WORKFLOW_INSTANCE_INVALID");
        }

        WorkflowInstanceId = instanceId;
        MarkUpdated(userId, utcNow);
    }

    public void AddInitialHistory(Guid? userId, DateTime utcNow)
    {
        AddHistory(null, TicketStatus.Open, userId, utcNow, "ایجاد تیکت");
    }

    private void AddHistory(
        TicketStatus? fromStatus,
        TicketStatus toStatus,
        Guid? userId,
        DateTime changedAt,
        string? note)
    {
        StatusHistory.Add(new TicketStatusHistory(
            Id,
            fromStatus,
            toStatus,
            userId,
            changedAt,
            note));
    }
}
