using System.Data;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Application.Queries;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence.Repositories;

public sealed class InvoiceReadRepository : IInvoiceReadRepository
{
    private const string FilterClause = """
        WHERE i.is_deleted = 0
          AND (@Status IS NULL OR i.status = @Status)
          AND (@CustomerId IS NULL OR i.customer_id = @CustomerId)
          AND (@FromUtc IS NULL OR i.issue_date >= @FromUtc)
          AND (@ToUtc IS NULL OR i.issue_date <= @ToUtc)
          AND (@Search IS NULL
               OR i.invoice_number LIKE @Like
               OR o.order_number LIKE @Like
               OR c.first_name LIKE @Like
               OR c.last_name LIKE @Like
               OR c.company_name LIKE @Like)
        """;

    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public InvoiceReadRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<PagedResult<InvoiceListItemDto>> SearchAsync(
        InvoiceSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Status", filter.Status?.ToString());
        parameters.Add("@CustomerId", filter.CustomerId);
        parameters.Add("@FromUtc", filter.FromUtc);
        parameters.Add("@ToUtc", filter.ToUtc);
        parameters.Add("@Search", string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim());
        parameters.Add("@Like", $"%{filter.Search?.Trim()}%");
        parameters.Add("@PageSize", filter.PageSize);
        parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);

        var countSql = $"""
            SELECT COUNT(*)
            FROM invoices i
            LEFT JOIN customers c ON c.id = i.customer_id AND c.is_deleted = 0
            LEFT JOIN orders o ON o.id = i.order_id AND o.is_deleted = 0
            {FilterClause};
            """;
        var dataSql = $"""
            SELECT
                i.id AS Id,
                i.invoice_number AS InvoiceNumber,
                i.order_id AS OrderId,
                o.order_number AS OrderNumber,
                i.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                i.type AS Type,
                i.issue_date AS IssueDate,
                i.due_date AS DueDate,
                i.total_amount AS TotalAmount,
                i.paid_amount AS PaidAmount,
                i.status AS Status
            FROM invoices i
            LEFT JOIN customers c ON c.id = i.customer_id AND c.is_deleted = 0
            LEFT JOIN orders o ON o.id = i.order_id AND o.is_deleted = 0
            {FilterClause}
            ORDER BY i.issue_date DESC
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

            var rows = await connection.QueryAsync<InvoiceRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

            return new PagedResult<InvoiceListItemDto>(
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

    public async Task<IReadOnlyList<InvoiceListItemDto>> GetCustomerInvoicesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                i.id AS Id,
                i.invoice_number AS InvoiceNumber,
                i.order_id AS OrderId,
                o.order_number AS OrderNumber,
                i.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                i.type AS Type,
                i.issue_date AS IssueDate,
                i.due_date AS DueDate,
                i.total_amount AS TotalAmount,
                i.paid_amount AS PaidAmount,
                i.status AS Status
            FROM invoices i
            LEFT JOIN customers c ON c.id = i.customer_id AND c.is_deleted = 0
            LEFT JOIN orders o ON o.id = i.order_id AND o.is_deleted = 0
            WHERE i.is_deleted = 0 AND i.customer_id = @CustomerId
            ORDER BY i.issue_date DESC;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var rows = await connection.QueryAsync<InvoiceRow>(
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

    private InvoiceListItemDto Map(InvoiceRow row)
    {
        var issueDate = AsUtc(row.IssueDate);
        var dueDate = AsUtc(row.DueDate);

        return new InvoiceListItemDto(
            row.Id,
            row.InvoiceNumber,
            row.OrderId,
            row.OrderNumber,
            row.CustomerId,
            row.CustomerName?.Trim(),
            row.Type,
            issueDate,
            _jalaliDate.ToJalali(issueDate),
            dueDate,
            _jalaliDate.ToJalali(dueDate),
            row.TotalAmount,
            row.PaidAmount,
            row.Status,
        row.RequiresPayment,
        row.PaymentMode);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class InvoiceRow
    {
        public Guid Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public Guid OrderId { get; set; }

        public string? OrderNumber { get; set; }

        public Guid CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string Type { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }

        public DateTime DueDate { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool RequiresPayment { get; set; } = true;

        public string? PaymentMode { get; set; }
    }
}
