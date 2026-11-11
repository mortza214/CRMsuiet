using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed record CustomerLoginWithPasswordCommand(
    string Mobile,
    string Password) : IRequest<Result<CustomerAuthResultDto>>;

public sealed record CustomerChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result<CustomerDto>>;
