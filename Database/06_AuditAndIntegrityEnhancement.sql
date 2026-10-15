-- ============================================================================
-- SCRIPT 06: AUDIT TRAIL, DEBT ADJUSTMENTS & INTEGRITY ENHANCEMENTS
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- Phiên bản migration: 06_AuditAndIntegrityEnhancement (Idempotent - Non-destructive)
-- ============================================================================

USE SalesARDB;
GO

PRINT N'>> Bắt đầu thực thi migration 06: Bổ sung Bảng Audit, Điều chỉnh công nợ và Tối ưu Index...';
GO

-- 1. BẢNG NHẬT KÝ KIỂM TOÁN / THAY ĐỔI DỮ LIỆU (AuditLogs)
-- Bảo đảm quy tắc nghiệp vụ: "Không xóa dữ liệu lịch sử âm thầm"
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    PRINT N'-- Đang tạo bảng dbo.AuditLogs...';
    CREATE TABLE dbo.AuditLogs (
        Id INT IDENTITY(1,1) NOT NULL,
        TableName NVARCHAR(100) NOT NULL,
        RecordId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL, -- 'INSERT', 'UPDATE', 'DELETE', 'STATUS_CHANGE', 'BALANCE_ADJUST'
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        ChangedBy NVARCHAR(100) NOT NULL DEFAULT N'System',
        ChangedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        Reason NVARCHAR(500) NULL,
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT CK_AuditLogs_Action CHECK (Action IN ('INSERT', 'UPDATE', 'DELETE', 'STATUS_CHANGE', 'BALANCE_ADJUST'))
    );
    PRINT N'-- Đã tạo bảng dbo.AuditLogs thành công.';
END
ELSE
BEGIN
    PRINT N'-- Bảng dbo.AuditLogs đã tồn tại. Bỏ qua tạo mới.';
END
GO

-- 2. BẢNG ĐIỀU CHỈNH CÔNG NỢ & LỊCH SỬ HẠN MỨC (DebtAdjustments)
-- Lưu lại toàn bộ lịch sử can thiệp công nợ (chiết khấu, giảm trừ, xóa nợ xấu, nâng/hạ hạn mức nợ)
IF OBJECT_ID(N'dbo.DebtAdjustments', N'U') IS NULL
BEGIN
    PRINT N'-- Đang tạo bảng dbo.DebtAdjustments...';
    CREATE TABLE dbo.DebtAdjustments (
        Id INT IDENTITY(1,1) NOT NULL,
        AdjustmentNumber NVARCHAR(50) NOT NULL,
        CustomerId INT NOT NULL,
        InvoiceId INT NULL,
        AdjustmentDate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        AdjustmentType NVARCHAR(50) NOT NULL, -- 'CreditLimitChange', 'BadDebtWriteOff', 'Discount', 'Return', 'ManualCorrection'
        Amount DECIMAL(18,2) NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        CreatedBy NVARCHAR(100) NOT NULL DEFAULT N'System',
        CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_DebtAdjustments PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_DebtAdjustments_Number UNIQUE (AdjustmentNumber),
        CONSTRAINT CK_DebtAdjustments_Amount CHECK (Amount <> 0),
        CONSTRAINT CK_DebtAdjustments_Type CHECK (AdjustmentType IN ('CreditLimitChange', 'BadDebtWriteOff', 'Discount', 'Return', 'ManualCorrection')),
        CONSTRAINT FK_DebtAdjustments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_DebtAdjustments_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(Id) ON DELETE NO ACTION
    );
    PRINT N'-- Đã tạo bảng dbo.DebtAdjustments thành công.';
END
ELSE
BEGIN
    PRINT N'-- Bảng dbo.DebtAdjustments đã tồn tại. Bỏ qua tạo mới.';
END
GO

-- 3. CHỈ MỤC TỐI ƯU HIỆU NĂNG CHO BÁO CÁO CÔNG NỢ & PHÂN BỔ FIFO (Composite Indexes)
PRINT N'-- Đang kiểm tra và bổ sung các chỉ mục tối ưu hóa truy vấn công nợ...';

-- Index hỗ trợ tìm hóa đơn chưa thanh toán/quá hạn theo từng khách hàng
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_Customer_Status' AND object_id = OBJECT_ID(N'dbo.Invoices'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Invoices_Customer_Status 
    ON dbo.Invoices (CustomerId, Status)
    INCLUDE (TotalAmount, DueDate, InvoiceNumber);
    PRINT N'-- Đã tạo IX_Invoices_Customer_Status.';
END
GO

-- Index hỗ trợ tính tổng phân bổ thanh toán theo từng hóa đơn nhanh chóng
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentAllocations_Invoice_Amount' AND object_id = OBJECT_ID(N'dbo.PaymentAllocations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PaymentAllocations_Invoice_Amount 
    ON dbo.PaymentAllocations (InvoiceId)
    INCLUDE (AllocatedAmount);
    PRINT N'-- Đã tạo IX_PaymentAllocations_Invoice_Amount.';
END
GO

-- Index tra cứu lịch sử điều chỉnh công nợ theo khách hàng
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DebtAdjustments_CustomerId' AND object_id = OBJECT_ID(N'dbo.DebtAdjustments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DebtAdjustments_CustomerId 
    ON dbo.DebtAdjustments (CustomerId, AdjustmentDate);
    PRINT N'-- Đã tạo IX_DebtAdjustments_CustomerId.';
END
GO

