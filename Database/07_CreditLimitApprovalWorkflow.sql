-- ============================================================================
-- SCRIPT 07: CREDIT LIMIT APPROVAL WORKFLOW
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- Phiên bản migration: 07_CreditLimitApprovalWorkflow (Idempotent - Non-destructive)
-- ============================================================================

USE SalesARDB;
GO

PRINT N'>> Bắt đầu thực thi migration 07: Bổ sung Bảng Quản lý Phê duyệt Vượt hạn mức Tín dụng...';
GO

-- 1. BỔ SUNG VAI TRÒ CREDIT_MANAGER (NẾU CHƯA CÓ)
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = 'CREDIT_MANAGER')
BEGIN
    PRINT N'-- Đang thêm vai trò CREDIT_MANAGER...';
    INSERT INTO dbo.Roles (Name, Description)
    VALUES ('CREDIT_MANAGER', N'Trưởng phòng Quản lý Tín dụng & Công nợ (Phê duyệt vượt hạn mức)');
END
GO

-- 2. BỔ SUNG TÀI KHOẢN creditmanager (MẬT KHẨU: Credit@123 HASHED PBKDF2)
DECLARE @CreditManagerRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'CREDIT_MANAGER');
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'creditmanager')
BEGIN
    PRINT N'-- Đang thêm tài khoản creditmanager...';
    INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, RoleId, IsActive)
    VALUES ('creditmanager', 'Nuym4vxVLoKKVr3J3mpG1Q==:/lqKYD51goAbNqIIw0QmTAvW7Ct3RxJqslzPKpvlXJQ=', N'Trần Quản Lý Tín Dụng', 'creditmanager@salesar.vn', @CreditManagerRoleId, 1);
END
GO

-- 3. BẢNG YÊU CẦU DUYỆT VƯỢT HẠN MỨC (CreditLimitRequests)
IF OBJECT_ID(N'dbo.CreditLimitRequests', N'U') IS NULL
BEGIN
    PRINT N'-- Đang tạo bảng dbo.CreditLimitRequests...';
    CREATE TABLE dbo.CreditLimitRequests (
        Id INT IDENTITY(1,1) NOT NULL,
        RequestNumber NVARCHAR(50) NOT NULL,
        CustomerId INT NOT NULL,
        TransactionAmount DECIMAL(18,2) NOT NULL,
        CurrentDebtSnapshot DECIMAL(18,2) NOT NULL,
        CreditLimitSnapshot DECIMAL(18,2) NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        RequestedBy NVARCHAR(100) NOT NULL,
        RequestedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        ApprovedBy NVARCHAR(100) NULL,
        ApprovedAt DATETIME2(0) NULL,
        DecisionReason NVARCHAR(500) NULL,
        ExpiresAt DATETIME2(0) NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        UsedAt DATETIME2(0) NULL,
        UsedInvoiceId INT NULL,
        CONSTRAINT PK_CreditLimitRequests PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_CreditLimitRequests_Number UNIQUE (RequestNumber),
        CONSTRAINT CK_CreditLimitRequests_Amount CHECK (TransactionAmount > 0),
        CONSTRAINT CK_CreditLimitRequests_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'Cancelled', 'Expired')),
        CONSTRAINT FK_CreditLimitRequests_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CreditLimitRequests_Invoices FOREIGN KEY (UsedInvoiceId) REFERENCES dbo.Invoices(Id) ON DELETE NO ACTION
    );
    PRINT N'-- Đã tạo bảng dbo.CreditLimitRequests thành công.';
END
ELSE
BEGIN
    PRINT N'-- Bảng dbo.CreditLimitRequests đã tồn tại. Bỏ qua tạo mới.';
END
GO

