using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Orders.Application.Notifications;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.EventHandlers;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;
using CrmSuite.Modules.Wallet.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace CrmSuite.UnitTests.Wallet;

public sealed class ServiceActivatedCommissionHandlerTests
{
    [Fact]
    public async Task Activated_service_creates_monthly_pending_commission_and_shares()
    {
        var now = new DateTime(2026, 11, 10, 16, 0, 0, DateTimeKind.Utc);
        var customerId = Guid.NewGuid();
        var salesExpertId = Guid.NewGuid();
        CommissionEntry? created = null;
        var repository = new Mock<ICommissionRepository>();
        repository.Setup(item => item.ExistsForOrderAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(item => item.Add(It.IsAny<CommissionEntry>()))
            .Callback<CommissionEntry>(entry => created = entry);

        var customer = new Customer(
            CustomerType.Natural,
            "Test",
            "Customer",
            null,
            null,
            "5094444444",
            "09195550007",
            null,
            null,
            null,
            salesExpertId,
            null,
            false,
            null);
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(item => item.GetByIdAsync(
                customerId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var unitOfWork = new Mock<IWalletUnitOfWork>();
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(now);

        var handler = new ServiceActivatedCommissionHandler(
            repository.Object,
            new CommissionCalculator(),
            new CommissionAllocationService(),
            customers.Object,
            unitOfWork.Object,
            clock.Object);

        await handler.Handle(
            new ServiceActivatedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                customerId,
                Guid.NewGuid(),
                salesExpertId,
                10_000_000m,
                2m,
                "پهنای باند اختصاصی"),
            CancellationToken.None);

        created.Should().NotBeNull();
        created!.Amount.Should().Be(16_666m);
        created.Status.Should().Be(CommissionStatus.Pending);
        created.PayoutType.Should().Be(CommissionPayoutType.Monthly);
        created.Shares.Should().ContainSingle();
        created.Shares.Single().Percentage.Should().Be(100m);
    }
}
