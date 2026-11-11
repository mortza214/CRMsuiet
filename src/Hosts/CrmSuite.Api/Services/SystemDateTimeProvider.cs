using CrmSuite.BuildingBlocks.Application.Abstractions;

namespace CrmSuite.Api.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
