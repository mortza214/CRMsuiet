using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Roles.Queries;

public sealed record GetRolesQuery : IRequest<Result<IReadOnlyList<RoleDto>>>;

public sealed record GetRoleByIdQuery(Guid Id) : IRequest<Result<RoleDto>>;
