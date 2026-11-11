using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed class CreateAddonCommandHandler : IRequestHandler<CreateAddonCommand, Result<ServiceAddonDto>>
{
    private readonly IAddonRepository _addons;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public CreateAddonCommandHandler(
        IAddonRepository addons,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _addons = addons;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServiceAddonDto>> Handle(
        CreateAddonCommand request,
        CancellationToken cancellationToken)
    {
        if (await _addons.NameExistsAsync(request.Name, null, cancellationToken))
        {
            return Result.Failure<ServiceAddonDto>("آیتم جانبی با این نام وجود دارد.", "CATALOG_ADDON_NAME_DUPLICATE");
        }

        var addon = new ServiceAddon(
            request.ServiceId,
            string.IsNullOrWhiteSpace(request.Code)
                ? $"ADD-{Guid.NewGuid():N}"[..12].ToUpperInvariant()
                : request.Code,
            request.Name,
            request.PricingMode ?? AddonPricingMode.Fixed,
            request.Unit,
            request.Price,
            request.IsActive,
            request.SortOrder,
            request.Description,
            _currentUser.UserId,
            _clock.UtcNow);
        _addons.Add(addon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = addon.Id.ToString();

        return Result.Success(_mapper.ToAddonDto(addon));
    }
}

public sealed class UpdateAddonCommandHandler : IRequestHandler<UpdateAddonCommand, Result<ServiceAddonDto>>
{
    private readonly IAddonRepository _addons;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;
    private readonly IServiceUsageGuard? _usageGuard;

    public UpdateAddonCommandHandler(
        IAddonRepository addons,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper,
        IServiceUsageGuard? usageGuard = null)
    {
        _addons = addons;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _usageGuard = usageGuard;
    }

    public async Task<Result<ServiceAddonDto>> Handle(
        UpdateAddonCommand request,
        CancellationToken cancellationToken)
    {
        var addon = await _addons.GetByIdAsync(request.Id, cancellationToken);
        if (addon is null)
        {
            return Result.Failure<ServiceAddonDto>("آیتم جانبی یافت نشد.", "CATALOG_ADDON_NOT_FOUND");
        }

        if (await _addons.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            return Result.Failure<ServiceAddonDto>("آیتم جانبی با این نام وجود دارد.", "CATALOG_ADDON_NAME_DUPLICATE");
        }

        if (_usageGuard is not null
            && await _usageGuard.HasActiveOrderItemsUsingAddonAsync(
                addon.Id,
                cancellationToken))
        {
            return Result.Failure<ServiceAddonDto>(
                "آیتم جانبی در سفارش فعال استفاده شده و ویرایش آن مجاز نیست.",
                "CATALOG_ADDON_IN_ACTIVE_ORDER");
        }

        _auditScope.OldValues = _mapper.ToAddonDto(addon);
        _auditScope.EntityId = addon.Id.ToString();
        addon.UpdateDetails(
            request.ServiceId,
            string.IsNullOrWhiteSpace(request.Code) ? addon.Code : request.Code,
            request.Name,
            request.PricingMode ?? addon.PricingMode,
            request.Unit,
            request.Price,
            request.IsActive,
            request.SortOrder,
            request.Description,
            _currentUser.UserId,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToAddonDto(addon));
    }
}

public sealed class DeleteAddonCommandHandler : IRequestHandler<DeleteAddonCommand, Result>
{
    private readonly IAddonRepository _addons;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public DeleteAddonCommandHandler(
        IAddonRepository addons,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _addons = addons;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result> Handle(DeleteAddonCommand request, CancellationToken cancellationToken)
    {
        var addon = await _addons.GetByIdAsync(request.Id, cancellationToken);
        if (addon is null)
        {
            return Result.Failure("آیتم جانبی یافت نشد.", "CATALOG_ADDON_NOT_FOUND");
        }

        _auditScope.OldValues = _mapper.ToAddonDto(addon);
        _auditScope.EntityId = addon.Id.ToString();
        addon.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("آیتم جانبی حذف شد.");
    }
}
