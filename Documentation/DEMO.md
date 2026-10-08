# 🎬 KỊCH BẢN THUYẾT MINH & DEMO ĐỒ ÁN (10 PHÚT)

> **ĐỀ TÀI SỐ 18**: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable  
> **SINH VIÊN**: **Đinh Hữu Quang** (MSSV: **23K4080042**)  
> **THỜI LƯỢNG DEMO**: 10 Phút  

---

## ⏱️ LỘ TRÌNH 17 BƯỚC DEMO CHUẨN XÁC

### 1. BƯỚC 1: ĐĂNG NHẬP VỚI QUYỀN ADMIN (00:00 - 00:45)
- **Thao tác**:
  1. Mở trình duyệt tại: `http://localhost:5182/login`.
  2. Giới thiệu màn hình đăng nhập có bảo mật mật khẩu.
  3. Nhập tài khoản:
     - Tên đăng nhập: `admin`
     - Mật khẩu: `Admin@123`
  4. Bấm nút **"Đăng Nhập"**.
- **Lời thoại gợi ý**:  
  > *"Em xin phép bắt đầu phần demo với tài khoản Quản trị viên (ADMIN) có toàn quyền trên hệ thống. Mật khẩu được mã hóa an toàn qua thuật toán PBKDF2 with SHA-256 kèm Salt."*

---

### 2. BƯỚC 2: TỔNG QUAN DASHBOARD & KPI CÔNG NỢ (00:45 - 01:30)
- **Thao tác**:
  - Giao diện tự động chuyển hướng về trang `/dashboard`.
  - Giới thiệu 4 thẻ Metric: Tổng công nợ toàn hệ thống, Nợ quá hạn, Số khách hàng đang nợ, Số hóa đơn chưa thanh toán.
  - Giới thiệu Biểu đồ / Thống kê phân tổ tuổi nợ (0–30 ngày, 31–60 ngày, 61–90 ngày, >90 ngày).
  - Giới thiệu Top khách hàng có dư nợ cao nhất.
- **Lời thoại gợi ý**:  
  > *"Trang Dashboard cung cấp cái nhìn tức thời về sức khỏe tài chính và công nợ của doanh nghiệp, tự động tính toán tổng nợ theo các nhóm tuổi nợ từ cơ sở dữ liệu."*

---

### 3. BƯỚC 3: QUẢN LÝ KHÁCH HÀNG & HẠN MỨC TÍN DỤNG (01:30 - 02:15)
- **Thao tác**:
  1. Bấm menu **"Khách hàng"** (`/customers`).
  2. Bấm vào khách hàng **KH001 (Công ty TNHH Bách Hóa An Bình)** hoặc **KH002 (Đại lý Mai Linh)**.
  3. Chỉ cho Thầy/Cô thấy thông tin: **Hạn mức tín dụng (`CreditLimit`)**, **Công nợ hiện tại (`CurrentDebt`)**, và **Hạn mức còn lại (`AvailableCredit`)**.
- **Lời thoại gợi ý**:  
  > *"Mỗi khách hàng được gán một hạn mức tín dụng nhất định để kiểm soát rủi ro bán chịu. Ví dụ Đại lý Mai Linh có hạn mức 100.000.000đ và hiện đang có nợ tồn đọng."*

---

### 4. BƯỚC 4: DANH MỤC SẢN PHẨM & TỒN KHO (02:15 - 02:45)
- **Thao tác**:
  - Bấm menu **"Sản phẩm"** (`/products`).
  - Lướt nhanh bảng sản phẩm: Mã hàng, Tên hàng, Đơn vị tính, Đơn giá, Tồn kho khả dụng.

---

### 5. BƯỚC 5: TẠO HÓA ĐƠN HỢP LỆ (02:45 - 03:45)
- **Thao tác**:
  1. Bấm menu **"Hóa đơn"** $\rightarrow$ **"Lập hóa đơn mới"** (`/invoices/create`).
  2. Chọn Khách hàng: **KH001 (Công ty TNHH Bách Hóa An Bình)**.
  3. Chọn Sản phẩm: **SP001 (Nước ngọt Coca-Cola 330ml)**, Số lượng: `10` (Đơn giá 195.000đ $\rightarrow$ Thành tiền 1.950.000đ).
  4. Bấm **"Lưu & Xuất Hóa Đơn"**.
  5. Hệ thống hiển thị Toast xanh thông báo thành công và chuyển về danh sách hóa đơn.

