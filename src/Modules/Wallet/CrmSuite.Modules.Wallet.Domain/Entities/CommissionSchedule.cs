using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Domain.Entities;

public sealed class CommissionSchedule : BaseEntity
{
    private CommissionSchedule()
    {
    }

    public CommissionSchedule(
        Guid commissionEntryId,
        DateTime dueDateUtc,
        decimal amount,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (amount < 0)
        {
            throw new DomainException("مبلغ قسط پورسانت نمی‌تواند منفی باشد.", "COMMISSION_SCHEDULE_AMOUNT_INVALID");
        }

        CommissionEntryId = commissionEntryId;
        DueDate = DateTime.SpecifyKind(dueDateUtc, DateTimeKind.Utc);
        Amount = amount;
        MarkCreated(null, createdAtUtc);
    }

    public Guid CommissionEntryId { get; private set; }

    public DateTime DueDate { get; private set; }

    public decimal Amount { get; private set; }

    public bool IsPaid { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public void MarkPaid(DateTime utcNow)
    {
        if (IsPaid)
        {
            throw new DomainException("این قسط قبلاً پرداخت شده است.", "COMMISSION_SCHEDULE_ALREADY_PAID");
        }

        IsPaid = true;
        PaidAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }
}
