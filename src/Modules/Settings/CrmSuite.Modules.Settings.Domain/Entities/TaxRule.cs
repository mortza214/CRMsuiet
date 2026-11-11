using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Settings.Domain.Entities;

public sealed class TaxRule : BaseEntity
{
    private TaxRule()
    {
    }

    public TaxRule(
        string name,
        decimal percentage,
        bool isActive,
        DateTime effectiveFromUtc,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام قانون مالیاتی الزامی است.", "SETTINGS_TAX_NAME_REQUIRED");
        }

        if (percentage < 0 || percentage > 100)
        {
            throw new DomainException("درصد مالیات باید بین ۰ تا ۱۰۰ باشد.", "SETTINGS_TAX_PERCENTAGE_INVALID");
        }

        Name = name.Trim();
        Percentage = percentage;
        IsActive = isActive;
        EffectiveFrom = DateTime.SpecifyKind(effectiveFromUtc, DateTimeKind.Utc);
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string Name { get; private set; } = string.Empty;

    public decimal Percentage { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime EffectiveFrom { get; private set; }

    public void Update(
        string name,
        decimal percentage,
        bool isActive,
        DateTime effectiveFromUtc,
        Guid? userId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام قانون مالیاتی الزامی است.", "SETTINGS_TAX_NAME_REQUIRED");
        }

        if (percentage < 0 || percentage > 100)
        {
            throw new DomainException("درصد مالیات باید بین ۰ تا ۱۰۰ باشد.", "SETTINGS_TAX_PERCENTAGE_INVALID");
        }

        Name = name.Trim();
        Percentage = percentage;
        IsActive = isActive;
        EffectiveFrom = DateTime.SpecifyKind(effectiveFromUtc, DateTimeKind.Utc);
        MarkUpdated(userId, utcNow);
    }
}
