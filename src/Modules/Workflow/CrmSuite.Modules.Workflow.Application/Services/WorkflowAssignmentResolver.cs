using CrmSuite.BuildingBlocks.Domain.Exceptions;
using System.Diagnostics.CodeAnalysis;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Services;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Assignment resolution is a DI service with a stable interface.")]
public sealed class WorkflowAssignmentResolver
{
    public WorkflowAssignment Resolve(
        WorkflowState state,
        Guid? customerOwnerUserId,
        Guid? customerUserId = null)
    {
        return state.AssigneeType switch
        {
            WorkflowAssigneeType.None => new WorkflowAssignment(null, null, null),
            WorkflowAssigneeType.User => new WorkflowAssignment(
                ParseGuid(state.AssigneeValue, "کاربر"),
                null,
                null),
            WorkflowAssigneeType.Role => new WorkflowAssignment(
                null,
                ParseGuid(state.AssigneeValue, "نقش"),
                null),
            WorkflowAssigneeType.Department => new WorkflowAssignment(
                null,
                null,
                ParseGuid(state.AssigneeValue, "دپارتمان")),
            WorkflowAssigneeType.CustomerOwner => new WorkflowAssignment(
                customerOwnerUserId,
                null,
                null),
            WorkflowAssigneeType.Customer => new WorkflowAssignment(
                customerUserId,
                null,
                null),
            _ => throw new DomainException("نوع مسئول وضعیت معتبر نیست.", "WORKFLOW_ASSIGNEE_INVALID"),
        };
    }

    private static Guid? ParseGuid(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var id)
            ? id
            : throw new DomainException(
                $"شناسه {label} برای اختصاص تسک معتبر نیست.",
                "WORKFLOW_ASSIGNEE_VALUE_INVALID");
    }
}
