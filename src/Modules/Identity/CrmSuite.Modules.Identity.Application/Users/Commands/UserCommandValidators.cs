using FluentValidation;

namespace CrmSuite.Modules.Identity.Application.Users.Commands;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است.")
            .MaximumLength(100).WithMessage("نام کاربری حداکثر ۱۰۰ کاراکتر است.")
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("نام کاربری فقط می‌تواند شامل حروف لاتین، رقم، نقطه، خط تیره و زیرخط باشد.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور حداقل ۸ کاراکتر است.")
            .MaximumLength(100).WithMessage("رمز عبور حداکثر ۱۰۰ کاراکتر است.");

        RuleFor(command => command.FullName)
            .NotEmpty().WithMessage("نام کامل الزامی است.")
            .MaximumLength(200).WithMessage("نام کامل حداکثر ۲۰۰ کاراکتر است.");

        RuleFor(command => command.Mobile)
            .NotEmpty().WithMessage("موبایل الزامی است.")
            .Matches("^09\\d{9}$").WithMessage("موبایل باید با 09 شروع شده و ۱۱ رقم باشد.");

        RuleFor(command => command.Email)
            .EmailAddress().WithMessage("ایمیل معتبر نیست.")
            .When(command => !string.IsNullOrWhiteSpace(command.Email));
    }
}

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(command => command.FullName)
            .NotEmpty().WithMessage("نام کامل الزامی است.")
            .MaximumLength(200).WithMessage("نام کامل حداکثر ۲۰۰ کاراکتر است.");

        RuleFor(command => command.Mobile)
            .NotEmpty().WithMessage("موبایل الزامی است.")
            .Matches("^09\\d{9}$").WithMessage("موبایل باید با 09 شروع شده و ۱۱ رقم باشد.");

        RuleFor(command => command.Email)
            .EmailAddress().WithMessage("ایمیل معتبر نیست.")
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.NewPassword)
            .MinimumLength(8).WithMessage("رمز عبور جدید حداقل ۸ کاراکتر است.")
            .MaximumLength(100).WithMessage("رمز عبور جدید حداکثر ۱۰۰ کاراکتر است.")
            .When(command => !string.IsNullOrWhiteSpace(command.NewPassword));
    }
}

public sealed class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
{
    public AssignRolesCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(command => command.RoleIds)
            .NotNull().WithMessage("فهرست نقش‌ها الزامی است.");
    }
}

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");
    }
}
