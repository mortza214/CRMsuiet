namespace CrmSuite.Modules.Identity.Application.Dtos;

public sealed record RoleDto(
    Guid Id,
    string Name,
    string DisplayName,
    IReadOnlyList<PermissionDto> Permissions,
    DateTime CreatedAt,
    string CreatedAtJalali);
