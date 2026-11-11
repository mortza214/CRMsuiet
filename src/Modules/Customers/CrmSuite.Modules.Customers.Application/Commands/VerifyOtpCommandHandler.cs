using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Application.Options;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Foundation.Application.Auditing;
using CrmSuite.Modules.Foundation.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class VerifyOtpCommandHandler
    : IRequestHandler<VerifyOtpCommand, Result<CustomerAuthResultDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerOtpRepository _otps;
    private readonly ICustomerIdentityService _identity;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;
    private readonly OtpOptions _options;

    public VerifyOtpCommandHandler(
        ICustomerRepository customers,
        ICustomerOtpRepository otps,
        ICustomerIdentityService identity,
        ICustomerUnitOfWork unitOfWork,
        IAuditService auditService,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper,
        IOptions<OtpOptions> options)
    {
        _customers = customers;
        _otps = otps;
        _identity = identity;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _options = options.Value;
    }

    public async Task<Result<CustomerAuthResultDto>> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByMobileAsync(
            request.Mobile,
            includeDetails: true,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerAuthResultDto>(
                "مشتری با این موبایل یافت نشد.",
                "CUSTOMERS_NOT_FOUND");
        }

        var otp = await _otps.GetLatestUnusedAsync(customer.Mobile, cancellationToken);
        var now = _clock.UtcNow;

        if (otp is null || otp.IsUsed || otp.IsExpired(now))
        {
            if (otp is not null && !otp.IsUsed)
            {
                otp.MarkUsed();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await RecordFailureAsync(customer.Id, customer.Mobile, "OTP منقضی یا یافت نشد.", cancellationToken);
            return Result.Failure<CustomerAuthResultDto>(
                "کد OTP معتبر یا فعال نیست.",
                "CUSTOMERS_OTP_INVALID");
        }

        if (!string.Equals(otp.Code, request.Code, StringComparison.Ordinal))
        {
            otp.RegisterFailedAttempt();
            if (otp.AttemptCount >= _options.MaxAttempts)
            {
                otp.MarkUsed();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await RecordFailureAsync(customer.Id, customer.Mobile, "کد OTP اشتباه است.", cancellationToken);

            return Result.Failure<CustomerAuthResultDto>(
                "کد OTP اشتباه است.",
                "CUSTOMERS_OTP_MISMATCH");
        }

        otp.MarkUsed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (customer.UserId is null)
        {
            var identityResult = await _identity.EnsurePortalUserAsync(customer, cancellationToken);
            if (identityResult.IsFailure)
            {
                return Result.Failure<CustomerAuthResultDto>(
                    identityResult.Errors,
                    identityResult.Message ?? "کاربر پورتال یافت نشد.");
            }

            customer.SetPortalUserId(identityResult.Value);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var tokenResult = await _identity.IssueTokenAsync(customer.UserId!.Value, cancellationToken);
        if (tokenResult.IsFailure)
        {
            return Result.Failure<CustomerAuthResultDto>(
                tokenResult.Errors,
                tokenResult.Message ?? "صدور توکن انجام نشد.");
        }

        await _auditService.RecordAsync(
            new AuditEntry(
                "CustomerAuth",
                customer.Id.ToString(),
                AuditAction.LoginSucceeded,
                null,
                new { customer.Mobile },
                customer.UserId),
            cancellationToken);

        var token = tokenResult.Value;
        return Result.Success(new CustomerAuthResultDto(
            token!.AccessToken,
            token.ExpiresAtUtc,
            token.ExpiresAtJalali,
            _mapper.ToCustomerDto(customer)));
    }

    private async Task RecordFailureAsync(
        Guid customerId,
        string mobile,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditService.RecordAsync(
            new AuditEntry(
                "CustomerAuth",
                customerId.ToString(),
                AuditAction.LoginFailed,
                null,
                new { mobile, reason }),
            cancellationToken);
    }
}
