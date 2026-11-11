using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Sales;

namespace CrmSuite.Modules.Workflow.Api.Contracts;

public sealed class SubmitFeasibilityResultRequest
{
    public string DataJson { get; init; } = "{}";
}

public sealed class ReviewFeasibilityRequest
{
    public bool Approved { get; init; }
    public string? Comment { get; init; }
}

public sealed class SelectEquipmentSupplyRequest
{
    public EquipmentSupplyMode Mode { get; init; }
}

public sealed class IssueProformaRequest
{
    public ServicePaymentMode PaymentMode { get; init; }
    public BillingInvoiceType InvoiceType { get; init; } = BillingInvoiceType.Proforma;
    public bool RequiresPayment { get; init; } = true;
}

public sealed class ConfirmProformaRequest
{
    public bool Approved { get; init; }
    public bool NeedsInstallation { get; init; } = true;
}

public sealed class SubmitInstallationSpecsRequest
{
    public string DataJson { get; init; } = "{}";
}

public sealed class IssueFinalInvoiceRequest
{
    public BillingInvoiceType InvoiceType { get; init; } = BillingInvoiceType.Official;
    public bool RequiresContract { get; init; }
}

public sealed class CancelServiceRequest
{
    public string? Reason { get; init; }
}

public sealed class AdjustBandwidthRequest
{
    public int NewQuantity { get; init; }
}
