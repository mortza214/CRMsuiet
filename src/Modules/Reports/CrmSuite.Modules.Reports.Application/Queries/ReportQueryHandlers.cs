using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Reports.Application.Abstractions;
using CrmSuite.Modules.Reports.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Reports.Application.Queries;

public sealed class GetFinancialSummaryQueryHandler
    : IRequestHandler<GetFinancialSummaryQuery, Result<FinancialSummaryDto>>
{
    private readonly IReportRepository _reports;

    public GetFinancialSummaryQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<Result<FinancialSummaryDto>> Handle(
        GetFinancialSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GetFinancialSummaryAsync(
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        return Result.Success(result);
    }
}

public sealed class GetFinancialTrendQueryHandler
    : IRequestHandler<GetFinancialTrendQuery, Result<IReadOnlyList<FinancialTrendPointDto>>>
{
    private readonly IReportRepository _reports;

    public GetFinancialTrendQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<Result<IReadOnlyList<FinancialTrendPointDto>>> Handle(
        GetFinancialTrendQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GetFinancialTrendAsync(
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        return Result.Success(result);
    }
}

public sealed class GetCustomerDebtorsQueryHandler
    : IRequestHandler<GetCustomerDebtorsQuery, Result<IReadOnlyList<DebtorDto>>>
{
    private readonly IReportRepository _reports;
    private readonly IDateTimeProvider _clock;

    public GetCustomerDebtorsQueryHandler(IReportRepository reports, IDateTimeProvider clock)
    {
        _reports = reports;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<DebtorDto>>> Handle(
        GetCustomerDebtorsQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = request.AsOfUtc ?? _clock.UtcNow;
        var debtors = await _reports.GetDebtorsAsync(asOf, cancellationToken);
        return Result.Success(debtors);
    }
}

public sealed class GetBadDebtorsQueryHandler
    : IRequestHandler<GetBadDebtorsQuery, Result<IReadOnlyList<BadDebtDto>>>
{
    private readonly IReportRepository _reports;
    private readonly IDateTimeProvider _clock;

    public GetBadDebtorsQueryHandler(IReportRepository reports, IDateTimeProvider clock)
    {
        _reports = reports;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<BadDebtDto>>> Handle(
        GetBadDebtorsQuery request,
        CancellationToken cancellationToken)
    {
        var asOf = request.AsOfUtc ?? _clock.UtcNow;
        var debtors = await _reports.GetBadDebtorsAsync(asOf, cancellationToken);
        return Result.Success(debtors);
    }
}

public sealed class GetCommissionReportQueryHandler
    : IRequestHandler<GetCommissionReportQuery, Result<CommissionReportDto>>
{
    private readonly IReportRepository _reports;

    public GetCommissionReportQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<Result<CommissionReportDto>> Handle(
        GetCommissionReportQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _reports.GetCommissionReportAsync(
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        return Result.Success(result);
    }
}

public sealed class GetCustomerLedgerQueryHandler
    : IRequestHandler<GetCustomerLedgerQuery, Result<IReadOnlyList<CustomerLedgerEntryDto>>>
{
    private readonly IReportRepository _reports;

    public GetCustomerLedgerQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<Result<IReadOnlyList<CustomerLedgerEntryDto>>> Handle(
        GetCustomerLedgerQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await _reports.GetCustomerLedgerAsync(request.CustomerId, cancellationToken);
        var balance = 0m;
        var result = new List<CustomerLedgerEntryDto>(entries.Count);

        foreach (var entry in entries.OrderBy(entry => entry.Date))
        {
            balance += entry.Debit - entry.Credit;
            result.Add(entry with { Balance = balance });
        }

        return Result.Success<IReadOnlyList<CustomerLedgerEntryDto>>(result);
    }
}

public sealed class GetSalesReportQueryHandler
    : IRequestHandler<GetSalesReportQuery, Result<IReadOnlyList<SalesReportItemDto>>>
{
    private readonly IReportRepository _reports;

    public GetSalesReportQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<Result<IReadOnlyList<SalesReportItemDto>>> Handle(
        GetSalesReportQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetSalesReportAsync(
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        return Result.Success(report);
    }
}
