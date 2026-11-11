using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Billing.Application.Commands;

public sealed record GenerateInvoiceFromOrderCommand(
    Guid OrderId,
    BillingInvoiceType Type,
    DateTime? DueDateUtc = null) : IRequest<Result<InvoiceDto>>;

public sealed record GenerateContractCommand(
    Guid CustomerId,
    Guid? OrderId,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string? Content = null) : IRequest<Result<ContractDto>>;

public sealed record UpdateInvoiceStatusCommand(
    Guid InvoiceId,
    InvoiceStatus Status) : IRequest<Result<InvoiceDto>>;