---

### 6. BƯỚC 6: CHỨNG MINH LUẬT HẠN MỨC TÍN DỤNG (BUSINESS RULE 1) (03:45 - 04:45) ⭐ QUAN TRỌNG
- **Thao tác**:
  1. Tiếp tục bấm **"Lập hóa đơn mới"** (`/invoices/create`).
  2. Chọn khách hàng: **KH002 (Đại lý Mai Linh)** (Khách hàng này hiện đang có dư nợ cao ~45.000.000đ, hạn mức 100.000.000đ).
  3. Chọn sản phẩm **SP002 (Bia Heineken)**, nhập số lượng cực lớn: `200` thùng (Trị giá 90.000.000đ).
  4. Tổng nợ mới sẽ là $45.000.000 + 90.000.000 = 135.000.000đ > 100.000.000đ (Vượt hạn mức).
  5. Bấm **"Lưu & Xuất Hóa Đơn"**.
  6. **Kết quả**: Hệ thống chặn đứng giao dịch và hiển thị Banner lỗi nghiệp vụ màu đỏ:
     > ⚠️ *"Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng."*
- **Lời thoại gợi ý**:  
  > *"Đây là minh chứng cho Business Rule 1 (Tiêu chí K1.3). Hệ thống kiểm tra trước khi ghi sổ, nếu vượt hạn mức tín dụng thì lập tức từ chối và hiển thị thông báo lỗi rõ ràng."*

---

### 7. BƯỚC 7 & 8: GHI NHẬN THU TIỀN & CHỨNG MINH PHÂN BỔ FIFO (04:45 - 06:00) ⭐ QUAN TRỌNG
- **Thao tác**:
  1. Bấm menu **"Thu tiền (FIFO)"** $\rightarrow$ **"Lập phiếu thu"** (`/payments/create`).
  2. Chọn khách hàng: **KH002 (Đại lý Mai Linh)**.
  3. Ngay lập tức bảng **"Dự kiến phân bổ FIFO"** hiện ra:
     - Hệ thống liệt kê các hóa đơn chưa thanh toán xếp theo thứ tự ngày đến hạn cũ nhất (`HD001`, `HD002`, `HD003`...).
  4. Nhập Số tiền thu: `20.000.000đ`.
  5. Hệ thống hiển thị bảng cấn trừ FIFO tự động:
     - Hóa đơn cũ nhất `HD001` (Nợ 15M) được trả hết `15.000.000đ` $\rightarrow$ Đủ tiền, đóng hóa đơn.
     - Hóa đơn kế tiếp `HD002` (Nợ 12M) được cấn trừ nốt phần tiền còn lại `5.000.000đ` $\rightarrow$ Trở thành `PartiallyPaid`.
     - Các hóa đơn mới hơn không được nhận tiền đợt này.
  6. Bấm nút **"Xác Nhận Thu Tiền & Phân Bổ"**.
  7. Kết quả lưu thành công trong một Database Transaction toàn vẹn.
- **Lời thoại gợi ý**:  
  > *"Đây là minh chứng cho Business Rule 2 (Phân bổ nợ theo nguyên tắc FIFO). Thuật toán duyệt qua hóa đơn cũ nhất trước để cấn trừ tiền, bảo vệ tuổi nợ của doanh nghiệp."*

---

### 8. BƯỚC 9: BÁO CÁO PHÂN TÍCH TUỔI NỢ (AGING REPORT) (06:00 - 06:45)
- **Thao tác**:
  1. Bấm menu **"Báo cáo Tuổi nợ"** (`/aging-report`).
  2. Xem bảng phân tích tuổi nợ động:
     - Các cột nợ chia theo kỳ hạn: `0–30 ngày`, `31–60 ngày`, `61–90 ngày`, và `>90 ngày`.
     - Các khoản nợ xấu quá hạn >90 ngày được tô màu đỏ cảnh báo.
     - Dòng tổng cộng hiển thị đầy đủ tổng nợ toàn hệ thống.

---

