using CrmSuite.BuildingBlocks.Domain.Results;
using FluentAssertions;

namespace CrmSuite.UnitTests.Foundation;

public sealed class ResultTests
{
    [Fact]
    public void Success_carries_value_and_message()
    {
        var result = Result.Success(42, "ok");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Message.Should().Be("ok");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_normalizes_errors_and_keeps_message()
    {
        var result = Result.Failure<int>("invalid", "one", "one", "two");

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Be("invalid");
        result.Errors.Should().Equal("one", "two");
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Failure_without_errors_uses_message_as_error()
    {
        var result = Result.Failure("boom");

        result.Errors.Should().ContainSingle().Which.Should().Be("boom");
    }
}