-- Index tra cứu nhật ký kiểm toán theo bảng và bản ghi
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_Table_Record' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Table_Record 
    ON dbo.AuditLogs (TableName, RecordId);
    PRINT N'-- Đã tạo IX_AuditLogs_Table_Record.';
END
GO

-- Index sắp xếp thời gian nhật ký kiểm toán
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_ChangedAt' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_ChangedAt 
    ON dbo.AuditLogs (ChangedAt DESC);
    PRINT N'-- Đã tạo IX_AuditLogs_ChangedAt.';
END
GO

-- 4. SEED DỮ LIỆU MẪU MÔI TRƯỜNG DEV/TEST (DEVELOPMENT / TEST ONLY)
-- Ghi nhận dữ liệu khởi tạo để kiểm thử tính năng điều chỉnh công nợ & nhật ký
IF NOT EXISTS (SELECT 1 FROM dbo.DebtAdjustments)
BEGIN
    PRINT N'-- Đang nạp seed data kiểm thử cho dbo.DebtAdjustments...';
    
    DECLARE @Cust1Id INT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Code = 'KH001');
    DECLARE @Cust2Id INT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Code = 'KH002');
    DECLARE @Cust3Id INT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Code = 'KH003');
    DECLARE @Inv1Id INT = (SELECT TOP 1 Id FROM dbo.Invoices WHERE CustomerId = @Cust1Id);
    DECLARE @Inv2Id INT = (SELECT TOP 1 Id FROM dbo.Invoices WHERE CustomerId = @Cust2Id);

    IF @Cust1Id IS NOT NULL
    BEGIN
        INSERT INTO dbo.DebtAdjustments (AdjustmentNumber, CustomerId, InvoiceId, AdjustmentDate, AdjustmentType, Amount, Reason, CreatedBy, CreatedAt)
        VALUES 
        (N'ADJ-2026-001', @Cust1Id, @Inv1Id, DATEADD(DAY, -15, SYSDATETIME()), N'Discount', 500000.00, N'Chiết khấu thương mại thanh toán đúng hạn quý 3/2026', N'accountant', DATEADD(DAY, -15, SYSDATETIME())),
        (N'ADJ-2026-002', @Cust1Id, NULL, DATEADD(DAY, -10, SYSDATETIME()), N'CreditLimitChange', 50000000.00, N'Nâng hạn mức tín dụng công nợ từ 100M lên 150M theo quyết định BGĐ', N'admin', DATEADD(DAY, -10, SYSDATETIME()));
    END

    IF @Cust2Id IS NOT NULL
    BEGIN
        INSERT INTO dbo.DebtAdjustments (AdjustmentNumber, CustomerId, InvoiceId, AdjustmentDate, AdjustmentType, Amount, Reason, CreatedBy, CreatedAt)
        VALUES 
        (N'ADJ-2026-003', @Cust2Id, @Inv2Id, DATEADD(DAY, -5, SYSDATETIME()), N'Return', 1200000.00, N'Giảm trừ công nợ do khách trả lại hàng lỗi vận chuyển', N'sales', DATEADD(DAY, -5, SYSDATETIME()));
    END

    IF @Cust3Id IS NOT NULL
    BEGIN
        INSERT INTO dbo.DebtAdjustments (AdjustmentNumber, CustomerId, InvoiceId, AdjustmentDate, AdjustmentType, Amount, Reason, CreatedBy, CreatedAt)
        VALUES 
        (N'ADJ-2026-004', @Cust3Id, NULL, DATEADD(DAY, -2, SYSDATETIME()), N'ManualCorrection', -250000.00, N'Điều chỉnh số dư đầu kỳ theo biên bản đối chiếu công nợ', N'accountant', DATEADD(DAY, -2, SYSDATETIME()));
    END

    PRINT N'-- Đã nạp 4 bản ghi DebtAdjustments mẫu cho Dev/Test.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AuditLogs)
BEGIN
    PRINT N'-- Đang nạp seed data kiểm thử cho dbo.AuditLogs...';
    
    INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, OldValues, NewValues, ChangedBy, ChangedAt, Reason)
    VALUES
    (N'Invoices', 1, N'STATUS_CHANGE', N'{"Status":"Unpaid"}', N'{"Status":"PartiallyPaid"}', N'accountant', DATEADD(DAY, -14, SYSDATETIME()), N'Hệ thống tự động cập nhật trạng thái khi phân bổ phiếu thu REC-2026-001'),
    (N'Customers', 1, N'UPDATE', N'{"CreditLimit":100000000}', N'{"CreditLimit":150000000}', N'admin', DATEADD(DAY, -10, SYSDATETIME()), N'Cập nhật hạn mức tín dụng theo phê duyệt BGĐ'),
    (N'Invoices', 2, N'STATUS_CHANGE', N'{"Status":"PartiallyPaid"}', N'{"Status":"Paid"}', N'accountant', DATEADD(DAY, -7, SYSDATETIME()), N'Hoàn tất thu đủ tiền hóa đơn'),
    (N'Payments', 1, N'INSERT', NULL, N'{"PaymentNumber":"REC-2026-001","Amount":25000000,"CustomerId":1}', N'accountant', DATEADD(DAY, -14, SYSDATETIME()), N'Lập phiếu thu chuyển khoản VCB');

    PRINT N'-- Đã nạp 4 bản ghi AuditLogs mẫu cho Dev/Test.';
END
GO

PRINT N'>> Hoàn tất migration 06: Bổ sung Bảng Audit, Điều chỉnh công nợ và Tối ưu Index thành công!';
GO
