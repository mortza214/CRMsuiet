using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Auth;
using CrmSuite.Modules.Identity.Application.Dtos;
using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Identity.Application.Mapping;

public sealed class IdentityDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public IdentityDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public UserDto ToUserDto(User user)
    {
        var createdAt = AsUtc(user.CreatedAt);
        var lockoutEnd = user.LockoutEnd.HasValue ? AsUtc(user.LockoutEnd.Value) : (DateTime?)null;

        return new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Mobile,
            user.Email,
            user.IsActive,
            user.FailedLoginCount,
            lockoutEnd,
            lockoutEnd.HasValue ? _jalaliDate.ToJalali(lockoutEnd.Value) : null,
            user.UserRoles
                .Where(item => item.Role is not null)
                .Select(item => new RoleSummaryDto(item.Role.Id, item.Role.Name, item.Role.DisplayName))
                .OrderBy(item => item.Name)
                .ToArray(),
            user.UserDepartments
                .Where(item => item.Department is not null)
                .Select(item => new DepartmentSummaryDto(item.Department.Id, item.Department.Name))
                .OrderBy(item => item.Name)
                .ToArray(),
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public UserProfileDto ToUserProfile(User user, IReadOnlyCollection<string> permissions)
    {
        return new UserProfileDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Mobile,
            user.Email,
            user.IsActive,
            user.UserRoles
                .Where(item => item.Role is not null)
                .Select(item => new RoleSummaryDto(item.Role.Id, item.Role.Name, item.Role.DisplayName))
                .OrderBy(item => item.Name)
                .ToArray(),
            permissions.Order(StringComparer.Ordinal).ToArray());
    }

    public RoleDto ToRoleDto(Role role)
    {
        var createdAt = AsUtc(role.CreatedAt);

        return new RoleDto(
            role.Id,
            role.Name,
            role.DisplayName,
            role.RolePermissions
                .Where(item => item.Permission is not null)
                .Select(item => ToPermissionDto(item.Permission))
                .OrderBy(item => item.Code, StringComparer.Ordinal)
                .ToArray(),
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public PermissionDto ToPermissionDto(Permission permission) =>
        new(permission.Id, permission.Code, permission.Group, permission.DisplayName);

    public DepartmentDto ToDepartmentDto(Department department)
    {
        var createdAt = AsUtc(department.CreatedAt);

        return new DepartmentDto(
            department.Id,
            department.Name,
            department.ParentId,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public AuthResultDto ToAuthResult(TokenPair tokens, User user, IReadOnlyCollection<string> permissions)
    {
        var accessExpiresAt = AsUtc(tokens.AccessToken.ExpiresAtUtc);
        var refreshExpiresAt = AsUtc(tokens.RefreshToken.ExpiresAtUtc);

        return new AuthResultDto(
            tokens.AccessToken.AccessToken,
            accessExpiresAt,
            _jalaliDate.ToJalali(accessExpiresAt),
            tokens.RefreshToken.Token,
            refreshExpiresAt,
            _jalaliDate.ToJalali(refreshExpiresAt),
            ToUserProfile(user, permissions));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
