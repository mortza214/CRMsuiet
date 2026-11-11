using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Commands;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Application.Mapping;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Dtos;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Orders.Application.Dtos;
using CrmSuite.Modules.Orders.Application.Queries;
using FluentAssertions;
using MediatR;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Billing;

public sealed class GenerateInvoiceFromOrderCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Invoice_calculates_vat_and_insurance_on_order_subtotal()
    {
        var order = CreateOrder();
        var invoices = new Mock<IInvoiceRepository>();
        invoices.Setup(repository => repository.ExistsForOrderAsync(
                order.Id,
                BillingInvoiceType.Official,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Invoice? saved = null;
        invoices.Setup(repository => repository.Add(It.IsAny<Invoice>()))
            .Callback<Invoice>(invoice => saved = invoice);

        var handler = CreateHandler(order, invoices.Object);
        var result = await handler.Handle(
            new GenerateInvoiceFromOrderCommand(order.Id, BillingInvoiceType.Official),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.SubTotal.Should().Be(10_000_000m);
        saved.TaxAmount.Should().Be(1_000_000m);
        saved.InsuranceAmount.Should().Be(100_000m);
        saved.TotalAmount.Should().Be(11_100_000m);
        saved.Status.Should().Be(InvoiceStatus.Issued);
        saved.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Duplicate_invoice_for_same_order_and_type_is_rejected()
    {
        var order = CreateOrder();
        var invoices = new Mock<IInvoiceRepository>();
        invoices.Setup(repository => repository.ExistsForOrderAsync(
                order.Id,
                BillingInvoiceType.Official,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler(order, invoices.Object);
        var result = await handler.Handle(
            new GenerateInvoiceFromOrderCommand(order.Id, BillingInvoiceType.Official),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Message.Should().Contain("قبلاً");
        invoices.Verify(
            repository => repository.Add(It.IsAny<Invoice>()),
            Times.Never);
    }

    private static GenerateInvoiceFromOrderCommandHandler CreateHandler(
        OrderDto order,
        IInvoiceRepository invoices)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetOrderByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(order));

        var settings = new Mock<ISettingService>();
        settings.Setup(service => service.GetAsync(
                It.IsAny<string>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, bool _, CancellationToken _) => key switch
            {
                "Billing.VatPercentage" => new SettingDto(
                    key, "10", "Billing", false, Now, "1405/02/20", null, null),
                "Billing.InsurancePercentage" => new SettingDto(
                    key, "1", "Billing", false, Now, "1405/02/20", null, null),
                _ => null,
            });

        var sequences = new Mock<ISequenceService>();
        sequences.Setup(service => service.GetNextValueAsync("InvoiceNumber", It.IsAny<CancellationToken>()))
            .ReturnsAsync("INV-1405-00001");

        var unitOfWork = new Mock<IBillingUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        return new GenerateInvoiceFromOrderCommandHandler(
            invoices,
            unitOfWork.Object,
            mediator.Object,
            Mock.Of<ICustomerRepository>(),
            settings.Object,
            sequences.Object,
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            new BillingDtoMapper(new JalaliDateService()));
    }

    private static OrderDto CreateOrder()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        return new OrderDto(
            orderId,
            "ORD-1405-00001",
            customerId,
            "مشتری تست",
            Guid.NewGuid(),
            Now,
            "1405/02/20",
            "PendingPayment",
            "New",
            10_000_000m,
            0m,
            10_000_000m,
            null,
            [
                new OrderItemDto(
                    Guid.NewGuid(), Guid.NewGuid(), null, null,
                    "پهنای باند اختصاصی", 10, 800_000m, 8_000_000m,
                    "Monthly", Now, "1405/02/20"),
                new OrderItemDto(
                    Guid.NewGuid(), null, null, Guid.NewGuid(),
                    "نصب دکل", 1, 2_000_000m, 2_000_000m,
                    "OneTime", Now, "1405/02/20"),
            ],
            [],
            Now,
            "1405/02/20");
    }
}
