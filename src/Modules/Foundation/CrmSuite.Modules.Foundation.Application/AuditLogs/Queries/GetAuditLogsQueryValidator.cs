using FluentValidation;

namespace CrmSuite.Modules.Foundation.Application.AuditLogs.Queries;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل ۱ باشد.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200).WithMessage("اندازه صفحه باید بین ۱ تا ۲۰۰ باشد.");

        RuleFor(query => query.EntityName)
            .MaximumLength(200).WithMessage("نام موجودیت حداکثر ۲۰۰ کاراکتر است.");

        RuleFor(query => query.ToUtc)
            .GreaterThanOrEqualTo(query => query.FromUtc)
            .When(query => query.FromUtc.HasValue && query.ToUtc.HasValue)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد.");
    }
}
