using FluentValidation;

namespace CrmSuite.Modules.Identity.Application.Auth.Commands;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است.")
            .MaximumLength(100).WithMessage("نام کاربری حداکثر ۱۰۰ کاراکتر است.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MaximumLength(200).WithMessage("رمز عبور بیش از حد طولانی است.");
    }
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("Refresh Token الزامی است.");
    }
}

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty().WithMessage("Refresh Token الزامی است.");
    }
}
