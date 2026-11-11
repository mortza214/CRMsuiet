using FluentValidation;

namespace CrmSuite.Modules.Forms.Application.Queries;

public sealed class GetFormTemplateByIdQueryValidator
    : AbstractValidator<GetFormTemplateByIdQuery>
{
    public GetFormTemplateByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetFormTemplateByCodeQueryValidator
    : AbstractValidator<GetFormTemplateByCodeQuery>
{
    public GetFormTemplateByCodeQueryValidator()
    {
        RuleFor(query => query.Code)
            .NotEmpty().WithMessage("کد فرم الزامی است.")
            .MaximumLength(64);
    }
}

public sealed class GetFormSubmissionsQueryValidator : AbstractValidator<GetFormSubmissionsQuery>
{
    public GetFormSubmissionsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
    }
}
