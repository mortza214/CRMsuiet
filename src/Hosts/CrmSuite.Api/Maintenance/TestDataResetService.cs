using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Identity.Domain.Entities;
using CrmSuite.Modules.Notifications.Domain.Entities;
using CrmSuite.Modules.Orders.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Payments.Domain.Entities;
using CrmSuite.Modules.Wallet.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Api.Maintenance;

public sealed record TestDataResetResult(
    int WorkflowInstances,
    int Orders,
    int Invoices,
    int Payments,
    int Commissions,
    int FormSubmissions,
    int CustomerServices,
    int Customers,
    int Users);

/// <summary>Development-only reset used to return the database to a clean demo state.</summary>
public sealed class TestDataResetService
{
    private readonly AppDbContext _dbContext;

    public TestDataResetService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TestDataResetResult> ResetAsync(CancellationToken cancellationToken) =>
        _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            async () => await ResetCoreAsync(cancellationToken));

    private async Task<TestDataResetResult> ResetCoreAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.Set<WorkflowHistory>().ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Set<WorkflowTask>().ExecuteDeleteAsync(cancellationToken);
        var workflowInstances = await _dbContext.Set<WorkflowInstance>().ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Set<CommissionSchedule>().ExecuteDeleteAsync(cancellationToken);
        var commissions = await _dbContext.Set<CommissionEntry>().ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Set<PaymentGatewayTransaction>().ExecuteDeleteAsync(cancellationToken);
        var payments = await _dbContext.Set<Transaction>().ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Set<InvoiceItem>().ExecuteDeleteAsync(cancellationToken);
        var invoices = await _dbContext.Set<Invoice>().ExecuteDeleteAsync(cancellationToken);

        var customerServices = await _dbContext.Set<CustomerService>().ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Set<OrderStatusHistory>().ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Set<OrderItem>().ExecuteDeleteAsync(cancellationToken);
        var orders = await _dbContext.Set<Order>().ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Set<FormSubmissionItem>().ExecuteDeleteAsync(cancellationToken);
        var formSubmissions = await _dbContext.Set<FormSubmission>().ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Set<NotificationLog>().ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Set<CustomerOtp>().ExecuteDeleteAsync(cancellationToken);
        var customers = await _dbContext.Set<Customer>().ExecuteDeleteAsync(cancellationToken);

        var superAdmin = await _dbContext.Set<User>()
            .SingleOrDefaultAsync(user => user.Username == "superadmin", cancellationToken);

        if (superAdmin is not null)
        {
            await _dbContext.Set<UserRole>()
                .Where(userRole => userRole.UserId != superAdmin.Id)
                .ExecuteDeleteAsync(cancellationToken);
            await _dbContext.Set<UserDepartment>()
                .Where(userDepartment => userDepartment.UserId != superAdmin.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync(cancellationToken);
            await _dbContext.Set<UserDepartment>().ExecuteDeleteAsync(cancellationToken);
        }

        await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync(cancellationToken);

        var testUsers = superAdmin is null
            ? await _dbContext.Set<User>().ExecuteDeleteAsync(cancellationToken)
            : await _dbContext.Set<User>()
                .Where(user => user.Id != superAdmin.Id)
                .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Set<AuditLog>().ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Set<Sequence>()
            .ExecuteUpdateAsync(
                update => update.SetProperty(sequence => sequence.CurrentValue, 0L),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new TestDataResetResult(
            workflowInstances,
            orders,
            invoices,
            payments,
            commissions,
            formSubmissions,
            customerServices,
            customers,
            testUsers);
    }
}
