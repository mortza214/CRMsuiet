using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Payments.Api.Contracts;
using CrmSuite.Modules.Payments.Application.Commands;
using CrmSuite.Modules.Payments.Application.Queries;
using CrmSuite.Modules.Payments.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Payments.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
[Audit]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("manual")]
    [HasPermission(Permissions.Payments.ManualPayment.Create)]
    public async Task<IActionResult> CreateManual(
        [FromBody] CreateManualPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateManualPaymentCommand(
                request.InvoiceId,
                request.CustomerId,
                request.Amount,
                request.PaymentMethod,
                request.ReferenceNumber),
            cancellationToken);

        return this.ToApiResult(result, "پرداخت دستی ثبت شد و در انتظار تأیید مدیر مالی است.");
    }

    [HttpPut("manual/{id:guid}/approve")]
    [HasPermission(Permissions.Payments.Payment.Reconcile)]
    public async Task<IActionResult> ApproveManual(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApproveManualPaymentCommand(id), cancellationToken);
        return this.ToApiResult(result, "پرداخت دستی تأیید شد.");
    }

    [HttpPost("saman/initiate")]
    [HasPermission(Permissions.Payments.Payment.Create)]
    public async Task<IActionResult> InitiateOnline(
        [FromBody] InitiateOnlinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new InitiateOnlinePaymentCommand(request.InvoiceId, request.CallbackUrl),
            cancellationToken);

        return this.ToApiResult(result, "لینک پرداخت ایجاد شد.");
    }

    [HttpPost("saman/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOnline(
        [FromBody] VerifyOnlinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new VerifyOnlinePaymentCommand(request.TrackingCode, request.Token),
            cancellationToken);

        return this.ToApiResult(result, "پرداخت تأیید شد.");
    }

    [HttpGet("transactions")]
    [HasPermission(Permissions.Payments.Payment.Manage)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TransactionStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? invoiceId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTransactionsQuery(page, pageSize, status, customerId, invoiceId, fromUtc, toUtc),
            cancellationToken);

        return this.ToApiResult(result);
    }
}
