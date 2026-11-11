using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Commands;
using CrmSuite.Modules.Customers.Application.Options;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Customers;

public sealed class OtpIdempotencyTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Same_correlation_id_does_not_send_another_sms()
    {
        var customer = CreateCustomer();
        var existing = new CustomerOtp(
            customer.Mobile,
            "123456",
            Now.AddMinutes(2),
            "127.0.0.1",
            Now,
            "correlation-1");

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByMobileAsync(
                customer.Mobile,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var otps = new Mock<ICustomerOtpRepository>();
        otps.Setup(repository => repository.GetByCorrelationIdAsync(
                "correlation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sender = new Mock<IOtpSender>();
        var unitOfWork = new Mock<ICustomerUnitOfWork>();
        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.IpAddress).Returns("127.0.0.1");
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var handler = new RequestOtpCommandHandler(
            customers.Object,
            otps.Object,
            Mock.Of<ICustomerIdentityService>(),
            sender.Object,
            unitOfWork.Object,
            currentUser.Object,
            clock.Object,
            new JalaliDateService(),
            Options.Create(new OtpOptions()));

        var result = await handler.Handle(
            new RequestOtpCommand(customer.Mobile, "correlation-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CorrelationId.Should().Be("correlation-1");
        sender.Verify(service => service.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        otps.Verify(repository => repository.Add(It.IsAny<CustomerOtp>()), Times.Never);
    }

    private static Customer CreateCustomer() =>
        new(
            CustomerType.Natural,
            "آزمون",
            "SMS",
            null,
            null,
            "0012345678",
            "09120000003",
            null,
            null,
            null,
            Guid.NewGuid(),
            null,
            false,
            null,
            Guid.NewGuid());
}
