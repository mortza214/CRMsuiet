using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Infrastructure.Services;

public sealed class CommissionCalculator : ICommissionCalculator
{
    public decimal CalculateMonthly(decimal finalInvoiceAmount, decimal percentage, int months = 12)
    {
        if (finalInvoiceAmount <= 0 || percentage <= 0 || months <= 0)
        {
            return 0m;
        }

        return decimal.Floor(
            finalInvoiceAmount * percentage / 100m / months);
    }

    public decimal Calculate(CommissionRule rule, decimal itemTotal)
    {
        if (rule.FixedAmount.HasValue && rule.FixedAmount.Value > 0)
        {
            return decimal.Round(rule.FixedAmount.Value, 2, MidpointRounding.AwayFromZero);
        }

        return decimal.Round(
            itemTotal * rule.Percentage / 100m,
            2,
            MidpointRounding.AwayFromZero);
    }
}
