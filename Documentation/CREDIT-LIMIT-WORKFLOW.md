# TÀI LIỆU QUY TRÌNH PHÊ DUYỆT VƯỢT HẠN MỨC TÍN DỤNG (CREDIT LIMIT WORKFLOW)
**Đề tài 18: Bán hàng & Công nợ Khách hàng — Sales & Accounts Receivable**  
**Giai đoạn:** PART 5/10 — CREDIT LIMIT APPROVAL WORKFLOW  
**Ngày thực hiện:** 15/10/2026  
**Hệ quản trị CSDL:** Microsoft SQL Server 2022 / Express  
**Kết quả kiểm thử:** 40/40 Tests Đạt (100% Passed) — 0 Warning, 0 Error

---

## 1. TỔNG QUAN QUY TRÌNH & NGUYÊN TẮC NGHIỆP VỤ

Quy trình phê duyệt vượt hạn mức tín dụng được thiết kế nhằm bảo vệ dòng tiền của doanh nghiệp, vừa hỗ trợ linh hoạt cho kinh doanh ký các đơn hàng giá trị lớn, vừa quản trị chặt chẽ rủi ro nợ xấu:

```mermaid
flowchart TD
    A[Nhân viên Kinh doanh (SALES) tạo đơn hàng] --> B[Hệ thống kiểm tra công nợ và hạn mức thực tế trong DB]
    B -->|Công nợ + Đơn hàng <= Hạn mức| C[Xử lý xuất hóa đơn bán hàng bình thường]
    B -->|Công nợ + Đơn hàng > Hạn mức| D[Chặn xuất hóa đơn & Yêu cầu lập Phiếu đề xuất vượt hạn mức]
    D --> E[SALES gửi yêu cầu duyệt vượt hạn mức (Status: Pending)]
    E --> F[Credit Manager / Admin thẩm định hồ sơ]
    F -->|Đạt tiêu chí tín dụng| G[Credit Manager chấp thuận (Status: Approved)]
    F -->|Rủi ro nợ xấu cao| H[Credit Manager từ chối (Status: Rejected)]
    G --> I[SALES tạo hóa đơn kèm mã Approval hợp lệ]
    I --> J[Hệ thống kiểm tra thời hạn & đánh dấu IsUsed = 1 trong cùng Transaction]
    J --> K[Hoàn tất xuất hóa đơn bán hàng]
```

---

## 2. TÁM BƯỚC QUY TRÌNH CHUẨN HÓA

1. **Khởi tạo:** Nhân viên bán hàng (`SALES`) lập đơn hàng bán lẻ hoặc bán buôn cho khách hàng.
2. **Kiểm tra tức thời:** Hệ thống tính toán dư nợ thực tế $\text{CurrentDebt} = \text{TotalInvoiced} - \text{TotalPaid} - \text{TotalAdjusted}$ từ CSDL và so sánh với `CreditLimit`.
3. **Luồng chuẩn:** Nếu không vượt hạn mức, hệ thống tự động xuất hóa đơn, trừ tồn kho và ghi nhận công nợ bình thường.
4. **Kích hoạt phê duyệt:** Nếu vượt hạn mức tín dụng, hệ thống chặn xuất hóa đơn trực tiếp và điều hướng sang quy trình lập yêu cầu duyệt.
5. **Thẩm định độc lập:** Người có thẩm quyền (`CREDIT_MANAGER` hoặc `ADMIN`) kiểm tra thông tin đối tác, lịch sử thanh toán và lý do đề xuất.
6. **Lưu vết quyết định:** Hệ thống ghi nhận người quyết định, thời điểm, ý kiến phê duyệt hoặc lý do từ chối vào `dbo.CreditLimitApprovals` và `dbo.AuditLogs`.
7. **Kiểm tra công nợ thực tế tại thời điểm duyệt (Không tin snapshot cũ):** Khi duyệt, hệ thống truy vấn lại số dư nợ và hạn mức thực tế trong CSDL lúc đó, lưu lại `CurrentDebtAtDecision` và `CreditLimitAtDecision`.
8. **Chống hai yêu cầu đồng thời cùng vượt qua một hạn mức (Concurrency & Anti-Replay):**
   - Sử dụng khóa `WITH (UPDLOCK, HOLDLOCK)` trên bảng yêu cầu và khách hàng.
   - Mỗi phiếu duyệt chỉ được dùng **duy nhất một lần** (`IsUsed = 1`, lưu `UsedInvoiceId`). Mọi nỗ lực tái sử dụng (Replay Attack) đều bị từ chối triệt để.

