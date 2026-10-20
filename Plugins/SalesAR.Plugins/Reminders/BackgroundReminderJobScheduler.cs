namespace SalesAR.Plugins.Reminders;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SalesAR.UseCases.Reminders;

public class BackgroundReminderJobScheduler : BackgroundService, IBackgroundJobScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundReminderJobScheduler> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsJobRunning { get; private set; } = false;
    public DateTime? LastRunTime { get; private set; }
    public string? LastRunResult { get; private set; }

    public BackgroundReminderJobScheduler(
        IServiceProvider serviceProvider,
        ILogger<BackgroundReminderJobScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task TriggerDueRemindersJobAsync(CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("[ReminderScheduler] Job nhắc nợ đang chạy, bỏ qua yêu cầu kích hoạt đồng thời (Prevent Concurrent Job Execution).");
            return;
        }

        IsJobRunning = true;
        LastRunTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("[ReminderScheduler] Bắt đầu thực thi Job Quét và Nhắc nợ tự động lúc {Time}...", LastRunTime);
            using var scope = _serviceProvider.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

            int count = await reminderService.ProcessAutomaticRemindersJobAsync(DateTime.Today, cancellationToken);
            LastRunResult = $"Thành công: Đã xử lý {count} đối tác/hóa đơn cần nhắc nợ vào lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}.";
            _logger.LogInformation("[ReminderScheduler] Hoàn tất Job Nhắc nợ. Kết quả: {Result}", LastRunResult);
        }
        catch (Exception ex)
        {
            LastRunResult = $"Lỗi thực thi Job: {ex.Message}";
            _logger.LogError(ex, "[ReminderScheduler] Lỗi trong quá trình chạy Job Nhắc nợ tự động.");
        }
        finally
        {
            IsJobRunning = false;
            _semaphore.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chạy kiểm tra định kỳ mỗi 6 giờ hoặc khi được kích hoạt thủ công
        _logger.LogInformation("[ReminderScheduler] Dịch vụ nền nhắc nợ công nợ (Background Reminder Service) đã khởi động.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Chờ 6 tiếng giữa các chu kỳ tự động
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                await TriggerDueRemindersJobAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReminderScheduler] Lỗi chu kỳ định kỳ.");
            }
        }
    }
}
