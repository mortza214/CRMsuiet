using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Departments.Commands;

public sealed class UpdateDepartmentCommandHandler
    : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentDto>>
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _departments = departments;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<DepartmentDto>> Handle(
        UpdateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await _departments.GetByIdAsync(request.Id, cancellationToken);
        if (department is null)
        {
            return Result.Failure<DepartmentDto>("دپارتمان یافت نشد.", "IDENTITY_DEPARTMENT_NOT_FOUND");
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == request.Id)
            {
                return Result.Failure<DepartmentDto>(
                    "دپارتمان نمی‌تواند والد خودش باشد.",
                    "IDENTITY_DEPARTMENT_SELF_PARENT");
            }

            if (await _departments.GetByIdAsync(request.ParentId.Value, cancellationToken) is null)
            {
                return Result.Failure<DepartmentDto>(
                    "دپارتمان والد یافت نشد.",
                    "IDENTITY_DEPARTMENT_PARENT_NOT_FOUND");
            }

            if (await _departments.IsDescendantAsync(request.Id, request.ParentId.Value, cancellationToken))
            {
                return Result.Failure<DepartmentDto>(
                    "انتخاب این والد باعث ایجاد حلقه در درخت دپارتمان می‌شود.",
                    "IDENTITY_DEPARTMENT_CYCLE");
            }
        }

        if (await _departments.NameExistsAsync(
                request.Name,
                request.ParentId,
                request.Id,
                cancellationToken))
        {
            return Result.Failure<DepartmentDto>(
                "دپارتمانی با این نام در همان سطح وجود دارد.",
                "IDENTITY_DEPARTMENT_NAME_DUPLICATE");
        }

        _auditScope.OldValues = _mapper.ToDepartmentDto(department);
        _auditScope.EntityId = department.Id.ToString();

        department.Rename(request.Name, request.ParentId, _currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToDepartmentDto(department));
    }
}
