using CrmSuite.Modules.Catalog.Domain.Enums;

namespace CrmSuite.Modules.Catalog.Api.Contracts;

public sealed class CreateServiceRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public ServiceType ServiceType { get; init; }

    public ServiceUnit Unit { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public decimal? InitialPrice { get; init; }

    public DateTime? PriceEffectiveFromUtc { get; init; }

    public PricingMode? PricingMode { get; init; }

    public BillingPeriod? BillingPeriod { get; init; }

    public decimal? CommissionPercent { get; init; }

    public string? AttributeSchemaJson { get; init; }

    public Guid? WorkflowDefinitionId { get; init; }
}

public sealed class UpdateServiceRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public ServiceType ServiceType { get; init; }

    public ServiceUnit Unit { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public PricingMode? PricingMode { get; init; }

    public BillingPeriod? BillingPeriod { get; init; }

    public decimal? CommissionPercent { get; init; }

    public string? AttributeSchemaJson { get; init; }

    public Guid? WorkflowDefinitionId { get; init; }
}

public sealed class SetServicePriceRequest
{
    public decimal Price { get; init; }

    public DateTime? EffectiveFromUtc { get; init; }

    public string Currency { get; init; } = "IRR";
}

public sealed class SetAttributeSchemaRequest
{
    public string? AttributeSchemaJson { get; init; }
}

public sealed class LinkFormRequest
{
    public Guid FormDefinitionId { get; init; }

    public bool IsRequired { get; init; }

    public int SortOrder { get; init; }
}

public sealed class CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;

    public Guid? ParentId { get; init; }
}

public sealed class UpdateCategoryRequest
{
    public string Name { get; init; } = string.Empty;

    public Guid? ParentId { get; init; }
}

public sealed class CreateAddonRequest
{
    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public ServiceUnit Unit { get; init; }

    public bool IsOptional { get; init; }

    public Guid? ServiceId { get; init; }

    public string? Code { get; init; }

    public AddonPricingMode? PricingMode { get; init; }

    public int SortOrder { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateAddonRequest
{
    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public ServiceUnit Unit { get; init; }

    public bool IsOptional { get; init; }

    public Guid? ServiceId { get; init; }

    public string? Code { get; init; }

    public AddonPricingMode? PricingMode { get; init; }

    public int SortOrder { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class AddonSelectionRequest
{
    public Guid AddonId { get; init; }

    public int Quantity { get; init; }
}

public sealed class PricePreviewRequest
{
    public Guid ServiceId { get; init; }

    public int Quantity { get; init; }

    public IReadOnlyCollection<AddonSelectionRequest> Addons { get; init; } = [];

    public string? AttributesJson { get; init; }
}

public sealed class CreateDataCenterItemRequest
{
    public DataCenterItemType ItemType { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public ServiceUnit Unit { get; init; }
}

public sealed class UpdateDataCenterItemRequest
{
    public DataCenterItemType ItemType { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public ServiceUnit Unit { get; init; }
}
