using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Identity.Domain.Entities;

public sealed class User : BaseEntity
{
    private User()
    {
    }

    public User(
        string username,
        string passwordHash,
        string fullName,
        string mobile,
        string? email,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        Username = NormalizeUsername(username);
        PasswordHash = NormalizePasswordHash(passwordHash);
        FullName = NormalizeFullName(fullName);
        Mobile = NormalizeMobile(mobile);
        Email = NormalizeEmail(email);
        IsActive = isActive;
    }

    public string Username { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string Mobile { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public Guid? CustomerId { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? LockoutEnd { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public ICollection<UserDepartment> UserDepartments { get; private set; } = new List<UserDepartment>();

    public bool IsLockedOut(DateTime utcNow) =>
        LockoutEnd.HasValue && LockoutEnd.Value > utcNow;

    public void UpdateProfile(
        string fullName,
        string mobile,
        string? email,
        bool isActive,
        Guid? userId,
        DateTime utcNow)
    {
        FullName = NormalizeFullName(fullName);
        Mobile = NormalizeMobile(mobile);
        Email = NormalizeEmail(email);
        IsActive = isActive;
        MarkUpdated(userId, utcNow);
    }

    public void SetCustomerId(Guid? customerId, Guid? userId, DateTime utcNow)
    {
        CustomerId = customerId;
        MarkUpdated(userId, utcNow);
    }

    public void SetPasswordHash(string passwordHash, Guid? userId, DateTime utcNow)
    {
        PasswordHash = NormalizePasswordHash(passwordHash);
        MarkUpdated(userId, utcNow);
    }

    public void Activate(Guid? userId, DateTime utcNow)
    {
        IsActive = true;
        MarkUpdated(userId, utcNow);
    }

    public void Deactivate(Guid? userId, DateTime utcNow)
    {
        IsActive = false;
        MarkUpdated(userId, utcNow);
    }

    public void RegisterFailedLogin(
        DateTime utcNow,
        int maxAttempts = 5,
        TimeSpan? lockoutDuration = null)
    {
        if (IsLockedOut(utcNow))
        {
            return;
        }

        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
        {
            LockoutEnd = utcNow.Add(lockoutDuration ?? TimeSpan.FromMinutes(15));
        }
    }

    public void RegisterSuccessfulLogin(Guid? userId, DateTime utcNow)
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        MarkUpdated(userId ?? Id, utcNow);
    }

    public void ReplaceRoles(IEnumerable<Guid> roleIds, Guid? userId, DateTime utcNow)
    {
        var distinctRoleIds = roleIds.Distinct().ToArray();
        UserRoles.Clear();

        foreach (var roleId in distinctRoleIds)
        {
            UserRoles.Add(new UserRole(Id, roleId));
        }

        MarkUpdated(userId, utcNow);
    }

    public void ReplaceDepartments(IEnumerable<Guid> departmentIds, Guid? userId, DateTime utcNow)
    {
        var distinctDepartmentIds = departmentIds.Distinct().ToArray();
        UserDepartments.Clear();

        foreach (var departmentId in distinctDepartmentIds)
        {
            UserDepartments.Add(new UserDepartment(Id, departmentId));
        }

        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("نام کاربری نمی‌تواند خالی باشد.", "IDENTITY_USERNAME_REQUIRED");
        }

        var normalized = username.Trim().ToLowerInvariant();
        if (normalized.Length > 100)
        {
            throw new DomainException("نام کاربری حداکثر ۱۰۰ کاراکتر است.", "IDENTITY_USERNAME_TOO_LONG");
        }

        return normalized;
    }

    private static string NormalizePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("هش رمز عبور نمی‌تواند خالی باشد.", "IDENTITY_PASSWORD_HASH_REQUIRED");
        }

        return passwordHash;
    }

    private static string NormalizeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("نام کامل نمی‌تواند خالی باشد.", "IDENTITY_FULLNAME_REQUIRED");
        }

        var normalized = fullName.Trim();
        if (normalized.Length > 200)
        {
            throw new DomainException("نام کامل حداکثر ۲۰۰ کاراکتر است.", "IDENTITY_FULLNAME_TOO_LONG");
        }

        return normalized;
    }

    private static string NormalizeMobile(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            throw new DomainException("موبایل نمی‌تواند خالی باشد.", "IDENTITY_MOBILE_REQUIRED");
        }

        var normalized = mobile.Trim();
        if (normalized.Length > 20)
        {
            throw new DomainException("موبایل حداکثر ۲۰ کاراکتر است.", "IDENTITY_MOBILE_TOO_LONG");
        }

        return normalized;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToLowerInvariant();
        return normalized.Length > 256 ? normalized[..256] : normalized;
    }
}
