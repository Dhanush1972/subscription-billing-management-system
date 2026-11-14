using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;

namespace SubscriptionBilling.Infrastructure.Services;

public class PdfInvoiceService : IPdfInvoiceGenerator
{
    static PdfInvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(Invoice invoice, User user, SubscriptionPlan? plan)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => ComposeHeader(header, invoice));
                page.Content().Element(content => ComposeContent(content, invoice, user, plan));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Invoice invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("SUBSCRIPTION BILLING").FontSize(22).SemiBold().FontColor(Colors.Blue.Darken2);
                col.Item().Text("Enterprise Subscription & Cloud Services").FontSize(9).FontColor(Colors.Grey.Medium);
                col.Item().Text("100 Tech Blvd, Suite 400, San Francisco, CA").FontSize(8).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(180).Column(col =>
            {
                col.Item().AlignRight().Text("INVOICE").FontSize(24).ExtraBold().FontColor(Colors.Grey.Darken4);
                col.Item().AlignRight().Text($"#{invoice.InvoiceNumber}").FontSize(11).SemiBold();
                col.Item().AlignRight().Text($"Date: {invoice.InvoiceDateUtc:yyyy-MM-dd}").FontSize(9);
                col.Item().AlignRight().Text($"Due: {invoice.DueDateUtc:yyyy-MM-dd}").FontSize(9);
            });
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice, User user, SubscriptionPlan? plan)
    {
        container.PaddingVertical(25).Column(col =>
        {
            // Billed To & Status
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("BILLED TO:").FontSize(9).SemiBold().FontColor(Colors.Grey.Medium);
                    c.Item().Text($"{user.FirstName} {user.LastName}").FontSize(11).Bold();
                    c.Item().Text(user.Email).FontSize(9);
                    if (!string.IsNullOrEmpty(user.StripeCustomerId))
                    {
                        c.Item().Text($"Customer ID: {user.StripeCustomerId}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(140).Column(c =>
                {
                    c.Item().AlignRight().Text("PAYMENT STATUS:").FontSize(9).SemiBold().FontColor(Colors.Grey.Medium);
                    var statusColor = invoice.Status == Domain.Enums.InvoiceStatus.Paid
                        ? Colors.Green.Darken2
                        : Colors.Orange.Darken2;
                    c.Item().AlignRight().Text(invoice.Status.ToString().ToUpperInvariant())
                        .FontSize(14).ExtraBold().FontColor(statusColor);
                    c.Item().AlignRight().Text($"Period: {invoice.BillingPeriod}").FontSize(8);
                });
            });

            col.Item().PaddingTop(25);

            // Table of items
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(Block).Text("#").SemiBold();
                    header.Cell().Element(Block).Text("Description").SemiBold();
                    header.Cell().Element(Block).AlignRight().Text("Qty").SemiBold();
                    header.Cell().Element(Block).AlignRight().Text("Unit Price").SemiBold();
                    header.Cell().Element(Block).AlignRight().Text("Amount").SemiBold();

                    static IContainer Block(IContainer container) =>
                        container.Background(Colors.Grey.Lighten3).Padding(6);
                });

                // Row
                var planName = plan?.Name ?? "Active Subscription Plan";
                var interval = plan?.BillingInterval.ToString() ?? "Monthly";

                table.Cell().Element(RowBlock).Text("1");
                table.Cell().Element(RowBlock).Column(c =>
                {
                    c.Item().Text($"{planName} ({interval})").Bold();
                    c.Item().Text($"Subscription Period: {invoice.BillingPeriod}").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                table.Cell().Element(RowBlock).AlignRight().Text("1");
                table.Cell().Element(RowBlock).AlignRight().Text($"${invoice.Subtotal:F2}");
                table.Cell().Element(RowBlock).AlignRight().Text($"${invoice.Subtotal:F2}");

                static IContainer RowBlock(IContainer container) =>
                    container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6);
            });

            col.Item().PaddingTop(15);

            // Totals
            col.Item().Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(200).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Subtotal:");
                        r.RelativeItem().AlignRight().Text($"${invoice.Subtotal:F2}");
                    });

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Tax (0.00%):");
                        r.RelativeItem().AlignRight().Text($"${invoice.Tax:F2}");
                    });

                    c.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Medium).Row(r =>
                    {
                        r.RelativeItem().Text("Total Due:").FontSize(12).Bold();
                        r.RelativeItem().AlignRight().Text($"${invoice.Total:F2} {invoice.Currency.ToUpper()}").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    });
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text("Thank you for your business! For billing queries, contact billing@subscriptionbilling.com")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(100).AlignRight().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }
}
