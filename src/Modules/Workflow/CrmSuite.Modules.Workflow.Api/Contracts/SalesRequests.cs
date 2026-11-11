using CrmSuite.Modules.Workflow.Application.Sales;

namespace CrmSuite.Modules.Workflow.Api.Contracts;

public sealed class CreateServiceRequestRequest
{
    public Guid CustomerId { get; init; }
    public Guid ServiceId { get; init; }
    public string AttributesJson { get; init; } = "{}";
    public bool RequiresFeasibility { get; init; }
}

public sealed class SubmitFeasibilityReportRequest
{
    public string ReportJson { get; init; } = "{}";
}

public sealed class DecidePurchaseOrInvoiceRequest
{
    public PurchaseOrInvoiceDecision Decision { get; init; }
}

public sealed class SetPaymentModeRequest
{
    public ServicePaymentMode PaymentMode { get; init; }
}

public sealed class SubmitInstallationReportRequest
{
    public string UsedItemsJson { get; init; } = "{}";
}
