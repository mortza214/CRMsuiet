using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed record CreateCategoryCommand(
    string Name,
    Guid? ParentId = null) : IRequest<Result<ServiceCategoryDto>>;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    Guid? ParentId = null) : IRequest<Result<ServiceCategoryDto>>;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;

public sealed record CreateAddonCommand(
    string Name,
    decimal Price,
    ServiceUnit Unit,
    bool IsOptional,
    Guid? ServiceId = null,
    string? Code = null,
    AddonPricingMode? PricingMode = null,
    int SortOrder = 0,
    string? Description = null,
    bool IsActive = true) : IRequest<Result<ServiceAddonDto>>;

public sealed record UpdateAddonCommand(
    Guid Id,
    string Name,
    decimal Price,
    ServiceUnit Unit,
    bool IsOptional,
    Guid? ServiceId = null,
    string? Code = null,
    AddonPricingMode? PricingMode = null,
    int SortOrder = 0,
    string? Description = null,
    bool IsActive = true) : IRequest<Result<ServiceAddonDto>>;

public sealed record DeleteAddonCommand(Guid Id) : IRequest<Result>;

public sealed record CreateDataCenterItemCommand(
    DataCenterItemType ItemType,
    string Name,
    decimal Price,
    ServiceUnit Unit) : IRequest<Result<DataCenterItemDto>>;

public sealed record UpdateDataCenterItemCommand(
    Guid Id,
    DataCenterItemType ItemType,
    string Name,
    decimal Price,
    ServiceUnit Unit) : IRequest<Result<DataCenterItemDto>>;

public sealed record DeleteDataCenterItemCommand(Guid Id) : IRequest<Result>;
