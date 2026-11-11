using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Wallet.Api.Contracts;
using CrmSuite.Modules.Wallet.Application.Commands;
using CrmSuite.Modules.Wallet.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Wallet.Api.Controllers;

[ApiController]
[Route("api/wallets")]
[Authorize]
[Audit]
public sealed class WalletsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("my-balance")]
    public async Task<IActionResult> GetMyBalance(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWalletBalanceQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("my-transactions")]
    public async Task<IActionResult> GetMyTransactions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWalletTransactionsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpPost("charge")]
    [HasPermission(Permissions.Wallet.CustomerWallet.Adjust)]
    public async Task<IActionResult> Charge(
        [FromBody] ChargeWalletRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChargeCustomerWalletCommand(
                request.CustomerId,
                request.Amount,
                request.PaymentMethod,
                request.ReferenceNumber),
            cancellationToken);

        return this.ToApiResult(result, "کیف پول مشتری شارژ شد.");
    }

    [HttpGet("customers/{customerId:guid}/balance")]
    [HasPermission(Permissions.Wallet.CustomerWallet.Read)]
    public async Task<IActionResult> GetCustomerBalance(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCustomerWalletBalanceQuery(customerId),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpPost("pay-invoice")]
    [HasPermission(Permissions.Wallet.CustomerWallet.Read)]
    public async Task<IActionResult> PayInvoice(
        [FromBody] PayInvoiceFromWalletRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new PayInvoiceFromWalletCommand(request.InvoiceId, request.CustomerId),
            cancellationToken);

        return this.ToApiResult(result, "فاکتور از کیف پول پرداخت شد.");
    }

    [HttpPost("withdraw")]
    [HasPermission(Permissions.Wallet.Withdrawal.Request)]
    public async Task<IActionResult> RequestWithdraw(
        [FromBody] RequestWithdrawRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RequestCommissionWithdrawCommand(request.Amount),
            cancellationToken);

        return this.ToApiResult(result, "درخواست برداشت ثبت شد.");
    }

    [HttpPut("withdraw/{id:guid}/approve")]
    [HasPermission(Permissions.Wallet.Withdrawal.Approve)]
    public async Task<IActionResult> ApproveWithdraw(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ApproveCommissionWithdrawCommand(id),
            cancellationToken);

        return this.ToApiResult(result, "برداشت تأیید شد.");
    }
}

public sealed class PayInvoiceFromWalletRequest
{
    public Guid InvoiceId { get; init; }

    public Guid? CustomerId { get; init; }
}
