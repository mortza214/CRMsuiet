using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Customers.Domain.Entities;

public sealed class Customer : BaseEntity
{
    private Customer()
    {
    }

    public Customer(
        CustomerType type,
        string? firstName,
        string? lastName,
        string? companyName,
        string? economicCode,
        string nationalId,
        string mobile,
        string? phone,
        string? email,
        string? address,
        Guid? ownerUserId,
        Guid? parentCustomerId,
        bool isReseller,
        Guid? groupId,
        Guid? userId = null,
        CustomerOwnerType ownerType = CustomerOwnerType.CurrentUser,
        CustomerNotificationChannel preferredNotificationChannel = CustomerNotificationChannel.Sms)
        : base(Guid.NewGuid())
    {
        ApplyProfile(
            type,
            firstName,
            lastName,
            companyName,
            economicCode,
            nationalId,
            mobile,
            phone,
            email,
            address);

        SetRelations(ownerUserId, parentCustomerId, isReseller, groupId, userId, ownerType);
        PreferredNotificationChannel = preferredNotificationChannel;
    }

    public CustomerType Type { get; private set; }

    public CustomerOwnerType OwnerType { get; private set; } = CustomerOwnerType.CurrentUser;

    public Guid? OwnerId => OwnerUserId;

    public CustomerNotificationChannel PreferredNotificationChannel { get; private set; } =
        CustomerNotificationChannel.Sms;

    public string? PasswordHash { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? CompanyName { get; private set; }

    public string? EconomicCode { get; private set; }

    public string NationalId { get; private set; } = string.Empty;

    public string Mobile { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public Guid? ParentCustomerId { get; private set; }

    public bool IsReseller { get; private set; }

    public Guid? GroupId { get; private set; }

    public CustomerGroup? Group { get; private set; }

    public Guid? UserId { get; private set; }

    public ICollection<CustomerTagMap> TagMaps { get; private set; } = new List<CustomerTagMap>();

    public string DisplayName =>
        Type == CustomerType.Legal
            ? CompanyName ?? NationalId
            : $"{FirstName} {LastName}".Trim();

    public bool IsSubCustomer => ParentCustomerId.HasValue;

    public void Update(
        CustomerType type,
        string? firstName,
        string? lastName,
        string? companyName,
        string? economicCode,
        string nationalId,
        string mobile,
        string? phone,
        string? email,
        string? address,
        Guid? ownerUserId,
        Guid? parentCustomerId,
        bool isReseller,
        Guid? groupId,
        Guid? userId,
        DateTime utcNow,
        Guid? updatedByUserId,
        CustomerOwnerType ownerType = CustomerOwnerType.CurrentUser,
        CustomerNotificationChannel preferredNotificationChannel = CustomerNotificationChannel.Sms)
    {
        if (parentCustomerId == Id)
        {
            throw new DomainException("مشتری نمی‌تواند والد خودش باشد.", "CUSTOMERS_SELF_PARENT");
        }

        ApplyProfile(
            type,
            firstName,
            lastName,
            companyName,
            economicCode,
            nationalId,
            mobile,
            phone,
            email,
            address);

        SetRelations(ownerUserId, parentCustomerId, isReseller, groupId, userId, ownerType);
        PreferredNotificationChannel = preferredNotificationChannel;
        MarkUpdated(updatedByUserId, utcNow);
    }

    public void SetReseller(bool isReseller, Guid? parentCustomerId, Guid? updatedByUserId, DateTime utcNow)
    {
        if (parentCustomerId == Id)
        {
            throw new DomainException("مشتری نمی‌تواند والد خودش باشد.", "CUSTOMERS_SELF_PARENT");
        }

        IsReseller = isReseller;
        ParentCustomerId = parentCustomerId;
        MarkUpdated(updatedByUserId, utcNow);
    }

    public void SetPortalUserId(Guid userId) => UserId = userId;

    public void SetPasswordHash(string passwordHash, Guid? userId, DateTime utcNow)
    {
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash;
        MarkUpdated(userId, utcNow);
    }

    public void ReplaceTags(IEnumerable<Guid> tagIds)
    {
        var distinct = tagIds.Distinct().ToArray();
        TagMaps.Clear();

        foreach (var tagId in distinct)
        {
            TagMaps.Add(new CustomerTagMap(Id, tagId));
        }
    }

    public void MarkDeletedBy(Guid? userId, DateTime utcNow) => MarkDeleted(userId, utcNow);

    private void ApplyProfile(
        CustomerType type,
        string? firstName,
        string? lastName,
        string? companyName,
        string? economicCode,
        string nationalId,
        string mobile,
        string? phone,
        string? email,
        string? address)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("نوع مشتری معتبر نیست.", "CUSTOMERS_TYPE_INVALID");
        }

        Type = type;
        FirstName = NormalizeOptional(firstName, 100);
        LastName = NormalizeOptional(lastName, 100);
        CompanyName = NormalizeOptional(companyName, 200);
        EconomicCode = NormalizeOptional(economicCode, 20);
        NationalId = NormalizeNationalId(nationalId);
        Mobile = NormalizeMobile(mobile);
        Phone = NormalizeOptional(phone, 30);
        Email = NormalizeEmail(email);
        Address = NormalizeOptional(address, 500);

        if (Type == CustomerType.Natural && (FirstName is null || LastName is null))
        {
            throw new DomainException(
                "برای مشتری حقیقی، نام و نام خانوادگی الزامی است.",
                "CUSTOMERS_NATURAL_NAME_REQUIRED");
        }

        if (Type == CustomerType.Legal && CompanyName is null)
        {
            throw new DomainException(
                "برای مشتری حقوقی، نام شرکت الزامی است.",
                "CUSTOMERS_LEGAL_COMPANY_REQUIRED");
        }
    }

    private void SetRelations(
        Guid? ownerUserId,
        Guid? parentCustomerId,
        bool isReseller,
        Guid? groupId,
        Guid? userId,
        CustomerOwnerType ownerType = CustomerOwnerType.CurrentUser)
    {
        if (parentCustomerId == Id)
        {
            throw new DomainException("مشتری نمی‌تواند والد خودش باشد.", "CUSTOMERS_SELF_PARENT");
        }

        OwnerUserId = ownerUserId;
        ParentCustomerId = parentCustomerId;
        IsReseller = isReseller;
        GroupId = groupId;
        UserId = userId;
        OwnerType = ownerType;
    }

    public void SetOwnerType(CustomerOwnerType ownerType, Guid? userId, DateTime utcNow)
    {
        OwnerType = ownerType;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeNationalId(string nationalId)
    {
        var normalized = NormalizeDigits(nationalId).Trim();
        if (normalized.Length is not (10 or 11) || !normalized.All(char.IsDigit))
        {
            throw new DomainException(
                "کد ملی باید ۱۰ رقم و شناسه ملی حقوقی ۱۱ رقم باشد.",
                "CUSTOMERS_NATIONAL_ID_INVALID");
        }

        return normalized;
    }

    private static string NormalizeMobile(string mobile)
    {
        var normalized = NormalizeDigits(mobile).Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^09\\d{9}$"))
        {
            throw new DomainException(
                "موبایل باید با 09 شروع شده و ۱۱ رقم باشد.",
                "CUSTOMERS_MOBILE_INVALID");
        }

        return normalized;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeDigits(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];

        for (var index = 0; index < value.Length; index++)
        {
            buffer[index] = value[index] switch
            {
                >= '۰' and <= '۹' => (char)('0' + (value[index] - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (value[index] - '٠')),
                _ => value[index],
            };
        }

        return new string(buffer);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
