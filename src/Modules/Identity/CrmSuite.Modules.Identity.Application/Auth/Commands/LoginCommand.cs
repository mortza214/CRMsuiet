using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Auth.Commands;

public sealed record LoginCommand(string Username, string Password) : IRequest<Result<AuthResultDto>>;
