using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Domain.Entities;

public sealed class CommissionEntry : BaseEntity
{
    private CommissionEntry()
    {
    }

    public CommissionEntry(
        Guid orderId,
        Guid orderItemId,
        Guid employeeId,
        decimal amount,
        CommissionPayoutType payoutType,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (amount < 0)
        {
            throw new DomainException("مبلغ پورسانت نمی‌تواند منفی باشد.", "COMMISSION_AMOUNT_INVALID");
        }

        OrderId = orderId;
        OrderItemId = orderItemId;
        EmployeeId = employeeId;
        Amount = amount;
        PayoutType = payoutType;
        Status = CommissionStatus.Calculated;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid OrderId { get; private set; }

    public Guid OrderItemId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public decimal Amount { get; private set; }

    public CommissionStatus Status { get; private set; }

    public decimal AdjustmentAmount { get; private set; }

    public CommissionPayoutType PayoutType { get; private set; }

    public decimal NetAmount => Amount - AdjustmentAmount;

    public Guid? CustomerId { get; private set; }

    public Guid? CustomerServiceId { get; private set; }

    public string? CustomerName { get; private set; }

    public string? ServiceName { get; private set; }

    public decimal CommissionBase { get; private set; }

    public int PaidInstallments { get; private set; }

    public DateTime? LastPaidAt { get; private set; }

    public ICollection<CommissionSchedule> Schedules { get; private set; } = new List<CommissionSchedule>();

    public ICollection<CommissionShare> Shares { get; private set; } = new List<CommissionShare>();

    public void SetServiceContext(
        Guid customerId,
        Guid customerServiceId,
        string? customerName,
        string? serviceName,
        decimal commissionBase)
    {
        CustomerId = customerId;
        CustomerServiceId = customerServiceId;
        CustomerName = customerName;
        ServiceName = serviceName;
        CommissionBase = commissionBase;
    }

    public void AddShare(CommissionShare share)
    {
        if (share.CommissionEntryId != Id)
        {
            throw new DomainException("سهم به پورسانت جاری تعلق ندارد.", "COMMISSION_SHARE_MISMATCH");
        }

        Shares.Add(share);
    }

    public void ReplaceShares(IEnumerable<CommissionShare> shares)
    {
        if (Status is not (CommissionStatus.Calculated or CommissionStatus.Pending))
        {
            throw new DomainException("سهم‌ها فقط در وضعیت در انتظار تأیید قابل ویرایش هستند.", "COMMISSION_STATUS_INVALID");
        }

        var items = shares.ToArray();
        if (items.Sum(share => share.Percentage) != 100m)
        {
            throw new DomainException("مجموع درصد سهم‌ها باید ۱۰۰ باشد.", "COMMISSION_SHARES_TOTAL_INVALID");
        }

        Shares.Clear();
        foreach (var share in items)
        {
            if (share.CommissionEntryId != Id)
            {
                throw new DomainException("سهم به پورسانت جاری تعلق ندارد.", "COMMISSION_SHARE_MISMATCH");
            }

            Shares.Add(share);
        }
    }

    public void MarkInstallmentPaid(DateTime utcNow)
    {
        if (PaidInstallments >= 12)
        {
            return;
        }

        PaidInstallments++;
        LastPaidAt = utcNow;
        if (PaidInstallments >= 12)
        {
            Status = CommissionStatus.Paid;
        }

        MarkUpdated(null, utcNow);
    }

    public void AddSchedule(CommissionSchedule schedule)
    {
        if (schedule.CommissionEntryId != Id)
        {
            throw new DomainException("سررسید به پورسانت جاری تعلق ندارد.", "COMMISSION_SCHEDULE_MISMATCH");
        }

        Schedules.Add(schedule);
    }

    public void MarkPending(Guid? userId, DateTime utcNow)
    {
        if (Status != CommissionStatus.Calculated)
        {
            throw new DomainException("فقط پورسانت محاسبه‌شده قابل انتقال به انتظار تأیید است.", "COMMISSION_STATUS_INVALID");
        }

        Status = CommissionStatus.Pending;
        MarkUpdated(userId, utcNow);
    }

    public void Approve(Guid? managerId, DateTime utcNow)
    {
        if (Status is not (CommissionStatus.Calculated or CommissionStatus.Pending))
        {
            throw new DomainException("فقط پورسانت محاسبه‌شده قابل تأیید است.", "COMMISSION_STATUS_INVALID");
        }

        Status = CommissionStatus.Approved;
        MarkUpdated(managerId, utcNow);
    }

    public void Reject(Guid? managerId, DateTime utcNow)
    {
        if (Status is CommissionStatus.Paid or CommissionStatus.Rejected)
        {
            throw new DomainException("پورسانت در وضعیت قابل رد نیست.", "COMMISSION_STATUS_INVALID");
        }

        Status = CommissionStatus.Rejected;
        MarkUpdated(managerId, utcNow);
    }

    public void MarkPaid(Guid? userId, DateTime utcNow)
    {
        if (Status is not (CommissionStatus.Payable or CommissionStatus.Approved))
        {
            throw new DomainException("پورسانت در وضعیت قابل پرداخت نیست.", "COMMISSION_STATUS_INVALID");
        }

        Status = CommissionStatus.Paid;
        MarkUpdated(userId, utcNow);
    }
}
