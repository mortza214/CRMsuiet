using CrmSuite.Modules.Catalog.Application.Abstractions;
using CrmSuite.Modules.Catalog.Application.Queries;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Workflow.Application.Abstractions;
using CrmSuite.Modules.Workflow.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Workflow.Api.Controllers;

[ApiController]
[Route("api/service-intake")]
[Authorize]
public sealed class ServiceIntakeController : ControllerBase
{
    [HttpGet("options")]
    public async Task<IActionResult> Options([FromServices] ICatalogReadRepository catalog,
        [FromServices] IServiceRepository services, [FromServices] IWorkflowDefinitionRepository workflows,
        [FromServices] IFormTemplateRepository forms, CancellationToken cancellationToken)
    {
        var page = await catalog.SearchServicesAsync(new ServiceSearchFilter(null, null, null, true, 1, 200), cancellationToken);
        var options = new List<object>();
        foreach (var summary in page.Items)
        {
            var service = await services.GetByIdAsync(summary.Id, cancellationToken);
            if (service?.WorkflowDefinitionId is not Guid workflowId) continue;
            var workflow = await workflows.GetByIdWithGraphAsync(workflowId, cancellationToken);
            if (workflow?.Status != CrmSuite.Modules.Workflow.Domain.Enums.WorkflowDefinitionStatus.Published) continue;
            var requestForm = workflow.Code == BandwidthIntake.Code
                ? await forms.GetByCodeAsync(BandwidthIntake.RequestForm, cancellationToken) : null;
            if (workflow.Code == BandwidthIntake.Code && requestForm?.Status != FormDefinitionStatus.Published) continue;
            options.Add(new { service.Id, service.Name, service.CategoryId, summary.CategoryName,
                serviceType = service.ServiceType.ToString(), workflowName = workflow.Name,
                schemaJson = requestForm?.SchemaJson ?? service.AttributeSchemaJson });
        }
        return Ok(new { success = true, data = options });
    }
}
