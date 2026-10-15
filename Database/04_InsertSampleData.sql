-- ============================================================================
-- SCRIPT 04: INSERT SAMPLE DATA (BẢNG DỮ LIỆU MẪU MỞ RỘNG ĐA DẠNG NGHIỆP VỤ)
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- Hỗ trợ đầy đủ tiếng Việt Unicode có dấu (NVARCHAR + N'...')
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

-- 2. CHÈN DỮ LIỆU KHÁCH HÀNG (15 Khách hàng đa ngành nghề & địa bàn toàn quốc)
SET IDENTITY_INSERT dbo.Customers ON;
INSERT INTO dbo.Customers (Id, Code, Name, Phone, Address, CreditLimit, PaymentTermDays, IsActive)
VALUES 
(1,  N'KH001', N'Công ty TNHH Bách Hóa An Bình', N'0901234567', N'123 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', 100000000.00, 30, 1),
(2,  N'KH002', N'Đại lý Tạp hóa Mai Linh', N'0912345678', N'45 Trần Phú, Phường Hải Châu 1, Quận Hải Châu, TP. Đà Nẵng', 50000000.00, 30, 1),
(3,  N'KH003', N'Siêu thị Mini Hoàng Gia', N'0983456789', N'78 Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, TP. Hà Nội', 80000000.00, 15, 1),
(4,  N'KH004', N'Chuỗi Tiện lợi Phúc Long Mart', N'0974567890', N'89 Nguyễn Trãi, Phường 2, Quận 5, TP. Hồ Chí Minh', 150000000.00, 45, 1),
(5,  N'KH005', N'Cửa hàng Thực phẩm Sạch Minh Tâm', N'0935678901', N'12 Hùng Vương, Phường Phú Hội, TP. Huế, Thừa Thiên Huế', 40000000.00, 30, 1),
(6,  N'KH006', N'Siêu thị Điện máy & Gia dụng Tân Á', N'0961122334', N'250 Xã Đàn, Phường Nam Đồng, Quận Đống Đa, TP. Hà Nội', 250000000.00, 30, 1),
(7,  N'KH007', N'Công ty CP Bánh kẹo Á Châu (ABC Bakery)', N'0942233445', N'545 Kinh Dương Vương, Phường An Lạc, Quận Bình Tân, TP. Hồ Chí Minh', 180000000.00, 30, 1),
(8,  N'KH008', N'Khách sạn & Khu Nghỉ dưỡng Biển Xanh', N'0905566778', N'12 Hoàng Sa, Phường Mân Thái, Quận Sơn Trà, TP. Đà Nẵng', 120000000.00, 30, 1),
(9,  N'KH009', N'Chuỗi Cà phê & Trà Sữa Highlands Star', N'0939988776', N'108 đường 30 Tháng 4, Phường An Phú, Quận Ninh Kiều, TP. Cần Thơ', 90000000.00, 30, 1),
(10, N'KH010', N'Nhà hàng Ẩm thực Lẩu Nướng Cung Đình', N'0913344556', N'34 Lê Lợi, Phường Vĩnh Ninh, TP. Huế, Thừa Thiên Huế', 60000000.00, 30, 1),
(11, N'KH011', N'Công ty TNHH Thương mại Dịch vụ Hải Đăng', N'0988776655', N'68 Lạch Tray, Quận Ngô Quyền, TP. Hải Phòng', 150000000.00, 30, 1),
(12, N'KH012', N'Đại lý Phân phối Tiêu dùng Phương Nam', N'0977665544', N'15 Đại lộ Bình Dương, TP. Thủ Dầu Một, Bình Dương', 100000000.00, 30, 1),
(13, N'KH013', N'Hệ thống Cửa hàng Tiện ích Sài Gòn Mart', N'0908899112', N'320 Cách Mạng Tháng 8, Phường 10, Quận 3, TP. Hồ Chí Minh', 70000000.00, 30, 1),
(14, N'KH014', N'Hợp tác xã Nông sản Hữu cơ Đà Lạt Gap', N'0918822334', N'88 Phan Đình Phùng, Phường 2, TP. Đà Lạt, Lâm Đồng', 85000000.00, 30, 1),
(15, N'KH015', N'Công ty Dược phẩm & Y tế Tâm Đức', N'0903344112', N'42 Trần Phú, Phường Lộc Thọ, TP. Nha Trang, Khánh Hòa', 200000000.00, 30, 1);
SET IDENTITY_INSERT dbo.Customers OFF;
GO

