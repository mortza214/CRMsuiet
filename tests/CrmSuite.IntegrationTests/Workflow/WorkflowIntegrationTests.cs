using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using CrmSuite.Modules.Foundation.Infrastructure.Dates;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Notifications;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.EventHandlers;
using CrmSuite.Modules.Workflow.Application.Mapping;
using CrmSuite.Modules.Workflow.Application.Queries;
using CrmSuite.Modules.Workflow.Application.Services;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using CrmSuite.Modules.Workflow.Infrastructure.Persistence;
using CrmSuite.Modules.Workflow.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.IntegrationTests.Workflow;

public sealed class WorkflowIntegrationTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime Now = new(2026, 11, 8, 10, 30, 0, DateTimeKind.Utc);
    private static readonly string[] WorkflowPermissions = ["Workflow.Instance.Execute"];

    [Fact]
    public async Task Full_cycle_publishes_starts_and_completes_workflow()
    {
        using var fixture = new Fixture();
        var definition = fixture.AddDefinition(
            "WF-FULL",
            WorkflowAssigneeType.User,
            UserId.ToString());
        var start = await fixture.StartAsync(definition.Code);
        start.IsSuccess.Should().BeTrue();
        start.Value.Should().NotBeNull();

        var first = await fixture.ExecuteAsync(
            start.Value!.Id,
            definition.Transitions.ElementAt(0).Id,
            "شروع بررسی",
            "idem-first");
        first.IsSuccess.Should().BeTrue();
        first.Value!.CurrentStateCode.Should().Be("CHECK");

        var second = await fixture.ExecuteAsync(
            start.Value.Id,
            definition.Transitions.ElementAt(1).Id,
            "پایان",
            "idem-second");
        second.IsSuccess.Should().BeTrue();
        second.Value!.Status.Should().Be("Completed");
        second.Value.History.Should().HaveCount(3);
    }

    [Fact]
    public async Task Inbox_returns_tasks_assigned_to_current_user()
    {
        using var fixture = new Fixture();
        var definition = fixture.AddDefinition(
            "WF-INBOX",
            WorkflowAssigneeType.User,
            UserId.ToString());
        var started = await fixture.StartAsync(definition.Code);
        await fixture.ExecuteAsync(
            started.Value!.Id,
            definition.Transitions.ElementAt(0).Id,
            "در انتظار بررسی",
            "inbox-transition");

        var tasks = await fixture.GetMyTasksAsync();

        tasks.IsSuccess.Should().BeTrue();
        tasks.Value!.Items.Should().ContainSingle();
        tasks.Value.Items[0].AssignedUserId.Should().Be(UserId);
        tasks.Value.Items[0].CurrentStateName.Should().Be("Check");
    }

    [Fact]
    public async Task Failed_guard_does_not_change_state_or_write_history()
    {
        using var fixture = new Fixture();
        var definition = fixture.AddDefinition(
            "WF-GUARD",
            WorkflowAssigneeType.User,
            UserId.ToString(),
            "Some.Permission.That.UserDoesNotHave");
        var started = await fixture.StartAsync(definition.Code);

        var result = await fixture.ExecuteAsync(
            started.Value!.Id,
            definition.Transitions.ElementAt(0).Id,
            "should fail",
            "guard-test");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("WORKFLOW_GUARD_FAILED");
        var instance = await fixture.GetInstanceAsync(started.Value.Id);
        instance!.History.Should().HaveCount(1);
        instance.CurrentStateId.Should().NotBe(definition.Transitions.ElementAt(0).ToStateId);
    }

    [Fact]
    public async Task Repeating_transition_with_same_idempotency_key_is_a_no_op()
    {
        using var fixture = new Fixture();
        var definition = fixture.AddDefinition(
            "WF-IDEMPOTENT",
            WorkflowAssigneeType.User,
            UserId.ToString());
        var started = await fixture.StartAsync(definition.Code);

        var first = await fixture.ExecuteAsync(
            started.Value!.Id,
            definition.Transitions.ElementAt(0).Id,
            "first",
            "same-key");
        var second = await fixture.ExecuteAsync(
            started.Value.Id,
            definition.Transitions.ElementAt(0).Id,
            "second",
            "same-key");

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        var instance = await fixture.GetInstanceAsync(started.Value.Id);
        instance!.History.Should().HaveCount(2);
        instance.CurrentStateId.Should().Be(definition.Transitions.ElementAt(0).ToStateId);
    }

    [Fact]
    public async Task Transition_writes_an_audit_record()
    {
        using var fixture = new Fixture();
        var definition = fixture.AddDefinition(
            "WF-AUDIT",
            WorkflowAssigneeType.User,
            UserId.ToString());
        var started = await fixture.StartAsync(definition.Code);

        await fixture.ExecuteAsync(
            started.Value!.Id,
            definition.Transitions.ElementAt(0).Id,
            "audit reason",
            "audit-key");

        fixture.Audit.Entries.Should().Contain(entry =>
            entry.EntityName == "WorkflowInstance"
            && entry.Action == AuditAction.Update
            && entry.EntityId == started.Value.Id.ToString()
            && entry.NewValues!.ToString()!.Contains("audit reason"));
    }

    [Fact]
    public async Task Order_created_handler_starts_workflow_when_service_has_definition()
    {
        var orderId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var definitionId = Guid.NewGuid();
        var definition = new WorkflowDefinition(
            "WF-ORDER",
            "Order",
            WorkflowEntityType.Order,
            null,
            null,
            Now);
        var order = new Order(
            "ORD-WF-1",
            customerId,
            UserId,
            Now,
            OrderType.New,
            null,
            UserId,
            Now);
        order.AddItem(new OrderItem(
            order.Id,
            serviceId,
            null,
            null,
            "Service",
            1,
            100,
            BillingCycle.Monthly,
            Now));

        var service = new Service(
            "WF-SVC",
            "Workflow service",
            Guid.NewGuid(),
            ServiceType.BandwidthDedicated,
            ServiceUnit.BandwidthMbps,
            null);
        service.SetWorkflowDefinition(definitionId, UserId, Now);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var services = new Mock<IServiceRepository>();
        services.Setup(repository => repository.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        var definitions = new Mock<IWorkflowDefinitionRepository>();
        definitions.Setup(repository => repository.GetByIdWithGraphAsync(definitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var starter = new Mock<IWorkflowStarter>();

        var handler = new OrderCreatedWorkflowHandler(
            orders.Object,
            services.Object,
            definitions.Object,
            starter.Object);

        await handler.Handle(new OrderCreatedNotification(order.Id), CancellationToken.None);

        starter.Verify(instance => instance.TryStartAsync(
            WorkflowEntityType.Order,
            order.Id,
            "WF-ORDER",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly WorkflowDefinitionRepository _definitions;
        private readonly WorkflowInstanceRepository _instances;
        private readonly EfWorkflowUnitOfWork _unitOfWork;
        private readonly WorkflowDtoMapper _mapper;
        private readonly FakeActorAccessor _actor;

        public FakeAuditService Audit { get; } = new();

        public Fixture()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"workflow-{Guid.NewGuid():N}")
                .Options;
            _db = new AppDbContext(
                options,
                new[] { new WorkflowModelConfigurator() });
            _db.Database.EnsureCreated();

            _definitions = new WorkflowDefinitionRepository(_db);
            _instances = new WorkflowInstanceRepository(_db);
            _unitOfWork = new EfWorkflowUnitOfWork(_db);
            _mapper = new WorkflowDtoMapper(new JalaliDateService());
            _actor = new FakeActorAccessor(
                new WorkflowActor(
                    UserId,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>(),
                    WorkflowPermissions));
        }

        public WorkflowDefinition AddDefinition(
            string code,
            WorkflowAssigneeType assigneeType,
            string? assigneeValue,
            string? requiredPermission = null)
        {
            var definition = new WorkflowDefinition(
                code,
                code,
                WorkflowEntityType.ManualTask,
                null,
                UserId,
                Now);
            var start = definition.AddState(
                "START",
                "Start",
                WorkflowStateType.Start,
                1,
                false,
                WorkflowAssigneeType.None,
                null,
                null,
                "#1677ff");
            var check = definition.AddState(
                "CHECK",
                "Check",
                WorkflowStateType.Normal,
                2,
                false,
                assigneeType,
                assigneeValue,
                null,
                "#faad14");
            var end = definition.AddState(
                "END",
                "End",
                WorkflowStateType.End,
                3,
                true,
                WorkflowAssigneeType.None,
                null,
                null,
                "#52c41a");
            definition.AddTransition(
                start.Id,
                check.Id,
                "Check",
                requiredPermission is null
                    ? WorkflowGuardType.None
                    : WorkflowGuardType.PermissionOnly,
                requiredPermission,
                null,
                false,
                1,
                UserId,
                Now);
            definition.AddTransition(
                check.Id,
                end.Id,
                "Finish",
                WorkflowGuardType.None,
                null,
                null,
                false,
                2,
                UserId,
                Now);
            definition.Publish(UserId, Now);
            _definitions.Add(definition);
            _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
            return definition;
        }

        public Task<Result<WorkflowInstanceDto>> StartAsync(string code)
        {
            var handler = new StartWorkflowCommandHandler(
                _definitions,
                _instances,
                _unitOfWork,
                _actor,
                new FakeCustomerResolver(),
                new WorkflowAssignmentResolver(),
                new FakeLinker(),
                new FakeNotifier(),
                new FakeAuditScope(),
                Audit,
                new FakeCurrentUser(),
                new FakeClock(),
                _mapper);

            return handler.Handle(
                new StartWorkflowCommand(
                    WorkflowEntityType.ManualTask,
                    Guid.NewGuid(),
                    code,
                    "payload",
                    Guid.NewGuid().ToString("N")),
                CancellationToken.None);
        }

        public Task<Result<WorkflowInstanceDto>> ExecuteAsync(
            Guid instanceId,
            Guid transitionId,
            string comment,
            string idempotencyKey)
        {
            var handler = new ExecuteWorkflowTransitionCommandHandler(
                _definitions,
                _instances,
                _unitOfWork,
                _actor,
                new FakeCustomerResolver(),
                new WorkflowAssignmentResolver(),
                Array.Empty<IWorkflowGuard>(),
                Array.Empty<IWorkflowTransitionHook>(),
                new FakeNotifier(),
                new FakeAuditScope(),
                Audit,
                new FakeCurrentUser(),
                new FakeClock(),
                _mapper);

            return handler.Handle(
                new ExecuteWorkflowTransitionCommand(
                    instanceId,
                    transitionId,
                    comment,
                    "{}",
                    idempotencyKey),
                CancellationToken.None);
        }

        public Task<Result<PagedResult<WorkflowTaskDto>>> GetMyTasksAsync()
        {
            var handler = new GetMyWorkflowTasksQueryHandler(
                _instances,
                _definitions,
                _actor,
                _mapper);
            return handler.Handle(new GetMyWorkflowTasksQuery(), CancellationToken.None);
        }

        public Task<WorkflowInstance?> GetInstanceAsync(Guid id) =>
            _instances.GetByIdWithGraphAsync(id, CancellationToken.None);

        public void Dispose() => _db.Dispose();
    }

    private sealed class FakeActorAccessor : IWorkflowActorAccessor
    {
        private readonly WorkflowActor _actor;
        public FakeActorAccessor(WorkflowActor actor) => _actor = actor;
        public Task<WorkflowActor> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_actor);
    }

    private sealed class FakeCustomerResolver : IWorkflowEntityCustomerResolver
    {
        public Task<Guid?> GetCustomerOwnerUserIdAsync(
            WorkflowEntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task<Guid?> GetCustomerPortalUserIdAsync(
            WorkflowEntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);
    }

    private sealed class FakeLinker : IWorkflowEntityLinker
    {
        public Task LinkAsync(WorkflowInstance instance, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeNotifier : IWorkflowNotifier
    {
        public Task NotifyTaskAssignedAsync(
            WorkflowTask task,
            WorkflowInstance instance,
            WorkflowDefinition definition,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task NotifyStateEnteredAsync(
            WorkflowInstance instance,
            WorkflowState state,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUser : FoundationCurrentUser
    {
        public Guid? UserId => WorkflowIntegrationTests.UserId;
        public string? UserName => "workflow-test";
        public string? IpAddress => "127.0.0.1";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow => Now;
    }

    private sealed class FakeAuditScope : IAuditScope
    {
        public object? OldValues { get; set; }
        public string? EntityId { get; set; }
    }

    private sealed class FakeAuditService : IAuditService
    {
        public List<AuditEntry> Entries { get; } = new();
        public Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
