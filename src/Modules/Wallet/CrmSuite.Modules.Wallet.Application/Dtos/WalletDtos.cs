namespace CrmSuite.Modules.Wallet.Application.Dtos;

public sealed record WalletDto(
    Guid Id,
    Guid OwnerId,
    string OwnerType,
    decimal Balance);

public sealed record WalletTransactionDto(
    Guid Id,
    Guid WalletId,
    decimal Amount,
    string TransactionType,
    Guid? ReferenceId,
    string? Description,
    decimal BalanceAfter,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record WithdrawalDto(
    Guid Id,
    Guid WalletId,
    Guid EmployeeId,
    decimal Amount,
    string Status,
    DateTime RequestedAt,
    string RequestedAtJalali,
    DateTime? ApprovedAt,
    string? ApprovedAtJalali,
    string? DecisionNote);

public sealed record CommissionScheduleDto(
    Guid Id,
    DateTime DueDate,
    string DueDateJalali,
    decimal Amount,
    bool IsPaid,
    DateTime? PaidAt);

public sealed record CommissionEntryDto(
    Guid Id,
    Guid OrderId,
    string? OrderNumber,
    Guid OrderItemId,
    Guid EmployeeId,
    string? EmployeeName,
    decimal Amount,
    decimal AdjustmentAmount,
    decimal NetAmount,
    string Status,
    string PayoutType,
    int ScheduleCount,
    IReadOnlyList<CommissionScheduleDto> Schedules,
    DateTime CreatedAt,
    string CreatedAtJalali,
    string? CustomerName = null,
    string? ServiceName = null,
    int PaidInstallments = 0,
    IReadOnlyList<CommissionShareDto>? Shares = null);

public sealed record CommissionShareDto(
    Guid Id,
    Guid? UserId,
    decimal Percentage,
    decimal Amount,
    bool IsCompanyShare);

public sealed record CommissionRuleDto(
    Guid Id,
    Guid? ServiceId,
    string? ServiceType,
    decimal Percentage,
    decimal? FixedAmount,
    string PayoutType);

public sealed record CommissionEvaluationResultDto(
    int EntriesCreated,
    decimal TotalAmount);
