using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class UpdateCustomerCommandHandler
    : IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerGroupRepository _groups;
    private readonly ICustomerTagRepository _tags;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customers,
        ICustomerGroupRepository groups,
        ICustomerTagRepository tags,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _customers = customers;
        _groups = groups;
        _tags = tags;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerDto>> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.Id, includeDetails: true, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerDto>("مشتری یافت نشد.", "CUSTOMERS_NOT_FOUND");
        }

        if (await _customers.MobileExistsAsync(request.Mobile, request.Id, cancellationToken))
        {
            return Result.Failure<CustomerDto>(
                "مشتری دیگری با این موبایل ثبت شده است.",
                "CUSTOMERS_MOBILE_DUPLICATE");
        }

        if (await _customers.NationalIdExistsAsync(request.NationalId, request.Id, cancellationToken))
        {
            return Result.Failure<CustomerDto>(
                "مشتری دیگری با این کد/شناسه ملی ثبت شده است.",
                "CUSTOMERS_NATIONAL_ID_DUPLICATE");
        }

        if (request.ParentCustomerId.HasValue)
        {
            if (request.ParentCustomerId.Value == request.Id)
            {
                return Result.Failure<CustomerDto>(
                    "مشتری نمی‌تواند والد خودش باشد.",
                    "CUSTOMERS_SELF_PARENT");
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

            if (await _customers.IsDescendantAsync(request.Id, request.ParentCustomerId.Value, cancellationToken))
            {
                return Result.Failure<CustomerDto>(
                    "انتخاب این والد باعث ایجاد حلقه در سلسله‌مراتب مشتریان می‌شود.",
                    "CUSTOMERS_PARENT_CYCLE");
            }
        }

        if (request.GroupId.HasValue
            && await _groups.GetByIdAsync(request.GroupId.Value, cancellationToken) is null)
        {
            return Result.Failure<CustomerDto>("گروه مشتری انتخاب‌شده معتبر نیست.", "CUSTOMERS_GROUP_NOT_FOUND");
        }

        if (request.TagIds is { Count: > 0 })
        {
            var tags = await _tags.GetByIdsAsync(request.TagIds, cancellationToken);
            if (tags.Count != request.TagIds.Distinct().Count())
            {
                return Result.Failure<CustomerDto>("یکی از تگ‌های انتخاب‌شده معتبر نیست.", "CUSTOMERS_TAG_NOT_FOUND");
            }
        }

        _auditScope.OldValues = _mapper.ToCustomerDto(customer);
        _auditScope.EntityId = customer.Id.ToString();

        customer.Update(
            request.Type,
            request.FirstName,
            request.LastName,
            request.CompanyName,
            request.EconomicCode,
            request.NationalId,
            request.Mobile,
            request.Phone,
            request.Email,
            request.Address,
            request.OwnerUserId ?? _currentUser.UserId,
            request.ParentCustomerId,
            request.IsReseller,
            request.GroupId,
            customer.UserId,
            _clock.UtcNow,
            _currentUser.UserId,
            request.OwnerType,
            request.PreferredNotificationChannel);

        if (request.TagIds is not null)
        {
            customer.ReplaceTags(request.TagIds);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _customers.GetByIdAsync(customer.Id, includeDetails: true, cancellationToken);

        return Result.Success(_mapper.ToCustomerDto(updated ?? customer));
    }
}
