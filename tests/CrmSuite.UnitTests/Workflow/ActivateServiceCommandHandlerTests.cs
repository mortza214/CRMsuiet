using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Entities;
using FluentAssertions;
using MediatR;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Workflow;

public sealed class ActivateServiceCommandHandlerTests
{
    [Fact]
    public async Task Activation_sets_order_timestamp_and_creates_snapshot_customer_service()
    {
        var now = new DateTime(2026, 11, 9, 13, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var order = new Order(
            "ORD-ACT-1",
            Guid.NewGuid(),
            userId,
            now,
            OrderType.New,
            null,
            userId,
            now);
        var serviceId = Guid.NewGuid();
        order.AddItem(new OrderItem(
            order.Id,
            serviceId,
            "پهنای باند اختصاصی",
            20,
            500_000m,
            0,
            null,
            """{"bandwidth_mb":20,"substrate":"fiber"}""",
            BillingCycle.Monthly,
            now));
        order.ChangeStatus(OrderStatus.Pending, userId, now, null);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdWithDetailsAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        CustomerService? created = null;
        var customerServices = new Mock<ICustomerServiceRepository>();
        customerServices.Setup(repository => repository.GetByOrderIdAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerService?)null);
        customerServices.Setup(repository => repository.Add(It.IsAny<CustomerService>()))
            .Callback<CustomerService>(service => created = service);

        var service = new Service(
            "BW-ACT",
            "پهنای باند",
            Guid.NewGuid(),
            ServiceType.BandwidthDedicated,
            ServiceUnit.BandwidthMbps,
            null,
            true);
        service.ConfigurePricing(
            PricingMode.UnitBased,
            BillingPeriod.Monthly,
            ServiceUnit.BandwidthMbps,
            2m,
            null,
            null,
            now);
        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetByIdAsync(
                serviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var workflow = new Mock<BandwidthWorkflowSupport>(
            Mock.Of<IWorkflowInstanceRepository>(),
            Mock.Of<IWorkflowDefinitionRepository>(),
            Mock.Of<IMediator>());
        workflow.Setup(support => support.GetActiveAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<(WorkflowInstance Instance, WorkflowDefinition Definition)?>(
                null));

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(now);

        var handler = new ActivateServiceCommandHandler(
            orders.Object,
            customerServices.Object,
            services.Object,
            Mock.Of<IOrderUnitOfWork>(),
            workflow.Object,
            clock.Object,
            currentUser.Object);

        var result = await handler.Handle(
            new ActivateServiceCommand(order.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.ActivatedAt.Should().Be(now);
        order.Status.Should().Be(OrderStatus.Completed);
        created.Should().NotBeNull();
        created!.UnitPriceSnapshot.Should().Be(500_000m);
        created.Quantity.Should().Be(20);
        created.MonthlyAmount.Should().Be(10_000_000m);
        created.CommissionPercent.Should().Be(2m);
    }
}
