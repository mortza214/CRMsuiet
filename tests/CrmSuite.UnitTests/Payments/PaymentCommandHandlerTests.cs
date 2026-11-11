using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Application.Commands;
using CrmSuite.Modules.Payments.Application.Mapping;
using CrmSuite.Modules.Payments.Application.Services;
using CrmSuite.Modules.Payments.Domain.Entities;
using CrmSuite.Modules.Payments.Domain.Enums;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Payments;

public sealed class PaymentCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Online_verification_marks_invoice_and_order_paid()
    {
        var context = CreateContext();
        var gateway = new Mock<ISamanGateway>();
        gateway.Setup(g => g.VerifyPaymentAsync(
                "SMN-TOKEN",
                context.Transaction.Amount,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SamanVerifyResult(true, "REF-1", "{\"status\":1}", null));

        var handler = CreateVerifyHandler(context, gateway.Object);
        var result = await handler.Handle(
            new VerifyOnlinePaymentCommand(context.Transaction.TrackingCode, "SMN-TOKEN"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.Transaction.Status.Should().Be(TransactionStatus.Success);
        context.Invoice.Status.Should().Be(InvoiceStatus.Paid);
        context.Invoice.PaidAmount.Should().Be(context.Invoice.TotalAmount);
        context.Order.Status.Should().Be(OrderStatus.Paid);
        context.Order.StatusHistory.Should().Contain(history =>
            history.ToStatus == OrderStatus.Paid);
    }

    [Fact]
    public async Task Manual_payment_stays_pending_until_manager_approval()
    {
        var context = CreateContext();
        var manual = new Transaction(
            context.Invoice.Id,
            context.Invoice.CustomerId,
            context.Invoice.TotalAmount,
            PaymentMethod.ManualTransfer,
            PaymentGatewayName.None,
            "CARD-123",
            "PAY-MANUAL-01",
            context.UserId,
            Now);

        context.Transactions
            .Setup(repository => repository.GetByIdAsync(manual.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manual);

        var handler = new ApproveManualPaymentCommandHandler(
            context.Transactions.Object,
            context.Invoices.Object,
            context.Customers.Object,
            context.Settlement,
            new PaymentDtoMapper(new JalaliDateService()));

        manual.Status.Should().Be(TransactionStatus.Pending);
        context.Invoice.Status.Should().Be(InvoiceStatus.Issued);

        var result = await handler.Handle(new ApproveManualPaymentCommand(manual.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        manual.Status.Should().Be(TransactionStatus.Success);
        context.Invoice.Status.Should().Be(InvoiceStatus.Paid);
        context.Order.Status.Should().Be(OrderStatus.Paid);
    }

    private static TestContext CreateContext()
    {
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var order = new Order(
            "ORD-1405-00001",
            customerId,
            userId,
            Now,
            OrderType.New,
            null,
            userId,
            Now);

        order.ChangeStatus(OrderStatus.PendingPayment, userId, Now, "start");

        var invoice = new Invoice(
            "INV-1405-00001",
            order.Id,
            customerId,
            BillingInvoiceType.Official,
            Now,
            Now.AddDays(7),
            0,
            userId,
            Now);

        invoice.AddItem(new InvoiceItem(invoice.Id, "پهنای باند", 1, 10_000_000m, Now));
        invoice.Issue(10m, 0m, userId, Now);

        var transaction = new Transaction(
            invoice.Id,
            customerId,
            invoice.TotalAmount,
            PaymentMethod.OnlineGateway,
            PaymentGatewayName.Saman,
            "SMN-TOKEN",
            "PAY-ONLINE-01",
            userId,
            Now);

        var invoices = new Mock<IInvoiceRepository>();
        invoices.Setup(repository => repository.GetByIdWithItemsAsync(
                invoice.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdWithDetailsAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var transactions = new Mock<ITransactionRepository>();
        transactions.Setup(repository => repository.GetByTrackingCodeAsync(
                transaction.TrackingCode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var customers = new Mock<ICustomerRepository>();
        var unitOfWork = new Mock<IPaymentUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var audit = new Mock<IAuditScope>();
        var settlement = new PaymentSettlementService(
            invoices.Object,
            orders.Object,
            unitOfWork.Object,
            Mock.Of<MediatR.IMediator>(),
            audit.Object,
            currentUser.Object,
            clock.Object);

        return new TestContext(
            invoice,
            order,
            transaction,
            transactions,
            invoices,
            orders,
            customers,
            unitOfWork,
            currentUser,
            clock,
            audit,
            settlement,
            userId);
    }

    private static VerifyOnlinePaymentCommandHandler CreateVerifyHandler(
        TestContext context,
        ISamanGateway gateway)
    {
        return new VerifyOnlinePaymentCommandHandler(
            context.Transactions.Object,
            Mock.Of<IPaymentGatewayTransactionRepository>(),
            context.Customers.Object,
            gateway,
            context.Settlement,
            context.UnitOfWork.Object,
            context.CurrentUser.Object,
            context.Clock.Object,
            new PaymentDtoMapper(new JalaliDateService()));
    }

    private sealed record TestContext(
        Invoice Invoice,
        Order Order,
        Transaction Transaction,
        Mock<ITransactionRepository> Transactions,
        Mock<IInvoiceRepository> Invoices,
        Mock<IOrderRepository> Orders,
        Mock<ICustomerRepository> Customers,
        Mock<IPaymentUnitOfWork> UnitOfWork,
        Mock<FoundationCurrentUser> CurrentUser,
        Mock<IDateTimeProvider> Clock,
        Mock<IAuditScope> Audit,
        PaymentSettlementService Settlement,
        Guid UserId);
}
