-- ============================================================================
-- SCRIPT 02: CREATE TABLES (3NF COMPLIANT)
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- ============================================================================

USE SalesARDB;
GO

-- 1. BẢNG KHÁCH HÀNG (Customers)
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
    DROP TABLE dbo.Customers;
GO

CREATE TABLE dbo.Customers (
    Id INT IDENTITY(1,1) NOT NULL,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Address NVARCHAR(500) NOT NULL,
    CreditLimit DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaymentTermDays INT NOT NULL DEFAULT 30,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Customers_Code UNIQUE (Code),
    CONSTRAINT CK_Customers_CreditLimit CHECK (CreditLimit >= 0),
    CONSTRAINT CK_Customers_PaymentTermDays CHECK (PaymentTermDays >= 0)
);
GO

-- 2. BẢNG SẢN PHẨM / HÀNG HÓA (Products)
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL
    DROP TABLE dbo.Products;
GO

CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) NOT NULL,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Unit NVARCHAR(50) NOT NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    StockQuantity INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Products_Code UNIQUE (Code),
    CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0)
);
GO

-- 3. BẢNG HÓA ĐƠN BÁN HÀNG (Invoices)
IF OBJECT_ID(N'dbo.Invoices', N'U') IS NOT NULL
    DROP TABLE dbo.Invoices;
GO

CREATE TABLE dbo.Invoices (
    Id INT IDENTITY(1,1) NOT NULL,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    CustomerId INT NOT NULL,
    InvoiceDate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    DueDate DATETIME2(0) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Unpaid',
    CONSTRAINT PK_Invoices PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE (InvoiceNumber),
    CONSTRAINT CK_Invoices_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Invoices_Status CHECK (Status IN ('Draft', 'Unpaid', 'PartiallyPaid', 'Paid', 'Cancelled'))
);
GO

-- 4. BẢNG CHI TIẾT HÓA ĐƠN (InvoiceLines)
IF OBJECT_ID(N'dbo.InvoiceLines', N'U') IS NOT NULL
    DROP TABLE dbo.InvoiceLines;
GO

CREATE TABLE dbo.InvoiceLines (
    Id INT IDENTITY(1,1) NOT NULL,
    InvoiceId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_InvoiceLines PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_InvoiceLines_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_InvoiceLines_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_InvoiceLines_Amount CHECK (Amount >= 0)
);
GO

-- 5. BẢNG PHIẾU THU TIỀN (Payments)
IF OBJECT_ID(N'dbo.Payments', N'U') IS NOT NULL
    DROP TABLE dbo.Payments;
GO

CREATE TABLE dbo.Payments (
    Id INT IDENTITY(1,1) NOT NULL,
    PaymentNumber NVARCHAR(50) NOT NULL,
    CustomerId INT NOT NULL,
    PaymentDate DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    Amount DECIMAL(18,2) NOT NULL,
    Note NVARCHAR(500) NULL,
    CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Payments_PaymentNumber UNIQUE (PaymentNumber),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0)
);
GO

-- 6. BẢNG PHÂN BỔ THANH TOÁN (PaymentAllocations)
IF OBJECT_ID(N'dbo.PaymentAllocations', N'U') IS NOT NULL
    DROP TABLE dbo.PaymentAllocations;
GO

CREATE TABLE dbo.PaymentAllocations (
    Id INT IDENTITY(1,1) NOT NULL,
    PaymentId INT NOT NULL,
    InvoiceId INT NOT NULL,
    AllocatedAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_PaymentAllocations PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_PaymentAllocations_AllocatedAmount CHECK (AllocatedAmount > 0)
);
GO

PRINT N'Tables created successfully with primary keys, unique constraints, and check constraints.';
GO
