using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed class CreateDataCenterItemCommandHandler
    : IRequestHandler<CreateDataCenterItemCommand, Result<DataCenterItemDto>>
{
    private readonly IDataCenterItemRepository _items;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public CreateDataCenterItemCommandHandler(
        IDataCenterItemRepository items,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _items = items;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<DataCenterItemDto>> Handle(
        CreateDataCenterItemCommand request,
        CancellationToken cancellationToken)
    {
        if (await _items.NameExistsAsync(request.Name, null, cancellationToken))
        {
            return Result.Failure<DataCenterItemDto>(
                "آیتم دیتاسنتر با این نام وجود دارد.",
                "CATALOG_DC_ITEM_NAME_DUPLICATE");
        }

        var item = new DataCenterItem(request.ItemType, request.Name, request.Price, request.Unit);
        item.MarkCreated(_currentUser.UserId, _clock.UtcNow);
        _items.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = item.Id.ToString();

        return Result.Success(_mapper.ToDataCenterItemDto(item));
    }
}

public sealed class UpdateDataCenterItemCommandHandler
    : IRequestHandler<UpdateDataCenterItemCommand, Result<DataCenterItemDto>>
{
    private readonly IDataCenterItemRepository _items;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public UpdateDataCenterItemCommandHandler(
        IDataCenterItemRepository items,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _items = items;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<DataCenterItemDto>> Handle(
        UpdateDataCenterItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure<DataCenterItemDto>("آیتم دیتاسنتر یافت نشد.", "CATALOG_DC_ITEM_NOT_FOUND");
        }

        if (await _items.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            return Result.Failure<DataCenterItemDto>(
                "آیتم دیتاسنتر با این نام وجود دارد.",
                "CATALOG_DC_ITEM_NAME_DUPLICATE");
        }

        _auditScope.OldValues = _mapper.ToDataCenterItemDto(item);
        _auditScope.EntityId = item.Id.ToString();
        item.Update(request.ItemType, request.Name, request.Price, request.Unit, _clock.UtcNow, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToDataCenterItemDto(item));
    }
}

public sealed class DeleteDataCenterItemCommandHandler
    : IRequestHandler<DeleteDataCenterItemCommand, Result>
{
    private readonly IDataCenterItemRepository _items;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public DeleteDataCenterItemCommandHandler(
        IDataCenterItemRepository items,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _items = items;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result> Handle(
        DeleteDataCenterItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure("آیتم دیتاسنتر یافت نشد.", "CATALOG_DC_ITEM_NOT_FOUND");
        }

        _auditScope.OldValues = _mapper.ToDataCenterItemDto(item);
        _auditScope.EntityId = item.Id.ToString();
        item.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("آیتم دیتاسنتر حذف شد.");
    }
}
