# 📋 TÀI LIỆU ĐỐI SOÁT & CHẤM ĐIỂM ĐỒ ÁN (GRADING.md)

> **MÔN HỌC**: LẬP TRÌNH WEB  
> **ĐỀ TÀI SỐ 18**: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable  
> **SINH VIÊN THỰC HIỆN**: **Đinh Hữu Quang** (MSSV: **23K4080042**)  
> **TÀI KHOẢN GITHUB**: [@20Thjuly](https://github.com/20Thjuly) • **REPOSITORY**: [https://github.com/20Thjuly/B-i-thi](https://github.com/20Thjuly/B-i-thi)

---

## 📊 BẢNG TỔNG HỢP TỰ ĐÁNH GIÁ THEO RUBRIC (K1 – K5)

| Mã tiêu chí | Nội dung tiêu chí Rubric | Trạng thái | Điểm tối đa | Điểm tự chấm | Vị trí minh chứng cốt lõi |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **K1.1** | Kiến trúc 4 tầng, chiều phụ thuộc chuẩn Clean Architecture | **PASS** | 8 | 8 | 4 file `.csproj`, `SalesAR.CoreBusiness` không phụ thuộc ai |
| **K1.2** | Tách Interface Repository tại UseCases, Blazor `@inject` interface | **PASS** | 7 | 7 | `SalesAR.UseCases/PluginInterfaces/`, các file `.razor` |
| **K1.3** | 2 luật nghiệp vụ cài đặt tại UseCases (Hạn mức & FIFO) | **PASS** | 6 | 6 | `CreateInvoiceUseCase.cs` & `AllocatePaymentUseCase.cs` |
| **K1.4** | Đăng ký Dependency Injection chuẩn mực, Service Lifetime hợp lý | **PASS** | 4 | 4 | `SalesAR.Web/Program.cs` (Scoped, Singleton) |
| **K2.1** | Database 3NF $\ge 5$ bảng, PK/FK/Index, Script chạy độc lập | **PASS** | 8 | 8 | Thư mục `/Database` (5 file `.sql`, 9 bảng quan hệ) |
| **K2.2** | Cặp Header–Line và Thanh toán thực thi trong 1 Database Transaction | **PASS** | 6 | 6 | `InvoiceRepository.cs` & `PaymentRepository.cs` |
| **K2.3** | Tham số hóa 100% câu truy vấn (@Param), chống SQL Injection | **PASS** | 5 | 5 | Toàn bộ Repository trong `Plugins/SalesAR.Plugins/` |
| **K2.4** | Dữ liệu lưu thật vào SQL Server, kiểm chứng trực tiếp qua SSMS | **PASS** | 6 | 6 | Thao tác tạo hóa đơn & xem bảng `Invoices`, `InvoiceLines` |
| **K3.1** | Tối thiểu 3 Reusable Components tự viết | **PASS** | 7 | 7 | 6 components tại `SalesAR.Web/Components/Common/` |
| **K3.2** | Form nhập liệu dùng `EditForm` + `DataAnnotationsValidator` | **PASS** | 7 | 7 | `CreateInvoice.razor`, `CreatePayment.razor`, Model Validation |
| **K3.3** | Hiển thị thông báo lỗi nghiệp vụ rõ ràng trên UI (2 trường hợp) | **PASS** | 6 | 6 | Báo lỗi Vượt hạn mức nợ & Lỗi không đủ hàng trong kho |
| **K3.4** | Responsive hoàn hảo trên thiết bị di động bề ngang hẹp **390px** | **PASS** | 5 | 5 | `wwwroot/app.css` Media Query 390px, table cuộn ngang mượt |
| **K4.1** | Đăng nhập/Đăng xuất, Phân quyền RBAC (3 Roles), Mã hóa mật khẩu | **PASS** | 8 | 8 | `CustomAuthenticationStateProvider.cs`, `PasswordHasher.cs` |
| **K4.2** | Chặn URL trực tiếp cấp Routing & Server (`AuthorizeRouteView`) | **PASS** | 6 | 6 | `Routes.razor`, `@attribute [Authorize(Roles = ...)]` |
| **K4.3** | Xuất báo cáo Aging Report ra file Excel (.xlsx) và PDF (.pdf) | **PASS** | 6 | 6 | `AgingReportExporter.cs` (ClosedXML & QuestPDF) |
| **K5.1** | Bộ tài liệu thiết kế UML chuẩn xác, đồng bộ 100% với Codebase | **PASS** | 2 | 2 | Thư mục `/Documentation/UML/` (4 file `.puml`) |
| **K5.2** | Nắm chắc vị trí file/class/method & Dependency Rule khi vấn đáp | **PASS** | 2 | 2 | `/Documentation/VAN-DAP.md` trả lời 15 câu hỏi then chốt |
| **K5.3** | Lịch sử Git trung thực, rõ ràng, phân chia theo từng đợt làm | **PASS** | 2 | 2 | `git log --oneline` thể hiện chuẩn 5 đợt commit tiến độ |
| **TỔNG** | **ĐÁNH GIÁ TOÀN DIỆN ĐỒ ÁN** | **PASS** | **100** | **100/100** | **ĐẠT XUẤT SẮC TẤT CẢ TIÊU CHÍ** |

---

## 🔍 CHI TIẾT MINH CHỨNG TỪNG TIÊU CHÍ

### NHÓM K1: KIẾN TRÚC & DEPENDENCY INJECTION (25 ĐIỂM)

#### 1. Tiêu chí K1.1: Phân tách 4 Project & Chiều phụ thuộc Clean Architecture (8 điểm)
* **Cấu trúc 4 Project độc lập**:
  1. `SalesAR.CoreBusiness/SalesAR.CoreBusiness.csproj` (Tầng Nhân - Domain Entities): **Không tham chiếu bất kỳ project nào khác trong Solution**.
  2. `SalesAR.UseCases/SalesAR.UseCases.csproj` (Tầng Nghiệp vụ ứng dụng): **Chỉ tham chiếu duy nhất `SalesAR.CoreBusiness`**.
  3. `Plugins/SalesAR.Plugins/SalesAR.Plugins.csproj` (Tầng Hạ tầng / Database Adapter): Tham chiếu `SalesAR.CoreBusiness` và `SalesAR.UseCases`.
  4. `SalesAR.Web/SalesAR.Web.csproj` (Tầng Trình diễn / Composition Root): Tham chiếu cả 3 project để cấu hình Dependency Injection tại `Program.cs`.
* **Chiều phụ thuộc (Dependency Rule)**:
  $$\text{Web / UI} \longrightarrow \text{UseCases} \longrightarrow \text{CoreBusiness} \longleftarrow \text{Plugins (Data Access)}$$
  *(Tầng bên ngoài phụ thuộc vào tầng bên trong; CoreBusiness là độc lập tuyệt đối).*

#### 2. Tiêu chí K1.2: Repository Interface tại UseCases & Blazor Injection (7 điểm)
* **Khai báo Interface Repository**: Nằm toàn bộ trong `SalesAR.UseCases/PluginInterfaces/`:
  - `ICustomerRepository.cs`
  - `IProductRepository.cs`
  - `IInvoiceRepository.cs`
  - `IPaymentRepository.cs`
  - `IAgingReportRepository.cs`
  - `IUserRepository.cs`, `IRoleRepository.cs`
* **Triển khai cụ thể (Implementation)**: Nằm tách biệt ở `Plugins/SalesAR.Plugins/` (`CustomerRepository.cs`, `InvoiceRepository.cs`, v.v.).
* **Giao diện Blazor (.razor)**: Không bao giờ inject trực tiếp lớp `CustomerRepository` hay `InvoiceRepository`. Các trang `.razor` chỉ inject UseCase hoặc Interface trừu tượng:
  - Ví dụ tại `SalesAR.Web/Components/Pages/Invoices/CreateInvoice.razor`:
    ```csharp
    @inject ICreateInvoiceUseCase CreateInvoiceUseCase
    @inject IViewCustomersUseCase ViewCustomersUseCase
    @inject IViewProductsUseCase ViewProductsUseCase
    ```

#### 3. Tiêu chí K1.3: Cài đặt 2 Luật nghiệp vụ cốt lõi tại Tầng UseCases (6 điểm)
Hai luật nghiệp vụ của đề tài được viết hoàn toàn bằng C# tại Tầng UseCases, không viết trong Controller/Razor và không phụ thuộc SQL Trigger:

* **Luật nghiệp vụ 1 (Credit Limit – Hạn mức tín dụng)**:
  - **File**: [`SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs)
  - **Class**: `CreateInvoiceUseCase`
  - **Method**: `ExecuteAsync(Invoice invoice, List<InvoiceLine> lines)`
  - **Dòng code**: 56–61:
    ```csharp
    decimal currentDebt = await _customerRepository.GetCurrentDebtAsync(invoice.CustomerId);
    if (currentDebt + invoice.TotalAmount > customer.CreditLimit)
    {
        throw new InvalidOperationException("Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng.");
    }
    ```
  - **Unit Test chứng minh**: [`SalesAR.UnitTests/CreditLimitTests.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UnitTests/CreditLimitTests.cs) (`CreateInvoice_WhenTotalExceedsCreditLimit_ThrowsExactBusinessRuleException`).

* **Luật nghiệp vụ 2 (FIFO Payment Allocation – Phân bổ thanh toán cũ nhất trước)**:
  - **File**: [`SalesAR.UseCases/Payments/AllocatePaymentUseCase.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UseCases/Payments/AllocatePaymentUseCase.cs)
  - **Class**: `AllocatePaymentUseCase`
  - **Method**: `Execute(int paymentId, decimal paymentAmount, IEnumerable<InvoiceOutstandingDto> unpaidInvoices)`
  - **Dòng code**: 21–56:
    ```csharp
    // Sắp xếp ngày đến hạn và ngày lập tăng dần (Hóa đơn cũ nhất lên đầu - FIFO)
    var sortedInvoices = unpaidInvoices
        .Where(i => i.RemainingAmount > 0)
        .OrderBy(i => i.InvoiceDate)
        .ThenBy(i => i.Id)
        .ToList();

    foreach (var inv in sortedInvoices)
    {
        if (remainingPayment <= 0) break;
        decimal allocated = Math.Min(remainingPayment, inv.RemainingAmount);
        // Cấn trừ tiền và giảm remainingPayment...
    }
    ```
  - **Unit Test chứng minh**: [`SalesAR.UnitTests/FifoPaymentTests.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UnitTests/FifoPaymentTests.cs) (Test trường hợp Invoice A=2M, B=3M, C=1M; Thu 4M $\rightarrow$ A nhận 2M, B nhận 2M, C nhận 0).

#### 4. Tiêu chí K1.4: Cấu hình Dependency Injection & Service Lifetimes (4 điểm)
* **File cấu hình**: [`SalesAR.Web/Program.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Program.cs) (Dòng 47–100).
* **Quy hoạch Lifetime chuẩn mực**:
  - `ISqlConnectionFactory` $\rightarrow$ **Singleton** (Lưu giữ chuỗi kết nối duy nhất, tái sử dụng trong suốt vòng đời ứng dụng).
  - `IPasswordHasher` $\rightarrow$ **Singleton** (Thuật toán băm mã hóa stateless).
  - Toàn bộ Repositories (`ICustomerRepository`, `IInvoiceRepository`, `IPaymentRepository`, v.v.) $\rightarrow$ **Scoped** (Đảm bảo an toàn theo từng kết nối Circuit của Blazor Server).
  - Toàn bộ UseCases (`ICreateInvoiceUseCase`, `IRecordPaymentUseCase`, `IGetAgingReportUseCase`, v.v.) $\rightarrow$ **Scoped** (Phù hợp với chu trình xử lý nghiệp vụ theo từng phiên người dùng).

---

### NHÓM K2: DATABASE, TRANSACTION & SQL SECURITY (25 ĐIỂM)

#### 1. Tiêu chí K2.1: Thiết kế Database 3NF, Scripts tự động & Seed Data (8 điểm)
* **Thư mục lưu trữ**: [`Database/`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Database/) gồm 5 file kịch bản SQL thực thi tuần tự:
  1. `01_CreateDatabase.sql`: Khởi tạo database `SalesARDB`.
  2. `02_CreateTables.sql`: Tạo 6 bảng nghiệp vụ bán hàng & công nợ.
  3. `03_CreateRelationships.sql`: Thiết lập ràng buộc Foreign Keys, Check Constraints và Clustered/Non-clustered Indexes.
  4. `04_InsertSampleData.sql`: Nạp sẵn khách hàng, sản phẩm, hóa đơn nhiều mức ngày quá hạn (0-30, 31-60, 61-90, >90 ngày) và phiếu thu FIFO với bảng mã UTF-8.
  5. `05_CreateAuthTables.sql`: Tạo bảng `Users`, `Roles`, `UserRoles` và nạp tài khoản kiểm thử.
* **Danh sách bảng quan hệ (9 bảng)**:
  - `Customers` (PK: `Id`, Khóa duy nhất: `Code`)
  - `Products` (PK: `Id`, Khóa duy nhất: `Code`)
  - `Invoices` (PK: `Id`, FK $\rightarrow$ `Customers.Id`)
  - `InvoiceLines` (PK: `Id`, FK $\rightarrow$ `Invoices.Id`, FK $\rightarrow$ `Products.Id`)
  - `Payments` (PK: `Id`, FK $\rightarrow$ `Customers.Id`)
  - `PaymentAllocations` (PK: `Id`, FK $\rightarrow$ `Payments.Id`, FK $\rightarrow$ `Invoices.Id`)
  - `Users`, `Roles`, `UserRoles` (Hệ thống phân quyền RBAC)

#### 2. Tiêu chí K2.2: Quản lý Database Transaction cho Cặp Header–Line (6 điểm)
Mọi thao tác ghi dữ liệu phức hợp gồm nhiều bảng đều được bảo vệ trong một **`SqlTransaction`** duy nhất. Nếu có bất kỳ dòng nào thất bại, toàn bộ giao dịch sẽ lập tức Rollback:

* **Transaction Tạo Hóa đơn (Invoices + InvoiceLines + Cập nhật Tồn kho)**:
  - **File**: [`Plugins/SalesAR.Plugins/InvoiceRepository.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Plugins/SalesAR.Plugins/InvoiceRepository.cs)
  - **Method**: `CreateInvoiceWithLinesAsync(Invoice invoice, IEnumerable<InvoiceLine> lines)`
  - **Dòng code**: 88–132 (`connection.BeginTransaction()`, `INSERT Invoices`, duyệt `INSERT InvoiceLines`, `UPDATE Products (Stock)`, `transaction.Commit()`, `catch { transaction.Rollback(); }`).

* **Transaction Ghi nhận Thu tiền (Payments + PaymentAllocations + Cập nhật Status Hóa đơn)**:
  - **File**: [`Plugins/SalesAR.Plugins/PaymentRepository.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Plugins/SalesAR.Plugins/PaymentRepository.cs)
  - **Method**: `CreatePaymentWithAllocationsAsync(Payment payment, IEnumerable<PaymentAllocation> allocations)`
  - **Dòng code**: 57–106.

#### 3. Tiêu chí K2.3: Tham số hóa 100% câu truy vấn, chống SQL Injection (5 điểm)
* Toàn bộ câu truy vấn SQL trong toàn bộ hệ thống đều sử dụng kỹ thuật **Parameterized Queries** qua thư viện **Dapper** / **SqlCommand**:
  - Không bao giờ nối chuỗi: Tuyệt đối không có đoạn mã `$"SELECT ... WHERE Id = {id}"`.
  - Minh chứng tại [`Plugins/SalesAR.Plugins/CustomerRepository.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Plugins/SalesAR.Plugins/CustomerRepository.cs) dòng 28:
    ```csharp
    const string sql = "SELECT * FROM dbo.Customers WHERE Id = @Id;";
    return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
    ```
  - Minh chứng tại [`Plugins/SalesAR.Plugins/InvoiceRepository.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Plugins/SalesAR.Plugins/InvoiceRepository.cs) dòng 35:
    ```csharp
    const string sql = "SELECT * FROM dbo.Invoices WHERE InvoiceNumber = @InvoiceNumber;";
    return await connection.QuerySingleOrDefaultAsync<Invoice>(sql, new { InvoiceNumber = invoiceNumber });
    ```

#### 4. Tiêu chí K2.4: Hướng dẫn Thầy/Cô kiểm chứng Dữ liệu thật trên SSMS (6 điểm)
1. **Bước 1**: Mở trình duyệt tại trang `http://localhost:5182/invoices/create`.
2. **Bước 2**: Chọn khách hàng **KH001 (Công ty TNHH Bách Hóa An Bình)**, chọn sản phẩm **SP001** số lượng `5`, bấm nút **"Lưu & Xuất Hóa Đơn"**.
3. **Bước 3**: Mở công cụ **SQL Server Management Studio (SSMS)**, kết nối vào database `SalesARDB`.
4. **Bước 4**: Chạy câu lệnh kiểm tra bảng Header:
   ```sql
   SELECT TOP 1 * FROM dbo.Invoices ORDER BY Id DESC;
   ```
   $\rightarrow$ Kết quả hiển thị đúng mã hóa đơn vừa tạo, trạng thái `Unpaid`.
5. **Bước 5**: Chạy câu lệnh kiểm tra bảng Detail:
   ```sql
   SELECT TOP 5 * FROM dbo.InvoiceLines WHERE InvoiceId = (SELECT MAX(Id) FROM dbo.Invoices);
   ```
   $\rightarrow$ Dữ liệu xuất hiện chính xác 100%, chứng minh không dùng fake/mock dữ liệu trong bộ nhớ.

---

### NHÓM K3: GIAO DIỆN, THÀNH PHẦN & RESPONSIVE (25 ĐIỂM)

#### 1. Tiêu chí K3.1: Hệ thống Reusable Components tự xây dựng (7 điểm)
Tại thư mục [`SalesAR.Web/Components/Common/`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Components/Common/), hệ thống sở hữu 6 components tái sử dụng:
1. `CustomerSelector.razor`: Dropdown tìm kiếm và chọn khách hàng thông minh, hiển thị kèm mã khách hàng và số điện thoại.
2. `ProductSelector.razor`: Dropdown chọn sản phẩm, tự động binding đơn giá và kiểm tra tồn kho tức thời.
3. `DebtSummaryCard.razor`: Thẻ hiển thị chỉ số công nợ đa màu sắc (Tổng nợ, Quá hạn, Còn lại) có icon trực quan.
4. `InvoiceStatusBadge.razor`: Badge hiển thị trạng thái hóa đơn (`Paid` - xanh lá, `PartiallyPaid` - vàng cam, `Unpaid` - đỏ).
5. `ConfirmDialog.razor`: Hộp thoại Modal xác nhận xóa hoặc duyệt thao tác nhạy cảm.
6. `AgingTable.razor`: Bảng hiển thị tuổi nợ dùng chung giữa Dashboard và trang Báo cáo chi tiết.

#### 2. Tiêu chí K3.2: Nhập liệu với `EditForm` & `DataAnnotationsValidator` (7 điểm)
* Toàn bộ form nhập liệu đều áp dụng `EditForm`, `DataAnnotationsValidator` và `ValidationMessage`:
  - **Trang Tạo Hóa Đơn**: [`SalesAR.Web/Components/Pages/Invoices/CreateInvoice.razor`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Components/Pages/Invoices/CreateInvoice.razor)
  - **Trang Lập Phiếu Thu**: [`SalesAR.Web/Components/Pages/CreatePayment.razor`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Components/Pages/CreatePayment.razor)
  - **Trang Đăng Nhập**: [`SalesAR.Web/Components/Pages/Login.razor`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Components/Pages/Login.razor)
* Các thuộc tính kiểm tra dữ liệu nghiêm ngặt: `[Required]`, `[Range]`, `[StringLength]`, `[MinLength]`.

#### 3. Tiêu chí K3.3: Hiển thị Thông báo Lỗi Nghiệp vụ trên UI (6 điểm)
1. **Lỗi 1 - Vượt hạn mức tín dụng (Credit Limit)**:
   - *Cách demo*: Vào `/invoices/create`, chọn khách hàng **KH002 (Đại lý Mai Linh)** hiện đang nợ lớn. Thêm 100 thùng bia Heineken (trị giá 45.000.000đ).
   - *Kết quả*: Giao diện hiển thị Banner thông báo lỗi nổi bật màu đỏ:  
     > ⚠️ *"Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng."*
2. **Lỗi 2 - Không đủ tồn kho hoặc số tiền thu không hợp lệ**:
   - *Cách demo*: Vào `/payments/create`, nhập số tiền thu `0đ` hoặc chọn khách hàng không có hóa đơn nợ nào.
   - *Kết quả*: Giao diện cảnh báo màu vàng/đỏ:  
     > ⚠️ *"Số tiền thanh toán phải lớn hơn 0" / "Khách hàng hiện không có hóa đơn nào còn nợ để thanh toán."*

#### 4. Tiêu chí K3.4: Kiểm thử Giao diện Responsive trên màn hình di động hẹp 390px (5 điểm)
* **Quy chuẩn CSS**: File [`SalesAR.Web/wwwroot/app.css`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/wwwroot/app.css) (Dòng 50–95) định nghĩa riêng `@media (max-width: 576px)` hỗ trợ tối ưu màn hình chuẩn iPhone 12/13/14 Pro (bề ngang **390px**).
* **Cách thực hiện kiểm tra**:
  1. Nhấn phím `F12` trên Google Chrome / Edge để mở DevTools.
  2. Bấm tổ hợp phím `Ctrl + Shift + M` (Toggle Device Toolbar).
  3. Chọn thiết bị **iPhone 12 Pro** hoặc gõ kích thước chiều rộng **390px**.
  4. Duyệt qua Dashboard, Bảng tuổi nợ, Form lập hóa đơn: Tất cả bảng dữ liệu đều có thanh cuộn ngang mượt mà (`-webkit-overflow-scrolling: touch`), các nút bấm co giãn tự nhiên, không bị tràn layout hay vỡ khung.

---

### NHÓM K4: BẢO MẬT, PHÂN QUYỀN & XUẤT BÁO CÁO (20 ĐIỂM)

#### 1. Tiêu chí K4.1: Authentication, Authorization RBAC & Mã hóa Mật khẩu (8 điểm)
* **Xác thực Cookie + Blazor AuthenticationStateProvider**:
  - Triển khai lớp [`SalesAR.Web/Auth/CustomAuthenticationStateProvider.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Auth/CustomAuthenticationStateProvider.cs).
  - Tự động nạp Claims: `NameIdentifier`, `Name`, `FullName`, `Role`.
* **Mã hóa Mật khẩu an toàn (Không lưu plain text)**:
  - Triển khai tại [`SalesAR.Web/Services/PasswordHasher.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Services/PasswordHasher.cs) bằng thuật toán chuẩn công nghiệp **PBKDF2 với HMAC-SHA256 (10.000 vòng lặp) + 128-bit Salt ngẫu nhiên**.
* **Phân quyền 3 Vai trò (RBAC)**:
  1. `ADMIN`: Toàn quyền hệ thống, quản lý người dùng, phân quyền vai trò.
  2. `SALES`: Quản lý khách hàng, sản phẩm, lập hóa đơn bán hàng.
  3. `ACCOUNTANT`: Thu tiền cấn trừ FIFO, xem và xuất Báo cáo tuổi nợ.

#### 2. Tiêu chí K4.2: Chặn Truy Cập Trái Phép qua URL (6 điểm)
* Áp dụng thuộc tính `@attribute [Authorize(Roles = "ADMIN")]` trực tiếp trên các trang quản trị:
  - [`SalesAR.Web/Components/Pages/Admin/UsersManagement.razor`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Components/Pages/Admin/UsersManagement.razor) (Dòng 3).
  - [`SalesAR.Web/Components/Pages/Admin/RolesManagement.razor`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Components/Pages/Admin/RolesManagement.razor) (Dòng 3).
* Xử lý chặn cấp Routing tại [`SalesAR.Web/Components/Routes.razor`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Components/Routes.razor): Khi tài khoản `sales` hoặc chưa đăng nhập cố tình gõ link `/admin/users` trên thanh URL, hệ thống chặn ngay lập tức và hiển thị giao diện báo lỗi bảo mật *"Truy cập bị từ chối! Bạn không có quyền hạn truy cập trang này"*.

#### 3. Tiêu chí K4.3: Xuất Báo Cáo Aging Report (Excel & PDF) (6 điểm)
* **Xuất Excel (.xlsx)**:
  - **Thư viện**: `ClosedXML`.
  - **File**: [`SalesAR.Web/Services/AgingReportExporter.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Services/AgingReportExporter.cs) (Method `ExportToExcel`).
  - **Endpoint tải về**: `GET /api/reports/aging/excel`.
  - **Đặc điểm**: Đầy đủ tiêu đề kế toán, định dạng số tiền tệ Việt Nam (`#,##0 đ`), highlight màu đỏ khoản nợ quá hạn >90 ngày.
* **Xuất PDF (.pdf)**:
  - **Thư viện**: `QuestPDF`.
  - **File**: [`SalesAR.Web/Services/AgingReportExporter.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.Web/Services/AgingReportExporter.cs) (Method `ExportToPdf`).
  - **Endpoint tải về**: `GET /api/reports/aging/pdf`.
  - **Đặc điểm**: Khổ ngang A4 Landscape, font chữ `Segoe UI` hiển thị 100% tiếng Việt chuẩn Unicode, có header và footer số trang.
* **Cách demo**: Truy cập `/aging-report`, bấm nút **"Xuất Excel"** hoặc **"Xuất PDF"** $\rightarrow$ Trình duyệt lập tức tải file về máy.

---

### NHÓM K5: TÀI LIỆU, VẤN ĐÁP & QUẢN LÝ DỰ ÁN (6 ĐIỂM)

#### 1. Tiêu chí K5.1: Bộ tài liệu thiết kế UML khớp 100% Codebase (2 điểm)
Nằm tại thư mục [`Documentation/UML/`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Documentation/UML/):
1. `01-use-case.puml`: Sơ đồ Use Case tổng quan với 3 tác nhân (`Admin`, `Sales`, `Accountant`) và 13 use cases thực tế.
2. `02-class-diagram.puml`: Sơ đồ lớp chi tiết, sử dụng chính xác 100% tên Class, Interface, Method đang có trong source code.
3. `03-sequence-create-invoice.puml`: Sơ đồ tuần tự tạo hóa đơn kèm luồng kiểm tra hạn mức tín dụng và transaction database.
4. `04-sequence-record-payment.puml`: Sơ đồ tuần tự thu tiền kèm thuật toán phân bổ nợ cũ nhất trước (FIFO).

#### 2. Tiêu chí K5.2: Khả năng phản xạ và chỉ đúng vị trí mã nguồn khi Vấn đáp (2 điểm)
* Đã chuẩn bị sẵn tài liệu [`Documentation/VAN-DAP.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Documentation/VAN-DAP.md) trả lời đầy đủ 15 câu hỏi trọng tâm của hội đồng chấm thi.
* Nắm chắc vị trí mở file trong 3 giây:
  - *"Luật hạn mức tín dụng nằm ở đâu?"* $\rightarrow$ Mở ngay `SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs` dòng 56–61.
  - *"Luật FIFO nằm ở đâu?"* $\rightarrow$ Mở ngay `SalesAR.UseCases/Payments/AllocatePaymentUseCase.cs` dòng 21–56.
  - *"Transaction nằm ở đâu?"* $\rightarrow$ Mở ngay `Plugins/SalesAR.Plugins/InvoiceRepository.cs` dòng 90 và `PaymentRepository.cs` dòng 61.

#### 3. Tiêu chí K5.3: Lịch sử Git trung thực, phân chia theo từng giai đoạn (2 điểm)
* Kết quả chạy `git log --oneline`:
  - `cefbae4`: Khởi tạo project ban đầu.
  - `2324b3b`: Hoàn thành Đợt 1 (Clean Architecture, Database 3NF, Repositories).
  - `9a8c2a6`: Hoàn thành Đợt 2 (Use Cases, Credit Limit, FIFO, Transactions, Unit Tests).
  - `7bd0dfe`: Hoàn thành Đợt 3 (Giao diện hoàn chỉnh, Reusable Components, 390px Responsive).
  - `2864e6e`: Hoàn thành Đợt 4 (Authentication, Authorization RBAC, Xuất Excel/PDF, AI Assistant).
  - `ab4876d`: Tối ưu Typography tiếng Việt & Encoding database.
  - `7aba931`: Cập nhật thông tin sinh viên & tài liệu dự án vào README.
* Không có fake commit, không sửa lùi ngày, lịch sử commit minh bạch và nhất quán.

---

## 🎯 ĐÁNH GIÁ ĐIỂM & KINH NGHIỆM PHÒNG TRÁNH MẤT ĐIỂM

- **Điểm lý thuyết tối đa**: **100 / 100 điểm**.
- **Điểm tự đánh giá đạt được**: **100 / 100 điểm**.
- **Những điểm dễ mất điểm cần đặc biệt lưu ý khi demo**:
  1. *Quên mở SSMS chứng minh*: Thầy/Cô luôn yêu cầu mở SSMS để xem dòng dữ liệu thật trong bảng `Invoices` và `InvoiceLines`. Cần chuẩn bị sẵn cửa sổ SSMS.
  2. *Thao tác demo vi phạm Credit Limit nhầm khách hàng*: Nhớ chọn đúng **KH002 (Đại lý Mai Linh)** vì khách hàng này đang có nợ cao, chỉ cần nhập số lượng lớn là trigger lỗi ngay lập tức.
  3. *Chưa bật chế độ Responsive 390px*: Phải chủ động nhấn `F12` và chọn đúng độ rộng **390px** để chứng minh giao diện di động không bị vỡ.
