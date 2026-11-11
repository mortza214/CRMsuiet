using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Billing.Infrastructure.Jobs;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using FluentAssertions;
using MediatR;
using Moq;

namespace CrmSuite.UnitTests.Billing;

public sealed class MonthlyBandwidthInvoiceJobTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Monthly_invoice_uses_customer_service_snapshot_and_not_current_catalog_price()
    {
        var service = new CustomerService(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "پهنای باند اختصاصی",
            100m,
            2,
            50m,
            """{"bandwidth_mb":20}""",
            2m,
            Now.AddMonths(-1),
            Now.AddDays(-1),
            null);

        var services = new Mock<ICustomerServiceRepository>();
        services.Setup(repository => repository.GetDueForInvoicingAsync(
                Now,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([service]);

        Invoice? created = null;
        var invoices = new Mock<IInvoiceRepository>();
        invoices.Setup(repository => repository.Add(It.IsAny<Invoice>()))
            .Callback<Invoice>(invoice => created = invoice);

        var unitOfWork = new Mock<IBillingUnitOfWork>();
        var sequences = new Mock<ISequenceService>();
        sequences.Setup(service => service.GetNextValueAsync(
                "InvoiceNumber",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-TEST-1");

        var settings = new Mock<ISettingService>();
        settings.Setup(setting => setting.GetAsync(
                "Billing.VatPercentage",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Setting("Billing.VatPercentage", "10"));
        settings.Setup(setting => setting.GetAsync(
                "Billing.InsurancePercentage",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Setting("Billing.InsurancePercentage", "0"));

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var job = new MonthlyBandwidthInvoiceJob(
            services.Object,
            invoices.Object,
            unitOfWork.Object,
            sequences.Object,
            settings.Object,
            Mock.Of<IMediator>(),
            clock.Object);

        await job.ExecuteAsync(CancellationToken.None);

        created.Should().NotBeNull();
        created!.Type.Should().Be(BillingInvoiceType.Monthly);
        created.Items.Should().ContainSingle();
        created.Items.Single().UnitPrice.Should().Be(250m);
        created.SubTotal.Should().Be(250m);
        created.TotalAmount.Should().Be(275m);
        service.MonthlyAmount.Should().Be(250m);
        service.NextInvoiceAt.Should().BeAfter(Now);
    }

    private static SettingDto Setting(string key, string value) =>
        new(
            key,
            value,
            "Billing",
            false,
            Now,
            "1405/08/18",
            null,
            null);
}
