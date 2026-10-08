# 🎓 BỘ CÂU HỎI & HƯỚNG DẪN TRẢ LỜI VẤN ĐÁP ĐỒ ÁN (15 CÂU)

> **MÔN HỌC**: LẬP TRÌNH WEB  
> **ĐỀ TÀI SỐ 18**: Bán hàng & Công nợ Khách hàng – Sales & Accounts Receivable  
> **SINH VIÊN**: **Đinh Hữu Quang** (MSSV: **23K4080042**)  

---

### CÂU 1: Clean Architecture là gì? Mục đích chính của nó trong dự án này?
* **Trả lời**:  
  Clean Architecture là mô hình kiến trúc phần mềm phân tầng theo nguyên lý **Đảo ngược phụ thuộc (Dependency Inversion)**. Mục đích chính là:
  1. Tách biệt hoàn toàn phần nghiệp vụ lõi (Business Logic) khỏi giao diện (UI) và công nghệ cơ sở dữ liệu (Database).
  2. Giúp mã nguồn dễ bảo trì, dễ viết Unit Test độc lập và có thể thay đổi công nghệ cơ sở dữ liệu hoặc framework giao diện mà không ảnh hưởng đến nghiệp vụ.

---

### CÂU 2: Vì sao `SalesAR.CoreBusiness` không được reference tới tầng Web hay bất kỳ project nào?
* **Trả lời**:  
  `SalesAR.CoreBusiness` chứa các thực thể nghiệp vụ cốt lõi (Domain Entities). Theo quy tắc bất di bất dịch của Clean Architecture (**Dependency Rule**):
  - Chiều phụ thuộc luôn hướng từ ngoài vào trong: UI $\rightarrow$ UseCases $\rightarrow$ CoreBusiness.
  - CoreBusiness là tầng trung tâm, không được phụ thuộc vào bất kỳ công nghệ bên ngoài nào (kể cả UI hay thư viện truy cập dữ liệu) để đảm bảo tính độc lập và toàn vẹn của mô hình dữ liệu.

---

### CÂU 3: Vì sao các Repository Interface lại nằm ở `SalesAR.UseCases` chứ không nằm ở `SalesAR.Plugins`?
* **Trả lời**:  
  Đây là ứng dụng nguyên lý **Dependency Inversion Principle (DIP)**:
  - Tầng Nghiệp vụ (UseCases) chỉ định nghĩa các hợp đồng (Interfaces) nó cần để đọc/ghi dữ liệu (ví dụ: `IInvoiceRepository`, `ICustomerRepository`).
  - Tầng Hạ tầng (`SalesAR.Plugins`) đóng vai trò là Plugin cắm vào và triển khai cụ thể các Interface đó.
  - Nhờ vậy, UseCases chỉ phụ thuộc vào Interface trừu tượng, không bị phụ thuộc cứng vào SQL Server hay thư viện Dapper/Entity Framework.

---

### CÂU 4: Dependency Injection (DI) là gì? Lợi ích là gì?
* **Trả lời**:  
  Dependency Injection là kỹ thuật mà trong đó một đối tượng nhận các phụ thuộc (Dependencies) từ bên ngoài truyền vào thay vì tự khởi tạo (`new`).
  - **Lợi ích**: Giảm sự phụ thuộc chặt (Loose Coupling), giúp dễ dàng thay thế việc triển khai, hỗ trợ Mocking dữ liệu khi viết Unit Test, và quản lý tập trung vòng đời đối tượng.

---

### CÂU 5: Service Lifetime `Scoped` trong Blazor Server hoạt động như thế nào? Vì sao Repositories lại chọn `Scoped`?
* **Trả lời**:  
  - Trong Blazor Server, một `Scoped` service tồn tại tương ứng với **một phiên kết nối WebSocket (Circuit)** của một người dùng.
  - **Lý do chọn Scoped cho Repository/UseCase**: Giúp đảm bảo trạng thái dữ liệu cô lập giữa các phiên làm việc của các nhân viên khác nhau, tránh xung đột dữ liệu dùng chung (khác với Singleton) và tiết kiệm tài nguyên khởi tạo liên tục (tối ưu hơn Transient).

