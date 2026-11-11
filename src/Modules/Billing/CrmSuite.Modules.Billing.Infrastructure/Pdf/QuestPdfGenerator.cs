using System.Globalization;
using CrmSuite.Modules.Billing.Application.Abstractions;
using CrmSuite.Modules.Billing.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CrmSuite.Modules.Billing.Infrastructure.Pdf;

public sealed class QuestPdfGenerator : IPdfGenerator
{
    static QuestPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoice(InvoicePdfModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(style => style.FontFamily("Tahoma").FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("CrmSuite — فاکتور فروش").Bold().FontSize(18);
                    column.Item().Text($"شماره فاکتور: {model.InvoiceNumber}");
                    column.Item().Text($"مشتری: {model.CustomerName}");
                    column.Item().Text($"نوع: {model.Type} | وضعیت: {model.Status}");
                    column.Item().Text($"تاریخ صدور: {model.IssueDateUtc:yyyy-MM-dd} | سررسید: {model.DueDateUtc:yyyy-MM-dd}");
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("شرح");
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("تعداد");
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("قیمت واحد");
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("جمع");
                        });

                        foreach (var item in model.Items)
                        {
                            table.Cell().Padding(4).Text(item.ItemName);
                            table.Cell().Padding(4).Text(item.Quantity.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Padding(4).Text(item.UnitPrice.ToString("N0", CultureInfo.InvariantCulture));
                            table.Cell().Padding(4).Text(item.TotalPrice.ToString("N0", CultureInfo.InvariantCulture));
                        }
                    });

                    column.Item().PaddingTop(15).AlignRight().Column(summary =>
                    {
                        summary.Item().Text($"جمع کل: {model.SubTotal:N0} تومان");
                        summary.Item().Text($"تخفیف: {model.DiscountAmount:N0} تومان");
                        summary.Item().Text($"مالیات: {model.TaxAmount:N0} تومان");
                        summary.Item().Text($"بیمه: {model.InsuranceAmount:N0} تومان");
                        summary.Item().Text($"مبلغ نهایی: {model.TotalAmount:N0} تومان").Bold();
                        summary.Item().Text($"پرداخت‌شده: {model.PaidAmount:N0} تومان");
                    });
                });

                page.Footer().AlignCenter().Text("CrmSuite");
            });
        }).GeneratePdf();
    }

    public byte[] GenerateContract(ContractPdfModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(style => style.FontFamily("Tahoma").FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("CrmSuite — قرارداد").Bold().FontSize(18);
                    column.Item().Text($"شماره قرارداد: {model.ContractNumber}");
                    column.Item().Text($"مشتری: {model.CustomerName}");
                    column.Item().Text($"از {model.StartDateUtc:yyyy-MM-dd} تا {model.EndDateUtc:yyyy-MM-dd}");
                    column.Item().Text($"وضعیت: {model.Status}");
                });

                page.Content().PaddingVertical(20).Text(model.Content);
                page.Footer().AlignCenter().Text("CrmSuite");
            });
        }).GeneratePdf();
    }
}
