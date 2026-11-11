namespace CrmSuite.Modules.Billing.Infrastructure.Jobs;

/// <summary>Named job for the phase-6 recurring bandwidth invoice process.</summary>
public sealed class GenerateRecurringInvoiceJob
{
    private readonly MonthlyBandwidthInvoiceJob _inner;

    public GenerateRecurringInvoiceJob(MonthlyBandwidthInvoiceJob inner)
    {
        _inner = inner;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken) =>
        _inner.ExecuteAsync(cancellationToken);
}
