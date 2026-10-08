-- ============================================================================
-- SCRIPT 04: INSERT SAMPLE DATA
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- ============================================================================

USE SalesARDB;
GO

SET NOCOUNT ON;

-- 1. XÓA DỮ LIỆU CŨ THEO THỨ TỰ RÀNG BUỘC KHÓA NGOẠI
DELETE FROM dbo.PaymentAllocations;
DELETE FROM dbo.InvoiceLines;
DELETE FROM dbo.Payments;
DELETE FROM dbo.Invoices;
DELETE FROM dbo.Products;
DELETE FROM dbo.Customers;
GO

-- 2. CHÈN DỮ LIỆU KHÁCH HÀNG (5 Customers)
-- KH001: Nợ 93/100 tr (gần hạn mức tín dụng 100tr)
-- KH002: Đủ 4 nhóm tuổi nợ (0-30, 31-60, 61-90, >90 ngày) & dùng demo FIFO
-- KH003: Đã thanh toán đầy đủ (Paid)
-- KH004: Có hóa đơn thanh toán 1 phần (PartiallyPaid)
-- KH005: Khách hàng mới, công nợ thấp
SET IDENTITY_INSERT dbo.Customers ON;
INSERT INTO dbo.Customers (Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, IsActive)
VALUES 
(1, N'KH001', N'Công ty TNHH Bách Hóa An Bình', N'0901234567', N'123 Lê Lợi, Quận 1, TP. Hồ Chí Minh', 100000000.00, 30, 1),
(2, N'KH002', N'Đại lý Tạp hóa Mai Linh', N'0912345678', N'45 Trần Phú, Quận Hải Châu, TP. Đà Nẵng', 50000000.00, 30, 1),
(3, N'KH003', N'Siêu thị Mini Hoàng Gia', N'0983456789', N'78 Cầu Giấy, Quận Cầu Giấy, TP. Hà Nội', 80000000.00, 15, 1),
(4, N'KH004', N'Chuỗi Tiện lợi Phúc Long', N'0974567890', N'89 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh', 150000000.00, 45, 1),
(5, N'KH005', N'Cửa hàng Thực phẩm Sạch Minh Tâm', N'0935678901', N'12 Hùng Vương, TP. Huế', 40000000.00, 30, 1);
SET IDENTITY_INSERT dbo.Customers OFF;
GO

-- 3. CHÈN DỮ LIỆU SẢN PHẨM (10 Products)
SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (Id, Code, Name, Unit, Price, StockQuantity, IsActive)
VALUES
(1, N'SP001', N'Nước ngọt Coca-Cola 330ml (Thùng 24 lon)', N'Thùng', 240000.00, 150, 1),
(2, N'SP002', N'Bia Heineken 330ml (Thùng 24 lon)', N'Thùng', 430000.00, 200, 1),
(3, N'SP003', N'Mì Hảo Hảo Tôm Chua Cay (Thùng 30 gói)', N'Thùng', 125000.00, 300, 1),
(4, N'SP004', N'Sữa tươi tiệt trùng Vinamilk 100% 1L (Thùng 12 hộp)', N'Thùng', 380000.00, 120, 1),
(5, N'SP005', N'Dầu ăn thượng hạng Simply Canola 1L', N'Chai', 65000.00, 250, 1),
(6, N'SP006', N'Gạo Thơm ST25 Túi 5kg', N'Túi', 195000.00, 80, 1),
(7, N'SP007', N'Nước mắm Nam Ngư Đệ Nhị 900ml', N'Chai', 35000.00, 400, 1),
(8, N'SP008', N'Bột ngọt Ajinomoto 454g', N'Gói', 38000.00, 180, 1),
(9, N'SP009', N'Cà phê hòa tan G7 3in1 (Hộp 18 gói)', N'Hộp', 58000.00, 220, 1),
(10, N'SP010', N'Nước tinh khiết Aquafina 500ml (Thùng 24 chai)', N'Thùng', 105000.00, 350, 1);
SET IDENTITY_INSERT dbo.Products OFF;
GO

