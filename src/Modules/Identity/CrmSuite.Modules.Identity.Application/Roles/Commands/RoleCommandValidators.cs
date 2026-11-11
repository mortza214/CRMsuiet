using FluentValidation;

namespace CrmSuite.Modules.Identity.Application.Roles.Commands;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام نقش الزامی است.")
            .MaximumLength(100).WithMessage("نام نقش حداکثر ۱۰۰ کاراکتر است.");

        RuleFor(command => command.DisplayName)
            .NotEmpty().WithMessage("عنوان نقش الزامی است.")
            .MaximumLength(200).WithMessage("عنوان نقش حداکثر ۲۰۰ کاراکتر است.");
    }
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه نقش الزامی است.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام نقش الزامی است.")
            .MaximumLength(100).WithMessage("نام نقش حداکثر ۱۰۰ کاراکتر است.");

        RuleFor(command => command.DisplayName)
            .NotEmpty().WithMessage("عنوان نقش الزامی است.")
            .MaximumLength(200).WithMessage("عنوان نقش حداکثر ۲۰۰ کاراکتر است.");
    }
}

public sealed class AssignPermissionsCommandValidator : AbstractValidator<AssignPermissionsCommand>
{
    public AssignPermissionsCommandValidator()
    {
        RuleFor(command => command.RoleId)
            .NotEmpty().WithMessage("شناسه نقش الزامی است.");

        RuleFor(command => command.PermissionIds)
            .NotNull().WithMessage("فهرست Permissionها الزامی است.");
    }
}

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("شناسه نقش الزامی است.");
    }
}
