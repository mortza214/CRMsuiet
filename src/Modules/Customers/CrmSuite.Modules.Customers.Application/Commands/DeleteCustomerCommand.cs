using CrmSuite.BuildingBlocks.Domain.Results;
using MediatR;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed record DeleteCustomerCommand(Guid Id) : IRequest<Result>;
