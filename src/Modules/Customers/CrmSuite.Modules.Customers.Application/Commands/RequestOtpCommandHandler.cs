using System.Globalization;
using System.Security.Cryptography;
using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Options;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Options;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class RequestOtpCommandHandler
    : IRequestHandler<RequestOtpCommand, Result<CustomerOtpRequestDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerOtpRepository _otps;
    private readonly ICustomerIdentityService _identity;
    private readonly IOtpSender _otpSender;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IJalaliDateService _jalaliDate;
    private readonly OtpOptions _options;

    public RequestOtpCommandHandler(
        ICustomerRepository customers,
        ICustomerOtpRepository otps,
        ICustomerIdentityService identity,
        IOtpSender otpSender,
        ICustomerUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        IJalaliDateService jalaliDate,
        IOptions<OtpOptions> options)
    {
        _customers = customers;
        _otps = otps;
        _identity = identity;
        _otpSender = otpSender;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _jalaliDate = jalaliDate;
        _options = options.Value;
    }

    public async Task<Result<CustomerOtpRequestDto>> Handle(
        RequestOtpCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByMobileAsync(
            request.Mobile,
            includeDetails: false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerOtpRequestDto>(
                "مشتری با این موبایل یافت نشد.",
                "CUSTOMERS_NOT_FOUND");
        }

        var now = _clock.UtcNow;
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId.Trim();

        var previous = await _otps.GetByCorrelationIdAsync(
            correlationId,
            cancellationToken);
        if (previous is not null)
        {
            return Result.Success(new CustomerOtpRequestDto(
                previous.Mobile,
                previous.ExpiresAtUtc,
                _jalaliDate.ToJalali(previous.ExpiresAtUtc),
                previous.CorrelationId));
        }

        if (customer.UserId is null)
        {
            var identityResult = await _identity.EnsurePortalUserAsync(customer, cancellationToken);
            if (identityResult.IsFailure)
            {
                return Result.Failure<CustomerOtpRequestDto>(
                    identityResult.Errors,
                    identityResult.Message ?? "ایجاد کاربر پورتال انجام نشد.");
            }

            customer.SetPortalUserId(identityResult.Value);
        }

        await _otps.InvalidateActiveAsync(customer.Mobile, now, cancellationToken);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
        var expiresAt = now.AddSeconds(Math.Max(30, _options.TtlSeconds));
        var otp = new CustomerOtp(
            customer.Mobile,
            code,
            expiresAt,
            _currentUser.IpAddress,
            now,
            correlationId);

        _otps.Add(otp);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _otpSender.SendAsync(customer.Mobile, code, cancellationToken);

        return Result.Success(new CustomerOtpRequestDto(
            customer.Mobile,
            expiresAt,
            _jalaliDate.ToJalali(expiresAt),
            correlationId));
    }
}
