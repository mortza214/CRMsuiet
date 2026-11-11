using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence.Repositories;

public sealed class ContractRepository : IContractRepository
{
    private readonly AppDbContext _dbContext;

    public ContractRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Contract?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Contract>().SingleOrDefaultAsync(contract => contract.Id == id, cancellationToken);

    public void Add(Contract contract) => _dbContext.Set<Contract>().Add(contract);
}
