using System.Text.Json;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Forms.Application.Abstractions;

namespace CrmSuite.Modules.Catalog.Application.Services;

public sealed record AddonSelection(Guid AddonId, int Quantity);

public sealed record ServicePricingRequest(
    Guid ServiceId,
    int Quantity,
    IReadOnlyCollection<AddonSelection> Addons,
    string? AttributesJson);

public sealed record ServicePricingResult(
    PricePreviewDto Preview,
    decimal UnitPriceSnapshot,
    decimal AddonsTotal,
    string? AddonsJson);

public interface IServicePricingService
{
    Task<Result<ServicePricingResult>> CalculateAsync(
        ServicePricingRequest request,
        CancellationToken cancellationToken);
}

public sealed class ServicePricingService : IServicePricingService
{
    private readonly IServiceRepository _services;
    private readonly IAddonRepository _addons;
    private readonly IFormSchemaValidator _schemaValidator;

    public ServicePricingService(
        IServiceRepository services,
        IAddonRepository addons,
        IFormSchemaValidator schemaValidator)
    {
        _services = services;
        _addons = addons;
        _schemaValidator = schemaValidator;
    }

    public async Task<Result<ServicePricingResult>> CalculateAsync(
        ServicePricingRequest request,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdWithPricesAsync(
            request.ServiceId,
            cancellationToken);
        if (service is null || !service.IsActive)
        {
            return Result.Failure<ServicePricingResult>(
                "سرویس یافت نشد یا فعال نیست.",
                "CATALOG_SERVICE_NOT_FOUND");
        }

        var quantity = request.Quantity;
        if (service.PricingMode == PricingMode.UnitBased && quantity <= 0)
        {
            return Result.Failure<ServicePricingResult>(
                "برای قیمت‌گذاری واحدی، تعداد باید بزرگ‌تر از صفر باشد.",
                "CATALOG_QUANTITY_REQUIRED");
        }

        if (quantity <= 0)
        {
            quantity = 1;
        }

        if (!string.IsNullOrWhiteSpace(service.AttributeSchemaJson))
        {
            var attributes = string.IsNullOrWhiteSpace(request.AttributesJson)
                ? "{}"
                : request.AttributesJson;
            var errors = _schemaValidator.Validate(service.AttributeSchemaJson, attributes);
            if (errors.Count > 0)
            {
                return Result.Failure<ServicePricingResult>(
                    errors,
                    "مقادیر فیلدهای سرویس با AttributeSchema مطابقت ندارد.");
            }
        }

        var basePrice = service.CurrentOpenPrice?.Price ?? 0;
        if (service.PricingMode != PricingMode.Additive && basePrice <= 0)
        {
            return Result.Failure<ServicePricingResult>(
                "برای این سرویس قیمت معتبری ثبت نشده است.",
                "CATALOG_EFFECTIVE_PRICE_NOT_FOUND");
        }

        var selectedAddons = new List<PricePreviewAddonDto>();
        var addonsTotal = 0m;
        if (request.Addons.Count > 0)
        {
            var ids = request.Addons.Select(item => item.AddonId).Distinct().ToArray();
            var addonEntities = await _addons.GetByIdsAsync(ids, cancellationToken);
            var map = addonEntities.ToDictionary(addon => addon.Id);

            foreach (var selection in request.Addons)
            {
                if (!map.TryGetValue(selection.AddonId, out var addon))
                {
                    return Result.Failure<ServicePricingResult>(
                        "آیتم جانبی انتخاب‌شده یافت نشد.",
                        "CATALOG_ADDON_NOT_FOUND");
                }

                if (!addon.IsActive)
                {
                    return Result.Failure<ServicePricingResult>(
                        $"آیتم جانبی «{addon.Name}» فعال نیست.",
                        "CATALOG_ADDON_INACTIVE");
                }

                if (addon.ServiceId.HasValue && addon.ServiceId.Value != service.Id)
                {
                    return Result.Failure<ServicePricingResult>(
                        $"آیتم جانبی «{addon.Name}» به این سرویس تعلق ندارد.",
                        "CATALOG_ADDON_SERVICE_MISMATCH");
                }

                var addonQuantity = selection.Quantity <= 0 ? 1 : selection.Quantity;
                var total = addon.Price * addonQuantity;
                addonsTotal += total;
                selectedAddons.Add(new PricePreviewAddonDto(
                    addon.Id,
                    addon.Name,
                    addon.Price,
                    addonQuantity,
                    total));
            }
        }

        var unitPrice = service.PricingMode == PricingMode.Additive ? 0 : basePrice;
        var totalPrice = unitPrice * quantity + addonsTotal;
        var preview = new PricePreviewDto(
            service.Id,
            service.Name,
            service.PricingMode.ToString(),
            quantity,
            unitPrice,
            addonsTotal,
            totalPrice,
            null,
            totalPrice,
            selectedAddons);

        var addonsJson = selectedAddons.Count == 0
            ? null
            : JsonSerializer.Serialize(selectedAddons.Select(addon => new
            {
                addon.AddonId,
                addon.Name,
                addon.UnitPrice,
                addon.Quantity,
                addon.Total,
            }));

        return Result.Success(new ServicePricingResult(
            preview,
            unitPrice,
            addonsTotal,
            addonsJson));
    }
}
