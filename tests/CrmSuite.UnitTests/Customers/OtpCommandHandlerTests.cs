using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Commands;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Application.Options;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Customers;

public sealed class OtpCommandHandlerTests
{
    private static readonly DateTime Now = new(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PortalUserId = Guid.NewGuid();

    [Fact]
    public async Task Request_otp_stores_and_sends_a_six_digit_code()
    {
        var customer = CreateCustomer();
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByMobileAsync(
                customer.Mobile,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        CustomerOtp? stored = null;
        var otps = new Mock<ICustomerOtpRepository>();
        otps.Setup(repository => repository.InvalidateActiveAsync(
                customer.Mobile,
                Now,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        otps.Setup(repository => repository.Add(It.IsAny<CustomerOtp>()))
            .Callback<CustomerOtp>(otp => stored = otp);

        string? sentCode = null;
        var sender = new Mock<IOtpSender>();
        sender.Setup(service => service.SendAsync(
                customer.Mobile,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, code, _) => sentCode = code)
            .Returns(Task.CompletedTask);

        var handler = CreateRequestHandler(customers.Object, otps.Object, sender.Object);
        var result = await handler.Handle(new RequestOtpCommand(customer.Mobile), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sentCode.Should().MatchRegex("^\\d{6}$");
        stored.Should().NotBeNull();
        stored!.Code.Should().Be(sentCode);
    }

    [Fact]
    public async Task Expired_otp_is_rejected()
    {
        var customer = CreateCustomer();
        var otp = new CustomerOtp(customer.Mobile, "123456", Now.AddMinutes(-1), "127.0.0.1", Now.AddMinutes(-2));

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByMobileAsync(
                customer.Mobile,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var otps = new Mock<ICustomerOtpRepository>();
        otps.Setup(repository => repository.GetLatestUnusedAsync(
                customer.Mobile,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otp);

        var handler = CreateVerifyHandler(customers.Object, otps.Object, Mock.Of<ICustomerIdentityService>());
        var result = await handler.Handle(
            new VerifyOtpCommand(customer.Mobile, "123456"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("معتبر");
    }

    [Fact]
    public async Task Valid_otp_returns_customer_token()
    {
        var customer = CreateCustomer();
        var otp = new CustomerOtp(customer.Mobile, "123456", Now.AddMinutes(2), "127.0.0.1", Now);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByMobileAsync(
                customer.Mobile,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var otps = new Mock<ICustomerOtpRepository>();
        otps.Setup(repository => repository.GetLatestUnusedAsync(
                customer.Mobile,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otp);

        var identity = new Mock<ICustomerIdentityService>();
        identity.Setup(service => service.IssueTokenAsync(
                PortalUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CustomerTokenDto(
                "customer-token",
                Now.AddHours(1),
                "1403/01/01",
                ["Customer"])));

        var handler = CreateVerifyHandler(customers.Object, otps.Object, identity.Object);
        var result = await handler.Handle(
            new VerifyOtpCommand(customer.Mobile, "123456"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("customer-token");
        otp.IsUsed.Should().BeTrue();
    }

    private static RequestOtpCommandHandler CreateRequestHandler(
        ICustomerRepository customers,
        ICustomerOtpRepository otps,
        IOtpSender sender)
    {
        var unitOfWork = new Mock<ICustomerUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new RequestOtpCommandHandler(
            customers,
            otps,
            Mock.Of<ICustomerIdentityService>(),
            sender,
            unitOfWork.Object,
            CreateCurrentUser().Object,
            CreateClock().Object,
            new JalaliDateService(),
            Options.Create(new OtpOptions()));
    }

    private static VerifyOtpCommandHandler CreateVerifyHandler(
        ICustomerRepository customers,
        ICustomerOtpRepository otps,
        ICustomerIdentityService identity)
    {
        var unitOfWork = new Mock<ICustomerUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var audit = new Mock<IAuditService>();
        audit.Setup(service => service.RecordAsync(
                It.IsAny<AuditEntry>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new VerifyOtpCommandHandler(
            customers,
            otps,
            identity,
            unitOfWork.Object,
            audit.Object,
            CreateCurrentUser().Object,
            CreateClock().Object,
            new CustomerDtoMapper(new JalaliDateService()),
            Options.Create(new OtpOptions()));
    }

    private static Mock<FoundationCurrentUser> CreateCurrentUser()
    {
        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(service => service.IpAddress).Returns("127.0.0.1");
        return currentUser;
    }

    private static Mock<IDateTimeProvider> CreateClock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);
        return clock;
    }

    private static Customer CreateCustomer() =>
        new(
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
            Guid.NewGuid(),
            null,
            false,
            null,
            PortalUserId);
}
