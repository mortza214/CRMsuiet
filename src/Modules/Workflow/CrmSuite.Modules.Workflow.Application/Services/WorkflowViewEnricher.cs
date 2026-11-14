using CrmSuite.Modules.Identity.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Dtos;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Entities;

namespace CrmSuite.Modules.Workflow.Application.Services;

public sealed class WorkflowViewEnricher(IUserRepository users, IRoleRepository roles,
    IDepartmentRepository departments, IWorkflowActorAccessor actors)
{
    public async Task<WorkflowInstanceDto> EnrichAsync(WorkflowInstance instance, WorkflowDefinition definition,
        WorkflowInstanceDto dto, CancellationToken cancellationToken)
    {
        var actor = await actors.GetAsync(cancellationToken);
        var allowed = dto.AvailableTransitions;
        var canAct = definition.Code != BandwidthIntake.Code || BandwidthIntake.CanAct(instance, actor);
        if (!canAct) allowed = [];
        else if (definition.Code == BandwidthIntake.Code && dto.CurrentStateCode == "FEASIBILITY_REPORT")
        {
            allowed = allowed.Where(transition => (definition.FindState(transition.ToStateId).Code == "SALES_RESULT")
                == BandwidthIntake.IsManager(instance, definition, actor)).ToArray();
        }
        var names = new Dictionary<string, string?>();
        var tasks = new List<WorkflowTaskDto>();
        foreach (var task in dto.Tasks)
        {
            var key = $"{task.AssignedUserId}/{task.AssignedRoleId}/{task.AssignedDepartmentId}";
            if (!names.TryGetValue(key, out var name))
            {
                name = task.AssignedUserId.HasValue
                    ? (await users.GetByIdAsync(task.AssignedUserId.Value, cancellationToken))?.FullName
                    : task.AssignedRoleId.HasValue
                        ? (await roles.GetByIdWithPermissionsAsync(task.AssignedRoleId.Value, cancellationToken))?.DisplayName
                        : task.AssignedDepartmentId.HasValue
                            ? (await departments.GetByIdAsync(task.AssignedDepartmentId.Value, cancellationToken))?.Name : null;
                names[key] = name;
            }
            tasks.Add(task with { AssigneeName = name ?? "بدون مسئول", AvailableTransitions = allowed });
        }
        return dto with { Tasks = tasks, AvailableTransitions = allowed, CanExecute = canAct };
    }
}
