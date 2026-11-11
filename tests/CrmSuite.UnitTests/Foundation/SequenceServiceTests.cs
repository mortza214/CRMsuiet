using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Sequences;
using CrmSuite.Modules.Foundation.Domain.Sequences;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Foundation.Infrastructure.Sequences;
using FluentAssertions;

namespace CrmSuite.UnitTests.Foundation;

public sealed class SequenceServiceTests
{
    [Fact]
    public async Task One_hundred_concurrent_requests_produce_unique_values()
    {
        var store = new AtomicSequenceStore();
        var service = new SequenceService(
            store,
            new JalaliDateService(),
            new FixedDateTimeProvider(new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc)));

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => service.GetNextValueAsync("InvoiceNumber"))
            .ToArray();

        var values = await Task.WhenAll(tasks);

        values.Should().OnlyHaveUniqueItems();
        values.Should().HaveCount(100);
        values.Should().OnlyContain(value => value.StartsWith("INV-1403", StringComparison.Ordinal));
    }

    [Fact]
    public void Formatter_supports_gregorian_year_and_unpadded_counter()
    {
        var result = SequenceNumberFormatter.Format(
            "ORD-",
            "{Prefix}{YearGregorian}-{CurrentValue}",
            42,
            1403,
            2024);

        result.Should().Be("ORD-2024-42");
    }

    private sealed class AtomicSequenceStore : ISequenceStore
    {
        private long _currentValue;

        public async Task<SequenceAllocation> AllocateNextAsync(
            string sequenceName,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            var value = Interlocked.Increment(ref _currentValue);
            return new SequenceAllocation("INV-", value, "{Prefix}{Year}{CurrentValue:D5}");
        }
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
