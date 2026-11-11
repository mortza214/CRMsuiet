using FluentValidation;

namespace CrmSuite.Modules.Catalog.Application.Commands;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("کد سرویس الزامی است.")
            .Matches("^[A-Za-z0-9._-]{2,50}$")
            .WithMessage("کد سرویس فقط می‌تواند شامل حروف لاتین، رقم، نقطه، خط تیره و زیرخط باشد.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام سرویس الزامی است.")
            .MaximumLength(200).WithMessage("نام سرویس حداکثر ۲۰۰ کاراکتر است.");

        RuleFor(command => command.CategoryId)
            .NotEmpty().WithMessage("دسته‌بندی سرویس الزامی است.");

        RuleFor(command => command.ServiceType).IsInEnum().WithMessage("نوع سرویس معتبر نیست.");
        RuleFor(command => command.Unit).IsInEnum().WithMessage("واحد سرویس معتبر نیست.");

        RuleFor(command => command.InitialPrice)
            .GreaterThan(0).WithMessage("قیمت اولیه باید بزرگ‌تر از صفر باشد.")
            .When(command => command.InitialPrice.HasValue);

        RuleFor(command => command.InitialPrice)
            .NotNull().WithMessage("برای فعال‌سازی سرویس، قیمت اولیه الزامی است.")
            .When(command => command.IsActive);
    }
}

public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه سرویس الزامی است.");
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("کد سرویس الزامی است.")
            .Matches("^[A-Za-z0-9._-]{2,50}$")
            .WithMessage("کد سرویس معتبر نیست.");
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام سرویس الزامی است.")
            .MaximumLength(200).WithMessage("نام سرویس حداکثر ۲۰۰ کاراکتر است.");
        RuleFor(command => command.CategoryId).NotEmpty().WithMessage("دسته‌بندی سرویس الزامی است.");
        RuleFor(command => command.ServiceType).IsInEnum().WithMessage("نوع سرویس معتبر نیست.");
        RuleFor(command => command.Unit).IsInEnum().WithMessage("واحد سرویس معتبر نیست.");
    }
}

public sealed class ToggleServiceStatusCommandValidator : AbstractValidator<ToggleServiceStatusCommand>
{
    public ToggleServiceStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه سرویس الزامی است.");
    }
}

public sealed class SetServicePriceCommandValidator : AbstractValidator<SetServicePriceCommand>
{
    public SetServicePriceCommandValidator()
    {
        RuleFor(command => command.ServiceId).NotEmpty().WithMessage("شناسه سرویس الزامی است.");
        RuleFor(command => command.Price)
            .GreaterThan(0).WithMessage("قیمت باید بزرگ‌تر از صفر باشد.");
    }
}

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام دسته‌بندی الزامی است.")
            .MaximumLength(150).WithMessage("نام دسته‌بندی حداکثر ۱۵۰ کاراکتر است.");
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه دسته‌بندی الزامی است.");
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("نام دسته‌بندی الزامی است.")
            .MaximumLength(150).WithMessage("نام دسته‌بندی حداکثر ۱۵۰ کاراکتر است.");
    }
}

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه دسته‌بندی الزامی است.");
    }
}

public sealed class CreateAddonCommandValidator : AbstractValidator<CreateAddonCommand>
{
    public CreateAddonCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("نام آیتم جانبی الزامی است.");
        RuleFor(command => command.Price).GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد.");
        RuleFor(command => command.Unit).IsInEnum().WithMessage("واحد معتبر نیست.");
    }
}

public sealed class UpdateAddonCommandValidator : AbstractValidator<UpdateAddonCommand>
{
    public UpdateAddonCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه آیتم جانبی الزامی است.");
        RuleFor(command => command.Name).NotEmpty().WithMessage("نام آیتم جانبی الزامی است.");
        RuleFor(command => command.Price).GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد.");
        RuleFor(command => command.Unit).IsInEnum().WithMessage("واحد معتبر نیست.");
    }
}

public sealed class DeleteAddonCommandValidator : AbstractValidator<DeleteAddonCommand>
{
    public DeleteAddonCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه آیتم جانبی الزامی است.");
    }
}

public sealed class CreateDataCenterItemCommandValidator : AbstractValidator<CreateDataCenterItemCommand>
{
    public CreateDataCenterItemCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("نام آیتم دیتاسنتر الزامی است.");
        RuleFor(command => command.Price).GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد.");
        RuleFor(command => command.ItemType).IsInEnum().WithMessage("نوع آیتم معتبر نیست.");
        RuleFor(command => command.Unit).IsInEnum().WithMessage("واحد معتبر نیست.");
    }
}

public sealed class UpdateDataCenterItemCommandValidator : AbstractValidator<UpdateDataCenterItemCommand>
{
    public UpdateDataCenterItemCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه آیتم الزامی است.");
        RuleFor(command => command.Name).NotEmpty().WithMessage("نام آیتم دیتاسنتر الزامی است.");
        RuleFor(command => command.Price).GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد.");
        RuleFor(command => command.ItemType).IsInEnum().WithMessage("نوع آیتم معتبر نیست.");
        RuleFor(command => command.Unit).IsInEnum().WithMessage("واحد معتبر نیست.");
    }
}

public sealed class DeleteDataCenterItemCommandValidator : AbstractValidator<DeleteDataCenterItemCommand>
{
    public DeleteDataCenterItemCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه آیتم الزامی است.");
    }
}
