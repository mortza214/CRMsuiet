namespace CrmSuite.Modules.Reports.Application.Dtos;

public sealed record FinancialSummaryDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal TotalSales,
    decimal TotalReceived,
    decimal TotalDebt,
    int InvoiceCount);

public sealed record FinancialTrendPointDto(
    string Month,
    string MonthJalali,
    decimal Sales,
    decimal Received);

public sealed record DebtorDto(
    Guid CustomerId,
    string CustomerName,
    string? Mobile,
    decimal OutstandingAmount,
    int InvoiceCount);

public sealed record BadDebtDto(
    Guid CustomerId,
    string CustomerName,
    string? Mobile,
    decimal OutstandingAmount,
    int OverdueInvoiceCount,
    int MaxOverdueDays);

public sealed record CommissionReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal PaidAmount,
    decimal UnpaidAmount,
    int PaidCount,
    int UnpaidCount);

public sealed record CustomerLedgerEntryDto(
    DateTime Date,
    string DateJalali,
    string EntryType,
    string? Reference,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance);

public sealed record SalesReportItemDto(
    string ServiceType,
    int OrderCount,
    decimal Quantity,
    decimal TotalAmount);
