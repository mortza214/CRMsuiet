using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using CrmSuite.Modules.Identity.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IDepartmentRepository _departments;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public CreateUserCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IDepartmentRepository departments,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _users = users;
        _roles = roles;
        _departments = departments;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var roleIds = request.RoleIds?.Distinct().ToArray() ?? [];
        var departmentIds = request.DepartmentIds?.Distinct().ToArray() ?? [];

        if (await _users.UsernameExistsAsync(request.Username, null, cancellationToken))
        {
            return Result.Failure<UserDto>("نام کاربری قبلاً استفاده شده است.", "IDENTITY_USERNAME_DUPLICATE");
        }

        if (await _users.MobileExistsAsync(request.Mobile, null, cancellationToken))
        {
            return Result.Failure<UserDto>("موبایل قبلاً استفاده شده است.", "IDENTITY_MOBILE_DUPLICATE");
        }

        var assignmentError = await ValidateAssignmentsAsync(
            roleIds,
            departmentIds,
            cancellationToken);

        if (assignmentError is not null)
        {
            return Result.Failure<UserDto>(assignmentError);
        }

        var now = _clock.UtcNow;
        var user = new User(
            request.Username,
            _passwordHasher.Hash(request.Password),
            request.FullName,
            request.Mobile,
            request.Email,
            request.IsActive);

        user.MarkCreated(_currentUser.UserId, now);
        user.ReplaceRoles(roleIds, _currentUser.UserId, now);
        user.ReplaceDepartments(departmentIds, _currentUser.UserId, now);
        _users.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _auditScope.EntityId = user.Id.ToString();
        var created = await _users.GetByIdWithAccessAsync(user.Id, cancellationToken);

        return Result.Success(_mapper.ToUserDto(created ?? user));
    }

    private async Task<string?> ValidateAssignmentsAsync(
        IReadOnlyCollection<Guid> roleIds,
        IReadOnlyCollection<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count > 0)
        {
            var roles = await _roles.GetByIdsAsync(roleIds, false, cancellationToken);
            if (roles.Count != roleIds.Count)
            {
                return "یکی از نقش‌های انتخاب‌شده معتبر نیست.";
            }
        }

        if (departmentIds.Count > 0)
        {
            var departments = await _departments.GetByIdsAsync(departmentIds, cancellationToken);
            if (departments.Count != departmentIds.Count)
            {
                return "یکی از دپارتمان‌های انتخاب‌شده معتبر نیست.";
            }
        }

        return null;
    }
}
