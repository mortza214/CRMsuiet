using System.Globalization;

namespace CrmSuite.BuildingBlocks.Domain.Common;

/// <summary>
/// UTC is the storage/transport format. This helper only converts at the presentation boundary.
/// </summary>
public static class JalaliDate
{
    private static readonly PersianCalendar Calendar = new();
    private static readonly TimeZoneInfo TehranTimeZone = ResolveTehranTimeZone();

    public static DateTime ToTehran(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TehranTimeZone);

    public static string Format(DateTime utc, string format = "yyyy/MM/dd", bool usePersianDigits = false)
    {
        var local = ToTehran(utc);
        var year = Calendar.GetYear(local);
        var month = Calendar.GetMonth(local);
        var day = Calendar.GetDayOfMonth(local);

        var result = format
            .Replace("yyyy", year.ToString("0000", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("MM", month.ToString("00", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("dd", day.ToString("00", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("HH", local.Hour.ToString("00", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("mm", local.Minute.ToString("00", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("ss", local.Second.ToString("00", CultureInfo.InvariantCulture), StringComparison.Ordinal);

        return usePersianDigits ? ToPersianDigits(result) : result;
    }

    public static string FormatDateTime(DateTime utc, bool usePersianDigits = false) =>
        Format(utc, "yyyy/MM/dd HH:mm", usePersianDigits);

    public static (int Year, int Month, int Day) ToJalaliParts(DateTime utc)
    {
        var local = ToTehran(utc);
        return (Calendar.GetYear(local), Calendar.GetMonth(local), Calendar.GetDayOfMonth(local));
    }

    public static DateTime ToUtc(
        int year,
        int month,
        int day,
        int hour = 0,
        int minute = 0,
        int second = 0)
    {
        var local = Calendar.ToDateTime(year, month, day, hour, minute, second, 0);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TehranTimeZone);
    }

    public static string ToPersianDigits(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];

        for (var index = 0; index < value.Length; index++)
        {
            buffer[index] = value[index] switch
            {
                >= '0' and <= '9' => (char)('۰' + (value[index] - '0')),
                _ => value[index],
            };
        }

        return new string(buffer);
    }

    private static TimeZoneInfo ResolveTehranTimeZone()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next platform-specific identifier.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next platform-specific identifier.
            }
        }

        return TimeZoneInfo.Utc;
    }
}
