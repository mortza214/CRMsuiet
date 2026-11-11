using CrmSuite.Modules.Customers.Domain.Enums;

namespace CrmSuite.Modules.Customers.Api.Contracts;

public sealed class CreateCustomerRequest
{
    public CustomerType Type { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? CompanyName { get; init; }

    public string? EconomicCode { get; init; }

    public string NationalId { get; init; } = string.Empty;

    public string Mobile { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public Guid? OwnerUserId { get; init; }

    public Guid? ParentCustomerId { get; init; }

    public bool IsReseller { get; init; }

    public Guid? GroupId { get; init; }

    public CustomerOwnerType OwnerType { get; init; } = CustomerOwnerType.CurrentUser;

    public List<Guid>? TagIds { get; init; }
}

public sealed class UpdateCustomerRequest
{
    public CustomerType Type { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? CompanyName { get; init; }

    public string? EconomicCode { get; init; }

    public string NationalId { get; init; } = string.Empty;

    public string Mobile { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public Guid? OwnerUserId { get; init; }

    public Guid? ParentCustomerId { get; init; }

    public bool IsReseller { get; init; }

    public Guid? GroupId { get; init; }

    public CustomerOwnerType OwnerType { get; init; } = CustomerOwnerType.CurrentUser;

    public List<Guid>? TagIds { get; init; }
}

public sealed class SetResellerRequest
{
    public bool IsReseller { get; init; }

    public Guid? ParentCustomerId { get; init; }
}

public sealed class CreateCustomerGroupRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class UpdateCustomerGroupRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class CreateCustomerTagRequest
{
    public string Name { get; init; } = string.Empty;

    public string Color { get; init; } = "#1677ff";
}

public sealed class UpdateCustomerTagRequest
{
    public string Name { get; init; } = string.Empty;

    public string Color { get; init; } = "#1677ff";
}

public sealed class UpdateMyProfileRequest
{
    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }
}

public sealed class RequestOtpRequest
{
    public string Mobile { get; init; } = string.Empty;

    public string? CorrelationId { get; init; }
}

public sealed class VerifyOtpRequest
{
    public string Mobile { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
