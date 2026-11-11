using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Users.Queries;

public sealed record GetCurrentUserQuery : IRequest<Result<UserProfileDto>>;
