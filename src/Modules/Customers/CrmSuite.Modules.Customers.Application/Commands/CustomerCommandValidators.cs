using CrmSuite.Modules.Customers.Domain.Enums;
using FluentValidation;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.Type)
            .IsInEnum().WithMessage("نوع مشتری معتبر نیست.");

        RuleFor(command => command.Mobile)
            .NotEmpty().WithMessage("موبایل الزامی است.")
            .Matches("^09\\d{9}$").WithMessage("موبایل باید با 09 شروع شده و ۱۱ رقم باشد.");

        RuleFor(command => command.NationalId)
            .NotEmpty().WithMessage("کد ملی/شناسه ملی الزامی است.")
            .Matches("^\\d{10,11}$").WithMessage("کد ملی باید ۱۰ یا ۱۱ رقم باشد.");

        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage("نام برای مشتری حقیقی الزامی است.")
            .When(command => command.Type == CustomerType.Natural);

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage("نام خانوادگی برای مشتری حقیقی الزامی است.")
            .When(command => command.Type == CustomerType.Natural);

        RuleFor(command => command.CompanyName)
            .NotEmpty().WithMessage("نام شرکت برای مشتری حقوقی الزامی است.")
            .When(command => command.Type == CustomerType.Legal);

        RuleFor(command => command.Email)
            .EmailAddress().WithMessage("ایمیل معتبر نیست.")
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.Address)
            .MaximumLength(500).WithMessage("آدرس حداکثر ۵۰۰ کاراکتر است.");
    }
}

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه مشتری الزامی است.");

        RuleFor(command => command.Type)
            .IsInEnum().WithMessage("نوع مشتری معتبر نیست.");

        RuleFor(command => command.Mobile)
            .NotEmpty().WithMessage("موبایل الزامی است.")
            .Matches("^09\\d{9}$").WithMessage("موبایل باید با 09 شروع شده و ۱۱ رقم باشد.");

        RuleFor(command => command.NationalId)
            .NotEmpty().WithMessage("کد ملی/شناسه ملی الزامی است.")
            .Matches("^\\d{10,11}$").WithMessage("کد ملی باید ۱۰ یا ۱۱ رقم باشد.");

        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage("نام برای مشتری حقیقی الزامی است.")
            .When(command => command.Type == CustomerType.Natural);

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage("نام خانوادگی برای مشتری حقیقی الزامی است.")
            .When(command => command.Type == CustomerType.Natural);

        RuleFor(command => command.CompanyName)
            .NotEmpty().WithMessage("نام شرکت برای مشتری حقوقی الزامی است.")
            .When(command => command.Type == CustomerType.Legal);

        RuleFor(command => command.Email)
            .EmailAddress().WithMessage("ایمیل معتبر نیست.")
            .When(command => !string.IsNullOrWhiteSpace(command.Email));
    }
}

public sealed class SetResellerCommandValidator : AbstractValidator<SetResellerCommand>
{
    public SetResellerCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty().WithMessage("شناسه مشتری الزامی است.");
    }
}

public sealed class DeleteCustomerCommandValidator : AbstractValidator<DeleteCustomerCommand>
{
    public DeleteCustomerCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه مشتری الزامی است.");
    }
}

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(command => command.Mobile)
            .NotEmpty().WithMessage("موبایل الزامی است.")
            .Matches("^09\\d{9}$").WithMessage("موبایل باید با 09 شروع شده و ۱۱ رقم باشد.");
    }
}

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(command => command.Mobile)
            .NotEmpty().WithMessage("موبایل الزامی است.")
            .Matches("^09\\d{9}$").WithMessage("موبایل باید با 09 شروع شده و ۱۱ رقم باشد.");

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("کد OTP الزامی است.")
            .Matches("^\\d{6}$").WithMessage("کد OTP باید ۶ رقمی باشد.");
    }
}
