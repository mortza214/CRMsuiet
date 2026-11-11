using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Payments.Domain.Enums;
using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Api.Contracts;

public sealed class ChargeWalletRequest
{
    public Guid CustomerId { get; init; }

    public decimal Amount { get; init; }

    public PaymentMethod PaymentMethod { get; init; }

    public string? ReferenceNumber { get; init; }
}

public sealed class RequestWithdrawRequest
{
    public decimal Amount { get; init; }
}

public sealed class UpdateCommissionSharesRequest
{
    public IReadOnlyList<CommissionShareItemRequest> Shares { get; init; } = [];
}

public sealed class CommissionShareItemRequest
{
    public Guid? UserId { get; init; }

    public decimal Percentage { get; init; }

    public bool IsCompanyShare { get; init; }
}

public sealed class CreateCommissionRuleRequest
{
    public Guid? ServiceId { get; init; }

    public ServiceType? ServiceType { get; init; }

    public decimal Percentage { get; init; }

    public decimal? FixedAmount { get; init; }

    public CommissionPayoutType PayoutType { get; init; }
}
