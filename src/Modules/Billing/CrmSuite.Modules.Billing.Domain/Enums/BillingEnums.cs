namespace CrmSuite.Modules.Billing.Domain.Enums;

public enum BillingInvoiceType
{
    Official = 1,
    Unofficial = 2,
    Proforma = 3,
    Monthly = 4,
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Canceled = 5,
    PendingPayment = 6,
}

public enum ContractStatus
{
    Draft = 1,
    Active = 2,
    Expired = 3,
    Terminated = 4,
}
