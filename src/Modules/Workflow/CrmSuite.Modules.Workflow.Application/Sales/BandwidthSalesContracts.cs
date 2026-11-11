using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.Sales;

public static class BandwidthWorkflowCodes
{
    public const string WithFeasibility = "WF-BANDWIDTH-FEASIBILITY";
    public const string NoFeasibility = "WF-BANDWIDTH-NO-FEASIBILITY";
}

public enum ServicePaymentMode
{
    Gateway = 1,
    ManualDeposit = 2,
    AfterInstallation = 3,
}

public enum PurchaseOrInvoiceDecision
{
    PurchaseEquipment = 1,
    IssueProforma = 2,
}

public sealed record ServiceRequestResultDto(
    Guid OrderId,
    string OrderNumber,
    Guid? WorkflowInstanceId,
    string? WorkflowCode,
    decimal TotalAmount,
    bool RequiresFeasibility);

public sealed record ServiceDecisionResultDto(
    WorkflowInstanceDto Workflow,
    Guid? InvoiceId,
    decimal Amount);

public sealed record ServiceActivationResultDto(
    Guid OrderId,
    Guid CustomerServiceId,
    Guid? WorkflowInstanceId,
    DateTime ActivatedAtUtc,
    decimal CommissionBase);

public sealed record CreateServiceRequestCommand(
    Guid CustomerId,
    Guid ServiceId,
    string AttributesJson,
    bool RequiresFeasibility = false) : IRequest<Result<ServiceRequestResultDto>>;

public sealed record SubmitFeasibilityReportCommand(
    Guid InstanceId,
    string ReportJson) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record DecidePurchaseOrInvoiceCommand(
    Guid InstanceId,
    PurchaseOrInvoiceDecision Decision) : IRequest<Result<ServiceDecisionResultDto>>;

public sealed record SetPaymentModeCommand(
    Guid InstanceId,
    ServicePaymentMode PaymentMode) : IRequest<Result<WorkflowInstanceDto>>;

public sealed record SubmitInstallationReportCommand(
    Guid InstanceId,
    string UsedItemsJson) : IRequest<Result<ServiceDecisionResultDto>>;

public sealed record ActivateServiceCommand(
    Guid OrderId) : IRequest<Result<ServiceActivationResultDto>>;

public sealed record ConfirmServicePaymentCommand(
    Guid InvoiceId) : IRequest<Result<ServiceActivationResultDto>>;
