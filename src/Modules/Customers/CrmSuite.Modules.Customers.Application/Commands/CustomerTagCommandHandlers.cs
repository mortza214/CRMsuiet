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

public sealed class CreateCustomerTagCommandHandler
    : IRequestHandler<CreateCustomerTagCommand, Result<CustomerTagDto>>
{
    private readonly ICustomerTagRepository _tags;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public CreateCustomerTagCommandHandler(
        ICustomerTagRepository tags,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _tags = tags;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerTagDto>> Handle(
        CreateCustomerTagCommand request,
        CancellationToken cancellationToken)
    {
        if (await _tags.NameExistsAsync(request.Name, null, cancellationToken))
        {
            return Result.Failure<CustomerTagDto>("تگی با این نام وجود دارد.", "CUSTOMERS_TAG_NAME_DUPLICATE");
        }

        var tag = new CustomerTag(request.Name, request.Color);
        tag.MarkCreated(_currentUser.UserId, _clock.UtcNow);
        _tags.Add(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _auditScope.EntityId = tag.Id.ToString();

        return Result.Success(_mapper.ToTagDto(tag));
    }
}

public sealed class UpdateCustomerTagCommandHandler
    : IRequestHandler<UpdateCustomerTagCommand, Result<CustomerTagDto>>
{
    private readonly ICustomerTagRepository _tags;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public UpdateCustomerTagCommandHandler(
        ICustomerTagRepository tags,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _tags = tags;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result<CustomerTagDto>> Handle(
        UpdateCustomerTagCommand request,
        CancellationToken cancellationToken)
    {
        var tag = await _tags.GetByIdAsync(request.Id, cancellationToken);
        if (tag is null)
        {
            return Result.Failure<CustomerTagDto>("تگ یافت نشد.", "CUSTOMERS_TAG_NOT_FOUND");
        }

        if (await _tags.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            return Result.Failure<CustomerTagDto>("تگی با این نام وجود دارد.", "CUSTOMERS_TAG_NAME_DUPLICATE");
        }

        _auditScope.OldValues = _mapper.ToTagDto(tag);
        _auditScope.EntityId = tag.Id.ToString();
        tag.Update(request.Name, request.Color, _clock.UtcNow, _currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.ToTagDto(tag));
    }
}

public sealed class DeleteCustomerTagCommandHandler : IRequestHandler<DeleteCustomerTagCommand, Result>
{
    private readonly ICustomerTagRepository _tags;
    private readonly ICustomerUnitOfWork _unitOfWork;
    private readonly IAuditScope _auditScope;
    private readonly FoundationCurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly CustomerDtoMapper _mapper;

    public DeleteCustomerTagCommandHandler(
        ICustomerTagRepository tags,
        ICustomerUnitOfWork unitOfWork,
        IAuditScope auditScope,
        FoundationCurrentUser currentUser,
        IDateTimeProvider clock,
        CustomerDtoMapper mapper)
    {
        _tags = tags;
        _unitOfWork = unitOfWork;
        _auditScope = auditScope;
        _currentUser = currentUser;
        _clock = clock;
        _mapper = mapper;
    }

    public async Task<Result> Handle(
        DeleteCustomerTagCommand request,
        CancellationToken cancellationToken)
    {
        var tag = await _tags.GetByIdAsync(request.Id, cancellationToken);
        if (tag is null)
        {
            return Result.Failure("تگ یافت نشد.", "CUSTOMERS_TAG_NOT_FOUND");
        }

        if (await _tags.IsInUseAsync(request.Id, cancellationToken))
        {
            return Result.Failure("تگ به مشتریان اختصاص داده شده و قابل حذف نیست.", "CUSTOMERS_TAG_IN_USE");
        }

        _auditScope.OldValues = _mapper.ToTagDto(tag);
        _auditScope.EntityId = tag.Id.ToString();
        tag.MarkDeleted(_currentUser.UserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success("تگ حذف شد.");
    }
}
