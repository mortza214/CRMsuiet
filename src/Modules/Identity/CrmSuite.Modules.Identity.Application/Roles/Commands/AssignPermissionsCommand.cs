using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed record AssignPermissionsCommand(
    Guid RoleId,
    IReadOnlyCollection<Guid> PermissionIds) : IRequest<Result<RoleDto>>;
