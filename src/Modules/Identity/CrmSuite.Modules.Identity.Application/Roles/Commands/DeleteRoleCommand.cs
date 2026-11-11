using CrmSuite.BuildingBlocks.Domain.Results;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed record DeleteRoleCommand(Guid Id) : IRequest<Result>;