---

### CÂU 6: ⭐ Luật Hạn Mức Tín Dụng (Credit Limit) nằm ở đâu trong Code?
* **MỞ NGAY FILE**: [`SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UseCases/Invoices/CreateInvoiceUseCase.cs)
* **Class**: `CreateInvoiceUseCase`
* **Method**: `ExecuteAsync(Invoice invoice, List<InvoiceLine> lines)`
* **Dòng code**: Dòng **56 – 61**:
  ```csharp
  decimal currentDebt = await _customerRepository.GetCurrentDebtAsync(invoice.CustomerId);
  if (currentDebt + invoice.TotalAmount > customer.CreditLimit)
  {
      throw new InvalidOperationException("Không thể tạo hóa đơn. Công nợ hiện tại và giá trị hóa đơn vượt quá hạn mức tín dụng của khách hàng.");
  }
  ```

---

### CÂU 7: ⭐ Thuật toán Thanh toán FIFO nằm ở đâu trong Code?
* **MỞ NGAY FILE**: [`SalesAR.UseCases/Payments/AllocatePaymentUseCase.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UseCases/Payments/AllocatePaymentUseCase.cs)
* **Class**: `AllocatePaymentUseCase`
* **Method**: `Execute(int paymentId, decimal paymentAmount, IEnumerable<InvoiceOutstandingDto> unpaidInvoices)`
* **Dòng code**: Dòng **21 – 56**:
  - Hóa đơn chưa thanh toán được sắp xếp: `.OrderBy(i => i.InvoiceDate).ThenBy(i => i.Id)`.
  - Phân bổ lần lượt: `decimal allocated = Math.Min(remainingPayment, inv.RemainingAmount)`.
  - Khấu trừ đến khi hết tiền thu (`remainingPayment <= 0`).

---

### CÂU 8: Vì sao phải sử dụng Database Transaction?
* **Trả lời**:  
  Để đảm bảo 4 tính chất **ACID (Atomicity, Consistency, Isolation, Durability)**. Khi một thao tác nghiệp vụ cần ghi đồng thời vào nhiều bảng quan hệ (như Tạo Hóa đơn phải ghi cả `Invoices`, `InvoiceLines` và trừ `StockQuantity` của `Products`), Transaction đảm bảo: **Tất cả cùng thành công, hoặc không có bảng nào bị thay đổi**.

---

