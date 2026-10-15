using SalesAR.CoreBusiness.Models;

namespace SalesAR.UseCases.PluginInterfaces;

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 50);
    Task<IEnumerable<AuditLog>> GetByRecordAsync(string tableName, int recordId);
    Task AddAsync(AuditLog log);
}
