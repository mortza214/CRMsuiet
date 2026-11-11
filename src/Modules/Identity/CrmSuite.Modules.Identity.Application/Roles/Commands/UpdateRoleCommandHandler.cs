using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<RoleDto>>
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public UpdateRoleCommandHandler(
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
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdWithPermissionsAsync(request.Id, cancellationToken);
        if (role is null)
        {
            return Result.Failure<RoleDto>("نقش یافت نشد.", "IDENTITY_ROLE_NOT_FOUND");
        }

        if (await _roles.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            return Result.Failure<RoleDto>("نام نقش قبلاً استفاده شده است.", "IDENTITY_ROLE_NAME_DUPLICATE");
        }

        var permissionIds = request.PermissionIds?.Distinct().ToArray();
        if (permissionIds is { Length: > 0 })
        {
            var permissions = await _permissions.GetByIdsAsync(permissionIds, cancellationToken);
            if (permissions.Count != permissionIds.Length)
            {
                return Result.Failure<RoleDto>("یکی از Permissionهای انتخاب‌شده معتبر نیست.");
            }
        }

        _auditScope.OldValues = _mapper.ToRoleDto(role);
        _auditScope.EntityId = role.Id.ToString();

        var now = _clock.UtcNow;
        role.Rename(request.Name, request.DisplayName, _currentUser.UserId, now);

        if (permissionIds is not null)
        {
            role.ReplacePermissions(permissionIds, _currentUser.UserId, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _roles.GetByIdWithPermissionsAsync(role.Id, cancellationToken);

        return Result.Success(_mapper.ToRoleDto(updated ?? role));
    }
}
