using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Mapping;
using CrmSuite.Modules.Billing.Infrastructure.Jobs;
using CrmSuite.Modules.Billing.Infrastructure.Pdf;
using CrmSuite.Modules.Billing.Infrastructure.Persistence;
using CrmSuite.Modules.Billing.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.Modules.Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IInvoiceReadRepository, InvoiceReadRepository>();
        services.AddScoped<IBillingUnitOfWork, EfBillingUnitOfWork>();
        services.AddScoped<BillingDtoMapper>();
        services.AddScoped<IPdfGenerator, QuestPdfGenerator>();
        services.AddScoped<MonthlyBandwidthInvoiceJob>();
        services.AddScoped<GenerateRecurringInvoiceJob>();

        return services;
    }
}
