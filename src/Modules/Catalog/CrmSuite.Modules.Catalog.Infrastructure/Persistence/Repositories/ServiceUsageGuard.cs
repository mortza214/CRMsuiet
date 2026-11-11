using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class ServiceUsageGuard : IServiceUsageGuard
{
    private readonly AppDbContext _dbContext;

    public ServiceUsageGuard(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasActiveOrderItemsForServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken) =>
        (from item in _dbContext.Set<OrderItem>()
         join order in _dbContext.Set<Order>() on item.OrderId equals order.Id
         where item.ServiceId == serviceId
               && order.Status != OrderStatus.Canceled
               && order.Status != OrderStatus.Completed
         select item.Id).AnyAsync(cancellationToken);

    public Task<bool> HasActiveOrderItemsUsingAddonAsync(
        Guid addonId,
        CancellationToken cancellationToken)
    {
        var addonKey = addonId.ToString();
        return (from item in _dbContext.Set<OrderItem>()
                join order in _dbContext.Set<Order>() on item.OrderId equals order.Id
                where order.Status != OrderStatus.Canceled
                      && order.Status != OrderStatus.Completed
                      && (item.AddonId == addonId
                          || (item.AddonsJson != null && item.AddonsJson.Contains(addonKey)))
                select item.Id).AnyAsync(cancellationToken);
    }
}
