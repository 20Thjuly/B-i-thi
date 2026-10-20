namespace SalesAR.UseCases.PluginInterfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SalesAR.CoreBusiness.Models;

public class AuditLogFilterDto
{
    public string? TableName { get; set; }
    public string? Action { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int Offset => Math.Max(0, (PageIndex - 1) * Math.Max(1, PageSize));
}

public class PagedAuditLogsDto
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public List<AuditLog> Items { get; set; } = new();

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
}

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 50);
    Task<IEnumerable<AuditLog>> GetByRecordAsync(string tableName, int recordId);
    Task AddAsync(AuditLog log);
    Task<PagedAuditLogsDto> GetPagedLogsAsync(AuditLogFilterDto filter);
    Task<IEnumerable<string>> GetDistinctTablesAsync();
    Task<IEnumerable<string>> GetDistinctActionsAsync();
}
