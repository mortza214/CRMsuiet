using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Domain.Entities;

public sealed class WithdrawalRequest : BaseEntity
{
    private WithdrawalRequest()
    {
    }

    public WithdrawalRequest(
        Guid walletId,
        Guid employeeId,
        decimal amount,
        Guid? requestedByUserId,
        DateTime requestedAtUtc)
        : base(Guid.NewGuid())
    {
        if (amount <= 0)
        {
            throw new DomainException("مبلغ برداشت باید بزرگ‌تر از صفر باشد.", "WALLET_WITHDRAW_AMOUNT_INVALID");
        }

        WalletId = walletId;
        EmployeeId = employeeId;
        Amount = amount;
        Status = WithdrawalStatus.Pending;
        MarkCreated(requestedByUserId, requestedAtUtc);
    }

    public Guid WalletId { get; private set; }

    public Guid EmployeeId { get; private set; }

    public decimal Amount { get; private set; }

    public WithdrawalStatus Status { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public string? DecisionNote { get; private set; }

    public void Approve(Guid? managerId, DateTime utcNow, string? note)
    {
        if (Status != WithdrawalStatus.Pending)
        {
            throw new DomainException("درخواست برداشت در وضعیت قابل تأیید نیست.", "WALLET_WITHDRAW_STATUS_INVALID");
        }

        Status = WithdrawalStatus.Approved;
        ApprovedAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        ApprovedByUserId = managerId;
        DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        MarkUpdated(managerId, utcNow);
    }

    public void Reject(Guid? managerId, DateTime utcNow, string? note)
    {
        if (Status != WithdrawalStatus.Pending)
        {
            throw new DomainException("درخواست برداشت در وضعیت قابل رد نیست.", "WALLET_WITHDRAW_STATUS_INVALID");
        }

        Status = WithdrawalStatus.Rejected;
        ApprovedAt = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        ApprovedByUserId = managerId;
        DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        MarkUpdated(managerId, utcNow);
    }
}
