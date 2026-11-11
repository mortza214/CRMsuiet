using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed record CreateCustomerCommand(
    CustomerType Type,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? EconomicCode,
    string NationalId,
    string Mobile,
    string? Phone,
    string? Email,
    string? Address,
    Guid? OwnerUserId = null,
    Guid? ParentCustomerId = null,
    bool IsReseller = false,
    Guid? GroupId = null,
    IReadOnlyCollection<Guid>? TagIds = null,
    CustomerOwnerType OwnerType = CustomerOwnerType.CurrentUser) : IRequest<Result<CustomerDto>>;
