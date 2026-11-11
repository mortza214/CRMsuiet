using CrmSuite.Modules.Payments.Domain.Entities;

namespace CrmSuite.Modules.Payments.Application.Abstractions;

public interface IPaymentGatewayTransactionRepository
{
    void Add(PaymentGatewayTransaction transaction);
}
