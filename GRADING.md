# HƯỚNG DẪN CHẤM ĐỒ ÁN (GRADING.md)
## Đề tài 18: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable

---

### BẢNG ĐỐI SOÁT TIÊU CHÍ RUBRIC (ĐỢT 1 & ĐỢT 2)

| Tiêu chí | Điểm tối đa | Mô tả kiểm tra | Vị trí minh chứng trong Code | Kết quả |
| :--- | :---: | :--- | :--- | :---: |
| **K1.1** | 8 | Đủ 4 project; CoreBusiness không tham chiếu project nào; UseCases chỉ tham chiếu CoreBusiness | Mở 4 file `.csproj`: `SalesAR.CoreBusiness`, `SalesAR.UseCases`, `SalesAR.Plugins`, `SalesAR.Web` | **PASS (8/8)** |
| **K1.2** | 7 | Interface repository nằm ở UseCases, Plugin cài đặt; file `.razor` inject interface/use case, không inject lớp cụ thể | Interface tại `SalesAR.UseCases/PluginInterfaces/`<br>Implementation tại `Plugins/SalesAR.Plugins/`<br>`Program.cs` & `Home.razor` | **PASS (7/7)** |
| **K1.3** | 6 | 2 luật nghiệp vụ của đề cài trong CoreBusiness/UseCases, không nằm trong `.razor` (3đ mỗi luật) | • **Luật 1 (Hạn mức tín dụng)**: `SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs`<br>• **Luật 2 (Phân bổ FIFO)**: `SalesAR.UseCases/Payments/AllocatePaymentUseCase.cs` & `RecordPaymentUseCase.cs` | **PASS (6/6)** |
| **K1.4** | 4 | DI đăng ký đủ; repository/service đăng ký Scoped | `SalesAR.Web/Program.cs` | **PASS (4/4)** |
| **K2.1** | 8 | Từ 5 bảng, 3NF, có khóa ngoại; script tạo bảng + dữ liệu mẫu trong repo chạy được trên DB trống | Thư mục `/Database` gồm 4 file `.sql`: `01_CreateDatabase.sql`, `02_CreateTables.sql`, `03_CreateRelationships.sql`, `04_InsertSampleData.sql` | **PASS (8/8)** |
| **K2.2** | 6 | Cặp header–line ghi trong 1 transaction | • `Plugins/SalesAR.Plugins/InvoiceRepository.cs` (Method `CreateInvoiceWithLinesAsync`: Header + Lines + Stock)<br>• `Plugins/SalesAR.Plugins/PaymentRepository.cs` (Method `CreatePaymentWithAllocationsAsync`: Payment + Allocations + Update Status) | **PASS (6/6)** |
| **K2.3** | 5 | Mọi truy vấn tham số hóa (@Param), không nối chuỗi SQL | Toàn bộ các Repository trong `Plugins/SalesAR.Plugins/` dùng Parameterized Dapper / SqlCommand | **PASS (5/5)** |

---

### VỊ TRÍ MINH CHỨNG CÁC LUẬT NGHIỆP VỤ & TRANSACTION (ĐỢT 2)

#### 1. Business Rule 1 – Credit Limit (Hạn mức tín dụng)
* **Vị trí**: `SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs`
* **Công thức**: `CurrentDebt + NewInvoiceTotal <= Customer.CreditLimit`.
* **Thông báo lỗi khi vi phạm**:
  > *"Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng."*
* **Unit Test**: `SalesAR.UnitTests/CreditLimitTests.cs` (Method `CreateInvoice_WhenTotalExceedsCreditLimit_ThrowsExactBusinessRuleException`).

#### 2. Business Rule 2 – FIFO Payment Allocation (Phân bổ thanh toán cũ nhất trước)
* **Vị trí**: `SalesAR.UseCases/Payments/AllocatePaymentUseCase.cs` & `RecordPaymentUseCase.cs`
* **Nguyên tắc**: Sắp xếp Invoice theo `InvoiceDate ASC, Id ASC`; Phân bổ lần lượt vào hóa đơn cũ nhất trước; Không phân bổ vượt số dư hóa đơn; Tổng allocation $\le$ `Payment.Amount`.
* **Unit Test**: `SalesAR.UnitTests/FifoPaymentTests.cs` (Method `AllocatePayment_MatchesExactSpecificationExample`: A=2M, B=3M, C=1M, Payment=4M $\rightarrow$ A=2M, B=2M, C=0).

#### 3. Invoice Transaction (Header + Lines + Stock)
* **Vị trí**: `Plugins/SalesAR.Plugins/InvoiceRepository.cs` (Method `CreateInvoiceWithLinesAsync`)
* **Transaction Flow**:
  1. `connection.BeginTransaction()`
  2. Insert `Invoices` header $\rightarrow$ Lấy `invoiceId`
  3. Insert từng dòng `InvoiceLines`
  4. Trừ tồn kho sản phẩm: `UPDATE Products SET StockQuantity = StockQuantity - @Quantity WHERE Id = @ProductId AND StockQuantity >= @Quantity`
  5. Nếu bất kỳ bước nào lỗi hoặc thiếu tồn kho: `transaction.Rollback()`
  6. Thành công: `transaction.Commit()`

#### 4. Payment Transaction (Payment + Allocations + Status Update)
* **Vị trí**: `Plugins/SalesAR.Plugins/PaymentRepository.cs` (Method `CreatePaymentWithAllocationsAsync`)
* **Transaction Flow**:
  1. `connection.BeginTransaction()`
  2. Insert `Payments` $\rightarrow$ Lấy `paymentId`
  3. Insert từng dòng `PaymentAllocations`
  4. Cập nhật `Status` tương ứng của từng hóa đơn (`Paid` / `PartiallyPaid`)
  5. Rollback toàn bộ nếu lỗi, commit nếu thành công.

#### 5. Công nợ khách hàng (Customer Debt)
* **Vị trí UseCase**: `SalesAR.UseCases/Customers/GetCustomerDebtUseCase.cs`
* **Vị trí Repository**: `Plugins/SalesAR.Plugins/CustomerRepository.cs` (Method `GetCustomerDebtDetailsAsync`)
* **Chỉ số tính toán**:
  - `TotalInvoiced`: Tổng công nợ phát sinh.
  - `TotalPaid`: Tổng tiền đã thanh toán.
  - `CurrentDebt`: Tổng nợ còn lại (`TotalInvoiced - TotalPaid`).
  - `CreditLimit`: Hạn mức tín dụng.
  - `AvailableCredit`: Hạn mức còn lại (`CreditLimit - CurrentDebt`).

#### 6. Báo cáo tuổi nợ (Aging Report)
* **Vị trí UseCase**: `SalesAR.UseCases/AgingReport/GetAgingReportUseCase.cs`
* **Vị trí Repository**: `Plugins/SalesAR.Plugins/AgingReportRepository.cs` (Method `GetAgingReportAsync`)
* **Phân loại theo DueDate**:
  - `0To30`: Quá hạn $\le$ 30 ngày.
  - `31To60`: Quá hạn 31–60 ngày.
  - `61To90`: Quá hạn 61–90 ngày.
  - `Over90`: Quá hạn > 90 ngày.
  - Có dòng tổng cộng `TotalCreditLimit`, `TotalDebt`, `Total0To30`, `Total31To60`, `Total61To90`, `TotalOver90`.
