namespace CrmSuite.Modules.Identity.Application.Dtos;

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string FullName,
    string Mobile,
    string? Email,
    bool IsActive,
    IReadOnlyList<RoleSummaryDto> Roles,
    IReadOnlyList<string> Permissions);
