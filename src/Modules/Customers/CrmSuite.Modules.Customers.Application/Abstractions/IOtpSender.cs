namespace CrmSuite.Modules.Customers.Application.Abstractions;

public interface IOtpSender
{
    Task SendAsync(string mobile, string code, CancellationToken cancellationToken);
}
