using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Moq;

namespace CrmSuite.UnitTests.Catalog;

public sealed class GetEffectivePriceQueryHandlerTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly DateTime JanStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FebStart = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Returns_the_price_that_is_effective_on_the_requested_date()
    {
        var oldPrice = new ServicePrice(ServiceId, 500_000m, JanStart, JanStart);
        oldPrice.Close(FebStart);
        var newPrice = new ServicePrice(ServiceId, 600_000m, FebStart, FebStart);
        var prices = new[] { oldPrice, newPrice };

        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetEffectivePriceAsync(
                ServiceId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, DateTime date, CancellationToken _) =>
                prices.SingleOrDefault(price => price.IsEffectiveOn(date)));

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc));

        var handler = new GetEffectivePriceQueryHandler(
            services.Object,
            clock.Object,
            new CatalogDtoMapper(new JalaliDateService()));

        var january = await handler.Handle(
            new GetEffectivePriceQuery(ServiceId, new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        var february = await handler.Handle(
            new GetEffectivePriceQuery(ServiceId, new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        january.IsSuccess.Should().BeTrue();
        january.Value!.Price.Should().Be(500_000m);
        february.IsSuccess.Should().BeTrue();
        february.Value!.Price.Should().Be(600_000m);
    }

    [Fact]
    public async Task Returns_failure_when_no_price_exists_on_the_date()
    {
        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetEffectivePriceAsync(
                ServiceId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServicePrice?)null);

        var handler = new GetEffectivePriceQueryHandler(
            services.Object,
            Mock.Of<IDateTimeProvider>(),
            new CatalogDtoMapper(new JalaliDateService()));

        var result = await handler.Handle(
            new GetEffectivePriceQuery(ServiceId, JanStart.AddMonths(-1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
