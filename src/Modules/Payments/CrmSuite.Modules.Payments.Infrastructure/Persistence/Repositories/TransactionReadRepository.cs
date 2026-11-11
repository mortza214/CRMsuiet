using System.Data;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Application.Dtos;
using CrmSuite.Modules.Payments.Application.Queries;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Payments.Infrastructure.Persistence.Repositories;

public sealed class TransactionReadRepository : ITransactionReadRepository
{
    private const string FilterClause = """
        WHERE t.is_deleted = 0
          AND (@Status IS NULL OR t.status = @Status)
          AND (@CustomerId IS NULL OR t.customer_id = @CustomerId)
          AND (@InvoiceId IS NULL OR t.invoice_id = @InvoiceId)
          AND (@FromUtc IS NULL OR t.created_at >= @FromUtc)
          AND (@ToUtc IS NULL OR t.created_at <= @ToUtc)
        """;

    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public TransactionReadRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<PagedResult<TransactionListItemDto>> SearchAsync(
        TransactionSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Status", filter.Status?.ToString());
        parameters.Add("@CustomerId", filter.CustomerId);
        parameters.Add("@InvoiceId", filter.InvoiceId);
        parameters.Add("@FromUtc", filter.FromUtc);
        parameters.Add("@ToUtc", filter.ToUtc);
        parameters.Add("@PageSize", filter.PageSize);
        parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);

        var countSql = $"SELECT COUNT(*) FROM transactions t {FilterClause};";
        var dataSql = $"""
            SELECT
                t.id AS Id,
                t.invoice_id AS InvoiceId,
                i.invoice_number AS InvoiceNumber,
                t.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                t.amount AS Amount,
                t.payment_method AS PaymentMethod,
                t.gateway_name AS GatewayName,
                t.reference_number AS ReferenceNumber,
                t.tracking_code AS TrackingCode,
                t.status AS Status,
                t.paid_at AS PaidAt,
                t.created_at AS CreatedAt
            FROM transactions t
            LEFT JOIN invoices i ON i.id = t.invoice_id AND i.is_deleted = 0
            LEFT JOIN customers c ON c.id = t.customer_id AND c.is_deleted = 0
            {FilterClause}
            ORDER BY t.created_at DESC
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

            var rows = await connection.QueryAsync<TransactionRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

            return new PagedResult<TransactionListItemDto>(
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

    public async Task<IReadOnlyList<TransactionListItemDto>> GetCustomerTransactionsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                t.id AS Id,
                t.invoice_id AS InvoiceId,
                i.invoice_number AS InvoiceNumber,
                t.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                t.amount AS Amount,
                t.payment_method AS PaymentMethod,
                t.gateway_name AS GatewayName,
                t.reference_number AS ReferenceNumber,
                t.tracking_code AS TrackingCode,
                t.status AS Status,
                t.paid_at AS PaidAt,
                t.created_at AS CreatedAt
            FROM transactions t
            LEFT JOIN invoices i ON i.id = t.invoice_id AND i.is_deleted = 0
            LEFT JOIN customers c ON c.id = t.customer_id AND c.is_deleted = 0
            WHERE t.is_deleted = 0 AND t.customer_id = @CustomerId
            ORDER BY t.created_at DESC;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var rows = await connection.QueryAsync<TransactionRow>(
                new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: cancellationToken));

            return rows.Select(Map).ToArray();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private TransactionListItemDto Map(TransactionRow row)
    {
        var createdAt = AsUtc(row.CreatedAt);
        DateTime? paidAt = row.PaidAt.HasValue ? AsUtc(row.PaidAt.Value) : null;

        return new TransactionListItemDto(
            row.Id,
            row.InvoiceId,
            row.InvoiceNumber,
            row.CustomerId,
            row.CustomerName?.Trim(),
            row.Amount,
            row.PaymentMethod,
            row.GatewayName,
            row.ReferenceNumber,
            row.TrackingCode,
            row.Status,
            paidAt,
            paidAt.HasValue ? _jalaliDate.ToJalali(paidAt.Value) : null,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class TransactionRow
    {
        public Guid Id { get; set; }

        public Guid? InvoiceId { get; set; }

        public string? InvoiceNumber { get; set; }

        public Guid CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string GatewayName { get; set; } = string.Empty;

        public string? ReferenceNumber { get; set; }

        public string TrackingCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
