using System.Data;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderReadRepository : IOrderReadRepository
{
    private const string FilterClause = """
        WHERE o.is_deleted = 0
          AND (@Status IS NULL OR o.status = @Status)
          AND (@CustomerId IS NULL OR o.customer_id = @CustomerId)
          AND (@SalesExpertId IS NULL OR o.sales_expert_id = @SalesExpertId)
          AND (@FromUtc IS NULL OR o.order_date >= @FromUtc)
          AND (@ToUtc IS NULL OR o.order_date <= @ToUtc)
          AND (@Search IS NULL
               OR o.order_number LIKE @Like
               OR c.first_name LIKE @Like
               OR c.last_name LIKE @Like
               OR c.company_name LIKE @Like)
        """;

    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public OrderReadRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<PagedResult<OrderListItemDto>> SearchAsync(
        OrderSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Status", filter.Status?.ToString());
        parameters.Add("@CustomerId", filter.CustomerId);
        parameters.Add("@SalesExpertId", filter.SalesExpertId);
        parameters.Add("@FromUtc", filter.FromUtc);
        parameters.Add("@ToUtc", filter.ToUtc);
        parameters.Add("@Search", string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim());
        parameters.Add("@Like", $"%{filter.Search?.Trim()}%");
        parameters.Add("@PageSize", filter.PageSize);
        parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);

        var countSql = $"""
            SELECT COUNT(*)
            FROM orders o
            LEFT JOIN customers c ON c.id = o.customer_id AND c.is_deleted = 0
            {FilterClause};
            """;
        var dataSql = $"""
            SELECT
                o.id AS Id,
                o.order_number AS OrderNumber,
                o.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                o.sales_expert_id AS SalesExpertId,
                o.order_date AS OrderDate,
                o.status AS Status,
                o.order_type AS OrderType,
                o.total_amount AS TotalAmount,
                o.discount_amount AS DiscountAmount,
                o.payable_amount AS PayableAmount,
                (SELECT COUNT(*) FROM order_items i WHERE i.order_id = o.id) AS ItemCount
            FROM orders o
            LEFT JOIN customers c ON c.id = o.customer_id AND c.is_deleted = 0
            {FilterClause}
            ORDER BY o.order_date DESC
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

            var rows = await connection.QueryAsync<OrderRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

            return new PagedResult<OrderListItemDto>(
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

    public async Task<IReadOnlyList<OrderListItemDto>> GetCustomerOrdersAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                o.id AS Id,
                o.order_number AS OrderNumber,
                o.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                o.sales_expert_id AS SalesExpertId,
                o.order_date AS OrderDate,
                o.status AS Status,
                o.order_type AS OrderType,
                o.total_amount AS TotalAmount,
                o.discount_amount AS DiscountAmount,
                o.payable_amount AS PayableAmount,
                (SELECT COUNT(*) FROM order_items i WHERE i.order_id = o.id) AS ItemCount
            FROM orders o
            LEFT JOIN customers c ON c.id = o.customer_id AND c.is_deleted = 0
            WHERE o.is_deleted = 0 AND o.customer_id = @CustomerId
            ORDER BY o.order_date DESC;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var rows = await connection.QueryAsync<OrderRow>(
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

    private OrderListItemDto Map(OrderRow row)
    {
        var orderDate = AsUtc(row.OrderDate);

        return new OrderListItemDto(
            row.Id,
            row.OrderNumber,
            row.CustomerId,
            row.CustomerName?.Trim(),
            row.SalesExpertId,
            orderDate,
            _jalaliDate.ToJalali(orderDate),
            row.Status,
            row.OrderType,
            row.TotalAmount,
            row.DiscountAmount,
            row.PayableAmount,
            row.ItemCount);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class OrderRow
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public Guid SalesExpertId { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string OrderType { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal PayableAmount { get; set; }

        public int ItemCount { get; set; }
    }
}
