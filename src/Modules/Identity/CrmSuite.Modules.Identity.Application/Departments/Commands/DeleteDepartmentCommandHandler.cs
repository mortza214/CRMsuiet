using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Departments.Commands;

public sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Result>
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public DeleteDepartmentCommandHandler(
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

    public async Task<Result> Handle(
        DeleteDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await _departments.GetByIdAsync(request.Id, cancellationToken);
        if (department is null)
        {
            return Result.Failure("دپارتمان یافت نشد.", "IDENTITY_DEPARTMENT_NOT_FOUND");
        }

        if (await _departments.HasChildrenAsync(request.Id, cancellationToken))
        {
            return Result.Failure(
                "ابتدا دپارتمان‌های زیرمجموعه را جابه‌جا یا حذف کنید.",
                "IDENTITY_DEPARTMENT_HAS_CHILDREN");
        }

        if (await _departments.CountUsersAsync(request.Id, cancellationToken) > 0)
        {
            return Result.Failure(
                "دپارتمان به کاربران اختصاص داده شده است و قابل حذف نیست.",
                "IDENTITY_DEPARTMENT_HAS_USERS");
        }

        _auditScope.OldValues = _mapper.ToDepartmentDto(department);
        _auditScope.EntityId = department.Id.ToString();

        department.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("دپارتمان حذف شد.");
    }
}
