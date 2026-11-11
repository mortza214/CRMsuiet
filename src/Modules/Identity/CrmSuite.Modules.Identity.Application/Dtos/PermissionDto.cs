namespace CrmSuite.Modules.Identity.Application.Dtos;

public sealed record PermissionDto(Guid Id, string Code, string Group, string DisplayName);

public sealed record PermissionGroupDto(string Group, IReadOnlyList<PermissionDto> Permissions);
