using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Payments.Application.Dtos;
using CrmSuite.Modules.Payments.Domain.Entities;

namespace CrmSuite.Modules.Payments.Application.Mapping;

public sealed class PaymentDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public PaymentDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public TransactionDto ToDto(
        Transaction transaction,
        string? invoiceNumber,
        string? customerName)
    {
        var createdAt = AsUtc(transaction.CreatedAt);
        var paidAt = transaction.PaidAt.HasValue ? AsUtc(transaction.PaidAt.Value) : (DateTime?)null;

        return new TransactionDto(
            transaction.Id,
            transaction.InvoiceId,
            invoiceNumber,
            transaction.CustomerId,
            customerName,
            transaction.Amount,
            transaction.PaymentMethod.ToString(),
            transaction.GatewayName.ToString(),
            transaction.ReferenceNumber,
            transaction.TrackingCode,
            transaction.Status.ToString(),
            paidAt,
            paidAt.HasValue ? _jalaliDate.ToJalali(paidAt.Value) : null,
            transaction.GatewayResponse,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
