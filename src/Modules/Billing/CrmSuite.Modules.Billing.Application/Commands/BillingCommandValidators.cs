using FluentValidation;

namespace CrmSuite.Modules.Billing.Application.Commands;

public sealed class GenerateInvoiceFromOrderCommandValidator : AbstractValidator<GenerateInvoiceFromOrderCommand>
{
    public GenerateInvoiceFromOrderCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty().WithMessage("شناسه سفارش الزامی است.");
        RuleFor(command => command.Type).IsInEnum().WithMessage("نوع فاکتور معتبر نیست.");
    }
}

public sealed class GenerateContractCommandValidator : AbstractValidator<GenerateContractCommand>
{
    public GenerateContractCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty().WithMessage("شناسه مشتری الزامی است.");
        RuleFor(command => command.EndDateUtc)
            .GreaterThan(command => command.StartDateUtc)
            .WithMessage("تاریخ پایان قرارداد باید بعد از شروع باشد.");
    }
}

public sealed class UpdateInvoiceStatusCommandValidator : AbstractValidator<UpdateInvoiceStatusCommand>
{
    public UpdateInvoiceStatusCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty().WithMessage("شناسه فاکتور الزامی است.");
        RuleFor(command => command.Status).IsInEnum().WithMessage("وضعیت فاکتور معتبر نیست.");
    }
}
