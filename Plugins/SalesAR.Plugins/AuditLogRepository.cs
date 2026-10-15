namespace SalesAR.Plugins.DataStore.SQL;

using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.PluginInterfaces;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AuditLogRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 50)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP (@Count) Id, TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason
            FROM dbo.AuditLogs
            ORDER BY ChangedAt DESC;";
        return await connection.QueryAsync<AuditLog>(sql, new { Count = count });
    }

    public async Task<IEnumerable<AuditLog>> GetByRecordAsync(string tableName, int recordId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason
            FROM dbo.AuditLogs
            WHERE TableName = @TableName AND RecordId = @RecordId
            ORDER BY ChangedAt DESC;";
        return await connection.QueryAsync<AuditLog>(sql, new { TableName = tableName, RecordId = recordId });
    }

    public async Task AddAsync(AuditLog log)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason)
            VALUES (@TableName, @RecordId, @Action, @OldValues, @NewValues, @ChangedBy, @ChangedAt, @Reason);";
        await connection.ExecuteAsync(sql, log);
    }
}
