using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _dbContext;

    public OrderRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Set<Order>()
            .Include(order => order.Items)
            .Include(order => order.StatusHistory)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public void Add(Order order) => _dbContext.Set<Order>().Add(order);
}
