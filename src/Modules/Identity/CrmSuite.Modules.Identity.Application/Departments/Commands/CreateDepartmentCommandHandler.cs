using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using CrmSuite.Modules.Identity.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Departments.Commands;

public sealed class CreateDepartmentCommandHandler
    : IRequestHandler<CreateDepartmentCommand, Result<DepartmentDto>>
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public CreateDepartmentCommandHandler(
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
        CreateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue
            && await _departments.GetByIdAsync(request.ParentId.Value, cancellationToken) is null)
        {
            return Result.Failure<DepartmentDto>("دپارتمان والد یافت نشد.", "IDENTITY_DEPARTMENT_PARENT_NOT_FOUND");
        }

        if (await _departments.NameExistsAsync(request.Name, request.ParentId, null, cancellationToken))
        {
            return Result.Failure<DepartmentDto>(
                "دپارتمانی با این نام در همان سطح وجود دارد.",
                "IDENTITY_DEPARTMENT_NAME_DUPLICATE");
        }

        var now = _clock.UtcNow;
        var department = new Department(request.Name, request.ParentId);
        department.MarkCreated(_currentUser.UserId, now);
        _departments.Add(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _auditScope.EntityId = department.Id.ToString();
        return Result.Success(_mapper.ToDepartmentDto(department));
    }
}