### 9. BƯỚC 10 & 11: XUẤT BÁO CÁO EXCEL VÀ PDF (06:45 - 07:30)
- **Thao tác**:
  1. Tại trang Báo cáo Tuổi nợ, bấm nút **"Xuất Excel"**:
     - File `AgingReport_YYYYMMDD.xlsx` được tải về máy.
     - Mở file Excel: Đầy đủ định dạng tiền tệ Việt Nam `#,##0 đ`, màu sắc phân loại tiêu chuẩn kế toán.
  2. Bấm nút **"Xuất PDF"**:
     - File `AgingReport_YYYYMMDD.pdf` được tải về máy.
     - Mở file PDF: Khổ ngang A4 Landscape chuyên nghiệp, font chữ `Segoe UI` hiển thị 100% tiếng Việt chuẩn sắc nét, không bị lỗi font hay ô vuông.
- **Lời thoại gợi ý**:  
  > *"Em sử dụng ClosedXML cho Excel và QuestPDF cho PDF, cấu hình font chữ Segoe UI để văn bản in ấn tiếng Việt luôn sắc nét và chuẩn mực kế toán."*

---

### 10. BƯỚC 12, 13, 14, 15: CHỨNG MINH BẢO MẬT & PHÂN QUYỀN (RBAC) (07:30 - 08:30)
- **Thao tác**:
  1. Bấm nút **"Đăng xuất"** ở góc trên bên phải.
  2. Đăng nhập lại với tài khoản Nhân viên Kinh doanh (SALES):
     - Tên đăng nhập: `sales`
     - Mật khẩu: `Sales@123`
  3. Menu bên trái tự động ẩn các mục quản trị (`Quản lý người dùng`, `Phân quyền`).
  4. Cố tình gõ trực tiếp URL quản trị trên trình duyệt: `http://localhost:5182/admin/users`.
  5. **Kết quả**: Giao diện hiển thị Thẻ cảnh báo bảo mật màu đỏ:
     > 🛡️ *"Truy cập bị từ chối! Tài khoản sales (Vai trò: SALES) không có quyền hạn để truy cập trang này."*
- **Lời thoại gợi ý**:  
  > *"Hệ thống không chỉ ẩn menu mà còn khóa chặt ở tầng Routing thông qua AuthorizeRouteView và Policy, ngăn chặn hoàn toàn việc can thiệp trái phép qua URL."*

---

### 11. BƯỚC 16: KIỂM TRA RESPONSIVE MÀN HÌNH DI ĐỘNG 390PX (08:30 - 09:15)
- **Thao tác**:
  1. Nhấn phím `F12` $\rightarrow$ Bấm `Ctrl + Shift + M` để bật Device Mode.
  2. Chọn màn hình **iPhone 12 Pro (390 x 844)**.
  3. Lướt qua Dashboard, Bảng tuổi nợ, Trang tạo hóa đơn:
     - Bảng tự động cho phép cuộn ngang mà không làm bể layout trang.
     - Nút bấm và thẻ card co giãn tối ưu, font chữ cân đối.
- **Lời thoại gợi ý**:  
  > *"Toàn bộ giao diện được thiết kế Mobile-first với breakpoint 390px, tương thích tối đa với các dòng smartphone phổ biến hiện nay."*

---

### 12. BƯỚC 17: MỞ SSMS CHỨNG MINH DỮ LIỆU THẬT (09:15 - 10:00)
- **Thao tác**:
  1. Mở cửa sổ **SQL Server Management Studio (SSMS)**.
  2. Chạy câu lệnh truy vấn:
     ```sql
     SELECT TOP 3 * FROM dbo.Invoices ORDER BY Id DESC;
     SELECT TOP 5 * FROM dbo.InvoiceLines WHERE InvoiceId = (SELECT MAX(Id) FROM dbo.Invoices);
     SELECT TOP 3 * FROM dbo.Payments ORDER BY Id DESC;
     SELECT TOP 5 * FROM dbo.PaymentAllocations WHERE PaymentId = (SELECT MAX(Id) FROM dbo.Payments);
     ```
  3. Chỉ cho Thầy/Cô thấy dòng hóa đơn và phiếu thu vừa tạo ở các bước trên đã được lưu trực tiếp vào database SQL Server `SalesARDB`.
- **Lời thoại kết thúc**:  
  > *"Dạ thưa Thầy/Cô, toàn bộ dữ liệu giao dịch phát sinh từ giao diện đã được lưu trữ vĩnh viễn và toàn vẹn vào cơ sở dữ liệu SQL Server. Em xin kết thúc phần demo và sẵn sàng nhận câu hỏi vấn đáp ạ!"*
