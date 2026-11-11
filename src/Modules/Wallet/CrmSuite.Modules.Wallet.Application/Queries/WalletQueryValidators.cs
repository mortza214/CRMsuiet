using FluentValidation;

namespace CrmSuite.Modules.Wallet.Application.Queries;

public sealed class GetAllCommissionsQueryValidator : AbstractValidator<GetAllCommissionsQuery>
{
    public GetAllCommissionsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
    }
}
