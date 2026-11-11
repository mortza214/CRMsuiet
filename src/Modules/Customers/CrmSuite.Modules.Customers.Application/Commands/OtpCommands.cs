using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Customers.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Customers.Application.Commands;

public sealed record RequestOtpCommand(
    string Mobile,
    string? CorrelationId = null) : IRequest<Result<CustomerOtpRequestDto>>;

public sealed record VerifyOtpCommand(string Mobile, string Code) : IRequest<Result<CustomerAuthResultDto>>;
