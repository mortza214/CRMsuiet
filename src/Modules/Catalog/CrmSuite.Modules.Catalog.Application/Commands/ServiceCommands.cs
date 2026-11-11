using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed record CreateServiceCommand(
    string Code,
    string Name,
    Guid CategoryId,
    ServiceType ServiceType,
    ServiceUnit Unit,
    string? Description,
    bool IsActive,
    decimal? InitialPrice = null,
    DateTime? PriceEffectiveFromUtc = null,
    PricingMode? PricingMode = null,
    BillingPeriod? BillingPeriod = null,
    decimal? CommissionPercent = null,
    string? AttributeSchemaJson = null,
    Guid? WorkflowDefinitionId = null) : IRequest<Result<ServiceDto>>;

public sealed record UpdateServiceCommand(
    Guid Id,
    string Code,
    string Name,
    Guid CategoryId,
    ServiceType ServiceType,
    ServiceUnit Unit,
    string? Description,
    bool IsActive,
    PricingMode? PricingMode = null,
    BillingPeriod? BillingPeriod = null,
    decimal? CommissionPercent = null,
    string? AttributeSchemaJson = null,
    Guid? WorkflowDefinitionId = null) : IRequest<Result<ServiceDto>>;

public sealed record ToggleServiceStatusCommand(
    Guid Id,
    bool IsActive) : IRequest<Result<ServiceDto>>;

public sealed record SetServicePriceCommand(
    Guid ServiceId,
    decimal Price,
    DateTime? EffectiveFromUtc = null,
    string Currency = "IRR") : IRequest<Result<ServicePriceDto>>;

public sealed record SetServiceAttributeSchemaCommand(
    Guid ServiceId,
    string? AttributeSchemaJson) : IRequest<Result<ServiceDto>>;

public sealed record ActivateServiceCommand(Guid Id) : IRequest<Result<ServiceDto>>;

public sealed record DeactivateServiceCommand(Guid Id) : IRequest<Result<ServiceDto>>;
