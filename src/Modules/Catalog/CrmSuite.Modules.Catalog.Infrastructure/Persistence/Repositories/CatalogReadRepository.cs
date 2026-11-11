using System.Data;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class CatalogReadRepository : ICatalogReadRepository
{
    private const string FilterClause = """
        WHERE s.is_deleted = 0
          AND (@Search IS NULL
               OR s.code LIKE @Like
               OR s.name LIKE @Like)
          AND (@CategoryId IS NULL OR s.category_id = @CategoryId)
          AND (@ServiceType IS NULL OR s.service_type = @ServiceType)
          AND (@IsActive IS NULL OR s.is_active = @IsActive)
          AND (@PricingMode IS NULL OR s.pricing_mode = @PricingMode)
        """;

    private readonly AppDbContext _dbContext;
    private readonly IJalaliDateService _jalaliDate;

    public CatalogReadRepository(AppDbContext dbContext, IJalaliDateService jalaliDate)
    {
        _dbContext = dbContext;
        _jalaliDate = jalaliDate;
    }

    public async Task<PagedResult<ServiceListItemDto>> SearchServicesAsync(
        ServiceSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Search", string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim());
        parameters.Add("@Like", $"%{filter.Search?.Trim()}%");
        parameters.Add("@CategoryId", filter.CategoryId);
        parameters.Add("@ServiceType", filter.ServiceType?.ToString());
        parameters.Add("@IsActive", filter.IsActive);
        parameters.Add("@PricingMode", filter.PricingMode?.ToString());
        parameters.Add("@PageSize", filter.PageSize);
        parameters.Add("@Offset", (filter.Page - 1) * filter.PageSize);

        var countSql = $"SELECT COUNT(*) FROM services s {FilterClause};";

        var dataSql = $"""
            SELECT
                s.id AS Id,
                s.code AS Code,
                s.name AS Name,
                s.category_id AS CategoryId,
                c.name AS CategoryName,
                s.service_type AS ServiceType,
                s.pricing_mode AS PricingMode,
                s.unit AS Unit,
                s.is_active AS IsActive,
                (
                    SELECT p.price
                    FROM service_prices p
                    WHERE p.service_id = s.id
                      AND p.effective_from <= UTC_TIMESTAMP(6)
                      AND (p.effective_to IS NULL OR p.effective_to > UTC_TIMESTAMP(6))
                    ORDER BY p.effective_from DESC
                    LIMIT 1
                ) AS CurrentPrice,
                (
                    SELECT p.effective_from
                    FROM service_prices p
                    WHERE p.service_id = s.id
                      AND p.effective_from <= UTC_TIMESTAMP(6)
                      AND (p.effective_to IS NULL OR p.effective_to > UTC_TIMESTAMP(6))
                    ORDER BY p.effective_from DESC
                    LIMIT 1
                ) AS PriceEffectiveFrom,
                s.created_at AS CreatedAt
            FROM services s
            LEFT JOIN service_categories c ON c.id = s.category_id AND c.is_deleted = 0
            {FilterClause}
            ORDER BY s.created_at DESC
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

            var rows = await connection.QueryAsync<ServiceRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

            var items = rows.Select(row =>
            {
                var createdAt = AsUtc(row.CreatedAt);
                var priceFrom = row.PriceEffectiveFrom.HasValue
                    ? AsUtc(row.PriceEffectiveFrom.Value)
                    : (DateTime?)null;

                return new ServiceListItemDto(
                    row.Id,
                    row.Code,
                    row.Name,
                    row.CategoryName,
                    row.ServiceType,
                    row.Unit,
                    row.IsActive,
                    row.CurrentPrice,
                    priceFrom,
                    priceFrom.HasValue ? _jalaliDate.ToJalali(priceFrom.Value) : null,
                    createdAt,
                    _jalaliDate.ToJalali(createdAt),
                    row.PricingMode,
                    row.CategoryId);
            }).ToArray();

            return new PagedResult<ServiceListItemDto>(
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

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed class ServiceRow
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string ServiceType { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public string PricingMode { get; set; } = "UnitBased";

        public bool IsActive { get; set; }

        public decimal? CurrentPrice { get; set; }

        public DateTime? PriceEffectiveFrom { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
