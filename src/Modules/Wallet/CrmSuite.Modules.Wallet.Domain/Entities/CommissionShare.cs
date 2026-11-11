using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Domain.Entities;

public sealed class CommissionShare : BaseEntity
{
    private CommissionShare()
    {
    }

    public CommissionShare(
        Guid commissionEntryId,
        Guid? userId,
        decimal percentage,
        decimal amount,
        bool isCompanyShare)
        : base(Guid.NewGuid())
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new DomainException("درصد سهم پورسانت معتبر نیست.", "COMMISSION_SHARE_PERCENT_INVALID");
        }

        if (amount < 0)
        {
            throw new DomainException("مبلغ سهم پورسانت معتبر نیست.", "COMMISSION_SHARE_AMOUNT_INVALID");
        }

        CommissionEntryId = commissionEntryId;
        UserId = userId;
        Percentage = percentage;
        Amount = amount;
        IsCompanyShare = isCompanyShare;
    }

    public Guid CommissionEntryId { get; private set; }

    public Guid? UserId { get; private set; }

    public decimal Percentage { get; private set; }

    public decimal Amount { get; private set; }

    public bool IsCompanyShare { get; private set; }
}
