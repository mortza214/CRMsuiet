using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Domain.Entities;

public sealed class CommissionRule : BaseEntity
{
    private CommissionRule()
    {
    }

    public CommissionRule(
        Guid? serviceId,
        ServiceType? serviceType,
        decimal percentage,
        decimal? fixedAmount,
        CommissionPayoutType payoutType,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new DomainException("درصد پورسانت باید بین ۰ تا ۱۰۰ باشد.", "COMMISSION_PERCENTAGE_INVALID");
        }

        if (fixedAmount is <= 0)
        {
            throw new DomainException("مبلغ ثابت پورسانت باید بزرگ‌تر از صفر باشد.", "COMMISSION_FIXED_INVALID");
        }

        if (percentage == 0 && fixedAmount is null)
        {
            throw new DomainException("درصد یا مبلغ ثابت پورسانت الزامی است.", "COMMISSION_VALUE_REQUIRED");
        }

        ServiceId = serviceId;
        ServiceType = serviceType;
        Percentage = percentage;
        FixedAmount = fixedAmount;
        PayoutType = payoutType;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid? ServiceId { get; private set; }

    public ServiceType? ServiceType { get; private set; }

    public decimal Percentage { get; private set; }

    public decimal? FixedAmount { get; private set; }

    public CommissionPayoutType PayoutType { get; private set; }

    public bool Matches(Guid? serviceId, ServiceType? serviceType) =>
        (ServiceId.HasValue && ServiceId == serviceId)
        || (!ServiceId.HasValue && ServiceType.HasValue && ServiceType == serviceType)
        || (!ServiceId.HasValue && !ServiceType.HasValue);
}
