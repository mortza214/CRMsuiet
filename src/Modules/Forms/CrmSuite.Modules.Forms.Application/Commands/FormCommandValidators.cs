using FluentValidation;

namespace CrmSuite.Modules.Forms.Application.Commands;

public sealed class CreateFormTemplateCommandValidator : AbstractValidator<CreateFormTemplateCommand>
{
    public CreateFormTemplateCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("عنوان فرم الزامی است.")
            .MaximumLength(200).WithMessage("عنوان فرم حداکثر ۲۰۰ کاراکتر است.");
        RuleFor(command => command.Code)
            .MaximumLength(64)
            .When(command => !string.IsNullOrWhiteSpace(command.Code));
        RuleFor(command => command.SchemaJson)
            .NotEmpty().WithMessage("Schema فرم الزامی است.");
        RuleFor(command => command.Version).GreaterThanOrEqualTo(1);
        RuleFor(command => command.EntityType).IsInEnum();
    }
}

public sealed class UpdateFormTemplateCommandValidator : AbstractValidator<UpdateFormTemplateCommand>
{
    public UpdateFormTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("کد فرم الزامی است.")
            .MaximumLength(64);
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("عنوان فرم الزامی است.")
            .MaximumLength(200);
        RuleFor(command => command.SchemaJson).NotEmpty();
        RuleFor(command => command.EntityType).IsInEnum();
    }
}

public sealed class PublishFormTemplateCommandValidator : AbstractValidator<PublishFormTemplateCommand>
{
    public PublishFormTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class ArchiveFormTemplateCommandValidator
    : AbstractValidator<ArchiveFormTemplateCommand>
{
    public ArchiveFormTemplateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class SubmitFormCommandValidator : AbstractValidator<SubmitFormCommand>
{
    public SubmitFormCommandValidator()
    {
        RuleFor(command => command.FormTemplateId).NotEmpty();
        RuleFor(command => command.SubmittedDataJson)
            .NotEmpty().WithMessage("داده‌های فرم الزامی است.");
    }
}

public sealed class ReviewFormSubmissionCommandValidator
    : AbstractValidator<ReviewFormSubmissionCommand>
{
    public ReviewFormSubmissionCommandValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.Note).MaximumLength(2000);
    }
}
