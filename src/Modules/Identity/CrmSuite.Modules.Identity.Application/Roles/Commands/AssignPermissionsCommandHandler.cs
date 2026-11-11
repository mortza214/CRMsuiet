using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed class AssignPermissionsCommandHandler
    : IRequestHandler<AssignPermissionsCommand, Result<RoleDto>>
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public AssignPermissionsCommandHandler(
        IRoleRepository roles,
        IPermissionRepository permissions,
        IUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IdentityDtoMapper mapper)
    {
        _roles = roles;
        _permissions = permissions;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<RoleDto>> Handle(
        AssignPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleDto>("نقش یافت نشد.", "IDENTITY_ROLE_NOT_FOUND");
        }

        var permissionIds = request.PermissionIds.Distinct().ToArray();
        var permissions = await _permissions.GetByIdsAsync(permissionIds, cancellationToken);
        if (permissions.Count != permissionIds.Length)
        {
            return Result.Failure<RoleDto>("یکی از Permissionهای انتخاب‌شده معتبر نیست.");
        }

        _auditScope.OldValues = _mapper.ToRoleDto(role);
        _auditScope.EntityId = role.Id.ToString();

        role.ReplacePermissions(permissionIds, _currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _roles.GetByIdWithPermissionsAsync(role.Id, cancellationToken);
        return Result.Success(_mapper.ToRoleDto(updated ?? role));
    }
}
