using CrmSuite.BuildingBlocks.Application.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Abstractions;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Application.Mapping;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using MediatR;
using FoundationCurrentUser = CrmSuite.Modules.Foundation.Application.Abstractions.ICurrentUserService;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed class CreateCustomerGroupCommandHandler
    : IRequestHandler<CreateCustomerGroupCommand, Result<CustomerGroupDto>>
{
    private readonly ICustomerGroupRepository _groups;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public CreateCustomerGroupCommandHandler(
        ICustomerGroupRepository groups,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _groups = groups;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerGroupDto>> Handle(
        CreateCustomerGroupCommand request,
        CancellationToken cancellationToken)
    {
        if (await _groups.NameExistsAsync(request.Name, null, cancellationToken))
        {
            return Result.Failure<CustomerGroupDto>("گروهی با این نام وجود دارد.", "CUSTOMERS_GROUP_NAME_DUPLICATE");
        }

        var group = new CustomerGroup(request.Name, request.Description);
        group.MarkCreated(_currentUser.UserId, _clock.UtcNow);
        _groups.Add(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = group.Id.ToString();

        return Result.Success(_mapper.ToGroupDto(group));
    }
}

public sealed class UpdateCustomerGroupCommandHandler
    : IRequestHandler<UpdateCustomerGroupCommand, Result<CustomerGroupDto>>
{
    private readonly ICustomerGroupRepository _groups;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public UpdateCustomerGroupCommandHandler(
        ICustomerGroupRepository groups,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _groups = groups;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerGroupDto>> Handle(
        UpdateCustomerGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _groups.GetByIdAsync(request.Id, cancellationToken);
        if (group is null)
        {
            return Result.Failure<CustomerGroupDto>("گروه یافت نشد.", "CUSTOMERS_GROUP_NOT_FOUND");
        }

        if (await _groups.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            return Result.Failure<CustomerGroupDto>("گروهی با این نام وجود دارد.", "CUSTOMERS_GROUP_NAME_DUPLICATE");
        }

        _auditScope.OldValues = _mapper.ToGroupDto(group);
        _auditScope.EntityId = group.Id.ToString();
        group.Update(request.Name, request.Description, _clock.UtcNow, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToGroupDto(group));
    }
}

public sealed class DeleteCustomerGroupCommandHandler
    : IRequestHandler<DeleteCustomerGroupCommand, Result>
{
    private readonly ICustomerGroupRepository _groups;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public DeleteCustomerGroupCommandHandler(
        ICustomerGroupRepository groups,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _groups = groups;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result> Handle(
        DeleteCustomerGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _groups.GetByIdAsync(request.Id, cancellationToken);
        if (group is null)
        {
            return Result.Failure("گروه یافت نشد.", "CUSTOMERS_GROUP_NOT_FOUND");
        }

        if (await _groups.IsInUseAsync(request.Id, cancellationToken))
        {
            return Result.Failure("گروه به مشتریان اختصاص داده شده و قابل حذف نیست.", "CUSTOMERS_GROUP_IN_USE");
        }

        _auditScope.OldValues = _mapper.ToGroupDto(group);
        _auditScope.EntityId = group.Id.ToString();
        group.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("گروه حذف شد.");
    }
}
