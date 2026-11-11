using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Catalog.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Catalog.Application.Queries;

public sealed record GetServicesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    ServiceType? ServiceType = null,
    bool? IsActive = null,
    PricingMode? PricingMode = null) : IRequest<Result<PagedResult<ServiceListItemDto>>>;

public sealed record GetServiceByIdQuery(Guid Id) : IRequest<Result<ServiceDto>>;

public sealed record GetEffectivePriceQuery(
    Guid ServiceId,
    DateTime? DateUtc = null) : IRequest<Result<ServicePriceDto>>;

public sealed record GetCategoriesQuery : IRequest<Result<IReadOnlyList<ServiceCategoryDto>>>;

public sealed record GetAddonsQuery : IRequest<Result<IReadOnlyList<ServiceAddonDto>>>;

public sealed record GetServicePriceHistoryQuery(Guid ServiceId)
    : IRequest<Result<IReadOnlyList<ServicePriceDto>>>;

public sealed record GetServiceFormsQuery(Guid ServiceId)
    : IRequest<Result<IReadOnlyList<ServiceFormLinkDto>>>;

public sealed record GetServiceFormRequirementsQuery(Guid ServiceId)
    : IRequest<Result<IReadOnlyList<ServiceFormLinkDto>>>;

public sealed record GetAvailableAddonsForServiceQuery(Guid ServiceId)
    : IRequest<Result<IReadOnlyList<ServiceAddonDto>>>;

public sealed record PreviewServicePriceQuery(
    Guid ServiceId,
    int Quantity,
    IReadOnlyCollection<AddonSelection> Addons,
    string? AttributesJson) : IRequest<Result<PricePreviewDto>>;

public sealed record GetDataCenterItemsQuery : IRequest<Result<IReadOnlyList<DataCenterItemDto>>>;