-- 4. BẢNG LỊCH SỬ QUYẾT ĐỊNH PHÊ DUYỆT (CreditLimitApprovals)
-- Lưu lại quyết định và snapshot công nợ thực tế lúc ra quyết định (không tin snapshot cũ)
IF OBJECT_ID(N'dbo.CreditLimitApprovals', N'U') IS NULL
BEGIN
    PRINT N'-- Đang tạo bảng dbo.CreditLimitApprovals...';
    CREATE TABLE dbo.CreditLimitApprovals (
        Id INT IDENTITY(1,1) NOT NULL,
        RequestId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL, -- 'SUBMIT', 'APPROVE', 'REJECT', 'CANCEL', 'EXPIRE'
        Actor NVARCHAR(100) NOT NULL,
        ActionAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        Comment NVARCHAR(500) NULL,
        CurrentDebtAtDecision DECIMAL(18,2) NOT NULL,
        CreditLimitAtDecision DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_CreditLimitApprovals PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT CK_CreditLimitApprovals_Action CHECK (Action IN ('SUBMIT', 'APPROVE', 'REJECT', 'CANCEL', 'EXPIRE')),
        CONSTRAINT FK_CreditLimitApprovals_Requests FOREIGN KEY (RequestId) REFERENCES dbo.CreditLimitRequests(Id) ON DELETE CASCADE
    );
    PRINT N'-- Đã tạo bảng dbo.CreditLimitApprovals thành công.';
END
ELSE
BEGIN
    PRINT N'-- Bảng dbo.CreditLimitApprovals đã tồn tại. Bỏ qua tạo mới.';
END
GO

