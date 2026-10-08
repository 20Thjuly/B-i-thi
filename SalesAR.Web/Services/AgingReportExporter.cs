namespace SalesAR.Web.Services;

using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalesAR.UseCases.AgingReport;
using SalesAR.UseCases.Models;

public class AgingReportExporter : IAgingReportExporter
{
    public AgingReportExporter()
    {
        // Kích hoạt giấy phép QuestPDF Community (miễn phí phi thương mại)
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] ExportToExcel(AgingReportSummary summary, DateTime asOfDate)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Báo cáo Tuổi nợ");

        // 1. Tiêu đề báo cáo
        ws.Cell(1, 1).Value = "HỆ THỐNG QUẢN LÝ BÁN HÀNG & CÔNG NỢ (SALES & AR)";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 11;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E40AF");

        ws.Cell(2, 1).Value = "BÁO CÁO PHÂN TÍCH TUỔI NỢ KHÁCH HÀNG (AGING REPORT)";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 15;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#111827");

        ws.Cell(3, 1).Value = $"Thời điểm chốt dữ liệu: {asOfDate:dd/MM/yyyy}";
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#4B5563");

        // 2. Dòng Header Bảng
        int headerRow = 5;
        string[] headers = 
        { 
            "STT", "Mã KH", "Tên Khách Hàng", "Hạn Mức Tín Dụng", 
            "Tổng Công Nợ", "0–30 Ngày", "31–60 Ngày", "61–90 Ngày", 
            ">90 Ngày", "Hạn Mức Còn Lại" 
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A"); // Navy Blue
            cell.Style.Alignment.Horizontal = (i >= 3) ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // 3. Dữ liệu các dòng
        int currentRow = headerRow + 1;
        int stt = 1;

        if (summary != null && summary.Items.Any())
        {
            foreach (var item in summary.Items)
            {
                ws.Cell(currentRow, 1).Value = stt++;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 2).Value = item.CustomerCode;
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 3).Value = item.CustomerName;

                // Các cột tiền tệ
                ws.Cell(currentRow, 4).Value = item.CreditLimit;
                ws.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0 \"đ\"";

                ws.Cell(currentRow, 5).Value = item.TotalDebt;
                ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0 \"đ\"";
                ws.Cell(currentRow, 5).Style.Font.Bold = true;
                if (item.TotalDebt > 0)
                    ws.Cell(currentRow, 5).Style.Font.FontColor = XLColor.Red;

                ws.Cell(currentRow, 6).Value = item.Bucket0To30;
                ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"đ\"";

                ws.Cell(currentRow, 7).Value = item.Bucket31To60;
                ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"đ\"";

                ws.Cell(currentRow, 8).Value = item.Bucket61To90;
                ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0 \"đ\"";

                ws.Cell(currentRow, 9).Value = item.BucketOver90;
                ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0 \"đ\"";
                if (item.BucketOver90 > 0)
                    ws.Cell(currentRow, 9).Style.Font.FontColor = XLColor.Red;

                ws.Cell(currentRow, 10).Value = item.AvailableCredit;
                ws.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0 \"đ\"";
                ws.Cell(currentRow, 10).Style.Font.FontColor = XLColor.FromHtml("#059669");

                for (int col = 1; col <= 10; col++)
                {
                    ws.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    ws.Cell(currentRow, col).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E5E7EB");
                }

                currentRow++;
            }

