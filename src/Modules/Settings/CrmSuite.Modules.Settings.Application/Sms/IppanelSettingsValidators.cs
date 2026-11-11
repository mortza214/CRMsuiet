using FluentValidation;

namespace CrmSuite.Modules.Settings.Application.Sms;

public sealed class UpdateIppanelSettingsCommandValidator
    : AbstractValidator<UpdateIppanelSettingsCommand>
{
    public UpdateIppanelSettingsCommandValidator()
    {
        RuleFor(command => command.FromNumber).MaximumLength(30);
        RuleFor(command => command.PatternCode).MaximumLength(100);
        RuleFor(command => command.PatternParamName).MaximumLength(100);
        RuleFor(command => command.BaseUrl)
            .MaximumLength(500)
            .Must(BeAbsoluteUrl)
            .WithMessage("BaseUrl باید یک URL معتبر باشد.");
    }

    private static bool BeAbsoluteUrl(string value) =>
        string.IsNullOrWhiteSpace(value)
        || (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}

public sealed class TestIppanelSettingsCommandValidator
    : AbstractValidator<TestIppanelSettingsCommand>
{
    public TestIppanelSettingsCommandValidator()
    {
        RuleFor(command => command.Mobile)
            .NotEmpty().WithMessage("موبایل تست الزامی است.")
            .MaximumLength(20);
    }
}
