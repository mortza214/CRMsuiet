using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Mapping;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Catalog.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Forms.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Infrastructure.Persistence.Repositories;
using CrmSuite.Modules.Forms.Infrastructure.Validation;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Orders.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.IntegrationTests.Catalog;

public sealed class CatalogPricingIntegrationTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Pricing_preview_matches_persisted_order_item_snapshot()
    {
        using var fixture = new Fixture();
        var service = fixture.CreateService(500_000m);
        var addon = fixture.CreateAddon(2_000_000m);
        var storedService = await fixture.DbContext.Set<Service>()
            .SingleAsync(candidate => candidate.Id == service.Id);
        storedService.IsActive.Should().BeTrue();
        storedService.IsDeleted.Should().BeFalse();
        var serviceRepository = new ServiceRepository(fixture.DbContext);
        var loadedById = await serviceRepository.GetByIdAsync(service.Id, CancellationToken.None);
        loadedById.Should().NotBeNull("plain service lookup should find the tracked row");
        var loadedService = await serviceRepository.GetByIdWithPricesAsync(service.Id, CancellationToken.None);
        loadedService.Should().NotBeNull("service with prices should be loadable");
        var pricing = fixture.CreatePricingService();
        var preview = await pricing.CalculateAsync(
            new ServicePricingRequest(
                service.Id,
                20,
                [new AddonSelection(addon.Id, 1)],
                """{"bandwidth_mb":20}"""),
            CancellationToken.None);

        preview.IsSuccess.Should().BeTrue(
            "errors: {0}",
            string.Join(" | ", preview.Errors));
        preview.Value!.Preview.Total.Should().Be(12_000_000m);

        var order = new Order(
            "ORD-CAT-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            OrderType.New,
            null,
            null,
            Now);
        var item = new OrderItem(
            order.Id,
            service.Id,
            service.Name,
            20,
            preview.Value.UnitPriceSnapshot,
            preview.Value.AddonsTotal,
            preview.Value.AddonsJson,
            """{"bandwidth_mb":20}""",
            BillingCycle.Monthly,
            Now);
        order.AddItem(item);
        fixture.DbContext.Set<Order>().Add(order);
        await fixture.DbContext.SaveChangesAsync();

        var saved = await fixture.DbContext.Set<OrderItem>().SingleAsync();
        saved.UnitPriceSnapshot.Should().Be(500_000m);
        saved.EffectivePrice.Should().Be(preview.Value.Preview.Total);
        saved.EffectivePrice.Should().Be(12_000_000m);
        saved.CommissionBase.Should().Be(12_000_000m);
    }

    [Fact]
    public async Task Required_forms_are_returned_for_service()
    {
        using var fixture = new Fixture();
        var service = fixture.CreateService(500_000m);
        var form = new FormTemplate(
            "FORM-CAT-1",
            "فرم نیازمندی",
            FormEntityType.Service,
            null,
            null,
            """{"fields":[{"key":"bandwidth_mb","label":"پهنای باند","type":"number","required":true}]}""",
            1,
            FormDefinitionStatus.Published,
            null,
            Now);
        form.Fields.Add(new FormField(
            form.Id,
            "bandwidth_mb",
            "پهنای باند",
            FormFieldType.Number,
            true,
            null,
            null,
            "1",
            "10000",
            null,
            1,
            null));
        fixture.DbContext.Set<FormTemplate>().Add(form);
        await fixture.DbContext.SaveChangesAsync();

        var link = new ServiceFormLink(service.Id, form.Id, true, 1, null, Now);
        fixture.DbContext.Set<ServiceFormLink>().Add(link);
        await fixture.DbContext.SaveChangesAsync();

        var handler = new GetServiceFormRequirementsQueryHandler(
            new ServiceFormLinkRepository(fixture.DbContext),
            new FormTemplateRepository(fixture.DbContext),
            new CatalogDtoMapper(new JalaliDateService()));
        var result = await handler.Handle(
            new GetServiceFormRequirementsQuery(service.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].IsRequired.Should().BeTrue();
        result.Value[0].FormCode.Should().Be("FORM-CAT-1");
    }

    private sealed class Fixture : IDisposable
    {
        public AppDbContext DbContext { get; }
        private readonly ServiceRepository _services;
        private readonly AddonRepository _addons;
        private readonly EfCatalogUnitOfWork _unitOfWork;

        public Fixture()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"catalog-{Guid.NewGuid():N}")
                .Options;
            DbContext = new AppDbContext(
                options,
                new CrmSuite.BuildingBlocks.Infrastructure.Persistence.IModuleModelConfigurator[]
                {
                    new CatalogModelConfigurator(),
                    new OrdersModelConfigurator(),
                    new FormsModelConfigurator(),
                });
            DbContext.Database.EnsureCreated();
            _services = new ServiceRepository(DbContext);
            _addons = new AddonRepository(DbContext);
            _unitOfWork = new EfCatalogUnitOfWork(DbContext);
        }

        public Service CreateService(decimal price)
        {
            var category = new ServiceCategory($"CAT-{Guid.NewGuid():N}"[..12]);
            DbContext.Set<ServiceCategory>().Add(category);
            DbContext.SaveChanges();
            var service = new Service(
                $"SVC-{Guid.NewGuid():N}"[..20],
                "سرویس تست",
                category.Id,
                ServiceType.BandwidthDedicated,
                ServiceUnit.BandwidthMbps,
                null);
            service.ConfigurePricing(
                PricingMode.UnitBased,
                BillingPeriod.Monthly,
                ServiceUnit.BandwidthMbps,
                2,
                """{"fields":[{"key":"bandwidth_mb","label":"پهنای باند","type":"number","required":true,"min":1}]}""",
                null,
                Now);
            service.AddPrice(price, Now, Now, null);
            service.SetStatus(true, Now, null);
            _services.Add(service);
            _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
            return service;
        }

        public ServiceAddon CreateAddon(decimal price)
        {
            var addon = new ServiceAddon(
                null,
                $"ADD-{Guid.NewGuid():N}"[..10],
                "نصب دکل",
                AddonPricingMode.Fixed,
                ServiceUnit.Piece,
                price,
                true,
                1,
                null,
                null,
                Now);
            _addons.Add(addon);
            _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
            return addon;
        }

        public ServicePricingService CreatePricingService() =>
            new(_services, _addons, new JsonSchemaValidator());

        public void Dispose() => DbContext.Dispose();
    }
}
