using FluentValidation;

namespace CrmSuite.Modules.Tickets.Application.Commands;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(command => command.Subject)
            .NotEmpty().WithMessage("موضوع تیکت الزامی است.")
            .MaximumLength(300);

        RuleFor(command => command.Message)
            .NotEmpty().WithMessage("متن پیام اولیه الزامی است.")
            .MaximumLength(5000);

        RuleFor(command => command.DepartmentId).NotEmpty().WithMessage("دپارتمان الزامی است.");
        RuleFor(command => command.Priority).IsInEnum();
        RuleFor(command => command.Source).IsInEnum();
    }
}

public sealed class AddTicketMessageCommandValidator : AbstractValidator<AddTicketMessageCommand>
{
    public AddTicketMessageCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Message)
            .NotEmpty().WithMessage("متن پیام الزامی است.")
            .MaximumLength(5000);
        RuleFor(command => command.SenderType).IsInEnum();
    }
}

public sealed class ChangeTicketStatusCommandValidator : AbstractValidator<ChangeTicketStatusCommand>
{
    public ChangeTicketStatusCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
    }
}

public sealed class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
    }
}
