using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Foundation.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Abstractions;
using CrmSuite.Modules.Orders.Application.Dtos;
using MediatR;

namespace CrmSuite.Modules.Orders.Application.Queries;

public sealed class GetCustomerServicesQueryHandler
    : IRequestHandler<GetCustomerServicesQuery, Result<IReadOnlyList<CustomerServiceDto>>>
{
    private readonly ICustomerServiceRepository _services;
    private readonly IJalaliDateService _jalali;

    public GetCustomerServicesQueryHandler(
        ICustomerServiceRepository services,
        IJalaliDateService jalali)
    {
        _services = services;
        _jalali = jalali;
    }

    public async Task<Result<IReadOnlyList<CustomerServiceDto>>> Handle(
        GetCustomerServicesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _services.GetByCustomerAsync(request.CustomerId, cancellationToken);
        var result = items
            .Select(service => new CustomerServiceDto(
                service.Id,
                service.CustomerId,
                service.ServiceId,
                service.OrderId,
                service.ServiceName,
                service.MonthlyAmount,
                service.Status.ToString(),
                service.ActivatedAt,
                _jalali.ToJalali(service.ActivatedAt),
                service.NextInvoiceAt,
                service.NextInvoiceAt.HasValue
                    ? _jalali.ToJalali(service.NextInvoiceAt.Value)
                    : null,
                service.CommissionBase))
            .ToArray();

        return Result.Success<IReadOnlyList<CustomerServiceDto>>(result);
    }
}
