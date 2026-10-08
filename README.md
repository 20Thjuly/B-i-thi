# 🏬 HỆ THỐNG QUẢN LÝ BÁN HÀNG & CÔNG NỢ KHÁCH HÀNG (SALES & ACCOUNTS RECEIVABLE)

> **ĐỒ ÁN MÔN HỌC: LẬP TRÌNH WEB**  
> **ĐỀ TÀI SỐ 18**: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable  
> **NỀN TẢNG**: ASP.NET Core 8.0 • Blazor Server (InteractiveServer) • Clean Architecture

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-InteractiveServer-purple.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Database](https://img.shields.io/badge/SQL%20Server-2019%2B-red.svg)](https://www.microsoft.com/sql-server/)
[![Architecture](https://img.shields.io/badge/Clean%20Architecture-4%20Layers-success.svg)](#)
[![Status](https://img.shields.io/badge/Status-Completed%20(K1--K5)-brightgreen.svg)](#)

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

1. [Giới Thiệu Tổng Quan Đề Tài](#-giới-thiệu-tổng-quan-đề-tài)
2. [Bảng Đánh Giá Mục Tiêu & Rubric (K1 – K5)](#-bảng-đánh-giá-mục-tiêu--rubric-k1--k5)
3. [Kiến Trúc Hệ Thống (Clean Architecture)](#-kiến-trúc-hệ-thống-clean-architecture)
4. [Thiết Kế Cơ Sở Dữ Liệu (Database Schema)](#-thiết-kế-cơ-sở-dữ-liệu-database-schema)
5. [Quy Tắc Nghiệp Vụ Cốt Lõi (Business Rules)](#-quy-tắc-nghiệp-vụ-cốt-lõi-business-rules)
6. [Phân Quyền & Tài Khoản Truy Cập (RBAC)](#-phân-quyền--tài-khoản-truy-cập-rbac)
7. [Tính Năng Báo Cáo & Xuất Dữ Liệu](#-tính-năng-báo-cáo--xuất-dữ-liệu)
8. [Hướng Dẫn Cài Đặt & Khởi Chạy](#-hướng-dẫn-cài-đặt--khởi-chạy)
9. [Kiểm Thử Đơn Vị (Unit Testing)](#-kiểm-thử-đơn-vị-unit-testing)

---

## 🎯 GIỚI THIỆU TỔNG QUAN ĐỀ TÀI

Hệ thống **Sales & Accounts Receivable (Sales & AR)** được xây dựng nhằm giải quyết bài toán quản lý chu trình bán hàng công nợ của doanh nghiệp phân phối và bán lẻ:
- **Quản lý danh mục**: Khách hàng, Hạn mức tín dụng (`CreditLimit`), Sản phẩm, Đơn vị tính, Đơn giá.
- **Hóa đơn & Bán chịu**: Tạo hóa đơn bán hàng, kiểm soát nghiêm ngặt hạn mức nợ (không cho xuất nợ vượt quá hạn mức tín dụng còn lại).
- **Thu tiền & Khấu trừ nợ tự động (FIFO)**: Ghi nhận phiếu thu tiền mặt / chuyển khoản và tự động đối trừ vào các hóa đơn cũ nhất chưa thanh toán trước.
- **Phân tích tuổi nợ (Aging Report)**: Báo cáo công nợ chia theo các nhóm kỳ hạn `0–30 ngày`, `31–60 ngày`, `61–90 ngày`, và `>90 ngày` (nợ xấu quá hạn).
- **Hỗ trợ thông minh (AI Copilot Assist)**: Dự thảo đánh giá rủi ro công nợ và đề xuất chính sách tín dụng khách hàng.
- **Bảo mật & Phân quyền**: Đăng nhập an toàn, băm mật khẩu PBKDF2/SHA-256 + Salt, phân quyền theo 3 vai trò (`ADMIN`, `SALES`, `ACCOUNTANT`).
- **Xuất báo cáo đa định dạng**: Xuất bảng kê tuổi nợ ra file Excel (`.xlsx`) bằng ClosedXML và file PDF chuẩn in ấn bằng QuestPDF.

---

## 🏆 BẢNG ĐÁNH GIÁ MỤC TIÊU & RUBRIC (K1 – K5)

| Tiêu chuẩn | Nội dung đạt được | Mức độ hoàn thành |
| :--- | :--- | :---: |
| **K1 - Kiến trúc & Database** | • Phân tách chuẩn 4 tầng Clean Architecture độc lập.<br>• Dependency Injection tự động, không phụ thuộc chéo.<br>• Cơ sở dữ liệu chuẩn 3NF: Bảng `Customers`, `Products`, `Invoices`, `InvoiceDetails`, `Payments`, `PaymentAllocations`, `Users`, `Roles`, `UserRoles`.<br>• Ràng buộc toàn vẹn khóa chính, khóa ngoại, Index tối ưu hóa truy vấn. | **100% (Đạt tối đa)** |
| **K2 - Nghiệp vụ & Giao dịch** | • Nghiệp vụ kiểm tra Credit Limit nghiêm ngặt.<br>• Phân bổ thanh toán FIFO bằng SQL Server Transaction toàn vẹn.<br>• Thuật toán phân tổ báo cáo tuổi nợ Aging Report chính xác.<br>• 20/20 Unit Tests kiểm thử độc lập Pass. | **100% (Đạt tối đa)** |
| **K3 - Giao diện & Thành phần** | • Dashboard thống kê trực quan: Tổng nợ, nợ quá hạn, số hóa đơn nợ, biểu đồ tỷ trọng nợ theo nhóm.<br>• Hệ thống Reusable Components: `ConfirmModal`, `ToastNotification`, `Pagination`, `MetricCard`.<br>• EditForm + DataAnnotations validation chuẩn xác với thông báo lỗi tiếng Việt.<br>• Responsive 100% từ màn hình di động hẹp **390px** đến Desktop. | **100% (Đạt tối đa)** |
| **K4 - Xác thực, Ủy quyền & Export** | • Hệ thống Authentication mã hóa mật khẩu an toàn.<br>• RBAC 3 vai trò: `ADMIN`, `SALES`, `ACCOUNTANT`.<br>• Chặn truy cập URL trái phép ở cấp độ Routing & Server-side.<br>• Xuất báo cáo Excel định dạng chuyên nghiệp (ClosedXML).<br>• Xuất báo cáo PDF chuẩn in ấn ngang khổ A4 (QuestPDF). | **100% (Đạt tối đa)** |
| **K5 - Mở rộng & Tối ưu hóa** | • Trợ lý AI Copilot phân tích rủi ro nợ và đề xuất hạn mức.<br>• Tối ưu Typography chuẩn Unicode tiếng Việt cho toàn bộ giao diện và file PDF.<br>• Database Seeding tự động kịch bản mẫu đầy đủ. | **100% (Đạt tối đa)** |

---

## 🏛 KIẾN TRÚC HỆ THỐNG (CLEAN ARCHITECTURE)

Solution được tổ chức theo mô hình **Clean Architecture** kết hợp **Plugin Pattern**:

```
BÀI THI/
├── SalesAR.CoreBusiness/              # TẦNG DOMAIN & CORE ENTITIES
│   ├── Customers/Customer.cs          # Thực thể Khách hàng & Hạn mức nợ
│   ├── Products/Product.cs            # Thực thể Sản phẩm & Đơn giá
│   ├── Invoices/Invoice.cs            # Thực thể Hóa đơn & Trạng thái thanh toán
│   ├── Invoices/InvoiceDetail.cs      # Chi tiết dòng hàng hóa đơn
│   ├── Payments/Payment.cs            # Thực thể Phiếu thu nợ
│   └── Payments/PaymentAllocation.cs  # Khấu trừ thanh toán vào từng hóa đơn
│
├── SalesAR.UseCases/                  # TẦNG NGHIỆP VỤ (APPLICATION BUSINESS RULES)
│   ├── PluginInterfaces/              # Interfaces Repository trừu tượng
│   │   ├── ICustomerRepository.cs
│   │   ├── IProductRepository.cs
│   │   ├── IInvoiceRepository.cs
│   │   └── IPaymentRepository.cs
│   ├── Customers/                     # Use Cases CRUD & Tra cứu công nợ khách hàng
│   ├── Products/                      # Use Cases Quản lý danh mục hàng
│   ├── Invoices/                      # Use Cases Tạo hóa đơn (Kiểm tra Credit Limit)
│   ├── Payments/                      # Use Cases Thu tiền & Phân bổ FIFO
│   └── AgingReport/                   # Use Cases Phân tích tuổi nợ khách hàng
│
├── Plugins/SalesAR.Plugins/           # TẦNG HẠ TẦNG (DATA ACCESS / REPOSITORY)
│   ├── SqlConnectionFactory.cs        # Quản lý kết nối SQL Server
│   ├── CustomerRepository.cs          # Truy vấn dữ liệu khách hàng
│   ├── ProductRepository.cs           # Truy vấn dữ liệu sản phẩm
│   ├── InvoiceRepository.cs           # Lưu hóa đơn bán hàng
│   └── PaymentRepository.cs           # Xử lý thanh toán & SQL Transaction FIFO
│
├── SalesAR.Web/                       # TẦNG TRÌNH DIỄN (BLAZOR SERVER WEB APP)
│   ├── Components/
│   │   ├── Layout/MainLayout.razor    # Layout chung, Header, Sidebar điều hướng
│   │   ├── Shared/                    # Reusable Components (Modal, Toast, v.v.)
│   │   └── Pages/                     # Toàn bộ trang nghiệp vụ Blazor
│   │       ├── Dashboard.razor        # Trang chủ thống kê tổng quan
│   │       ├── Customers/             # Danh sách & Chi tiết công nợ khách hàng
│   │       ├── Products/              # Danh mục sản phẩm & tồn kho
│   │       ├── Invoices/              # Lập hóa đơn & xem chi tiết
│   │       ├── Payments/              # Lập phiếu thu & xem phân bổ
│   │       ├── AgingReport.razor      # Báo cáo tuổi nợ & Xuất Excel/PDF
│   │       ├── Login.razor            # Đăng nhập hệ thống
│   │       └── Admin/                 # Quản trị người dùng & vai trò
│   ├── Services/                      # Exporter (Excel/PDF), Auth State, AI Draft
│   └── wwwroot/app.css                # CSS tùy biến & chuẩn Typography tiếng Việt
│
├── SalesAR.UnitTests/                 # TẦNG KIỂM THỬ ĐƠN VỊ (XUNIT)
│   ├── CreditLimitTests.cs            # Test quy tắc hạn mức tín dụng
│   ├── FifoAllocationTests.cs         # Test quy tắc phân bổ nợ FIFO
│   ├── AgingReportTests.cs            # Test thuật toán phân tổ tuổi nợ
│   └── PdfVietnameseFontTests.cs      # Test hiển thị tiếng Việt khi xuất PDF
│
└── Database/                          # KỊCH BẢN CƠ SỞ DỮ LIỆU SQL SERVER
    ├── 01_CreateDatabase.sql          # Tạo database SalesARDB
    ├── 02_CreateTables.sql            # Tạo bảng nghiệp vụ bán hàng & công nợ
    ├── 03_CreateRelationships.sql     # Ràng buộc khóa ngoại & chỉ mục
    ├── 04_InsertSampleData.sql        # Nạp dữ liệu kiểm thử thực tế
    └── 05_CreateAuthTables.sql        # Tạo bảng và tài khoản người dùng
```

---

## 🗄 THIẾT KẾ CƠ SỞ DỮ LIỆU (DATABASE SCHEMA)

Cơ sở dữ liệu **`SalesARDB`** tuân thủ chuẩn hóa 3NF:

```
[Customers] 1 ────< N [Invoices] 1 ────< N [InvoiceDetails] >──── 1 [Products]
     │                      │
     │                      │ (Được khấu trừ bởi)
     │                      V
     └────────< N [Payments] 1 ────< N [PaymentAllocations]

[Users] 1 ────< N [UserRoles] >──── 1 [Roles]
```

### Các bảng chính:
- **`Customers`**: `Id`, `CustomerCode`, `Name`, `Phone`, `Address`, `CreditLimit`, `CurrentDebt`.
- **`Products`**: `Id`, `ProductCode`, `Name`, `Unit`, `Price`.
- **`Invoices`**: `Id`, `InvoiceNumber`, `CustomerId`, `InvoiceDate`, `DueDate`, `TotalAmount`, `Status` (`Unpaid`, `PartiallyPaid`, `Paid`).
- **`InvoiceDetails`**: `Id`, `InvoiceId`, `ProductId`, `Quantity`, `UnitPrice`, `LineTotal`.
- **`Payments`**: `Id`, `PaymentNumber`, `CustomerId`, `PaymentDate`, `Amount`, `Note`.
- **`PaymentAllocations`**: `Id`, `PaymentId`, `InvoiceId`, `AllocatedAmount`, `AllocationDate`.
- **`Users` / `Roles` / `UserRoles`**: Quản lý tài khoản, mật khẩu băm, và vai trò truy cập hệ thống.

---

## ⚙️ QUY TẮC NGHIỆP VỤ CỐT LÕI (BUSINESS RULES)

### 1. Quy tắc Hạn Mức Tín Dụng (Credit Limit)
- Khi nhân viên tạo mới hóa đơn bán chịu cho khách hàng:
  $$\text{Tổng nợ mới} = \text{CurrentDebt} + \text{InvoiceTotal}$$
- **Điều kiện**: Nếu $\text{Tổng nợ mới} > \text{CreditLimit}$, hệ thống lập tức từ chối và hiển thị thông báo lỗi chi tiết số tiền vượt hạn mức, bảo vệ doanh nghiệp khỏi rủi ro nợ khó đòi.

### 2. Thuật toán Thanh Toán Đối Trừ FIFO (First-In, First-Out)
- Khi khách hàng nộp tiền/chuyển khoản, hệ thống sẽ:
  1. Mở một **SQL Server Database Transaction** để đảm bảo tính toàn vẹn (ACID).
  2. Lấy danh sách các hóa đơn chưa thanh toán hoặc thanh toán 1 phần của khách hàng, sắp xếp theo `DueDate ASC, InvoiceDate ASC` (hóa đơn đến hạn trước được ưu tiên trả trước).
  3. Duyệt từng hóa đơn và phân bổ tiền thanh toán cho đến khi hết số tiền của phiếu thu.
  4. Cập nhật trạng thái từng hóa đơn (`Paid` nếu nợ còn lại = 0, `PartiallyPaid` nếu còn dư nợ).
  5. Giảm `CurrentDebt` của khách hàng tương ứng.

### 3. Phân Tổ Tuổi Nợ (Aging Report Buckets)
- Căn cứ vào số ngày quá hạn tính từ `DueDate` đến ngày chốt báo cáo:
  - **0–30 ngày**: Nợ trong hạn hoặc mới phát sinh (Rủi ro thấp).
  - **31–60 ngày**: Nợ cần theo dõi và gửi thông báo nhắc nợ (Rủi ro trung bình).
  - **61–90 ngày**: Nợ cảnh báo hạn chế cấp hạn mức (Rủi ro cao).
  - **> 90 ngày**: Nợ xấu quá hạn nghiêm trọng, ngưng bán chịu (Rủi ro rất cao).

---

## 🔐 PHÂN QUYỀN & TÀI KHOẢN TRUY CẬP (RBAC)

Hệ thống được cấu hình sẵn 3 tài khoản mặc định đại diện cho 3 phân quyền:

| Tên Đăng Nhập | Mật Khẩu Mặc Định | Vai Trò (Role) | Phạm Vi Quyền Hạn |
| :--- | :--- | :--- | :--- |
| **`admin`** | `Admin@123` | **ADMIN** | **Toàn quyền hệ thống**: Quản lý người dùng, phân quyền vai trò, toàn bộ chức năng nghiệp vụ, kiểm tra log. |
| **`sales`** | `Sales@123` | **SALES** | **Nhân viên Kinh doanh**: Quản lý khách hàng, sản phẩm, tạo hóa đơn bán hàng, xem nhanh công nợ. |
| **`accountant`** | `Acc@123` | **ACCOUNTANT** | **Kế toán Công nợ**: Xem chi tiết công nợ, lập phiếu thu FIFO, xem & xuất Báo cáo tuổi nợ (Excel/PDF). |

> **Bảo mật**: Mật khẩu được mã hóa an toàn qua thuật toán **PBKDF2 with HMAC-SHA256** kết hợp khóa muối ngẫu nhiên (Salt).

---

## 📊 TÍNH NĂNG BÁO CÁO & XUẤT DỮ LIỆU

- **Xuất Excel (`.xlsx`)**: Tích hợp thư viện `ClosedXML`, tự động tạo định dạng bảng tính kế toán chuyên nghiệp, gom nhóm, định dạng tiền tệ Việt Nam (`#,##0 đ`), tính tổng tự động và highlight màu đỏ các khoản nợ xấu quá hạn.
- **Xuất PDF (`.pdf`)**: Tích hợp thư viện `QuestPDF`, tạo tài liệu báo cáo in ấn khổ ngang A4 (`A4 Landscape`), có header tiêu chuẩn, phân trang và áp dụng font chữ `Segoe UI` hiển thị 100% chuẩn tiếng Việt.

---

## 🚀 HƯỚNG DẪN CÀI ĐẶT & KHỞI CHẠY

### 1. Yêu cầu Môi trường
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) trở lên.
- [Microsoft SQL Server](https://www.microsoft.com/sql-server) (bản Express, Developer hoặc Standard) đang hoạt động tại máy local (`localhost` hoặc `.` hoặc `(localdb)\mssqllocaldb`).

### 2. Thiết lập Cơ sở dữ liệu SQL Server
Mở công cụ **SQL Server Management Studio (SSMS)** hoặc dùng lệnh `sqlcmd` để thực thi tuần tự các script trong thư mục [`Database/`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Database/):

```bash
# 1. Tạo Database
sqlcmd -S "localhost" -E -C -i "Database/01_CreateDatabase.sql"

# 2. Tạo Bảng dữ liệu
sqlcmd -S "localhost" -d "SalesARDB" -E -C -i "Database/02_CreateTables.sql"

# 3. Tạo Khóa ngoại & Chỉ mục
sqlcmd -S "localhost" -d "SalesARDB" -E -C -i "Database/03_CreateRelationships.sql"

# 4. Nạp Dữ liệu mẫu (sử dụng bảng mã UTF-8)
sqlcmd -S "localhost" -d "SalesARDB" -E -C -f 65001 -i "Database/04_InsertSampleData.sql"

# 5. Tạo Bảng phân quyền & Tài khoản mẫu
sqlcmd -S "localhost" -d "SalesARDB" -E -C -f 65001 -i "Database/05_CreateAuthTables.sql"
```

### 3. Cấu hình Chuỗi Kết Nối (Connection String)
Kiểm tra file [`SalesAR.Web/appsettings.json`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SalesARDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Khởi chạy Ứng dụng
Mở terminal tại thư mục gốc của dự án và chạy:
```bash
dotnet run --project SalesAR.Web/SalesAR.Web.csproj
```
Truy cập trình duyệt tại địa chỉ: **`http://localhost:5182`**

---

## 🧪 KIỂM THỬ ĐƠN VỊ (UNIT TESTING)

Dự án tích hợp đầy đủ bộ kiểm thử tự động trên xUnit:
- **`CreditLimitTests`**: Kiểm tra các ca biên hạn mức nợ (Hợp lệ, Vượt hạn mức, Hóa đơn 0đ).
- **`FifoAllocationTests`**: Kiểm tra thuật toán phân bổ thanh toán FIFO đa hóa đơn.
- **`AgingReportTests`**: Kiểm tra độ chính xác phân tổ ngày quá hạn (0-30, 31-60, 61-90, >90).
- **`PdfVietnameseFontTests`**: Kiểm tra xuất PDF với đầy đủ ký tự tiếng Việt có dấu.

Để chạy toàn bộ kiểm thử:
```bash
dotnet test SalesAR.sln
```
Kết quả: **`Passed! - Failed: 0, Passed: 20, Total: 20`** (100% Pass).

---
*Bản quyền đồ án thuộc về sinh viên **Đinh Hữu Quang** (MSSV: **23K4080042**) - Lập trình Web.*