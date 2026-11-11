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
    public const string RequestFormCode = "RequestForm";
    public const string FeasibilityReportCode = "FeasibilityReport";
    public const string EquipmentSupplyFormCode = "EQUIPMENT_SUPPLY_FORM";
    public const string PurchaseEquipmentFormCode = "PurchaseEquipmentForm";
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
        if (!existing.Contains(RequestFormCode.ToUpperInvariant()))
        {
            await AddAsync(
                RequestFormCode,
                "فرم درخواست اولیه سرویس",
                FormEntityType.Order,
                RequestFormSchema,
                now,
                cancellationToken);
        }

        if (!existing.Contains(PurchaseEquipmentFormCode.ToUpperInvariant()))
        {
            await AddAsync(
                PurchaseEquipmentFormCode,
                "فرم خرید تجهیزات",
                FormEntityType.Order,
                PurchaseEquipmentSchema,
                now,
                cancellationToken);
        }

        if (!existing.Contains(EquipmentSupplyFormCode.ToUpperInvariant()))
        {
            await AddAsync(
                EquipmentSupplyFormCode,
                "تامین تجهیزات",
                FormEntityType.Order,
                EquipmentSupplySchema,
                now,
                cancellationToken);
        }

        if (!existing.Contains(FeasibilityReportCode.ToUpperInvariant()))
        {
            await AddAsync(
                FeasibilityReportCode,
                "گزارش امکان‌سنجی",
                FormEntityType.Order,
                FeasibilityReportSchema,
                now,
                cancellationToken);
        }

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

    private const string EquipmentSupplySchema = """
        {
          "fields": [
            { "key": "equipment_source", "label": "حالت تامین تجهیزات", "type": "select", "required": true, "order": 1,
              "options": [
                { "value": "purchase", "label": "خرید تجهیزات" },
                { "value": "consigned", "label": "تجهیزات امانی" },
                { "value": "available", "label": "تجهیزات موجود" }
              ] },
            { "key": "notes", "label": "توضیحات", "type": "textarea", "required": false, "order": 2 }
          ]
        }
        """;

    private const string PurchaseEquipmentSchema = """
        {
          "fields": [
            { "key": "equipment_name", "label": "نام تجهیز", "type": "text", "required": true, "order": 1 },
            { "key": "quantity", "label": "تعداد", "type": "number", "required": true, "order": 2 },
            { "key": "unit_price", "label": "قیمت واحد", "type": "number", "required": true, "order": 3 },
            { "key": "warehouse", "label": "انبار", "type": "text", "required": false, "order": 4 },
            { "key": "description", "label": "توضیحات", "type": "textarea", "required": false, "order": 5 }
          ]
        }
        """;

    private const string RequestFormSchema = """
        {
          "fields": [
            { "key": "cooperation_type", "label": "نوع همکاری", "type": "select", "required": true, "order": 1,
              "options": [{ "value": "contract", "label": "قرارداد" }, { "value": "invoice", "label": "فاکتوری" }] },
            { "key": "contract_end_date", "label": "تاریخ پایان قرارداد", "type": "text", "required": false, "order": 2,
              "visibleIf": { "field": "cooperation_type", "equals": "contract" } },
            { "key": "bandwidth_mb", "label": "پهنای باند (مگابیت)", "type": "number", "required": true, "order": 3 },
            { "key": "substrate", "label": "نوع بستر", "type": "select", "required": true, "order": 4,
              "options": [{ "value": "wireless", "label": "وایرلس" }, { "value": "fiber", "label": "فیبر" }, { "value": "cable", "label": "کابل" }] },
            { "key": "geo_coordinates", "label": "مختصات جغرافیایی", "type": "text", "required": true, "order": 5 },
            { "key": "billing_period_months", "label": "دوره صدور فاکتور (ماه)", "type": "select", "required": true, "order": 6,
              "options": [{ "value": "1", "label": "یک ماه" }, { "value": "3", "label": "سه ماه" }, { "value": "6", "label": "شش ماه" }, { "value": "12", "label": "یک سال" }] },
            { "key": "description", "label": "توضیحات", "type": "textarea", "required": false, "order": 7 }
          ]
        }
        """;

    private const string FeasibilityReportSchema = """
        {
          "fields": [
            { "key": "needs_visit", "label": "نیاز به مراجعه حضوری", "type": "select", "required": true, "order": 1,
              "options": [{ "value": "yes", "label": "بله" }, { "value": "no", "label": "خیر" }] },
            { "key": "available_facilities", "label": "امکانات موجود", "type": "textarea", "required": false, "order": 2 },
            { "key": "needs_tower", "label": "نیاز به دکل", "type": "select", "required": true, "order": 3,
              "options": [{ "value": "yes", "label": "بله" }, { "value": "no", "label": "خیر" }] },
            { "key": "is_feasible", "label": "امکان ارائه", "type": "select", "required": true, "order": 4,
              "options": [{ "value": "yes", "label": "دارد" }, { "value": "no", "label": "ندارد" }] },
            { "key": "description", "label": "توضیحات", "type": "textarea", "required": false, "order": 5 }
          ]
        }
        """;

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
