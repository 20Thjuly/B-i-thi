namespace SalesAR.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesAR.UseCases.Dashboard;
using SalesAR.UseCases.Dashboard.Models;
using SalesAR.UseCases.PluginInterfaces;
using Xunit;

public class ArDashboardAndForecastTests
{
    private class FakeArDashboardRepository : IArDashboardRepository
    {
        public ArDashboardFilterDto? LastReceivedFilter { get; private set; }
        public DateTime? LastAsOfDate { get; private set; }
        public int? LastCustomerId { get; private set; }

        public ArKpiSummaryDto KpiToReturn { get; set; } = new();
        public AgingBucketSummaryDto AgingToReturn { get; set; } = new();
        public List<TopDebtorCustomerDto> TopDebtorsToReturn { get; set; } = new();
        public List<MonthlyCashFlowTrendDto> MonthlyTrendsToReturn { get; set; } = new();
        public List<CashCollectionForecastDto> ForecastsToReturn { get; set; } = new();
        public PagedInvoiceDetailDto PagedInvoicesToReturn { get; set; } = new();

        public Task<ArKpiSummaryDto> GetKpiSummaryAsync(ArDashboardFilterDto filter)
        {
            LastReceivedFilter = filter;
            return Task.FromResult(KpiToReturn);
        }

        public Task<AgingBucketSummaryDto> GetAgingBucketsAsync(DateTime asOfDate, int? customerId = null)
        {
            LastAsOfDate = asOfDate;
            LastCustomerId = customerId;
            return Task.FromResult(AgingToReturn);
        }

        public Task<IEnumerable<TopDebtorCustomerDto>> GetTopDebtorsAsync(DateTime asOfDate, int topCount = 5)
        {
            return Task.FromResult<IEnumerable<TopDebtorCustomerDto>>(TopDebtorsToReturn);
        }

        public Task<IEnumerable<MonthlyCashFlowTrendDto>> GetMonthlyCashFlowTrendAsync(DateTime asOfDate, int monthsCount = 6)
        {
            return Task.FromResult<IEnumerable<MonthlyCashFlowTrendDto>>(MonthlyTrendsToReturn);
        }

        public Task<IEnumerable<CashCollectionForecastDto>> GetCashCollectionForecastAsync(DateTime asOfDate, int? customerId = null)
        {
            return Task.FromResult<IEnumerable<CashCollectionForecastDto>>(ForecastsToReturn);
        }

