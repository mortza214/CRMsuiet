using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence;
using CrmSuite.Modules.Foundation.Infrastructure.Sequences;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.IntegrationTests;

public sealed class SequenceConcurrencyTests
{
    [MySqlFact]
    public async Task One_hundred_parallel_store_allocations_are_unique()
    {
        var connectionString = Environment.GetEnvironmentVariable("CRMSUITE_TEST_MYSQL")!;
        var versionText = Environment.GetEnvironmentVariable("CRMSUITE_TEST_MYSQL_VERSION") ?? "10.11.16";
        var serverVersion = new MariaDbServerVersion(Version.Parse(versionText));
        var sequenceName = $"ConcurrencyTest_{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        await using (var setup = new AppDbContext(options, [new FoundationModelConfigurator()]))
        {
            var sequence = new Sequence(sequenceName, "TST-", "{Prefix}{Year}{CurrentValue:D8}");
            setup.Set<Sequence>().Add(sequence);
            await setup.SaveChangesAsync();
        }

        try
        {
            var tasks = Enumerable.Range(0, 100).Select(async _ =>
            {
                await using var db = new AppDbContext(options, [new FoundationModelConfigurator()]);
                var store = new MySqlSequenceStore(db, new TestUser(), new TestClock());
                var service = new SequenceService(store, new JalaliDateService(), new TestClock());

                return await service.GetNextValueAsync(sequenceName);
            });

            var values = await Task.WhenAll(tasks);

            values.Should().OnlyHaveUniqueItems();
            values.Should().HaveCount(100);
            values.Should().OnlyContain(value => value.StartsWith("TST-", StringComparison.Ordinal));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options, [new FoundationModelConfigurator()]);
            await cleanup.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM sequences WHERE name = {sequenceName}");
        }
    }

    private sealed class TestUser : FoundationCurrentUser
    {
        public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");

        public string? UserName => "integration-test";

        public string? IpAddress => "127.0.0.1";

        public bool IsAuthenticated => true;
    }

    private sealed class TestClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);
    }
}
