namespace CrmSuite.Modules.Foundation.Application.Dtos;

public sealed record HealthDto(
    string Status,
    DateTime ServerTimeUtc,
    string ServerTimeJalali,
    string Environment,
    string Version);
