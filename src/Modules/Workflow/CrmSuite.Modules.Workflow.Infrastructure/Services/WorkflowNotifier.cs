using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.Modules.Notifications.Application.Abstractions;
using CrmSuite.Modules.Notifications.Domain.Entities;
using CrmSuite.Modules.Notifications.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Entities;

namespace CrmSuite.Modules.Workflow.Infrastructure.Services;

public sealed class WorkflowNotifier : IWorkflowNotifier
{
    private readonly INotificationLogRepository _logs;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public WorkflowNotifier(
        INotificationLogRepository logs,
        INotificationUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _logs = logs;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task NotifyTaskAssignedAsync(
        WorkflowTask task,
        WorkflowInstance instance,
        WorkflowDefinition definition,
        CancellationToken cancellationToken)
    {
        if (!task.AssignedUserId.HasValue)
        {
            return;
        }

        var now = _clock.UtcNow;
        var log = new NotificationLog(
            task.AssignedUserId.Value.ToString(),
            NotificationChannel.InApp,
            "WorkflowTaskAssigned",
            $"تسک جدید «{definition.Name}» برای شما ایجاد شد.",
            null,
            now);
        log.MarkSent(now);
        _logs.Add(log);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task NotifyStateEnteredAsync(
        WorkflowInstance instance,
        WorkflowState state,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
