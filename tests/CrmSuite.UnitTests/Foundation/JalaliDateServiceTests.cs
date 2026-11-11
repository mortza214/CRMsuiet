using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;

namespace CrmSuite.UnitTests.Foundation;

public sealed class JalaliDateServiceTests
{
    private readonly JalaliDateService _service = new();

    [Fact]
    public void Converts_gregorian_to_jalali_at_nowruz_boundary()
    {
        var utc = new DateTime(2024, 3, 19, 20, 30, 0, DateTimeKind.Utc);

        _service.ToJalali(utc).Should().Be("1403/01/01");
    }

    [Fact]
    public void Converts_jalali_leap_day_to_gregorian()
    {
        var result = _service.ToGregorian("1403/12/30");

        result.Should().Be(new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Converts_next_jalali_year_first_day()
    {
        var result = _service.ToGregorian("1404/01/01");

        result.Should().Be(new DateTime(2025, 3, 21, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Rejects_thirtieth_day_of_non_leap_jalali_year()
    {
        var action = () => _service.ToGregorian("1404/12/30");

        action.Should().Throw<DomainException>()
            .Which.Code.Should().Be("FOUNDATION_JALALI_INVALID_DATE");
    }

    [Fact]
    public void Accepts_persian_digits_and_hyphen_separator()
    {
        var result = _service.ToGregorian("۱۴۰۳-۱۲-۳۰");

        _service.ToJalali(result).Should().Be("1403/12/30");
    }

    [Fact]
    public void Returns_tehran_year_for_sequence_formatting()
    {
        var utc = new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc);

        _service.GetJalaliYear(utc).Should().Be(1403);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1403/01")]
    [InlineData("not-a-date")]
    [InlineData("1403/13/01")]
    public void Rejects_invalid_formats(string value)
    {
        var action = () => _service.ToGregorian(value);

        action.Should().Throw<DomainException>();
    }
}
