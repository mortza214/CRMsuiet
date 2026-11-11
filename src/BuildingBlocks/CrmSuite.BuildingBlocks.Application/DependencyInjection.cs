using System.Reflection;
using CrmSuite.BuildingBlocks.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CrmSuite.BuildingBlocks.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers CQRS handlers and validators from the shared application assembly
    /// plus every supplied module application assembly.
    /// </summary>
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        params Assembly[] moduleApplicationAssemblies)
    {
        var assemblies = new[] { typeof(ApplicationAssemblyMarker).Assembly }
            .Concat(moduleApplicationAssemblies)
            .Where(assembly => assembly is not null)
            .Distinct()
            .ToArray();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(assemblies);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        return services;
    }
}
