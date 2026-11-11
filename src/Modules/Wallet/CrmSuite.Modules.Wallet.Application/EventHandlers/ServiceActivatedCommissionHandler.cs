using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Notifications;
using CrmSuite.Modules.Wallet.Application.Abstractions;
using CrmSuite.Modules.Wallet.Application.Services;
using CrmSuite.Modules.Wallet.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Wallet.Application.EventHandlers;

/// <summary>
/// Sole commission entry point after a service is activated.
/// It consumes the final paid invoice amount and never runs inside sales commands.
/// </summary>
public sealed class ServiceActivatedCommissionHandler
    : INotificationHandler<ServiceActivatedEvent>
{
    private readonly ICommissionRepository _commissions;
    private readonly ICommissionCalculator _calculator;
    private readonly CommissionAllocationService _allocation;
    private readonly ICustomerRepository _customers;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ServiceActivatedCommissionHandler(
        ICommissionRepository commissions,
        ICommissionCalculator calculator,
        CommissionAllocationService allocation,
        ICustomerRepository customers,
        IWalletUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _commissions = commissions;
        _calculator = calculator;
        _allocation = allocation;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(
        ServiceActivatedEvent notification,
        CancellationToken cancellationToken)
    {
        if (notification.CommissionPercent is null or <= 0
            || notification.FinalInvoiceAmount <= 0
            || await _commissions.ExistsForOrderAsync(notification.OrderId, cancellationToken))
        {
            return;
        }

        var customer = await _customers.GetByIdAsync(
            notification.CustomerId,
            false,
            cancellationToken);

        var monthlyAmount = _calculator.CalculateMonthly(
            notification.FinalInvoiceAmount,
            notification.CommissionPercent.Value,
            12);
        if (monthlyAmount <= 0 || customer is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var entry = new CommissionEntry(
            notification.OrderId,
            notification.OrderItemId,
            notification.SalesExpertId,
            monthlyAmount,
            CommissionPayoutType.Monthly,
            null,
            now);
        entry.SetServiceContext(
            notification.CustomerId,
            notification.CustomerServiceId,
            customer.DisplayName,
            notification.ServiceName,
            notification.FinalInvoiceAmount);
        entry.MarkPending(null, now);

        var allocations = _allocation.Allocate(
            customer.OwnerType,
            customer.OwnerUserId,
            notification.SalesExpertId,
            null);

        decimal allocated = 0m;
        for (var index = 0; index < allocations.Count; index++)
        {
            var spec = allocations[index];
            var amount = index == allocations.Count - 1
                ? monthlyAmount - allocated
                : decimal.Round(
                    monthlyAmount * spec.Percentage / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
            allocated += amount;
            entry.AddShare(new CommissionShare(
                entry.Id,
                spec.UserId,
                spec.Percentage,
                amount,
                spec.IsCompanyShare));
        }

        for (var month = 1; month <= 12; month++)
        {
            entry.AddSchedule(new CommissionSchedule(
                entry.Id,
                now.AddMonths(month),
                monthlyAmount,
                now));
        }

        _commissions.Add(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
