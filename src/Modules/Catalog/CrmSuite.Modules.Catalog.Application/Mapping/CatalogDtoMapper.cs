using System.Diagnostics.CodeAnalysis;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;

namespace CrmSuite.Modules.Catalog.Application.Mapping;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Projection methods share the mapper instance for DI consistency.")]
public sealed class CatalogDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public CatalogDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public ServiceDto ToServiceDto(
        Service service,
        IReadOnlyList<ServiceAddon>? addons = null,
        IReadOnlyList<ServiceFormLinkDto>? forms = null)
    {
        var createdAt = AsUtc(service.CreatedAt);
        var current = service.CurrentOpenPrice;

        return new ServiceDto(
            service.Id,
            service.Code,
            service.Name,
            service.CategoryId,
            service.Category?.Name ?? string.Empty,
            service.ServiceType.ToString(),
            service.Unit.ToString(),
            service.Description,
            service.IsActive,
            current?.Price,
            current is null ? null : AsUtc(current.EffectiveFrom),
            current is null ? null : _jalaliDate.ToJalali(AsUtc(current.EffectiveFrom)),
            service.Prices
                .OrderByDescending(price => price.EffectiveFrom)
                .Select(ToPriceDto)
                .ToArray(),
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            service.PricingMode.ToString(),
            service.BillingPeriod.ToString(),
            service.CommissionPercent,
            service.AttributeSchemaJson,
            service.WorkflowDefinitionId,
            addons?.Select(ToAddonDto).ToArray(),
            forms);
    }

    public ServicePriceDto ToPriceDto(ServicePrice price)
    {
        var effectiveFrom = AsUtc(price.EffectiveFrom);
        var effectiveTo = price.EffectiveTo.HasValue ? AsUtc(price.EffectiveTo.Value) : (DateTime?)null;

        return new ServicePriceDto(
            price.Id,
            price.Price,
            effectiveFrom,
            _jalaliDate.ToJalali(effectiveFrom),
            effectiveTo,
            effectiveTo.HasValue ? _jalaliDate.ToJalali(effectiveTo.Value) : null,
            price.Currency);
    }

    public ServiceCategoryDto ToCategoryDto(ServiceCategory category)
    {
        var createdAt = AsUtc(category.CreatedAt);
        return new ServiceCategoryDto(
            category.Id,
            category.Name,
            category.ParentId,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public ServiceAddonDto ToAddonDto(ServiceAddon addon)
    {
        var createdAt = AsUtc(addon.CreatedAt);
        return new ServiceAddonDto(
            addon.Id,
            addon.Name,
            addon.Price,
            addon.Unit.ToString(),
            addon.IsOptional,
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            addon.ServiceId,
            addon.Code,
            addon.PricingMode.ToString(),
            addon.IsActive,
            addon.SortOrder,
            addon.Description);
    }

    public ServiceFormLinkDto ToFormLinkDto(
        ServiceFormLink link,
        string? formCode = null,
        string? formTitle = null) =>
        new(
            link.Id,
            link.FormDefinitionId,
            formCode,
            formTitle,
            link.IsRequired,
            link.SortOrder);

    public DataCenterItemDto ToDataCenterItemDto(DataCenterItem item)
    {
        var createdAt = AsUtc(item.CreatedAt);
        return new DataCenterItemDto(
            item.Id,
            item.ItemType.ToString(),
            item.Name,
            item.Price,
            item.Unit.ToString(),
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
