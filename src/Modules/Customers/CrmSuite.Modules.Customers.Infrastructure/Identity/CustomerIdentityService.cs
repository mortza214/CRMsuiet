using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Auth;
using CrmSuite.Modules.Identity.Domain.Entities;

namespace CrmSuite.Modules.Customers.Infrastructure.Identity;

public sealed class CustomerIdentityService : ICustomerIdentityService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPermissionProvider _permissionProvider;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _clock;
    private readonly IJalaliDateService _jalaliDate;

    public CustomerIdentityService(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IPermissionProvider permissionProvider,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider clock,
        IJalaliDateService jalaliDate)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _permissionProvider = permissionProvider;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _jalaliDate = jalaliDate;
    }

    public async Task<Result<Guid>> EnsurePortalUserAsync(
        Customer customer,
        CancellationToken cancellationToken)
    {
        if (customer.UserId.HasValue)
        {
            return Result.Success(customer.UserId.Value);
        }

        var existing = await _users.GetByUsernameWithAccessAsync(customer.Mobile, cancellationToken);
        if (existing is not null)
        {
            var isCustomer = existing.UserRoles
                .Any(userRole => userRole.Role is not null
                                 && userRole.Role.Name == Roles.Customer);

            return isCustomer
                ? Result.Success(existing.Id)
                : Result.Failure<Guid>(
                    "این موبایل قبلاً برای یک کاربر کارمند استفاده شده است.",
                    "CUSTOMERS_MOBILE_USED_BY_STAFF");
        }

        if (await _users.MobileExistsAsync(customer.Mobile, null, cancellationToken))
        {
            return Result.Failure<Guid>(
                "این موبایل قبلاً برای یک کاربر دیگر ثبت شده است.",
                "CUSTOMERS_MOBILE_USED_BY_USER");
        }

        var allRoles = await _roles.GetAllAsync(includePermissions: false, cancellationToken);
        var customerRole = allRoles.SingleOrDefault(role => role.Name == Roles.Customer);

        if (customerRole is null)
        {
            return Result.Failure<Guid>(
                "نقش Customer در سیستم تعریف نشده است.",
                "CUSTOMERS_CUSTOMER_ROLE_MISSING");
        }

        var roleIds = new List<Guid> { customerRole.Id };

        if (customer.IsReseller)
        {
            var resellerRole = allRoles.SingleOrDefault(role => role.Name == Roles.Reseller);
            if (resellerRole is not null)
            {
                roleIds.Add(resellerRole.Id);
            }
        }

        var now = _clock.UtcNow;
        var defaultPassword = customer.Type == CustomerType.Natural
                              && !string.IsNullOrWhiteSpace(customer.NationalId)
            ? customer.NationalId
            : "123456";
        var user = new User(
            customer.Mobile,
            _passwordHasher.Hash(defaultPassword),
            customer.DisplayName,
            customer.Mobile,
            customer.Email);

        user.MarkCreated(null, now);
        user.SetCustomerId(customer.Id, null, now);
        user.ReplaceRoles(roleIds, null, now);
        _users.Add(user);

        return Result.Success(user.Id);
    }

    public async Task<Result<CustomerTokenDto>> IssueTokenAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithAccessAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<CustomerTokenDto>(
                "کاربر پورتال فعال نیست.",
                "CUSTOMERS_PORTAL_USER_INACTIVE");
        }

        var roles = user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (!roles.Contains(Roles.Customer, StringComparer.Ordinal))
        {
            return Result.Failure<CustomerTokenDto>(
                "کاربر پورتال نقش Customer ندارد.",
                "CUSTOMERS_PORTAL_ROLE_MISSING");
        }

        var permissions = await _permissionProvider.GetPermissionsAsync(user.Id, cancellationToken);
        var tokens = _jwtTokenService.CreateTokens(user, roles, permissions);
        var expiresAt = tokens.AccessToken.ExpiresAtUtc;

        return Result.Success(new CustomerTokenDto(
            tokens.AccessToken.AccessToken,
            expiresAt,
            _jalaliDate.ToJalali(expiresAt),
            roles));
    }
}
