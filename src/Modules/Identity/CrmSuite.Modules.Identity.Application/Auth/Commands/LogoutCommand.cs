using CrmSuite.BuildingBlocks.Domain.Results;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
