using CrmSuite.BuildingBlocks.Application.Abstractions;
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
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Orders;

public sealed class UpdateOrderStatusCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Status_change_is_recorded_in_history()
    {
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

        var order = new Order(
            "ORD-1405-00001",
            customer.Id,
            Guid.NewGuid(),
            Now,
            OrderType.New,
            null,
            Guid.NewGuid(),
            Now);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByIdAsync(
                customer.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var unitOfWork = new Mock<IOrderUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var handler = new UpdateOrderStatusCommandHandler(
            orders.Object,
            unitOfWork.Object,
            customers.Object,
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            new OrderDtoMapper(new JalaliDateService()));

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.PendingPayment, "waiting"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.StatusHistory.Should().ContainSingle();
        order.StatusHistory.Single().FromStatus.Should().Be(OrderStatus.Draft);
        order.StatusHistory.Single().ToStatus.Should().Be(OrderStatus.PendingPayment);
    }

    [Fact]
    public async Task Direct_completion_from_draft_is_rejected()
    {
        var order = new Order(
            "ORD-1405-00002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            OrderType.New,
            null,
            Guid.NewGuid(),
            Now);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var customer = new Customer(
            CustomerType.Natural,
            "علی",
            "تستی",
            null,
            null,
            "0012345679",
            "09120000002",
            null,
            null,
            null,
            null,
            null,
            false,
            null);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByIdAsync(
                order.CustomerId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var handler = new UpdateOrderStatusCommandHandler(
            orders.Object,
            Mock.Of<IOrderUnitOfWork>(),
            customers.Object,
            Mock.Of<IAuditScope>(),
            Mock.Of<FoundationCurrentUser>(),
            Mock.Of<IDateTimeProvider>(),
            new OrderDtoMapper(new JalaliDateService()));

        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.Completed),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("مجاز نیست");
    }
}
