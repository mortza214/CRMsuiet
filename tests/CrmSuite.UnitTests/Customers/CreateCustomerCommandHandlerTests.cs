using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Commands;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Customers;

public sealed class CreateCustomerCommandHandlerTests
{
    [Fact]
    public async Task Duplicate_mobile_is_rejected_before_creating_identity_user()
    {
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.MobileExistsAsync(
                "09120000001",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var identity = new Mock<ICustomerIdentityService>();
        var handler = CreateHandler(customers.Object, identity.Object);

        var result = await handler.Handle(
            new CreateCustomerCommand(
                CustomerType.Natural,
                "علی",
                "تستی",
                null,
                null,
                "0012345678",
                "09120000001",
                null,
                null,
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("موبایل");
        identity.Verify(
            service => service.EnsurePortalUserAsync(
                It.IsAny<CrmSuite.Modules.Customers.Domain.Entities.Customer>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static CreateCustomerCommandHandler CreateHandler(
        ICustomerRepository customers,
        ICustomerIdentityService identity)
    {
        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(service => service.UserId).Returns(Guid.NewGuid());
        currentUser.SetupGet(service => service.IpAddress).Returns("127.0.0.1");

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc));

        return new CreateCustomerCommandHandler(
            customers,
            Mock.Of<ICustomerGroupRepository>(),
            Mock.Of<ICustomerTagRepository>(),
            identity,
            Mock.Of<ICustomerUnitOfWork>(),
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            new CustomerDtoMapper(new JalaliDateService()));
    }
}
