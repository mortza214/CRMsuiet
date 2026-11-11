namespace CrmSuite.Modules.Catalog.Domain.Enums;

public enum ServiceType
{
    BandwidthDedicated = 1,
    BandwidthShared = 2,
    DedicatedServer = 3,
    SharedServer = 4,
    RackSpace = 5,
    PhysicalProduct = 6,
    Other = 7,
}

public enum PricingMode
{
    UnitBased = 1,
    Fixed = 2,
    Additive = 3,
}

public enum BillingPeriod
{
    Monthly = 1,
    OneTime = 2,
    Yearly = 3,
}

public enum AddonPricingMode
{
    Fixed = 1,
    PerUnit = 2,
}

public enum ServiceUnit
{
    BandwidthMbps = 1,
    Package = 2,
    RackUnit = 3,
    Piece = 4,
    Monthly = 5,
}

public enum DataCenterItemType
{
    Rack = 1,
    Power = 2,
    Bandwidth = 3,
    IP = 4,
    Extra = 5,
}
