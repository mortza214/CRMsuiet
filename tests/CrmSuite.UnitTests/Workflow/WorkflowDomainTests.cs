using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Services;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using FluentAssertions;

namespace CrmSuite.UnitTests.Workflow;

public sealed class WorkflowDomainTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Publish_requires_exactly_one_start_and_one_end()
    {
        var definition = new WorkflowDefinition(
            "WF-EMPTY",
            "Empty",
            WorkflowEntityType.Order,
            null,
            null,
            Now);

        var act = () => definition.Publish(null, Now);

        act.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "WORKFLOW_STATES_EMPTY");

        var stateOnly = new WorkflowDefinition(
            "WF-NO-START",
            "No start",
            WorkflowEntityType.Order,
            null,
            null,
            Now);
        stateOnly.AddState(
            "NORMAL",
            "Normal",
            WorkflowStateType.Normal,
            1,
            false,
            WorkflowAssigneeType.None,
            null,
            null,
            "#1677ff");

        var noStart = () => stateOnly.Publish(null, Now);
        noStart.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "WORKFLOW_START_REQUIRED");

        var noEnd = new WorkflowDefinition(
            "WF-NO-END",
            "No end",
            WorkflowEntityType.Order,
            null,
            null,
            Now);
        noEnd.AddState(
            "START",
            "Start",
            WorkflowStateType.Start,
            1,
            false,
            WorkflowAssigneeType.None,
            null,
            null,
            "#1677ff");
        var missingEnd = () => noEnd.Publish(null, Now);
        missingEnd.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "WORKFLOW_END_REQUIRED");
    }

    [Fact]
    public void Published_definition_cannot_be_modified()
    {
        var definition = CreatePublishedDefinition();

        var update = () => definition.UpdateDraft("Changed", null, null, Now);
        var remove = () => definition.RemoveState(definition.States.First().Id, null, Now);

        update.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "WORKFLOW_DEFINITION_NOT_DRAFT");
        remove.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "WORKFLOW_DEFINITION_NOT_DRAFT");
    }

    [Fact]
    public void Instance_is_locked_to_definition_version()
    {
        var definition = CreatePublishedDefinition();
        var start = definition.States.Single(state => state.StateType == WorkflowStateType.Start);
        var instance = new WorkflowInstance(
            definition,
            WorkflowEntityType.ManualTask,
            Guid.NewGuid(),
            start.Id,
            null,
            Now);

        instance.DefinitionVersion.Should().Be(definition.Version);
        definition.Version.Should().Be(1);
    }

    [Fact]
    public void Assignment_resolver_supports_all_five_assignee_types()
    {
        var resolver = new WorkflowAssignmentResolver();
        var owner = Guid.NewGuid();
        var user = Guid.NewGuid();
        var role = Guid.NewGuid();
        var department = Guid.NewGuid();
        var definition = new WorkflowDefinition(
            "WF-ASSIGN",
            "Assign",
            WorkflowEntityType.ManualTask,
            null,
            null,
            Now);

        var none = definition.AddState("NONE", "None", WorkflowStateType.Normal, 1, false, WorkflowAssigneeType.None, null, null, "#fff");
        var assignedUser = definition.AddState("USER", "User", WorkflowStateType.Normal, 2, false, WorkflowAssigneeType.User, user.ToString(), null, "#fff");
        var assignedRole = definition.AddState("ROLE", "Role", WorkflowStateType.Normal, 3, false, WorkflowAssigneeType.Role, role.ToString(), null, "#fff");
        var assignedDepartment = definition.AddState("DEPT", "Department", WorkflowStateType.Normal, 4, false, WorkflowAssigneeType.Department, department.ToString(), null, "#fff");
        var customerOwner = definition.AddState("OWNER", "Owner", WorkflowStateType.Normal, 5, false, WorkflowAssigneeType.CustomerOwner, null, null, "#fff");

        resolver.Resolve(none, owner).Should().Be(new WorkflowAssignment(null, null, null));
        resolver.Resolve(assignedUser, owner).UserId.Should().Be(user);
        resolver.Resolve(assignedRole, owner).RoleId.Should().Be(role);
        resolver.Resolve(assignedDepartment, owner).DepartmentId.Should().Be(department);
        resolver.Resolve(customerOwner, owner).UserId.Should().Be(owner);
    }

    [Fact]
    public void Transition_duplicate_and_self_loop_are_rejected()
    {
        var definition = new WorkflowDefinition(
            "WF-TRANS",
            "Transitions",
            WorkflowEntityType.ManualTask,
            null,
            null,
            Now);
        var start = definition.AddState("START", "Start", WorkflowStateType.Start, 1, false, WorkflowAssigneeType.None, null, null, "#fff");
        var normal = definition.AddState("NORMAL", "Normal", WorkflowStateType.Normal, 2, false, WorkflowAssigneeType.None, null, null, "#fff");

        definition.AddTransition(start.Id, normal.Id, "Go", WorkflowGuardType.None, null, null, false, 1, null, Now);

        var duplicate = () => definition.AddTransition(start.Id, normal.Id, "Go again", WorkflowGuardType.None, null, null, false, 2, null, Now);
        var selfLoop = () => definition.AddTransition(normal.Id, normal.Id, "Loop", WorkflowGuardType.None, null, null, false, 3, null, Now);

        duplicate.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "WORKFLOW_TRANSITION_DUPLICATE");
        selfLoop.Should().Throw<DomainException>()
            .Where(exception => exception.Code == "WORKFLOW_TRANSITION_SELF_LOOP");
    }

    private static WorkflowDefinition CreatePublishedDefinition()
    {
        var definition = new WorkflowDefinition(
            "WF-VERSION",
            "Versioned",
            WorkflowEntityType.Order,
            null,
            null,
            Now);
        var start = definition.AddState("START", "Start", WorkflowStateType.Start, 1, false, WorkflowAssigneeType.None, null, null, "#fff");
        var normal = definition.AddState("CHECK", "Check", WorkflowStateType.Normal, 2, false, WorkflowAssigneeType.None, null, null, "#fff");
        var end = definition.AddState("END", "End", WorkflowStateType.End, 3, true, WorkflowAssigneeType.None, null, null, "#fff");
        definition.AddTransition(start.Id, normal.Id, "Check", WorkflowGuardType.None, null, null, false, 1, null, Now);
        definition.AddTransition(normal.Id, end.Id, "Finish", WorkflowGuardType.None, null, null, false, 2, null, Now);
        definition.Publish(null, Now);
        return definition;
    }
}
