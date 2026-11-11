namespace CrmSuite.Modules.Identity.Api.Contracts;

public sealed class CreateUserRequest
{
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Mobile { get; init; } = string.Empty;

    public string? Email { get; init; }

    public bool IsActive { get; init; } = true;

    public List<Guid>? RoleIds { get; init; }

    public List<Guid>? DepartmentIds { get; init; }
}

public sealed class UpdateUserRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Mobile { get; init; } = string.Empty;

    public string? Email { get; init; }

    public bool IsActive { get; init; } = true;

    public string? NewPassword { get; init; }

    public List<Guid>? RoleIds { get; init; }

    public List<Guid>? DepartmentIds { get; init; }
}

public sealed class AssignRolesRequest
{
    public List<Guid> RoleIds { get; init; } = [];
}
