using System.Data;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Dtos;
using CrmSuite.Modules.Wallet.Application.Queries;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Repositories;

public sealed class WalletReadRepository : IWalletReadRepository
{
    private const string CommissionFilter = """
        WHERE ce.is_deleted = 0
          AND (@Status IS NULL OR ce.status = @Status)
          AND (@EmployeeId IS NULL OR ce.employee_id = @EmployeeId)
          AND (@FromUtc IS NULL OR ce.created_at >= @FromUtc)
          AND (@ToUtc IS NULL OR ce.created_at <= @ToUtc)
        """;

    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public WalletReadRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsAsync(
        Guid walletId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                wt.id AS Id,
                wt.wallet_id AS WalletId,
                wt.amount AS Amount,
                wt.transaction_type AS TransactionType,
                wt.reference_id AS ReferenceId,
                wt.description AS Description,
                wt.balance_after AS BalanceAfter,
                wt.created_at AS CreatedAt
            FROM wallet_transactions wt
            WHERE wt.is_deleted = 0 AND wt.wallet_id = @WalletId
            ORDER BY wt.created_at DESC;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var rows = await connection.QueryAsync<WalletTransactionRow>(
                new CommandDefinition(sql, new { WalletId = walletId }, cancellationToken: cancellationToken));

            return rows.Select(row =>
            {
                var createdAt = AsUtc(row.CreatedAt);
                return new WalletTransactionDto(
                    row.Id,
                    row.WalletId,
                    row.Amount,
                    row.TransactionType,
                    row.ReferenceId,
                    row.Description,
                    row.BalanceAfter,
                    createdAt,
                    _jalaliDate.ToJalali(createdAt));
            }).ToArray();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task<PagedResult<CommissionEntryDto>> GetCommissionsAsync(
        CommissionSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Status", filter.Status?.ToString());
        parameters.Add("@EmployeeId", filter.EmployeeId);
        parameters.Add("@FromUtc", filter.FromUtc);
        parameters.Add("@ToUtc", filter.ToUtc);
        parameters.Add("@PageSize", filter.PageSize);
        parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);

        var countSql = $"SELECT COUNT(*) FROM commission_entries ce {CommissionFilter};";
        var dataSql = $"""
            SELECT
                ce.id AS Id,
                ce.order_id AS OrderId,
                o.order_number AS OrderNumber,
                ce.order_item_id AS OrderItemId,
                ce.employee_id AS EmployeeId,
                u.full_name AS EmployeeName,
                ce.amount AS Amount,
                ce.adjustment_amount AS AdjustmentAmount,
                (ce.amount - ce.adjustment_amount) AS NetAmount,
                ce.status AS Status,
                ce.payout_type AS PayoutType,
                (SELECT COUNT(*) FROM commission_schedules cs WHERE cs.commission_entry_id = ce.id) AS ScheduleCount,
                ce.customer_name AS CustomerName,
                ce.service_name AS ServiceName,
                ce.paid_installments AS PaidInstallments,
                ce.created_at AS CreatedAt
            FROM commission_entries ce
            LEFT JOIN orders o ON o.id = ce.order_id AND o.is_deleted = 0
            LEFT JOIN users u ON u.id = ce.employee_id AND u.is_deleted = 0
            {CommissionFilter}
            ORDER BY ce.created_at DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var totalCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

            var rows = await connection.QueryAsync<CommissionRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

            return new PagedResult<CommissionEntryDto>(
                rows.Select(Map).ToArray(),
                totalCount,
                filter.Page,
                filter.PageSize);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private CommissionEntryDto Map(CommissionRow row)
    {
        var createdAt = AsUtc(row.CreatedAt);

        return new CommissionEntryDto(
            row.Id,
            row.OrderId,
            row.OrderNumber,
            row.OrderItemId,
            row.EmployeeId,
            row.EmployeeName,
            row.Amount,
            row.AdjustmentAmount,
            row.NetAmount,
            row.Status,
            row.PayoutType,
            row.ScheduleCount,
            [],
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            row.CustomerName,
            row.ServiceName,
            row.PaidInstallments,
            []);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class WalletTransactionRow
    {
        public Guid Id { get; set; }

        public Guid WalletId { get; set; }

        public decimal Amount { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public Guid? ReferenceId { get; set; }

        public string? Description { get; set; }

        public decimal BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    private sealed class CommissionRow
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string? OrderNumber { get; set; }

        public Guid OrderItemId { get; set; }

        public Guid EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public decimal Amount { get; set; }

        public decimal AdjustmentAmount { get; set; }

        public decimal NetAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PayoutType { get; set; } = string.Empty;

        public int ScheduleCount { get; set; }

        public string? CustomerName { get; set; }

        public string? ServiceName { get; set; }

        public int PaidInstallments { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
