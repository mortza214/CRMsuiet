using System.Globalization;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Notifications;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using MediatR;

namespace CrmSuite.Modules.Billing.Infrastructure.Jobs;

/// <summary>
/// Creates one snapshot-priced invoice for each active bandwidth service whose next invoice is due.
/// The job is registered as a Hangfire recurring job; NextInvoiceAt prevents duplicate runs.
/// </summary>
public sealed class MonthlyBandwidthInvoiceJob
{
    private readonly ICustomerServiceRepository _services;
    private readonly IInvoiceRepository _invoices;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly ISequenceService _sequences;
    private readonly ISettingService _settings;
    private readonly IMediator _mediator;
    private readonly IDateTimeProvider _clock;

    public MonthlyBandwidthInvoiceJob(
        ICustomerServiceRepository services,
        IInvoiceRepository invoices,
        IBillingUnitOfWork unitOfWork,
        ISequenceService sequences,
        ISettingService settings,
        IMediator mediator,
        IDateTimeProvider clock)
    {
        _services = services;
        _invoices = invoices;
        _unitOfWork = unitOfWork;
        _sequences = sequences;
        _settings = settings;
        _mediator = mediator;
        _clock = clock;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var due = await _services.GetDueForInvoicingAsync(now, cancellationToken);
        if (due.Count == 0)
        {
            return;
        }

        var vat = await ReadPercentageAsync("Billing.VatPercentage", cancellationToken);
        var insurance = await ReadPercentageAsync("Billing.InsurancePercentage", cancellationToken);

        foreach (var service in due)
        {
            var amount = service.MonthlyAmount;
            if (amount <= 0)
            {
                continue;
            }

            var number = await _sequences.GetNextValueAsync("InvoiceNumber", cancellationToken);
            var invoice = new Invoice(
                number,
                service.OrderId,
                service.CustomerId,
                BillingInvoiceType.Monthly,
                now,
                now.AddDays(7),
                0,
                null,
                now);
            invoice.AddItem(new InvoiceItem(
                invoice.Id,
                $"{service.ServiceName} - دوره ماهانه",
                1,
                amount,
                now));
            invoice.Issue(vat, insurance, null, now);

            _invoices.Add(invoice);
            service.MarkInvoiced(now, now.AddMonths(1), null);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(
                new MonthlyInvoiceIssuedNotification(
                    invoice.Id,
                    service.CustomerId,
                    invoice.InvoiceNumber,
                    invoice.TotalAmount),
                cancellationToken);
        }
    }

    private async Task<decimal> ReadPercentageAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var setting = await _settings.GetAsync(key, false, cancellationToken);
        return decimal.TryParse(
            setting?.Value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0m;
    }
}
