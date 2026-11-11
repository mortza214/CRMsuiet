namespace CrmSuite.Modules.Payments.Domain.Enums;

public enum PaymentMethod
{
    OnlineGateway = 1,
    ManualTransfer = 2,
    Cash = 3,
    Wallet = 4,
}

public enum PaymentGatewayName
{
    Saman = 1,
    None = 2,
}

public enum TransactionStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3,
    Refunded = 4,
}
