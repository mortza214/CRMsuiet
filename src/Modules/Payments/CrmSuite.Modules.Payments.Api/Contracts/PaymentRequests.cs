using CrmSuite.Modules.Payments.Domain.Enums;

namespace CrmSuite.Modules.Payments.Api.Contracts;

public sealed class CreateManualPaymentRequest
{
    public Guid? InvoiceId { get; init; }

    public Guid CustomerId { get; init; }

    public decimal Amount { get; init; }

    public PaymentMethod PaymentMethod { get; init; }

    public string? ReferenceNumber { get; init; }

    public string? ReceiptImageUrl { get; init; }
}

public sealed class InitiateOnlinePaymentRequest
{
    public Guid InvoiceId { get; init; }

    public string? CallbackUrl { get; init; }
}

public sealed class VerifyOnlinePaymentRequest
{
    public string TrackingCode { get; init; } = string.Empty;

    public string? Token { get; init; }
}
