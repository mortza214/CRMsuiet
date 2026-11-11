namespace CrmSuite.BuildingBlocks.Application.Security;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string SystemAdmin = "SystemAdmin";
    public const string SalesExpert = "SalesExpert";
    public const string SalesManager = "SalesManager";
    public const string FinanceManager = "FinanceManager";
    public const string FinanceExpert = "FinanceExpert";
    public const string TechnicalExpert = "TechnicalExpert";
    public const string TechnicalManager = "TechnicalManager";
    public const string PurchasingExpert = "PurchasingExpert";
    public const string Support = "Support";
    public const string DataCenterSupport = "DataCenterSupport";
    public const string ContractsExpert = "ContractsExpert";
    public const string Installer = "Installer";
    public const string Customer = "Customer";
    public const string Reseller = "Reseller";

    public static IReadOnlyCollection<string> All { get; } =
    [
        SuperAdmin,
        SystemAdmin,
        SalesExpert,
        SalesManager,
        TechnicalExpert,
        TechnicalManager,
        PurchasingExpert,
        FinanceManager,
        FinanceExpert,
        Support,
        DataCenterSupport,
        ContractsExpert,
        Installer,
        Customer,
        Reseller,
    ];
}
