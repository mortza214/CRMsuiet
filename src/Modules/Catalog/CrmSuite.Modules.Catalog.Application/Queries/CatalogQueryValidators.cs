using FluentValidation;

namespace CrmSuite.Modules.Catalog.Application.Queries;

public sealed class GetServicesQueryValidator : AbstractValidator<GetServicesQuery>
{
    public GetServicesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1).WithMessage("شماره صفحه باید حداقل ۱ باشد.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200).WithMessage("اندازه صفحه باید بین ۱ تا ۲۰۰ باشد.");
        RuleFor(query => query.Search).MaximumLength(200).WithMessage("عبارت جستجو حداکثر ۲۰۰ کاراکتر است.");
    }
}

public sealed class GetServiceByIdQueryValidator : AbstractValidator<GetServiceByIdQuery>
{
    public GetServiceByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithMessage("شناسه سرویس الزامی است.");
    }
}

public sealed class GetEffectivePriceQueryValidator : AbstractValidator<GetEffectivePriceQuery>
{
    public GetEffectivePriceQueryValidator()
    {
        RuleFor(query => query.ServiceId).NotEmpty().WithMessage("شناسه سرویس الزامی است.");
    }
}