-- 5. CHỈ MỤC TỐI ƯU HIỆU NĂNG CHO TRA CỨU YÊU CẦU DUYỆT
PRINT N'-- Đang kiểm tra và tạo chỉ mục cho CreditLimitRequests và CreditLimitApprovals...';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CreditLimitRequests_CustomerId_Status' AND object_id = OBJECT_ID(N'dbo.CreditLimitRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CreditLimitRequests_CustomerId_Status 
    ON dbo.CreditLimitRequests (CustomerId, Status)
    INCLUDE (TransactionAmount, IsUsed, ExpiresAt);
    PRINT N'-- Đã tạo IX_CreditLimitRequests_CustomerId_Status.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CreditLimitRequests_Status_ExpiresAt' AND object_id = OBJECT_ID(N'dbo.CreditLimitRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CreditLimitRequests_Status_ExpiresAt 
    ON dbo.CreditLimitRequests (Status, ExpiresAt);
    PRINT N'-- Đã tạo IX_CreditLimitRequests_Status_ExpiresAt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CreditLimitApprovals_RequestId' AND object_id = OBJECT_ID(N'dbo.CreditLimitApprovals'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CreditLimitApprovals_RequestId 
    ON dbo.CreditLimitApprovals (RequestId);
    PRINT N'-- Đã tạo IX_CreditLimitApprovals_RequestId.';
END
GO

-- 6. SEED DỮ LIỆU KIỂM THỬ DEV/TEST (DEVELOPMENT / TEST ONLY)
IF NOT EXISTS (SELECT 1 FROM dbo.CreditLimitRequests)
BEGIN
    PRINT N'-- Đang nạp dữ liệu mẫu cho CreditLimitRequests và CreditLimitApprovals...';

    DECLARE @Cust1Id INT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Code = 'KH001');
    DECLARE @Cust2Id INT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Code = 'KH002');
    DECLARE @Cust3Id INT = (SELECT TOP 1 Id FROM dbo.Customers WHERE Code = 'KH003');

    -- Request 1: Đã duyệt cho KH001 (giá trị 60.000.000 đ)
    IF @Cust1Id IS NOT NULL
    BEGIN
        INSERT INTO dbo.CreditLimitRequests 
        (RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot, Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason, ExpiresAt, IsUsed)
        VALUES 
        (N'CLR-2026-0001', @Cust1Id, 60000000.00, 120000000.00, 150000000.00, N'Khách hàng ký hợp đồng dự án cung cấp thiết bị tin học đợt 2', N'Approved', N'sales', DATEADD(DAY, -3, SYSDATETIME()), N'creditmanager', DATEADD(DAY, -2, SYSDATETIME()), N'Chấp thuận do đối tác thanh toán uy tín và có bảo lãnh thanh toán', DATEADD(DAY, 5, SYSDATETIME()), 0);

        DECLARE @Req1Id INT = SCOPE_IDENTITY();
        INSERT INTO dbo.CreditLimitApprovals (RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision)
        VALUES 
        (@Req1Id, N'SUBMIT', N'sales', DATEADD(DAY, -3, SYSDATETIME()), N'Tạo yêu cầu duyệt vượt hạn mức', 120000000.00, 150000000.00),
        (@Req1Id, N'APPROVE', N'creditmanager', DATEADD(DAY, -2, SYSDATETIME()), N'Chấp thuận theo đề xuất dự án', 120000000.00, 150000000.00);
    END

    -- Request 2: Đang chờ duyệt (Pending) cho KH002 (giá trị 45.000.000 đ)
    IF @Cust2Id IS NOT NULL
    BEGIN
        INSERT INTO dbo.CreditLimitRequests 
        (RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot, Reason, Status, RequestedBy, RequestedAt, ExpiresAt, IsUsed)
        VALUES 
        (N'CLR-2026-0002', @Cust2Id, 45000000.00, 180000000.00, 200000000.00, N'Khách mở rộng showroom cần bổ sung hàng gấp', N'Pending', N'sales', DATEADD(HOUR, -4, SYSDATETIME()), DATEADD(DAY, 2, SYSDATETIME()), 0);

        DECLARE @Req2Id INT = SCOPE_IDENTITY();
        INSERT INTO dbo.CreditLimitApprovals (RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision)
        VALUES 
        (@Req2Id, N'SUBMIT', N'sales', DATEADD(HOUR, -4, SYSDATETIME()), N'Chờ Credit Manager phê duyệt', 180000000.00, 200000000.00);
    END

    -- Request 3: Bị từ chối (Rejected) cho KH003
    IF @Cust3Id IS NOT NULL
    BEGIN
        INSERT INTO dbo.CreditLimitRequests 
        (RequestNumber, CustomerId, TransactionAmount, CurrentDebtSnapshot, CreditLimitSnapshot, Reason, Status, RequestedBy, RequestedAt, ApprovedBy, ApprovedAt, DecisionReason, IsUsed)
        VALUES 
        (N'CLR-2026-0003', @Cust3Id, 80000000.00, 45000000.00, 50000000.00, N'Đơn hàng đột xuất số lượng lớn', N'Rejected', N'sales', DATEADD(DAY, -5, SYSDATETIME()), N'creditmanager', DATEADD(DAY, -4, SYSDATETIME()), N'Từ chối do khách hàng đang có nợ quá hạn trên 60 ngày chưa thanh toán', 0);

        DECLARE @Req3Id INT = SCOPE_IDENTITY();
        INSERT INTO dbo.CreditLimitApprovals (RequestId, Action, Actor, ActionAt, Comment, CurrentDebtAtDecision, CreditLimitAtDecision)
        VALUES 
        (@Req3Id, N'SUBMIT', N'sales', DATEADD(DAY, -5, SYSDATETIME()), N'Đệ trình xin duyệt', 45000000.00, 50000000.00),
        (@Req3Id, N'REJECT', N'creditmanager', DATEADD(DAY, -4, SYSDATETIME()), N'Từ chối vì nợ quá hạn', 45000000.00, 50000000.00);
    END

    PRINT N'-- Đã nạp 3 bản ghi CreditLimitRequests và lịch sử Approvals cho Dev/Test.';
END
GO

PRINT N'>> Hoàn tất migration 07: Bổ sung Bảng Quản lý Phê duyệt Vượt hạn mức Tín dụng thành công!';
GO
