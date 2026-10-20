namespace SalesAR.Plugins.AI;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using SalesAR.CoreBusiness.Models;
using SalesAR.Plugins.DataStore.SQL;
using SalesAR.UseCases.PluginInterfaces;

public class CreditRiskAssessmentRepository : ICreditRiskAssessmentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CreditRiskAssessmentRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<int> SaveAssessmentAsync(CreditRiskAssessment assessment)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.CreditRiskAssessments (
                CustomerId, AssessedAt, ModelVersion, RiskScore, RiskLevel,
                RequiresHumanReview, ExplanationFactors, RecommendedAction,
                AssessedBy, HumanReviewedBy, HumanReviewedAt, HumanReviewNotes
            )
            OUTPUT INSERTED.Id
            VALUES (
                @CustomerId, @AssessedAt, @ModelVersion, @RiskScore, @RiskLevel,
                @RequiresHumanReview, @ExplanationFactors, @RecommendedAction,
                @AssessedBy, @HumanReviewedBy, @HumanReviewedAt, @HumanReviewNotes
            );";

        return await connection.ExecuteScalarAsync<int>(sql, assessment);
    }

    public async Task<IEnumerable<CreditRiskAssessment>> GetHistoryByCustomerAsync(int customerId, int limit = 10)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP (@Limit)
                cra.Id,
                cra.CustomerId,
                cra.AssessedAt,
                cra.ModelVersion,
                cra.RiskScore,
                cra.RiskLevel,
                cra.RequiresHumanReview,
                cra.ExplanationFactors,
                cra.RecommendedAction,
                cra.AssessedBy,
                cra.HumanReviewedBy,
                cra.HumanReviewedAt,
                cra.HumanReviewNotes,
                c.Code AS CustomerCode,
                c.Name AS CustomerName
            FROM dbo.CreditRiskAssessments cra
            INNER JOIN dbo.Customers c ON cra.CustomerId = c.Id
            WHERE cra.CustomerId = @CustomerId
            ORDER BY cra.AssessedAt DESC, cra.Id DESC;";

        return await connection.QueryAsync<CreditRiskAssessment>(sql, new { CustomerId = customerId, Limit = limit });
    }

    public async Task<CreditRiskAssessment?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                cra.Id,
                cra.CustomerId,
                cra.AssessedAt,
                cra.ModelVersion,
                cra.RiskScore,
                cra.RiskLevel,
                cra.RequiresHumanReview,
                cra.ExplanationFactors,
                cra.RecommendedAction,
                cra.AssessedBy,
                cra.HumanReviewedBy,
                cra.HumanReviewedAt,
                cra.HumanReviewNotes,
                c.Code AS CustomerCode,
                c.Name AS CustomerName
            FROM dbo.CreditRiskAssessments cra
            INNER JOIN dbo.Customers c ON cra.CustomerId = c.Id
            WHERE cra.Id = @Id;";

        return await connection.QuerySingleOrDefaultAsync<CreditRiskAssessment>(sql, new { Id = id });
    }

    public async Task UpdateReviewAsync(int id, string reviewedBy, string reviewNotes, DateTime reviewedAt)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.CreditRiskAssessments
            SET 
                HumanReviewedBy = @ReviewedBy,
                HumanReviewNotes = @ReviewNotes,
                HumanReviewedAt = @ReviewedAt,
                RequiresHumanReview = 0
            WHERE Id = @Id;";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            ReviewedBy = reviewedBy,
            ReviewNotes = reviewNotes,
            ReviewedAt = reviewedAt
        });
    }
}
