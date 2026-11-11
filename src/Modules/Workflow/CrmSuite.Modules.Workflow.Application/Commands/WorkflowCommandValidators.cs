using FluentValidation;
using CrmSuite.Modules.Workflow.Domain.Enums;

namespace CrmSuite.Modules.Workflow.Application.Commands;

public sealed class CreateWorkflowDefinitionCommandValidator
    : AbstractValidator<CreateWorkflowDefinitionCommand>
{
    public CreateWorkflowDefinitionCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("کد گردش کار الزامی است.")
            .MaximumLength(80).WithMessage("کد گردش کار حداکثر ۸۰ کاراکتر است.");
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام گردش کار الزامی است.")
            .MaximumLength(200).WithMessage("نام گردش کار حداکثر ۲۰۰ کاراکتر است.");
        RuleFor(command => command.EntityType)
            .IsInEnum().WithMessage("نوع موجودیت گردش کار معتبر نیست.");
        RuleFor(command => command.Description)
            .MaximumLength(1000).WithMessage("توضیحات حداکثر ۱۰۰۰ کاراکتر است.");
    }
}

public sealed class UpdateWorkflowDraftCommandValidator
    : AbstractValidator<UpdateWorkflowDraftCommand>
{
    public UpdateWorkflowDraftCommandValidator()
    {
        RuleFor(command => command.DefinitionId).NotEmpty();
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام گردش کار الزامی است.")
            .MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

public sealed class AddWorkflowStateCommandValidator
    : AbstractValidator<AddWorkflowStateCommand>
{
    public AddWorkflowStateCommandValidator()
    {
        RuleFor(command => command.DefinitionId).NotEmpty();
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("کد وضعیت الزامی است.")
            .MaximumLength(80);
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام وضعیت الزامی است.")
            .MaximumLength(200);
        RuleFor(command => command.StateType).IsInEnum();
        RuleFor(command => command.Order).GreaterThanOrEqualTo(0);
        RuleFor(command => command.AssigneeType).IsInEnum();
        RuleFor(command => command.Color).MaximumLength(30);
    }
}

public sealed class UpdateWorkflowStateCommandValidator
    : AbstractValidator<UpdateWorkflowStateCommand>
{
    public UpdateWorkflowStateCommandValidator()
    {
        RuleFor(command => command.DefinitionId).NotEmpty();
        RuleFor(command => command.StateId).NotEmpty();
        RuleFor(command => command.Code).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.StateType).IsInEnum();
        RuleFor(command => command.Order).GreaterThanOrEqualTo(0);
        RuleFor(command => command.AssigneeType).IsInEnum();
    }
}

public sealed class AddWorkflowTransitionCommandValidator
    : AbstractValidator<AddWorkflowTransitionCommand>
{
    public AddWorkflowTransitionCommandValidator()
    {
        RuleFor(command => command.DefinitionId).NotEmpty();
        RuleFor(command => command.FromStateId).NotEmpty();
        RuleFor(command => command.ToStateId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.GuardType).IsInEnum();
        RuleFor(command => command.GuardPluginKey)
            .NotEmpty()
            .When(command => command.GuardType == WorkflowGuardType.Plugin)
            .WithMessage("کلید Plugin برای Guard الزامی است.");
    }
}

public sealed class StartWorkflowCommandValidator
    : AbstractValidator<StartWorkflowCommand>
{
    public StartWorkflowCommandValidator()
    {
        RuleFor(command => command.EntityId).NotEmpty();
        RuleFor(command => command.EntityType).IsInEnum();
    }
}

public sealed class ExecuteWorkflowTransitionCommandValidator
    : AbstractValidator<ExecuteWorkflowTransitionCommand>
{
    public ExecuteWorkflowTransitionCommandValidator()
    {
        RuleFor(command => command.InstanceId).NotEmpty();
        RuleFor(command => command.TransitionId).NotEmpty();
        RuleFor(command => command.Comment).MaximumLength(2000);
        RuleFor(command => command.IdempotencyKey).MaximumLength(150);
    }
}
