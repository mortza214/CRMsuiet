using CrmSuite.Modules.Tickets.Domain.Enums;

namespace CrmSuite.Modules.Tickets.Api.Contracts;

public sealed class CreateStaffTicketRequest
{
    public Guid CustomerId { get; init; }

    public Guid? ServiceId { get; init; }

    public string Subject { get; init; } = string.Empty;

    public Guid DepartmentId { get; init; }

    public TicketPriority Priority { get; init; }

    public TicketSource Source { get; init; }

    public string Message { get; init; } = string.Empty;

    public Guid? AssignedToUserId { get; init; }
}

public sealed class CreatePortalTicketRequest
{
    public Guid? ServiceId { get; init; }

    public string Subject { get; init; } = string.Empty;

    public Guid DepartmentId { get; init; }

    public TicketPriority Priority { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class TicketMessageRequest
{
    public string Message { get; init; } = string.Empty;

    public bool IsInternalNote { get; init; }
}

public sealed class ChangeTicketStatusRequest
{
    public TicketStatus Status { get; init; }

    public string? Note { get; init; }
}

public sealed class AssignTicketRequest
{
    public Guid? AssignedToUserId { get; init; }
}