-- 4. CHÈN DỮ LIỆU HÓA ĐƠN (12 Invoices)
DECLARE @Now DATETIME2(0) = SYSDATETIME();

SET IDENTITY_INSERT dbo.Invoices ON;
-- KH002: Đủ 4 nhóm tuổi nợ (>90 ngày, 61-90 ngày, 31-60 ngày, 0-30 ngày)
-- HD001: > 90 ngày (quá hạn 95 ngày), Tổng tiền: 15,000,000
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (1, N'HD001', 2, DATEADD(DAY, -125, @Now), DATEADD(DAY, -95, @Now), 15000000.00, N'Unpaid');

-- HD002: 61-90 ngày (quá hạn 70 ngày), Tổng tiền: 12,000,000
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (2, N'HD002', 2, DATEADD(DAY, -100, @Now), DATEADD(DAY, -70, @Now), 12000000.00, N'Unpaid');

-- HD003: 31-60 ngày (quá hạn 45 ngày), Tổng tiền: 8,000,000
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (3, N'HD003', 2, DATEADD(DAY, -75, @Now), DATEADD(DAY, -45, @Now), 8000000.00, N'Unpaid');

-- HD004: 0-30 ngày (quá hạn 10 ngày), Tổng tiền: 10,000,000
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (4, N'HD004', 2, DATEADD(DAY, -40, @Now), DATEADD(DAY, -10, @Now), 10000000.00, N'Unpaid');

