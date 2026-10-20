-- ============================================================================
-- SCRIPT 08: AUTOMATED DEBT REMINDERS WORKFLOW
-- Đề tài: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- Phiên bản migration: 08_AutomatedDebtReminders (Idempotent - Non-destructive)
-- ============================================================================

USE SalesARDB;
GO

PRINT N'>> Bắt đầu thực thi migration 08: Quy trình Nhắc nợ Tự động (Automated Debt Reminders)...';
GO

-- 1. BỔ SUNG CỘT CHO BẢNG dbo.Customers (Email, IsInDispute, DisputeReason, SpecialAgreementNotes)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Customers') AND name = 'Email')
BEGIN
    PRINT N'-- Đang thêm cột Email vào dbo.Customers...';
    ALTER TABLE dbo.Customers ADD Email NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Customers') AND name = 'IsInDispute')
BEGIN
    PRINT N'-- Đang thêm cột IsInDispute vào dbo.Customers...';
    ALTER TABLE dbo.Customers ADD IsInDispute BIT NOT NULL CONSTRAINT DF_Customers_IsInDispute DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Customers') AND name = 'DisputeReason')
BEGIN
    PRINT N'-- Đang thêm cột DisputeReason vào dbo.Customers...';
    ALTER TABLE dbo.Customers ADD DisputeReason NVARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Customers') AND name = 'SpecialAgreementNotes')
BEGIN
    PRINT N'-- Đang thêm cột SpecialAgreementNotes vào dbo.Customers...';
    ALTER TABLE dbo.Customers ADD SpecialAgreementNotes NVARCHAR(500) NULL;
END
GO

-- 2. BỔ SUNG CỘT CHO BẢNG dbo.Invoices (IsInDispute, DisputeReason)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Invoices') AND name = 'IsInDispute')
BEGIN
    PRINT N'-- Đang thêm cột IsInDispute vào dbo.Invoices...';
    ALTER TABLE dbo.Invoices ADD IsInDispute BIT NOT NULL CONSTRAINT DF_Invoices_IsInDispute DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Invoices') AND name = 'DisputeReason')
BEGIN
    PRINT N'-- Đang thêm cột DisputeReason vào dbo.Invoices...';
    ALTER TABLE dbo.Invoices ADD DisputeReason NVARCHAR(500) NULL;
END
GO

-- 3. CẬP NHẬT EMAIL VÀ TRẠNG THÁI MẪU CHO KHÁCH HÀNG
UPDATE dbo.Customers SET Email = 'anbinh@bachhoaanbinh.vn' WHERE Code = 'KH001' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'contact@mailinh.com.vn' WHERE Code = 'KH002' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'ketoan@hoanggiamart.vn' WHERE Code = 'KH003' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'sales@phucphat.com' WHERE Code = 'KH004' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'muahang@thanhhai.com' WHERE Code = 'KH005' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'kinhdoanh@minhkhang.vn' WHERE Code = 'KH006' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'ketoan@thinhvuong.com' WHERE Code = 'KH007' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'contact@truongthanh.vn' WHERE Code = 'KH008' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'vattubachhoa@ngoclan.vn' WHERE Code = 'KH009' AND Email IS NULL;
UPDATE dbo.Customers SET Email = 'admin@hoanglong.com.vn' WHERE Code = 'KH010' AND Email IS NULL;

-- Đặt 1 khách hàng đang có tranh chấp chất lượng hàng hóa để kiểm thử tính năng tạm hoãn
UPDATE dbo.Customers 
SET IsInDispute = 1, 
    DisputeReason = N'Đang có tranh chấp khiếu nại về 20 thùng nước ngọt lô giao ngày 15/09 - Tạm ngưng nhắc nợ theo phê duyệt của Ban Giám đốc'
WHERE Code = 'KH005' AND IsInDispute = 0;
GO

