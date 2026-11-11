using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Payments.Application.Abstractions;
using CrmSuite.Modules.Payments.Domain.Entities;

namespace CrmSuite.Modules.Payments.Infrastructure.Persistence.Repositories;

public sealed class PaymentGatewayTransactionRepository : IPaymentGatewayTransactionRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentGatewayTransactionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(PaymentGatewayTransaction transaction) =>
        _dbContext.Set<PaymentGatewayTransaction>().Add(transaction);
}
