using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Domain.Entities;

public sealed class Wallet : BaseEntity
{
    private Wallet()
    {
    }

    public Wallet(
        Guid ownerId,
        WalletOwnerType ownerType,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException("مالک کیف پول الزامی است.", "WALLET_OWNER_REQUIRED");
        }

        OwnerId = ownerId;
        OwnerType = ownerType;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid OwnerId { get; private set; }

    public WalletOwnerType OwnerType { get; private set; }

    public decimal Balance { get; private set; }

    public ICollection<WalletTransaction> Transactions { get; private set; } = new List<WalletTransaction>();

    public WalletTransaction Deposit(
        decimal amount,
        WalletTransactionType type,
        Guid? referenceId,
        string? description,
        Guid? userId,
        DateTime utcNow)
    {
        EnsurePositive(amount);
        if (type is not (WalletTransactionType.Deposit or WalletTransactionType.CommissionPayout))
        {
            throw new DomainException("نوع تراکنش برای واریز معتبر نیست.", "WALLET_TRANSACTION_TYPE_INVALID");
        }

        Balance += amount;
        var transaction = new WalletTransaction(
            Id,
            amount,
            type,
            referenceId,
            description,
            Balance,
            userId,
            utcNow);

        Transactions.Add(transaction);
        MarkUpdated(userId, utcNow);
        return transaction;
    }

    public WalletTransaction Withdraw(
        decimal amount,
        WalletTransactionType type,
        Guid? referenceId,
        string? description,
        Guid? userId,
        DateTime utcNow)
    {
        EnsurePositive(amount);
        if (type is not (WalletTransactionType.Withdraw or WalletTransactionType.InvoicePayment))
        {
            throw new DomainException("نوع تراکنش برای برداشت معتبر نیست.", "WALLET_TRANSACTION_TYPE_INVALID");
        }

        if (Balance < amount)
        {
            throw new DomainException("موجودی کیف پول کافی نیست.", "WALLET_INSUFFICIENT_BALANCE");
        }

        Balance -= amount;
        var transaction = new WalletTransaction(
            Id,
            amount,
            type,
            referenceId,
            description,
            Balance,
            userId,
            utcNow);

        Transactions.Add(transaction);
        MarkUpdated(userId, utcNow);
        return transaction;
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("مبلغ تراکنش باید بزرگ‌تر از صفر باشد.", "WALLET_AMOUNT_INVALID");
        }
    }
}
