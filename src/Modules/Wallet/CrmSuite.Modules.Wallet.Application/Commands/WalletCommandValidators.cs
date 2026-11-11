using CrmSuite.Modules.Payments.Domain.Enums;
using CrmSuite.Modules.Wallet.Domain.Enums;
using FluentValidation;

namespace CrmSuite.Modules.Wallet.Application.Commands;

public sealed class ChargeCustomerWalletCommandValidator : AbstractValidator<ChargeCustomerWalletCommand>
{
    public ChargeCustomerWalletCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0).WithMessage("مبلغ شارژ باید بزرگ‌تر از صفر باشد.");
        RuleFor(command => command.PaymentMethod)
            .Must(method => method is PaymentMethod.ManualTransfer or PaymentMethod.Cash)
            .WithMessage("شارژ کیف پول فقط با پرداخت دستی یا نقدی ثبت می‌شود.");
    }
}

public sealed class PayInvoiceFromWalletCommandValidator : AbstractValidator<PayInvoiceFromWalletCommand>
{
    public PayInvoiceFromWalletCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
    }
}

public sealed class RequestCommissionWithdrawCommandValidator : AbstractValidator<RequestCommissionWithdrawCommand>
{
    public RequestCommissionWithdrawCommandValidator()
    {
        RuleFor(command => command.Amount).GreaterThan(0).WithMessage("مبلغ برداشت باید بزرگ‌تر از صفر باشد.");
    }
}

public sealed class ApproveCommissionWithdrawCommandValidator : AbstractValidator<ApproveCommissionWithdrawCommand>
{
    public ApproveCommissionWithdrawCommandValidator()
    {
        RuleFor(command => command.WithdrawalId).NotEmpty();
    }
}

public sealed class EvaluateOrderCommissionCommandValidator : AbstractValidator<EvaluateOrderCommissionCommand>
{
    public EvaluateOrderCommissionCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
    }
}

public sealed class ApproveCommissionCommandValidator : AbstractValidator<ApproveCommissionCommand>
{
    public ApproveCommissionCommandValidator()
    {
        RuleFor(command => command.CommissionId).NotEmpty();
    }
}

public sealed class CreateCommissionRuleCommandValidator : AbstractValidator<CreateCommissionRuleCommand>
{
    public CreateCommissionRuleCommandValidator()
    {
        RuleFor(command => command.Percentage)
            .InclusiveBetween(0, 100).WithMessage("درصد پورسانت باید بین ۰ تا ۱۰۰ باشد.");

        RuleFor(command => command.FixedAmount)
            .GreaterThan(0).When(command => command.FixedAmount.HasValue)
            .WithMessage("مبلغ ثابت باید بزرگ‌تر از صفر باشد.");

        RuleFor(command => command)
            .Must(command => command.Percentage > 0 || command.FixedAmount.HasValue)
            .WithMessage("درصد یا مبلغ ثابت پورسانت الزامی است.");

        RuleFor(command => command.PayoutType).IsInEnum();
    }
}
