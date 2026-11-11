using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Application.Mapping;
using CrmSuite.Modules.Identity.Domain.Entities;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IdentityDtoMapper _mapper;

    public CreateRoleCommandHandler(
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
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (await _roles.NameExistsAsync(request.Name, null, cancellationToken))
        {
            return Result.Failure<RoleDto>("نام نقش قبلاً استفاده شده است.", "IDENTITY_ROLE_NAME_DUPLICATE");
        }

        var permissionIds = request.PermissionIds?.Distinct().ToArray() ?? [];
        if (permissionIds.Length > 0)
        {
            var permissions = await _permissions.GetByIdsAsync(permissionIds, cancellationToken);
            if (permissions.Count != permissionIds.Length)
            {
                return Result.Failure<RoleDto>("یکی از Permissionهای انتخاب‌شده معتبر نیست.");
            }
        }

        var now = _clock.UtcNow;
        var role = new Role(request.Name, request.DisplayName);
        role.MarkCreated(_currentUser.UserId, now);
        role.ReplacePermissions(permissionIds, _currentUser.UserId, now);
        _roles.Add(role);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = role.Id.ToString();

        var created = await _roles.GetByIdWithPermissionsAsync(role.Id, cancellationToken);
        return Result.Success(_mapper.ToRoleDto(created ?? role));
    }
}
