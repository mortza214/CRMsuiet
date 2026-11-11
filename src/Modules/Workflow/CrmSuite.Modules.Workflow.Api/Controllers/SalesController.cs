using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Workflow.Api.Contracts;
using CrmSuite.Modules.Workflow.Application.Sales;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
[Audit]
public sealed class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("service-requests")]
    [HasPermission(Permissions.Orders.Order.Read)]
    public async Task<IActionResult> GetRecentServiceRequests(
        [FromQuery] Guid? salesExpertId = null,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetRecentServiceRequestsQuery(salesExpertId, limit),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("service-requests/{orderId:guid}")]
    [HasPermission(Permissions.Orders.Order.Read)]
    public async Task<IActionResult> GetServiceRequest(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetServiceRequestDetailsQuery(orderId),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("customers/{customerId:guid}/service-requests")]
    [HasPermission(Permissions.Orders.Order.Read)]
    public async Task<IActionResult> GetCustomerServiceRequests(
        Guid customerId,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCustomerServiceRequestsQuery(customerId, limit),
            cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("service-requests")]
    [HasPermission(Permissions.Orders.Order.Create)]
    public async Task<IActionResult> CreateServiceRequest(
        [FromBody] CreateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateServiceRequestCommand(
                request.CustomerId,
                request.ServiceId,
                request.AttributesJson,
                request.RequiresFeasibility),
            cancellationToken);
        return this.ToApiResult(result, "درخواست سرویس ثبت و گردش کار آغاز شد.");
    }

    [HttpPost("instances/{instanceId:guid}/feasibility-result")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> SubmitFeasibilityResult(
        Guid instanceId,
        [FromBody] SubmitFeasibilityResultRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitFeasibilityResultCommand(instanceId, request.DataJson),
            cancellationToken);
        return this.ToApiResult(result, "نتیجه امکان‌سنجی ثبت شد.");
    }

    [HttpPost("instances/{instanceId:guid}/feasibility-review")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> ReviewFeasibility(
        Guid instanceId,
        [FromBody] ReviewFeasibilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ReviewFeasibilityCommand(instanceId, request.Approved, request.Comment),
            cancellationToken);
        return this.ToApiResult(result, "بررسی امکان‌سنجی ثبت شد.");
    }

    [HttpPost("instances/{instanceId:guid}/equipment-supply")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> SelectEquipmentSupply(
        Guid instanceId,
        [FromBody] SelectEquipmentSupplyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SelectEquipmentSupplyCommand(instanceId, request.Mode),
            cancellationToken);
        return this.ToApiResult(result, "روش تأمین تجهیزات ثبت شد.");
    }

    [HttpPost("instances/{instanceId:guid}/purchase-completed")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> CompletePurchase(
        Guid instanceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompletePurchaseCommand(instanceId),
            cancellationToken);
        return this.ToApiResult(result, "خرید تجهیزات تکمیل شد.");
    }

    [HttpPost("instances/{instanceId:guid}/proforma")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> IssueProforma(
        Guid instanceId,
        [FromBody] IssueProformaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new IssueProformaCommand(
                instanceId,
                request.PaymentMode,
                request.InvoiceType,
                request.RequiresPayment),
            cancellationToken);
        return this.ToApiResult(result, "پیش‌فاکتور صادر شد.");
    }

    [HttpPost("instances/{instanceId:guid}/confirm-proforma")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> ConfirmProforma(
        Guid instanceId,
        [FromBody] ConfirmProformaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConfirmProformaCommand(instanceId, request.Approved, request.NeedsInstallation),
            cancellationToken);
        return this.ToApiResult(result, "تأیید پیش‌فاکتور ثبت شد.");
    }

    [HttpPost("instances/{instanceId:guid}/installation-specs")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> SubmitInstallationSpecs(
        Guid instanceId,
        [FromBody] SubmitInstallationSpecsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitInstallationSpecsCommand(instanceId, request.DataJson),
            cancellationToken);
        return this.ToApiResult(result, "مشخصات نصب ثبت شد.");
    }

    [HttpPost("instances/{instanceId:guid}/final-invoice")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> IssueFinalInvoice(
        Guid instanceId,
        [FromBody] IssueFinalInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new IssueFinalInvoiceCommand(instanceId, request.InvoiceType, request.RequiresContract),
            cancellationToken);
        return this.ToApiResult(result, "فاکتور نهایی صادر شد.");
    }

    [HttpPost("instances/{instanceId:guid}/feasibility")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> SubmitFeasibility(
        Guid instanceId,
        [FromBody] SubmitFeasibilityReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitFeasibilityReportCommand(instanceId, request.ReportJson),
            cancellationToken);
        return this.ToApiResult(result, "گزارش امکان‌سنجی ثبت و به فروش بازگشت.");
    }

    [HttpPost("instances/{instanceId:guid}/decision")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> Decide(
        Guid instanceId,
        [FromBody] DecidePurchaseOrInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DecidePurchaseOrInvoiceCommand(instanceId, request.Decision),
            cancellationToken);
        return this.ToApiResult(result, "تصمیم خرید/پیش‌فاکتور ثبت شد.");
    }

    [HttpPost("instances/{instanceId:guid}/payment-mode")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> SetPaymentMode(
        Guid instanceId,
        [FromBody] SetPaymentModeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SetPaymentModeCommand(instanceId, request.PaymentMode),
            cancellationToken);
        return this.ToApiResult(result, "روش پرداخت ثبت شد.");
    }

    [HttpPost("instances/{instanceId:guid}/installation")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> SubmitInstallation(
        Guid instanceId,
        [FromBody] SubmitInstallationReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SubmitInstallationReportCommand(instanceId, request.UsedItemsJson),
            cancellationToken);
        return this.ToApiResult(result, "گزارش نصب ثبت و پیش‌فاکتور نهایی صادر شد.");
    }

    [HttpPost("orders/{orderId:guid}/activate-customer-service")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> ActivateCustomerService(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ActivateCustomerServiceCommand(orderId),
            cancellationToken);
        return this.ToApiResult(result, "سرویس مشتری فعال شد.");
    }

    [HttpPost("orders/{orderId:guid}/activate")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> Activate(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ActivateServiceCommand(orderId),
            cancellationToken);
        return this.ToApiResult(result, "سرویس فعال شد.");
    }

    [HttpPost("orders/{orderId:guid}/cancel-request")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> CancelRequest(
        Guid orderId,
        [FromBody] CancelServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelServiceRequestCommand(orderId, request.Reason),
            cancellationToken);
        return this.ToApiResult(result, "درخواست لغو شد.");
    }

    [HttpPost("services/{customerServiceId:guid}/collect")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> CollectService(
        Guid customerServiceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CollectServiceCommand(customerServiceId),
            cancellationToken);
        return this.ToApiResult(result, "سرویس جمع‌آوری شد.");
    }

    [HttpPost("services/{customerServiceId:guid}/adjust-bandwidth")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> AdjustBandwidth(
        Guid customerServiceId,
        [FromBody] AdjustBandwidthRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AdjustBandwidthCommand(customerServiceId, request.NewQuantity),
            cancellationToken);
        return this.ToApiResult(result, "تغییر پهنای باند ثبت شد.");
    }

    [HttpPost("invoices/{invoiceId:guid}/confirm-payment")]
    [HasPermission(Permissions.Orders.Order.Edit)]
    public async Task<IActionResult> ConfirmPayment(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConfirmServicePaymentCommand(invoiceId),
            cancellationToken);
        return this.ToApiResult(result, "پرداخت تأیید و سرویس فعال شد.");
    }
}
