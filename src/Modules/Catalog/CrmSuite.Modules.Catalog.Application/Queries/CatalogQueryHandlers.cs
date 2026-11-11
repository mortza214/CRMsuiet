using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Forms.Application.Abstractions;
using MediatR;

namespace CrmSuite.Modules.Catalog.Application.Queries;

public sealed class GetServicesQueryHandler
    : IRequestHandler<GetServicesQuery, Result<PagedResult<ServiceListItemDto>>>
{
    private readonly ICatalogReadRepository _readRepository;

    public GetServicesQueryHandler(ICatalogReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<ServiceListItemDto>>> Handle(
        GetServicesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.SearchServicesAsync(
            new ServiceSearchFilter(
                request.Search,
                request.CategoryId,
                request.ServiceType,
                request.IsActive,
                request.Page,
                request.PageSize,
                request.PricingMode),
            cancellationToken);

        return Result.Success(page);
    }
}

public sealed class GetServiceByIdQueryHandler
    : IRequestHandler<GetServiceByIdQuery, Result<ServiceDto>>
{
    private readonly IServiceRepository _services;
    private readonly IAddonRepository _addons;
    private readonly IServiceFormLinkRepository _links;
    private readonly IFormTemplateRepository _forms;
    private readonly IServiceUsageGuard _usageGuard;
    private readonly CatalogDtoMapper _mapper;

    public GetServiceByIdQueryHandler(
        IServiceRepository services,
        IAddonRepository addons,
        IServiceFormLinkRepository links,
        IFormTemplateRepository forms,
        IServiceUsageGuard usageGuard,
        CatalogDtoMapper mapper)
    {
        _services = services;
        _addons = addons;
        _links = links;
        _forms = forms;
        _usageGuard = usageGuard;
        _mapper = mapper;
    }

    public async Task<Result<ServiceDto>> Handle(
        GetServiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (service is null)
        {
            return Result.Failure<ServiceDto>("سرویس یافت نشد.", "CATALOG_SERVICE_NOT_FOUND");
        }

        var addons = await _addons.GetAvailableForServiceAsync(service.Id, cancellationToken);
        var links = await _links.GetByServiceAsync(service.Id, cancellationToken);
        var forms = new List<ServiceFormLinkDto>(links.Count);
        foreach (var link in links)
        {
            var form = await _forms.GetByIdAsync(link.FormDefinitionId, cancellationToken);
            forms.Add(_mapper.ToFormLinkDto(link, form?.Code, form?.Title));
        }

        var dto = _mapper.ToServiceDto(service, addons, forms);
        var hasActiveOrders = await _usageGuard.HasActiveOrderItemsForServiceAsync(
            service.Id,
            cancellationToken);

        return Result.Success(hasActiveOrders
            ? dto with
            {
                CanEdit = false,
                EditBlockedReason = "برای ویرایش، نسخهٔ جدید بسازید.",
            }
            : dto);
    }
}

public sealed class GetEffectivePriceQueryHandler
    : IRequestHandler<GetEffectivePriceQuery, Result<ServicePriceDto>>
{
    private readonly IServiceRepository _services;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public GetEffectivePriceQueryHandler(
        IServiceRepository services,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _services = services;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServicePriceDto>> Handle(
        GetEffectivePriceQuery request,
        CancellationToken cancellationToken)
    {
        var date = DateTime.SpecifyKind(request.DateUtc ?? _clock.UtcNow, DateTimeKind.Utc);
        var price = await _services.GetEffectivePriceAsync(request.ServiceId, date, cancellationToken);

        return price is null
            ? Result.Failure<ServicePriceDto>(
                "برای این سرویس در تاریخ درخواستی قیمت معتبری وجود ندارد.",
                "CATALOG_EFFECTIVE_PRICE_NOT_FOUND")
            : Result.Success(_mapper.ToPriceDto(price));
    }
}

public sealed class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<ServiceCategoryDto>>>
{
    private readonly ICategoryRepository _categories;
    private readonly CatalogDtoMapper _mapper;

    public GetCategoriesQueryHandler(ICategoryRepository categories, CatalogDtoMapper mapper)
    {
        _categories = categories;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ServiceCategoryDto>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _categories.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ServiceCategoryDto>>(
            categories.Select(_mapper.ToCategoryDto).ToArray());
    }
}

public sealed class GetAddonsQueryHandler
    : IRequestHandler<GetAddonsQuery, Result<IReadOnlyList<ServiceAddonDto>>>
{
    private readonly IAddonRepository _addons;
    private readonly CatalogDtoMapper _mapper;

    public GetAddonsQueryHandler(IAddonRepository addons, CatalogDtoMapper mapper)
    {
        _addons = addons;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ServiceAddonDto>>> Handle(
        GetAddonsQuery request,
        CancellationToken cancellationToken)
    {
        var addons = await _addons.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ServiceAddonDto>>(
            addons.Select(_mapper.ToAddonDto).ToArray());
    }
}

public sealed class GetDataCenterItemsQueryHandler
    : IRequestHandler<GetDataCenterItemsQuery, Result<IReadOnlyList<DataCenterItemDto>>>
{
    private readonly IDataCenterItemRepository _items;
    private readonly CatalogDtoMapper _mapper;

    public GetDataCenterItemsQueryHandler(IDataCenterItemRepository items, CatalogDtoMapper mapper)
    {
        _items = items;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<DataCenterItemDto>>> Handle(
        GetDataCenterItemsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _items.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DataCenterItemDto>>(
            items.Select(_mapper.ToDataCenterItemDto).ToArray());
    }
}

public sealed class GetServicePriceHistoryQueryHandler
    : IRequestHandler<GetServicePriceHistoryQuery, Result<IReadOnlyList<ServicePriceDto>>>
{
    private readonly IServiceRepository _services;
    private readonly CatalogDtoMapper _mapper;

    public GetServicePriceHistoryQueryHandler(
        IServiceRepository services,
        CatalogDtoMapper mapper)
    {
        _services = services;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ServicePriceDto>>> Handle(
        GetServicePriceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdWithPricesAsync(request.ServiceId, cancellationToken);
        if (service is null)
        {
            return Result.Failure<IReadOnlyList<ServicePriceDto>>(
                "سرویس یافت نشد.",
                "CATALOG_SERVICE_NOT_FOUND");
        }

        return Result.Success<IReadOnlyList<ServicePriceDto>>(
            service.Prices
                .OrderByDescending(price => price.EffectiveFrom)
                .Select(_mapper.ToPriceDto)
                .ToArray());
    }
}

public sealed class GetServiceFormsQueryHandler
    : IRequestHandler<GetServiceFormsQuery, Result<IReadOnlyList<ServiceFormLinkDto>>>
{
    private readonly IServiceFormLinkRepository _links;
    private readonly IFormTemplateRepository _forms;
    private readonly CatalogDtoMapper _mapper;

    public GetServiceFormsQueryHandler(
        IServiceFormLinkRepository links,
        IFormTemplateRepository forms,
        CatalogDtoMapper mapper)
    {
        _links = links;
        _forms = forms;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ServiceFormLinkDto>>> Handle(
        GetServiceFormsQuery request,
        CancellationToken cancellationToken)
    {
        var links = await _links.GetByServiceAsync(request.ServiceId, cancellationToken);
        var result = new List<ServiceFormLinkDto>(links.Count);
        foreach (var link in links)
        {
            var form = await _forms.GetByIdAsync(link.FormDefinitionId, cancellationToken);
            result.Add(_mapper.ToFormLinkDto(link, form?.Code, form?.Title));
        }

        return Result.Success<IReadOnlyList<ServiceFormLinkDto>>(result);
    }
}

public sealed class GetServiceFormRequirementsQueryHandler
    : IRequestHandler<GetServiceFormRequirementsQuery, Result<IReadOnlyList<ServiceFormLinkDto>>>
{
    private readonly IServiceFormLinkRepository _links;
    private readonly IFormTemplateRepository _forms;
    private readonly CatalogDtoMapper _mapper;

    public GetServiceFormRequirementsQueryHandler(
        IServiceFormLinkRepository links,
        IFormTemplateRepository forms,
        CatalogDtoMapper mapper)
    {
        _links = links;
        _forms = forms;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ServiceFormLinkDto>>> Handle(
        GetServiceFormRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        var links = await _links.GetByServiceAsync(request.ServiceId, cancellationToken);
        var result = new List<ServiceFormLinkDto>();
        foreach (var link in links.Where(link => link.IsRequired))
        {
            var form = await _forms.GetByIdAsync(link.FormDefinitionId, cancellationToken);
            result.Add(_mapper.ToFormLinkDto(link, form?.Code, form?.Title));
        }

        return Result.Success<IReadOnlyList<ServiceFormLinkDto>>(result);
    }
}

public sealed class GetAvailableAddonsForServiceQueryHandler
    : IRequestHandler<GetAvailableAddonsForServiceQuery, Result<IReadOnlyList<ServiceAddonDto>>>
{
    private readonly IAddonRepository _addons;
    private readonly CatalogDtoMapper _mapper;

    public GetAvailableAddonsForServiceQueryHandler(
        IAddonRepository addons,
        CatalogDtoMapper mapper)
    {
        _addons = addons;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ServiceAddonDto>>> Handle(
        GetAvailableAddonsForServiceQuery request,
        CancellationToken cancellationToken)
    {
        var addons = await _addons.GetAvailableForServiceAsync(
            request.ServiceId,
            cancellationToken);
        return Result.Success<IReadOnlyList<ServiceAddonDto>>(
            addons.Select(_mapper.ToAddonDto).ToArray());
    }
}

public sealed class PreviewServicePriceQueryHandler
    : IRequestHandler<PreviewServicePriceQuery, Result<PricePreviewDto>>
{
    private readonly IServicePricingService _pricing;

    public PreviewServicePriceQueryHandler(IServicePricingService pricing)
    {
        _pricing = pricing;
    }

    public async Task<Result<PricePreviewDto>> Handle(
        PreviewServicePriceQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _pricing.CalculateAsync(
            new ServicePricingRequest(
                request.ServiceId,
                request.Quantity,
                request.Addons,
                request.AttributesJson),
            cancellationToken);

        return result.IsSuccess
            ? Result.Success(result.Value!.Preview)
            : Result.Failure<PricePreviewDto>(result.Errors, result.Message);
    }
}