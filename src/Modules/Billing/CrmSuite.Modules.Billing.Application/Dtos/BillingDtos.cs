namespace CrmSuite.Modules.Billing.Application.Dtos;

public sealed record InvoiceItemDto(
    Guid Id,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string? OrderNumber,
    Guid CustomerId,
    string? CustomerName,
    string Type,
    DateTime IssueDate,
    string IssueDateJalali,
    DateTime DueDate,
    string DueDateJalali,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal InsuranceAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    string Status,
    IReadOnlyList<InvoiceItemDto> Items,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string? OrderNumber,
    Guid CustomerId,
    string? CustomerName,
    string Type,
    DateTime IssueDate,
    string IssueDateJalali,
    DateTime DueDate,
    string DueDateJalali,
    decimal TotalAmount,
    decimal PaidAmount,
    string Status);

public sealed record ContractDto(
    Guid Id,
    string ContractNumber,
    Guid CustomerId,
    string? CustomerName,
    Guid? OrderId,
    DateTime StartDate,
    string StartDateJalali,
    DateTime EndDate,
    string EndDateJalali,
    string Status,
    string Content,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record InvoicePdfModel(
    string InvoiceNumber,
    string CustomerName,
    string Type,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal InsuranceAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    string Status,
    IReadOnlyList<InvoiceItemDto> Items);

public sealed record ContractPdfModel(
    string ContractNumber,
    string CustomerName,
    DateTime StartDateUtc,
    DateTime EndDateUtc,
    string Status,
    string Content);
