using FluentValidation;

namespace CrmSuite.Modules.Customers.Application.Queries;

public sealed class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل ۱ باشد.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200).WithMessage("اندازه صفحه باید بین ۱ تا ۲۰۰ باشد.");

        RuleFor(query => query.Search)
            .MaximumLength(200).WithMessage("عبارت جستجو حداکثر ۲۰۰ کاراکتر است.");
    }
}

public sealed class GetCustomerByIdQueryValidator : AbstractValidator<GetCustomerByIdQuery>
{
    public GetCustomerByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithMessage("شناسه مشتری الزامی است.");
    }
}
