using System.Globalization;
using CrmSuite.BuildingBlocks.Domain.Common;
using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Application.Abstractions;

namespace CrmSuite.Modules.Foundation.Infrastructure.Dates;

public sealed class JalaliDateService : IJalaliDateService
{
    private static readonly PersianCalendar Calendar = new();

    public string ToJalali(DateTime utc) => JalaliDate.Format(utc, "yyyy/MM/dd");

    public int GetJalaliYear(DateTime utc) => JalaliDate.ToJalaliParts(utc).Year;

    public DateTime ToGregorian(string jalali)
    {
        if (string.IsNullOrWhiteSpace(jalali))
        {
            throw new DomainException("تاریخ شمسی نمی‌تواند خالی باشد.", "FOUNDATION_JALALI_EMPTY");
        }

        var normalized = NormalizeDigits(jalali)
            .Replace('-', '/')
            .Replace('.', '/')
            .Trim();

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day))
        {
            throw new DomainException(
                "قالب تاریخ شمسی باید yyyy/MM/dd باشد.",
                "FOUNDATION_JALALI_INVALID_FORMAT");
        }

        try
        {
            // ToGregorian is a calendar-date conversion, so return the Gregorian date at
            // 00:00 UTC. Business code that needs a Tehran-midnight instant should combine
            // this with TimeZoneInfo.ConvertTimeToUtc.
            var gregorian = Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return DateTime.SpecifyKind(gregorian, DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new DomainException(
                "تاریخ شمسی واردشده معتبر نیست.",
                "FOUNDATION_JALALI_INVALID_DATE",
                new Dictionary<string, string[]> { ["jalali"] = [exception.Message] });
        }
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
}
