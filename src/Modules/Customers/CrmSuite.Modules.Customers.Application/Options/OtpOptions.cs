namespace CrmSuite.Modules.Customers.Application.Options;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int Length { get; set; } = 6;

    public int TtlSeconds { get; set; } = 120;

    public int MaxAttempts { get; set; } = 5;
}
