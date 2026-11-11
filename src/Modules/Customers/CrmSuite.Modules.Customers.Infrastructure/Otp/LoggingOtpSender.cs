using CrmSuite.Modules.Customers.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CrmSuite.Modules.Customers.Infrastructure.Otp;

public sealed class LoggingOtpSender : IOtpSender
{
    private readonly ILogger<LoggingOtpSender> _logger;

    public LoggingOtpSender(ILogger<LoggingOtpSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string mobile, string code, CancellationToken cancellationToken)
    {
        // Development adapter. Replace with the SMS/Bale provider in the Notifications module.
        _logger.LogWarning("OTP for {Mobile}: {Code}", mobile, code);
        return Task.CompletedTask;
    }
}
