using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Commands;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;
using CrmSuite.Modules.Wallet.Infrastructure.Jobs;
using CrmSuite.Modules.Wallet.Infrastructure.Services;
using FluentAssertions;
using MediatR;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;
using WalletEntity = CrmSuite.Modules.Wallet.Domain.Entities.Wallet;

namespace CrmSuite.UnitTests.Wallet;

public sealed class CommissionTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Percentage_rule_creates_calculated_commission()
    {
        var context = CreateEvaluationContext(null, 10_000_000m, null, 5m);
        var result = await context.Handler.Handle(
            new EvaluateOrderCommissionCommand(context.Order.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.Entries.Should().ContainSingle();
        context.Entries[0].Amount.Should().Be(500_000m);
        context.Entries[0].Status.Should().Be(CommissionStatus.Calculated);
        context.Entries[0].Schedules.Should().ContainSingle();
    }

    [Fact]
    public async Task Fixed_commission_is_split_between_seller_and_customer_owner()
    {
        var ownerId = Guid.NewGuid();
        var context = CreateEvaluationContext(ownerId, 1_000_000m, 1_000_000m, 0m);
        var result = await context.Handler.Handle(
            new EvaluateOrderCommissionCommand(context.Order.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.Entries.Should().HaveCount(2);
        context.Entries.Select(entry => entry.Amount).Should().BeEquivalentTo([500_000m, 500_000m]);
        context.Entries.Select(entry => entry.EmployeeId)
            .Should().BeEquivalentTo([context.Order.SalesExpertId, ownerId]);
    }

    [Fact]
    public async Task Monthly_job_pays_due_schedule_into_employee_wallet()
    {
        var employeeId = Guid.NewGuid();
        var entry = new CommissionEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            employeeId,
            1_200_000m,
            CommissionPayoutType.Monthly,
            null,
            Now);

        entry.Approve(null, Now);
        var schedule = new CommissionSchedule(entry.Id, Now.AddDays(-1), 100_000m, Now);
        entry.AddSchedule(schedule);

        var wallet = new WalletEntity(employeeId, WalletOwnerType.Employee, null, Now);
        var scheduleRepository = new Mock<ICommissionScheduleRepository>();
        scheduleRepository.Setup(repository => repository.GetDueAsync(
                Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([schedule]);

        var commissionRepository = new Mock<ICommissionRepository>();
        commissionRepository.Setup(repository => repository.GetByIdWithSchedulesAsync(
                entry.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var wallets = new Mock<IWalletRepository>();
        wallets.Setup(repository => repository.GetByOwnerAsync(
                employeeId,
                WalletOwnerType.Employee,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var unitOfWork = new Mock<IWalletUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var walletAccounts = new WalletAccountService(wallets.Object, clock.Object);
        var job = new ProcessMonthlyCommissionsJob(
            scheduleRepository.Object,
            commissionRepository.Object,
            walletAccounts,
            unitOfWork.Object,
            clock.Object,
            Mock.Of<IAuditScope>());

        await job.ExecuteAsync(CancellationToken.None);

        wallet.Balance.Should().Be(100_000m);
        schedule.IsPaid.Should().BeTrue();
        entry.Status.Should().Be(CommissionStatus.Paid);
    }

    private static EvaluationContext CreateEvaluationContext(
        Guid? ownerId,
        decimal itemTotal,
        decimal? fixedAmount,
        decimal percentage)
    {
        var salesExpertId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var service = new Service(
            "BW-DED-10",
            "پهنای باند اختصاصی",
            Guid.NewGuid(),
            ServiceType.BandwidthDedicated,
            ServiceUnit.BandwidthMbps,
            null,
            isActive: true);

        var order = new OrderDto(
            Guid.NewGuid(),
            "ORD-1405-00001",
            customerId,
            "مشتری تست",
            salesExpertId,
            Now,
            "1405/02/20",
            "Draft",
            "New",
            itemTotal,
            0m,
            itemTotal,
            null,
            [
                new OrderItemDto(
                    Guid.NewGuid(), service.Id, null, null,
                    service.Name, 1, itemTotal, itemTotal,
                    "Monthly", Now, "1405/02/20"),
            ],
            [],
            Now,
            "1405/02/20");

        var customer = new Customer(
            CustomerType.Natural,
            "مشتری",
            "تست",
            null,
            null,
            "0012345678",
            "09120000001",
            null,
            null,
            null,
            ownerId,
            null,
            false,
            null,
            null);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetOrderByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(order));

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByIdAsync(
                customerId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        var rule = new CommissionRule(
            service.Id,
            null,
            percentage,
            fixedAmount,
            CommissionPayoutType.OneTime,
            salesExpertId,
            Now);

        var rules = new Mock<ICommissionRuleRepository>();
        rules.Setup(repository => repository.GetMatchingAsync(
                service.Id,
                ServiceType.BandwidthDedicated,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var entries = new List<CommissionEntry>();
        var commissions = new Mock<ICommissionRepository>();
        commissions.Setup(repository => repository.ExistsForOrderAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        commissions.Setup(repository => repository.Add(It.IsAny<CommissionEntry>()))
            .Callback<CommissionEntry>(entries.Add);

        var unitOfWork = new Mock<IWalletUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(salesExpertId);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var handler = new EvaluateOrderCommissionCommandHandler(
            mediator.Object,
            customers.Object,
            services.Object,
            rules.Object,
            new CommissionCalculator(),
            commissions.Object,
            unitOfWork.Object,
            currentUser.Object,
            clock.Object);

        return new EvaluationContext(order, handler, entries);
    }

    private sealed record EvaluationContext(
        OrderDto Order,
        EvaluateOrderCommissionCommandHandler Handler,
        List<CommissionEntry> Entries);
}