-- 3. CHÈN DỮ LIỆU SẢN PHẨM (25 Sản phẩm đa dạng ngành hàng thiết yếu)
SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (Id, Code, Name, Unit, Price, StockQuantity, IsActive)
VALUES
(1,  N'SP001', N'Nước ngọt Coca-Cola 330ml (Thùng 24 lon)', N'Thùng', 240000.00, 150, 1),
(2,  N'SP002', N'Bia Heineken Silver 330ml (Thùng 24 lon)', N'Thùng', 430000.00, 200, 1),
(3,  N'SP003', N'Mì Hảo Hảo Tôm Chua Cay (Thùng 30 gói)', N'Thùng', 125000.00, 300, 1),
(4,  N'SP004', N'Sữa tươi tiệt trùng Vinamilk 100% 1L (Thùng 12 hộp)', N'Thùng', 380000.00, 120, 1),
(5,  N'SP005', N'Dầu ăn thượng hạng Simply Canola 1L', N'Chai', 65000.00, 250, 1),
(6,  N'SP006', N'Gạo Thơm ST25 Thượng Hạng Túi 5kg', N'Túi', 195000.00, 80, 1),
(7,  N'SP007', N'Nước mắm Nam Ngư Đệ Nhị 900ml', N'Chai', 35000.00, 400, 1),
(8,  N'SP008', N'Bột ngọt Ajinomoto 454g', N'Gói', 38000.00, 180, 1),
(9,  N'SP009', N'Cà phê hòa tan G7 3in1 (Hộp 18 gói)', N'Hộp', 58000.00, 220, 1),
(10, N'SP010', N'Nước khoáng thiên nhiên Lavie 500ml (Thùng 24 chai)', N'Thùng', 105000.00, 350, 1),
(11, N'SP011', N'Nước ngọt Pepsi Không Calo 330ml (Thùng 24 lon)', N'Thùng', 235000.00, 180, 1),
(12, N'SP012', N'Bia Tiger Crystal 330ml (Thùng 24 lon)', N'Thùng', 410000.00, 160, 1),
(13, N'SP013', N'Nước tương Maggi Đậm Đặc 700ml', N'Chai', 32000.00, 280, 1),
(14, N'SP014', N'Hạt nêm Knorr Thịt Thăn Xương Ống 900g', N'Gói', 78000.00, 190, 1),
(15, N'SP015', N'Sữa đặc có đường Ông Thọ Đỏ 380g', N'Lon', 26000.00, 450, 1),
(16, N'SP016', N'Nước tăng lực Red Bull 250ml (Thùng 24 lon)', N'Thùng', 265000.00, 140, 1),
(17, N'SP017', N'Bột giặt OMO Comfort Tinh Dầu Thơm 5.5kg', N'Túi', 245000.00, 95, 1),
(18, N'SP018', N'Nước rửa chén Sunlight Chanh 3.6kg', N'Can', 115000.00, 110, 1),
(19, N'SP019', N'Nước lau sàn Sunlight Tinh Dầu Hoa Hạ 3.6kg', N'Can', 98000.00, 130, 1),
(20, N'SP020', N'Trà xanh C2 Hương Chanh 455ml (Lốc 6 chai)', N'Lốc', 52000.00, 210, 1),
(21, N'SP021', N'Bánh Chocopie Orion Hộp 12 cái (Hộp 396g)', N'Hộp', 56000.00, 175, 1),
(22, N'SP022', N'Khăn giấy rút Pulppy 3 lớp (Lốc 3 gói)', N'Lốc', 55000.00, 160, 1),
(23, N'SP023', N'Xúc xích Tiệt trùng Vissan Heo 40g (Gói 5 cây)', N'Gói', 22000.00, 320, 1),
(24, N'SP024', N'Nước uống bù khoáng Pocari Sweat 500ml (Thùng 24 chai)', N'Thùng', 290000.00, 115, 1),
(25, N'SP025', N'Tương ớt Chinsu Chai 500g', N'Chai', 24000.00, 300, 1);
SET IDENTITY_INSERT dbo.Products OFF;
GO

-- 4. CHÈN DỮ LIỆU HÓA ĐƠN (30 Hóa đơn trải đều các nhóm tuổi nợ và trạng thái)
DECLARE @Now DATETIME2(0) = SYSDATETIME();

