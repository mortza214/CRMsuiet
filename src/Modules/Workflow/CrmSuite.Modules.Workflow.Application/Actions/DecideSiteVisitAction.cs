using System.Text.Json;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Workflow.Application.Sales;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Actions;

public sealed class DecideSiteVisitAction : IWorkflowAction
{
    private readonly BandwidthPhase6Support _support;

    public DecideSiteVisitAction(BandwidthPhase6Support support)
    {
        _support = support;
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

        if (needsVisit == "yes")
        {
            var expert = ReadString(root, "expert_user_id")
                         ?? ReadString(root, "expertUserId");
            if (!Guid.TryParse(expert, out _))
            {
                return Result.Failure(
                    "برای مراجعه حضوری، انتخاب کارشناس فنی الزامی است.",
                    "WORKFLOW_SITE_VISIT_EXPERT_REQUIRED");
            }
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
