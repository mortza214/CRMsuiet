using CrmSuite.Modules.Catalog.Domain.Enums;

namespace CrmSuite.Modules.Catalog.Application.Queries;

public sealed record ServiceSearchFilter(
    string? Search,
    Guid? CategoryId,
    ServiceType? ServiceType,
    bool? IsActive,
    int Page,
    int PageSize,
    PricingMode? PricingMode = null);
