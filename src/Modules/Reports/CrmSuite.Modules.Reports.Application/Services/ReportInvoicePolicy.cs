namespace CrmSuite.Modules.Reports.Application.Services;

public static class ReportInvoicePolicy
{
    public static bool IsOutstanding(string status, decimal totalAmount, decimal paidAmount) =>
        status is "Issued" or "PartiallyPaid" && totalAmount > paidAmount;

    public static bool IsBadDebt(
        string status,
        decimal totalAmount,
        decimal paidAmount,
        DateTime dueDateUtc,
        DateTime asOfUtc) =>
        IsOutstanding(status, totalAmount, paidAmount)
        && dueDateUtc < asOfUtc;
}
