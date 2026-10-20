namespace SalesAR.UnitTests;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SalesAR.Web.Models;
using SalesAR.Web.Services;
using Xunit;

public class VietQrPaymentTests
{
    private class MockWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "SalesAR.Web";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
    }

    [Fact]
    public void GenerateVietQrUrl_WithPositiveAmount_IncludesCorrectParameters()
    {
        // Arrange
        var env = new MockWebHostEnvironment();
        var service = new BankSettingsService(env, NullLogger<BankSettingsService>.Instance);
        var settings = new BankSettings
        {
            BankId = "MB",
            AccountNumber = "0987654321",
            AccountName = "NGUYEN VAN A",
            Template = "compact2"
        };
        decimal amount = 2500000m;
        string paymentNumber = "PT-20261020-001";
        string customerCode = "KH001";

        // Act
        string url = service.GenerateVietQrUrl(settings, amount, paymentNumber, customerCode);

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("https://img.vietqr.io/image/MB-0987654321-compact2.png", url);
        Assert.Contains("amount=2500000", url);
        Assert.Contains("accountName=NGUYEN%20VAN%20A", url);
        Assert.Contains("addInfo=", url);
    }

    [Fact]
    public void GenerateVietQrUrl_SanitizesVietnameseDescription()
    {
        // Arrange
        var env = new MockWebHostEnvironment();
        var service = new BankSettingsService(env, NullLogger<BankSettingsService>.Instance);
        var settings = new BankSettings
        {
            BankId = "VCB",
            AccountNumber = "0071000888999",
            AccountName = "CONG TY SALESAR",
            Template = "compact2"
        };
        decimal amount = 1000000m;
        string paymentNumber = "PT-01";
        string customerNameWithAccents = "Nguyễn Văn Ánh";

        // Act
        string url = service.GenerateVietQrUrl(settings, amount, paymentNumber, customerNameWithAccents);

        // Assert
        Assert.NotNull(url);
        Assert.Contains("amount=1000000", url);
        // Kiểm tra URL không chứa ký tự có dấu thô (đã được sanitize và escape)
        Assert.DoesNotContain("ễ", url);
        Assert.DoesNotContain("Á", url);
    }

    [Fact]
    public void SupportedBanks_ContainsTopVietnameseBanks()
    {
        // Assert
        var banks = BankSettings.SupportedBanks;
        Assert.NotEmpty(banks);
        Assert.Contains(banks, b => b.BankId == "MB");
        Assert.Contains(banks, b => b.BankId == "VCB");
        Assert.Contains(banks, b => b.BankId == "TCB");
        Assert.Contains(banks, b => b.BankId == "BIDV");
        Assert.Contains(banks, b => b.BankId == "ACB");
    }

    [Fact]
    public void PaymentFormModel_DefaultsToBankTransfer()
    {
        // Arrange & Act
        var model = new PaymentFormModel();

        // Assert
        Assert.Equal("BankTransfer", model.PaymentMethod);
        Assert.StartsWith("PT-", model.PaymentNumber);
        Assert.True(model.PaymentDate <= DateTime.Today.AddDays(1));
    }

    [Fact]
    public async Task BankSettingsService_SaveAndGetSettings_WorksCorrectly()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var env = new MockWebHostEnvironment { ContentRootPath = tempDir };
            var service = new BankSettingsService(env, NullLogger<BankSettingsService>.Instance);

            var newSettings = new BankSettings
            {
                BankId = "TCB",
                BankName = "Techcombank",
                AccountNumber = "190333444555",
                AccountName = "TRAN THI B",
                Template = "compact"
            };

            // Act
            await service.SaveSettingsAsync(newSettings);
            var loadedSettings = await service.GetSettingsAsync();

            // Assert
            Assert.Equal("TCB", loadedSettings.BankId);
            Assert.Equal("190333444555", loadedSettings.AccountNumber);
            Assert.Equal("TRAN THI B", loadedSettings.AccountName);
            Assert.Equal("compact", loadedSettings.Template);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