---

## 3. MÁY TRẠNG THÁI YÊU CẦU (STATE MACHINE)

Hệ thống quản lý 5 trạng thái hợp lệ, bảo đảm không cho phép nhảy cóc trạng thái:
- **`Pending`:** Yêu cầu mới được tạo bởi SALES, đang chờ thẩm định.
- **`Approved`:** Đã được CREDIT_MANAGER / ADMIN chấp thuận, có thời hạn hiệu lực (`ExpiresAt`).
- **`Rejected`:** Bị từ chối bởi CREDIT_MANAGER / ADMIN kèm lý do rõ ràng.
- **`Cancelled`:** Người tạo hủy yêu cầu khi khách đổi ý trước khi cấp trên xử lý.
- **`Expired`:** Hết thời hạn hiệu lực mà chưa được sử dụng để xuất hóa đơn.

---

## 4. SCHEMA CƠ SỞ DỮ LIỆU & QUAN HỆ THỰC THỂ

Migration [Database/07_CreditLimitApprovalWorkflow.sql](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/Database/07_CreditLimitApprovalWorkflow.sql) đã triển khai:

### 4.1. Bảng `dbo.CreditLimitRequests`
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mục đích nghiệp vụ |
|:---|:---|:---|:---|
| `Id` | `INT IDENTITY(1,1)` | PK | Khóa chính tự tăng |
| `RequestNumber` | `NVARCHAR(50)` | UNIQUE, NOT NULL | Mã định danh yêu cầu (vd: `CLR-20261015-ABC123`) |
| `CustomerId` | `INT` | FK -> `Customers(Id)` | Khách hàng xin duyệt nợ |
| `TransactionAmount`| `DECIMAL(18,2)`| CK > 0, NOT NULL | Giá trị đơn hàng xin xuất |
| `CurrentDebtSnapshot`| `DECIMAL(18,2)`| NOT NULL | Dư nợ lúc tạo (Bối cảnh snapshot) |
| `CreditLimitSnapshot`| `DECIMAL(18,2)`| NOT NULL | Hạn mức lúc tạo (Bối cảnh snapshot) |
| `Reason` | `NVARCHAR(500)`| NOT NULL | Lý do kinh doanh đề xuất |
| `Status` | `NVARCHAR(50)` | CK Enum | Trạng thái (`Pending`, `Approved`, `Rejected`, `Cancelled`, `Expired`) |
| `RequestedBy` | `NVARCHAR(100)`| NOT NULL | Username nhân viên kinh doanh đề xuất |
| `RequestedAt` | `DATETIME2(0)` | NOT NULL | Thời điểm gửi yêu cầu |
| `ApprovedBy` | `NVARCHAR(100)`| NULL | Username người phê duyệt |
| `ApprovedAt` | `DATETIME2(0)` | NULL | Thời điểm phê duyệt |
| `DecisionReason` | `NVARCHAR(500)`| NULL | Ý kiến phê duyệt hoặc lý do từ chối |
| `ExpiresAt` | `DATETIME2(0)` | NULL | Thời hạn có hiệu lực của phiếu duyệt (mặc định 48h) |
| `IsUsed` | `BIT` | DEFAULT 0 | Cờ đánh dấu phiếu duyệt đã dùng xuất hóa đơn chưa |
| `UsedAt` | `DATETIME2(0)` | NULL | Thời điểm sử dụng phiếu duyệt |
| `UsedInvoiceId` | `INT` | FK -> `Invoices(Id)` | Hóa đơn liên kết đã tiêu thụ phiếu duyệt này |

