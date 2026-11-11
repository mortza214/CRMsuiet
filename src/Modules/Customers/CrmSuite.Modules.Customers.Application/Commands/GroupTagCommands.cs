using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed record CreateCustomerGroupCommand(
    string Name,
    string? Description) : IRequest<Result<CustomerGroupDto>>;

public sealed record UpdateCustomerGroupCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest<Result<CustomerGroupDto>>;

public sealed record DeleteCustomerGroupCommand(Guid Id) : IRequest<Result>;

public sealed record CreateCustomerTagCommand(
    string Name,
    string Color) : IRequest<Result<CustomerTagDto>>;

public sealed record UpdateCustomerTagCommand(
    Guid Id,
    string Name,
    string Color) : IRequest<Result<CustomerTagDto>>;

public sealed record DeleteCustomerTagCommand(Guid Id) : IRequest<Result>;