### CÂU 9: Nếu tạo `Invoices` thành công nhưng ghi `InvoiceLines` hoặc trừ Kho thất bại thì sao?
* **Trả lời**:  
  Khối lệnh `try-catch` tại [`Plugins/SalesAR.Plugins/InvoiceRepository.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Plugins/SalesAR.Plugins/InvoiceRepository.cs) (Dòng 127–131) sẽ ngay lập tức kích hoạt lệnh **`transaction.Rollback()`**:
  - Dòng hóa đơn vừa tạo trong bảng `Invoices` sẽ bị hủy bỏ hoàn toàn.
  - Số lượng tồn kho và công nợ giữ nguyên hiện trạng.
  - Ngăn ngừa tình trạng hóa đơn ma (Header có nhưng chi tiết rỗng).

---

### CÂU 10: Làm thế nào để chống tấn công SQL Injection trong dự án?
* **Trả lời**:  
  Dự án áp dụng **100% Parameterized Queries** thông qua Dapper và SqlCommand:
  - Các tham số đầu vào được đóng gói dạng `@Param` (ví dụ `@CustomerId`, `@InvoiceNumber`).
  - Dữ liệu người dùng nhập vào luôn được SQL Server coi là giá trị chuỗi thuần túy (Literal Value), không bao giờ bị thực thi như một mệnh đề lệnh SQL độc hại.

---

### CÂU 11: Vì sao chỉ ẩn nút bấm trên Menu là chưa đủ để phân quyền?
* **Trả lời**:  
  Vì người dùng am hiểu công nghệ hoàn toàn có thể gõ trực tiếp URL trên trình duyệt (ví dụ: gõ thẳng `/admin/users` hoặc `/admin/roles`).
  - Do đó, dự án thiết lập bảo vệ 2 lớp:
    1. Lớp hiển thị UI: `NavMenu.razor` dùng `<AuthorizeView Roles="...">` để ẩn menu không thuộc quyền.
    2. Lớp bảo vệ Server-side: Trang Razor áp dụng `@attribute [Authorize(Roles = "ADMIN")]` kết hợp `AuthorizeRouteView` tại `Routes.razor` để chặn đứng truy cập trực tiếp qua thanh địa chỉ.

---

### CÂU 12: Authentication khác Authorization như thế nào trong dự án?
* **Trả lời**:  
  - **Authentication (Xác thực)**: Trả lời câu hỏi *"Bạn là ai?"* $\rightarrow$ Kiểm tra Username và Password băm qua PBKDF2 tại trang `Login.razor`.
  - **Authorization (Phân quyền)**: Trả lời câu hỏi *"Bạn được phép làm gì?"* $\rightarrow$ Kiểm tra các Claims Role (`ADMIN`, `SALES`, `ACCOUNTANT`) để cho phép hoặc từ chối truy cập từng chức năng cụ thể.

---

### CÂU 13: Báo cáo Tuổi nợ (Aging Report) tính toán như thế nào?
* **Trả lời**:  
  Dựa trên hiệu số giữa **Thời điểm chốt số liệu (`asOfDate`)** và **Hạn thanh toán (`DueDate`)** của từng hóa đơn chưa thanh toán:
  - $\le 30$ ngày: Xếp vào nhóm `0–30 ngày`.
  - Từ $31$ đến $60$ ngày: Xếp vào nhóm `31–60 ngày`.
  - Từ $61$ đến $90$ ngày: Xếp vào nhóm `61–90 ngày`.
  - $> 90$ ngày: Xếp vào nhóm `>90 ngày` (nợ xấu quá hạn).
  - Triển khai tại: [`Plugins/SalesAR.Plugins/AgingReportRepository.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Plugins/SalesAR.Plugins/AgingReportRepository.cs).

---

### CÂU 14: Vì sao Cơ sở dữ liệu của dự án đạt chuẩn 3NF (Chuẩn 3)?
* **Trả lời**:  
  1. **1NF**: Mọi thuộc tính đều mang giá trị nguyên tố (Atomic), không có mảng lặp trong cột.
  2. **2NF**: Đã đạt 1NF và mọi thuộc tính không khóa đều phụ thuộc hàm toàn phần vào Khóa chính (Tách riêng `Invoices` và `InvoiceLines`).
  3. **3NF**: Đã đạt 2NF và không có phụ thuộc bắc cầu (Transitive Dependency) giữa các thuộc tính không khóa (Ví dụ thông tin `Product.Price`, `Product.Name` nằm ở bảng `Products`, `InvoiceLines` chỉ lưu khóa ngoại `ProductId` và đơn giá tại thời điểm xuất bán).

---

### CÂU 15: Sơ đồ UML trong thư mục `/Documentation/UML` khớp với Code như thế nào?
* **Trả lời**:  
  Tất cả các thành phần trong 4 sơ đồ PlantUML đều ánh xạ 1:1 với mã nguồn thực tế:
  - **Class Diagram**: Tên các Entity (`Customer`, `InvoiceLine`...), Interface (`ICustomerRepository`...), Class UseCase (`CreateInvoiceUseCase`...) trùng khớp chính xác từng ký tự với code C#.
  - **Sequence Diagrams**: Mô tả chính xác các bước gọi method từ Blazor UI qua UseCase, Repository cho đến câu lệnh `BEGIN TRANSACTION` / `COMMIT` của SQL Server.
