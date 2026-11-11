using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Application.Notifications;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class CreateCustomerCommandHandler
    : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerGroupRepository _groups;
    private readonly ICustomerTagRepository _tags;
    private readonly ICustomerIdentityService _identity;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;
    private readonly IMediator? _mediator;

    public CreateCustomerCommandHandler(
        ICustomerRepository customers,
        ICustomerGroupRepository groups,
        ICustomerTagRepository tags,
        ICustomerIdentityService identity,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper,
        IMediator? mediator = null)
    {
        _customers = customers;
        _groups = groups;
        _tags = tags;
        _identity = identity;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        if (await _customers.MobileExistsAsync(request.Mobile, null, cancellationToken))
        {
            return Result.Failure<CustomerDto>(
                "مشتری با این موبایل قبلاً ثبت شده است.",
                "CUSTOMERS_MOBILE_DUPLICATE");
        }

        if (await _customers.NationalIdExistsAsync(request.NationalId, null, cancellationToken))
        {
            return Result.Failure<CustomerDto>(
                "مشتری با این کد/شناسه ملی قبلاً ثبت شده است.",
                "CUSTOMERS_NATIONAL_ID_DUPLICATE");
        }

        var validation = await ValidateReferencesAsync(request, cancellationToken);
        if (validation is not null)
        {
            return Result.Failure<CustomerDto>(validation);
        }

        var now = _clock.UtcNow;
        var ownerUserId = request.OwnerUserId ?? _currentUser.UserId;
        var customer = new Customer(
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
            ownerUserId,
            request.ParentCustomerId,
            request.IsReseller,
            request.GroupId,
            null,
            request.OwnerType);

        customer.MarkCreated(_currentUser.UserId, now);

        var identityResult = await _identity.EnsurePortalUserAsync(customer, cancellationToken);
        if (identityResult.IsFailure)
        {
            return Result.Failure<CustomerDto>(identityResult.Message ?? "ایجاد کاربر پورتال انجام نشد.");
        }

        customer.SetPortalUserId(identityResult.Value);
        customer.ReplaceTags(request.TagIds ?? []);
        _customers.Add(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _auditScope.EntityId = customer.Id.ToString();

        if (_mediator is not null)
        {
            try
            {
                await _mediator.Publish(
                    new CustomerCreatedNotification(customer.Id),
                    cancellationToken);
            }
            catch
            {
                // Customer creation must not fail because a downstream workflow is unavailable.
            }
        }

        var created = await _customers.GetByIdAsync(customer.Id, includeDetails: true, cancellationToken);

        return Result.Success(_mapper.ToCustomerDto(created ?? customer));
    }

    private async Task<string?> ValidateReferencesAsync(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        if (request.GroupId.HasValue)
        {
            var group = await _groups.GetByIdAsync(request.GroupId.Value, cancellationToken);
            if (group is null)
            {
                return "گروه مشتری انتخاب‌شده معتبر نیست.";
            }
        }

        if (request.ParentCustomerId.HasValue)
        {
            var parent = await _customers.GetByIdAsync(
                request.ParentCustomerId.Value,
                includeDetails: false,
                cancellationToken);

            if (parent is null)
            {
                return "مشتری والد یافت نشد.";
            }

            if (!parent.IsReseller)
            {
                return "مشتری والد باید ریسلر باشد.";
            }
        }

        if (request.TagIds is { Count: > 0 })
        {
            var tags = await _tags.GetByIdsAsync(request.TagIds, cancellationToken);
            if (tags.Count != request.TagIds.Distinct().Count())
            {
                return "یکی از تگ‌های انتخاب‌شده معتبر نیست.";
            }
        }

        return null;
    }
}
