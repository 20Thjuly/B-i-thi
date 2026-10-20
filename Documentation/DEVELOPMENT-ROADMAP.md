# 🗺 LỘ TRÌNH PHÁT TRIỂN & TIẾN ĐỘ THỰC HIỆN DỰ ÁN (DEVELOPMENT ROADMAP)
## ĐỒ ÁN MÔN HỌC: LẬP TRÌNH WEB
### Đề tài 18: Bán hàng & Quản lý Công nợ Phải thu Khách hàng (Sales & Accounts Receivable)

> **Sinh viên thực hiện**: **Đinh Hữu Quang** (MSSV: **23K4080042**)  
> **Repository GitHub**: [https://github.com/20Thjuly/B-i-thi](https://github.com/20Thjuly/B-i-thi)  
> **Nền tảng kỹ thuật**: .NET 8.0 LTS • C# 12 • ASP.NET Core Blazor Server • Microsoft SQL Server • Dapper • QuestPDF • ClosedXML  
> **Trạng thái**: Hoàn thành 10/10 Giai đoạn (100% Completed) • 102/102 Tests Passed • Sẵn sàng nghiệm thu

---

## 📌 BẢNG TỔNG HỢP TIẾN ĐỘ 10 PHẦN DỰ ÁN

| Giai đoạn (Part) | Nội dung trọng tâm | Trạng thái | Số Test Unit/Integration | Tài liệu nghiệm thu liên quan |
| :---: | :--- | :---: | :---: | :--- |
| **PART 1** | Khảo sát hệ thống hiện có, kiểm toán kiến trúc, loại bỏ nợ kỹ thuật | **PASS** | Baseline | `Documentation/INITIAL-AUDIT.md` |
| **PART 2** | Tái cấu trúc 4 tầng Clean Architecture & Plugin Architecture | **PASS** | 20 Tests | `Documentation/ARCHITECTURE.md` |
| **PART 3** | Toàn vẹn cơ sở dữ liệu 3NF, bảo toàn số học `decimal(18,2)`, Audit Log | **PASS** | 23 Tests | `Documentation/DATABASE-CHANGES.md` |
| **PART 4** | Nghiệp vụ cốt lõi: Hóa đơn, Thu tiền & Cấn trừ nợ tự động FIFO | **PASS** | 35 Tests | `Documentation/ACCOUNTS-RECEIVABLE-CORE.md` |
| **PART 5** | Quy trình phê duyệt hạn mức tín dụng (Credit Limit Approval & SoD) | **PASS** | 45 Tests | `Documentation/CREDIT-LIMIT-WORKFLOW.md` |
| **PART 6** | Tự động hóa nhắc nợ công nợ, Cooldown Window & Background Service | **PASS** | 58 Tests | `Documentation/AUTOMATED-DEBT-REMINDERS.md` |
| **PART 7** | Dashboard quản trị công nợ & Dự báo dòng tiền (Cash Forecast) | **PASS** | 74 Tests | `Documentation/AR-DASHBOARD-FORECAST.md` |
| **PART 8** | Trợ lý AI phân tích công nợ, Chấm điểm rủi ro minh bạch & Guardrails | **PASS** | 84 Tests | `Documentation/AI-GOVERNANCE.md` |
| **PART 9** | Hoàn thiện UI/UX (Mobile 390px), RBAC 4 Roles & Nhật ký kiểm toán | **PASS** | 84 Tests | `Documentation/SECURITY-AND-AUDIT.md` |
| **PART 10** | Kiểm thử End-to-End, Kiểm chứng Concurrency SQL Server & Nghiệm thu | **PASS** | **102 Tests** | `Documentation/TEST-REPORT.md`, `GRADING.md` |

---

## 🔍 CHI TIẾT NỘI DUNG TỪNG GIAI ĐOẠN

### PART 1 — INITIAL AUDIT & GAP ANALYSIS
- **Mục tiêu**: Khảo sát hiện trạng code ban đầu, xác định các điểm nghẽn kiến trúc và nợ kỹ thuật.
- **Kết quả đạt được**:
  - Phát hiện giao diện Blazor ban đầu gọi trực tiếp DbContext/Repository gây vi phạm kiến trúc.
  - Phân tích hiện tượng mất an toàn khi truyền mật khẩu thô và lưu số tiền ở dạng số thực trôi nổi.
  - Lập tài liệu phân tích rủi ro và xác lập ranh giới tái cấu trúc tại `Documentation/INITIAL-AUDIT.md`.

### PART 2 — CLEAN ARCHITECTURE FOUNDATION
- **Mục tiêu**: Phân tách solution thành 4 project độc lập tuân thủ nghiêm ngặt nguyên tắc Clean Architecture và Dependency Inversion.
- **Kết quả đạt được**:
  - `SalesAR.CoreBusiness`: Chứa Entities thuần túy, 0 phụ thuộc bên ngoài.
  - `SalesAR.UseCases`: Chứa Business Rules và Interfaces Repository, chỉ phụ thuộc CoreBusiness.
  - `Plugins/SalesAR.Plugins`: Hiện thực hóa Data Access với Dapper và SQL Server.
  - `SalesAR.Web`: Composition Root, Blazor Server UI chỉ giao tiếp qua UseCases.
  - Thiết lập dự án kiểm thử tự động `SalesAR.UnitTests`.

### PART 3 — DATABASE INTEGRITY & NUMERIC PRECISION
- **Mục tiêu**: Chuẩn hóa cơ sở dữ liệu lên 3NF, triệt tiêu sai số tiền tệ.
- **Kết quả đạt được**:
  - Chuyển đổi 100% các cột tiền sang `DECIMAL(18,2)`.
  - Bổ sung bảng `AuditLogs` và `DebtAdjustments`.
  - Tạo composite indexes cho hóa đơn và cấn trừ thanh toán.
  - Soạn thảo script migration lũy kế `06_AuditAndIntegrityEnhancement.sql`.

### PART 4 — ACCOUNTS RECEIVABLE CORE & FIFO ALLOCATION
- **Mục tiêu**: Cài đặt quy trình cốt lõi về bán chịu, hóa đơn và thu hồi nợ.
- **Kết quả đạt được**:
  - `CreateInvoiceUseCase`: Kiểm tra hạn mức khả dụng trước khi ghi nhận nợ.
  - `AllocatePaymentUseCase`: Thuật toán phân bổ thanh toán FIFO tự động cho hóa đơn cũ nhất trước.
  - Đảm bảo thực thi trong một Database Transaction nguyên khối (Atomicity).
  - Xuất báo cáo tuổi nợ ra file Excel (.xlsx) và PDF (.pdf) hỗ trợ tiếng Việt Unicode hoàn chỉnh.

### PART 5 — CREDIT LIMIT APPROVAL WORKFLOW
- **Mục tiêu**: Xây dựng quy trình xin vượt hạn mức tạm thời khi phát sinh giao dịch lớn.
- **Kết quả đạt được**:
  - Bổ sung 2 bảng `CreditLimitRequests` và `CreditLimitApprovals`.
  - Thực thi nguyên tắc Tách biệt nhiệm vụ (Segregation of Duties - SoD): Người tạo đơn (`RequestedBy`) tuyệt đối không thể tự duyệt đơn của mình.
  - Kiểm tra lại dư nợ thực tế (`liveDebt`) tại thời điểm phê duyệt thay vì tin tưởng snapshot cũ.
  - Khóa bi quan `WITH (UPDLOCK, HOLDLOCK)` chống Race Condition khi duyệt đồng thời.
  - Cơ chế Anti-Replay: Phiếu duyệt chỉ được sử dụng đúng 1 lần duy nhất cho 1 hóa đơn (`IsUsed = 1`).

### PART 6 — AUTOMATED DEBT REMINDERS & WORKER
- **Mục tiêu**: Tự động hóa phân tầng nhắc nợ theo hạn thanh toán với các mức độ âm điệu khác nhau.
- **Kết quả đạt được**:
  - Bổ sung bảng `ReminderRules` và `ReminderLogs`.
  - Hỗ trợ 4 mốc cấu hình: Sắp đến hạn (-3 ngày), Quá hạn nhẹ (+1 ngày), Quá hạn vừa (+8 ngày), Nợ khó đòi (+31 ngày).
  - Cơ chế **Cooldown Window** và **IdempotencyKey** chống gửi nhắc nợ trùng lặp cho cùng một hóa đơn/khách hàng.
  - Tự động tạm dừng gửi nhắc nợ khi khách hàng hoặc hóa đơn đang có tranh chấp (`IsInDispute = 1`).
  - Dịch vụ nền `BackgroundReminderJobScheduler` (IHostedService) tự động quét và retry lũy tiến khi gặp sự cố mạng/SMTP.

### PART 7 — AR DASHBOARD & CASH COLLECTION FORECAST
- **Mục tiêu**: Xây dựng trung tâm điều hành công nợ và dự báo dòng tiền từ dữ liệu SQL Server thật 100%.
- **Kết quả đạt được**:
  - Tính toán 10 chỉ số KPI tài chính trọng yếu: Tổng nợ, Nợ quá hạn, Tiền đã thu, Doanh thu tín dụng, Tỷ lệ quá hạn, DSO (Days Sales Outstanding).
  - Phân tích tuổi nợ 4 nhóm (`0-30`, `31-60`, `61-90`, `>90` ngày).
  - Top khách hàng nợ nhiều nhất và tỷ trọng công nợ toàn hệ thống.
  - Biểu đồ xu hướng dòng tiền thu thực tế theo 6 tháng gần nhất.
  - Mô hình Dự báo thu tiền tương lai (Cash Inflow Forecast) theo phương pháp Xác suất thu nợ có trọng số (Weighted Probability Inflow Model) dựa trên hành vi lịch sử.

### PART 8 — AI ASSISTANCE FOR ACCOUNTS RECEIVABLE
- **Mục tiêu**: Ứng dụng AI hỗ trợ phân tích công nợ, soạn thảo thư tín, tuân thủ nghiêm ngặt ranh giới an toàn.
- **Kết quả đạt được**:
  - Kiến trúc phân tách rõ ràng giữa `GroundTruth` (số liệu thật từ SQL Server) và `AiNarrative` (diễn giải thông minh).
  - Mô hình chấm điểm rủi ro tín dụng theo quy tắc minh bạch `v1.0-RuleBasedScoring` (Thang điểm 0 - 100).
  - Rào cản an toàn (Guardrails): AI tuyệt đối không thể tự sửa hạn mức, không tự ghi nhận thanh toán và không tự ý hủy đơn hàng.
  - Làm sạch dữ liệu cá nhân (PII Masking) trước khi truyền prompt qua API ngoại vi.
  - Cơ chế chuyển đổi dự phòng tức thì sang Heuristic Engine khi AI mất mạng hoặc hết quota.

### PART 9 — UI/UX, AUTHORIZATION & SECURITY
- **Mục tiêu**: Hoàn thiện trải nghiệm sử dụng, bảo mật nhiều lớp và phân quyền hệ thống.
- **Kết quả đạt được**:
  - Tối ưu hóa Responsive hoàn hảo cho màn hình di động bề ngang hẹp **390px** (iPhone Mini / Pro).
  - Hệ thống kiểm soát truy cập dựa trên vai trò (RBAC) với 4 vai trò độc lập: `ADMIN`, `SALES`, `ACCOUNTANT`, `CREDIT_MANAGER`.
  - Cơ chế Cookie Authentication bảo mật cao: `HttpOnly = true`, `SameSite = Strict`, `SlidingExpiration = true`.
  - Trang xem Nhật ký kiểm toán (`/admin/audit-logs`) phân trang máy chủ, hỗ trợ lọc đa tiêu chí và so sánh Diff JSON Old/New values.
  - Che giấu hoàn toàn Stack Trace trên giao diện `Error.razor`, chỉ hiển thị Request ID.

### PART 10 — END-TO-END TESTING & FINAL ACCEPTANCE
- **Mục tiêu**: Kiểm thử toàn diện 15 kịch bản nghiệp vụ bắt buộc và nghiệm thu toàn bộ hệ thống.
- **Kết quả đạt được**:
  - Build thành công 100% solution với 0 cảnh báo, 0 lỗi.
  - Chạy thành công toàn bộ **102/102 Tests** tự động.
  - Thực thi kiểm thử tích hợp thực tế với SQL Server: kiểm chứng khóa bi quan chống Race Condition khi phê duyệt đồng thời, khóa lạc quan chống tái sử dụng phiếu duyệt, ràng buộc Unique chống trùng phiếu thu, và cơ chế Rollback Transaction khi gặp lỗi.
  - Đối chiếu khớp 100% giữa số liệu tổng hợp trên Dashboard và kết quả câu truy vấn SQL thô trong CSDL.
  - Hoàn thiện bộ 7 tài liệu nghiệm thu kỹ thuật và tài liệu hướng dẫn vấn đáp.
