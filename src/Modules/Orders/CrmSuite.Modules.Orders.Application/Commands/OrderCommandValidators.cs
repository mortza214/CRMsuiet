using FluentValidation;

namespace CrmSuite.Modules.Orders.Application.Commands;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty().WithMessage("شناسه مشتری الزامی است.");

        RuleFor(command => command.OrderType)
            .IsInEnum().WithMessage("نوع سفارش معتبر نیست.");

        RuleFor(command => command.Items)
            .NotNull().WithMessage("آیتم‌های سفارش الزامی است.")
            .Must(items => items.Count > 0).WithMessage("سفارش باید حداقل یک آیتم داشته باشد.");

        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(value => value.Quantity)
                .GreaterThan(0).WithMessage("تعداد آیتم باید بزرگ‌تر از صفر باشد.");

            item.RuleFor(value => value.BillingCycle)
                .IsInEnum().WithMessage("دوره پرداخت معتبر نیست.");

            item.RuleFor(value => value)
                .Must(value => new[] { value.ServiceId, value.DataCenterItemId, value.AddonId }
                    .Count(id => id.HasValue) == 1)
                .WithMessage("هر آیتم باید دقیقاً یکی از سرویس، آیتم دیتاسنتر یا آیتم جانبی باشد.");
        });
    }
}

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه سفارش الزامی است.");
        RuleFor(command => command.Status).IsInEnum().WithMessage("وضعیت سفارش معتبر نیست.");
    }
}

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه سفارش الزامی است.");
    }
}

public sealed class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("شناسه سفارش الزامی است.");
        RuleFor(command => command.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("مبلغ تخفیف نمی‌تواند منفی باشد.");
    }
}
