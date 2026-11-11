using CrmSuite.BuildingBlocks.Infrastructure.Persistence;
using CrmSuite.Modules.Identity.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Entities;
using CrmSuite.Modules.Workflow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CrmSuite.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// Seeds the two bandwidth sales workflows. They remain editable database definitions;
/// application code only references their stable codes to choose the right path.
/// </summary>
public sealed class WorkflowSeedService
{
    public const string BandwidthCode = "WF-BANDWIDTH";
    public const string WithFeasibilityCode = "WF-BANDWIDTH-FEASIBILITY";
    public const string NoFeasibilityCode = "WF-BANDWIDTH-NO-FEASIBILITY";

    private readonly AppDbContext _dbContext;

    public WorkflowSeedService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = await _dbContext.Set<WorkflowDefinition>()
            .Where(definition => definition.Code == BandwidthCode
                                 || definition.Code == WithFeasibilityCode
                                 || definition.Code == NoFeasibilityCode)
            .Select(definition => definition.Code)
            .ToListAsync(cancellationToken);

        var technical = await EnsureDepartmentAsync("فنی", cancellationToken);
        var purchase = await EnsureDepartmentAsync("خرید", cancellationToken);
        var technicalManager = await EnsureRoleAsync("TechnicalManager", "مدیر فنی", cancellationToken);
        var technicalExpert = await EnsureRoleAsync("TechnicalExpert", "کارشناس فنی", cancellationToken);
        var financeExpert = await EnsureRoleAsync("FinanceExpert", "کارشناس مالی", cancellationToken);
        var now = DateTime.UtcNow;

