namespace CrmSuite.Modules.Identity.Api.Contracts;

public sealed class CreateRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public List<Guid>? PermissionIds { get; init; }
}

public sealed class UpdateRoleRequest
{
    public string Name { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public List<Guid>? PermissionIds { get; init; }
}

public sealed class AssignPermissionsRequest
{
    public List<Guid> PermissionIds { get; init; } = [];
}
