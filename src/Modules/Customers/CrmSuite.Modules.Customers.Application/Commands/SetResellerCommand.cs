using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed record SetResellerCommand(
    Guid CustomerId,
    bool IsReseller,
    Guid? ParentCustomerId = null) : IRequest<Result<CustomerDto>>;
