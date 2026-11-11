using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed record UpdateMyCustomerProfileCommand(
    string? Phone,
    string? Email,
    string? Address) : IRequest<Result<CustomerDto>>;

public sealed class UpdateMyCustomerProfileCommandHandler
    : IRequestHandler<UpdateMyCustomerProfileCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public UpdateMyCustomerProfileCommandHandler(
        ICustomerRepository customers,
        ICustomerUnitOfWork unitOfWork,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(
        UpdateMyCustomerProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Result.Failure<CustomerDto>(
                "کد کاربر جاری قابل تشخیص نیست.",
                "CUSTOMERS_CURRENT_USER_MISSING");
        }

        var customer = await _customers.GetByUserIdAsync(
            _currentUser.UserId.Value,
            includeDetails: false,
            cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerDto>(
                "پروفایل مشتری یافت نشد.",
                "CUSTOMERS_NOT_FOUND");
        }

        customer.Update(
            customer.Type,
            customer.FirstName,
            customer.LastName,
            customer.CompanyName,
            customer.EconomicCode,
            customer.NationalId,
            customer.Mobile,
            request.Phone,
            request.Email,
            request.Address,
            customer.OwnerUserId,
            customer.ParentCustomerId,
            customer.IsReseller,
            customer.GroupId,
            customer.UserId,
            _clock.UtcNow,
            _currentUser.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToCustomerDto(customer));
    }
}
