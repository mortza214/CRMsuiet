using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Sales;

public enum EquipmentSupplyMode
{
    Purchase = 1,
    Consignment = 2,
    Existing = 3,
}

public sealed record ServiceChangeResultDto(
    Guid CustomerServiceId,
    Guid? InvoiceId,
    decimal Amount);

public sealed record SubmitFeasibilityResultCommand(
    Guid InstanceId,
    string DataJson) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record ReviewFeasibilityCommand(
    Guid InstanceId,
    bool Approved,
    string? Comment = null) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record SelectEquipmentSupplyCommand(
    Guid InstanceId,
    EquipmentSupplyMode Mode) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record CompletePurchaseCommand(
    Guid InstanceId) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record IssueProformaCommand(
    Guid InstanceId,
    ServicePaymentMode PaymentMode,
    BillingInvoiceType InvoiceType,
    bool RequiresPayment) : IRequest<Result<ServiceDecisionResultDto>>;

public sealed record ConfirmProformaCommand(
    Guid InstanceId,
    bool Approved,
    bool NeedsInstallation = true) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record SubmitInstallationSpecsCommand(
    Guid InstanceId,
    string DataJson) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record IssueFinalInvoiceCommand(
    Guid InstanceId,
    BillingInvoiceType InvoiceType,
    bool RequiresContract = false) : IRequest<Result<ServiceDecisionResultDto>>;

public sealed record ActivateCustomerServiceCommand(
    Guid OrderId) : IRequest<Result<ServiceActivationResultDto>>;

public sealed record CancelServiceRequestCommand(
    Guid OrderId,
    string? Reason = null) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record CollectServiceCommand(
    Guid CustomerServiceId) : IRequest<Result>;

public sealed record AdjustBandwidthCommand(
    Guid CustomerServiceId,
    int NewQuantity) : IRequest<Result<ServiceChangeResultDto>>;
