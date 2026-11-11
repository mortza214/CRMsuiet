namespace CrmSuite.Modules.Payments.Application.Abstractions;

public sealed record SamanPaymentRequestResult(
    bool Success,
    string? Token,
    string? PaymentUrl,
    string RequestPayload,
    string? ResponsePayload,
    string? Error);

public sealed record SamanVerifyResult(
    bool Success,
    string? ReferenceNumber,
    string? ResponsePayload,
    string? Error);

public interface ISamanGateway
{
    Task<SamanPaymentRequestResult> RequestPaymentAsync(
        decimal amount,
        Guid invoiceId,
        string callbackUrl,
        CancellationToken cancellationToken);

    Task<SamanVerifyResult> VerifyPaymentAsync(
        string token,
        decimal amount,
        CancellationToken cancellationToken);
}
