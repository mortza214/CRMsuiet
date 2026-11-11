namespace CrmSuite.Modules.Identity.Application.Dtos;

public sealed record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string Mobile,
    string? Email,
    bool IsActive,
    int FailedLoginCount,
    DateTime? LockoutEnd,
    string? LockoutEndJalali,
    IReadOnlyList<RoleSummaryDto> Roles,
    IReadOnlyList<DepartmentSummaryDto> Departments,
    DateTime CreatedAt,
    string CreatedAtJalali);
