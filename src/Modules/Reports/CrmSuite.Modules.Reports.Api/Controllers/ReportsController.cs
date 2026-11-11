using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Identity.Api.Authorization;
using CrmSuite.Modules.Reports.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Reports.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("financial-summary")]
    [HasPermission(Permissions.Reports.Financial.Read)]
    public async Task<IActionResult> GetFinancialSummary(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = DefaultRange(fromUtc, toUtc);
        var result = await _mediator.Send(
            new GetFinancialSummaryQuery(from, to),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("financial-trend")]
    [HasPermission(Permissions.Reports.Financial.Read)]
    public async Task<IActionResult> GetFinancialTrend(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = DefaultRange(fromUtc, toUtc);
        var result = await _mediator.Send(
            new GetFinancialTrendQuery(from, to),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("debtors")]
    [HasPermission(Permissions.Reports.Debtors.Read)]
    public async Task<IActionResult> GetDebtors(
        [FromQuery] DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomerDebtorsQuery(asOfUtc), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("bad-debtors")]
    [HasPermission(Permissions.Reports.Debtors.Read)]
    public async Task<IActionResult> GetBadDebtors(
        [FromQuery] DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBadDebtorsQuery(asOfUtc), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("commissions")]
    [HasPermission(Permissions.Reports.Commissions.Read)]
    public async Task<IActionResult> GetCommissions(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = DefaultRange(fromUtc, toUtc);
        var result = await _mediator.Send(
            new GetCommissionReportQuery(from, to),
            cancellationToken);

        return this.ToApiResult(result);
    }

    [HttpGet("customers/{id:guid}/ledger")]
    [HasPermission(Permissions.Reports.CustomerPayments.Read)]
    public async Task<IActionResult> GetCustomerLedger(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerLedgerQuery(id), cancellationToken);
        return this.ToApiResult(result);
    }

    [HttpGet("sales")]
    [HasPermission(Permissions.Reports.Sales.Read)]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = DefaultRange(fromUtc, toUtc);
        var result = await _mediator.Send(new GetSalesReportQuery(from, to), cancellationToken);
        return this.ToApiResult(result);
    }

    private static (DateTime From, DateTime To) DefaultRange(
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var to = toUtc ?? DateTime.UtcNow;
        var from = fromUtc ?? to.AddDays(-30);
        return (
            DateTime.SpecifyKind(from, DateTimeKind.Utc),
            DateTime.SpecifyKind(to, DateTimeKind.Utc));
    }
}
