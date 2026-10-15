# BÁO CÁO NGHIỆP VỤ CỐT LÕI CÔNG NỢ & THANH TOÁN (AR CORE)
**Đề tài 18: Bán hàng & Công nợ Khách hàng — Sales & Accounts Receivable**  
**Giai đoạn:** PART 4/10 — ACCOUNTS RECEIVABLE CORE  
**Ngày thực hiện:** 15/10/2026  
**Hệ quản trị CSDL:** Microsoft SQL Server 2022 / Express  
**Kết quả kiểm thử:** 32/32 Tests Đạt (100% Passed) — Gồm Unit Tests và Integration Tests trực tiếp trên SQL Server

---

## 1. THỐNG NHẤT CÔNG THỨC TÍNH CÔNG NỢ & SỐ DƯ CÓ THỂ THU

Hệ thống đã chuẩn hóa và thống nhất công thức tính toán tài chính trên toàn bộ các tầng (Domain, UseCase, Repository, SQL Query, UI và Báo cáo Excel/PDF):

### 1.1. Công thức số dư nợ của từng hóa đơn (Invoice Outstanding Amount)
$$\text{OutstandingAmount} = \text{InvoiceAmount} - \text{ValidAllocatedPayments} - \text{ValidAdjustments}$$

- **`InvoiceAmount` (`TotalAmount`):** Tổng giá trị hóa đơn xuất bán ban đầu.
- **`ValidAllocatedPayments` (`AllocatedAmount`):** Tổng số tiền đã được phân bổ thực tế từ các phiếu thu hợp lệ (`PaymentAllocations` liên kết với hóa đơn).
- **`ValidAdjustments` (`AdjustedAmount`):** Tổng các khoản điều chỉnh hợp lệ gắn trực tiếp với hóa đơn (`DebtAdjustments` có `InvoiceId = i.Id`), bao gồm:
  - Chiết khấu thương mại thanh toán đúng hạn (`Discount`).
  - Hàng bán bị trả lại do lỗi (`Return`).
  - Xóa nợ xấu đã được duyệt (`BadDebtWriteOff`).
- **Nguyên tắc dấu:** Các khoản giảm trừ công nợ / Credit Notes mang giá trị dương biểu thị số tiền giảm nợ. Nếu là điều chỉnh tăng nợ (phí phát sinh), số tiền sẽ là âm hoặc cộng thêm tương ứng.
- **Quy tắc chặn cận dưới:** $\text{RemainingAmount} = \max(0, \text{TotalAmount} - \text{PaidAmount} - \text{AdjustedAmount})$. Số dư nợ của hóa đơn không bao giờ âm.

### 1.2. Công thức tổng công nợ khách hàng (Customer Total Debt)
$$\text{CurrentDebt} = \text{TotalInvoiced} - \text{TotalPaid} - \text{TotalAdjusted}$$

- **`TotalInvoiced`:** Tổng tiền các hóa đơn hợp lệ (bỏ qua hóa đơn có trạng thái `Cancelled`).
- **`TotalPaid`:** Tổng số tiền khách hàng đã thanh toán qua các phiếu thu.
- **`TotalAdjusted`:** Tổng tất cả các khoản điều chỉnh công nợ của khách hàng (cả cấp hóa đơn và cấp khách hàng tổng thể).
- **Quy tắc chống tính trùng:** Mọi bút toán điều chỉnh đều có `Id` duy nhất trong `dbo.DebtAdjustments`. Khi tính toán theo hóa đơn, chỉ lấy điều chỉnh gắn với `InvoiceId`. Khi tính tổng nợ khách hàng, tính toán tổng hợp bảo đảm không một khoản điều chỉnh nào bị tính 2 lần.

---

## 2. NGHIỆP VỤ THANH TOÁN & PHÂN BỔ CÔNG NỢ (PAYMENT & FIFO ALLOCATION)

### 2.1. Triển khai phân bổ FIFO theo ngày đến hạn (`DueDate`)
Quy tắc phân bổ FIFO trong `AllocatePaymentUseCase` được chuẩn hóa theo thứ tự ưu tiên:
1. **`DueDate ASC` (Ưu tiên số 1):** Hóa đơn có hạn nợ đến trước bắt buộc phải được ưu tiên thanh toán trước để giảm thiểu rủi ro quá hạn và lãi phạt cho khách hàng.
2. **`InvoiceDate ASC` (Ưu tiên số 2):** Nếu cùng ngày đến hạn, hóa đơn lập trước được thanh toán trước.
3. **`Id ASC` (Ưu tiên số 3):** Đảm bảo thuật toán mang tính tất định tuyệt đối (`deterministic`).

