namespace SalesAR.UseCases.AgingReport;

using SalesAR.UseCases.Models;

public interface IAgingReportExporter
{
    byte[] ExportToExcel(AgingReportSummary summary, DateTime asOfDate);
    byte[] ExportToPdf(AgingReportSummary summary, DateTime asOfDate);
}
