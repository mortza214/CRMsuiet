using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed class AssignRolesCommandHandler : IRequestHandler<AssignRolesCommand, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public AssignRolesCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPermissionProvider permissionProvider,
        IUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _users = users;
        _roles = roles;
        _permissionProvider = permissionProvider;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> Handle(
        AssignRolesCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithAccessAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserDto>("کاربر یافت نشد.", "IDENTITY_USER_NOT_FOUND");
        }

        var roleIds = request.RoleIds.Distinct().ToArray();
        var roles = await _roles.GetByIdsAsync(roleIds, false, cancellationToken);
        if (roles.Count != roleIds.Length)
        {
            return Result.Failure<UserDto>("یکی از نقش‌های انتخاب‌شده معتبر نیست.");
        }

        _auditScope.OldValues = _mapper.ToUserDto(user);
        _auditScope.EntityId = user.Id.ToString();

        user.ReplaceRoles(roleIds, _currentUser.UserId, _clock.UtcNow);
        _permissionProvider.Invalidate(user.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _users.GetByIdWithAccessAsync(user.Id, cancellationToken);

        return Result.Success(_mapper.ToUserDto(updated ?? user));
    }
}
