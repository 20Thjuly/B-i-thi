namespace SalesAR.Web.Services;

using SalesAR.Web.Models;

public interface IBankSettingsService
{
    Task<BankSettings> GetSettingsAsync();
    Task SaveSettingsAsync(BankSettings settings);
    string GenerateVietQrUrl(BankSettings settings, decimal amount, string paymentNumber, string? customerCodeOrName = null);
}
