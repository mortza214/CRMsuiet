using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Dtos;
using CrmSuite.Modules.Wallet.Domain.Entities;
using WalletEntity = CrmSuite.Modules.Wallet.Domain.Entities.Wallet;

namespace CrmSuite.Modules.Wallet.Application.Mapping;

public sealed class WalletDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public WalletDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public WalletDto ToWalletDto(WalletEntity wallet) =>
        new(wallet.Id, wallet.OwnerId, wallet.OwnerType.ToString(), wallet.Balance);

    public WithdrawalDto ToWithdrawalDto(WithdrawalRequest request)
    {
        var requestedAt = AsUtc(request.CreatedAt);
        var approvedAt = request.ApprovedAt.HasValue ? AsUtc(request.ApprovedAt.Value) : (DateTime?)null;

        return new WithdrawalDto(
            request.Id,
            request.WalletId,
            request.EmployeeId,
            request.Amount,
            request.Status.ToString(),
            requestedAt,
            _jalaliDate.ToJalali(requestedAt),
            approvedAt,
            approvedAt.HasValue ? _jalaliDate.ToJalali(approvedAt.Value) : null,
            request.DecisionNote);
    }

    public CommissionEntryDto ToCommissionDto(
        CommissionEntry entry,
        string? employeeName,
        string? orderNumber)
    {
        var createdAt = AsUtc(entry.CreatedAt);

        return new CommissionEntryDto(
            entry.Id,
            entry.OrderId,
            orderNumber,
            entry.OrderItemId,
            entry.EmployeeId,
            employeeName,
            entry.Amount,
            entry.AdjustmentAmount,
            entry.NetAmount,
            entry.Status.ToString(),
            entry.PayoutType.ToString(),
            entry.Schedules.Count,
            entry.Schedules
                .OrderBy(schedule => schedule.DueDate)
                .Select(schedule => new CommissionScheduleDto(
                    schedule.Id,
                    AsUtc(schedule.DueDate),
                    _jalaliDate.ToJalali(AsUtc(schedule.DueDate)),
                    schedule.Amount,
                    schedule.IsPaid,
                    schedule.PaidAt.HasValue ? AsUtc(schedule.PaidAt.Value) : null))
                .ToArray(),
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            entry.CustomerName,
            entry.ServiceName,
            entry.PaidInstallments,
            entry.Shares.Select(share => new CommissionShareDto(
                share.Id,
                share.UserId,
                share.Percentage,
                share.Amount,
                share.IsCompanyShare)).ToArray());
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
