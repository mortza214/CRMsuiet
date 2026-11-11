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

public sealed class CreateCategoryCommandHandler
    : IRequestHandler<CreateCategoryCommand, Result<ServiceCategoryDto>>
{
    private readonly ICategoryRepository _categories;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public CreateCategoryCommandHandler(
        ICategoryRepository categories,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServiceCategoryDto>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue
            && await _categories.GetByIdAsync(request.ParentId.Value, cancellationToken) is null)
        {
            return Result.Failure<ServiceCategoryDto>("دسته‌بندی والد یافت نشد.", "CATALOG_CATEGORY_PARENT_NOT_FOUND");
        }

        if (await _categories.NameExistsAsync(request.Name, request.ParentId, null, cancellationToken))
        {
            return Result.Failure<ServiceCategoryDto>(
                "دسته‌بندی با این نام در همان سطح وجود دارد.",
                "CATALOG_CATEGORY_NAME_DUPLICATE");
        }

        var category = new ServiceCategory(request.Name, request.ParentId);
        category.MarkCreated(_currentUser.UserId, _clock.UtcNow);
        _categories.Add(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = category.Id.ToString();

        return Result.Success(_mapper.ToCategoryDto(category));
    }
}

public sealed class UpdateCategoryCommandHandler
    : IRequestHandler<UpdateCategoryCommand, Result<ServiceCategoryDto>>
{
    private readonly ICategoryRepository _categories;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categories,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<ServiceCategoryDto>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure<ServiceCategoryDto>("دسته‌بندی یافت نشد.", "CATALOG_CATEGORY_NOT_FOUND");
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == request.Id)
            {
                return Result.Failure<ServiceCategoryDto>(
                    "دسته‌بندی نمی‌تواند والد خودش باشد.",
                    "CATALOG_CATEGORY_SELF_PARENT");
            }

            if (await _categories.GetByIdAsync(request.ParentId.Value, cancellationToken) is null)
            {
                return Result.Failure<ServiceCategoryDto>(
                    "دسته‌بندی والد یافت نشد.",
                    "CATALOG_CATEGORY_PARENT_NOT_FOUND");
            }

            if (await _categories.IsDescendantAsync(request.Id, request.ParentId.Value, cancellationToken))
            {
                return Result.Failure<ServiceCategoryDto>(
                    "انتخاب این والد باعث ایجاد حلقه در درخت دسته‌بندی می‌شود.",
                    "CATALOG_CATEGORY_CYCLE");
            }
        }

        if (await _categories.NameExistsAsync(request.Name, request.ParentId, request.Id, cancellationToken))
        {
            return Result.Failure<ServiceCategoryDto>(
                "دسته‌بندی با این نام در همان سطح وجود دارد.",
                "CATALOG_CATEGORY_NAME_DUPLICATE");
        }

        _auditScope.OldValues = _mapper.ToCategoryDto(category);
        _auditScope.EntityId = category.Id.ToString();
        category.Rename(request.Name, request.ParentId, _clock.UtcNow, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToCategoryDto(category));
    }
}

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly ICategoryRepository _categories;
    private readonly ICatalogUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CatalogDtoMapper _mapper;

    public DeleteCategoryCommandHandler(
        ICategoryRepository categories,
        ICatalogUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CatalogDtoMapper mapper)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure("دسته‌بندی یافت نشد.", "CATALOG_CATEGORY_NOT_FOUND");
        }

        if (await _categories.HasChildrenAsync(request.Id, cancellationToken))
        {
            return Result.Failure("ابتدا زیردسته‌ها را جابه‌جا یا حذف کنید.", "CATALOG_CATEGORY_HAS_CHILDREN");
        }

        if (await _categories.HasServicesAsync(request.Id, cancellationToken))
        {
            return Result.Failure("دسته‌بندی دارای سرویس است و قابل حذف نیست.", "CATALOG_CATEGORY_HAS_SERVICES");
        }

        _auditScope.OldValues = _mapper.ToCategoryDto(category);
        _auditScope.EntityId = category.Id.ToString();
        category.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("دسته‌بندی حذف شد.");
    }
}
