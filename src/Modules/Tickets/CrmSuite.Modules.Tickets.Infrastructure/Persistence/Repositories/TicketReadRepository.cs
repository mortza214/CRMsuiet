using System.Data;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Dtos;
using CrmSuite.Modules.Tickets.Application.Queries;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence.Repositories;

public sealed class TicketReadRepository : ITicketReadRepository
{
    private const string FilterClause = """
        WHERE t.is_deleted = 0
          AND (@Status IS NULL OR t.status = @Status)
          AND (@Priority IS NULL OR t.priority = @Priority)
          AND (@DepartmentId IS NULL OR t.department_id = @DepartmentId)
          AND (@CustomerId IS NULL OR t.customer_id = @CustomerId)
          AND (@Search IS NULL
               OR t.ticket_number LIKE @Like
               OR t.subject LIKE @Like
               OR c.first_name LIKE @Like
               OR c.last_name LIKE @Like
               OR c.company_name LIKE @Like)
        """;

    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public TicketReadRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<PagedResult<TicketListItemDto>> SearchAsync(
        TicketSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Status", filter.Status?.ToString());
        parameters.Add("@Priority", filter.Priority?.ToString());
        parameters.Add("@DepartmentId", filter.DepartmentId);
        parameters.Add("@CustomerId", filter.CustomerId);
        parameters.Add("@Search", string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim());
        parameters.Add("@Like", $"%{filter.Search?.Trim()}%");
        parameters.Add("@PageSize", filter.PageSize);
        parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);

        var allowedClause = string.Empty;
        if (filter.AllowedDepartmentIds is not null)
        {
            if (filter.AllowedDepartmentIds.Count == 0)
            {
                allowedClause = " AND 1 = 0";
            }
            else
            {
                allowedClause = " AND t.department_id IN @AllowedDepartmentIds";
                parameters.Add("@AllowedDepartmentIds", filter.AllowedDepartmentIds.ToArray());
            }
        }

        var countSql = $"""
            SELECT COUNT(*)
            FROM tickets t
            LEFT JOIN customers c ON c.id = t.customer_id AND c.is_deleted = 0
            {FilterClause}{allowedClause};
            """;
        var dataSql = $"""
            SELECT
                t.id AS Id,
                t.ticket_number AS TicketNumber,
                t.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                t.department_id AS DepartmentId,
                d.name AS DepartmentName,
                t.assigned_to_user_id AS AssignedToUserId,
                u.full_name AS AssignedToName,
                t.subject AS Subject,
                t.priority AS Priority,
                t.status AS Status,
                t.source AS Source,
                (SELECT COUNT(*) FROM ticket_messages m WHERE m.ticket_id = t.id) AS MessageCount,
                t.created_at AS CreatedAt
            FROM tickets t
            LEFT JOIN customers c ON c.id = t.customer_id AND c.is_deleted = 0
            LEFT JOIN departments d ON d.id = t.department_id AND d.is_deleted = 0
            LEFT JOIN users u ON u.id = t.assigned_to_user_id AND u.is_deleted = 0
            {FilterClause}{allowedClause}
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

            var rows = await connection.QueryAsync<TicketRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

            return new PagedResult<TicketListItemDto>(
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

    public async Task<IReadOnlyList<TicketListItemDto>> GetCustomerTicketsAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                t.id AS Id,
                t.ticket_number AS TicketNumber,
                t.customer_id AS CustomerId,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS CustomerName,
                t.department_id AS DepartmentId,
                d.name AS DepartmentName,
                t.assigned_to_user_id AS AssignedToUserId,
                u.full_name AS AssignedToName,
                t.subject AS Subject,
                t.priority AS Priority,
                t.status AS Status,
                t.source AS Source,
                (SELECT COUNT(*) FROM ticket_messages m WHERE m.ticket_id = t.id) AS MessageCount,
                t.created_at AS CreatedAt
            FROM tickets t
            LEFT JOIN customers c ON c.id = t.customer_id AND c.is_deleted = 0
            LEFT JOIN departments d ON d.id = t.department_id AND d.is_deleted = 0
            LEFT JOIN users u ON u.id = t.assigned_to_user_id AND u.is_deleted = 0
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
            var rows = await connection.QueryAsync<TicketRow>(
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

    private TicketListItemDto Map(TicketRow row)
    {
        var createdAt = AsUtc(row.CreatedAt);

        return new TicketListItemDto(
            row.Id,
            row.TicketNumber,
            row.CustomerId,
            row.CustomerName?.Trim(),
            row.DepartmentId,
            row.DepartmentName,
            row.AssignedToUserId,
            row.AssignedToName,
            row.Subject,
            row.Priority,
            row.Status,
            row.Source,
            row.MessageCount,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class TicketRow
    {
        public Guid Id { get; set; }

        public string TicketNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public Guid DepartmentId { get; set; }

        public string? DepartmentName { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToName { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public int MessageCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
