namespace SalesAR.UseCases.AI.Providers;

using System.Threading;
using System.Threading.Tasks;

public class AiPrompt
{
    public string SystemInstruction { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 1500;
}

public class AiResponse
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
    public bool IsFallback { get; set; }

    public static AiResponse Successful(string content, string provider, string model, long elapsedMs, bool isFallback = false)
    {
        return new AiResponse
        {
            Success = true,
            Content = content,
            ProviderName = provider,
            ModelName = model,
            ElapsedMilliseconds = elapsedMs,
            IsFallback = isFallback
        };
    }

    public static AiResponse Failed(string error, string provider, string model = "", bool isFallback = false)
    {
        return new AiResponse
        {
            Success = false,
            ErrorMessage = error,
            ProviderName = provider,
            ModelName = model,
            IsFallback = isFallback
        };
    }
}

public interface IAIProvider
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    Task<AiResponse> GenerateTextAsync(AiPrompt prompt, CancellationToken cancellationToken = default);
}
