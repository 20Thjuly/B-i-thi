# 🏬 HỆ THỐNG QUẢN LÝ BÁN HÀNG & CÔNG NỢ KHÁCH HÀNG (SALES & ACCOUNTS RECEIVABLE)

> **ĐỒ ÁN MÔN HỌC**: LẬP TRÌNH WEB  
> **ĐỀ TÀI SỐ 18**: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable  
> **NỀN TẢNG**: .NET 8.0 LTS • ASP.NET Core Blazor Server (InteractiveServer) • Microsoft SQL Server • Clean Architecture • Dapper  
> **TRẠNG THÁI DỰ ÁN**: **Hoàn thành 10/10 Giai đoạn (100% Completed)** • **102/102 Tests Passed** • Sẵn sàng Nghiệm thu & Bảo vệ

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-InteractiveServer-purple.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Database](https://img.shields.io/badge/SQL%20Server-2022-red.svg)](https://www.microsoft.com/sql-server/)
[![Architecture](https://img.shields.io/badge/Clean%20Architecture-4%20Layers-success.svg)](#)
[![Tests](https://img.shields.io/badge/Unit%20Tests-102%2F102%20Passed-brightgreen.svg)](#)

---

## 👨‍💻 THÔNG TIN SINH VIÊN THỰC HIỆN

| Mục | Thông tin chi tiết |
| :--- | :--- |
| **Họ và tên sinh viên** | **Đinh Hữu Quang** |
| **Mã số sinh viên (MSSV)** | **23K4080042** |
| **Môn học** | Lập trình Web |
| **Đề tài** | 18. Bán hàng & Công nợ Khách hàng (Sales & AR) |
| **GitHub Repository** | [https://github.com/20Thjuly/B-i-thi](https://github.com/20Thjuly/B-i-thi) |
| **Tài khoản GitHub** | [@20Thjuly](https://github.com/20Thjuly) |

---

## 📑 MỤC LỤC

1. [Giới Thiệu Tổng Quan](#-giới-thiệu-tổng-quan)
2. [Điểm Nổi Bật Kỹ Thuật & Kiến Trúc](#-điểm-nổi-bật-kỹ-thuật--kiến-trúc)
3. [Phân Quyền & 4 Nhóm Vai Trò (RBAC)](#-phân-quyền--4-nhóm-vai-trò-rbac)
4. [Thiết Kế Cơ Sở Dữ Liệu 15 Bảng 3NF](#-thiết-kế-cơ-sở-dữ-liệu-15-bảng-3nf)
5. [Quy Tắc Nghiệp Vụ Cốt Lõi](#-quy-tắc-nghiệp-vụ-cốt-lõi)
6. [Trợ Lý AI & Ranh Giới An Toàn (AI Governance)](#-trợ-lý-ai--ranh-giới-an-toàn-ai-governance)
7. [Dịch Vụ Nền Nhắc Nợ Tự Động (Background Worker)](#-dịch-vụ-nền-nhắc-nợ-tự-động-background-worker)
8. [Hướng Dẫn Cài Đặt & Chạy Local](#-hướng-dẫn-cài-đặt--chạy-local)
9. [Hướng Dẫn Kịch Bản Demo Từng Chức Năng](#-hướng-dẫn-kịch-bản-demo-từng-chức-năng)
10. [Kết Quả Kiểm Thử (102 Tests)](#-kết-quả-kiểm-thử-102-tests)

---

## 🎯 GIỚI THIỆU TỔNG QUAN

Hệ thống **Sales & Accounts Receivable (Sales & AR)** là giải pháp phần mềm quản trị toàn diện chu trình bán hàng và quản lý công nợ doanh nghiệp B2B:
- **Kiểm soát hạn mức tín dụng tức thời**: Chặn lập tức đơn hàng vượt hạn mức; hỗ trợ quy trình xin phê duyệt vượt hạn mức tạm thời có thời hạn.
- **Khấu trừ thanh toán tự động (FIFO)**: Đối trừ phiếu thu vào các hóa đơn cũ nhất đến hạn trước trong một Transaction ACID duy nhất.
- **Phân tích tuổi nợ đa chiều**: Phân nhóm nợ 4 kỳ hạn (`0-30`, `31-60`, `61-90`, `>90` ngày), xuất bảng kê chuyên nghiệp ra Excel (`.xlsx`) và PDF (`.pdf`) chuẩn tiếng Việt.
- **Trung tâm điều hành AR Dashboard**: 10 chỉ số KPI tài chính trọng yếu, chỉ số DSO, xu hướng thu tiền và Mô hình dự báo dòng tiền vào tương lai (Cash Inflow Forecast).
- **Trợ lý AI Công nợ (AI Assistant)**: Tóm tắt sức khỏe tài chính khách hàng, chấm điểm rủi ro theo quy tắc minh bạch v1.0, soạn thảo thư nhắc nợ 3 giọng điệu, có rào cản an toàn bất khả xâm phạm.
- **Tự động nhắc nợ**: Dịch vụ nền chạy định kỳ, chống gửi trùng lặp qua Cooldown Window & IdempotencyKey, tạm dừng khi khách hàng có tranh chấp.
- **Bảo mật & Kiểm toán**: RBAC 4 vai trò, mã hóa mật khẩu PBKDF2/SHA-256 + Salt, lưu vết mọi thay đổi vào `AuditLogs` với màn hình tra cứu Diff JSON.

---

## 🏛 ĐIỂM NỔI BẬT KỸ THUẬT & KIẾN TRÚC

Hệ thống tuân thủ nghiêm ngặt **Clean Architecture** phân tách 4 project độc lập:
1. **`SalesAR.CoreBusiness`**: Entities thuần túy, 0 phụ thuộc bên ngoài.
2. **`SalesAR.UseCases`**: Toàn bộ nghiệp vụ và Interfaces Repository, chỉ tham chiếu CoreBusiness.
3. **`Plugins/SalesAR.Plugins`**: Data Access Dapper, Background Worker, AI Adapters.
4. **`SalesAR.Web`**: Blazor Server UI, Minimal APIs, Composition Root.
5. **`SalesAR.UnitTests`**: 102 bài test tự động bao phủ 100% UseCase và tích hợp SQL Server.

```
UI / Blazor (.razor) ──> UseCases ──> CoreBusiness (Entities)
                           ▲
                           │
             Plugins (Dapper SQL Server, Worker, AI)
```

---

## 🔐 PHÂN QUYỀN & 4 NHÓM VAI TRÒ (RBAC)

Hệ thống được cấu hình sẵn 4 tài khoản mặc định ứng với 4 vai trò độc lập:

| Tên Đăng Nhập | Mật Khẩu | Vai Trò (Role) | Thẩm Quyền & Nhiệm Vụ |
| :--- | :--- | :---: | :--- |
| **`admin`** | `Admin@123` | **`ADMIN`** | Quản lý người dùng, phân quyền, cấu hình hệ thống, xem toàn bộ Nhật ký kiểm toán. |
| **`sales`** | `Sales@123` | **`SALES`** | Quản lý khách hàng, sản phẩm, lập hóa đơn bán hàng, nộp đơn xin duyệt hạn mức. |
| **`accountant`** | `Acc@123` | **`ACCOUNTANT`** | Lập phiếu thu tiền FIFO, quản lý tuổi nợ, xuất Excel/PDF, quản trị nhắc nợ, Dashboard AR. |
| **`creditmanager`**| `Credit@123` | **`CREDIT_MANAGER`** | Thẩm định & phê duyệt đơn vượt hạn mức, đánh giá rủi ro tín dụng, xem Audit Log. |

> **Bảo mật cao**: Cookie xác thực `HttpOnly`, `SameSite=Strict`, `SlidingExpiration=true`. Không bao giờ lộ Stack Trace cho người dùng cuối.

---

## 🗄 THIẾT KẾ CƠ SỞ DỮ LIỆU 15 BẢNG 3NF

Cơ sở dữ liệu `SalesARDB` gồm 15 bảng chuẩn hóa 3NF, bảo toàn số học tiền tệ bằng `DECIMAL(18,2)`:
1. `Customers`: Khách hàng, hạn mức tín dụng, trạng thái tranh chấp.
2. `Products`: Danh mục sản phẩm, đơn giá, tồn kho.
3. `Invoices`: Hóa đơn bán chịu, hạn thanh toán, trạng thái.
4. `InvoiceLines`: Chi tiết dòng hàng và thành tiền.
5. `Payments`: Phiếu thu tiền (Khóa Unique `PaymentNumber` chống trùng).
6. `PaymentAllocations`: Phân bổ thanh toán cho hóa đơn theo FIFO.
7. `Roles`: Danh mục 4 vai trò bảo mật.
8. `Users`: Tài khoản người dùng và mật khẩu băm PBKDF2.
9. `DebtAdjustments`: Lịch sử điều chỉnh nợ, chiết khấu, xóa nợ xấu.
10. `AuditLogs`: Nhật ký kiểm toán mọi thay đổi trạng thái và dữ liệu.
11. `CreditLimitRequests`: Đơn yêu cầu duyệt vượt hạn mức tín dụng.
12. `CreditLimitApprovals`: Lịch sử phê duyệt hạn mức kèm snapshot dư nợ thực tế.
13. `ReminderRules`: Quy tắc các mốc nhắc nợ tự động.
14. `ReminderLogs`: Nhật ký gửi thư nhắc nợ, mã IdempotencyKey.
15. `CreditRiskAssessments`: Lịch sử đánh giá rủi ro tín dụng v1.0.

---

## ⚙️ QUY TẮC NGHIỆP VỤ CỐT LÕI

1. **Kiểm tra Hạn mức tín dụng**: Nếu `CurrentDebt + InvoiceTotal > CreditLimit`, giao dịch bị từ chối trừ khi có phiếu phê duyệt hợp lệ còn hiệu lực (`IsUsed = 0`).
2. **Khóa bi quan chống Race Condition**: Khi duyệt đơn đồng thời, SQL Server áp dụng `WITH (UPDLOCK, HOLDLOCK)`, đảm bảo duy nhất 1 giao dịch thành công.
3. **Phân bổ nợ FIFO nguyên khối**: Hóa đơn cũ nhất đến hạn trước được thanh toán trước. Toàn bộ quá trình diễn ra trong 1 `SqlTransaction` duy nhất (Rollback nếu xảy ra lỗi).
4. **Tách biệt nhiệm vụ (Segregation of Duties)**: Người tạo yêu cầu vượt hạn mức tuyệt đối không được phép tự phê duyệt đơn của mình.

---

## 🤖 TRỢ LÝ AI & RANH GIỚI AN TOÀN (AI GOVERNANCE)

- **Nguyên tắc Grounding**: Số liệu công nợ 100% trích xuất từ SQL Server. Phân tách rõ ràng giữa `GroundTruth` và `AiNarrative`.
- **Rào cản an toàn (Guardrails)**: AI không thể tự sửa hạn mức, không tự ghi nhận thanh toán và không tự hủy đơn hàng.
- **Chấm điểm rủi ro minh bạch**: Mô hình Explainable Rule-based scoring v1.0 (Thang điểm 0 - 100).
- **Bảo mật dữ liệu (PII Masking)**: Che số điện thoại, email, số tài khoản trước khi gửi prompt.
- **Chuyển đổi dự phòng (Fallback)**: Tự động chạy `LocalHeuristicAiProvider` nếu mất mạng hoặc chưa cấu hình API Key.

---

## 🚀 HƯỚNG DẪN CÀI ĐẶT & CHẠY LOCAL

### 1. Yêu cầu Hệ thống
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft SQL Server (LocalDB / Express / Standard) đang chạy tại `localhost` hoặc `.`.

### 2. Thiết lập Database (Chạy tuần tự 8 script SQL)
```powershell
sqlcmd -S . -E -C -i "Database/01_CreateDatabase.sql"
sqlcmd -S . -d SalesARDB -E -C -i "Database/02_CreateTables.sql"
sqlcmd -S . -d SalesARDB -E -C -i "Database/03_CreateRelationships.sql"
sqlcmd -S . -d SalesARDB -E -C -f 65001 -i "Database/04_InsertSampleData.sql"
sqlcmd -S . -d SalesARDB -E -C -f 65001 -i "Database/05_CreateAuthTables.sql"
sqlcmd -S . -d SalesARDB -E -C -f 65001 -i "Database/06_AuditAndIntegrityEnhancement.sql"
sqlcmd -S . -d SalesARDB -E -C -f 65001 -i "Database/07_CreditLimitApprovalWorkflow.sql"
sqlcmd -S . -d SalesARDB -E -C -f 65001 -i "Database/08_AutomatedDebtReminders.sql"
```

### 3. Cấu hình Chuỗi Kết Nối
Kiểm tra file `SalesAR.Web/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SalesARDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Khởi chạy Ứng dụng
```powershell
dotnet run --project SalesAR.Web/SalesAR.Web.csproj --launch-profile https
```
Truy cập trình duyệt:
- **HTTPS**: `https://localhost:7167`
- **HTTP**: `http://localhost:5182`

---

## 🎬 HƯỚNG DẪN KỊCH BẢN DEMO TỪNG CHỨC NĂNG

### Demo 1: Đăng nhập & Phân quyền RBAC
1. Truy cập `/login`. Nhấp vào các nút đăng nhập nhanh đại diện:
   - `admin` $\rightarrow$ Thấy toàn bộ menu, gồm Quản trị và Nhật ký Kiểm toán.
   - `sales` $\rightarrow$ Chỉ thấy Khách hàng, Sản phẩm, Hóa đơn, Xin duyệt hạn mức.
   - `accountant` $\rightarrow$ Thấy Phiếu thu, Tuổi nợ, Nhắc nợ, Báo cáo và Dashboard.
   - `creditmanager` $\rightarrow$ Thấy Danh sách Phê duyệt hạn mức, Trợ lý AI và Audit Log.

### Demo 2: Kiểm soát Hạn mức & Xin Duyệt Tín Dụng
1. Đăng nhập bằng `sales`. Vào **"Lập hóa đơn"** cho khách hàng có dư nợ gần đầy.
2. Chọn số lượng hàng hóa vượt hạn mức $\rightarrow$ Hệ thống cảnh báo đỏ và chặn tạo.
3. Chuyển sang menu **"Yêu cầu Duyệt Hạn mức"** $\rightarrow$ Tạo đơn xin cấp hạn mức tạm thời.
4. Đăng xuất, đăng nhập bằng `creditmanager` $\rightarrow$ Vào danh sách hàng đợi duyệt, xem dư nợ thực tế và bấm **"Phê duyệt"**.
5. Đăng nhập lại bằng `sales` $\rightarrow$ Tạo lại hóa đơn; hệ thống tự nhận diện phiếu duyệt hợp lệ và cho phép xuất hóa đơn thành công!

### Demo 3: Thu tiền & Khấu trừ FIFO Tự Động
1. Đăng nhập bằng `accountant`. Vào **"Lập phiếu thu"**.
2. Chọn khách hàng có nhiều hóa đơn nợ cũ và mới $\rightarrow$ Bảng tự động hiển thị tỷ lệ phân bổ theo thứ tự hóa đơn cũ nhất trước.
3. Bấm **"Ghi nhận thanh toán"** $\rightarrow$ Hệ thống cập nhật trạng thái hóa đơn cũ thành `Paid`, hóa đơn mới thành `PartiallyPaid`.

### Demo 4: Dashboard Quản Trị & Dự Báo Dòng Tiền
1. Đăng nhập bằng `accountant` hoặc `admin`. Vào **"AR Dashboard"**.
2. Xem 10 chỉ số KPI tài chính thời gian thực.
3. Xem biểu đồ tuổi nợ 4 nhóm và bảng Dự báo dòng tiền vào tương lai (Cash Inflow Forecast).

### Demo 5: Trợ Lý AI Công Nợ
1. Vào **"Trợ lý AI Công nợ"** (`/ai-assistance`).
2. Chọn khách hàng $\rightarrow$ Xem Tóm tắt sức khỏe công nợ (GroundTruth đối chiếu AI Narrative).
3. Chuyển sang tab **"Đánh giá rủi ro"** $\rightarrow$ Xem điểm tín dụng, yếu tố rủi ro và rào cản an toàn.
4. Chuyển sang tab **"Soạn thảo thư nhắc nợ"** $\rightarrow$ Chọn giọng điệu (Lịch sự, Trung tính, Cương quyết) để AI sinh nội dung nháp có kiểm soát.

### Demo 6: Nhật Ký Kiểm Toán (Audit Logs)
1. Đăng nhập bằng `admin` hoặc `creditmanager`. Vào **"Nhật ký Kiểm toán"** (`/admin/audit-logs`).
2. Tìm kiếm theo bảng `CreditLimitRequests` hoặc thao tác `STATUS_CHANGE`.
3. Bấm **"Xem Diff"** để xem so sánh JSON giá trị cũ và giá trị mới của bản ghi.

---

## 🧪 KẾT QUẢ KIỂM THỬ (102 TESTS)

Chạy lệnh kiểm thử tự động:
```powershell
dotnet test
```
**Kết quả đạt được**:
```
Passed!  - Failed: 0, Passed: 102, Skipped: 0, Total: 102, Duration: ~2.4 s
```
- 15/15 bài test nghiệp vụ bắt buộc tại `EndToEndAcceptanceBusinessTests.cs`.
- 6/6 bài test tích hợp giao dịch và concurrency khóa bi quan trực tiếp trên SQL Server thật tại `SqlServerAccountsReceivableIntegrationTests.cs`.
- 100% kiểm chứng các yêu cầu vận hành, an ninh và độ toàn vẹn số học tài chính.