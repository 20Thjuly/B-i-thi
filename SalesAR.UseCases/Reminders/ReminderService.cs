namespace SalesAR.UseCases.Reminders;

using System.Text;
using SalesAR.CoreBusiness.Models;
using SalesAR.UseCases.Models;
using SalesAR.UseCases.PluginInterfaces;
using SalesAR.UseCases.Reminders.Models;

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _reminderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IEmailSender _emailSender;

    public ReminderService(
        IReminderRepository reminderRepository,
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository,
        IEmailSender emailSender)
    {
        _reminderRepository = reminderRepository;
        _customerRepository = customerRepository;
        _invoiceRepository = invoiceRepository;
        _emailSender = emailSender;
    }

    public async Task<IEnumerable<DebtReminderCandidateDto>> IdentifyReminderCandidatesAsync(DateTime asOfDate)
    {
        var customers = (await _customerRepository.GetAllAsync()).Where(c => c.IsActive).ToList();
        var rules = (await _reminderRepository.GetActiveRulesAsync()).OrderBy(r => r.DaysOffset).ToList();
        var candidates = new List<DebtReminderCandidateDto>();

        foreach (var customer in customers)
        {
            var unpaidInvoices = (await _invoiceRepository.GetOutstandingInvoicesByCustomerAsync(customer.Id))
                .Where(i => i.RemainingAmount > 0)
                .ToList();

            if (!unpaidInvoices.Any())
                continue;

            // Phân nhóm hóa đơn theo số ngày quá hạn / sắp đến hạn
            int maxDaysOverdue = int.MinValue;
            int minDaysUntilDue = int.MaxValue;

            foreach (var inv in unpaidInvoices)
            {
                int daysUntilDue = (inv.DueDate.Date - asOfDate.Date).Days;
                int daysOverdue = (asOfDate.Date - inv.DueDate.Date).Days;

                if (daysOverdue > maxDaysOverdue) maxDaysOverdue = daysOverdue;
                if (daysUntilDue < minDaysUntilDue) minDaysUntilDue = daysUntilDue;
            }

            // Tìm quy tắc nhắc nợ phù hợp nhất
            ReminderRule? matchedRule = null;
            if (maxDaysOverdue > 30)
                matchedRule = rules.FirstOrDefault(r => r.TriggerType == "Overdue" && r.DaysOffset > 30) ?? rules.LastOrDefault();
            else if (maxDaysOverdue >= 8)
                matchedRule = rules.FirstOrDefault(r => r.TriggerType == "Overdue" && r.DaysOffset >= 8 && r.DaysOffset <= 30);
            else if (maxDaysOverdue >= 1)
                matchedRule = rules.FirstOrDefault(r => r.TriggerType == "Overdue" && r.DaysOffset >= 1 && r.DaysOffset < 8);
            else if (minDaysUntilDue <= 3 && minDaysUntilDue >= 0)
                matchedRule = rules.FirstOrDefault(r => r.TriggerType == "UpcomingDue");

            if (matchedRule == null)
                continue; // Chưa đến mốc kích hoạt nhắc nợ

            string idempotencyKey = $"REM-CUST-{customer.Id}-RULE-{matchedRule.Id}-{asOfDate:yyyyMMdd}";
            var existingLog = await _reminderRepository.GetByIdempotencyKeyAsync(idempotencyKey);

            var candidate = new DebtReminderCandidateDto
            {
                CustomerId = customer.Id,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                RecipientEmail = !string.IsNullOrWhiteSpace(customer.Email) ? customer.Email : $"{customer.Code.ToLower()}@doitac.vn",
                IsInDispute = customer.IsInDispute,
                DisputeReason = customer.DisputeReason,
                SpecialAgreementNotes = customer.SpecialAgreementNotes,
                ReminderRuleId = matchedRule.Id,
                RuleName = matchedRule.Name,
                TriggerType = matchedRule.TriggerType,
                Tone = matchedRule.Tone,
                AutoSendApproved = matchedRule.AutoSendApproved,
                Invoices = unpaidInvoices,
                TotalDebtAmount = unpaidInvoices.Sum(i => i.RemainingAmount),
                MaxDaysOverdue = maxDaysOverdue,
                MinDaysUntilDue = minDaysUntilDue,
                SuggestedIdempotencyKey = idempotencyKey,
                AlreadySentToday = existingLog != null && (existingLog.Status == "Sent" || existingLog.Status == "Queued"),
                LastLogStatus = existingLog?.Status
            };

            candidates.Add(candidate);
        }

        return candidates;
    }

    public async Task<ReminderDraftDto> GenerateReminderDraftAsync(int customerId, List<int> invoiceIds, int? ruleId = null, DateTime? asOfDate = null)
    {
        var refDate = asOfDate ?? DateTime.Today;
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new InvalidOperationException($"Không tìm thấy khách hàng ID {customerId}.");

        var allUnpaid = await _invoiceRepository.GetOutstandingInvoicesByCustomerAsync(customerId);
        var selectedInvoices = allUnpaid.Where(i => invoiceIds == null || !invoiceIds.Any() || invoiceIds.Contains(i.Id)).ToList();

        if (!selectedInvoices.Any())
            selectedInvoices = allUnpaid.ToList();

        var rules = (await _reminderRepository.GetActiveRulesAsync()).ToList();
        ReminderRule? rule = null;
        if (ruleId.HasValue)
            rule = rules.FirstOrDefault(r => r.Id == ruleId.Value);

        if (rule == null)
        {
            int maxDaysOverdue = selectedInvoices.Any() 
                ? selectedInvoices.Max(i => (refDate.Date - i.DueDate.Date).Days)
                : 0;

            if (maxDaysOverdue > 30) rule = rules.FirstOrDefault(r => r.DaysOffset > 30);
            else if (maxDaysOverdue >= 8) rule = rules.FirstOrDefault(r => r.DaysOffset >= 8);
            else if (maxDaysOverdue >= 1) rule = rules.FirstOrDefault(r => r.DaysOffset >= 1);
            else rule = rules.FirstOrDefault(r => r.TriggerType == "UpcomingDue") ?? rules.FirstOrDefault();
        }

        string email = !string.IsNullOrWhiteSpace(customer.Email) ? customer.Email : $"{customer.Code.ToLower()}@doitac.vn";
        decimal totalAmount = selectedInvoices.Sum(i => i.RemainingAmount);
        int matchedRuleId = rule?.Id ?? 1;
        string idempotencyKey = $"REM-CUST-{customer.Id}-RULE-{matchedRuleId}-{refDate:yyyyMMdd}";

        // Định dạng Subject & Body
        string subject = rule?.TemplateSubject.Replace("{CustomerName}", customer.Name) 
            ?? $"[THÔNG BÁO CÔNG NỢ] Đối soát công nợ - {customer.Name}";

        var sb = new StringBuilder();
        if (rule != null && !string.IsNullOrWhiteSpace(rule.TemplateBody))
        {
            string bodyBase = rule.TemplateBody
                .Replace("{CustomerName}", customer.Name)
                .Replace("{TotalAmount}", totalAmount.ToString("N0"));
            sb.AppendLine(bodyBase);
        }
        else
        {
            sb.AppendLine($"Kính gửi: Quý khách hàng {customer.Name},");
            sb.AppendLine();
            sb.AppendLine($"Chúng tôi xin gửi thông báo chi tiết về các hóa đơn cần thanh toán tính đến ngày {refDate:dd/MM/yyyy}:");
        }

        sb.AppendLine();
        sb.AppendLine("DANH SÁCH HÓA ĐƠN ĐỐI SOÁT CHI TIẾT:");
        sb.AppendLine("--------------------------------------------------------------------------------");
        int index = 1;
        foreach (var inv in selectedInvoices)
        {
            int daysOverdue = (refDate.Date - inv.DueDate.Date).Days;
            string overdueLabel = daysOverdue > 0 
                ? $"(Quá hạn {daysOverdue} ngày)" 
                : $"(Đến hạn trong {-daysOverdue} ngày - Hạn: {inv.DueDate:dd/MM/yyyy})";

            sb.AppendLine($"{index++}. Hóa đơn: {inv.InvoiceNumber} | Ngày lập: {inv.InvoiceDate:dd/MM/yyyy} | Còn nợ: {inv.RemainingAmount:N0} đ {overdueLabel}");
        }
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine($"TỔNG CỘNG CẦN THANH TOÁN: {totalAmount:N0} VNĐ");
        sb.AppendLine();
        sb.AppendLine("Quý khách vui lòng chuyển khoản theo thông tin tài khoản trên hợp đồng và phản hồi lại email này khi hoàn tất.");
        sb.AppendLine("Trân trọng cảm ơn!");

        return new ReminderDraftDto
        {
            CustomerId = customer.Id,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            RecipientEmail = email,
            ReminderRuleId = rule?.Id,
            RuleName = rule?.Name ?? "Quy tắc chung",
            Tone = rule?.Tone ?? "Polite",
            Subject = subject,
            Body = sb.ToString(),
            InvoiceIds = selectedInvoices.Select(i => i.Id).ToList(),
            TotalDebtAmount = totalAmount,
            AutoSendApproved = rule?.AutoSendApproved ?? false,
            IdempotencyKey = idempotencyKey
        };
    }

    public async Task<ReminderLog> QueueOrSendReminderAsync(ReminderDraftDto draft, string? confirmedBy = null, bool forceSend = false, CancellationToken cancellationToken = default)
    {
        if (draft == null) throw new ArgumentNullException(nameof(draft));

        var customer = await _customerRepository.GetByIdAsync(draft.CustomerId)
            ?? throw new InvalidOperationException($"Không tìm thấy khách hàng ID {draft.CustomerId}.");

        // 1. KIỂM TRA TRANH CHẤP / THỎA THUẬN ĐẶC BIỆT (Dispute Suspension Rule)
        if (customer.IsInDispute)
        {
            var disputeLog = new ReminderLog
            {
                IdempotencyKey = draft.IdempotencyKey,
                CustomerId = draft.CustomerId,
                CustomerName = draft.CustomerName,
                ReminderRuleId = draft.ReminderRuleId,
                RuleName = draft.RuleName,
                RecipientEmail = draft.RecipientEmail,
                Subject = draft.Subject,
                Body = draft.Body,
                Status = "PausedDispute",
                LastError = $"Tạm dừng nhắc nợ do tranh chấp: {customer.DisputeReason ?? "Có tranh chấp hàng hóa / dịch vụ đang giải quyết"}",
                TotalDebtAmount = draft.TotalDebtAmount,
                InvoiceIdsCsv = string.Join(",", draft.InvoiceIds),
                ConfirmedBy = confirmedBy,
                CreatedAt = DateTime.UtcNow
            };

            int logId = await _reminderRepository.SaveReminderLogAsync(disputeLog);
            disputeLog.Id = logId;
            return disputeLog;
        }

        // 2. CHỐNG GỬI TRÙNG BẰNG IDEMPOTENCY KEY (Anti-Duplicate / Idempotency Rule)
        var existingLog = await _reminderRepository.GetByIdempotencyKeyAsync(draft.IdempotencyKey);
        if (existingLog != null)
        {
            // Nếu đã gửi thành công hoặc đang chờ gửi, TUYỆT ĐỐI KHÔNG GỬI LẠI
            if (existingLog.Status == "Sent" || existingLog.Status == "Queued" || existingLog.Status == "PausedDispute")
            {
                return existingLog;
            }
        }

        // 3. KIỂM SOÁT PHÊ DUYỆT THEO CHÍNH SÁCH (Policy Approval Rule)
        // Nếu quy tắc không cho phép tự động gửi và chưa có người xác nhận duyệt
        if (!draft.AutoSendApproved && string.IsNullOrWhiteSpace(confirmedBy) && !forceSend)
        {
            var pendingLog = new ReminderLog
            {
                IdempotencyKey = draft.IdempotencyKey,
                CustomerId = draft.CustomerId,
                CustomerName = draft.CustomerName,
                ReminderRuleId = draft.ReminderRuleId,
                RuleName = draft.RuleName,
                RecipientEmail = draft.RecipientEmail,
                Subject = draft.Subject,
                Body = draft.Body,
                Status = "PendingApproval",
                TotalDebtAmount = draft.TotalDebtAmount,
                InvoiceIdsCsv = string.Join(",", draft.InvoiceIds),
                CreatedAt = DateTime.UtcNow
            };

            int logId = await _reminderRepository.SaveReminderLogAsync(pendingLog);
            pendingLog.Id = logId;
            return pendingLog;
        }

        // 4. TIẾN HÀNH GỬI EMAIL QUA PROVIDER
        var reminderLog = existingLog ?? new ReminderLog
        {
            IdempotencyKey = draft.IdempotencyKey,
            CustomerId = draft.CustomerId,
            CustomerName = draft.CustomerName,
            ReminderRuleId = draft.ReminderRuleId,
            RuleName = draft.RuleName,
            RecipientEmail = draft.RecipientEmail,
            Subject = draft.Subject,
            Body = draft.Body,
            Status = "Queued",
            TotalDebtAmount = draft.TotalDebtAmount,
            InvoiceIdsCsv = string.Join(",", draft.InvoiceIds),
            ConfirmedBy = confirmedBy ?? (draft.AutoSendApproved ? "SYSTEM_AUTO_POLICY" : "MANUAL"),
            ConfirmedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        if (reminderLog.Id == 0)
        {
            int logId = await _reminderRepository.SaveReminderLogAsync(reminderLog);
            reminderLog.Id = logId;
        }

        // Gọi Email Provider
        var sendResult = await _emailSender.SendEmailAsync(draft.RecipientEmail, draft.Subject, draft.Body, cancellationToken);

        if (sendResult.Success)
        {
            reminderLog.Status = "Sent";
            reminderLog.SentAt = DateTime.UtcNow;
            reminderLog.LastError = null;
            await _reminderRepository.UpdateReminderLogStatusAsync(reminderLog.Id, "Sent", reminderLog.RetryCount, null, reminderLog.SentAt, reminderLog.ConfirmedBy);
        }
        else
        {
            reminderLog.RetryCount++;
            reminderLog.LastError = sendResult.ErrorMessage;

            if (sendResult.IsTransientError && reminderLog.RetryCount < reminderLog.MaxRetries)
            {
                reminderLog.Status = "RetryPending";
            }
            else
            {
                reminderLog.Status = "Failed";
            }

            await _reminderRepository.UpdateReminderLogStatusAsync(reminderLog.Id, reminderLog.Status, reminderLog.RetryCount, reminderLog.LastError);
        }

        return reminderLog;
    }

    public async Task<int> ProcessPendingQueueAsync(CancellationToken cancellationToken = default)
    {
        var pendingLogs = (await _reminderRepository.GetPendingRetryLogsAsync(maxRetries: 3)).ToList();
        int processedCount = 0;

        foreach (var log in pendingLogs)
        {
            var customer = await _customerRepository.GetByIdAsync(log.CustomerId);
            if (customer != null && customer.IsInDispute)
            {
                await _reminderRepository.UpdateReminderLogStatusAsync(log.Id, "PausedDispute", log.RetryCount, "Tạm dừng retry do đối tác phát sinh tranh chấp");
                continue;
            }

            var sendResult = await _emailSender.SendEmailAsync(log.RecipientEmail, log.Subject, log.Body, cancellationToken);
            if (sendResult.Success)
            {
                await _reminderRepository.UpdateReminderLogStatusAsync(log.Id, "Sent", log.RetryCount, null, DateTime.UtcNow, log.ConfirmedBy);
                processedCount++;
            }
            else
            {
                int newRetry = log.RetryCount + 1;
                string newStatus = (sendResult.IsTransientError && newRetry < log.MaxRetries) ? "RetryPending" : "Failed";
                await _reminderRepository.UpdateReminderLogStatusAsync(log.Id, newStatus, newRetry, sendResult.ErrorMessage);
            }
        }

        return processedCount;
    }

    public async Task<int> ProcessAutomaticRemindersJobAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var candidates = (await IdentifyReminderCandidatesAsync(asOfDate)).ToList();
        int sentOrQueuedCount = 0;

        foreach (var candidate in candidates)
        {
            if (candidate.AlreadySentToday)
                continue; // Chống gửi lặp trong cùng ngày

            var draft = await GenerateReminderDraftAsync(candidate.CustomerId, candidate.Invoices.Select(i => i.Id).ToList(), candidate.ReminderRuleId, asOfDate);
            var resultLog = await QueueOrSendReminderAsync(draft, confirmedBy: null, forceSend: false, cancellationToken);

            if (resultLog.Status == "Sent" || resultLog.Status == "PendingApproval")
                sentOrQueuedCount++;
        }

        // Xử lý luôn các log đang chờ retry tạm thời
        await ProcessPendingQueueAsync(cancellationToken);

        return sentOrQueuedCount;
    }

    public async Task<bool> PauseReminderForDisputeAsync(int reminderLogId, string reason, string actor)
    {
        var log = await _reminderRepository.GetLogByIdAsync(reminderLogId);
        if (log == null) return false;

        await _reminderRepository.UpdateReminderLogStatusAsync(reminderLogId, "PausedDispute", log.RetryCount, $"Tạm dừng bởi {actor}: {reason}");
        await _reminderRepository.SetCustomerDisputeStatusAsync(log.CustomerId, true, reason);
        return true;
    }

    public async Task<bool> ResumeReminderFromDisputeAsync(int reminderLogId, string actor)
    {
        var log = await _reminderRepository.GetLogByIdAsync(reminderLogId);
        if (log == null) return false;

        await _reminderRepository.UpdateReminderLogStatusAsync(reminderLogId, "PendingApproval", log.RetryCount, $"Khôi phục bởi {actor} sau khi giải quyết tranh chấp");
        await _reminderRepository.SetCustomerDisputeStatusAsync(log.CustomerId, false, null);
        return true;
    }

    public Task<IEnumerable<ReminderLog>> GetReminderLogsAsync(int? customerId = null, string? status = null)
    {
        return _reminderRepository.GetReminderLogsAsync(customerId, status);
    }

    public Task<IEnumerable<ReminderRule>> GetReminderRulesAsync()
    {
        return _reminderRepository.GetActiveRulesAsync();
    }
}
