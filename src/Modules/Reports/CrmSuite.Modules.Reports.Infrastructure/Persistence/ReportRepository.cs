using System.Data;
using System.Globalization;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Reports.Application.Abstractions;
using CrmSuite.Modules.Reports.Application.Dtos;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Reports.Infrastructure.Persistence;

public sealed class ReportRepository : IReportRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public ReportRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COALESCE((
                    SELECT SUM(i.total_amount)
                    FROM invoices i
                    WHERE i.is_deleted = 0
                      AND i.status <> 'Canceled'
                      AND i.issue_date >= @FromUtc
                      AND i.issue_date <= @ToUtc
                ), 0) AS TotalSales,
                COALESCE((
                    SELECT SUM(t.amount)
                    FROM transactions t
                    WHERE t.is_deleted = 0
                      AND t.status = 'Success'
                      AND t.paid_at IS NOT NULL
                      AND t.paid_at >= @FromUtc
                      AND t.paid_at <= @ToUtc
                      AND t.payment_method <> 'Wallet'
                ), 0) AS TotalReceived,
                COALESCE((
                    SELECT SUM(i.total_amount - i.paid_amount)
                    FROM invoices i
                    WHERE i.is_deleted = 0
                      AND i.status IN ('Issued', 'PartiallyPaid')
                      AND i.issue_date >= @FromUtc
                      AND i.issue_date <= @ToUtc
                ), 0) AS TotalDebt,
                COALESCE((
                    SELECT COUNT(*)
                    FROM invoices i
                    WHERE i.is_deleted = 0
                      AND i.status <> 'Canceled'
                      AND i.issue_date >= @FromUtc
                      AND i.issue_date <= @ToUtc
                ), 0) AS InvoiceCount;
            """;

        var row = await Connection.QuerySingleAsync<FinancialRow>(
            new CommandDefinition(sql, new { FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: cancellationToken));

        return new FinancialSummaryDto(
            fromUtc,
            toUtc,
            row.TotalSales,
            row.TotalReceived,
            row.TotalDebt,
            row.InvoiceCount);
    }

    public async Task<IReadOnlyList<FinancialTrendPointDto>> GetFinancialTrendAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                m.Month,
                SUM(m.Sales) AS Sales,
                SUM(m.Received) AS Received
            FROM (
                SELECT
                    DATE_FORMAT(i.issue_date, '%Y-%m') AS Month,
                    SUM(i.total_amount) AS Sales,
                    0 AS Received
                FROM invoices i
                WHERE i.is_deleted = 0
                  AND i.status <> 'Canceled'
                  AND i.issue_date >= @FromUtc
                  AND i.issue_date <= @ToUtc
                GROUP BY DATE_FORMAT(i.issue_date, '%Y-%m')

                UNION ALL

                SELECT
                    DATE_FORMAT(t.paid_at, '%Y-%m') AS Month,
                    0 AS Sales,
                    SUM(t.amount) AS Received
                FROM transactions t
                WHERE t.is_deleted = 0
                  AND t.status = 'Success'
                  AND t.paid_at IS NOT NULL
                  AND t.paid_at >= @FromUtc
                  AND t.paid_at <= @ToUtc
                  AND t.payment_method <> 'Wallet'
                GROUP BY DATE_FORMAT(t.paid_at, '%Y-%m')
            ) m
            GROUP BY m.Month
            ORDER BY m.Month;
            """;

        var rows = await Connection.QueryAsync<TrendRow>(
            new CommandDefinition(
                sql,
                new { FromUtc = fromUtc, ToUtc = toUtc },
                cancellationToken: cancellationToken));

        return rows.Select(row =>
        {
            var monthStart = DateTime.ParseExact(
                $"{row.Month}-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            return new FinancialTrendPointDto(
                row.Month,
                _jalaliDate.ToJalali(monthStart),
                row.Sales,
                row.Received);
        }).ToArray();
    }

    public async Task<IReadOnlyList<DebtorDto>> GetDebtorsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                c.mobile AS Mobile,
                SUM(i.total_amount - i.paid_amount) AS OutstandingAmount,
                COUNT(*) AS InvoiceCount
            FROM invoices i
            JOIN customers c ON c.id = i.customer_id AND c.is_deleted = 0
            WHERE i.is_deleted = 0
              AND i.status IN ('Issued', 'PartiallyPaid')
              AND i.total_amount > i.paid_amount
              AND i.issue_date <= @AsOfUtc
            GROUP BY c.id, c.type, c.company_name, c.national_id, c.first_name, c.last_name, c.mobile
            HAVING SUM(i.total_amount - i.paid_amount) > 0
            ORDER BY OutstandingAmount DESC;
            """;

        var rows = await Connection.QueryAsync<DebtorRow>(
            new CommandDefinition(sql, new { AsOfUtc = asOfUtc }, cancellationToken: cancellationToken));

        return rows.Select(row => new DebtorDto(
            row.CustomerId,
            row.CustomerName?.Trim() ?? string.Empty,
            row.Mobile,
            row.OutstandingAmount,
            row.InvoiceCount)).ToArray();
    }

    public async Task<IReadOnlyList<BadDebtDto>> GetBadDebtorsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                c.mobile AS Mobile,
                SUM(i.total_amount - i.paid_amount) AS OutstandingAmount,
                COUNT(*) AS OverdueInvoiceCount,
                MAX(DATEDIFF(@AsOfUtc, i.due_date)) AS MaxOverdueDays
            FROM invoices i
            JOIN customers c ON c.id = i.customer_id AND c.is_deleted = 0
            WHERE i.is_deleted = 0
              AND i.status IN ('Issued', 'PartiallyPaid')
              AND i.total_amount > i.paid_amount
              AND i.due_date < @AsOfUtc
            GROUP BY c.id, c.type, c.company_name, c.national_id, c.first_name, c.last_name, c.mobile
            HAVING SUM(i.total_amount - i.paid_amount) > 0
            ORDER BY MaxOverdueDays DESC, OutstandingAmount DESC;
            """;

        var rows = await Connection.QueryAsync<BadDebtRow>(
            new CommandDefinition(sql, new { AsOfUtc = asOfUtc }, cancellationToken: cancellationToken));

        return rows.Select(row => new BadDebtDto(
            row.CustomerId,
            row.CustomerName?.Trim() ?? string.Empty,
            row.Mobile,
            row.OutstandingAmount,
            row.OverdueInvoiceCount,
            row.MaxOverdueDays)).ToArray();
    }

    public async Task<CommissionReportDto> GetCommissionReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COALESCE(SUM(CASE WHEN status = 'Paid' THEN amount - adjustment_amount ELSE 0 END), 0) AS PaidAmount,
                COALESCE(SUM(CASE WHEN status IN ('Calculated', 'Payable') THEN amount - adjustment_amount ELSE 0 END), 0) AS UnpaidAmount,
                COALESCE(SUM(CASE WHEN status = 'Paid' THEN 1 ELSE 0 END), 0) AS PaidCount,
                COALESCE(SUM(CASE WHEN status IN ('Calculated', 'Payable') THEN 1 ELSE 0 END), 0) AS UnpaidCount
            FROM commission_entries
            WHERE is_deleted = 0
              AND created_at >= @FromUtc
              AND created_at <= @ToUtc;
            """;

        var row = await Connection.QuerySingleAsync<CommissionRow>(
            new CommandDefinition(sql, new { FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: cancellationToken));

        return new CommissionReportDto(
            fromUtc,
            toUtc,
            row.PaidAmount,
            row.UnpaidAmount,
            row.PaidCount,
            row.UnpaidCount);
    }

    public async Task<IReadOnlyList<CustomerLedgerEntryDto>> GetCustomerLedgerAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                i.issue_date AS EntryDate,
                'Invoice' COLLATE utf8mb4_persian_ci AS EntryType,
                i.invoice_number COLLATE utf8mb4_persian_ci AS Reference,
                CONCAT('فاکتور ', i.invoice_number) COLLATE utf8mb4_persian_ci AS Description,
                i.total_amount AS Debit,
                0 AS Credit
            FROM invoices i
            WHERE i.is_deleted = 0 AND i.customer_id = @CustomerId AND i.status <> 'Canceled'

            UNION ALL

            SELECT
                t.paid_at AS EntryDate,
                'Payment' COLLATE utf8mb4_persian_ci AS EntryType,
                t.tracking_code COLLATE utf8mb4_persian_ci AS Reference,
                CONCAT('پرداخت ', t.tracking_code) COLLATE utf8mb4_persian_ci AS Description,
                0 AS Debit,
                t.amount AS Credit
            FROM transactions t
            WHERE t.is_deleted = 0
              AND t.customer_id = @CustomerId
              AND t.status = 'Success'
              AND t.paid_at IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM wallet_transactions wt
                  WHERE wt.reference_id = t.id
                    AND wt.is_deleted = 0
                    AND wt.transaction_type = 'Deposit'
              )

            UNION ALL

            SELECT
                wt.created_at AS EntryDate,
                'WalletDeposit' COLLATE utf8mb4_persian_ci AS EntryType,
                CAST(wt.id AS CHAR) COLLATE utf8mb4_persian_ci AS Reference,
                COALESCE(wt.description, 'شارژ کیف پول') COLLATE utf8mb4_persian_ci AS Description,
                0 AS Debit,
                wt.amount AS Credit
            FROM wallet_transactions wt
            JOIN wallets w ON w.id = wt.wallet_id AND w.is_deleted = 0
            WHERE wt.is_deleted = 0
              AND w.owner_type = 'Customer'
              AND w.owner_id = @CustomerId
              AND wt.transaction_type = 'Deposit'

            ORDER BY EntryDate;
            """;

        var rows = await Connection.QueryAsync<LedgerRow>(
            new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: cancellationToken));

        return rows.Select(row => new CustomerLedgerEntryDto(
            AsUtc(row.EntryDate),
            _jalaliDate.ToJalali(AsUtc(row.EntryDate)),
            row.EntryType,
            row.Reference,
            row.Description,
            row.Debit,
            row.Credit,
            0m)).ToArray();
    }

    public async Task<IReadOnlyList<SalesReportItemDto>> GetSalesReportAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COALESCE(cat.name, s.service_type) AS ServiceType,
                COUNT(DISTINCT o.id) AS OrderCount,
                COALESCE(SUM(oi.quantity), 0) AS Quantity,
                COALESCE(SUM(oi.total_price), 0) AS TotalAmount
            FROM order_items oi
            JOIN orders o ON o.id = oi.order_id AND o.is_deleted = 0
            JOIN services s ON s.id = oi.service_id AND s.is_deleted = 0
            LEFT JOIN service_categories cat ON cat.id = s.category_id AND cat.is_deleted = 0
            WHERE oi.is_deleted = 0
              AND o.status <> 'Canceled'
              AND o.order_date >= @FromUtc
              AND o.order_date <= @ToUtc
            GROUP BY COALESCE(cat.name, s.service_type)
            ORDER BY TotalAmount DESC;
            """;

        var rows = await Connection.QueryAsync<SalesRow>(
            new CommandDefinition(sql, new { FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: cancellationToken));

        return rows.Select(row => new SalesReportItemDto(
            row.ServiceType,
            row.OrderCount,
            row.Quantity,
            row.TotalAmount)).ToArray();
    }

    private IDbConnection Connection => _dbContext.Database.GetDbConnection();

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class FinancialRow
    {
        public decimal TotalSales { get; set; }

        public decimal TotalReceived { get; set; }

        public decimal TotalDebt { get; set; }

        public int InvoiceCount { get; set; }
    }

    private sealed class TrendRow
    {
        public string Month { get; set; } = string.Empty;

        public decimal Sales { get; set; }

        public decimal Received { get; set; }
    }

    private sealed class DebtorRow
    {
        public Guid CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string? Mobile { get; set; }

        public decimal OutstandingAmount { get; set; }

        public int InvoiceCount { get; set; }
    }

    private sealed class BadDebtRow
    {
        public Guid CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string? Mobile { get; set; }

        public decimal OutstandingAmount { get; set; }

        public int OverdueInvoiceCount { get; set; }

        public int MaxOverdueDays { get; set; }
    }

    private sealed class CommissionRow
    {
        public decimal PaidAmount { get; set; }

        public decimal UnpaidAmount { get; set; }

        public int PaidCount { get; set; }

        public int UnpaidCount { get; set; }
    }

    private sealed class LedgerRow
    {
        public DateTime EntryDate { get; set; }

        public string EntryType { get; set; } = string.Empty;

        public string? Reference { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }
    }

    private sealed class SalesRow
    {
        public string ServiceType { get; set; } = string.Empty;

        public int OrderCount { get; set; }

        public decimal Quantity { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