### 2.2. Xử lý các tình huống thanh toán thực tế
- **Thanh toán một phần (Partial Payment):** Khi số tiền thanh toán nhỏ hơn số dư nợ hóa đơn, hóa đơn được thanh toán một phần và chuyển sang trạng thái `PartiallyPaid`.
- **Thanh toán nhiều hóa đơn (Multi-Invoice Payment):** Số tiền thu được trừ dần lần lượt qua các hóa đơn theo thứ tự FIFO cho đến khi hết tiền.
- **Thanh toán dư (Overpayment):** Khi số tiền khách nộp lớn hơn tổng dư nợ hiện tại, hệ thống phân bổ đủ để tất toán toàn bộ hóa đơn (`Paid`). Khoản tiền thừa còn lại vẫn được lưu an toàn trên bản ghi `dbo.Payments` (phần chưa phân bổ), không bị mất mát hay gây lỗi hệ thống.

### 2.3. Ràng buộc bảo vệ tính toàn vẹn phân bổ
- **Giới hạn số tiền phân bổ:** $\sum \text{AllocatedAmount} \le \text{Payment.Amount}$. Tổng phân bổ không được vượt quá số tiền của phiếu thu.
- **Giới hạn số dư hóa đơn:** Đối với từng hóa đơn, $\text{AllocatedAmount} \le \text{Invoice.RemainingAmount}$. Không được phân bổ vượt số dư có thể thu.
- **Chống trùng lặp phiếu thu (Anti-duplicate Payment):**
  - Tầng CSDL: Ràng buộc duy nhất `UQ_Payments_PaymentNumber` trên cột `PaymentNumber`.
  - Tầng UseCase / Repository: Kiểm tra tồn tại trước khi chèn, khóa với `WITH (UPDLOCK, HOLDLOCK)`.

### 2.4. Phân bổ lại (Reallocation) & Kiểm soát quyền hạn
Khi kế toán cần sửa lại việc phân bổ tiền cho các hóa đơn:
- **Kiểm tra quyền:** Chỉ người dùng có vai trò **`ADMIN`** hoặc **`ACCOUNTANT`** mới được phép thực hiện (`ReallocatePaymentUseCase`). Người dùng có vai trò `SALES` bị từ chối với lỗi `UnauthorizedAccessException`.
- **Bắt buộc có lý do:** Thao tác phân bổ lại bắt buộc phải nhập lý do nghiệp vụ nhằm bảo đảm tính minh bạch kiểm toán.
- **Nguyên tử hóa:** Thực hiện xóa phân bổ cũ, chèn phân bổ mới, tính toán lại trạng thái toàn bộ các hóa đơn bị ảnh hưởng và ghi nhật ký kiểm toán trong **một Transaction duy nhất**.

### 2.5. Kiểm soát tranh chấp đồng thời (Concurrency Control)
- Sử dụng mức cô lập giao dịch `ReadCommitted` kết hợp gợi ý khóa `WITH (UPDLOCK, ROWLOCK)` khi đọc số dư hóa đơn trong lúc thanh toán.
- Khóa này ngăn chặn tình huống 2 giao dịch thanh toán đồng thời cùng nhìn thấy một số dư nợ và cùng phân bổ vượt mức (Race condition / Double spending).

---

## 3. PHÂN TÍCH TUỔI NỢ & BÁO CÁO (AGING ANALYSIS)

### 3.1. Ngày tham chiếu và dữ liệu thực tế
Tuổi nợ được tính toán trực tiếp từ dữ liệu thực tế trong CSDL dựa trên ngày đến hạn (`DueDate`) và ngày chốt công nợ tham chiếu (`@AsOfDate`):
$$\text{DaysOverdue} = \text{DATEDIFF}(\text{day}, \text{i.DueDate}, @\text{AsOfDate})$$

### 3.2. Chuẩn hóa các nhóm tuổi nợ (Aging Buckets)
Toàn bộ hệ thống (Database query, màn hình Razor, xuất Excel, xuất PDF) thống nhất 4 nhóm tuổi nợ:
1. **0–30 ngày (`Bucket0To30`):** Hóa đơn trong hạn nợ (`DaysOverdue <= 0`) hoặc quá hạn không quá 30 ngày (`DaysOverdue BETWEEN 1 AND 30`).
2. **31–60 ngày (`Bucket31To60`):** Nợ quá hạn từ 1 đến 2 tháng.
3. **61–90 ngày (`Bucket61To90`):** Nợ quá hạn từ 2 đến 3 tháng.
4. **Trên 90 ngày (`BucketOver90`):** Nợ quá hạn trên 3 tháng (Xếp vào nhóm nợ khó đòi / rủi ro cao).

---

## 4. HỆ THỐNG NHẬT KÝ KIỂM TOÁN (AUDIT TRAIL & LOGGING)

