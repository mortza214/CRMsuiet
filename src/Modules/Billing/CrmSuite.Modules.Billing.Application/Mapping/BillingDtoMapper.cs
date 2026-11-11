using CrmSuite.Modules.Billing.Application.Dtos;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;

namespace CrmSuite.Modules.Billing.Application.Mapping;

public sealed class BillingDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public BillingDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public InvoiceDto ToInvoiceDto(
        Invoice invoice,
        string? customerName,
        string? orderNumber = null)
    {
        var issueDate = AsUtc(invoice.IssueDate);
        var dueDate = AsUtc(invoice.DueDate);
        var createdAt = AsUtc(invoice.CreatedAt);

        return new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.OrderId,
            orderNumber,
            invoice.CustomerId,
            customerName,
            invoice.Type.ToString(),
            issueDate,
            _jalaliDate.ToJalali(issueDate),
            dueDate,
            _jalaliDate.ToJalali(dueDate),
            invoice.SubTotal,
            invoice.DiscountAmount,
            invoice.TaxAmount,
            invoice.InsuranceAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.Status.ToString(),
            invoice.Items
                .OrderBy(item => item.CreatedAt)
                .Select(item => new InvoiceItemDto(
                    item.Id,
                    item.ItemName,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice))
                .ToArray(),
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public ContractDto ToContractDto(Contract contract, string? customerName)
    {
        var start = AsUtc(contract.StartDate);
        var end = AsUtc(contract.EndDate);
        var createdAt = AsUtc(contract.CreatedAt);

        return new ContractDto(
            contract.Id,
            contract.ContractNumber,
            contract.CustomerId,
            customerName,
            contract.OrderId,
            start,
            _jalaliDate.ToJalali(start),
            end,
            _jalaliDate.ToJalali(end),
            contract.Status.ToString(),
            contract.Content,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public InvoicePdfModel ToInvoicePdfModel(InvoiceDto invoice) =>
        new(
            invoice.InvoiceNumber,
            invoice.CustomerName ?? "مشتری",
            invoice.Type,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.SubTotal,
            invoice.DiscountAmount,
            invoice.TaxAmount,
            invoice.InsuranceAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.Status,
            invoice.Items);

    public ContractPdfModel ToContractPdfModel(ContractDto contract, string? customerName) =>
        new(
            contract.ContractNumber,
            customerName ?? "مشتری",
            contract.StartDate,
            contract.EndDate,
            contract.Status,
            contract.Content);

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
