using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Mapping;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private static readonly HashSet<string> ProtectedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "SuperAdmin", "SystemAdmin" };

    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly IdentityDtoMapper _mapper;

    public DeleteRoleCommandHandler(
        IRoleRepository roles,
        IUnitOfWork unitOfWork,
        IAuditScope auditScope,
        IdentityDtoMapper mapper)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _mapper = mapper;
    }

    public async Task<Result> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdWithPermissionsAsync(request.Id, cancellationToken);
        if (role is null)
        {
            return Result.Failure("نقش یافت نشد.", "IDENTITY_ROLE_NOT_FOUND");
        }

        if (ProtectedRoles.Contains(role.Name))
        {
            return Result.Failure("نقش سیستمی قابل حذف نیست.", "IDENTITY_ROLE_PROTECTED");
        }

        _auditScope.OldValues = _mapper.ToRoleDto(role);
        _auditScope.EntityId = role.Id.ToString();

        _roles.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("نقش حذف شد.");
    }
}
