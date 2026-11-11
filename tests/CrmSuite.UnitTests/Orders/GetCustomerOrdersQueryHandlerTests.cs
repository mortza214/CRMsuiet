using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Orders;

public sealed class GetCustomerOrdersQueryHandlerTests
{
    [Fact]
    public async Task Portal_query_is_scoped_to_the_current_customer()
    {
        var userId = Guid.NewGuid();
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
            null,
            userId);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByUserIdAsync(
                userId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var orders = new List<OrderListItemDto>
        {
            new(
                Guid.NewGuid(),
                "ORD-1405-00001",
                customer.Id,
                customer.DisplayName,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "1405/08/16",
                "Draft",
                "New",
                1_000_000m,
                0m,
                1_000_000m,
                1),
        };

        var readRepository = new Mock<IOrderReadRepository>();
        readRepository.Setup(repository => repository.GetCustomerOrdersAsync(
                customer.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);

        var handler = new GetCustomerOrdersQueryHandler(
            customers.Object,
            readRepository.Object,
            currentUser.Object);

        var result = await handler.Handle(new GetCustomerOrdersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        readRepository.Verify(
            repository => repository.GetCustomerOrdersAsync(
                customer.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
