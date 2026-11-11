using CrmSuite.Modules.Payments.Domain.Enums;
using FluentValidation;

namespace CrmSuite.Modules.Payments.Application.Commands;

public sealed class CreateManualPaymentCommandValidator : AbstractValidator<CreateManualPaymentCommand>
{
    public CreateManualPaymentCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty().WithMessage("شناسه مشتری الزامی است.");
        RuleFor(command => command.Amount).GreaterThan(0).WithMessage("مبلغ پرداخت باید بزرگ‌تر از صفر باشد.");
        RuleFor(command => command.PaymentMethod)
            .Must(method => method is PaymentMethod.ManualTransfer or PaymentMethod.Cash)
            .WithMessage("پرداخت دستی فقط با روش کارت به کارت یا نقدی ثبت می‌شود.");
    }
}

public sealed class ApproveManualPaymentCommandValidator : AbstractValidator<ApproveManualPaymentCommand>
{
    public ApproveManualPaymentCommandValidator()
    {
        RuleFor(command => command.TransactionId).NotEmpty().WithMessage("شناسه تراکنش الزامی است.");
    }
}

public sealed class InitiateOnlinePaymentCommandValidator : AbstractValidator<InitiateOnlinePaymentCommand>
{
    public InitiateOnlinePaymentCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty().WithMessage("شناسه فاکتور الزامی است.");
    }
}

public sealed class VerifyOnlinePaymentCommandValidator : AbstractValidator<VerifyOnlinePaymentCommand>
{
    public VerifyOnlinePaymentCommandValidator()
    {
        RuleFor(command => command.TrackingCode).NotEmpty().WithMessage("کد رهگیری الزامی است.");
    }
}
