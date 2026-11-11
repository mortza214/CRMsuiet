using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Payments.Api.Contracts;
using CrmSuite.Modules.Payments.Application.Commands;
using CrmSuite.Modules.Payments.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Payments.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/payments")]
[Authorize(Roles = Roles.Customer)]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("manual")]
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
                request.ReferenceNumber,
                request.ReceiptImageUrl),
            cancellationToken);
        return this.ToApiResult(result, "فیش پرداخت ثبت شد و در انتظار تأیید مالی است.");
    }

    [HttpPost("saman/initiate")]
    public async Task<IActionResult> InitiateOnline(
        [FromBody] InitiateOnlinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new InitiateOnlinePaymentCommand(request.InvoiceId, request.CallbackUrl),
            cancellationToken);
        return this.ToApiResult(result, "لینک پرداخت ایجاد شد.");
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetMyTransactions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerTransactionsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }
}
