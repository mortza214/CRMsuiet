using CrmSuite.BuildingBlocks.Domain.Exceptions;

namespace CrmSuite.Modules.Foundation.Domain.Entities;

public sealed class Setting : BaseEntity
{
    private Setting()
    {
    }

    public Setting(string key, string value, string group, bool isSecret)
        : base(Guid.NewGuid())
    {
        Key = NormalizeKey(key);
        Value = value ?? string.Empty;
        Group = NormalizeGroup(group);
        IsSecret = isSecret;
    }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public string Group { get; private set; } = "General";

    public bool IsSecret { get; private set; }

    public void Update(
        string value,
        string group,
        bool isSecret,
        Guid? userId,
        DateTime utcNow)
    {
        Value = value ?? string.Empty;
        Group = NormalizeGroup(group);
        IsSecret = isSecret;
        MarkUpdated(userId, utcNow);
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("کلید تنظیمات نمی‌تواند خالی باشد.", "FOUNDATION_SETTING_KEY_REQUIRED");
        }

        var normalized = key.Trim();
        if (normalized.Length > 200)
        {
            throw new DomainException("کلید تنظیمات حداکثر ۲۰۰ کاراکتر است.", "FOUNDATION_SETTING_KEY_TOO_LONG");
        }

        return normalized;
    }

    private static string NormalizeGroup(string? group) =>
        string.IsNullOrWhiteSpace(group) ? "General" : group.Trim();
}
