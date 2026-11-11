using CrmSuite.Modules.Reports.Application.Dtos;

namespace CrmSuite.Modules.Reports.Application.Abstractions;

public interface IReportRepository
{
    Task<FinancialSummaryDto> GetFinancialSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FinancialTrendPointDto>> GetFinancialTrendAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DebtorDto>> GetDebtorsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BadDebtDto>> GetBadDebtorsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken);

    Task<CommissionReportDto> GetCommissionReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerLedgerEntryDto>> GetCustomerLedgerAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SalesReportItemDto>> GetSalesReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
