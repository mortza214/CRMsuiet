using FluentValidation;

namespace CrmSuite.Modules.Foundation.Application.Settings.Commands;

public sealed class UpdateSettingCommandValidator : AbstractValidator<UpdateSettingCommand>
{
    public UpdateSettingCommandValidator()
    {
        RuleFor(command => command.Key)
            .NotEmpty().WithMessage("کلید تنظیمات الزامی است.")
            .MaximumLength(200).WithMessage("کلید تنظیمات حداکثر ۲۰۰ کاراکتر است.")
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("کلید تنظیمات فقط می‌تواند شامل حروف لاتین، رقم، نقطه، خط تیره و زیرخط باشد.");

        RuleFor(command => command.Value)
            .NotNull().WithMessage("مقدار تنظیمات الزامی است.")
            .MaximumLength(32_000).WithMessage("مقدار تنظیمات بیش از حد طولانی است.");

        RuleFor(command => command.Group)
            .NotEmpty().WithMessage("گروه تنظیمات الزامی است.")
            .MaximumLength(100).WithMessage("گروه تنظیمات حداکثر ۱۰۰ کاراکتر است.");
    }
}
