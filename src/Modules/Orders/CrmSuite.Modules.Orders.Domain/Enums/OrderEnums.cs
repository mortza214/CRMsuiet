namespace CrmSuite.Modules.Orders.Domain.Enums;

public enum OrderStatus
{
    Draft = 1,
    PendingPayment = 2,
    Paid = 3,
    Canceled = 4,
    Provisioning = 5,
    Completed = 6,
    Pending = 7,
}

public enum CustomerServiceStatus
{
    Active = 1,
    Suspended = 2,
    Canceled = 3,
    Collected = 4,
}

public enum OrderType
{
    New = 1,
    Renewal = 2,
    Upgrade = 3,
}

public enum BillingCycle
{
    OneTime = 1,
    Monthly = 2,
    Yearly = 3,
}
