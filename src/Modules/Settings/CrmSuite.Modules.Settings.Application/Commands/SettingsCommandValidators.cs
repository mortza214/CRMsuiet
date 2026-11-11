using FluentValidation;

namespace CrmSuite.Modules.Settings.Application.Commands;

public sealed class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(command => command.Items).NotEmpty();
        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(value => value.Key).NotEmpty();
            item.RuleFor(value => value.Group).NotEmpty();
        });
    }
}

public sealed class CreateTaxRuleCommandValidator : AbstractValidator<CreateTaxRuleCommand>
{
    public CreateTaxRuleCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Percentage).InclusiveBetween(0, 100);
    }
}

public sealed class UpdateTaxRuleCommandValidator : AbstractValidator<UpdateTaxRuleCommand>
{
    public UpdateTaxRuleCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Percentage).InclusiveBetween(0, 100);
    }
}

public sealed class UpdateGatewayConfigCommandValidator : AbstractValidator<UpdateGatewayConfigCommand>
{
    public UpdateGatewayConfigCommandValidator()
    {
        RuleFor(command => command.Provider).IsInEnum();
        RuleFor(command => command.MerchantId).NotEmpty();
        RuleFor(command => command.TerminalId).NotEmpty();
    }
}
