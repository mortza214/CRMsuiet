namespace CrmSuite.Modules.Tickets.Application.Abstractions;

public interface INotificationService
{
    Task NotifyDepartmentAsync(
        Guid departmentId,
        string title,
        string body,
        CancellationToken cancellationToken);
}
