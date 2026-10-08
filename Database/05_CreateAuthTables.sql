-- ============================================================================
-- SCRIPT 05: CREATE AUTHENTICATION & AUTHORIZATION TABLES (ROLES & USERS)
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- ============================================================================

USE SalesARDB;
GO

-- 1. BẢNG VAI TRÒ (Roles)
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(50) NOT NULL,
        Description NVARCHAR(200) NULL,
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_Roles_Name UNIQUE (Name)
    );
END
GO

-- 2. BẢNG NGƯỜI DÙNG (Users)
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) NOT NULL,
        Username NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(256) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NULL,
        RoleId INT NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
    );
END
GO

-- 3. SEED VAI TRÒ (3 Role chuẩn theo yêu cầu Đợt 4)
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = 'ADMIN')
    INSERT INTO dbo.Roles (Name, Description) VALUES ('ADMIN', N'Quản trị viên toàn quyền hệ thống');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = 'SALES')
    INSERT INTO dbo.Roles (Name, Description) VALUES ('SALES', N'Nhân viên Kinh doanh (Khách hàng, Sản phẩm, Hóa đơn)');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = 'ACCOUNTANT')
    INSERT INTO dbo.Roles (Name, Description) VALUES ('ACCOUNTANT', N'Kế toán Công nợ (Thu tiền FIFO, Báo cáo tuổi nợ, Xuất báo cáo)');
GO

-- 4. SEED TÀI KHOẢN (MẬT KHẨU ĐÃ ĐƯỢC HASH BẰNG PBKDF2-SHA256, KHÔNG LƯU PLAIN TEXT)
DECLARE @AdminRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'ADMIN');
DECLARE @SalesRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'SALES');
DECLARE @AccRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'ACCOUNTANT');

-- User: admin (Mật khẩu: Admin@123)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, RoleId, IsActive)
    VALUES ('admin', 'UpvUwVGKu2LO4DSt0ADetQ==:eRTULFhEAeS6LW+AVUgsUYwfY095wT71EYDWPBveuds=', N'Quản trị viên Hệ thống', 'admin@salesar.vn', @AdminRoleId, 1);
END

-- User: sales (Mật khẩu: Sales@123)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'sales')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, RoleId, IsActive)
    VALUES ('sales', '3o2jrbF/PgOQNMFl/V6fAQ==:ukibHxYAQsiEhxFMcxfG9wfPybxnViFGCJ8etAmLvHQ=', N'Nguyễn Văn Kinh Doanh', 'sales@salesar.vn', @SalesRoleId, 1);
END

-- User: accountant (Mật khẩu: Acc@123)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'accountant')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, RoleId, IsActive)
    VALUES ('accountant', 'J1I1sAuiI8vw5NmpkKc2PQ==:wlv5OtKMYs/pSaHODTTtNjIUR+3mlD0RfRLYgfFTJEU=', N'Trần Thị Kế Toán', 'accountant@salesar.vn', @AccRoleId, 1);
END
GO

PRINT N'Authentication & Authorization tables created and seeded successfully!';
GO
