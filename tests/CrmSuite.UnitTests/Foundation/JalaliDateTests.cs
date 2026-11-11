using CrmSuite.BuildingBlocks.Domain.Common;
using FluentAssertions;

namespace CrmSuite.UnitTests.Foundation;

public sealed class JalaliDateTests
{
    [Fact]
    public void Converts_nowruz_boundary_to_jalali()
    {
        var utc = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);

        JalaliDate.Format(utc).Should().Be("1403/01/01");
    }

    [Fact]
    public void Formats_time_in_tehran_timezone()
    {
        var utc = new DateTime(2024, 3, 20, 20, 30, 0, DateTimeKind.Utc);

        JalaliDate.FormatDateTime(utc).Should().Be("1403/01/02 00:00");
    }

    [Fact]
    public void Converts_jalali_parts_back_to_utc()
    {
        var utc = JalaliDate.ToUtc(1403, 1, 1, 12, 0, 0);

        JalaliDate.Format(utc).Should().Be("1403/01/01");
        utc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Converts_digits_to_persian()
    {
        JalaliDate.ToPersianDigits("1403/01/01").Should().Be("۱۴۰۳/۰۱/۰۱");
    }
}
