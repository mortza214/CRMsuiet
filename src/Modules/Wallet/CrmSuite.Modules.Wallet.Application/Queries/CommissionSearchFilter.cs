using CrmSuite.Modules.Wallet.Domain.Enums;

namespace CrmSuite.Modules.Wallet.Application.Queries;

public sealed record CommissionSearchFilter(
    CommissionStatus? Status,
    Guid? EmployeeId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Page,
    int PageSize);
