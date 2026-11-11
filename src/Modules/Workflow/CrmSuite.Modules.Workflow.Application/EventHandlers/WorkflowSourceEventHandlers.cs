using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Notifications;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Notifications;
using CrmSuite.Modules.Tickets.Application.Notifications;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Workflow.Application.EventHandlers;

public sealed class OrderCreatedWorkflowHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IOrderRepository _orders;
    private readonly IServiceRepository _services;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowStarter _starter;

    public OrderCreatedWorkflowHandler(
        IOrderRepository orders,
        IServiceRepository services,
        IWorkflowDefinitionRepository definitions,
        IWorkflowStarter starter)
    {
        _orders = orders;
        _services = services;
        _definitions = definitions;
        _starter = starter;
    }

    public async Task Handle(
        OrderCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orders.GetByIdWithDetailsAsync(notification.OrderId, cancellationToken);
            if (order is null || order.WorkflowInstanceId.HasValue)
            {
                return;
            }

            foreach (var item in order.Items)
            {
                if (!item.ServiceId.HasValue)
                {
                    continue;
                }

                var service = await _services.GetByIdAsync(item.ServiceId.Value, cancellationToken);
                if (service?.WorkflowDefinitionId is null)
                {
                    continue;
                }

                var definition = await _definitions.GetByIdWithGraphAsync(
                    service.WorkflowDefinitionId.Value,
                    cancellationToken);
                if (definition is null)
                {
                    continue;
                }

                await _starter.TryStartAsync(
                    WorkflowEntityType.Order,
                    order.Id,
                    definition.Code,
                    "{\"source\":\"OrderCreated\"}",
                    $"order-created:{order.Id:N}",
                    cancellationToken);
                break;
            }
        }
        catch
        {
            // Workflow startup must not break the order creation flow.
        }
    }
}

public sealed class TicketCreatedWorkflowHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly IWorkflowStarter _starter;

    public TicketCreatedWorkflowHandler(IWorkflowStarter starter)
    {
        _starter = starter;
    }

    public async Task Handle(
        TicketCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await _starter.TryStartAsync(
                WorkflowEntityType.Ticket,
                notification.TicketId,
                null,
                "{\"source\":\"TicketCreated\"}",
                $"ticket-created:{notification.TicketId:N}",
                cancellationToken);
        }
        catch
        {
            // Workflow startup must not break ticket creation.
        }
    }
}

public sealed class CustomerCreatedWorkflowHandler
    : INotificationHandler<CustomerCreatedNotification>
{
    private readonly IWorkflowStarter _starter;

    public CustomerCreatedWorkflowHandler(IWorkflowStarter starter)
    {
        _starter = starter;
    }

    public async Task Handle(
        CustomerCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await _starter.TryStartAsync(
                WorkflowEntityType.Customer,
                notification.CustomerId,
                null,
                "{\"source\":\"CustomerCreated\"}",
                $"customer-created:{notification.CustomerId:N}",
                cancellationToken);
        }
        catch
        {
            // Workflow startup must not break customer creation.
        }
    }
}
