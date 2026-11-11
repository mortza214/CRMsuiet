using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Customers.Domain.Enums;
using CrmSuite.Modules.Identity.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class CustomerLoginWithPasswordCommandHandler
    : IRequestHandler<CustomerLoginWithPasswordCommand, Result<CustomerAuthResultDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerIdentityService _identity;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public CustomerLoginWithPasswordCommandHandler(
        ICustomerRepository customers,
        ICustomerIdentityService identity,
        ICustomerUnitOfWork unitOfWork,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _customers = customers;
        _identity = identity;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerAuthResultDto>> Handle(
        CustomerLoginWithPasswordCommand request,
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

        var passwordHash = customer.PasswordHash;
        var defaultPassword = customer.Type == CustomerType.Natural
                              && !string.IsNullOrWhiteSpace(customer.NationalId)
            ? customer.NationalId
            : "123456";

        var valid = passwordHash is not null
            ? _hasher.Verify(request.Password, passwordHash)
            : string.Equals(request.Password, defaultPassword, StringComparison.Ordinal);

        if (!valid)
        {
            return Result.Failure<CustomerAuthResultDto>(
                "رمز عبور اشتباه است.",
                "CUSTOMERS_PASSWORD_INVALID");
        }

        if (passwordHash is null)
        {
            customer.SetPasswordHash(
                _hasher.Hash(defaultPassword),
                null,
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (customer.UserId is null)
        {
            var portalUser = await _identity.EnsurePortalUserAsync(customer, cancellationToken);
            if (portalUser.IsFailure)
            {
                return Result.Failure<CustomerAuthResultDto>(
                    portalUser.Errors,
                    portalUser.Message ?? "کاربر پورتال ایجاد نشد.");
            }

            customer.SetPortalUserId(portalUser.Value);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var token = await _identity.IssueTokenAsync(customer.UserId!.Value, cancellationToken);
        if (token.IsFailure)
        {
            return Result.Failure<CustomerAuthResultDto>(token.Errors, token.Message);
        }

        return Result.Success(new CustomerAuthResultDto(
            token.Value!.AccessToken,
            token.Value.ExpiresAtUtc,
            token.Value.ExpiresAtJalali,
            _mapper.ToCustomerDto(customer)));
    }
}

public sealed class CustomerChangePasswordCommandHandler
    : IRequestHandler<CustomerChangePasswordCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;
    private readonly FoundationCurrentUser _currentUser;

    public CustomerChangePasswordCommandHandler(
        ICustomerRepository customers,
        ICustomerUnitOfWork unitOfWork,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper,
        FoundationCurrentUser currentUser)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _clock = clock;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerDto>> Handle(
        CustomerChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<CustomerDto>("کاربر جاری قابل تشخیص نیست.", "CUSTOMERS_USER_REQUIRED");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            includeDetails: true,
            cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerDto>("پروفایل مشتری یافت نشد.", "CUSTOMERS_NOT_FOUND");
        }

        var defaultPassword = customer.Type == CustomerType.Natural
                              && !string.IsNullOrWhiteSpace(customer.NationalId)
            ? customer.NationalId
            : "123456";
        var currentValid = customer.PasswordHash is not null
            ? _hasher.Verify(request.CurrentPassword, customer.PasswordHash)
            : string.Equals(request.CurrentPassword, defaultPassword, StringComparison.Ordinal);

        if (!currentValid)
        {
            return Result.Failure<CustomerDto>("رمز عبور فعلی اشتباه است.", "CUSTOMERS_PASSWORD_INVALID");
        }

        if (request.NewPassword.Length < 6)
        {
            return Result.Failure<CustomerDto>(
                "رمز عبور جدید حداقل ۶ کاراکتر باشد.",
                "CUSTOMERS_PASSWORD_WEAK");
        }

        customer.SetPasswordHash(
            _hasher.Hash(request.NewPassword),
            _currentUser.UserId,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(_mapper.ToCustomerDto(customer));
    }
}