        public Task<PagedInvoiceDetailDto> GetPagedInvoicesAsync(ArDashboardFilterDto filter)
        {
            return Task.FromResult(PagedInvoicesToReturn);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ValidFilter_CalculatesDsoCorrectly()
    {
        // Arrange
        var fakeRepo = new FakeArDashboardRepository
        {
            KpiToReturn = new ArKpiSummaryDto
            {
                TotalArOutstanding = 60_000_000m,
                TotalCreditSalesInPeriod = 120_000_000m,
                TotalCashCollectedInPeriod = 90_000_000m,
                UnpaidInvoicesCount = 8,
                TotalOverdueAr = 20_000_000m
            }
        };

        var useCase = new ArDashboardUseCase(fakeRepo);
        var filter = new ArDashboardFilterDto
        {
            FromDate = new DateTime(2026, 10, 1),
            ToDate = new DateTime(2026, 10, 30), // 30 ngày (1 đến 30) -> 30 days
            AsOfDate = new DateTime(2026, 10, 30)
        };

        // Act
        var result = await useCase.ExecuteAsync(filter);

        // Assert: DSO = (60M / 120M) * 30 = 0.5 * 30 = 15.0 ngày
        Assert.NotNull(result);
        Assert.NotNull(result.Kpi.DsoDays);
        Assert.Equal(15.0, result.Kpi.DsoDays.Value);
        Assert.Contains("15", result.Kpi.DsoExplanation);
        Assert.Contains("Doanh nghiệp mất trung bình", result.Kpi.DsoExplanation);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroCreditSales_HandlesDsoWithoutDivisionByZero()
    {
        // Arrange
        var fakeRepo = new FakeArDashboardRepository
        {
            KpiToReturn = new ArKpiSummaryDto
            {
                TotalArOutstanding = 50_000_000m,
                TotalCreditSalesInPeriod = 0m,
                TotalCashCollectedInPeriod = 10_000_000m
            }
        };

        var useCase = new ArDashboardUseCase(fakeRepo);
        var filter = new ArDashboardFilterDto
        {
            FromDate = new DateTime(2026, 10, 1),
            ToDate = new DateTime(2026, 10, 31)
        };

        // Act
        var result = await useCase.ExecuteAsync(filter);

        // Assert
        Assert.Null(result.Kpi.DsoDays);
        Assert.Contains("Không có doanh thu xuất bán trong kỳ", result.Kpi.DsoExplanation);
    }

    [Fact]
    public async Task ExecuteAsync_ReversedDateRange_AutomaticallyNormalizesDates()
    {
        // Arrange: Người dùng chọn FromDate sau ToDate (nhập nhầm)
        var fakeRepo = new FakeArDashboardRepository
        {
            KpiToReturn = new ArKpiSummaryDto
            {
                TotalArOutstanding = 10_000_000m,
                TotalCreditSalesInPeriod = 20_000_000m
            }
        };

        var useCase = new ArDashboardUseCase(fakeRepo);
        var filter = new ArDashboardFilterDto
        {
            FromDate = new DateTime(2026, 10, 31),
            ToDate = new DateTime(2026, 10, 1)
        };

        // Act
        var result = await useCase.ExecuteAsync(filter);

        // Assert: FromDate và ToDate đã được tự động hoán vị
        Assert.True(result.Filter.FromDate <= result.Filter.ToDate);
        Assert.Equal(new DateTime(2026, 10, 1), result.Filter.FromDate);
        Assert.Equal(new DateTime(2026, 10, 31), result.Filter.ToDate);
        Assert.NotNull(result.Kpi.DsoDays);
    }

    [Fact]
    public async Task ExecuteAsync_TopDebtors_CalculatesDebtSharePercentageCorrectly()
    {
        // Arrange
        var fakeRepo = new FakeArDashboardRepository
        {
            KpiToReturn = new ArKpiSummaryDto
            {
                TotalArOutstanding = 100_000_000m
            },
            TopDebtorsToReturn = new List<TopDebtorCustomerDto>
            {
                new() { CustomerId = 1, CustomerName = "Công ty Hải Nam", TotalDebt = 50_000_000m },
                new() { CustomerId = 2, CustomerName = "Công ty Ánh Dương", TotalDebt = 30_000_000m },
                new() { CustomerId = 3, CustomerName = "Công ty Sao Mai", TotalDebt = 20_000_000m }
            }
        };

        var useCase = new ArDashboardUseCase(fakeRepo);

        // Act
        var result = await useCase.ExecuteAsync(new ArDashboardFilterDto());

        // Assert
        Assert.Equal(3, result.TopDebtors.Count);
        Assert.Equal(50.0m, result.TopDebtors[0].DebtSharePercentage);
        Assert.Equal(30.0m, result.TopDebtors[1].DebtSharePercentage);
        Assert.Equal(20.0m, result.TopDebtors[2].DebtSharePercentage);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroSystemDebt_TopDebtorsShareIsZero()
    {
        // Arrange
        var fakeRepo = new FakeArDashboardRepository
        {
            KpiToReturn = new ArKpiSummaryDto { TotalArOutstanding = 0m },
            TopDebtorsToReturn = new List<TopDebtorCustomerDto>
            {
                new() { CustomerId = 1, CustomerName = "A", TotalDebt = 0m }
            }
        };

        var useCase = new ArDashboardUseCase(fakeRepo);
        var result = await useCase.ExecuteAsync(new ArDashboardFilterDto());

        Assert.Equal(0m, result.TopDebtors[0].DebtSharePercentage);
    }

    [Fact]
    public void ArKpiSummaryDto_CalculatesOverdueRatiosAccurately()
    {
        // Arrange
        var kpi = new ArKpiSummaryDto
        {
            TotalArOutstanding = 250_000_000m,
            TotalOverdueAr = 75_000_000m,
            UnpaidInvoicesCount = 20,
            OverdueInvoicesCount = 6
        };

        // Act & Assert
        // Tỷ lệ giá trị: 75M / 250M = 30.00%
        Assert.Equal(30.00m, kpi.OverdueValueRatio);
        // Tỷ lệ số lượng: 6 / 20 = 30.00%
        Assert.Equal(30.00m, kpi.OverdueCountRatio);
    }

    [Fact]
    public void ArKpiSummaryDto_ZeroOutstanding_OverdueRatioIsZeroWithoutException()
    {
        var kpi = new ArKpiSummaryDto
        {
            TotalArOutstanding = 0m,
            TotalOverdueAr = 0m,
            UnpaidInvoicesCount = 0,
            OverdueInvoicesCount = 0
        };

        Assert.Equal(0m, kpi.OverdueValueRatio);
        Assert.Equal(0m, kpi.OverdueCountRatio);
    }

    [Fact]
    public void AgingBucketSummaryDto_CalculatesTotalsAndPercentagesCorrectly()
    {
        // Arrange
        var aging = new AgingBucketSummaryDto
        {
            CurrentAmount = 40_000_000m,
            CurrentCount = 4,
            Overdue1To30Amount = 30_000_000m,
            Overdue1To30Count = 3,
            Overdue31To60Amount = 15_000_000m,
            Overdue31To60Count = 2,
            Overdue61To90Amount = 10_000_000m,
            Overdue61To90Count = 1,
            OverdueOver90Amount = 5_000_000m,
            OverdueOver90Count = 1
        };

        // Act & Assert
        Assert.Equal(100_000_000m, aging.TotalAmount);
        Assert.Equal(11, aging.TotalCount);

        Assert.Equal(40.0m, aging.CurrentPercentage);
        Assert.Equal(30.0m, aging.Overdue1To30Percentage);
        Assert.Equal(15.0m, aging.Overdue31To60Percentage);
        Assert.Equal(10.0m, aging.Overdue61To90Percentage);
        Assert.Equal(5.0m, aging.OverdueOver90Percentage);
    }

    [Fact]
    public void CashCollectionForecastDto_CalculatesExpectedCashInflowWeightedProbability()
    {
        // Arrange
        var forecast7d = new CashCollectionForecastDto
        {
            PeriodKey = "NEXT_7_DAYS",
            PeriodLabel = "Sắp đến hạn 7 ngày tới",
            NominalDueAmount = 100_000_000m,
            CollectionProbability = 0.90m,
            ConfidenceLevel = "Cao"
        };

        var forecastOverdue = new CashCollectionForecastDto
        {
            PeriodKey = "OVERDUE_BAD_DEBT",
            PeriodLabel = "Quá hạn trên 30 ngày (Nợ rủi ro)",
            NominalDueAmount = 50_000_000m,
            CollectionProbability = 0.20m,
            ConfidenceLevel = "Thận trọng"
        };

        // Act & Assert
        Assert.Equal(90_000_000m, forecast7d.ExpectedCashInflow);
        Assert.Equal(10_000_000m, forecastOverdue.ExpectedCashInflow);
    }

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(3, 20, 40)]
    [InlineData(0, 15, 0)]  // Clamped to page 1
    [InlineData(-5, 10, 0)] // Negative page clamped
    public void ArDashboardFilterDto_OffsetCalculation_IsSafeAndCorrect(int pageNumber, int pageSize, int expectedOffset)
    {
        var filter = new ArDashboardFilterDto
        {
            PageIndex = pageNumber,
            PageSize = pageSize
        };

        Assert.Equal(expectedOffset, filter.Offset);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesCustomerAndAgingFiltersProperly()
    {
        // Arrange
        var fakeRepo = new FakeArDashboardRepository();
        var useCase = new ArDashboardUseCase(fakeRepo);

        var filter = new ArDashboardFilterDto
        {
            CustomerId = 5,
            AgingBucket = "OVERDUE_31_60",
            InvoiceStatus = "Unpaid",
            AsOfDate = new DateTime(2026, 10, 20)
        };

        // Act
        var result = await useCase.ExecuteAsync(filter);

        // Assert
        Assert.NotNull(fakeRepo.LastReceivedFilter);
        Assert.Equal(5, fakeRepo.LastReceivedFilter.CustomerId);
        Assert.Equal("OVERDUE_31_60", fakeRepo.LastReceivedFilter.AgingBucket);
        Assert.Equal("Unpaid", fakeRepo.LastReceivedFilter.InvoiceStatus);
        Assert.Equal(new DateTime(2026, 10, 20), fakeRepo.LastAsOfDate);
        Assert.Equal(5, fakeRepo.LastCustomerId);
    }
}
