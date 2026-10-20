namespace SalesAR.Web.Services;

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SalesAR.Web.Models;

public class BankSettingsService : IBankSettingsService
{
    private readonly string _settingsFilePath;
    private readonly ILogger<BankSettingsService> _logger;
    private BankSettings? _cachedSettings;
    private static readonly object _lock = new();

    public BankSettingsService(IWebHostEnvironment env, ILogger<BankSettingsService> logger)
    {
        _logger = logger;
        _settingsFilePath = Path.Combine(env.ContentRootPath, "bank_settings.json");
    }

    public async Task<BankSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<BankSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (settings != null)
                {
                    _cachedSettings = settings;
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đọc tệp cấu hình ngân hàng: {Path}", _settingsFilePath);
        }

        // Default fallback settings
        _cachedSettings = new BankSettings();
        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(BankSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            lock (_lock)
            {
                _cachedSettings = settings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu cấu hình ngân hàng: {Path}", _settingsFilePath);
            throw;
        }
    }

    public string GenerateVietQrUrl(BankSettings settings, decimal amount, string paymentNumber, string? customerCodeOrName = null)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        string bankId = string.IsNullOrWhiteSpace(settings.BankId) ? "MB" : settings.BankId.Trim();
        string accountNo = string.IsNullOrWhiteSpace(settings.AccountNumber) ? "0000000000" : settings.AccountNumber.Trim();
        string template = string.IsNullOrWhiteSpace(settings.Template) ? "compact2" : settings.Template.Trim();
        string accountName = string.IsNullOrWhiteSpace(settings.AccountName) ? "CHUYEN KHOAN" : settings.AccountName.Trim();

        // Chuẩn hóa nội dung chuyển khoản: Không dấu, tối đa 25 ký tự để đảm bảo tương thích mọi ngân hàng
        string rawDescription = $"TT {paymentNumber}";
        if (!string.IsNullOrWhiteSpace(customerCodeOrName))
        {
            rawDescription += $" {customerCodeOrName}";
        }
        string cleanDescription = SanitizeDescription(rawDescription);
        if (cleanDescription.Length > 25)
        {
            cleanDescription = cleanDescription.Substring(0, 25);
        }

        long longAmount = Math.Max(0, (long)amount);

        // Chuẩn API VietQR Quick Link
        // https://img.vietqr.io/image/<BANK_ID>-<ACCOUNT_NO>-<TEMPLATE>.png?amount=<AMOUNT>&addInfo=<DESCRIPTION>&accountName=<ACCOUNT_NAME>
        string encodedDesc = Uri.EscapeDataString(cleanDescription);
        string encodedName = Uri.EscapeDataString(accountName);

        if (longAmount > 0)
        {
            return $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={longAmount}&addInfo={encodedDesc}&accountName={encodedName}";
        }
        else
        {
            return $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?addInfo={encodedDesc}&accountName={encodedName}";
        }
    }

    private static string SanitizeDescription(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "THANH TOAN";

        // Chuyển tiếng Việt có dấu thành không dấu để chuẩn hóa theo tiêu chuẩn chuyển khoản ngân hàng
        string[] vietnameseSigns = new string[]
        {
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        };

        for (int i = 1; i < vietnameseSigns.Length; i++)
        {
            for (int j = 0; j < vietnameseSigns[i].Length; j++)
            {
                text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
        }

        // Chỉ giữ lại chữ cái, số, dấu cách và dấu gạch nối
        text = Regex.Replace(text, @"[^a-zA-Z0-9\s\-]", "");
        return text.Trim();
    }
}
