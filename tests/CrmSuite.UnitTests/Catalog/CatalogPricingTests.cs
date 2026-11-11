using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Commands;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Schema;
using CrmSuite.Modules.Forms.Infrastructure.Validation;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;
using FluentAssertions;
using Moq;

namespace CrmSuite.UnitTests.Catalog;

public sealed class CatalogPricingTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_price_closes_previous_open_price_atomically()
    {
        var service = CreateService(500_000m);
        var secondDate = Now.AddDays(1);

        service.AddPrice(700_000m, secondDate, Now, null);

        service.Prices.Should().HaveCount(2);
        var first = service.Prices.Single(price => price.Price == 500_000m);
        var second = service.Prices.Single(price => price.Price == 700_000m);
        first.EffectiveTo.Should().Be(secondDate);
        second.EffectiveTo.Should().BeNull();
        service.CurrentOpenPrice!.Price.Should().Be(700_000m);
    }

    [Fact]
    public void Multiple_open_prices_are_rejected()
    {
        var service = CreateService(500_000m);
        service.Prices.Add(new ServicePrice(service.Id, 600_000m, Now.AddDays(1), Now));
        service.Prices.Add(new ServicePrice(service.Id, 700_000m, Now.AddDays(2), Now));

        var action = () => service.AddPrice(800_000m, Now.AddDays(3), Now, null);

        action.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "CATALOG_MULTIPLE_OPEN_PRICES");
    }

    [Fact]
    public async Task Unit_based_price_preview_uses_latest_price_and_quantity()
    {
        var service = CreateService(500_000m);
        var pricing = CreatePricing(service, []);

        var result = await pricing.CalculateAsync(
            new ServicePricingRequest(service.Id, 20, [], null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UnitPriceSnapshot.Should().Be(500_000m);
        result.Value.Preview.Total.Should().Be(10_000_000m);
    }

    [Fact]
    public async Task Addon_total_is_included_in_effective_price()
    {
        var service = CreateService(500_000m);
        var addon = CreateAddon(2_000_000m);
        var pricing = CreatePricing(service, [addon]);

        var result = await pricing.CalculateAsync(
            new ServicePricingRequest(
                service.Id,
                20,
                [new AddonSelection(addon.Id, 1)],
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AddonsTotal.Should().Be(2_000_000m);
        result.Value.Preview.Total.Should().Be(12_000_000m);
        result.Value.AddonsJson.Should().Contain(addon.Id.ToString());
    }

    [Fact]
    public async Task Addon_from_another_service_is_rejected()
    {
        var service = CreateService(500_000m);
        var otherService = CreateService(600_000m);
        var addon = CreateAddon(2_000_000m, otherService.Id);
        var pricing = CreatePricing(service, [addon]);

        var result = await pricing.CalculateAsync(
            new ServicePricingRequest(
                service.Id,
                1,
                [new AddonSelection(addon.Id, 1)],
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("CATALOG_ADDON_SERVICE_MISMATCH");
    }

    [Fact]
    public async Task Invalid_attribute_values_are_rejected()
    {
        var service = CreateService(
            500_000m,
            """{"fields":[{"key":"bandwidth_mb","label":"پهنای باند","type":"number","required":true,"min":1,"max":10000}]}""");
        var pricing = CreatePricing(service, []);

        var result = await pricing.CalculateAsync(
            new ServicePricingRequest(
                service.Id,
                20,
                [],
                """{"bandwidth_mb":"not-a-number"}"""),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("AttributeSchema");
    }

    [Fact]
    public void Override_without_reason_is_rejected()
    {
        var item = new OrderItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "آیتم",
            1,
            500_000m,
            0,
            null,
            null,
            BillingCycle.Monthly,
            Now);

        var action = () => item.OverrideUnitPrice(450_000m, null, null, Now);

        action.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "ORDERS_OVERRIDE_REASON_REQUIRED");
    }

    [Fact]
    public void Effective_price_is_snapshot_times_quantity_plus_addons()
    {
        var item = new OrderItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "آیتم",
            20,
            500_000m,
            2_000_000m,
            "[]",
            "{}",
            BillingCycle.Monthly,
            Now);

        item.EffectivePrice.Should().Be(12_000_000m);
        item.CommissionBase.Should().Be(12_000_000m);
        item.UnitPriceSnapshot.Should().Be(500_000m);
    }

    [Fact]
    public async Task Pricing_mode_cannot_change_when_service_has_active_order()
    {
        var service = CreateService(500_000m);
        var category = new ServiceCategory("اینترنت");
        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetByIdWithPricesAsync(
                service.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        services.Setup(repository => repository.CodeExistsAsync(
                It.IsAny<string>(),
                service.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        var usage = new Mock<IServiceUsageGuard>();
        usage.Setup(repository => repository.HasActiveOrderItemsForServiceAsync(
                service.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);
        var unitOfWork = new Mock<ICatalogUnitOfWork>();

        var handler = new UpdateServiceCommandHandler(
            services.Object,
            categories.Object,
            unitOfWork.Object,
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            new CatalogDtoMapper(new JalaliDateService()),
            usage.Object);

        var result = await handler.Handle(
            new UpdateServiceCommand(
                service.Id,
                service.Code,
                service.Name,
                category.Id,
                service.ServiceType,
                service.Unit,
                null,
                true,
                PricingMode.Fixed),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("CATALOG_PRICING_MODE_LOCKED");
    }

    [Fact]
    public void Invalid_attribute_schema_is_rejected_by_parser()
    {
        var result = FormSchemaParser.Parse("""{"fields":[{"key":"x","label":"X","type":"unknown"}]}""");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("FORMS_FIELD_TYPE_INVALID");
    }

    private static Service CreateService(decimal price, string? attributeSchema = null)
    {
        var service = new Service(
            "SVC-TEST",
            "سرویس تست",
            Guid.NewGuid(),
            ServiceType.BandwidthDedicated,
            ServiceUnit.BandwidthMbps,
            null);
        service.ConfigurePricing(
            PricingMode.UnitBased,
            BillingPeriod.Monthly,
            ServiceUnit.BandwidthMbps,
            2,
            attributeSchema,
            null,
            Now);
        service.AddPrice(price, Now, Now, null);
        service.SetStatus(true, Now, null);
        return service;
    }

    private static ServiceAddon CreateAddon(decimal price, Guid? serviceId = null) =>
        new(
            serviceId,
            $"ADD-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            "نصب دکل",
            AddonPricingMode.Fixed,
            ServiceUnit.Piece,
            price,
            true,
            1,
            null,
            null,
            Now);

    private static ServicePricingService CreatePricing(
        Service service,
        IReadOnlyList<ServiceAddon> addons)
    {
        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetByIdWithPricesAsync(
                service.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var addonRepository = new Mock<IAddonRepository>();
        addonRepository.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                addons.Where(addon => ids.Contains(addon.Id)).ToArray());

        return new ServicePricingService(
            services.Object,
            addonRepository.Object,
            new JsonSchemaValidator());
    }
}
