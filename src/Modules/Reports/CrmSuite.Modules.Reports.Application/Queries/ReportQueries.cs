using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Reports.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Reports.Application.Queries;

public sealed record GetFinancialSummaryQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IRequest<Result<FinancialSummaryDto>>;

public sealed record GetFinancialTrendQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IRequest<Result<IReadOnlyList<FinancialTrendPointDto>>>;

public sealed record GetCustomerDebtorsQuery(
    DateTime? AsOfUtc = null) : IRequest<Result<IReadOnlyList<DebtorDto>>>;

public sealed record GetBadDebtorsQuery(
    DateTime? AsOfUtc = null) : IRequest<Result<IReadOnlyList<BadDebtDto>>>;

public sealed record GetCommissionReportQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IRequest<Result<CommissionReportDto>>;

public sealed record GetCustomerLedgerQuery(
    Guid CustomerId) : IRequest<Result<IReadOnlyList<CustomerLedgerEntryDto>>>;

public sealed record GetSalesReportQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IRequest<Result<IReadOnlyList<SalesReportItemDto>>>;