Mọi thao tác tài chính trọng yếu đều tự động ghi vết vào bảng `dbo.AuditLogs`:
- **Tạo hóa đơn:** Ghi nhận `Action = 'INSERT'`, bảng `Invoices`, lưu `InvoiceNumber`, `CustomerId`, `TotalAmount`, `LinesCount`.
- **Ghi nhận thanh toán:** Ghi nhận `Action = 'INSERT'`, bảng `Payments`, lưu `PaymentNumber`, `CustomerId`, `Amount`, `AllocatedTotal`.
- **Phân bổ lại:** Ghi nhận `Action = 'REALLOCATE'`, bảng `PaymentAllocations`, lưu `OldValues` (danh sách phân bổ cũ) và `NewValues` (danh sách phân bổ mới).
- **Điều chỉnh công nợ:** Ghi nhận `Action = 'INSERT'`, bảng `DebtAdjustments`, loại điều chỉnh, số tiền và lý do.
- **Thay đổi hạn mức tín dụng:** Ghi nhận `Action = 'UPDATE'`, bảng `Customers`, lưu giá trị hạn mức cũ và mới.

**Nguyên tắc an toàn bảo mật:**
- **Không ghi bí mật:** Tuyệt đối không ghi mật khẩu, hash mật khẩu, token hay secrets vào Audit Log.
- **Tính bất biến (Immutability):** Không cung cấp bất kỳ API hoặc màn hình nào cho phép cập nhật hay xóa các dòng nhật ký kiểm toán.

---

## 5. BÁO CÁO NGHIỆM THU KIỂM THỬ (TEST VERIFICATION REPORT)

Hệ thống đã xây dựng và vượt qua **32/32 tests**:

| Nhóm Test | File Test | Số lượng | Mô tả & Trường hợp kiểm thử | Môi trường kiểm thử |
|:---|:---|:---:|:---|:---:|
| **FIFO & DueDate** | `FifoDueDateAndAdjustmentTests.cs` | 3 | - Kiểm tra FIFO ưu tiên `DueDate` đến hạn trước.<br>- Kiểm tra công thức trừ cả thanh toán và điều chỉnh.<br>- Kiểm tra xử lý thanh toán dư (overpayment). | In-Memory / Mock |
| **FIFO Specification** | `FifoPaymentTests.cs` | 4 | - Kiểm tra phân bổ khớp đặc tả 4tr cho 3 HĐ (2tr, 2tr, 0tr).<br>- Không vượt số dư nợ hóa đơn.<br>- Xử lý danh sách rỗng, sắp xếp ngày. | In-Memory / Mock |
| **Validation & Quyền hạn** | `PaymentValidationAndLimitsTests.cs` | 3 | - Chống ghi nhận trùng `PaymentNumber` ở UseCase.<br>- Phân bổ lại yêu cầu quyền `ADMIN` hoặc `ACCOUNTANT`.<br>- Phân bổ lại bắt buộc phải có lý do kiểm toán. | In-Memory / Mock |
| **SQL Server Integration** | `SqlServerAccountsReceivableIntegrationTests.cs` | 3 | - **Kiểm thử trên SQL Server thật:** Ràng buộc `UQ_Payments_PaymentNumber` kích hoạt ném `SqlException` khi chèn trùng.<br>- **Kiểm thử trên SQL Server thật:** Giao dịch Rollback sạch sẽ khi phân bổ phát sinh lỗi Check Constraint.<br>- **Kiểm thử trên SQL Server thật:** Bảng `dbo.AuditLogs` hoạt động, ghi nhận lịch sử và không chứa mật khẩu. | **SQL Server thật (`SalesARDB`)** |
| **Kiểm tra Hạn mức nợ** | `CreditLimitTests.cs` | 7 | - Chặn tạo hóa đơn khi vượt hạn mức tín dụng.<br>- Cho phép tạo khi còn hạn mức khả dụng. | In-Memory / Mock |
| **Hóa đơn & Tồn kho** | `InvoiceValidationTests.cs` | 4 | - Kiểm tra số lượng bán > 0, đơn giá không âm.<br>- Kiểm tra hóa đơn phải có chi tiết. | In-Memory / Mock |
| **Xác thực & Mã hóa** | `LoginUseCaseTests.cs`, `PasswordHasherTests.cs` | 6 | - Mã hóa PBKDF2 salt ngẫu nhiên, không plain text.<br>- Đăng nhập đúng sai tài khoản/mật khẩu. | In-Memory / Mock |
| **Tiền tệ & Toàn vẹn CSDL** | `DatabaseIntegrityTests.cs` | 3 | - Kiểm tra kiểu `decimal` không bị trôi số học.<br>- Khởi tạo AuditLog và loại điều chỉnh hợp lệ. | In-Memory / Mock |
| **Phông tiếng Việt PDF** | `PdfVietnameseFontTests.cs` | 2 | - Xuất file PDF tiếng Việt có dấu chuẩn Unicode. | In-Memory / Mock |

**Tổng kết:** **32/32 tests đạt 100%**. Toàn bộ logic nghiệp vụ công nợ, FIFO, transaction và audit logging đã sẵn sàng và nghiệm thu hoàn hảo!