-- 4. BẢNG QUY TẮC NHẮC NỢ (ReminderRules)
IF OBJECT_ID(N'dbo.ReminderRules', N'U') IS NULL
BEGIN
    PRINT N'-- Đang tạo bảng dbo.ReminderRules...';
    CREATE TABLE dbo.ReminderRules (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        DaysOffset INT NOT NULL, -- Âm: trước hạn N ngày; Dương: quá hạn N ngày
        TriggerType NVARCHAR(50) NOT NULL, -- 'UpcomingDue', 'Overdue'
        Tone NVARCHAR(50) NOT NULL, -- 'Polite', 'Firm', 'Urgent', 'Legal'
        TemplateSubject NVARCHAR(250) NOT NULL,
        TemplateBody NVARCHAR(MAX) NOT NULL,
        AutoSendApproved BIT NOT NULL DEFAULT 0, -- 1: Tự động gửi; 0: Phải được duyệt thủ công
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT PK_ReminderRules PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT CK_ReminderRules_TriggerType CHECK (TriggerType IN ('UpcomingDue', 'Overdue')),
        CONSTRAINT CK_ReminderRules_Tone CHECK (Tone IN ('Polite', 'Firm', 'Urgent', 'Legal'))
    );
END
GO

-- 5. NẠP CÁC QUY TẮC NHẮC NỢ TIÊU CHUẨN (NẾU CHƯA CÓ)
IF NOT EXISTS (SELECT 1 FROM dbo.ReminderRules WHERE Name = N'Sắp đến hạn thanh toán (Trước 3 ngày)')
BEGIN
    PRINT N'-- Đang nạp các quy tắc nhắc nợ mặc định...';
    INSERT INTO dbo.ReminderRules (Name, DaysOffset, TriggerType, Tone, TemplateSubject, TemplateBody, AutoSendApproved, IsActive)
    VALUES 
    (
        N'Sắp đến hạn thanh toán (Trước 3 ngày)',
        -3,
        'UpcomingDue',
        'Polite',
        N'[THÔNG BÁO ĐẾN HẠN] Nhắc đối soát & thanh toán hóa đơn sắp đến hạn - {CustomerName}',
        N'Kính gửi Quý khách hàng {CustomerName},\n\nPhòng Tài chính - Kế toán xin gửi thông báo hóa đơn mua hàng của Quý khách sẽ đến hạn thanh toán trong 3 ngày tới. Quý khách vui lòng sắp xếp kế hoạch tài chính và chuyển khoản đúng hạn để duy trì hạn mức tín dụng tốt nhất.\n\nTổng số tiền sắp đến hạn: {TotalAmount} VNĐ.\n\nTrân trọng cảm ơn sự hợp tác của Quý khách!',
        1, -- Cho phép gửi tự động vì mang tính chất lịch sự trước hạn
        1
    ),
    (
        N'Quá hạn đợt 1 (1 - 7 ngày)',
        1,
        'Overdue',
        'Polite',
        N'[NHẮC NỢ LẦN 1] Hóa đơn quá hạn thanh toán - {CustomerName}',
        N'Kính gửi Quý khách hàng {CustomerName},\n\nTheo dữ liệu đối soát trên hệ thống, hiện tại Quý khách đang có các hóa đơn đã quá hạn thanh toán từ 1 đến 7 ngày với tổng số tiền {TotalAmount} VNĐ.\n\nKính đề nghị Quý khách kiểm tra lại biên lai thanh toán hoặc phản hồi lại nếu có bất kỳ vướng mắc nào về hóa đơn.\n\nTrân trọng!',
        1, -- Cho phép gửi tự động đợt 1
        1
    ),
    (
        N'Quá hạn đợt 2 (8 - 30 ngày)',
        8,
        'Overdue',
        'Firm',
        N'[NHẮC NỢ LẦN 2 - QUAN TRỌNG] Yêu cầu tất toán công nợ quá hạn - {CustomerName}',
        N'Kính gửi Ban Giám đốc và Bộ phận Kế toán - {CustomerName},\n\nCông ty chúng tôi xin lưu ý công nợ của Quý công ty hiện đã quá hạn trên 7 ngày với tổng số tiền {TotalAmount} VNĐ. Việc chậm trễ này đang ảnh hưởng trực tiếp đến chính sách công nợ và khả năng giao các đơn hàng tiếp theo.\n\nĐề nghị Quý khách ưu tiên thanh toán dứt điểm trong vòng 3 ngày làm việc tới.\n\nTrân trọng!',
        0, -- Cần kế toán hoặc trưởng phòng duyệt trước khi gửi
        1
    ),
    (
        N'Cảnh báo nợ khó đòi (> 30 ngày)',
        31,
        'Overdue',
        'Urgent',
        N'[CẢNH BÁO NỢ KHÓ ĐÒI & ĐÌNH CHỈ GIAO DỊCH] - {CustomerName}',
        N'Kính gửi Ban Lãnh đạo Công ty {CustomerName},\n\nChúng tôi thông báo các hóa đơn mua hàng với tổng số tiền {TotalAmount} VNĐ của Quý khách đã quá hạn thanh toán nghiêm trọng (trên 30 ngày). Theo quy chế quản trị tín dụng, hệ thống đã khóa hạn mức mua hàng trả chậm của Quý khách.\n\nNếu Quý công ty không thanh toán hoặc không có cam kết bằng văn bản trong vòng 48 giờ, chúng tôi sẽ chuyển hồ sơ sang bộ phận pháp chế để tiến hành các biện pháp thu hồi nợ theo luật định.\n\nTrân trọng!',
        0, -- Bắt buộc phải có phê duyệt của Ban Giám đốc
        1
    );
