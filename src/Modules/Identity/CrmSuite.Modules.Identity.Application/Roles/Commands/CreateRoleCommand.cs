using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed record CreateRoleCommand(
    string Name,
    string DisplayName,
    IReadOnlyCollection<Guid>? PermissionIds = null) : IRequest<Result<RoleDto>>;
