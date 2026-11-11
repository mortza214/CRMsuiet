namespace CrmSuite.Modules.Wallet.Domain.Enums;

public enum WalletOwnerType
{
    Customer = 1,
    Employee = 2,
}

public enum WalletTransactionType
{
    Deposit = 1,
    Withdraw = 2,
    CommissionPayout = 3,
    InvoicePayment = 4,
}

public enum CommissionPayoutType
{
    OneTime = 1,
    Monthly = 2,
}

public enum CommissionStatus
{
    Calculated = 1,
    Payable = 2,
    Rejected = 3,
    Paid = 4,
    Pending = 5,
    Approved = 6,
}

public enum WithdrawalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
}
