# HƯỚNG DẪN CHẤM ĐỒ ÁN (GRADING.md)
## Đề tài 18: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable

---

### BẢNG ĐỐI SOÁT TIÊU CHÍ RUBRIC ĐỢT 1

| Tiêu chí | Điểm tối đa | Mô tả kiểm tra | Vị trí minh chứng trong Code | Kết quả |
| :--- | :---: | :--- | :--- | :---: |
| **K1.1** | 8 | Đủ 4 project; CoreBusiness không tham chiếu project nào; UseCases chỉ tham chiếu CoreBusiness | Mở 4 file `.csproj`: `SalesAR.CoreBusiness`, `SalesAR.UseCases`, `SalesAR.Plugins`, `SalesAR.Web` | **PASS (8/8)** |
| **K1.2** | 7 | Interface repository nằm ở UseCases, Plugin cài đặt; file `.razor` inject interface, không inject lớp cụ thể | Interface tại `SalesAR.UseCases/PluginInterfaces/`<br>Implementation tại `Plugins/SalesAR.Plugins/`<br>`Program.cs` & `Home.razor` | **PASS (7/7)** |
| **K1.3** | 6 | 2 luật nghiệp vụ của đề cài trong CoreBusiness/UseCases, không nằm trong `.razor` (3đ mỗi luật) | • **Luật 1 (Hạn mức tín dụng)**: `SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs` (Dòng 45–53)<br>• **Luật 2 (Phân bổ FIFO & Σ phân bổ ≤ Tiền thu)**: `SalesAR.UseCases/Payments/ProcessPaymentUseCase.cs` (Dòng 36–75) | **PASS (6/6)** |
| **K1.4** | 4 | DI đăng ký đủ; repository/service đăng ký Scoped | `SalesAR.Web/Program.cs` | **PASS (4/4)** |
| **K2.1** | 8 | Từ 5 bảng, 3NF, có khóa ngoại; script tạo bảng + dữ liệu mẫu trong repo chạy được trên DB trống | Thư mục `/Database` gồm 4 file `.sql`: `01_CreateDatabase.sql`, `02_CreateTables.sql`, `03_CreateRelationships.sql`, `04_InsertSampleData.sql` | **PASS (8/8)** |
| **K2.2** | 6 | Cặp header–line ghi trong 1 transaction | `Plugins/SalesAR.Plugins/InvoiceRepository.cs` (Method `CreateInvoiceWithLinesAsync` dùng `SqlTransaction`) | **PASS (6/6)** |
| **K2.3** | 5 | Mọi truy vấn tham số hóa (@Param), không nối chuỗi SQL | Toàn bộ các Repository trong `Plugins/SalesAR.Plugins/` dùng Parameterized Dapper / SqlCommand | **PASS (5/5)** |

---

### CHI TIẾT CÁC LUẬT NGHIỆP VỤ (K1.3)

#### 1. Luật 1: Kiểm tra hạn mức tín dụng khách hàng (Credit Limit Check)
- **Nội dung luật**: Chặn không cho lập hóa đơn mới nếu `Tổng công nợ hiện tại + Giá trị hóa đơn mới > Hạn mức tín dụng (CreditLimit)`.
- **Vị trí code**: `SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs`
- **Dữ liệu mẫu kiểm thử**: Khách hàng `KH001` (Công ty TNHH Bách Hóa An Bình) có hạn mức tín dụng 100,000,000 đ, công nợ hiện tại là 93,000,000 đ. Khi lập thêm hóa đơn > 7,000,000 đ hệ thống sẽ chặn và báo lỗi vi phạm.

#### 2. Luật 2: Phân bổ thanh toán theo FIFO và Báo cáo tuổi nợ
- **Nội dung luật**: Thanh toán phân bổ vào hóa đơn cũ nhất trước (FIFO), tổng tiền phân bổ $\le$ số tiền thu; báo cáo tuổi nợ 0–30 / 31–60 / 61–90 / trên 90 ngày cộng lại bằng tổng công nợ.
- **Vị trí code**: `SalesAR.UseCases/Payments/ProcessPaymentUseCase.cs`
- **Dữ liệu mẫu kiểm thử**: Khách hàng `KH002` (Đại lý Tạp hóa Mai Linh) có 4 hóa đơn đại diện đủ cho 4 dải tuổi nợ:
  - Nợ >90 ngày: `HD001` (15,000,000 đ)
  - Nợ 61–90 ngày: `HD002` (12,000,000 đ)
  - Nợ 31–60 ngày: `HD003` (8,000,000 đ)
  - Nợ 0–30 ngày: `HD004` (10,000,000 đ)
  - Tổng công nợ: 45,000,000 đ (Tổng 4 dải = Tổng nợ).
  - Khi thu tiền (ví dụ 20,000,000 đ), thuật toán FIFO sẽ tự động thanh toán dứt điểm hóa đơn cũ nhất `HD001` (15,000,000 đ) và phân bổ tiếp 5,000,000 đ cho `HD002`.