SET IDENTITY_INSERT dbo.Invoices ON;

-- KH002: Đủ 4 nhóm tuổi nợ (>90 ngày, 61-90 ngày, 31-60 ngày, 0-30 ngày)
-- HD001: > 90 ngày (quá hạn 95 ngày), Tổng: 15,000,000
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (1, N'HD001', 2, DATEADD(DAY, -125, @Now), DATEADD(DAY, -95, @Now), 15000000.00, N'Unpaid');

-- HD002: 61-90 ngày (quá hạn 70 ngày), Tổng: 12,000,000
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (2, N'HD002', 2, DATEADD(DAY, -100, @Now), DATEADD(DAY, -70, @Now), 12000000.00, N'Unpaid');

-- HD003: 31-60 ngày (quá hạn 45 ngày), Tổng: 8,000,000
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (3, N'HD003', 2, DATEADD(DAY, -75, @Now), DATEADD(DAY, -45, @Now), 8000000.00, N'Unpaid');

-- HD004: 0-30 ngày (quá hạn 10 ngày), Tổng: 10,000,000
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

-- KH006: Siêu thị Điện máy & Gia dụng Tân Á
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (13, N'HD013', 6, DATEADD(DAY, -20, @Now), DATEADD(DAY, 10, @Now), 15700000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (14, N'HD014', 6, DATEADD(DAY, -10, @Now), DATEADD(DAY, 20, @Now), 32100000.00, N'Unpaid');

-- KH007: Công ty Bánh kẹo ABC Bakery
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (15, N'HD015', 7, DATEADD(DAY, -40, @Now), DATEADD(DAY, -10, @Now), 39500000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (16, N'HD016', 7, DATEADD(DAY, -5, @Now), DATEADD(DAY, 25, @Now), 23300000.00, N'Unpaid');

-- KH008: Resort Biển Xanh Đà Nẵng
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (17, N'HD017', 8, DATEADD(DAY, -85, @Now), DATEADD(DAY, -55, @Now), 43300000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (18, N'HD018', 8, DATEADD(DAY, -15, @Now), DATEADD(DAY, 15, @Now), 25900000.00, N'Unpaid');

-- KH009: Highlands Star Cần Thơ
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (19, N'HD019', 9, DATEADD(DAY, -110, @Now), DATEADD(DAY, -80, @Now), 28700000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (20, N'HD020', 9, DATEADD(DAY, -20, @Now), DATEADD(DAY, 10, @Now), 14800000.00, N'Unpaid');

-- KH010: Nhà hàng Lẩu Nướng Cung Đình Huế
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (21, N'HD021', 10, DATEADD(DAY, -140, @Now), DATEADD(DAY, -110, @Now), 13000000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (22, N'HD022', 10, DATEADD(DAY, -35, @Now), DATEADD(DAY, -5, @Now), 25200000.00, N'PartiallyPaid');

-- KH011: Thương mại Dịch vụ Hải Đăng Hải Phòng
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (23, N'HD023', 11, DATEADD(DAY, -50, @Now), DATEADD(DAY, -20, @Now), 32800000.00, N'Paid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (24, N'HD024', 11, DATEADD(DAY, -12, @Now), DATEADD(DAY, 18, @Now), 34700000.00, N'Unpaid');

-- KH012: Đại lý Phương Nam Bình Dương
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (25, N'HD025', 12, DATEADD(DAY, -45, @Now), DATEADD(DAY, -15, @Now), 25950000.00, N'Unpaid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (26, N'HD026', 12, DATEADD(DAY, -8, @Now), DATEADD(DAY, 22, @Now), 19500000.00, N'Unpaid');

-- KH013: Sài Gòn Mart
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (27, N'HD027', 13, DATEADD(DAY, -70, @Now), DATEADD(DAY, -40, @Now), 29000000.00, N'Paid');

INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (28, N'HD028', 13, DATEADD(DAY, -15, @Now), DATEADD(DAY, 15, @Now), 9120000.00, N'Unpaid');

-- KH014: Đà Lạt Gap
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (29, N'HD029', 14, DATEADD(DAY, -100, @Now), DATEADD(DAY, -70, @Now), 6620000.00, N'Unpaid');

-- KH015: Y tế Tâm Đức Nha Trang
INSERT INTO dbo.Invoices (Id, InvoiceNumber, CustomerId, InvoiceDate, DueDate, TotalAmount, Status)
VALUES (30, N'HD030', 15, DATEADD(DAY, -60, @Now), DATEADD(DAY, -30, @Now), 34500000.00, N'PartiallyPaid');

SET IDENTITY_INSERT dbo.Invoices OFF;
GO

-- 5. CHÈN DỮ LIỆU CHI TIẾT HÓA ĐƠN (InvoiceLines)
SET IDENTITY_INSERT dbo.InvoiceLines ON;

-- HD001: 15,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (1, 1, 1, 50, 240000.00, 12000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (2, 1, 3, 24, 125000.00, 3000000.00);

-- HD002: 12,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (3, 2, 1, 50, 240000.00, 12000000.00);

-- HD003: 8,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (4, 3, 3, 64, 125000.00, 8000000.00);

-- HD004: 10,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (5, 4, 1, 25, 240000.00, 6000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (6, 4, 3, 32, 125000.00, 4000000.00);

-- HD005: 45,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (7, 5, 2, 100, 430000.00, 43000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (8, 5, 3, 16, 125000.00, 2000000.00);

-- HD006: 30,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (9, 6, 1, 100, 240000.00, 24000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (10, 6, 3, 48, 125000.00, 6000000.00);

-- HD007: 18,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (11, 7, 1, 75, 240000.00, 18000000.00);

-- HD008: 25,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (12, 8, 3, 200, 125000.00, 25000000.00);

-- HD009: 18,500,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (13, 9, 6, 80, 195000.00, 15600000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (14, 9, 5, 40, 65000.00, 2600000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (15, 9, 7, 8, 37500.00, 300000.00);

-- HD010: 50,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (16, 10, 2, 100, 430000.00, 43000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (17, 10, 3, 56, 125000.00, 7000000.00);

-- HD011: 35,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (18, 11, 1, 100, 240000.00, 24000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (19, 11, 3, 88, 125000.00, 11000000.00);

-- HD012: 8,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (20, 12, 3, 64, 125000.00, 8000000.00);

-- HD013: 15,700,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (21, 13, 17, 40, 245000.00, 9800000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (22, 13, 18, 30, 115000.00, 3450000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (23, 13, 19, 25, 98000.00, 2450000.00);

-- HD014: 32,100,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (24, 14, 2, 50, 430000.00, 21500000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (25, 14, 16, 40, 265000.00, 10600000.00);

-- HD015: 39,500,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (26, 15, 4, 80, 380000.00, 30400000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (27, 15, 15, 200, 26000.00, 5200000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (28, 15, 5, 60, 65000.00, 3900000.00);

-- HD016: 23,300,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (29, 16, 6, 100, 195000.00, 19500000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (30, 16, 8, 100, 38000.00, 3800000.00);

-- HD017: 43,300,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (31, 17, 1, 60, 240000.00, 14400000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (32, 17, 10, 80, 105000.00, 8400000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (33, 17, 12, 50, 410000.00, 20500000.00);

-- HD018: 25,900,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (34, 18, 2, 40, 430000.00, 17200000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (35, 18, 24, 30, 290000.00, 8700000.00);

-- HD019: 28,700,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (36, 19, 4, 50, 380000.00, 19000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (37, 19, 9, 100, 58000.00, 5800000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (38, 19, 15, 150, 26000.00, 3900000.00);

-- HD020: 14,800,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (39, 20, 20, 100, 52000.00, 5200000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (40, 20, 1, 40, 240000.00, 9600000.00);

-- HD021: 13,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (41, 21, 7, 100, 35000.00, 3500000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (42, 21, 13, 100, 32000.00, 3200000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (43, 21, 14, 50, 78000.00, 3900000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (44, 21, 25, 100, 24000.00, 2400000.00);

-- HD022: 25,200,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (45, 22, 2, 30, 430000.00, 12900000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (46, 22, 12, 30, 410000.00, 12300000.00);

-- HD023: 32,800,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (47, 23, 3, 160, 125000.00, 20000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (48, 23, 21, 150, 56000.00, 8400000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (49, 23, 23, 200, 22000.00, 4400000.00);

-- HD024: 34,700,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (50, 24, 11, 80, 235000.00, 18800000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (51, 24, 16, 60, 265000.00, 15900000.00);

-- HD025: 25,950,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (52, 25, 17, 60, 245000.00, 14700000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (53, 25, 18, 50, 115000.00, 5750000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (54, 25, 22, 100, 55000.00, 5500000.00);

-- HD026: 19,500,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (55, 26, 6, 80, 195000.00, 15600000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (56, 26, 5, 60, 65000.00, 3900000.00);

-- HD027: 29,000,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (57, 27, 1, 50, 240000.00, 12000000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (58, 27, 11, 50, 235000.00, 11750000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (59, 27, 10, 50, 105000.00, 5250000.00);

-- HD028: 9,120,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (60, 28, 21, 80, 56000.00, 4480000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (61, 28, 9, 80, 58000.00, 4640000.00);

-- HD029: 6,620,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (62, 29, 5, 40, 65000.00, 2600000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (63, 29, 7, 60, 35000.00, 2100000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (64, 29, 13, 60, 32000.00, 1920000.00);

-- HD030: 34,500,000
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (65, 30, 10, 100, 105000.00, 10500000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (66, 30, 22, 120, 55000.00, 6600000.00);
INSERT INTO dbo.InvoiceLines (Id, InvoiceId, ProductId, Quantity, UnitPrice, Amount) VALUES (67, 30, 24, 60, 290000.00, 17400000.00);

SET IDENTITY_INSERT dbo.InvoiceLines OFF;
GO

-- 6. CHÈN DỮ LIỆU PHIẾU THU TIỀN (7 Payments)
DECLARE @Now DATETIME2(0) = SYSDATETIME();

SET IDENTITY_INSERT dbo.Payments ON;
INSERT INTO dbo.Payments (Id, PaymentNumber, CustomerId, PaymentDate, Amount, Note)
VALUES 
(1, N'PT001', 3, DATEADD(DAY, -28, @Now), 25000000.00, N'Thanh toán chuyển khoản trọn gói hóa đơn HD008'),
(2, N'PT002', 3, DATEADD(DAY, -14, @Now), 18500000.00, N'Thanh toán chuyển khoản trọn gói hóa đơn HD009'),
(3, N'PT003', 4, DATEADD(DAY, -12, @Now), 30000000.00, N'Thanh toán đợt 1 tiền mặt cho hóa đơn HD010'),
(4, N'PT004', 10, DATEADD(DAY, -25, @Now), 15200000.00, N'Thanh toán đợt 1 chuyển khoản qua Vietcombank cho hóa đơn HD022'),
(5, N'PT005', 11, DATEADD(DAY, -18, @Now), 32800000.00, N'Thanh toán chuyển khoản MB Bank tất toán toàn bộ hóa đơn HD023'),
(6, N'PT006', 13, DATEADD(DAY, -38, @Now), 29000000.00, N'Thanh toán chuyển khoản Techcombank tất toán hóa đơn HD027'),
(7, N'PT007', 15, DATEADD(DAY, -28, @Now), 20000000.00, N'Thanh toán tạm ứng đợt 1 tiền mặt cho hóa đơn HD030');
SET IDENTITY_INSERT dbo.Payments OFF;
GO

-- 7. CHÈN DỮ LIỆU PHÂN BỔ THANH TOÁN (PaymentAllocations)
SET IDENTITY_INSERT dbo.PaymentAllocations ON;
INSERT INTO dbo.PaymentAllocations (Id, PaymentId, InvoiceId, AllocatedAmount)
VALUES 
(1, 1, 8, 25000000.00),
(2, 2, 9, 18500000.00),
(3, 3, 10, 30000000.00),
(4, 4, 22, 15200000.00),
(5, 5, 23, 32800000.00),
(6, 6, 27, 29000000.00),
(7, 7, 30, 20000000.00);
SET IDENTITY_INSERT dbo.PaymentAllocations OFF;
GO

-- 8. ĐỒNG BỘ LẠI IDENTITY SEED CHO CÁC THAO TÁC INSERT TIẾP THEO TỪ WEB APP
DBCC CHECKIDENT ('dbo.Customers', RESEED, 15);
DBCC CHECKIDENT ('dbo.Products', RESEED, 25);
DBCC CHECKIDENT ('dbo.Invoices', RESEED, 30);
DBCC CHECKIDENT ('dbo.InvoiceLines', RESEED, 67);
DBCC CHECKIDENT ('dbo.Payments', RESEED, 7);
DBCC CHECKIDENT ('dbo.PaymentAllocations', RESEED, 7);
GO

PRINT N'Sample data inserted successfully and identity seeds synced.';
GO
