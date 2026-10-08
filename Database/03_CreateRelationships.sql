-- ============================================================================
-- SCRIPT 03: CREATE RELATIONSHIPS & INDEXES
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- ============================================================================

USE SalesARDB;
GO

-- 1. FOREIGN KEY: Customer 1-N Invoice
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'dbo.FK_Invoices_Customers'))
BEGIN
    ALTER TABLE dbo.Invoices
    ADD CONSTRAINT FK_Invoices_Customers 
    FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
    ON DELETE NO ACTION;
END
GO

-- 2. FOREIGN KEY: Invoice 1-N InvoiceLine
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'dbo.FK_InvoiceLines_Invoices'))
BEGIN
    ALTER TABLE dbo.InvoiceLines
    ADD CONSTRAINT FK_InvoiceLines_Invoices 
    FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(Id)
    ON DELETE CASCADE;
END
GO

-- 3. FOREIGN KEY: Product 1-N InvoiceLine
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'dbo.FK_InvoiceLines_Products'))
BEGIN
    ALTER TABLE dbo.InvoiceLines
    ADD CONSTRAINT FK_InvoiceLines_Products 
    FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
    ON DELETE NO ACTION;
END
GO

-- 4. FOREIGN KEY: Customer 1-N Payment
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'dbo.FK_Payments_Customers'))
BEGIN
    ALTER TABLE dbo.Payments
    ADD CONSTRAINT FK_Payments_Customers 
    FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
    ON DELETE NO ACTION;
END
GO

-- 5. FOREIGN KEY: Payment 1-N PaymentAllocation
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'dbo.FK_PaymentAllocations_Payments'))
BEGIN
    ALTER TABLE dbo.PaymentAllocations
    ADD CONSTRAINT FK_PaymentAllocations_Payments 
    FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id)
    ON DELETE CASCADE;
END
GO

-- 6. FOREIGN KEY: Invoice 1-N PaymentAllocation
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'dbo.FK_PaymentAllocations_Invoices'))
BEGIN
    ALTER TABLE dbo.PaymentAllocations
    ADD CONSTRAINT FK_PaymentAllocations_Invoices 
    FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(Id)
    ON DELETE NO ACTION;
END
GO

-- ============================================================================
-- INDEXES FOR PERFORMANCE OPTIMIZATION
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Invoices_CustomerId' AND object_id = OBJECT_ID(N'dbo.Invoices'))
    CREATE NONCLUSTERED INDEX IX_Invoices_CustomerId ON dbo.Invoices (CustomerId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Invoices_DueDate' AND object_id = OBJECT_ID(N'dbo.Invoices'))
    CREATE NONCLUSTERED INDEX IX_Invoices_DueDate ON dbo.Invoices (DueDate, Status);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_InvoiceLines_InvoiceId' AND object_id = OBJECT_ID(N'dbo.InvoiceLines'))
    CREATE NONCLUSTERED INDEX IX_InvoiceLines_InvoiceId ON dbo.InvoiceLines (InvoiceId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_InvoiceLines_ProductId' AND object_id = OBJECT_ID(N'dbo.InvoiceLines'))
    CREATE NONCLUSTERED INDEX IX_InvoiceLines_ProductId ON dbo.InvoiceLines (ProductId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Payments_CustomerId' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE NONCLUSTERED INDEX IX_Payments_CustomerId ON dbo.Payments (CustomerId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PaymentAllocations_PaymentId' AND object_id = OBJECT_ID(N'dbo.PaymentAllocations'))
    CREATE NONCLUSTERED INDEX IX_PaymentAllocations_PaymentId ON dbo.PaymentAllocations (PaymentId);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_PaymentAllocations_InvoiceId' AND object_id = OBJECT_ID(N'dbo.PaymentAllocations'))
    CREATE NONCLUSTERED INDEX IX_PaymentAllocations_InvoiceId ON dbo.PaymentAllocations (InvoiceId);
GO

PRINT N'Foreign keys and indexes created successfully.';
GO
