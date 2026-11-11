namespace CrmSuite.Modules.Tickets.Domain.Enums;

public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    PendingCustomer = 3,
    Resolved = 4,
    Closed = 5,
}

public enum TicketSource
{
    Portal = 1,
    Phone = 2,
    Internal = 3,
}

public enum TicketSenderType
{
    Customer = 1,
    Employee = 2,
}
