using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Domain.Entities;

public sealed class WalletTransaction : BaseEntity
{
    private WalletTransaction()
    {
    }

    public WalletTransaction(
        Guid walletId,
        decimal amount,
        WalletTransactionType transactionType,
        Guid? referenceId,
        string? description,
        decimal balanceAfter,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        WalletId = walletId;
        Amount = amount;
        TransactionType = transactionType;
        ReferenceId = referenceId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        BalanceAfter = balanceAfter;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid WalletId { get; private set; }

    public decimal Amount { get; private set; }

    public WalletTransactionType TransactionType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Description { get; private set; }

    public decimal BalanceAfter { get; private set; }
}
