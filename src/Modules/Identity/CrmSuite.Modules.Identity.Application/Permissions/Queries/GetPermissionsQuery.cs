using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Permissions.Queries;

public sealed record GetPermissionsQuery : IRequest<Result<IReadOnlyList<PermissionGroupDto>>>;
