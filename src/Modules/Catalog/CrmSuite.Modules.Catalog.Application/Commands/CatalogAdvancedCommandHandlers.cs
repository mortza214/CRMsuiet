using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Schema;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed class SetServiceAttributeSchemaCommandHandler
    : IRequestHandler<SetServiceAttributeSchemaCommand, Result<ServiceDto>>
{
    private readonly IServiceRepository _services;
    private readonly IServiceUsageGuard _usageGuard;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public SetServiceAttributeSchemaCommandHandler(
        IServiceRepository services,
        IServiceUsageGuard usageGuard,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _services = services;
        _usageGuard = usageGuard;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServiceDto>> Handle(
        SetServiceAttributeSchemaCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _services.GetByIdWithPricesAsync(request.ServiceId, cancellationToken);
        if (service is null)
        {
            return Result.Failure<ServiceDto>("سرویس یافت نشد.", "CATALOG_SERVICE_NOT_FOUND");
        }

        if (await _usageGuard.HasActiveOrderItemsForServiceAsync(
            service.Id,
            cancellationToken))
        {
            return Result.Failure<ServiceDto>(
                "برای سرویس دارای سفارش فعال، تغییر AttributeSchema مجاز نیست.",
                "CATALOG_ATTRIBUTE_SCHEMA_LOCKED");
        }

        if (!string.IsNullOrWhiteSpace(request.AttributeSchemaJson))
        {
            var schema = FormSchemaParser.Parse(request.AttributeSchemaJson);
            if (schema.IsFailure)
            {
                return Result.Failure<ServiceDto>(schema.Errors, schema.Message);
            }
        }

        _auditScope.OldValues = _mapper.ToServiceDto(service);
        _auditScope.EntityId = service.Id.ToString();
        service.SetAttributeSchema(
            request.AttributeSchemaJson,
            _currentUser.UserId,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToServiceDto(service));
    }
}

public sealed class ActivateServiceCommandHandler
    : IRequestHandler<ActivateServiceCommand, Result<ServiceDto>>
{
    private readonly IMediator _mediator;

    public ActivateServiceCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<Result<ServiceDto>> Handle(
        ActivateServiceCommand request,
        CancellationToken cancellationToken) =>
        _mediator.Send(new ToggleServiceStatusCommand(request.Id, true), cancellationToken);
}

public sealed class DeactivateServiceCommandHandler
    : IRequestHandler<DeactivateServiceCommand, Result<ServiceDto>>
{
    private readonly IMediator _mediator;

    public DeactivateServiceCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<Result<ServiceDto>> Handle(
        DeactivateServiceCommand request,
        CancellationToken cancellationToken) =>
        _mediator.Send(new ToggleServiceStatusCommand(request.Id, false), cancellationToken);
}

public sealed class DeactivateAddonCommandHandler
    : IRequestHandler<DeactivateAddonCommand, Result<ServiceAddonDto>>
{
    private readonly IAddonRepository _addons;
    private readonly IServiceUsageGuard _usageGuard;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public DeactivateAddonCommandHandler(
        IAddonRepository addons,
        IServiceUsageGuard usageGuard,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _addons = addons;
        _usageGuard = usageGuard;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServiceAddonDto>> Handle(
        DeactivateAddonCommand request,
        CancellationToken cancellationToken)
    {
        var addon = await _addons.GetByIdAsync(request.Id, cancellationToken);
        if (addon is null)
        {
            return Result.Failure<ServiceAddonDto>("آیتم جانبی یافت نشد.", "CATALOG_ADDON_NOT_FOUND");
        }

        if (await _usageGuard.HasActiveOrderItemsUsingAddonAsync(
            addon.Id,
            cancellationToken))
        {
            return Result.Failure<ServiceAddonDto>(
                "آیتم جانبی در سفارش فعال استفاده شده و قابل غیرفعال‌سازی نیست.",
                "CATALOG_ADDON_IN_ACTIVE_ORDER");
        }

        _auditScope.OldValues = _mapper.ToAddonDto(addon);
        _auditScope.EntityId = addon.Id.ToString();
        addon.Deactivate(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToAddonDto(addon));
    }
}

public sealed class LinkFormToServiceCommandHandler
    : IRequestHandler<LinkFormToServiceCommand, Result<ServiceFormLinkDto>>
{
    private readonly IServiceRepository _services;
    private readonly IServiceFormLinkRepository _links;
    private readonly IFormTemplateRepository _forms;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public LinkFormToServiceCommandHandler(
        IServiceRepository services,
        IServiceFormLinkRepository links,
        IFormTemplateRepository forms,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _services = services;
        _links = links;
        _forms = forms;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServiceFormLinkDto>> Handle(
        LinkFormToServiceCommand request,
        CancellationToken cancellationToken)
    {
        if (await _services.GetByIdAsync(request.ServiceId, cancellationToken) is null)
        {
            return Result.Failure<ServiceFormLinkDto>(
                "سرویس یافت نشد.",
                "CATALOG_SERVICE_NOT_FOUND");
        }

        var form = await _forms.GetByIdAsync(request.FormDefinitionId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<ServiceFormLinkDto>(
                "فرم یافت نشد.",
                "FORMS_TEMPLATE_NOT_FOUND");
        }

        var link = await _links.GetAsync(
            request.ServiceId,
            request.FormDefinitionId,
            cancellationToken);

        if (link is null)
        {
            link = new ServiceFormLink(
                request.ServiceId,
                request.FormDefinitionId,
                request.IsRequired,
                request.SortOrder,
                _currentUser.UserId,
                _clock.UtcNow);
            _links.Add(link);
        }
        else
        {
            link.Update(request.IsRequired, request.SortOrder, _currentUser.UserId, _clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = link.Id.ToString();
        return Result.Success(_mapper.ToFormLinkDto(link, form.Code, form.Title));
    }
}

public sealed class UnlinkFormFromServiceCommandHandler
    : IRequestHandler<UnlinkFormFromServiceCommand, Result>
{
    private readonly IServiceFormLinkRepository _links;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;

    public UnlinkFormFromServiceCommandHandler(
        IServiceFormLinkRepository links,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope)
    {
        _links = links;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
    }

    public async Task<Result> Handle(
        UnlinkFormFromServiceCommand request,
        CancellationToken cancellationToken)
    {
        var link = await _links.GetAsync(
            request.ServiceId,
            request.FormDefinitionId,
            cancellationToken);
        if (link is null)
        {
            return Result.Failure("اتصال فرم یافت نشد.", "CATALOG_FORM_LINK_NOT_FOUND");
        }

        _links.Remove(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = link.Id.ToString();
        return Result.Success();
    }
}
