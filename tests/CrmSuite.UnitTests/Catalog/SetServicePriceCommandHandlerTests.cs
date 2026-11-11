using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Commands;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Catalog;

public sealed class SetServicePriceCommandHandlerTests
{
    private static readonly DateTime OldStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NewStart = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task New_price_closes_the_previous_open_price()
    {
        var service = new Service(
            "BW-DED-10",
            "پهنای باند اختصاصی ۱۰ مگ",
            Guid.NewGuid(),
            ServiceType.BandwidthDedicated,
            ServiceUnit.BandwidthMbps,
            "تست");

        service.AddPrice(500_000m, OldStart, OldStart, null);

        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetByIdWithPricesAsync(
                service.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(NewStart);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());

        var unitOfWork = new Mock<ICatalogUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SetServicePriceCommandHandler(
            services.Object,
            unitOfWork.Object,
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            new CatalogDtoMapper(new JalaliDateService()));

        var result = await handler.Handle(
            new SetServicePriceCommand(service.Id, 600_000m, NewStart),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Price.Should().Be(600_000m);
        service.Prices.Should().HaveCount(2);
        service.Prices.Single(price => price.EffectiveFrom == OldStart)
            .EffectiveTo.Should().Be(NewStart);
        service.Prices.Single(price => price.EffectiveFrom == NewStart)
            .EffectiveTo.Should().BeNull();
    }
}
