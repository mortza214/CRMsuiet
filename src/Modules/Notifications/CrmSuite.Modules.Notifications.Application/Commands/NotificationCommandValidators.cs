using FluentValidation;

namespace CrmSuite.Modules.Notifications.Application.Commands;

public sealed class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(command => command.TemplateCode).NotEmpty();
        RuleFor(command => command.Recipient).NotEmpty();
        RuleFor(command => command.Channel).IsInEnum();
    }
}

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