            // 4. Dòng TỔNG CỘNG
            ws.Cell(currentRow, 1).Value = "TỔNG CỘNG";
            ws.Range(currentRow, 1, currentRow, 3).Merge();
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 4).Value = summary.TotalCreditLimit;
            ws.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(currentRow, 4).Style.Font.Bold = true;

            ws.Cell(currentRow, 5).Value = summary.TotalDebt;
            ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(currentRow, 5).Style.Font.Bold = true;
            ws.Cell(currentRow, 5).Style.Font.FontColor = XLColor.Red;

            ws.Cell(currentRow, 6).Value = summary.Total0To30;
            ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(currentRow, 6).Style.Font.Bold = true;

            ws.Cell(currentRow, 7).Value = summary.Total31To60;
            ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(currentRow, 7).Style.Font.Bold = true;

            ws.Cell(currentRow, 8).Value = summary.Total61To90;
            ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(currentRow, 8).Style.Font.Bold = true;

            ws.Cell(currentRow, 9).Value = summary.TotalOver90;
            ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(currentRow, 9).Style.Font.Bold = true;
            ws.Cell(currentRow, 9).Style.Font.FontColor = XLColor.Red;

            ws.Cell(currentRow, 10).Value = summary.TotalCreditLimit - summary.TotalDebt;
            ws.Cell(currentRow, 10).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(currentRow, 10).Style.Font.Bold = true;

            var totalRange = ws.Range(currentRow, 1, currentRow, 10);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        }

        // Tự động căn chỉnh độ rộng cột
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf(AgingReportSummary summary, DateTime asOfDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8.5f));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("HỆ THỐNG QUẢN LÝ BÁN HÀNG & CÔNG NỢ (SALES & AR)").Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                            c.Item().Text("BÁO CÁO PHÂN TÍCH TUỔI NỢ (AGING REPORT)").Bold().FontSize(15).FontColor(Colors.Grey.Darken4);
                            c.Item().Text($"Thời điểm chốt số liệu: {asOfDate:dd/MM/yyyy}").Italic().FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingTop(5).PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content table
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(24);  // STT
                        columns.ConstantColumn(55);  // Mã KH
                        columns.RelativeColumn(3);   // Tên KH
                        columns.RelativeColumn(2.2f);// Hạn mức
                        columns.RelativeColumn(2.2f);// Tổng nợ
                        columns.RelativeColumn(2.0f);// 0-30
                        columns.RelativeColumn(2.0f);// 31-60
                        columns.RelativeColumn(2.0f);// 61-90
                        columns.RelativeColumn(2.0f);// >90
                        columns.RelativeColumn(2.2f);// Còn lại
                    });

                    // Header row
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignCenter().Text("STT").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignCenter().Text("Mã KH").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).Text("Khách Hàng").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignRight().Text("Hạn Mức").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignRight().Text("Tổng Nợ").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignRight().Text("0–30 ngày").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignRight().Text("31–60 ngày").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignRight().Text("61–90 ngày").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignRight().Text(">90 ngày").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken4).Padding(4).AlignRight().Text("Hạn Mức Còn").Bold().FontColor(Colors.White);
                    });

                    // Rows
                    if (summary != null && summary.Items.Any())
                    {
                        int idx = 1;
                        foreach (var item in summary.Items)
                        {
                            var bg = (idx % 2 == 0) ? Colors.Grey.Lighten5 : Colors.White;

                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(idx.ToString());
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(item.CustomerCode).Bold();
                            table.Cell().Background(bg).Padding(4).Text(item.CustomerName);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.CreditLimit:N0} đ");
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.TotalDebt:N0} đ").Bold().FontColor(item.TotalDebt > 0 ? Colors.Red.Darken2 : Colors.Grey.Darken2);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.Bucket0To30:N0} đ");
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.Bucket31To60:N0} đ");
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.Bucket61To90:N0} đ");
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.BucketOver90:N0} đ").FontColor(item.BucketOver90 > 0 ? Colors.Red.Darken2 : Colors.Grey.Darken2);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{item.AvailableCredit:N0} đ").FontColor(Colors.Green.Darken2);

                            idx++;
                        }

                        // Total Row
                        table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("TỔNG CỘNG").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{summary.TotalCreditLimit:N0} đ").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{summary.TotalDebt:N0} đ").Bold().FontColor(Colors.Red.Darken2);
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{summary.Total0To30:N0} đ").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{summary.Total31To60:N0} đ").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{summary.Total61To90:N0} đ").Bold();
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{summary.TotalOver90:N0} đ").Bold().FontColor(Colors.Red.Darken2);
                        table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{(summary.TotalCreditLimit - summary.TotalDebt):N0} đ").Bold().FontColor(Colors.Green.Darken2);
                    }
                });

                // Footer
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("Báo cáo xuất từ Hệ thống Quản lý Bán hàng & Công nợ Khách hàng").Italic().FontSize(8).FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
}
