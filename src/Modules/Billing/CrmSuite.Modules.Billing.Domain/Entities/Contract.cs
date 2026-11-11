using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Billing.Domain.Entities;

public sealed class Contract : BaseEntity
{
    private Contract()
    {
    }

    public Contract(
        string contractNumber,
        Guid customerId,
        Guid? orderId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string content,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(contractNumber))
        {
            throw new DomainException("شماره قرارداد الزامی است.", "BILLING_CONTRACT_NUMBER_REQUIRED");
        }

        if (endDateUtc < startDateUtc)
        {
            throw new DomainException("تاریخ پایان قرارداد باید بعد از شروع باشد.", "BILLING_CONTRACT_DATES_INVALID");
        }

        ContractNumber = contractNumber.Trim();
        CustomerId = customerId;
        OrderId = orderId;
        StartDate = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
        EndDate = DateTime.SpecifyKind(endDateUtc, DateTimeKind.Utc);
        Content = content;
        Status = ContractStatus.Draft;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public string ContractNumber { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public Guid? OrderId { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public ContractStatus Status { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public void Activate(Guid? userId, DateTime utcNow)
    {
        if (Status is ContractStatus.Terminated or ContractStatus.Expired)
        {
            throw new DomainException("قرارداد خاتمه‌یافته قابل فعال‌سازی نیست.", "BILLING_CONTRACT_FINALIZED");
        }

        Status = ContractStatus.Active;
        MarkUpdated(userId, utcNow);
    }

    public void Terminate(Guid? userId, DateTime utcNow)
    {
        if (Status == ContractStatus.Terminated)
        {
            return;
        }

        Status = ContractStatus.Terminated;
        MarkUpdated(userId, utcNow);
    }
}
