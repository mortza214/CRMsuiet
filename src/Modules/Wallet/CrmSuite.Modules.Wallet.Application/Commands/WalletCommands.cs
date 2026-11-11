using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Payments.Domain.Enums;
using CrmSuite.Modules.Wallet.Application.Dtos;
using CrmSuite.Modules.Wallet.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Wallet.Application.Commands;

public sealed record ChargeCustomerWalletCommand(
    Guid CustomerId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber) : IRequest<Result<WalletDto>>;

public sealed record PayInvoiceFromWalletCommand(
    Guid InvoiceId,
    Guid? CustomerId = null) : IRequest<Result<WalletDto>>;

public sealed record RequestCommissionWithdrawCommand(
    decimal Amount) : IRequest<Result<WithdrawalDto>>;

public sealed record ApproveCommissionWithdrawCommand(
    Guid WithdrawalId,
    string? Note = null) : IRequest<Result<WithdrawalDto>>;

public sealed record EvaluateOrderCommissionCommand(
    Guid OrderId) : IRequest<Result<CommissionEvaluationResultDto>>;

public sealed record ApproveCommissionCommand(
    Guid CommissionId) : IRequest<Result<CommissionEntryDto>>;

public sealed record RejectCommissionCommand(
    Guid CommissionId) : IRequest<Result<CommissionEntryDto>>;

public sealed record CommissionShareInput(
    Guid? UserId,
    decimal Percentage,
    bool IsCompanyShare);

public sealed record UpdateCommissionSharesCommand(
    Guid CommissionId,
    IReadOnlyList<CommissionShareInput> Shares) : IRequest<Result<CommissionEntryDto>>;

public sealed record CreateCommissionRuleCommand(
    Guid? ServiceId,
    ServiceType? ServiceType,
    decimal Percentage,
    decimal? FixedAmount,
    CommissionPayoutType PayoutType) : IRequest<Result<CommissionRuleDto>>;
