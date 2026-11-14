using System.Text.Json;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Sales;

public static class BandwidthIntake
{
    public const string Code = "WF-BANDWIDTH-INTAKE";
    public const string RequestForm = "BW_INTAKE_REQUEST";
    public const string DecisionForm = "BW_INTAKE_VISIT";
    public const string ResultForm = "BW_INTAKE_RESULT";

    public static WorkflowDefinition Build(Guid salesDepartment, Guid managerRole, DateTime now)
    {
        var definition = new WorkflowDefinition(Code, "درخواست پهنای باند اختصاصی — بررسی و امکان‌سنجی",
            WorkflowEntityType.Order, "ثبت درخواست، تأیید فروش، امکان‌سنجی و بازگشت نتیجه به فروش؛ ادامه فرایند در مرحله بعد تعریف می‌شود.", null, now);
        var sales = definition.AddState("SALES_REVIEW", "در انتظار بررسی و تأیید فروش", WorkflowStateType.Start, 1, false,
            WorkflowAssigneeType.Department, salesDepartment.ToString(), null, "#1677ff");
        var triage = definition.AddState("FEASIBILITY_TRIAGE", "تصمیم مدیر فنی درباره امکان‌سنجی", WorkflowStateType.Normal, 2, false,
            WorkflowAssigneeType.Role, managerRole.ToString(), DecisionForm, "#fa8c16", WorkflowActionKey.DecideSiteVisit);
        var report = definition.AddState("FEASIBILITY_REPORT", "تکمیل نتیجه امکان‌سنجی", WorkflowStateType.Normal, 3, false,
            WorkflowAssigneeType.Role, managerRole.ToString(), ResultForm, "#fa8c16", WorkflowActionKey.FillForm);
        var review = definition.AddState("FEASIBILITY_REVIEW", "در انتظار تأیید نتیجه توسط مدیر فنی", WorkflowStateType.Normal, 4, false,
            WorkflowAssigneeType.Role, managerRole.ToString(), ResultForm, "#722ed1", WorkflowActionKey.ReviewFeasibility);
        var result = definition.AddState("SALES_RESULT", "نتیجه نزد فروش — در انتظار ادامه فرایند", WorkflowStateType.Normal, 5, false,
            WorkflowAssigneeType.RequestSalesOwner, null, null, "#13c2c2");
        var rejected = definition.AddState("REJECTED", "درخواست توسط فروش رد شد", WorkflowStateType.End, 6, true,
            WorkflowAssigneeType.None, null, null, "#f5222d");
        void Link(WorkflowState from, WorkflowState to, string name, int order) => definition.AddTransition(
            from.Id, to.Id, name, WorkflowGuardType.None, null, null, false, order, null, now);
        Link(sales, triage, "تأیید فروش و ارسال به مدیر فنی", 1);
        Link(sales, rejected, "رد درخواست با توضیح", 3);
        Link(triage, report, "ثبت تصمیم و ارجاع برای تکمیل نتیجه", 4);
        Link(report, review, "ارسال نتیجه برای تأیید مدیر فنی", 5);
        Link(report, result, "ثبت نتیجه توسط مدیر فنی و ارسال به فروش", 6);
        Link(review, result, "تأیید / ویرایش نتیجه و ارسال به فروش", 7);
        Link(review, report, "بازگشت به کارشناس برای اصلاح", 8);
        definition.Publish(null, now);
        return definition;
    }

    public static bool IsManager(WorkflowInstance instance, WorkflowDefinition definition, WorkflowActor actor)
    {
        if (!actor.UserId.HasValue) return false;
        var triage = definition.States.FirstOrDefault(state => state.Code == "FEASIBILITY_TRIAGE");
        if (triage?.AssigneeType == WorkflowAssigneeType.Role && Guid.TryParse(triage.AssigneeValue, out var roleId)
            && actor.RoleIds.Contains(roleId)) return true;
        return instance.History.OrderBy(history => history.ActionAt).LastOrDefault(history =>
            history.FromStateId == triage?.Id)?.ActionBy == actor.UserId;
    }

    public static bool CanAct(WorkflowInstance instance, WorkflowActor actor) =>
        actor.Permissions.Contains("*", StringComparer.Ordinal) || instance.Tasks.Any(task =>
            task.StateId == instance.CurrentStateId && task.Status is WorkflowTaskStatus.Pending or WorkflowTaskStatus.InProgress
            && ((actor.UserId.HasValue && task.AssignedUserId == actor.UserId)
                || (task.AssignedRoleId.HasValue && actor.RoleIds.Contains(task.AssignedRoleId.Value))
                || (task.AssignedDepartmentId.HasValue && actor.DepartmentIds.Contains(task.AssignedDepartmentId.Value))));

    public static Guid? SalesOwner(WorkflowInstance instance, WorkflowDefinition definition) =>
        instance.History.OrderBy(history => history.ActionAt)
            .FirstOrDefault(history => history.FromStateId.HasValue
                && definition.FindState(history.FromStateId.Value).StateType == WorkflowStateType.Start)?.ActionBy
        ?? instance.StartedBy;

    public static Result ValidateTransition(WorkflowInstance instance, WorkflowDefinition definition,
        WorkflowTransition transition, string attributes, WorkflowActor actor, string? payload)
    {
        if (definition.Code != Code) return Result.Success();
        if (!CanAct(instance, actor)) return Result.Failure("این درخواست در کارتابل شما نیست.", "WORKFLOW_TASK_ACCESS_DENIED");
        var source = definition.FindState(transition.FromStateId).Code;
        var target = definition.FindState(transition.ToStateId).Code;
        if (target == "REJECTED" && (string.IsNullOrWhiteSpace(payload) || !HasComment(payload)))
            return Result.Failure("برای رد درخواست، توضیح را وارد کنید.", "WORKFLOW_REJECTION_COMMENT_REQUIRED");
        if (source == "FEASIBILITY_REPORT")
        {
            var selfReport = IsManager(instance, definition, actor);
            if ((target == "SALES_RESULT") != selfReport)
                return Result.Failure(selfReport ? "نتیجه مدیر فنی مستقیماً به فروش ارسال می‌شود." : "نتیجه کارشناس ابتدا باید به تأیید مدیر فنی برسد.", "WORKFLOW_REPORT_REVIEW_REQUIRED");
        }
        return Result.Success();
    }

    private static bool HasComment(string payload)
    {
        try
        {
            using var json = JsonDocument.Parse(payload);
            return json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("comment", out var value)
                && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());
        }
        catch (JsonException) { return false; }
    }
}
