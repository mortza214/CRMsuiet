using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Identity.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed record CreateUserCommand(
    string Username,
    string Password,
    string FullName,
    string Mobile,
    string? Email,
    bool IsActive = true,
    IReadOnlyCollection<Guid>? RoleIds = null,
    IReadOnlyCollection<Guid>? DepartmentIds = null) : IRequest<Result<UserDto>>;
