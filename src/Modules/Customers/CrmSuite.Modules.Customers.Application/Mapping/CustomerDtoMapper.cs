using CrmSuite.Modules.Customers.Application.Dtos;
using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Application.Abstractions;

namespace CrmSuite.Modules.Customers.Application.Mapping;

public sealed class CustomerDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public CustomerDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public CustomerDto ToCustomerDto(Customer customer)
    {
        var createdAt = AsUtc(customer.CreatedAt);
        var updatedAt = customer.UpdatedAt.HasValue ? AsUtc(customer.UpdatedAt.Value) : (DateTime?)null;

        return new CustomerDto(
            customer.Id,
            customer.Type.ToString(),
            customer.FirstName,
            customer.LastName,
            customer.CompanyName,
            customer.DisplayName,
            customer.EconomicCode,
            customer.NationalId,
            customer.Mobile,
            customer.Phone,
            customer.Email,
            customer.Address,
            customer.OwnerUserId,
            customer.ParentCustomerId,
            customer.IsReseller,
            customer.Group is null ? null : ToGroupDto(customer.Group),
            customer.TagMaps
                .Where(map => map.Tag is not null)
                .Select(map => new CustomerTagSummaryDto(map.Tag.Id, map.Tag.Name, map.Tag.Color))
                .OrderBy(tag => tag.Name)
                .ToArray(),
            customer.UserId,
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            updatedAt,
            updatedAt.HasValue ? _jalaliDate.ToJalali(updatedAt.Value) : null,
            customer.OwnerType.ToString(),
            customer.OwnerUserId,
            customer.PreferredNotificationChannel.ToString());
    }

    public CustomerGroupDto ToGroupDto(CustomerGroup group)
    {
        var createdAt = AsUtc(group.CreatedAt);
        return new CustomerGroupDto(
            group.Id,
            group.Name,
            group.Description,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    public CustomerTagDto ToTagDto(CustomerTag tag)
    {
        var createdAt = AsUtc(tag.CreatedAt);
        return new CustomerTagDto(
            tag.Id,
            tag.Name,
            tag.Color,
            createdAt,
            _jalaliDate.ToJalali(createdAt));
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
