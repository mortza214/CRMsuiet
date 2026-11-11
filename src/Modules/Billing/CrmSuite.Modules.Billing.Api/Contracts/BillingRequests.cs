using CrmSuite.Modules.Billing.Domain.Enums;

namespace CrmSuite.Modules.Billing.Api.Contracts;

public sealed class GenerateInvoiceRequest
{
    public BillingInvoiceType Type { get; init; }

    public DateTime? DueDateUtc { get; init; }
}

public sealed class UpdateInvoiceStatusRequest
{
    public InvoiceStatus Status { get; init; }
}

public sealed class GenerateContractRequest
{
    public Guid CustomerId { get; init; }

    public Guid? OrderId { get; init; }

    public DateTime StartDateUtc { get; init; }

    public DateTime EndDateUtc { get; init; }

    public string? Content { get; init; }
}
