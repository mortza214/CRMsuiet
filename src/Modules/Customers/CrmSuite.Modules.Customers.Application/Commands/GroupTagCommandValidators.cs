using FluentValidation;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class CreateCustomerGroupCommandValidator : AbstractValidator<CreateCustomerGroupCommand>
{
    public CreateCustomerGroupCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام گروه الزامی است.")
            .MaximumLength(100).WithMessage("نام گروه حداکثر ۱۰۰ کاراکتر است.");
    }
}

public sealed class UpdateCustomerGroupCommandValidator : AbstractValidator<UpdateCustomerGroupCommand>
{
    public UpdateCustomerGroupCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه گروه الزامی است.");
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام گروه الزامی است.")
            .MaximumLength(100).WithMessage("نام گروه حداکثر ۱۰۰ کاراکتر است.");
    }
}

public sealed class DeleteCustomerGroupCommandValidator : AbstractValidator<DeleteCustomerGroupCommand>
{
    public DeleteCustomerGroupCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه گروه الزامی است.");
    }
}

public sealed class CreateCustomerTagCommandValidator : AbstractValidator<CreateCustomerTagCommand>
{
    public CreateCustomerTagCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام تگ الزامی است.")
            .MaximumLength(100).WithMessage("نام تگ حداکثر ۱۰۰ کاراکتر است.");

        RuleFor(command => command.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("رنگ تگ باید با فرمت #RRGGBB باشد.");
    }
}

public sealed class UpdateCustomerTagCommandValidator : AbstractValidator<UpdateCustomerTagCommand>
{
    public UpdateCustomerTagCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه تگ الزامی است.");
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام تگ الزامی است.")
            .MaximumLength(100).WithMessage("نام تگ حداکثر ۱۰۰ کاراکتر است.");
        RuleFor(command => command.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("رنگ تگ باید با فرمت #RRGGBB باشد.");
    }
}

public sealed class DeleteCustomerTagCommandValidator : AbstractValidator<DeleteCustomerTagCommand>
{
    public DeleteCustomerTagCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه تگ الزامی است.");
    }
}
