using FluentValidation;

namespace CrmSuite.Modules.Reports.Application.Queries;

public sealed class GetFinancialSummaryQueryValidator : AbstractValidator<GetFinancialSummaryQuery>
{
    public GetFinancialSummaryQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThanOrEqualTo(query => query.FromUtc)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد.");
    }
}

public sealed class GetCommissionReportQueryValidator : AbstractValidator<GetCommissionReportQuery>
{
    public GetCommissionReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThanOrEqualTo(query => query.FromUtc)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد.");
    }
}

public sealed class GetSalesReportQueryValidator : AbstractValidator<GetSalesReportQuery>
{
    public GetSalesReportQueryValidator()
    {
        RuleFor(query => query.ToUtc)
            .GreaterThanOrEqualTo(query => query.FromUtc)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد.");
    }
}

public sealed class GetCustomerLedgerQueryValidator : AbstractValidator<GetCustomerLedgerQuery>
{
    public GetCustomerLedgerQueryValidator()
    {
        RuleFor(query => query.CustomerId).NotEmpty();
    }
}
