namespace SalesAR.UseCases.Dashboard.Models;

public class PagedInvoiceDetailDto
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
    public List<DashboardInvoiceItemDto> Items { get; set; } = new();
}
