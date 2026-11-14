using System.Text.Json;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed class DecideSiteVisitAction : IWorkflowAction
{
    private readonly BandwidthPhase6Support _support;
    private readonly CrmSuite.Modules.Identity.Application.Abstractions.IUserRepository _users;

    public DecideSiteVisitAction(BandwidthPhase6Support support, CrmSuite.Modules.Identity.Application.Abstractions.IUserRepository users)
    {
        _support = support;
        _users = users;
    }

    public WorkflowActionKey Key => WorkflowActionKey.DecideSiteVisit;

    public async Task<Result> ExecuteAsync(
        WorkflowActionContext context,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(context.ActionDataJson)
                ? "{}"
                : context.ActionDataJson);
        var root = document.RootElement;
        var needsVisit = ReadString(root, "needs_visit")
                         ?? ReadString(root, "needsVisit");

        if (needsVisit is not ("yes" or "no"))
        {
            return Result.Failure(
                "انتخاب «نیاز به مراجعه حضوری» الزامی است.",
                "WORKFLOW_SITE_VISIT_DECISION_REQUIRED");
        }

        var expert = ReadString(root, "expert_user_id") ?? ReadString(root, "expertUserId");
        if (needsVisit == "yes" || !string.IsNullOrWhiteSpace(expert))
        {
            if (!Guid.TryParse(expert, out var expertId))
            {
                return Result.Failure(
                    "برای مراجعه حضوری، انتخاب کارشناس فنی الزامی است.",
                    "WORKFLOW_SITE_VISIT_EXPERT_REQUIRED");
            }
            var user = await _users.GetByIdWithAccessAsync(expertId, cancellationToken);
            if (user is null || !user.IsActive || !(user.UserDepartments.Any(item => item.Department.Name == "فنی")
                || user.UserRoles.Any(item => item.Role.Name is "TechnicalExpert" or "TechnicalManager")))
                return Result.Failure("کارشناس انتخاب‌شده باید کاربر فعال واحد فنی باشد.", "WORKFLOW_TECHNICAL_EXPERT_INVALID");
        }

        var form = await _support.SubmitFormAsync(
            context.State,
            context.Order,
            context.ActionDataJson,
            cancellationToken);
        return form.IsFailure
            ? Result.Failure(form.Errors, form.Message)
            : Result.Success();
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
