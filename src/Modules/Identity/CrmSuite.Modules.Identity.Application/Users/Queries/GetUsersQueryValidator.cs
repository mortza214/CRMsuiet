using FluentValidation;

namespace CrmSuite.Modules.Identity.Application.Users.Queries;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل ۱ باشد.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200).WithMessage("اندازه صفحه باید بین ۱ تا ۲۰۰ باشد.");

        RuleFor(query => query.Search)
            .MaximumLength(200).WithMessage("عبارت جستجو حداکثر ۲۰۰ کاراکتر است.");
    }
}

public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");
    }
}
