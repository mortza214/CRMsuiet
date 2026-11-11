using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Payments.Application.Dtos;
using CrmSuite.Modules.Payments.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Payments.Application.Commands;

public sealed record CreateManualPaymentCommand(
    Guid? InvoiceId,
    Guid CustomerId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber) : IRequest<Result<TransactionDto>>;

public sealed record ApproveManualPaymentCommand(Guid TransactionId) : IRequest<Result<TransactionDto>>;

public sealed record InitiateOnlinePaymentCommand(
    Guid InvoiceId,
    string? CallbackUrl = null) : IRequest<Result<OnlinePaymentInitiationDto>>;

public sealed record VerifyOnlinePaymentCommand(
    string TrackingCode,
    string? Token = null) : IRequest<Result<TransactionDto>>;
