using System.Reflection;

namespace CrmSuite.Api;

/// <summary>
/// Central module discovery. Each module is compiled as its own assembly; the host only needs
/// these names to register controllers, CQRS handlers and EF model configurators.
/// </summary>
public static class ModuleAssemblies
{
    public static IReadOnlyList<string> ApiNames { get; } =
    [
        "CrmSuite.Modules.Foundation.Api",
        "CrmSuite.Modules.Identity.Api",
        "CrmSuite.Modules.Customers.Api",
        "CrmSuite.Modules.Catalog.Api",
        "CrmSuite.Modules.Orders.Api",
        "CrmSuite.Modules.Billing.Api",
        "CrmSuite.Modules.Payments.Api",
        "CrmSuite.Modules.Wallet.Api",
        "CrmSuite.Modules.Commission.Api",
        "CrmSuite.Modules.Tickets.Api",
        "CrmSuite.Modules.Forms.Api",
        "CrmSuite.Modules.Notifications.Api",
        "CrmSuite.Modules.Reports.Api",
        "CrmSuite.Modules.Settings.Api",
        "CrmSuite.Modules.Workflow.Api",
    ];

    public static IReadOnlyList<string> ApplicationNames { get; } =
    [
        "CrmSuite.Modules.Foundation.Application",
        "CrmSuite.Modules.Identity.Application",
        "CrmSuite.Modules.Customers.Application",
        "CrmSuite.Modules.Catalog.Application",
        "CrmSuite.Modules.Orders.Application",
        "CrmSuite.Modules.Billing.Application",
        "CrmSuite.Modules.Payments.Application",
        "CrmSuite.Modules.Wallet.Application",
        "CrmSuite.Modules.Commission.Application",
        "CrmSuite.Modules.Tickets.Application",
        "CrmSuite.Modules.Forms.Application",
        "CrmSuite.Modules.Notifications.Application",
        "CrmSuite.Modules.Reports.Application",
        "CrmSuite.Modules.Settings.Application",
        "CrmSuite.Modules.Workflow.Application",
    ];

    public static IReadOnlyList<string> InfrastructureNames { get; } =
    [
        "CrmSuite.Modules.Foundation.Infrastructure",
        "CrmSuite.Modules.Identity.Infrastructure",
        "CrmSuite.Modules.Customers.Infrastructure",
        "CrmSuite.Modules.Catalog.Infrastructure",
        "CrmSuite.Modules.Orders.Infrastructure",
        "CrmSuite.Modules.Billing.Infrastructure",
        "CrmSuite.Modules.Payments.Infrastructure",
        "CrmSuite.Modules.Wallet.Infrastructure",
        "CrmSuite.Modules.Commission.Infrastructure",
        "CrmSuite.Modules.Tickets.Infrastructure",
        "CrmSuite.Modules.Forms.Infrastructure",
        "CrmSuite.Modules.Notifications.Infrastructure",
        "CrmSuite.Modules.Reports.Infrastructure",
        "CrmSuite.Modules.Settings.Infrastructure",
        "CrmSuite.Modules.Workflow.Infrastructure",
    ];

    public static Assembly[] LoadApiAssemblies() => Load(ApiNames);

    public static Assembly[] LoadApplicationAssemblies() => Load(ApplicationNames);

    public static Assembly[] LoadInfrastructureAssemblies() => Load(InfrastructureNames);

    private static Assembly[] Load(IEnumerable<string> assemblyNames) =>
        assemblyNames
            .Select(name => Assembly.Load(new AssemblyName(name)))
            .Distinct()
            .ToArray();
}
