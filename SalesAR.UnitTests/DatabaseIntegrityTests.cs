namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Models;

public class DatabaseIntegrityTests
{
    [Fact]
    public void DebtAdjustment_AllowsValidAdjustmentTypes()
    {
        var validTypes = new[] { "CreditLimitChange", "BadDebtWriteOff", "Discount", "Return", "ManualCorrection" };
        
        foreach (var type in validTypes)
        {
            var adj = new DebtAdjustment
            {
                AdjustmentNumber = $"ADJ-{type}",
                CustomerId = 1,
                AdjustmentType = type,
                Amount = 1000000.00m,
                Reason = "Hợp lệ"
            };

            Assert.Equal(type, adj.AdjustmentType);
            Assert.True(adj.Amount > 0);
        }
    }

    [Fact]
    public void Currency_PrecisionIsStrictDecimal_NoFloatingPointDrift()
    {
        // Kiểm tra tính toàn vẹn số học tiền tệ: Phải dùng decimal tuyệt đối, không trôi số phẩy động
        decimal price1 = 100000.33m;
        decimal price2 = 200000.67m;
        decimal total = price1 + price2;

        Assert.Equal(300001.00m, total);
        Assert.IsType<decimal>(total);
    }

    [Fact]
    public void AuditLog_InitializesWithRequiredIntegrityFields()
    {
        var log = new AuditLog
        {
            TableName = "Invoices",
            RecordId = 101,
            Action = "STATUS_CHANGE",
            OldValues = "{\"Status\":\"Unpaid\"}",
            NewValues = "{\"Status\":\"Paid\"}",
            ChangedBy = "accountant",
            Reason = "Thu tiền đủ hóa đơn"
        };

        Assert.Equal("Invoices", log.TableName);
        Assert.Equal(101, log.RecordId);
        Assert.Equal("STATUS_CHANGE", log.Action);
        Assert.Equal("accountant", log.ChangedBy);
        Assert.NotNull(log.OldValues);
        Assert.NotNull(log.NewValues);
    }
}
