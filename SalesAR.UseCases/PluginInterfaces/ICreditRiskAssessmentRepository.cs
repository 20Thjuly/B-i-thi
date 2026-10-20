namespace SalesAR.UseCases.PluginInterfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SalesAR.CoreBusiness.Models;

public interface ICreditRiskAssessmentRepository
{
    Task<int> SaveAssessmentAsync(CreditRiskAssessment assessment);
    Task<IEnumerable<CreditRiskAssessment>> GetHistoryByCustomerAsync(int customerId, int limit = 10);
    Task<CreditRiskAssessment?> GetByIdAsync(int id);
    Task UpdateReviewAsync(int id, string reviewedBy, string reviewNotes, DateTime reviewedAt);
}
