using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Workflow.Application.Services;

public sealed class WorkflowActorAccessor : IWorkflowActorAccessor
{
    private readonly FoundationCurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IPermissionProvider _permissions;

    public WorkflowActorAccessor(
        FoundationCurrentUser currentUser,
        IUserRepository users,
        IPermissionProvider permissions)
    {
        _currentUser = currentUser;
        _users = users;
        _permissions = permissions;
    }

    public async Task<WorkflowActor> GetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return new WorkflowActor(
                null,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                Array.Empty<string>());
        }

        var user = await _users.GetByIdWithAccessAsync(
            _currentUser.UserId.Value,
            cancellationToken);

        if (user is null)
        {
            return new WorkflowActor(
                _currentUser.UserId,
                Array.Empty<Guid>(),
                Array.Empty<Guid>(),
                await _permissions.GetPermissionsAsync(
                    _currentUser.UserId.Value,
                    cancellationToken));
        }

        return new WorkflowActor(
            user.Id,
            user.UserRoles.Select(userRole => userRole.RoleId).Distinct().ToArray(),
            user.UserDepartments
                .Select(userDepartment => userDepartment.DepartmentId)
                .Distinct()
                .ToArray(),
            await _permissions.GetPermissionsAsync(user.Id, cancellationToken));
    }
}

public sealed class WorkflowEntityCustomerResolver : IWorkflowEntityCustomerResolver
{
    private readonly ICustomerRepository _customers;
    private readonly IOrderRepository _orders;
    private readonly ITicketRepository _tickets;

    public WorkflowEntityCustomerResolver(
        ICustomerRepository customers,
        IOrderRepository orders,
        ITicketRepository tickets)
    {
        _customers = customers;
        _orders = orders;
        _tickets = tickets;
    }

    public async Task<Guid?> GetCustomerOwnerUserIdAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        Guid? customerId = entityType switch
        {
            WorkflowEntityType.Customer => entityId,
            WorkflowEntityType.Order => (await _orders.GetByIdWithDetailsAsync(entityId, cancellationToken))?.CustomerId,
            WorkflowEntityType.Ticket => (await _tickets.GetByIdWithDetailsAsync(entityId, cancellationToken))?.CustomerId,
            _ => null,
        };

        if (!customerId.HasValue)
        {
            return null;
        }

        var customer = await _customers.GetByIdAsync(
            customerId.Value,
            includeDetails: false,
            cancellationToken);
        return customer?.OwnerUserId;
    }
    public async Task<Guid?> GetCustomerPortalUserIdAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        Guid? customerId = entityType switch
        {
            WorkflowEntityType.Customer => entityId,
            WorkflowEntityType.Order => (await _orders.GetByIdWithDetailsAsync(entityId, cancellationToken))?.CustomerId,
            WorkflowEntityType.Ticket => (await _tickets.GetByIdWithDetailsAsync(entityId, cancellationToken))?.CustomerId,
            _ => null,
        };

        if (!customerId.HasValue)
        {
            return null;
        }

        var customer = await _customers.GetByIdAsync(
            customerId.Value,
            includeDetails: false,
            cancellationToken);
        return customer?.UserId;
    }

}

public sealed class WorkflowEntityLinker : IWorkflowEntityLinker
{
    private readonly IOrderRepository _orders;
    private readonly IOrderUnitOfWork _orderUnitOfWork;
    private readonly ITicketRepository _tickets;
    private readonly ITicketUnitOfWork _ticketUnitOfWork;

    public WorkflowEntityLinker(
        IOrderRepository orders,
        IOrderUnitOfWork orderUnitOfWork,
        ITicketRepository tickets,
        ITicketUnitOfWork ticketUnitOfWork)
    {
        _orders = orders;
        _orderUnitOfWork = orderUnitOfWork;
        _tickets = tickets;
        _ticketUnitOfWork = ticketUnitOfWork;
    }

    public async Task LinkAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        switch (instance.EntityType)
        {
            case WorkflowEntityType.Order:
            {
                var order = await _orders.GetByIdWithDetailsAsync(instance.EntityId, cancellationToken);
                if (order is not null)
                {
                    order.AttachWorkflowInstance(instance.Id, DateTime.UtcNow);
                    await _orderUnitOfWork.SaveChangesAsync(cancellationToken);
                }

                break;
            }

            case WorkflowEntityType.Ticket:
            {
                var ticket = await _tickets.GetByIdWithDetailsAsync(instance.EntityId, cancellationToken);
                if (ticket is not null)
                {
                    ticket.AttachWorkflowInstance(instance.Id, DateTime.UtcNow);
                    await _ticketUnitOfWork.SaveChangesAsync(cancellationToken);
                }

                break;
            }
        }
    }
}

public sealed class WorkflowStarter : IWorkflowStarter
{
    private readonly IMediator _mediator;
    private readonly IWorkflowDefinitionRepository _definitions;

    public WorkflowStarter(IMediator mediator, IWorkflowDefinitionRepository definitions)
    {
        _mediator = mediator;
        _definitions = definitions;
    }

    public async Task<WorkflowInstanceDto?> TryStartAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        string? definitionCode,
        string? initialPayload,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        WorkflowDefinition? definition;
        if (string.IsNullOrWhiteSpace(definitionCode))
        {
            definition = await _definitions.GetPublishedByEntityTypeAsync(entityType, cancellationToken);
        }
        else
        {
            definition = await _definitions.GetByCodeAsync(definitionCode.Trim(), cancellationToken);
        }

        if (definition is null || definition.Status != WorkflowDefinitionStatus.Published)
        {
            return null;
        }

        var result = await _mediator.Send(
            new StartWorkflowCommand(
                entityType,
                entityId,
                definition.Code,
                initialPayload,
                idempotencyKey),
            cancellationToken);

        return result.IsSuccess ? result.Value : null;
    }
}
