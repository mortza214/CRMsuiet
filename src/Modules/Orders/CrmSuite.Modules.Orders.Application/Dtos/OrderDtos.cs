namespace CrmSuite.Modules.Orders.Application.Dtos;

public sealed record OrderItemDto(
    Guid Id,
    Guid? ServiceId,
    Guid? DataCenterItemId,
    Guid? AddonId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string BillingCycle,
    DateTime CreatedAt,
    string CreatedAtJalali,
    decimal UnitPriceSnapshot = 0,
    string? AddonsJson = null,
    string? AttributesJson = null,
    decimal? OverridePrice = null,
    string? OverrideReason = null,
    Guid? OverrideBy = null,
    decimal EffectivePrice = 0,
    decimal CommissionBase = 0);

public sealed record OrderStatusHistoryDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    Guid? ChangedByUserId,
    DateTime ChangedAt,
    string ChangedAtJalali,
    string? Note);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string? CustomerName,
    Guid SalesExpertId,
    DateTime OrderDate,
    string OrderDateJalali,
    string Status,
    string OrderType,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string? Notes,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory,
    DateTime CreatedAt,
    string CreatedAtJalali);

public sealed record CustomerServiceDto(
    Guid Id,
    Guid CustomerId,
    Guid ServiceId,
    Guid OrderId,
    string ServiceName,
    decimal MonthlyAmount,
    string Status,
    DateTime ActivatedAt,
    string ActivatedAtJalali,
    DateTime? NextInvoiceAt,
    string? NextInvoiceAtJalali,
    decimal CommissionBase);

public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string? CustomerName,
    Guid SalesExpertId,
    DateTime OrderDate,
    string OrderDateJalali,
    string Status,
    string OrderType,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    int ItemCount);
