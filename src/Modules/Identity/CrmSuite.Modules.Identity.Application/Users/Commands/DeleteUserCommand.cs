using CrmSuite.BuildingBlocks.Domain.Results;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed record DeleteUserCommand(Guid Id) : IRequest<Result>;
