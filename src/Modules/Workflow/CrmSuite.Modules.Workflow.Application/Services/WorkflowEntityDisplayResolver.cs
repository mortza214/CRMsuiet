using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Tickets.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Services;

public sealed record WorkflowEntityDisplay(
    string DisplayName,
    string Reference);

public interface IWorkflowEntityDisplayResolver
{
    Task<WorkflowEntityDisplay> ResolveAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken);
}

public sealed class WorkflowEntityDisplayResolver : IWorkflowEntityDisplayResolver
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;

    public WorkflowEntityDisplayResolver(
        IOrderRepository orders,
        ICustomerRepository customers,
        ITicketRepository tickets)
    {
        _orders = orders;
        _customers = customers;
        _tickets = tickets;
    }

    public async Task<WorkflowEntityDisplay> ResolveAsync(
        WorkflowEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        switch (entityType)
        {
            case WorkflowEntityType.Order:
            {
                var order = await _orders.GetByIdWithDetailsAsync(entityId, cancellationToken);
                if (order is null)
                {
                    return new WorkflowEntityDisplay("-", entityId.ToString());
                }

                var customer = await _customers.GetByIdAsync(
                    order.CustomerId,
                    includeDetails: false,
                    cancellationToken);
                return new WorkflowEntityDisplay(
                    customer?.DisplayName ?? "-",
                    order.OrderNumber);
            }

            case WorkflowEntityType.Ticket:
            {
                var ticket = await _tickets.GetByIdWithDetailsAsync(entityId, cancellationToken);
                return ticket is null
                    ? new WorkflowEntityDisplay("-", entityId.ToString())
                    : new WorkflowEntityDisplay(ticket.Subject, ticket.TicketNumber);
            }

            case WorkflowEntityType.Customer:
            {
                var customer = await _customers.GetByIdAsync(
                    entityId,
                    includeDetails: false,
                    cancellationToken);
                return new WorkflowEntityDisplay(
                    customer?.DisplayName ?? "-",
                    entityId.ToString());
            }

            default:
                return new WorkflowEntityDisplay("-", entityId.ToString());
        }
    }
}
