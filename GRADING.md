# 📋 TÀI LIỆU ĐỐI SOÁT & CHẤM ĐIỂM ĐỒ ÁN (GRADING.md)

> **MÔN HỌC**: LẬP TRÌNH WEB  
> **ĐỀ TÀI SỐ 18**: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable  
> **SINH VIÊN THỰC HIỆN**: **Đinh Hữu Quang** (MSSV: **23K4080042**)  
> **TÀI KHOẢN GITHUB**: [@20Thjuly](https://github.com/20Thjuly) • **REPOSITORY**: [https://github.com/20Thjuly/B-i-thi](https://github.com/20Thjuly/B-i-thi)  
> **KẾT QUẢ ĐÁNH GIÁ TỰ ĐỘNG**: **102 / 102 Tests PASSED (100%)**

---

## 📊 BẢNG TỔNG HỢP TỰ ĐÁNH GIÁ THEO RUBRIC (K1 – K5 & CÁC TÍNH NĂNG MỞ RỘNG)

| Mã tiêu chí | Nội dung tiêu chí Rubric | Trạng thái | Điểm tối đa | Điểm tự chấm | Vị trí minh chứng cốt lõi |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **K1.1** | Kiến trúc 4 tầng, chiều phụ thuộc chuẩn Clean Architecture | **PASS** | 8 | 8 | 4 file `.csproj`, `SalesAR.CoreBusiness` không phụ thuộc ai |
| **K1.2** | Tách Interface Repository tại UseCases, Blazor `@inject` interface | **PASS** | 7 | 7 | `SalesAR.UseCases/PluginInterfaces/`, các file `.razor` |
| **K1.3** | 2 luật nghiệp vụ cài đặt tại UseCases (Hạn mức & FIFO) | **PASS** | 6 | 6 | `CreateInvoiceUseCase.cs` & `AllocatePaymentUseCase.cs` |
| **K1.4** | Đăng ký Dependency Injection chuẩn mực, Service Lifetime hợp lý | **PASS** | 4 | 4 | `SalesAR.Web/Program.cs` (Scoped, Singleton) |
| **K2.1** | Database 3NF $\ge 5$ bảng (Thực tế: 15 bảng), PK/FK/Index, 8 scripts SQL | **PASS** | 8 | 8 | Thư mục `/Database` (8 file `.sql`, 15 bảng 3NF) |
| **K2.2** | Cặp Header–Line và Thanh toán thực thi trong 1 Database Transaction | **PASS** | 6 | 6 | `InvoiceRepository.cs` & `PaymentRepository.cs` |
| **K2.3** | Tham số hóa 100% câu truy vấn (@Param), chống SQL Injection | **PASS** | 5 | 5 | Toàn bộ Repository trong `Plugins/SalesAR.Plugins/` |
| **K2.4** | Dữ liệu lưu thật vào SQL Server, kiểm chứng trực tiếp qua SSMS | **PASS** | 6 | 6 | Thao tác tạo hóa đơn & xem bảng `Invoices`, `InvoiceLines` |
| **K3.1** | Tối thiểu 3 Reusable Components tự viết | **PASS** | 7 | 7 | 6 components tại `SalesAR.Web/Components/Common/` |
| **K3.2** | Form nhập liệu dùng `EditForm` + `DataAnnotationsValidator` | **PASS** | 7 | 7 | `CreateInvoice.razor`, `CreatePayment.razor`, Model Validation |
| **K3.3** | Hiển thị thông báo lỗi nghiệp vụ rõ ràng trên UI (2 trường hợp) | **PASS** | 6 | 6 | Báo lỗi Vượt hạn mức nợ & Lỗi không đủ hàng trong kho |
| **K3.4** | Responsive hoàn hảo trên thiết bị di động bề ngang hẹp **390px** | **PASS** | 5 | 5 | `wwwroot/app.css` Media Query 390px, table cuộn ngang mượt |
| **K4.1** | Đăng nhập/Đăng xuất, Phân quyền RBAC (4 Roles), Mã hóa mật khẩu | **PASS** | 8 | 8 | `CustomAuthenticationStateProvider.cs`, `PasswordHasher.cs` |
| **K4.2** | Chặn URL trực tiếp cấp Routing & Server (`AuthorizeRouteView`) | **PASS** | 6 | 6 | `Routes.razor`, `@attribute [Authorize(Roles = ...)]` |
| **K4.3** | Xuất báo cáo Aging Report ra file Excel (.xlsx) và PDF (.pdf) | **PASS** | 6 | 6 | `AgingReportExporter.cs` (ClosedXML & QuestPDF) |
| **K5.1** | Bộ tài liệu thiết kế UML chuẩn xác, đồng bộ 100% với Codebase | **PASS** | 2 | 2 | Thư mục `/Documentation/UML/` (4 file `.puml`) |
| **K5.2** | Nắm chắc vị trí file/class/method & Dependency Rule khi vấn đáp | **PASS** | 2 | 2 | `/Documentation/VAN-DAP.md` trả lời 15 câu hỏi then chốt |
| **K5.3** | Lịch sử Git trung thực, rõ ràng, phân chia theo từng đợt làm | **PASS** | 2 | 2 | `git log --oneline` thể hiện chuẩn 10 đợt commit tiến độ |
| **TỔNG** | **ĐÁNH GIÁ TOÀN DIỆN ĐỒ ÁN** | **PASS** | **100** | **100/100** | **ĐẠT XUẤT SẮC TẤT CẢ TIÊU CHÍ** |

---

## 🌟 CÁC TÍNH NĂNG MỞ RỘNG VƯỢT TRỘI (NÂNG CAO)

Ngoài 100 điểm chuẩn của khung Rubric, hệ thống đã hoàn thiện xuất sắc các tính năng cấp doanh nghiệp:

1. **Quy trình Phê duyệt Hạn mức Tín dụng & Segregation of Duties (Part 5)**:
   - Cơ chế duyệt nợ vượt hạn mức có thời hạn hiệu lực `ExpiresAt`.
   - Nguyên tắc Tách biệt nhiệm vụ (SoD): Người tạo đơn không được tự phê duyệt đơn của mình.
   - Kiểm tra dư nợ thực tế (`liveDebt`) tại thời điểm quyết định thay vì snapshot cũ.
   - Khóa bi quan `WITH (UPDLOCK, HOLDLOCK)` chống Race Condition khi duyệt đồng thời.
   - Khóa lạc quan chống tái sử dụng approval (`IsUsed = 1`).

2. **Dịch vụ Nền Tự Động Hóa Nhắc Nợ Công Nợ (Part 6)**:
   - Dịch vụ nền `BackgroundReminderJobScheduler` (`IHostedService`) quét định kỳ.
   - 4 mốc nhắc nợ: Sắp đến hạn (-3 ngày), Quá hạn lần 1 (+1 ngày), Quá hạn lần 2 (+8 ngày), Nợ khó đòi (+31 ngày).
   - Cơ chế **Cooldown Window** và **IdempotencyKey** chống gửi nhắc nợ trùng lặp.
   - Cơ chế phát hiện tranh chấp (`IsInDispute = 1`) tự động tạm hoãn gửi nhắc nợ.
   - Cơ chế Retry lũy tiến khi gặp sự cố mạng/SMTP.

3. **Trung Tâm Điều Hành AR Dashboard & Dự Báo Dòng Tiền (Part 7)**:
   - 10 chỉ số KPI tài chính trọng yếu tính từ dữ liệu thật 100% của SQL Server.
   - Chỉ số DSO (Days Sales Outstanding) đo lường số ngày thu hồi nợ trung bình.
   - Biểu đồ tuổi nợ 4 nhóm ngày và Top khách hàng nợ nhiều nhất.
   - Mô hình Dự báo thu tiền tương lai (Cash Inflow Forecast) theo phương pháp Xác suất thu nợ có trọng số (Weighted Probability Inflow Model).

4. **Trợ Lý AI Phân Tích Công Nợ & Rào Cản An Toàn (Part 8)**:
   - Phân định rõ ràng giữa `GroundTruth` (số liệu thật) và `AiNarrative` (diễn giải thông minh).
   - Mô hình Chấm điểm rủi ro tín dụng theo quy tắc minh bạch `v1.0-RuleBasedScoring` (Thang điểm 0 - 100).
   - Rào cản an toàn (AI Guardrails): AI không thể tự sửa hạn mức, không tự ghi nhận thanh toán, không tự hủy đơn.
   - Khử nhạy cảm thông tin cá nhân (PII Masking) trước khi truyền prompt qua API ngoại vi.
   - Chuyển đổi dự phòng (Graceful Fallback) tức thì sang `LocalHeuristicAiProvider` nếu mất mạng.

5. **Giao Diện Nhật Ký Kiểm Toán (Audit Log Viewer) (Part 9)**:
   - Phân trang máy chủ (Server-side pagination) chịu tải dữ liệu lớn.
   - Bộ lọc đa tiêu chí: Tên bảng, Thao tác (`INSERT`, `UPDATE`, `STATUS_CHANGE`), Người thực hiện, Khoảng ngày.
   - Cửa sổ Modal hiển thị so sánh Diff trực quan giữa `OldValues` và `NewValues`.

6. **Bộ Kiểm Thử Toàn Diện 102/102 Tests (Part 10)**:
   - 15/15 kịch bản kiểm thử nghiệp vụ bắt buộc tại `EndToEndAcceptanceBusinessTests.cs`.
   - 6/6 kịch bản kiểm thử tích hợp giao dịch ACID và Concurrency locking trên SQL Server thật tại `SqlServerAccountsReceivableIntegrationTests.cs`.
   - 100% test pass trong ~2.4 giây.

---

## 🔍 MINH CHỨNG CHI TIẾT THEO TỪNG TIÊU CHÍ RUBRIC

### NHÓM K1: KIẾN TRÚC & DEPENDENCY INJECTION (25 ĐIỂM)

#### 1. Tiêu chí K1.1: Phân tách 4 Project & Chiều phụ thuộc Clean Architecture (8 điểm)
- Cấu trúc solution:
  1. `SalesAR.CoreBusiness/SalesAR.CoreBusiness.csproj` (Domain): **0 references**.
  2. `SalesAR.UseCases/SalesAR.UseCases.csproj` (Application): **Chỉ tham chiếu duy nhất `SalesAR.CoreBusiness`**.
  3. `Plugins/SalesAR.Plugins/SalesAR.Plugins.csproj` (Infrastructure): Tham chiếu `SalesAR.CoreBusiness` và `SalesAR.UseCases`.
  4. `SalesAR.Web/SalesAR.Web.csproj` (Presentation / Composition Root): Tham chiếu 3 project trên.
  5. `SalesAR.UnitTests/SalesAR.UnitTests.csproj` (Testing): Tham chiếu CoreBusiness, UseCases, Plugins, Web.
- Chiều phụ thuộc:
  $$\text{Web} \longrightarrow \text{UseCases} \longrightarrow \text{CoreBusiness} \longleftarrow \text{Plugins}$$

#### 2. Tiêu chí K1.2: Repository Interface tại UseCases & Blazor Injection (7 điểm)
- Khai báo Interface: Nằm toàn bộ trong thư mục `SalesAR.UseCases/PluginInterfaces/` (gồm 12 interface: `ICustomerRepository`, `IProductRepository`, `IInvoiceRepository`, `IPaymentRepository`, `IAgingReportRepository`, `ICreditLimitRequestRepository`, `IReminderRepository`, `IArDashboardRepository`, `IAuditLogRepository`, `ICreditRiskAssessmentRepository`, `IUserRepository`, `IRoleRepository`).
- Triển khai cụ thể: Nằm trong `Plugins/SalesAR.Plugins/`.
- Mọi trang `.razor` trong `SalesAR.Web/Components/Pages/` CHỈ inject UseCases hoặc Abstractions, tuyệt đối không inject trực tiếp lớp Repository.

#### 3. Tiêu chí K1.3: 2 Luật nghiệp vụ cài đặt tại UseCases (6 điểm)
- Luật 1: Kiểm tra Hạn mức nợ cài đặt trong `CreateInvoiceUseCase.cs`.
- Luật 2: Phân bổ thanh toán FIFO cài đặt trong `AllocatePaymentUseCase.cs`.

#### 4. Tiêu chí K1.4: Đăng ký Dependency Injection chuẩn mực (4 điểm)
- `ISqlConnectionFactory`: **Singleton** (thread-safe, không giữ state tạm).
- `IPasswordHasher`: **Singleton** (pure logic).
- `IHostedService` (`BackgroundReminderJobScheduler`): **Singleton** (chạy nền).
- Các Repositories và UseCases: **Scoped** (gắn với phiên Circuit của Blazor).
- `AuthenticationStateProvider`: **Scoped** (danh tính người dùng).

---

### NHÓM K2: CƠ SỞ DỮ LIỆU & GIAO DỊCH (25 ĐIỂM)

#### 1. Tiêu chí K2.1: Database 3NF 15 bảng & 8 scripts SQL (8 điểm)
- 15 bảng 3NF trong `SalesARDB`.
- 8 scripts SQL chạy độc lập trong `Database/01_...` đến `Database/08_...`.
- 100% cột tiền tệ dùng `DECIMAL(18,2)`.

#### 2. Tiêu chí K2.2: Header-Line & Thanh toán thực thi trong 1 Database Transaction (6 điểm)
- `InvoiceRepository.cs`: Tạo `Invoice` và toàn bộ `InvoiceLines` trong 1 `SqlTransaction`.
- `PaymentRepository.cs`: Tạo `Payment` và toàn bộ `PaymentAllocations` trong 1 `SqlTransaction`. Rollback nếu có bất kỳ lỗi nào.

#### 3. Tiêu chí K2.3: Tham số hóa 100% câu truy vấn (@Param) (5 điểm)
- Toàn bộ câu lệnh SQL trong `Plugins/SalesAR.Plugins/` sử dụng Dapper parameterized queries (`@CustomerId`, `@Amount`, v.v.), loại trừ hoàn toàn nguy cơ SQL Injection.

#### 4. Tiêu chí K2.4: Dữ liệu lưu thật vào SQL Server (6 điểm)
- Thao tác thực tế qua giao diện được ghi nhận ngay lập tức vào database thật `SalesARDB`, kiểm chứng được qua SSMS hoặc câu lệnh `sqlcmd`.

---

### NHÓM K3: GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG (25 ĐIỂM)

#### 1. Tiêu chí K3.1: Hệ thống Reusable Components tự viết (7 điểm)
- `ConfirmModal.razor`: Hộp thoại xác nhận thao tác nhạy cảm (duyệt đơn, xóa).
- `ToastNotification.razor`: Thông báo thành công / lỗi tự động ẩn sau 4 giây.
- `Pagination.razor`: Phân trang dữ liệu.
- `MetricCard.razor`: Thẻ hiển thị KPI tài chính.
- `EmptyState.razor`: Trạng thái không có dữ liệu.
- `LoadingSpinner.razor`: Trạng thái đang tải dữ liệu.

#### 2. Tiêu chí K3.2: Form nhập liệu EditForm + DataAnnotations (7 điểm)
- `CreateInvoice.razor`, `CreatePayment.razor`, `AuditLogs.razor`.
- Thông báo lỗi hiển thị bằng tiếng Việt rõ ràng, dễ hiểu.

#### 3. Tiêu chí K3.3: Thông báo lỗi nghiệp vụ rõ ràng (6 điểm)
- Thông báo rõ số tiền vượt hạn mức, nguyên nhân từ chối, không bao giờ hiển thị Stack Trace trên UI.

#### 4. Tiêu chí K3.4: Responsive chuẩn màn hình di động 390px (5 điểm)
- File `wwwroot/app.css` chứa các media query `@media (max-width: 480px)` tối ưu cho màn hình iPhone Mini / 390px: bảng dữ liệu cuộn ngang mượt mà, thanh lọc xếp chồng gọn gàng, nút bấm to dễ thao tác.

---

### NHÓM K4: BẢO MẬT, PHÂN QUYỀN & BÁO CÁO (20 ĐIỂM)

#### 1. Tiêu chí K4.1: Xác thực, Phân quyền RBAC 4 Roles & Băm mật khẩu (8 điểm)
- 4 vai trò: `ADMIN`, `SALES`, `ACCOUNTANT`, `CREDIT_MANAGER`.
- Mật khẩu băm chuẩn PBKDF2 with HMAC-SHA256 kết hợp Salt ngẫu nhiên 128-bit, 10.000 vòng lặp.
- Cookie bảo mật cao: `HttpOnly = true`, `SameSite = Strict`.

#### 2. Tiêu chí K4.2: Chặn URL trực tiếp cấp Routing & Server (6 điểm)
- `Routes.razor` sử dụng `AuthorizeRouteView`.
- Từng trang được gán `@attribute [Authorize(Roles = "...")]`.
- Từng UseCase nhạy cảm kiểm tra vai trò tại server-side.

#### 3. Tiêu chí K4.3: Xuất báo cáo Excel & PDF (6 điểm)
- Excel (`.xlsx`): Sử dụng `ClosedXML`, định dạng tiền tệ Việt Nam `#,##0 đ`.
- PDF (`.pdf`): Sử dụng `QuestPDF`, thiết kế khổ ngang A4 Landscape, hỗ trợ font tiếng Việt Unicode 100%.

---

### NHÓM K5: TÀI LIỆU, GIT & PHONG CÁCH LÀM VIỆC (5 ĐIỂM)

#### 1. Tiêu chí K5.1: Bộ tài liệu UML đồng bộ 100% với Code (2 điểm)
- Thư mục `/Documentation/UML/`: 4 sơ đồ PlantUML (`01-use-case-diagram.puml`, `02-class-diagram.puml`, `03-sequence-diagram-fifo.puml`, `04-er-diagram.puml`).

#### 2. Tiêu chí K5.2: Tài liệu Vấn đáp & Nắm chắc kiến trúc (2 điểm)
- File `Documentation/VAN-DAP.md`: Trả lời chi tiết 15 câu hỏi trọng tâm của hội đồng chấm thi.

#### 3. Tiêu chí K5.3: Lịch sử Git trung thực, rõ ràng (1 điểm)
- Commit tuần tự theo từng Part trên nhánh `main`, đồng bộ hoàn toàn với repository GitHub `https://github.com/20Thjuly/B-i-thi.git`.
