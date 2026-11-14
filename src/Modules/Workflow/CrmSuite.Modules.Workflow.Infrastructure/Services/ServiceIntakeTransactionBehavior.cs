using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Workflow.Application.Commands;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Workflow.Infrastructure.Services;

/// <summary>Keeps an intake order, its forms and its workflow in one database transaction.</summary>
public sealed class ServiceIntakeTransactionBehavior<TRequest, TResponse>(AppDbContext db)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational() || db.Database.CurrentTransaction is not null) return await next();
        var transactional = request is CreateServiceRequestCommand;
        if (request is ExecuteWorkflowActionCommand action)
            transactional = await db.Set<WorkflowInstance>().AnyAsync(instance => instance.Id == action.InstanceId
                && db.Set<WorkflowDefinition>().Any(definition => definition.Id == instance.DefinitionId && definition.Code == BandwidthIntake.Code), cancellationToken);
        if (!transactional) return await next();
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var response = await next();
                if (response is Result { IsFailure: true })
                {
                    await transaction.RollbackAsync(cancellationToken);
                    db.ChangeTracker.Clear();
                }
                else await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
