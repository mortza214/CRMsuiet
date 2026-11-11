using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Foundation.Api.Filters;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Wallet.Api.Contracts;
using CrmSuite.Modules.Wallet.Application.Commands;
using CrmSuite.Modules.Wallet.Application.Queries;
using CrmSuite.Modules.Wallet.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Wallet.Api.Controllers;

[ApiController]
[Route("api/commissions")]
[Authorize]
[Audit]
public sealed class CommissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Commission.Entry.Approve)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] CommissionStatus? status = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAllCommissionsQuery(page, pageSize, status, employeeId, fromUtc, toUtc),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("my")]
    [HasPermission(Permissions.Commission.Entry.Read)]
    public async Task<IActionResult> GetMine(
        [FromQuery] CommissionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetEmployeeCommissionsQuery(status),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpPut("{id:guid}/approve")]
    [HasPermission(Permissions.Commission.Entry.Approve)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ApproveCommissionCommand(id), cancellationToken);
        return this.ToApiResult(result, "پورسانت تأیید و قابل پرداخت شد.");
    }

    [HttpPut("{id:guid}/reject")]
    [HasPermission(Permissions.Commission.Entry.Approve)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RejectCommissionCommand(id), cancellationToken);
        return this.ToApiResult(result, "پورسانت رد شد.");
    }

    [HttpPut("{id:guid}/shares")]
    [HasPermission(Permissions.Commission.Entry.Approve)]
    public async Task<IActionResult> UpdateShares(
        Guid id,
        [FromBody] UpdateCommissionSharesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCommissionSharesCommand(
                id,
                request.Shares
                    .Select(share => new CommissionShareInput(
                        share.UserId,
                        share.Percentage,
                        share.IsCompanyShare))
                    .ToArray()),
            cancellationToken);
        return this.ToApiResult(result, "سهم‌های پورسانت به‌روزرسانی شد.");
    }

    [HttpPost("rules")]
    [HasPermission(Permissions.Commission.Rule.Manage)]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateCommissionRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCommissionRuleCommand(
                request.ServiceId,
                request.ServiceType,
                request.Percentage,
                request.FixedAmount,
                request.PayoutType),
            cancellationToken);

        return this.ToApiResult(result, "قانون پورسانت ایجاد شد.");
    }

    [HttpPost("evaluate/{orderId:guid}")]
    [HasPermission(Permissions.Commission.Rule.Manage)]
    public async Task<IActionResult> Evaluate(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new EvaluateOrderCommissionCommand(orderId),
            cancellationToken);

        return this.ToApiResult(result, "پورسانت سفارش محاسبه شد.");
    }
}
