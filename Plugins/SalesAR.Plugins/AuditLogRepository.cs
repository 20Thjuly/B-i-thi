namespace SalesAR.Plugins.DataStore.SQL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<PagedAuditLogsDto> GetPagedLogsAsync(AuditLogFilterDto filter)
    {
        using var connection = _connectionFactory.CreateConnection();

        string whereClause = "WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(filter.TableName) && filter.TableName != "All")
            whereClause += " AND TableName = @TableName";

        if (!string.IsNullOrWhiteSpace(filter.Action) && filter.Action != "All")
            whereClause += " AND Action = @Action";

        if (!string.IsNullOrWhiteSpace(filter.ChangedBy))
            whereClause += " AND ChangedBy LIKE '%' + @ChangedBy + '%'";

        if (filter.FromDate.HasValue)
            whereClause += " AND ChangedAt >= @FromDate";

        if (filter.ToDate.HasValue)
            whereClause += " AND ChangedAt <= DATEADD(day, 1, @ToDate)";

        string countSql = $"SELECT COUNT(*) FROM dbo.AuditLogs {whereClause};";
        string selectSql = $@"
            SELECT Id, TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason
            FROM dbo.AuditLogs
            {whereClause}
            ORDER BY ChangedAt DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var parameters = new
        {
            TableName = filter.TableName,
            Action = filter.Action,
            ChangedBy = filter.ChangedBy,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Offset = filter.Offset,
            PageSize = filter.PageSize
        };

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<AuditLog>(selectSql, parameters)).ToList();

        return new PagedAuditLogsDto
        {
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize,
            Items = items
        };
    }

    public async Task<IEnumerable<string>> GetDistinctTablesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT DISTINCT TableName FROM dbo.AuditLogs ORDER BY TableName;";
        return await connection.QueryAsync<string>(sql);
    }

    public async Task<IEnumerable<string>> GetDistinctActionsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "SELECT DISTINCT Action FROM dbo.AuditLogs ORDER BY Action;";
        return await connection.QueryAsync<string>(sql);
    }
}
