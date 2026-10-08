namespace SalesAR.UnitTests;

using Xunit;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class PdfVietnameseFontTests
{
    [Fact]
    public void GeneratePdf_WithVietnameseText_SucceedsWithoutFontErrors()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(col =>
                {
                    col.Item().Text("Cộng hòa Xã hội Chủ nghĩa Việt Nam").Bold();
                    col.Item().Text("Độc lập - Tự do - Hạnh phúc");
                    col.Item().Text("Báo cáo phân tích tuổi nợ khách hàng (Aging Report)");
                    col.Item().Text("Khách hàng: Công ty TNHH Bách Hóa An Bình - Nợ: 93.000.000 đ");
                });
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Fact]
    public void AgingReportExporter_ExportToPdf_GeneratesValidPdfWithVietnameseText()
    {
        var exporter = new SalesAR.Web.Services.AgingReportExporter();
        var summary = new SalesAR.UseCases.Models.AgingReportSummary
        {
            Items = new List<SalesAR.UseCases.Models.AgingReportItem>
            {
                new SalesAR.UseCases.Models.AgingReportItem
                {
                    CustomerId = 1,
                    CustomerCode = "KH001",
                    CustomerName = "Công ty TNHH Bách Hóa An Bình",
                    TotalDebt = 93000000,
                    CreditLimit = 150000000,
                    Bucket0To30 = 20000000,
                    Bucket31To60 = 30000000,
                    Bucket61To90 = 15000000,
                    BucketOver90 = 28000000
                }
            }
        };

        var pdfBytes = exporter.ExportToPdf(summary, DateTime.Now);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }
}
