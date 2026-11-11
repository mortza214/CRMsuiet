using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed record AssignRolesCommand(
    Guid UserId,
    IReadOnlyCollection<Guid> RoleIds) : IRequest<Result<UserDto>>;
