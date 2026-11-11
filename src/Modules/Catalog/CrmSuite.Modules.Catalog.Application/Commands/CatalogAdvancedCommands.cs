using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed record CreateAddonAdvancedCommand(
    Guid? ServiceId,
    string Code,
    string Name,
    AddonPricingMode PricingMode,
    ServiceUnit Unit,
    decimal UnitPrice,
    bool IsActive = true,
    int SortOrder = 0,
    string? Description = null) : IRequest<Result<ServiceAddonDto>>;

public sealed record UpdateAddonAdvancedCommand(
    Guid Id,
    Guid? ServiceId,
    string Code,
    string Name,
    AddonPricingMode PricingMode,
    ServiceUnit Unit,
    decimal UnitPrice,
    bool IsActive,
    int SortOrder,
    string? Description) : IRequest<Result<ServiceAddonDto>>;

public sealed record DeactivateAddonCommand(Guid Id) : IRequest<Result<ServiceAddonDto>>;

public sealed record LinkFormToServiceCommand(
    Guid ServiceId,
    Guid FormDefinitionId,
    bool IsRequired,
    int SortOrder) : IRequest<Result<ServiceFormLinkDto>>;

public sealed record UnlinkFormFromServiceCommand(
    Guid ServiceId,
    Guid FormDefinitionId) : IRequest<Result>;
