using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Commands;
using CrmSuite.Modules.Orders.Application.Mapping;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using FluentAssertions;
using MediatR;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Orders;

public sealed class CreateOrderCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Prices_are_resolved_from_catalog_and_total_is_calculated()
    {
        var categoryId = Guid.NewGuid();
        var service = new Service(
            "BW-DED-10",
            "پهنای باند اختصاصی ۱۰ مگ",
            categoryId,
            ServiceType.BandwidthDedicated,
            ServiceUnit.BandwidthMbps,
            null,
            isActive: true);

        var addon = new ServiceAddon("نصب دکل", 1_000_000m, ServiceUnit.Piece, true);
        var customer = new Customer(
            CustomerType.Natural,
            "علی",
            "تستی",
            null,
            null,
            "0012345678",
            "09120000001",
            null,
            null,
            null,
            null,
            null,
            false,
            null);

        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var addons = new Mock<IAddonRepository>();
        addons.Setup(repository => repository.GetByIdAsync(addon.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addon);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByIdAsync(
                customer.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetEffectivePriceQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ServicePriceDto(
                Guid.NewGuid(),
                500_000m,
                Now,
                "1405/02/20",
                null,
                null)));

        var orders = new Mock<IOrderRepository>();
        Order? saved = null;
        orders.Setup(repository => repository.Add(It.IsAny<Order>()))
            .Callback<Order>(order => saved = order);
        orders.Setup(repository => repository.GetByIdWithDetailsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved!);

        var sequences = new Mock<ISequenceService>();
        sequences.Setup(service => service.GetNextValueAsync("OrderNumber", It.IsAny<CancellationToken>()))
            .ReturnsAsync("ORD-1405-00001");

        var unitOfWork = new Mock<IOrderUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var handler = new CreateOrderCommandHandler(
            orders.Object,
            unitOfWork.Object,
            mediator.Object,
            customers.Object,
            services.Object,
            addons.Object,
            Mock.Of<IDataCenterItemRepository>(),
            sequences.Object,
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            new OrderDtoMapper(new JalaliDateService()));

        var result = await handler.Handle(
            new CreateOrderCommand(
                customer.Id,
                OrderType.New,
                Now,
                null,
                [
                    new OrderItemInput(service.Id, null, null, 10, BillingCycle.Monthly),
                    new OrderItemInput(null, null, addon.Id, 1, BillingCycle.OneTime),
                ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.TotalAmount.Should().Be(6_000_000m);
        saved.Items.Should().HaveCount(2);
        saved.Items.Single(item => item.ServiceId == service.Id).UnitPrice.Should().Be(500_000m);
        saved.Items.Single(item => item.AddonId == addon.Id).UnitPrice.Should().Be(1_000_000m);
        result.Value!.OrderNumber.Should().Be("ORD-1405-00001");
    }
}
