using System.Globalization;
using System.Text.RegularExpressions;

namespace CrmSuite.Modules.Foundation.Domain.Sequences;

/// <summary>
/// Expands a configured sequence format. Supported tokens:
/// {Prefix}, {Year} (Jalali), {YearGregorian}, {CurrentValue}, {CurrentValue:D5}.
/// </summary>
public static partial class SequenceNumberFormatter
{
    public static string Format(
        string prefix,
        string format,
        long currentValue,
        int jalaliYear,
        int gregorianYear)
    {
        var result = format
            .Replace("{Prefix}", prefix, StringComparison.Ordinal)
            .Replace("{YearGregorian}", gregorianYear.ToString("0000", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{Year}", jalaliYear.ToString("0000", CultureInfo.InvariantCulture), StringComparison.Ordinal);

        return CounterTokenRegex().Replace(result, match =>
        {
            var digits = match.Groups["digits"].Success
                ? int.Parse(match.Groups["digits"].Value, CultureInfo.InvariantCulture)
                : 0;

            return digits > 0
                ? currentValue.ToString($"D{digits}", CultureInfo.InvariantCulture)
                : currentValue.ToString(CultureInfo.InvariantCulture);
        });
    }

    [GeneratedRegex(@"\{CurrentValue(?::D(?<digits>\d+))?\}")]
    private static partial Regex CounterTokenRegex();
}
