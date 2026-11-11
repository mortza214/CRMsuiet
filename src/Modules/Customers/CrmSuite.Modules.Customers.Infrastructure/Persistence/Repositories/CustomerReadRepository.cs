using System.Data;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Queries;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Repositories;

public sealed class CustomerReadRepository : ICustomerReadRepository
{
    private const string FilterClause = """
        WHERE c.is_deleted = 0
          AND (@Search IS NULL
               OR c.first_name LIKE @Like
               OR c.last_name LIKE @Like
               OR c.company_name LIKE @Like
               OR c.mobile LIKE @Like
               OR c.national_id LIKE @Like)
          AND (@GroupId IS NULL OR c.group_id = @GroupId)
          AND (@IsReseller IS NULL OR c.is_reseller = @IsReseller)
          AND (@TagId IS NULL OR EXISTS (
              SELECT 1 FROM customer_tag_maps m
              WHERE m.customer_id = c.id AND m.tag_id = @TagId))
          AND (@ParentCustomerId IS NULL OR c.parent_customer_id = @ParentCustomerId)
          AND (@OnlyDebtors = 0 OR EXISTS (
              SELECT 1 FROM invoices i
              WHERE i.customer_id = c.id
                AND i.is_deleted = 0
                AND i.status IN ('Issued', 'PartiallyPaid')
                AND i.total_amount > i.paid_amount))
        """;

    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public CustomerReadRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<PagedResult<CustomerListItemDto>> SearchAsync(
        CustomerSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Search", string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim());
        parameters.Add("@Like", $"%{filter.Search?.Trim()}%");
        parameters.Add("@GroupId", filter.GroupId);
        parameters.Add("@TagId", filter.TagId);
        parameters.Add("@IsReseller", filter.IsReseller);
        parameters.Add("@OnlyDebtors", filter.OnlyDebtors ? 1 : 0);
        parameters.Add("@ParentCustomerId", filter.ParentCustomerId);
        parameters.Add("@PageSize", filter.PageSize);
        parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);

        var countSql = $"SELECT COUNT(*) FROM customers c {FilterClause};";

        var dataSql = $"""
            SELECT
                c.id AS Id,
                c.type AS Type,
                CASE
                    WHEN c.type = 'Legal' THEN COALESCE(c.company_name, c.national_id)
                    ELSE CONCAT(COALESCE(c.first_name, ''), ' ', COALESCE(c.last_name, ''))
                END AS DisplayName,
                c.first_name AS FirstName,
                c.last_name AS LastName,
                c.company_name AS CompanyName,
                c.mobile AS Mobile,
                c.national_id AS NationalId,
                g.name AS GroupName,
                c.is_reseller AS IsReseller,
                c.parent_customer_id AS ParentCustomerId,
                c.owner_user_id AS OwnerUserId,
                u.full_name AS OwnerName,
                c.created_at AS CreatedAt,
                (
                    SELECT GROUP_CONCAT(
                        CONCAT(t.id, '~', t.name, '~', t.color)
                        ORDER BY t.name SEPARATOR '||')
                    FROM customer_tag_maps m
                    INNER JOIN customer_tags t ON t.id = m.tag_id AND t.is_deleted = 0
                    WHERE m.customer_id = c.id
                ) AS Tags
            FROM customers c
            LEFT JOIN customer_groups g ON g.id = c.group_id AND g.is_deleted = 0
            LEFT JOIN users u ON u.id = c.owner_user_id AND u.is_deleted = 0
            {FilterClause}
            ORDER BY c.created_at DESC
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

            var rows = await connection.QueryAsync<CustomerRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

            var items = rows.Select(row =>
            {
                var createdAt = row.CreatedAt.Kind == DateTimeKind.Utc
                    ? row.CreatedAt
                    : DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc);

                return new CustomerListItemDto(
                    row.Id,
                    row.Type,
                    row.DisplayName.Trim(),
                    row.FirstName,
                    row.LastName,
                    row.CompanyName,
                    row.Mobile,
                    row.NationalId,
                    row.GroupName,
                    row.IsReseller,
                    row.ParentCustomerId,
                    row.OwnerUserId,
                    row.OwnerName,
                    ParseTags(row.Tags),
                    createdAt,
                    _jalaliDate.ToJalali(createdAt));
            }).ToArray();

            return new PagedResult<CustomerListItemDto>(
                items,
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

    private static IReadOnlyList<CustomerTagSummaryDto> ParseTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var tags = new List<CustomerTagSummaryDto>();

        foreach (var part in raw.Split("||", StringSplitOptions.RemoveEmptyEntries))
        {
            var values = part.Split('~');
            if (values.Length != 3 || !Guid.TryParse(values[0], out var id))
            {
                continue;
            }

            tags.Add(new CustomerTagSummaryDto(id, values[1], values[2]));
        }

        return tags;
    }

    private sealed class CustomerRow
    {
        public Guid Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? CompanyName { get; set; }

        public string Mobile { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;

        public string? GroupName { get; set; }

        public bool IsReseller { get; set; }

        public Guid? ParentCustomerId { get; set; }

        public Guid? OwnerUserId { get; set; }

        public string? OwnerName { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Tags { get; set; }
    }
}