END
GO

-- 6. BẢNG NHẬT KÝ VÀ HÀNG ĐỢI NHẮC NỢ (ReminderLogs)
IF OBJECT_ID(N'dbo.ReminderLogs', N'U') IS NULL
BEGIN
    PRINT N'-- Đang tạo bảng dbo.ReminderLogs...';
    CREATE TABLE dbo.ReminderLogs (
        Id INT IDENTITY(1,1) NOT NULL,
        IdempotencyKey NVARCHAR(100) NOT NULL,
        CustomerId INT NOT NULL,
        ReminderRuleId INT NULL,
        RecipientEmail NVARCHAR(100) NOT NULL,
        Subject NVARCHAR(250) NOT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        RetryCount INT NOT NULL DEFAULT 0,
        MaxRetries INT NOT NULL DEFAULT 3,
        LastError NVARCHAR(MAX) NULL,
        SentAt DATETIME2(0) NULL,
        ConfirmedBy NVARCHAR(100) NULL,
        ConfirmedAt DATETIME2(0) NULL,
        InvoiceIdsCsv NVARCHAR(500) NULL,
        TotalDebtAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_ReminderLogs PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_ReminderLogs_IdempotencyKey UNIQUE (IdempotencyKey),
        CONSTRAINT FK_ReminderLogs_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ReminderLogs_Rules FOREIGN KEY (ReminderRuleId) REFERENCES dbo.ReminderRules(Id) ON DELETE SET NULL,
        CONSTRAINT CK_ReminderLogs_Status CHECK (Status IN ('Draft', 'PendingApproval', 'Queued', 'Sent', 'Failed', 'RetryPending', 'PausedDispute'))
    );

    CREATE NONCLUSTERED INDEX IX_ReminderLogs_Status ON dbo.ReminderLogs(Status ASC) INCLUDE (RetryCount, MaxRetries);
    CREATE NONCLUSTERED INDEX IX_ReminderLogs_Customer ON dbo.ReminderLogs(CustomerId ASC, CreatedAt DESC);
END
GO

PRINT N'>> Migration 08: Hoàn tất khởi tạo cơ sở dữ liệu Nhắc nợ tự động thành công!';
GO
