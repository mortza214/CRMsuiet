using CrmSuite.Modules.Wallet.Domain.Entities;

namespace CrmSuite.Modules.Wallet.Application.Abstractions;

public interface ICommissionCalculator
{
    decimal Calculate(CommissionRule rule, decimal itemTotal);

    decimal CalculateMonthly(decimal finalInvoiceAmount, decimal percentage, int months = 12);
}
