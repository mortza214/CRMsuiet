using CrmSuite.BuildingBlocks.Application.Security;
using CrmSuite.Modules.Foundation.Api.Extensions;
using CrmSuite.Modules.Payments.Api.Contracts;
using CrmSuite.Modules.Payments.Application.Commands;
using CrmSuite.Modules.Payments.Application.Queries;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmSuite.Modules.Payments.Api.Controllers.Portal;

[ApiController]
[Route("api/portal/payments")]
[Authorize(Roles = Roles.Customer)]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customers;
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(
        IMediator mediator,
        ICustomerRepository customers,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _customers = customers;
        _currentUser = currentUser;
    }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateManual(
        [FromBody] PortalManualPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CrmSuite.BuildingBlocks.Application.Common.ApiResponse<object>.Fail(
                    "کاربر جاری قابل تشخیص نیست."));
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            false,
            cancellationToken);
        if (customer is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CrmSuite.BuildingBlocks.Application.Common.ApiResponse<object>.Fail(
                    "پروفایل مشتری برای این حساب یافت نشد."));
        }

        var result = await _mediator.Send(
            new CreateManualPaymentCommand(
                request.InvoiceId,
                customer.Id,
                request.Amount,
                request.PaymentMethod,
                request.ReferenceNumber,
                request.ReceiptImageUrl),
            cancellationToken);

        if (result.IsFailure
            && result.Errors.Any(error => error.Contains(
                "PAYMENTS_INVOICE_CUSTOMER_MISMATCH",
                StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CrmSuite.BuildingBlocks.Application.Common.ApiResponse<object>.Fail(
                    "این فاکتور به شما تعلق ندارد."));
        }

        return this.ToApiResult(result, "فیش پرداخت ثبت شد و در انتظار تأیید مالی است.");
    }

    [HttpPost("saman/initiate")]
    public async Task<IActionResult> InitiateOnline(
        [FromBody] InitiateOnlinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new InitiateOnlinePaymentCommand(request.InvoiceId, request.CallbackUrl),
            cancellationToken);
        return this.ToApiResult(result, "لینک پرداخت ایجاد شد.");
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetMyTransactions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerTransactionsQuery(), cancellationToken);
        return this.ToApiResult(result);
    }
}


public sealed class PortalManualPaymentRequest
{
    public Guid? InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public CrmSuite.Modules.Payments.Domain.Enums.PaymentMethod PaymentMethod { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? ReceiptImageUrl { get; init; }
}
