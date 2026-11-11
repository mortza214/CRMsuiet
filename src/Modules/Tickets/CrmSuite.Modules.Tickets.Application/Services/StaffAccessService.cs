using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Tickets.Domain.Entities;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Tickets.Application.Services;

public sealed record StaffAccess(
    Guid UserId,
    bool IsAdmin,
    IReadOnlyCollection<Guid> DepartmentIds)
{
    public bool CanAccessDepartment(Guid departmentId) =>
        IsAdmin || DepartmentIds.Contains(departmentId);
}

public sealed class StaffAccessService
{
    private readonly IUserRepository _users;
    private readonly FoundationCurrentUser _currentUser;

    public StaffAccessService(IUserRepository users, FoundationCurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<StaffAccess?> GetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return null;
        }

        var user = await _users.GetByIdWithAccessAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var isAdmin = user.UserRoles.Any(userRole =>
            userRole.Role.Name is "SuperAdmin" or "SystemAdmin");

        return new StaffAccess(
            user.Id,
            isAdmin,
            user.UserDepartments.Select(userDepartment => userDepartment.DepartmentId).ToArray());
    }

    public async Task<bool> IsInDepartmentAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithAccessAsync(userId, cancellationToken);

        return user is not null
               && user.UserDepartments.Any(userDepartment => userDepartment.DepartmentId == departmentId);
    }
}
