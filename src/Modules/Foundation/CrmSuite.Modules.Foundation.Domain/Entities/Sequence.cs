using CrmSuite.BuildingBlocks.Domain.Exceptions;

namespace CrmSuite.Modules.Foundation.Domain.Entities;

public sealed class Sequence : BaseEntity
{
    private Sequence()
    {
    }

    public Sequence(string name, string prefix, string format, long currentValue = 0)
        : base(Guid.NewGuid())
    {
        Name = NormalizeName(name);
        Prefix = prefix ?? string.Empty;
        Format = NormalizeFormat(format);
        CurrentValue = currentValue;
    }

    public string Name { get; private set; } = string.Empty;

    public string Prefix { get; private set; } = string.Empty;

    public long CurrentValue { get; private set; }

    public string Format { get; private set; } = "{Prefix}{Year}{CurrentValue:D5}";

    public long AllocateNext() => ++CurrentValue;

    public void ChangeConfiguration(string prefix, string format)
    {
        Prefix = prefix ?? string.Empty;
        Format = NormalizeFormat(format);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("نام Sequence نمی‌تواند خالی باشد.", "FOUNDATION_SEQUENCE_NAME_REQUIRED");
        }

        var normalized = name.Trim();
        if (normalized.Length > 100)
        {
            throw new DomainException("نام Sequence حداکثر ۱۰۰ کاراکتر است.", "FOUNDATION_SEQUENCE_NAME_TOO_LONG");
        }

        return normalized;
    }

    private static string NormalizeFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new DomainException("قالب Sequence نمی‌تواند خالی باشد.", "FOUNDATION_SEQUENCE_FORMAT_REQUIRED");
        }

        if (!format.Contains("{CurrentValue", StringComparison.Ordinal))
        {
            throw new DomainException(
                "قالب Sequence باید شامل {CurrentValue} باشد.",
                "FOUNDATION_SEQUENCE_FORMAT_WITHOUT_COUNTER");
        }

        return format.Trim();
    }
}
