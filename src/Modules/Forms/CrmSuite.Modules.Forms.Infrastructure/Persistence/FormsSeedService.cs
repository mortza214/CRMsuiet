using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Forms.Application.Schema;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence;

/// <summary>
/// Seeds the operational forms used by the bandwidth workflow. Definitions remain editable
/// through the Forms builder; this service only creates them when they do not exist.
/// </summary>
public sealed class FormsSeedService
{
    public const string FeasibilityResultCode = "FeasibilityResult";
    public const string InstallationSpecsCode = "InstallationSpecs";
    public const string InstallationReportCode = "InstallationReport";
    public const string FeasibilityVisitDecisionCode = "FeasibilityVisitDecision";

    private readonly AppDbContext _dbContext;

    public FormsSeedService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = (await _dbContext.Set<FormTemplate>()
                .Select(template => template.Code)
                .ToListAsync(cancellationToken))
            .Select(code => code.ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var now = DateTime.UtcNow;
        if (!existing.Contains(FeasibilityResultCode.ToUpperInvariant()))
        {
            await AddAsync(
                FeasibilityResultCode,
                "نتیجه امکان‌سنجی",
                FormEntityType.Order,
                FeasibilitySchema,
                now,
                cancellationToken);
        }

        if (!existing.Contains(FeasibilityVisitDecisionCode.ToUpperInvariant()))
        {
            await AddAsync(
                FeasibilityVisitDecisionCode,
                "تصمیم مراجعه حضوری امکان‌سنجی",
                FormEntityType.Order,
                FeasibilityVisitDecisionSchema,
                now,
                cancellationToken);
        }

        if (!existing.Contains(InstallationSpecsCode.ToUpperInvariant()))
        {
            await AddAsync(
                InstallationSpecsCode,
                "مشخصات نصب",
                FormEntityType.Order,
                InstallationSpecsSchema,
                now,
                cancellationToken);
        }

        if (!existing.Contains(InstallationReportCode.ToUpperInvariant()))
        {
            await AddAsync(
                InstallationReportCode,
                "گزارش نصب",
                FormEntityType.Order,
                InstallationReportSchema,
                now,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddAsync(
        string code,
        string title,
        FormEntityType entityType,
        string schemaJson,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var template = new FormTemplate(
            code,
            title,
            entityType,
            null,
            null,
            schemaJson,
            1,
            FormDefinitionStatus.Published,
            null,
            now);

        var parsed = FormSchemaParser.Parse(schemaJson);
        if (parsed.IsSuccess)
        {
            foreach (var field in parsed.Value!.Fields)
            {
                template.Fields.Add(field.ToEntity(template.Id));
            }
        }

        _dbContext.Set<FormTemplate>().Add(template);
        await Task.CompletedTask;
    }

    private const string FeasibilityVisitDecisionSchema = """
        {
          "fields": [
            { "key": "needs_visit", "label": "نیاز به مراجعه حضوری", "type": "select", "required": true, "order": 1,
              "options": [{ "value": "yes", "label": "بله" }, { "value": "no", "label": "خیر" }] },
            { "key": "description", "label": "توضیحات", "type": "textarea", "required": false, "order": 2 }
          ]
        }
        """;

    private const string FeasibilitySchema = """
        {
          "fields": [
            { "key": "required_equipment", "label": "تجهیزات مورد نیاز", "type": "textarea", "required": true, "order": 1 },
            { "key": "is_feasible", "label": "امکان‌سنجی", "type": "select", "required": true, "order": 2,
              "options": [{ "value": "yes", "label": "بله" }, { "value": "no", "label": "خیر" }] },
            { "key": "needs_tower", "label": "نیاز به دکل", "type": "select", "required": true, "order": 3,
              "options": [{ "value": "yes", "label": "بله" }, { "value": "no", "label": "خیر" }] },
            { "key": "cable_meters", "label": "متراژ کابل", "type": "number", "required": true, "order": 4 },
            { "key": "description", "label": "توضیحات", "type": "textarea", "required": false, "order": 5 }
          ]
        }
        """;

    private const string InstallationSpecsSchema = """
        {
          "fields": [
            { "key": "pop_site", "label": "پاپ سایت", "type": "text", "required": true, "order": 1 },
            { "key": "antenna_mac", "label": "مک آنتن", "type": "text", "required": true, "order": 2 },
            { "key": "antenna_type", "label": "نوع آنتن", "type": "text", "required": true, "order": 3 },
            { "key": "antenna_count", "label": "تعداد آنتن", "type": "number", "required": true, "order": 4 },
            { "key": "ip_address", "label": "IP", "type": "text", "required": false, "order": 5 },
            { "key": "gateway", "label": "Gateway", "type": "text", "required": false, "order": 6 },
            { "key": "installation_address", "label": "آدرس محل نصب", "type": "textarea", "required": true, "order": 7 },
            { "key": "attachment", "label": "فایل / عکس", "type": "file", "required": false, "order": 8 }
          ]
        }
        """;

    private const string InstallationReportSchema = """
        {
          "fields": [
            { "key": "used_cable", "label": "کابل مصرفی", "type": "number", "required": true, "order": 1 },
            { "key": "used_pipe", "label": "لوله", "type": "number", "required": false, "order": 2 },
            { "key": "used_ip", "label": "IP مصرفی", "type": "number", "required": false, "order": 3 },
            { "key": "extra_services", "label": "خدمات اضافی", "type": "textarea", "required": false, "order": 4 },
            { "key": "summary", "label": "گزارش کوتاه", "type": "textarea", "required": true, "order": 5 },
            { "key": "attachment", "label": "فایل / عکس", "type": "file", "required": false, "order": 6 }
          ]
        }
        """;
}
