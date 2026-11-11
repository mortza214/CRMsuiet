using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Dtos;
using CrmSuite.Modules.Catalog.Application.Services;
using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using FluentAssertions;
using MediatR;
using Moq;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.UnitTests.Workflow;

public sealed class BandwidthWorkflowSelectionTests
{
    [Theory]
    [InlineData("wireless", BandwidthWorkflowCodes.WithFeasibility)]
    [InlineData("fiber", BandwidthWorkflowCodes.NoFeasibility)]
    [InlineData("cable", BandwidthWorkflowCodes.NoFeasibility)]
    public async Task Substrate_selects_the_expected_workflow(
        string substrate,
        string expectedCode)
    {
        var fixture = new Fixture();
        var result = await fixture.Handle(
            $$"""{"bandwidth_mb":20,"substrate":"{{substrate}}"}""");

        result.IsSuccess.Should().BeTrue();
        fixture.StartedCode.Should().Be(expectedCode);
    }

    private sealed class Fixture
    {
        private readonly Mock<IWorkflowStarter> _starter = new();
        private readonly Mock<BandwidthWorkflowSupport> _workflow;
        private readonly CreateServiceRequestCommandHandler _handler;
        private readonly Guid _customerId;
        private readonly Guid _serviceId;

        public Fixture()
        {
            var customer = new Customer(
                CustomerType.Natural,
                "تست",
                "فروش",
                null,
                null,
                "0012345678",
                "09120000123",
                null,
                null,
                null,
                Guid.NewGuid(),
                null,
                false,
                null,
                Guid.NewGuid());
            _customerId = customer.Id;

            var customerRepository = new Mock<ICustomerRepository>();
            customerRepository.Setup(repository => repository.GetByIdAsync(
                    customer.Id,
                    false,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            var service = new Service(
                "BW-TEST",
                "پهنای باند تست",
                Guid.NewGuid(),
                ServiceType.BandwidthDedicated,
                ServiceUnit.BandwidthMbps,
                null,
                true);
            _serviceId = service.Id;
            var serviceRepository = new Mock<IServiceRepository>();
            serviceRepository.Setup(repository => repository.GetByIdAsync(
                    service.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(service);

            var pricing = new Mock<IServicePricingService>();
            pricing.Setup(calculator => calculator.CalculateAsync(
                    It.IsAny<ServicePricingRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(new ServicePricingResult(
                    new PricePreviewDto(
                        service.Id,
                        service.Name,
                        "UnitBased",
                        20,
                        1000,
                        0,
                        20000,
                        null,
                        20000,
                        []),
                    1000,
                    0,
                    null)));

            var sequences = new Mock<ISequenceService>();
            sequences.Setup(provider => provider.GetNextValueAsync(
                    "OrderNumber",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("ORD-TEST-1");

            var orders = new Mock<IOrderRepository>();
            var unitOfWork = new Mock<IOrderUnitOfWork>();
            var definitions = new Mock<IWorkflowDefinitionRepository>();
            var currentUser = new Mock<FoundationCurrentUser>();
            currentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());
            var clock = new Mock<IDateTimeProvider>();
            clock.SetupGet(provider => provider.UtcNow).Returns(DateTime.UtcNow);

            var definition = BuildDefinition();
            var state = definition.States.First();
            var instance = new WorkflowInstance(
                definition,
                WorkflowEntityType.Order,
                Guid.NewGuid(),
                state.Id,
                currentUser.Object.UserId,
                DateTime.UtcNow);

            _workflow = new Mock<BandwidthWorkflowSupport>(
                Mock.Of<IWorkflowInstanceRepository>(),
                Mock.Of<IWorkflowDefinitionRepository>(),
                Mock.Of<IMediator>());
            _workflow.Setup(workflow => workflow.GetActiveAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((instance, definition));
            _workflow.Setup(workflow => workflow.MoveAsync(
                    instance,
                    definition,
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(InstanceDto(instance)));

            _starter.Setup(starter => starter.TryStartAsync(
                    WorkflowEntityType.Order,
                    It.IsAny<Guid>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<WorkflowEntityType, Guid, string?, string?, string?, CancellationToken>(
                    (_, _, code, _, _, _) => StartedCode = code ?? string.Empty)
                .ReturnsAsync(InstanceDto(instance));

            _handler = new CreateServiceRequestCommandHandler(
                orders.Object,
                unitOfWork.Object,
                customerRepository.Object,
                serviceRepository.Object,
                pricing.Object,
                sequences.Object,
                definitions.Object,
                _starter.Object,
                _workflow.Object,
                currentUser.Object,
                clock.Object);
        }

        public string StartedCode { get; private set; } = string.Empty;

        public Task<Result<ServiceRequestResultDto>> Handle(string attributesJson) =>
            _handler.Handle(
                new CreateServiceRequestCommand(
                    _customerId,
                    _serviceId,
                    attributesJson,
                    false),
                CancellationToken.None);

        private static WorkflowDefinition BuildDefinition()
        {
            var definition = new WorkflowDefinition(
                BandwidthWorkflowCodes.WithFeasibility,
                "تست",
                WorkflowEntityType.Order,
                null,
                null,
                DateTime.UtcNow);
            definition.AddState(
                "START", "شروع", WorkflowStateType.Start, 1, false,
                WorkflowAssigneeType.None, null, null, "#000");
            definition.AddState(
                "FEASIBILITY", "فنی", WorkflowStateType.Normal, 2, false,
                WorkflowAssigneeType.None, null, null, "#000");
            definition.AddState(
                "PROFORMA", "پیش‌فاکتور", WorkflowStateType.Normal, 3, false,
                WorkflowAssigneeType.None, null, null, "#000");
            return definition;
        }

        private static WorkflowInstanceDto InstanceDto(WorkflowInstance instance) =>
            new(
                instance.Id,
                instance.DefinitionId,
                BandwidthWorkflowCodes.WithFeasibility,
                "تست",
                1,
                "Order",
                instance.EntityId,
                instance.CurrentStateId,
                "START",
                "شروع",
                "Running",
                null,
                DateTime.UtcNow,
                "1405/08/18",
                null,
                null,
                null,
                [],
                [],
                []);
    }
}
