using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Actions;
using CrmSuite.Modules.Workflow.Application.Mapping;
using CrmSuite.Modules.Workflow.Application.Services;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Infrastructure.Persistence;
using CrmSuite.Modules.Workflow.Infrastructure.Persistence.Repositories;
using CrmSuite.Modules.Workflow.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Workflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IWorkflowUnitOfWork, EfWorkflowUnitOfWork>();
        services.AddScoped<IWorkflowActorAccessor, WorkflowActorAccessor>();
        services.AddScoped<IWorkflowEntityCustomerResolver, WorkflowEntityCustomerResolver>();
        services.AddScoped<IWorkflowEntityLinker, WorkflowEntityLinker>();
        services.AddScoped<IWorkflowNotifier, WorkflowNotifier>();
        services.AddScoped<WorkflowAssignmentResolver>();
        services.AddScoped<WorkflowDtoMapper>();
        services.AddScoped<IWorkflowStarter, WorkflowStarter>();
        services.AddScoped<WorkflowSeedService>();
        services.AddScoped<BandwidthWorkflowSupport>();
        services.AddScoped<BandwidthPhase6Support>();
        services.AddScoped<WorkflowBillingActionSupport>();
        services.AddScoped<IWorkflowAction, DecideSiteVisitAction>();
        services.AddScoped<IWorkflowAction, FillFormAction>();
        services.AddScoped<IWorkflowAction, ReviewFeasibilityAction>();
        services.AddScoped<IWorkflowAction, IssueProformaAction>();
        services.AddScoped<IWorkflowAction, IssueFinalInvoiceAction>();
        services.AddScoped<IWorkflowAction, RequestPaymentAction>();
        services.AddScoped<IWorkflowAction, ActivateServiceAction>();
        services.AddScoped<IWorkflowAction, SelectEquipmentAction>();
        services.AddScoped<IWorkflowAction, PurchaseEquipmentAction>();
        services.AddScoped<IWorkflowAction, CustomerAction>();
        services.AddScoped<IWorkflowAction, PayInvoiceAction>();
        services.AddScoped<IWorkflowAction, VerifyPaymentAction>();
        services.AddScoped<IWorkflowAction, SignContractAction>();
        services.AddScoped<IWorkflowAction, ReviewInstallationAction>();
        services.AddScoped<IWorkflowEntityDisplayResolver, WorkflowEntityDisplayResolver>();

        return services;
    }
}