        if (!existingCodes.Contains(BandwidthCode))
        {
            var definition = BuildBandwidth(technical.Id, purchase.Id, technicalManager.Id, technicalExpert.Id, financeExpert.Id, now);
            definition.Publish(null, now);
            _dbContext.Set<WorkflowDefinition>().Add(definition);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> EnsureRoleAsync(
        string name,
        string displayName,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Set<Role>()
            .FirstOrDefaultAsync(item => item.Name == name, cancellationToken);
        if (role is not null)
        {
            return role;
        }

        role = new Role(name, displayName);
        role.MarkCreated(null, DateTime.UtcNow);
        _dbContext.Set<Role>().Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    private async Task<Department> EnsureDepartmentAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var department = await _dbContext.Set<Department>()
            .FirstOrDefaultAsync(item => item.Name == name, cancellationToken);
        if (department is not null)
        {
            return department;
        }

        department = new Department(name);
        department.MarkCreated(null, DateTime.UtcNow);
        _dbContext.Set<Department>().Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return department;
    }

    private static WorkflowDefinition BuildBandwidth(
        Guid technicalDepartmentId,
        Guid purchaseDepartmentId,
        Guid technicalManagerRoleId,
        Guid technicalExpertRoleId,
        Guid financeExpertRoleId,
        DateTime now)
    {
        var definition = new WorkflowDefinition(
            BandwidthCode,
            "فرایند فروش پهنای باند",
            WorkflowEntityType.Order,
            "فرایند کامل فروش، امکان‌سنجی، تجهیزات، پرداخت، نصب، قرارداد و فعال‌سازی.",
            null,
            now);

        var initial = definition.AddState("INITIAL_REQUEST", "ثبت درخواست", WorkflowStateType.Start, 1, false, WorkflowAssigneeType.CustomerOwner, null, "RequestForm", "#1677ff", WorkflowActionKey.FillForm);
        var salesReview = definition.AddState("SALES_REVIEW", "بررسی فروش", WorkflowStateType.Normal, 2, false, WorkflowAssigneeType.CustomerOwner, null, null, "#1677ff", WorkflowActionKey.None);
        var feasibilityPending = definition.AddState("FEASIBILITY_PENDING", "امکان‌سنجی", WorkflowStateType.Normal, 3, false, WorkflowAssigneeType.Role, technicalManagerRoleId.ToString(), "FeasibilityVisitDecision", "#fa8c16", WorkflowActionKey.FillForm);
        var siteVisitPending = definition.AddState("SITE_VISIT_PENDING", "مراجعه حضوری", WorkflowStateType.Normal, 4, false, WorkflowAssigneeType.Role, technicalExpertRoleId.ToString(), "FeasibilityResult", "#fa8c16", WorkflowActionKey.FillForm);
        var feasibilityReview = definition.AddState("FEASIBILITY_REVIEW", "تایید فنی", WorkflowStateType.Normal, 5, false, WorkflowAssigneeType.Role, technicalManagerRoleId.ToString(), "FeasibilityResult", "#722ed1", WorkflowActionKey.ReviewFeasibility);
        var equipmentSupply = definition.AddState("EQUIPMENT_SUPPLY", "تامین تجهیزات", WorkflowStateType.Normal, 6, false, WorkflowAssigneeType.CustomerOwner, null, "EQUIPMENT_SUPPLY_FORM", "#13c2c2", WorkflowActionKey.FillForm);
        var purchasePending = definition.AddState("PURCHASE_PENDING", "خرید تجهیزات", WorkflowStateType.Normal, 7, false, WorkflowAssigneeType.Department, purchaseDepartmentId.ToString(), "PurchaseEquipmentForm", "#eb2f96", WorkflowActionKey.FillForm);
        var proforma = definition.AddState("PROFORMA", "صدور پیش‌فاکتور", WorkflowStateType.Normal, 8, false, WorkflowAssigneeType.CustomerOwner, null, null, "#2f54eb", WorkflowActionKey.IssueProforma);
        var customerApproval = definition.AddState("CUSTOMER_APPROVAL", "تایید مشتری", WorkflowStateType.Normal, 9, false, WorkflowAssigneeType.Customer, null, null, "#faad14", WorkflowActionKey.RequestPayment);
        var installationPending = definition.AddState("INSTALLATION_PENDING", "مشخصات نصب", WorkflowStateType.Normal, 10, false, WorkflowAssigneeType.Department, technicalDepartmentId.ToString(), "InstallationSpecs", "#faad14", WorkflowActionKey.FillForm);
        var installationInProgress = definition.AddState("INSTALLATION_IN_PROGRESS", "گزارش نصب", WorkflowStateType.Normal, 11, false, WorkflowAssigneeType.Department, technicalDepartmentId.ToString(), "InstallationReport", "#fa8c16", WorkflowActionKey.FillForm);
        var installationReview = definition.AddState("INSTALLATION_REVIEW", "تایید نصب", WorkflowStateType.Normal, 12, false, WorkflowAssigneeType.Department, technicalDepartmentId.ToString(), "InstallationReport", "#722ed1", WorkflowActionKey.ReviewInstallation);
        var finalInvoice = definition.AddState("FINAL_INVOICE", "فاکتور نهایی", WorkflowStateType.Normal, 13, false, WorkflowAssigneeType.CustomerOwner, null, null, "#722ed1", WorkflowActionKey.IssueFinalInvoice);
        var finalPayment = definition.AddState("FINAL_PAYMENT", "پرداخت نهایی", WorkflowStateType.Normal, 14, false, WorkflowAssigneeType.CustomerOwner, null, null, "#f5222d", WorkflowActionKey.RequestPayment);
        var financeReview = definition.AddState("FINANCE_REVIEW", "تایید مالی", WorkflowStateType.Normal, 15, false, WorkflowAssigneeType.Role, financeExpertRoleId.ToString(), null, "#fa8c16", WorkflowActionKey.VerifyPayment);
        var contract = definition.AddState("CONTRACT", "قرارداد", WorkflowStateType.Normal, 16, false, WorkflowAssigneeType.CustomerOwner, null, null, "#2f54eb", WorkflowActionKey.SignContract);
        var completed = definition.AddState("COMPLETED", "پایان (فعال‌سازی)", WorkflowStateType.End, 17, true, WorkflowAssigneeType.None, null, null, "#52c41a", WorkflowActionKey.ActivateService);
        var cancelled = definition.AddState("CANCELLED", "لغو شده", WorkflowStateType.End, 18, true, WorkflowAssigneeType.None, null, null, "#f5222d", WorkflowActionKey.None);

        AddTransition(definition, initial, salesReview, "ارسال به بررسی", 1);
        AddTransition(definition, salesReview, feasibilityPending, "نیاز به امکان‌سنجی", 2);
        AddTransition(definition, salesReview, equipmentSupply, "بدون امکان‌سنجی", 3);
        AddTransition(definition, feasibilityPending, siteVisitPending, "بله - ارجاع به کارشناس فنی", 4);
        AddTransition(definition, feasibilityPending, equipmentSupply, "خیر - ادامه بدون مراجعه", 5);
        AddTransition(definition, siteVisitPending, feasibilityReview, "ارسال نتیجه به مدیر فنی", 6);
        AddTransition(definition, feasibilityReview, equipmentSupply, "تایید فنی", 7);
        AddTransition(definition, equipmentSupply, purchasePending, "نیاز به خرید", 8);
        AddTransition(definition, equipmentSupply, proforma, "تجهیزات موجود/امانی", 9);
        AddTransition(definition, purchasePending, proforma, "تجهیزات آماده شد", 10);
        AddTransition(definition, proforma, customerApproval, "ارسال به مشتری", 11);
        AddTransition(definition, customerApproval, installationPending, "تایید مشتری", 12);
        AddTransition(definition, customerApproval, cancelled, "انصراف مشتری", 18);
        AddTransition(definition, customerApproval, financeReview, "ارسال به بررسی مالی", 22);
        AddTransition(definition, installationPending, installationInProgress, "ارسال به نیروی نصب", 13);
        AddTransition(definition, installationInProgress, installationReview, "ارسال به مدیر فنی", 14);
        AddTransition(definition, installationReview, finalInvoice, "تایید نصب", 15);
        AddTransition(definition, finalInvoice, finalPayment, "ارسال فاکتور", 16);
        AddTransition(definition, finalPayment, financeReview, "تایید پرداخت", 17);
        AddTransition(definition, financeReview, contract, "نیاز به قرارداد", 18);
        AddTransition(definition, financeReview, completed, "بدون قرارداد", 19);
        AddTransition(definition, financeReview, cancelled, "رد پرداخت", 21);
        AddTransition(definition, contract, completed, "امضای قرارداد", 20);

        return definition;
    }

    private static WorkflowDefinition BuildWithFeasibility(
        Guid technicalDepartmentId,
        Guid purchaseDepartmentId,
        DateTime now)
    {
        var definition = new WorkflowDefinition(
            WithFeasibilityCode,
            "فروش پهنای باند اختصاصی با امکان‌سنجی",
            WorkflowEntityType.Order,
            "ثبت درخواست، امکان‌سنجی فنی، تصمیم خرید/پیش‌فاکتور، پرداخت و نصب.",
            null,
            now);

        var initial = definition.AddState(
            "INITIAL_REQUEST", "شروع درخواست", WorkflowStateType.Start, 1, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#1677ff");
        var feasibilityTriage = definition.AddState(
            "FEASIBILITY_TRIAGE", "تصمیم مراجعه حضوری", WorkflowStateType.Normal, 2, false,
            WorkflowAssigneeType.Department, technicalDepartmentId.ToString(),
            "FeasibilityVisitDecision", "#faad14", WorkflowActionKey.DecideSiteVisit);
        var feasibilityPending = definition.AddState(
            "FEASIBILITY_PENDING", "در انتظار امکان‌سنجی", WorkflowStateType.Normal, 3, false,
            WorkflowAssigneeType.Department, technicalDepartmentId.ToString(),
            "FeasibilityResult", "#fa8c16", WorkflowActionKey.FillForm);
        var feasibilityReview = definition.AddState(
            "FEASIBILITY_REVIEW", "بررسی امکان‌سنجی", WorkflowStateType.Normal, 4, false,
            WorkflowAssigneeType.Department, technicalDepartmentId.ToString(),
            "FeasibilityResult", "#722ed1", WorkflowActionKey.ReviewFeasibility);
        var equipmentSupply = definition.AddState(
            "EQUIPMENT_SUPPLY", "تأمین تجهیزات", WorkflowStateType.Normal, 5, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#13c2c2");
        var purchasePending = definition.AddState(
            "PURCHASE_PENDING", "در انتظار خرید", WorkflowStateType.Normal, 6, false,
            WorkflowAssigneeType.Department, purchaseDepartmentId.ToString(), null, "#eb2f96");
        var proforma = definition.AddState(
            "PROFORMA", "صدور پیش‌فاکتور", WorkflowStateType.Normal, 7, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#2f54eb", WorkflowActionKey.IssueProforma);
        var customerApproval = definition.AddState(
            "CUSTOMER_APPROVAL", "تأیید مشتری", WorkflowStateType.Normal, 8, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#faad14", WorkflowActionKey.RequestPayment);
        var installationPending = definition.AddState(
            "INSTALLATION_PENDING", "در انتظار مشخصات نصب", WorkflowStateType.Normal, 9, false,
            WorkflowAssigneeType.Department, technicalDepartmentId.ToString(),
            "InstallationSpecs", "#faad14", WorkflowActionKey.FillForm);
        var installationInProgress = definition.AddState(
            "INSTALLATION_IN_PROGRESS", "در حال نصب", WorkflowStateType.Normal, 10, false,
            WorkflowAssigneeType.Department, technicalDepartmentId.ToString(),
            "InstallationReport", "#fa8c16", WorkflowActionKey.FillForm);
        var finalInvoice = definition.AddState(
            "FINAL_INVOICE", "فاکتور نهایی", WorkflowStateType.Normal, 11, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#722ed1", WorkflowActionKey.IssueFinalInvoice);
        var paymentPending = definition.AddState(
            "PAYMENT_PENDING", "در انتظار پرداخت", WorkflowStateType.Normal, 12, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#f5222d", WorkflowActionKey.RequestPayment);
        var contract = definition.AddState(
            "CONTRACT", "قرارداد و امضا", WorkflowStateType.Normal, 13, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#2f54eb");
        var completed = definition.AddState(
            "COMPLETED", "تکمیل و فعال‌سازی", WorkflowStateType.End, 14, true,
            WorkflowAssigneeType.None, null, null, "#52c41a", WorkflowActionKey.ActivateService);

        AddTransition(definition, initial, feasibilityTriage, "ارجاع به مدیر فنی", 1);
        AddTransition(definition, feasibilityTriage, feasibilityPending, "ثبت تصمیم مراجعه حضوری", 2);
        AddTransition(definition, feasibilityPending, feasibilityReview, "ثبت گزارش و ارسال برای بررسی", 2);
        AddTransition(definition, feasibilityReview, feasibilityPending, "بازگشت برای اصلاح", 3);
        AddTransition(definition, feasibilityReview, equipmentSupply, "تأیید امکان‌سنجی", 4);
        AddTransition(definition, equipmentSupply, purchasePending, "خرید تجهیزات", 5);
        AddTransition(definition, equipmentSupply, proforma, "تجهیزات موجود / امانی", 6);
        AddTransition(definition, purchasePending, proforma, "پس از خرید", 7);
        AddTransition(definition, proforma, customerApproval, "ارسال برای تأیید مشتری", 8);
        AddTransition(definition, customerApproval, installationPending, "تأیید و نیاز به نصب", 9);
        AddTransition(definition, customerApproval, paymentPending, "تأیید و پرداخت", 10);
        AddTransition(definition, installationPending, installationInProgress, "شروع نصب", 11);
        AddTransition(definition, installationInProgress, finalInvoice, "پایان نصب", 12);
        AddTransition(definition, finalInvoice, contract, "قرارداد", 13);
        AddTransition(definition, finalInvoice, paymentPending, "ارسال فاکتور نهایی", 14);
        AddTransition(definition, paymentPending, contract, "پرداخت‌شده - قرارداد", 15);
        AddTransition(definition, paymentPending, completed, "پرداخت‌شده - فعال‌سازی", 16);
        AddTransition(definition, contract, completed, "قرارداد امضا شد", 17);

        return definition;
    }

    private static WorkflowDefinition BuildWithoutFeasibility(
        Guid technicalDepartmentId,
        DateTime now)
    {
        var definition = new WorkflowDefinition(
            NoFeasibilityCode,
            "فروش پهنای باند اختصاصی بدون امکان‌سنجی",
            WorkflowEntityType.Order,
            "ثبت درخواست، تأمین تجهیزات، پیش‌فاکتور، نصب، پرداخت و فعال‌سازی.",
            null,
            now);

        var initial = definition.AddState(
            "INITIAL_REQUEST", "شروع درخواست", WorkflowStateType.Start, 1, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#1677ff");
        var equipmentSupply = definition.AddState(
            "EQUIPMENT_SUPPLY", "تأمین تجهیزات", WorkflowStateType.Normal, 2, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#13c2c2");
        var proforma = definition.AddState(
            "PROFORMA", "صدور پیش‌فاکتور", WorkflowStateType.Normal, 3, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#2f54eb", WorkflowActionKey.IssueProforma);
        var customerApproval = definition.AddState(
            "CUSTOMER_APPROVAL", "تأیید مشتری", WorkflowStateType.Normal, 4, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#faad14", WorkflowActionKey.RequestPayment);
        var installationPending = definition.AddState(
            "INSTALLATION_PENDING", "در انتظار مشخصات نصب", WorkflowStateType.Normal, 5, false,
            WorkflowAssigneeType.Department, technicalDepartmentId.ToString(),
            "InstallationSpecs", "#faad14", WorkflowActionKey.FillForm);
        var installationInProgress = definition.AddState(
            "INSTALLATION_IN_PROGRESS", "در حال نصب", WorkflowStateType.Normal, 6, false,
            WorkflowAssigneeType.Department, technicalDepartmentId.ToString(),
            "InstallationReport", "#fa8c16", WorkflowActionKey.FillForm);
        var finalInvoice = definition.AddState(
            "FINAL_INVOICE", "فاکتور نهایی", WorkflowStateType.Normal, 7, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#722ed1", WorkflowActionKey.IssueFinalInvoice);
        var paymentPending = definition.AddState(
            "PAYMENT_PENDING", "در انتظار پرداخت", WorkflowStateType.Normal, 8, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#f5222d", WorkflowActionKey.RequestPayment);
        var contract = definition.AddState(
            "CONTRACT", "قرارداد و امضا", WorkflowStateType.Normal, 9, false,
            WorkflowAssigneeType.CustomerOwner, null, null, "#2f54eb");
        var completed = definition.AddState(
            "COMPLETED", "تکمیل و فعال‌سازی", WorkflowStateType.End, 10, true,
            WorkflowAssigneeType.None, null, null, "#52c41a", WorkflowActionKey.ActivateService);

        AddTransition(definition, initial, equipmentSupply, "آغاز تأمین تجهیزات", 1);
        AddTransition(definition, equipmentSupply, proforma, "صدور پیش‌فاکتور", 2);
        AddTransition(definition, proforma, customerApproval, "ارسال برای تأیید مشتری", 3);
        AddTransition(definition, customerApproval, installationPending, "تأیید و نیاز به نصب", 4);
        AddTransition(definition, customerApproval, paymentPending, "تأیید و پرداخت", 5);
        AddTransition(definition, installationPending, installationInProgress, "شروع نصب", 6);
        AddTransition(definition, installationInProgress, finalInvoice, "پایان نصب", 7);
        AddTransition(definition, finalInvoice, contract, "قرارداد", 8);
        AddTransition(definition, finalInvoice, paymentPending, "ارسال فاکتور نهایی", 9);
        AddTransition(definition, paymentPending, contract, "پرداخت‌شده - قرارداد", 10);
        AddTransition(definition, paymentPending, completed, "پرداخت‌شده - فعال‌سازی", 11);
        AddTransition(definition, contract, completed, "قرارداد امضا شد", 12);

        return definition;
    }

    private static void AddTransition(
        WorkflowDefinition definition,
        WorkflowState from,
        WorkflowState to,
        string name,
        int sortOrder)
    {
        definition.AddTransition(
            from.Id,
            to.Id,
            name,
            WorkflowGuardType.None,
            null,
            null,
            false,
            sortOrder,
            null,
            DateTime.UtcNow);
    }
}
