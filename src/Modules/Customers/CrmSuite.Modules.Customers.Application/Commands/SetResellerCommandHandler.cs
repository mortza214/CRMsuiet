using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class SetResellerCommandHandler
    : IRequestHandler<SetResellerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public SetResellerCommandHandler(
        ICustomerRepository customers,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(
        SetResellerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, includeDetails: true, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerDto>("مشتری یافت نشد.", "CUSTOMERS_NOT_FOUND");
        }

        if (request.ParentCustomerId.HasValue)
        {
            if (request.ParentCustomerId.Value == request.CustomerId)
            {
                return Result.Failure<CustomerDto>("مشتری نمی‌تواند والد خودش باشد.", "CUSTOMERS_SELF_PARENT");
            }

            var parent = await _customers.GetByIdAsync(
                request.ParentCustomerId.Value,
                includeDetails: false,
                cancellationToken);

            if (parent is null)
            {
                return Result.Failure<CustomerDto>("مشتری والد یافت نشد.", "CUSTOMERS_PARENT_NOT_FOUND");
            }

            if (!parent.IsReseller)
            {
                return Result.Failure<CustomerDto>("مشتری والد باید ریسلر باشد.", "CUSTOMERS_PARENT_NOT_RESELLER");
            }

            if (await _customers.IsDescendantAsync(request.CustomerId, request.ParentCustomerId.Value, cancellationToken))
            {
                return Result.Failure<CustomerDto>(
                    "انتخاب این والد باعث ایجاد حلقه در سلسله‌مراتب می‌شود.",
                    "CUSTOMERS_PARENT_CYCLE");
            }
        }

        _auditScope.OldValues = _mapper.ToCustomerDto(customer);
        _auditScope.EntityId = customer.Id.ToString();

        customer.SetReseller(request.IsReseller, request.ParentCustomerId, _currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _customers.GetByIdAsync(customer.Id, includeDetails: true, cancellationToken);
        return Result.Success(_mapper.ToCustomerDto(updated ?? customer));
    }
}
