namespace SalesAR.Plugins.AI;

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesAR.UseCases.AI.Providers;

public class GeminiAiProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAiProvider> _logger;
    private readonly LocalHeuristicAiProvider _fallbackProvider;

    public string ProviderName => "GeminiAiProvider";

    public GeminiAiProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiProvider> logger,
        LocalHeuristicAiProvider fallbackProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fallbackProvider = fallbackProvider ?? throw new ArgumentNullException(nameof(fallbackProvider));

        // Thiết lập timeout tối đa 15 giây cho cuộc gọi AI
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public bool IsConfigured
    {
        get
        {
            string? key = GetApiKey();
            return !string.IsNullOrWhiteSpace(key) && 
                   !key.Equals("YOUR_GEMINI_API_KEY", StringComparison.OrdinalIgnoreCase) &&
                   !key.StartsWith("TODO", StringComparison.OrdinalIgnoreCase);
        }
    }

    public async Task<AiResponse> GenerateTextAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. Kiểm tra cấu hình API Key
        string? apiKey = GetApiKey();
        if (!IsConfigured || string.IsNullOrEmpty(apiKey))
        {
            _logger.LogInformation("[GeminiAiProvider] Chưa cấu hình API Key hoặc dùng khóa giả định. Chuyển sang Local Heuristic Fallback.");
            return await _fallbackProvider.GenerateTextAsync(prompt, cancellationToken);
        }

        string model = _configuration["AI:Model"] ?? "gemini-1.5-flash";
        string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        try
        {
            // 2. TỐI GIẢN & LÀM SẠCH DỮ LIỆU NHẠY CẢM (PII Masking & Data Minimization)
            string sanitizedUserPrompt = SanitizePrompt(prompt.UserPrompt);
            string sanitizedSystemInstruction = SanitizePrompt(prompt.SystemInstruction);

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"{sanitizedSystemInstruction}\n\n{sanitizedUserPrompt}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = prompt.Temperature,
                    maxOutputTokens = prompt.MaxTokens
                }
            };

            string jsonContent = JsonSerializer.Serialize(requestPayload);
            using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(endpoint, httpContent, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[GeminiAiProvider] API trả về mã lỗi {StatusCode}. Chuyển sang fallback an toàn. Lỗi: {Error}", 
                    response.StatusCode, MaskSensitiveDetails(errorBody));

                return await _fallbackProvider.GenerateTextAsync(prompt, cancellationToken);
            }

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            string? text = ExtractTextFromGeminiResponse(responseJson);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("[GeminiAiProvider] Phản hồi rỗng. Chuyển sang fallback.");
                return await _fallbackProvider.GenerateTextAsync(prompt, cancellationToken);
            }

            return AiResponse.Successful(text, ProviderName, model, sw.ElapsedMilliseconds, isFallback: false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning("[GeminiAiProvider] Cuộc gọi API bị quá thời gian (Timeout > 15s). Tự động kích hoạt Local Heuristic Fallback.");
            return await _fallbackProvider.GenerateTextAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[GeminiAiProvider] Lỗi ngoại lệ khi gọi AI: {Message}. Kích hoạt chế độ phòng vệ dữ liệu.", ex.Message);
            return await _fallbackProvider.GenerateTextAsync(prompt, cancellationToken);
        }
    }

    private string? GetApiKey()
    {
        return _configuration["AI:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
            ?? Environment.GetEnvironmentVariable("SALESAR_AI_KEY");
    }

    /// <summary>
    /// Giảm thiểu dữ liệu nhạy cảm (PII Minimization) trước khi gửi qua API bên ngoài
    /// </summary>
    public static string SanitizePrompt(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // 1. Che số điện thoại cá nhân (giữ 3 số cuối)
        string cleaned = Regex.Replace(input, @"\b(0|\+84)[0-9]{8,10}\b", "09***-xxx");

        // 2. Che địa chỉ email cá nhân (ví dụ: cty.nguyenvana@gmail.com -> ***@gmail.com)
        cleaned = Regex.Replace(cleaned, @"(?i)\b[a-zA-Z0-9._%+-]+@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})\b", "***@$1");

        // 3. Che số tài khoản ngân hàng chi tiết
        cleaned = Regex.Replace(cleaned, @"\b(STK|Tài khoản|Account)\s*[:=]?\s*([0-9]{8,16})\b", "$1: ***$2", RegexOptions.IgnoreCase);

        return cleaned;
    }

    private static string MaskSensitiveDetails(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return Regex.Replace(text, @"key=[a-zA-Z0-9_\-]+", "key=REDACTED");
    }

    private static string? ExtractTextFromGeminiResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString();
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi parse JSON để fallback
        }
        return null;
    }
}
