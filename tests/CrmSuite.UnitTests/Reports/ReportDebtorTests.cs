using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Reports.Application.Abstractions;
using CrmSuite.Modules.Reports.Application.Dtos;
using CrmSuite.Modules.Reports.Application.Queries;
using CrmSuite.Modules.Reports.Application.Services;
using FluentAssertions;
using Moq;

namespace CrmSuite.UnitTests.Reports;

public sealed class ReportDebtorTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Debtors_query_returns_outstanding_invoices_only()
    {
        var debtors = new List<DebtorDto>
        {
            new(Guid.NewGuid(), "بدهکار", "09120000001", 1_500_000m, 1),
        };

        var repository = new Mock<IReportRepository>();
        repository.Setup(report => report.GetDebtorsAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(debtors);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var handler = new GetCustomerDebtorsQueryHandler(repository.Object, clock.Object);
        var result = await handler.Handle(new GetCustomerDebtorsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].OutstandingAmount.Should().Be(1_500_000m);
    }

    [Fact]
    public void Outstanding_policy_excludes_paid_and_canceled_invoices()
    {
        ReportInvoicePolicy.IsOutstanding("Issued", 1_000m, 0m).Should().BeTrue();
        ReportInvoicePolicy.IsOutstanding("PartiallyPaid", 1_000m, 400m).Should().BeTrue();
        ReportInvoicePolicy.IsOutstanding("Paid", 1_000m, 1_000m).Should().BeFalse();
        ReportInvoicePolicy.IsOutstanding("Canceled", 1_000m, 0m).Should().BeFalse();
    }

    [Fact]
    public async Task Bad_debt_query_uses_due_date_and_open_status()
    {
        var badDebts = new List<BadDebtDto>
        {
            new(Guid.NewGuid(), "بدحساب", "09120000002", 2_000_000m, 2, 24),
        };

        var repository = new Mock<IReportRepository>();
        repository.Setup(report => report.GetBadDebtorsAsync(Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(badDebts);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var handler = new GetBadDebtorsQueryHandler(repository.Object, clock.Object);
        var result = await handler.Handle(new GetBadDebtorsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].MaxOverdueDays.Should().Be(24);
    }

    [Fact]
    public void Bad_debt_policy_detects_overdue_unpaid_invoices()
    {
        ReportInvoicePolicy.IsBadDebt(
            "Issued",
            1_000m,
            0m,
            Now.AddDays(-5),
            Now).Should().BeTrue();

        ReportInvoicePolicy.IsBadDebt(
            "Paid",
            1_000m,
            1_000m,
            Now.AddDays(-5),
            Now).Should().BeFalse();

        ReportInvoicePolicy.IsBadDebt(
            "Issued",
            1_000m,
            0m,
            Now.AddDays(5),
            Now).Should().BeFalse();
    }
}