-- KH001: 3 hóa đơn chưa thanh toán, tổng nợ 93,000,000 / hạn mức 100,000,000 (gần chạm hạn mức)
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (5, N'HD005', 1, DATEADD(DAY, -25, @Now), DATEADD(DAY, 5, @Now), 45000000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (6, N'HD006', 1, DATEADD(DAY, -15, @Now), DATEADD(DAY, 15, @Now), 30000000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (7, N'HD007', 1, DATEADD(DAY, -5, @Now), DATEADD(DAY, 25, @Now), 18000000.00, N'Unpaid');

-- KH003: Hóa đơn đã thanh toán đầy đủ (Paid)
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (8, N'HD008', 3, DATEADD(DAY, -45, @Now), DATEADD(DAY, -30, @Now), 25000000.00, N'Paid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (9, N'HD009', 3, DATEADD(DAY, -30, @Now), DATEADD(DAY, -15, @Now), 18500000.00, N'Paid');

-- KH004: Có hóa đơn thanh toán 1 phần (PartiallyPaid) và hóa đơn trong hạn
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (10, N'HD010', 4, DATEADD(DAY, -35, @Now), DATEADD(DAY, 10, @Now), 50000000.00, N'PartiallyPaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (11, N'HD011', 4, DATEADD(DAY, -10, @Now), DATEADD(DAY, 35, @Now), 35000000.00, N'Unpaid');

-- KH005: Khách hàng công nợ thấp
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (12, N'HD012', 5, DATEADD(DAY, -3, @Now), DATEADD(DAY, 27, @Now), 8000000.00, N'Unpaid');
SET IDENTITY_INSERT dbo.Invoices OFF;
GO

-- 5. CHÈN DỮ LIỆU CHI TIẾT HÓA ĐƠN (InvoiceLines)
SET IDENTITY_INSERT dbo.InvoiceLines ON;
-- HD001: 15,000,000 (SP001 x 50 = 12tr, SP003 x 24 = 3tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(1, 1, 1, 50, 240000.00, 12000000.00),
(2, 1, 3, 24, 125000.00, 3000000.00);

-- HD002: 12,000,000 (SP001 x 50 = 12tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(3, 2, 1, 50, 240000.00, 12000000.00);

-- HD003: 8,000,000 (SP003 x 64 = 8tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(4, 3, 3, 64, 125000.00, 8000000.00);

-- HD004: 10,000,000 (SP001 x 25 = 6tr, SP003 x 32 = 4tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(5, 4, 1, 25, 240000.00, 6000000.00),
(6, 4, 3, 32, 125000.00, 4000000.00);

-- HD005: 45,000,000 (SP002 x 100 = 43tr, SP003 x 16 = 2tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(7, 5, 2, 100, 430000.00, 43000000.00),
(8, 5, 3, 16, 125000.00, 2000000.00);

-- HD006: 30,000,000 (SP001 x 100 = 24tr, SP003 x 48 = 6tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(9, 6, 1, 100, 240000.00, 24000000.00),
(10, 6, 3, 48, 125000.00, 6000000.00);

-- HD007: 18,000,000 (SP001 x 75 = 18tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(11, 7, 1, 75, 240000.00, 18000000.00);

-- HD008: 25,000,000 (SP003 x 200 = 25tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(12, 8, 3, 200, 125000.00, 25000000.00);

-- HD009: 18,500,000 (SP006 x 80 = 15.6tr, SP005 x 40 = 2.6tr, SP007 x 8 = 300k)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(13, 9, 6, 80, 195000.00, 15600000.00),
(14, 9, 5, 40, 65000.00, 2600000.00),
(15, 9, 7, 8, 37500.00, 300000.00);

-- HD010: 50,000,000 (SP002 x 100 = 43tr, SP003 x 56 = 7tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(16, 10, 2, 100, 430000.00, 43000000.00),
(17, 10, 3, 56, 125000.00, 7000000.00);

-- HD011: 35,000,000 (SP001 x 100 = 24tr, SP003 x 88 = 11tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(18, 11, 1, 100, 240000.00, 24000000.00),
(19, 11, 3, 88, 125000.00, 11000000.00);

-- HD012: 8,000,000 (SP003 x 64 = 8tr)
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount)
VALUES 
(20, 12, 3, 64, 125000.00, 8000000.00);
SET IDENTITY_INSERT dbo.InvoiceLines OFF;
GO

-- 6. CHÈN DỮ LIỆU PHIẾU THU TIỀN (Payments)
DECLARE @Now DATETIME2(0) = SYSDATETIME();

SET IDENTITY_INSERT dbo.Payments ON;
INSERT INTO dbo.Payments (Id, PaymentNumber, CustomerId, PaymentDate, Amount, Note)
VALUES 
(1, N'PT001', 3, DATEADD(DAY, -28, @Now), 25000000.00, N'Thanh toán chuyển khoản trọn gói hóa đơn HD008'),
(2, N'PT002', 3, DATEADD(DAY, -14, @Now), 18500000.00, N'Thanh toán chuyển khoản trọn gói hóa đơn HD009'),
(3, N'PT003', 4, DATEADD(DAY, -12, @Now), 30000000.00, N'Thanh toán đợt 1 tiền mặt cho hóa đơn HD010');
SET IDENTITY_INSERT dbo.Payments OFF;
GO

-- 7. CHÈN DỮ LIỆU PHÂN BỔ THANH TOÁN (PaymentAllocations)
SET IDENTITY_INSERT dbo.PaymentAllocations ON;
INSERT INTO dbo.PaymentAllocations (Id, PaymentId, InvoiceId, AllocatedAmount)
VALUES 
(1, 1, 8, 25000000.00),
(2, 2, 9, 18500000.00),
(3, 3, 10, 30000000.00);
SET IDENTITY_INSERT dbo.PaymentAllocations OFF;
GO

-- Đồng bộ lại IDENTITY seed cho các thao tác INSERT tiếp theo từ ứng dụng
DBCC CHECKIDENT ('dbo.Customers', RESEED, 5);
DBCC CHECKIDENT ('dbo.Products', RESEED, 10);
DBCC CHECKIDENT ('dbo.Invoices', RESEED, 12);
DBCC CHECKIDENT ('dbo.InvoiceLines', RESEED, 20);
DBCC CHECKIDENT ('dbo.Payments', RESEED, 3);
DBCC CHECKIDENT ('dbo.PaymentAllocations', RESEED, 3);
GO

PRINT N'Sample data inserted successfully and identity seeds synced.';
GO
