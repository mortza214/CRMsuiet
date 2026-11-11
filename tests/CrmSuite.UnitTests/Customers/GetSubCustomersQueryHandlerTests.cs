using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Application.Queries;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Customers;

public sealed class GetSubCustomersQueryHandlerTests
{
    [Fact]
    public async Task Reseller_receives_only_its_own_sub_customers()
    {
        var userId = Guid.NewGuid();
        var reseller = CreateCustomer("09120000002", "0012345679", isReseller: true);
        reseller.SetPortalUserId(userId);

        var child = CreateCustomer("09120000003", "0012345680", parentId: reseller.Id);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByUserIdAsync(
                userId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reseller);
        customers.Setup(repository => repository.GetSubCustomersAsync(
                reseller.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([child]);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(service => service.UserId).Returns(userId);

        var handler = new GetSubCustomersQueryHandler(
            customers.Object,
            currentUser.Object,
            new CustomerDtoMapper(new JalaliDateService()));

        var result = await handler.Handle(new GetSubCustomersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Id.Should().Be(child.Id);
        customers.Verify(
            repository => repository.GetSubCustomersAsync(
                reseller.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Customer CreateCustomer(
        string mobile,
        string nationalId,
        bool isReseller = false,
        Guid? parentId = null) =>
        new(
            CustomerType.Natural,
            "نام",
            "خانوادگی",
            null,
            null,
            nationalId,
            mobile,
            null,
            null,
            null,
            null,
            parentId,
            isReseller,
            null);
}
