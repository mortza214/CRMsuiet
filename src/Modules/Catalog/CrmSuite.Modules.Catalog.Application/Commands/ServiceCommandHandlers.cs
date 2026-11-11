using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Forms.Application.Schema;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed class CreateServiceCommandHandler
    : IRequestHandler<CreateServiceCommand, Result<ServiceDto>>
{
    private readonly IServiceRepository _services;
    private readonly ICategoryRepository _categories;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public CreateServiceCommandHandler(
        IServiceRepository services,
        ICategoryRepository categories,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _services = services;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServiceDto>> Handle(
        CreateServiceCommand request,
        CancellationToken cancellationToken)
    {
        if (await _services.CodeExistsAsync(request.Code, null, cancellationToken))
        {
            return Result.Failure<ServiceDto>("کد سرویس قبلاً استفاده شده است.", "CATALOG_SERVICE_CODE_DUPLICATE");
        }

        if (await _categories.GetByIdAsync(request.CategoryId, cancellationToken) is null)
        {
            return Result.Failure<ServiceDto>("دسته‌بندی سرویس یافت نشد.", "CATALOG_CATEGORY_NOT_FOUND");
        }

        var pricingMode = request.PricingMode ?? PricingMode.UnitBased;
        if (request.IsActive && pricingMode != PricingMode.Additive && !request.InitialPrice.HasValue)
        {
            return Result.Failure<ServiceDto>(
                "برای فعال‌سازی سرویس، قیمت اولیه الزامی است.",
                "CATALOG_SERVICE_ACTIVE_WITHOUT_PRICE");
        }

        if (!string.IsNullOrWhiteSpace(request.AttributeSchemaJson))
        {
            var schema = FormSchemaParser.Parse(request.AttributeSchemaJson);
            if (schema.IsFailure)
            {
                return Result.Failure<ServiceDto>(schema.Errors, schema.Message);
            }
        }

        var now = _clock.UtcNow;
        var service = new Service(
            request.Code,
            request.Name,
            request.CategoryId,
            request.ServiceType,
            request.Unit,
            request.Description);

        service.MarkCreated(_currentUser.UserId, now);
        service.ConfigurePricing(
            pricingMode,
            request.BillingPeriod
                ?? (request.ServiceType == ServiceType.PhysicalProduct
                    ? BillingPeriod.OneTime
                    : BillingPeriod.Monthly),
            request.Unit,
            request.CommissionPercent,
            request.AttributeSchemaJson,
            _currentUser.UserId,
            now);
        service.SetWorkflowDefinition(request.WorkflowDefinitionId, _currentUser.UserId, now);

        if (request.InitialPrice.HasValue)
        {
            service.AddPrice(
                request.InitialPrice.Value,
                request.PriceEffectiveFromUtc ?? now,
                now,
                _currentUser.UserId);
        }

        if (request.IsActive)
        {
            service.SetStatus(true, now, _currentUser.UserId);
        }

        _services.Add(service);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = service.Id.ToString();

        var created = await _services.GetByIdWithPricesAsync(service.Id, cancellationToken);
        return Result.Success(_mapper.ToServiceDto(created ?? service));
    }
}

public sealed class UpdateServiceCommandHandler
    : IRequestHandler<UpdateServiceCommand, Result<ServiceDto>>
{
    private readonly IServiceRepository _services;
    private readonly ICategoryRepository _categories;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;
    private readonly IServiceUsageGuard _usageGuard;

    public UpdateServiceCommandHandler(
        IServiceRepository services,
        ICategoryRepository categories,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper,
        IServiceUsageGuard usageGuard)
    {
        _services = services;
        _categories = categories;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _usageGuard = usageGuard;
    }

    public async Task<Result<ServiceDto>> Handle(
        UpdateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdWithPricesAsync(request.Id, cancellationToken);
        if (service is null)
        {
            return Result.Failure<ServiceDto>("سرویس یافت نشد.", "CATALOG_SERVICE_NOT_FOUND");
        }

        if (await _services.CodeExistsAsync(request.Code, request.Id, cancellationToken))
        {
            return Result.Failure<ServiceDto>("کد سرویس قبلاً استفاده شده است.", "CATALOG_SERVICE_CODE_DUPLICATE");
        }

        if (await _categories.GetByIdAsync(request.CategoryId, cancellationToken) is null)
        {
            return Result.Failure<ServiceDto>("دسته‌بندی سرویس یافت نشد.", "CATALOG_CATEGORY_NOT_FOUND");
        }

        var hasActiveItems = await _usageGuard.HasActiveOrderItemsForServiceAsync(
            service.Id,
            cancellationToken);

        if (hasActiveItems
            && request.PricingMode.HasValue
            && request.PricingMode.Value != service.PricingMode)
        {
            return Result.Failure<ServiceDto>(
                "تغییر حالت قیمت‌گذاری برای سرویس دارای سفارش فعال مجاز نیست.",
                "CATALOG_PRICING_MODE_LOCKED");
        }

        if (hasActiveItems
            && request.AttributeSchemaJson is not null
            && !string.Equals(
                request.AttributeSchemaJson,
                service.AttributeSchemaJson,
                StringComparison.Ordinal))
        {
            return Result.Failure<ServiceDto>(
                "تغییر AttributeSchema برای سرویس دارای سفارش فعال مجاز نیست.",
                "CATALOG_ATTRIBUTE_SCHEMA_LOCKED");
        }

        if (hasActiveItems)
        {
            return Result.Failure<ServiceDto>(
                "برای ویرایش، نسخهٔ جدید بسازید.",
                "CATALOG_SERVICE_HAS_ACTIVE_ORDERS");
        }

        if (!string.IsNullOrWhiteSpace(request.AttributeSchemaJson))
        {
            var schema = FormSchemaParser.Parse(request.AttributeSchemaJson);
            if (schema.IsFailure)
            {
                return Result.Failure<ServiceDto>(schema.Errors, schema.Message);
            }
        }

        if (request.IsActive
            && (request.PricingMode ?? service.PricingMode) != PricingMode.Additive
            && service.Prices.Count == 0)
        {
            return Result.Failure<ServiceDto>(
                "سرویس بدون قیمت نمی‌تواند فعال شود.",
                "CATALOG_SERVICE_ACTIVE_WITHOUT_PRICE");
        }

        _auditScope.OldValues = _mapper.ToServiceDto(service);
        _auditScope.EntityId = service.Id.ToString();

        service.Update(
            request.Code,
            request.Name,
            request.CategoryId,
            request.ServiceType,
            request.Unit,
            request.Description,
            request.IsActive,
            _clock.UtcNow,
            _currentUser.UserId);
        service.ConfigurePricing(
            request.PricingMode ?? service.PricingMode,
            request.BillingPeriod ?? service.BillingPeriod,
            request.Unit,
            request.CommissionPercent ?? service.CommissionPercent,
            request.AttributeSchemaJson ?? service.AttributeSchemaJson,
            _currentUser.UserId,
            _clock.UtcNow);
        service.SetWorkflowDefinition(
            request.WorkflowDefinitionId ?? service.WorkflowDefinitionId,
            _currentUser.UserId,
            _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _services.GetByIdWithPricesAsync(service.Id, cancellationToken);

        return Result.Success(_mapper.ToServiceDto(updated ?? service));
    }
}

public sealed class ToggleServiceStatusCommandHandler
    : IRequestHandler<ToggleServiceStatusCommand, Result<ServiceDto>>
{
    private readonly IServiceRepository _services;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;
    private readonly IServiceUsageGuard _usageGuard;

    public ToggleServiceStatusCommandHandler(
        IServiceRepository services,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper,
        IServiceUsageGuard usageGuard)
    {
        _services = services;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _usageGuard = usageGuard;
    }

    public async Task<Result<ServiceDto>> Handle(
        ToggleServiceStatusCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdWithPricesAsync(request.Id, cancellationToken);
        if (service is null)
        {
            return Result.Failure<ServiceDto>("سرویس یافت نشد.", "CATALOG_SERVICE_NOT_FOUND");
        }

        if (request.IsActive
            && service.PricingMode != PricingMode.Additive
            && service.Prices.Count == 0)
        {
            return Result.Failure<ServiceDto>(
                "سرویس بدون قیمت نمی‌تواند فعال شود.",
                "CATALOG_SERVICE_ACTIVE_WITHOUT_PRICE");
        }

        if (!request.IsActive
            && await _usageGuard.HasActiveOrderItemsForServiceAsync(
                service.Id,
                cancellationToken))
        {
            return Result.Failure<ServiceDto>(
                "سرویس دارای سفارش فعال قابل غیرفعال‌سازی نیست.",
                "CATALOG_SERVICE_HAS_ACTIVE_ORDERS");
        }

        _auditScope.OldValues = _mapper.ToServiceDto(service);
        _auditScope.EntityId = service.Id.ToString();

        service.SetStatus(request.IsActive, _clock.UtcNow, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _services.GetByIdWithPricesAsync(service.Id, cancellationToken);
        return Result.Success(_mapper.ToServiceDto(updated ?? service));
    }
}

public sealed class SetServicePriceCommandHandler
    : IRequestHandler<SetServicePriceCommand, Result<ServicePriceDto>>
{
    private readonly IServiceRepository _services;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public SetServicePriceCommandHandler(
        IServiceRepository services,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _services = services;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServicePriceDto>> Handle(
        SetServicePriceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdWithPricesAsync(request.ServiceId, cancellationToken);
        if (service is null)
        {
            return Result.Failure<ServicePriceDto>("سرویس یافت نشد.", "CATALOG_SERVICE_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        var validFrom = DateTime.SpecifyKind(request.EffectiveFromUtc ?? now, DateTimeKind.Utc);
        if (validFrom < now.AddSeconds(-1))
        {
            return Result.Failure<ServicePriceDto>(
                "تاریخ شروع قیمت جدید نمی‌تواند در گذشته باشد.",
                "CATALOG_PRICE_VALID_FROM_PAST");
        }

        _auditScope.OldValues = _mapper.ToServiceDto(service);
        _auditScope.EntityId = service.Id.ToString();

        service.AddPrice(
            request.Price,
            validFrom,
            now,
            _currentUser.UserId,
            request.Currency);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newPrice = service.Prices
            .Where(price => price.EffectiveTo is null)
            .OrderByDescending(price => price.EffectiveFrom)
            .First();

        return Result.Success(_mapper.ToPriceDto(newPrice));
    }
}