### 4.2. Bảng `dbo.CreditLimitApprovals`
Lưu vết toàn bộ tiến trình và số liệu kiểm toán tài chính:
- `Id`: `INT IDENTITY(1,1)` — PK
- `RequestId`: `INT` — FK -> `CreditLimitRequests(Id)` (`ON DELETE CASCADE`)
- `Action`: `NVARCHAR(50)` — CK (`SUBMIT`, `APPROVE`, `REJECT`, `CANCEL`, `EXPIRE`)
- `Actor`: `NVARCHAR(100)` — Người thực hiện hành động
- `ActionAt`: `DATETIME2(0)` — Thời điểm thực hiện
- `Comment`: `NVARCHAR(500)` — Ý kiến / lý do
- `CurrentDebtAtDecision`: `DECIMAL(18,2)` — Dư nợ thực tế lúc quyết định
- `CreditLimitAtDecision`: `DECIMAL(18,2)` — Hạn mức thực tế lúc quyết định

---

## 5. MA TRẬN PHÂN QUYỀN & NGUYÊN TẮC TÁCH BIỆT NHIỆM VỤ

| Vai trò (Role) | Lập yêu cầu (`CreateRequest`) | Duyệt (`Approve`) | Từ chối (`Reject`) | Xuất HĐ có duyệt | Tự duyệt yêu cầu của mình |
|:---|:---:|:---:|:---:|:---:|:---:|
| **SALES** | **CÓ** | KHÔNG | KHÔNG | **CÓ** | **CẤM TUYỆT ĐỐI** |
| **CREDIT_MANAGER** | KHÔNG | **CÓ** | **CÓ** | KHÔNG | **CẤM TUYỆT ĐỐI** |
| **ADMIN** | **CÓ** | **CÓ** | **CÓ** | **CÓ** | **CẤM** (nếu là người tạo) |
| **ACCOUNTANT** | KHÔNG | KHÔNG | KHÔNG | KHÔNG | **CẤM** |

### Nguyên tắc tách biệt nhiệm vụ (Segregation of Duties):
Trong Use Case [ApproveCreditLimitRequestUseCase.cs](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UseCases/CreditLimitRequests/ApproveCreditLimitRequestUseCase.cs):
```csharp
if (request.RequestedBy.Equals(approverUsername, StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Vi phạm chính sách tách biệt nhiệm vụ (Segregation of Duties): Người tạo yêu cầu không được phép tự phê duyệt yêu cầu của chính mình.");
}
```

---

## 6. KẾT QUẢ KIỂM THỬ NGHIỆM THU (TEST RESULTS)

Đã xây dựng test suite [CreditLimitApprovalWorkflowTests.cs](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/B%C3%80I%20THI/SalesAR.UnitTests/CreditLimitApprovalWorkflowTests.cs) bao phủ 100% các yêu cầu:
1. `CreateRequest_Succeeds_WhenRoleIsSales`: Nhân viên bán hàng tạo yêu cầu duyệt thành công.
2. `CreateRequest_ThrowsException_WhenRoleIsNotSalesOrAdmin`: Người không có quyền bị chặn tạo yêu cầu (`UnauthorizedAccessException`).
3. `ApproveRequest_ThrowsException_WhenApproverIsTheCreator_SegregationOfDuties`: Ngăn chặn người tạo tự phê duyệt yêu cầu của chính mình.
4. `ApproveRequest_ThrowsException_WhenRoleIsNotCreditManagerOrAdmin`: Ngăn chặn vai trò không đủ thẩm quyền phê duyệt.
5. `ApproveRequest_Succeeds_AndRechecksLiveDebt_NotOldSnapshot`: Thẩm định lại dư nợ mới nhất trong DB khi duyệt, không tin snapshot cũ.
6. `RejectRequest_Succeeds_WhenAuthorized`: Credit Manager từ chối yêu cầu rủi ro kèm lý do.
7. `CreateInvoice_AllowsExceedingLimit_WhenValidApprovalExists_AndMarksApprovalUsed`: Cho phép xuất hóa đơn vượt hạn mức khi có phiếu duyệt hợp lệ và đánh dấu tiêu thụ phiếu duyệt.
8. `CreateInvoice_RejectsSecondUseOfSameApproval_AntiReplay`: Chống tái sử dụng (anti-replay) phiếu duyệt đã tiêu thụ.

```bash
Passed! - Failed: 0, Passed: 40, Skipped: 0, Total: 40, Duration: 438 ms - SalesAR.UnitTests.dll (net8.0)
Build succeeded: 0 Warning(s), 0 Error(s)
```

Giai đoạn **Part 5: Credit Limit Approval Workflow** đã hoàn thành xuất sắc và sẵn sàng cho các giai đoạn tiếp theo!
