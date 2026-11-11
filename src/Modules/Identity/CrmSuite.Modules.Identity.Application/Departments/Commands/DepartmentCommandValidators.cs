using FluentValidation;

namespace CrmSuite.Modules.Identity.Application.Departments.Commands;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام دپارتمان الزامی است.")
            .MaximumLength(200).WithMessage("نام دپارتمان حداکثر ۲۰۰ کاراکتر است.");
    }
}

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه دپارتمان الزامی است.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام دپارتمان الزامی است.")
            .MaximumLength(200).WithMessage("نام دپارتمان حداکثر ۲۰۰ کاراکتر است.");
    }
}

public sealed class DeleteDepartmentCommandValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه دپارتمان الزامی است.");
    }
}
