using CrmSuite.Modules.Foundation.Domain.Entities;
using CrmSuite.Modules.Payments.Domain.Enums;

namespace CrmSuite.Modules.Payments.Domain.Entities;

public sealed class PaymentGatewayTransaction : BaseEntity
{
    private PaymentGatewayTransaction()
    {
    }

    public PaymentGatewayTransaction(
        Guid? transactionId,
        PaymentGatewayName gatewayName,
        string? token,
        string requestPayload,
        string? responsePayload,
        bool isSuccessful,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        TransactionId = transactionId;
        GatewayName = gatewayName;
        Token = token;
        RequestPayload = requestPayload;
        ResponsePayload = responsePayload;
        IsSuccessful = isSuccessful;
        MarkCreated(null, createdAtUtc);
    }

    public Guid? TransactionId { get; private set; }

    public PaymentGatewayName GatewayName { get; private set; }

    public string? Token { get; private set; }

    public string RequestPayload { get; private set; } = string.Empty;

    public string? ResponsePayload { get; private set; }

    public bool IsSuccessful { get; private set; }
}
