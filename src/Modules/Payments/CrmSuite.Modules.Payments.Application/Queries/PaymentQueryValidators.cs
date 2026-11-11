using FluentValidation;

namespace CrmSuite.Modules.Payments.Application.Queries;

public sealed class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    public GetTransactionsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل ۱ باشد.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200).WithMessage("اندازه صفحه باید بین ۱ تا ۲۰۰ باشد.");
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
        RuleFor(query => query.ToUtc)
            .GreaterThanOrEqualTo(query => query.FromUtc)
            .When(query => query.FromUtc.HasValue && query.ToUtc.HasValue)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد.");
    }
}
