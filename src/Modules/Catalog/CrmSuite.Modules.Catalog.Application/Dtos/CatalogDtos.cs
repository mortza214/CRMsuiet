namespace CrmSuite.Modules.Catalog.Application.Dtos;

public sealed record ServiceCategoryDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record ServicePriceDto(
    Guid Id,
    decimal Price,
    DateTime EffectiveFrom,
    string EffectiveFromJalali,
    DateTime? EffectiveTo,
    string? EffectiveToJalali,
    string Currency = "IRR");

public sealed record ServiceFormLinkDto(
    Guid Id,
    Guid FormDefinitionId,
    string? FormCode,
    string? FormTitle,
    bool IsRequired,
    int SortOrder);

public sealed record ServiceDto(
    Guid Id,
    string Code,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string ServiceType,
    string Unit,
    string? Description,
    bool IsActive,
    decimal? CurrentPrice,
    DateTime? CurrentPriceEffectiveFrom,
    string? CurrentPriceEffectiveFromJalali,
    IReadOnlyList<ServicePriceDto> Prices,
    DateTime CreatedAt,
    string CreatedAtJalali,
    string PricingMode = "UnitBased",
    string BillingPeriod = "Monthly",
    decimal? CommissionPercent = null,
    string? AttributeSchemaJson = null,
    Guid? WorkflowDefinitionId = null,
    IReadOnlyList<ServiceAddonDto>? Addons = null,
    IReadOnlyList<ServiceFormLinkDto>? Forms = null,
    bool CanEdit = true,
    string? EditBlockedReason = null);

public sealed record ServiceListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? CategoryName,
    string ServiceType,
    string Unit,
    bool IsActive,
    decimal? CurrentPrice,
    DateTime? PriceEffectiveFrom,
    string? PriceEffectiveFromJalali,
    DateTime CreatedAt,
    string CreatedAtJalali,
    string PricingMode = "UnitBased",
    Guid? CategoryId = null);

public sealed record ServiceAddonDto(
    Guid Id,
    string Name,
    decimal Price,
    string Unit,
    bool IsOptional,
    DateTime CreatedAt,
    string CreatedAtJalali,
    Guid? ServiceId = null,
    string? Code = null,
    string PricingMode = "Fixed",
    bool IsActive = true,
    int SortOrder = 0,
    string? Description = null);

public sealed record DataCenterItemDto(
    Guid Id,
    string ItemType,
    string Name,
    decimal Price,
    string Unit,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record PricePreviewAddonDto(
    Guid AddonId,
    string Name,
    decimal UnitPrice,
    int Quantity,
    decimal Total);

public sealed record PricePreviewDto(
    Guid ServiceId,
    string ServiceName,
    string PricingMode,
    decimal Quantity,
    decimal UnitPrice,
    decimal AddonsTotal,
    decimal Total,
    decimal? OverridePrice,
    decimal EffectivePrice,
    IReadOnlyList<PricePreviewAddonDto> Addons);
