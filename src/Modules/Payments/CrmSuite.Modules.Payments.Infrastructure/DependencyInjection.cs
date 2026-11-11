using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Application.Mapping;
using CrmSuite.Modules.Payments.Application.Services;
using CrmSuite.Modules.Payments.Infrastructure.Gateways;
using CrmSuite.Modules.Payments.Infrastructure.Persistence;
using CrmSuite.Modules.Payments.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrmSuite.Modules.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SamanOptions>(configuration.GetSection(SamanOptions.SectionName));

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IPaymentGatewayTransactionRepository, PaymentGatewayTransactionRepository>();
        services.AddScoped<ITransactionReadRepository, TransactionReadRepository>();
        services.AddScoped<IPaymentUnitOfWork, EfPaymentUnitOfWork>();
        services.AddScoped<PaymentSettlementService>();
        services.AddScoped<PaymentDtoMapper>();

        services.AddScoped<ISamanGateway>(provider =>
            new SamanGateway(
                new HttpClient(),
                provider.GetRequiredService<IOptions<SamanOptions>>()));

        return services;
    }
}
