namespace CrmSuite.Modules.Payments.Application.Dtos;

public sealed record TransactionDto(
    Guid Id,
    Guid? InvoiceId,
    string? InvoiceNumber,
    Guid CustomerId,
    string? CustomerName,
    decimal Amount,
    string PaymentMethod,
    string GatewayName,
    string? ReferenceNumber,
    string TrackingCode,
    string Status,
    DateTime? PaidAt,
    string? PaidAtJalali,
    string? GatewayResponse,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record TransactionListItemDto(
    Guid Id,
    Guid? InvoiceId,
    string? InvoiceNumber,
    Guid CustomerId,
    string? CustomerName,
    decimal Amount,
    string PaymentMethod,
    string GatewayName,
    string? ReferenceNumber,
    string TrackingCode,
    string Status,
    DateTime? PaidAt,
    string? PaidAtJalali,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record OnlinePaymentInitiationDto(
    Guid TransactionId,
    string TrackingCode,
    string Token,
    string PaymentUrl);
