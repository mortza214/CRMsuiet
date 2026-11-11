using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Identity.Domain.Entities;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Commands;
using CrmSuite.Modules.Tickets.Application.Mapping;
using CrmSuite.Modules.Tickets.Application.Services;
using CrmSuite.Modules.Tickets.Domain.Entities;
using CrmSuite.Modules.Tickets.Domain.Enums;
using FluentAssertions;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Tickets;

public sealed class TicketTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Portal_ticket_gets_number_and_department_notification()
    {
        var context = CreateContext();
        var result = await context.Handler.Handle(
            new CreateTicketCommand(
                null,
                null,
                "مشکل اینترنت",
                context.Department.Id,
                TicketPriority.High,
                TicketSource.Portal,
                "اتصال قطع است."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TicketNumber.Should().Be("TKT-1405-00001");
        context.Saved.Should().NotBeNull();
        context.Saved!.Source.Should().Be(TicketSource.Portal);
        context.Notifications.Verify(service => service.NotifyDepartmentAsync(
            context.Department.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Customer_reply_reopens_a_pending_ticket()
    {
        var context = CreateContext();
        var ticket = new Ticket(
            "TKT-1405-00002",
            context.Customer.Id,
            null,
            "پیگیری",
            context.Department.Id,
            null,
            TicketPriority.Medium,
            TicketSource.Portal,
            context.UserId,
            Now);

        ticket.AddInitialHistory(context.UserId, Now);
        ticket.ChangeStatus(TicketStatus.PendingCustomer, context.UserId, Now, "منتظر مشتری");

        context.Tickets.Setup(repository => repository.GetByIdWithDetailsAsync(
                ticket.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var handler = CreateMessageHandler(context);
        var result = await handler.Handle(
            new AddTicketMessageCommand(
                ticket.Id,
                "پاسخ مشتری",
                false,
                TicketSenderType.Customer),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.Open);
    }

    [Fact]
    public void Portal_projection_hides_internal_notes()
    {
        var ticket = new Ticket(
            "TKT-1405-00003",
            Guid.NewGuid(),
            null,
            "تست",
            Guid.NewGuid(),
            null,
            TicketPriority.Low,
            TicketSource.Portal,
            Guid.NewGuid(),
            Now);

        ticket.AddMessage(new TicketMessage(
            ticket.Id,
            Guid.NewGuid(),
            TicketSenderType.Employee,
            "یادداشت داخلی",
            true,
            Now));

        var mapper = new TicketDtoMapper(new JalaliDateService());
        var portal = mapper.ToDto(ticket, "مشتری", null, "پشتیبانی", null, false);
        var staff = mapper.ToDto(ticket, "مشتری", null, "پشتیبانی", null, true);

        portal.Messages.Should().BeEmpty();
        staff.Messages.Should().ContainSingle();
        staff.Messages[0].IsInternalNote.Should().BeTrue();
    }

    private static TestContext CreateContext()
    {
        var userId = Guid.NewGuid();
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
            null,
            null,
            false,
            null,
            null);

        var department = new Department("پشتیبانی");
        var tickets = new Mock<ITicketRepository>();
        Ticket? saved = null;
        tickets.Setup(repository => repository.Add(It.IsAny<Ticket>()))
            .Callback<Ticket>(ticket => saved = ticket);

        var customers = new Mock<ICustomerRepository>();
        customers.Setup(repository => repository.GetByUserIdAsync(
                userId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        customers.Setup(repository => repository.GetByIdAsync(
                customer.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(repository => repository.GetByIdAsync(
                department.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        var services = new Mock<IServiceRepository>();
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdWithAccessAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var notifications = new Mock<INotificationService>();
        var sequences = new Mock<ISequenceService>();
        sequences.Setup(service => service.GetNextValueAsync(
                "TicketNumber",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("TKT-1405-00001");

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(provider => provider.UtcNow).Returns(Now);

        var currentUser = new Mock<FoundationCurrentUser>();
        currentUser.SetupGet(user => user.UserId).Returns(userId);

        var unitOfWork = new Mock<ITicketUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mapper = new TicketDtoMapper(new JalaliDateService());
        var staffAccess = new StaffAccessService(users.Object, currentUser.Object);
        var view = new TicketViewService(
            customers.Object,
            services.Object,
            departments.Object,
            users.Object,
            mapper);

        var handler = new CreateTicketCommandHandler(
            tickets.Object,
            unitOfWork.Object,
            customers.Object,
            services.Object,
            departments.Object,
            staffAccess,
            notifications.Object,
            sequences.Object,
            Mock.Of<MediatR.IMediator>(),
            Mock.Of<IAuditScope>(),
            currentUser.Object,
            clock.Object,
            view);

        return new TestContext(
            handler,
            tickets,
            customers,
            notifications,
            clock,
            currentUser,
            department,
            customer,
            userId,
            () => saved);
    }

    private static AddTicketMessageCommandHandler CreateMessageHandler(TestContext context)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByIdWithAccessAsync(
                context.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var departments = new Mock<IDepartmentRepository>();
        var services = new Mock<IServiceRepository>();
        var mapper = new TicketDtoMapper(new JalaliDateService());
        var view = new TicketViewService(
            context.Customers.Object,
            services.Object,
            departments.Object,
            users.Object,
            mapper);

        var staffAccess = new StaffAccessService(users.Object, context.CurrentUser.Object);
        var unitOfWork = new Mock<ITicketUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new AddTicketMessageCommandHandler(
            context.Tickets.Object,
            unitOfWork.Object,
            context.Customers.Object,
            staffAccess,
            context.Notifications.Object,
            Mock.Of<IAuditScope>(),
            context.CurrentUser.Object,
            context.Clock.Object,
            view);
    }

    private sealed record TestContext(
        CreateTicketCommandHandler Handler,
        Mock<ITicketRepository> Tickets,
        Mock<ICustomerRepository> Customers,
        Mock<INotificationService> Notifications,
        Mock<IDateTimeProvider> Clock,
        Mock<FoundationCurrentUser> CurrentUser,
        Department Department,
        Customer Customer,
        Guid UserId,
        Func<Ticket?> SavedAccessor)
    {
        public Ticket? Saved => SavedAccessor();
    }
}
