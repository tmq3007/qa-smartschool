# KẾ HOẠCH NÂNG CẤP CHI TIẾT: ĐỒNG BỘDbContext NGẮN HẠN & DỌN DẸP BỘ NHỚ ĐỆM NHẬP TAY
**Đơn vị lập:** Trưởng ban Thiết kế dự án QA Smart School
**Tiêu chuẩn áp dụng:** Bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật QA SmartClass v4.2
**Trạng thái:** Chờ Duyệt (Pending Approval)

---

## ═══ MỤC TIÊU NÂNG CẤP ═══
Tái cấu trúc kiến trúc truy xuất dữ liệu trong Phân hệ Nhật ký Bảo vệ để chuyển đổi hoàn toàn sang mô hình DbContext ngắn hạn (Short-lived DbContext). Giải quyết triệt để rủi ro xung đột bộ nhớ đệm (stale data) và lỗi xung đột đa luồng trên Entity Framework Core, đồng thời làm sạch UI khi chuyển đổi sự kiện.

---

## ═══ BẢN ĐỒ TỔNG THỂ CÁC BƯỚC THỰC HIỆN ═══

| Bước | Nội dung cải tiến | File ảnh hưởng | Trách nhiệm kiểm tra |
| :--- | :--- | :--- | :--- |
| **B1** | Loại bỏ DbContext và SecurityService mức class | `SecurityKioskViewModel.cs` | Architect & DB Engine |
| **B2** | Làm sạch trường nhập tay CCCD/Địa chỉ khi đổi event | `SecurityKioskViewModel.cs` | UI/UX Designer |
| **B3** | Áp dụng using DbContext cục bộ trong các hàm ViewModel | `SecurityKioskViewModel.cs` | DB Specialist & Coder |

---

## ═══ PHẦN I: THIẾT KẾ KỸ THUẬT CHI TIẾT ═══

### Bước 1 & 3: Tái cấu trúc sang DbContext ngắn hạn trong các phương thức
*   **Yêu cầu:** Loại bỏ hoàn toàn trường `private readonly AppDbContext _db;` và `private readonly SecurityService _securityService;` dùng chung để triệt tiêu Change Tracker dài hạn.
*   **Phương pháp thực hiện:**
    *   Mở tệp [SecurityKioskViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs).
    *   Xóa bỏ các khai báo:
        ```csharp
        private readonly AppDbContext _db;
        private readonly SecurityService _securityService;
        ```
    *   Tái cấu trúc constructor chỉ chứa:
        ```csharp
        public SecurityKioskViewModel()
        {
            GuardName = QASmartClass.Staff.Services.StaffSession.DisplayName;
            _ = LoadTeachersAsync();
            _ = LoadActiveVisitorsAsync();
        }
        ```
    *   Tái cấu trúc các phương thức `LoadTeachersAsync`, `LoadActiveVisitorsAsync`, `LoadTodayLogsAsync`, `OnSelectedActiveVisitorChanged`, `SaveAsync` và `SearchAsync` để khởi tạo `using var db = new AppDbContext();` và `var securityService = new SecurityService(db);` cục bộ bên trong khối `try`.
*   **Phản biện hệ thống:** Việc sử dụng một instance DbContext duy nhất trong ViewModel lưu giữ các trạng thái đối tượng cũ. Khi có cập nhật mới từ các luồng giám sát hoặc hệ thống bên ngoài, ViewModel bảo vệ sẽ không thể đọc được dữ liệu mới (stale data). Cơ chế `using var db` giải phóng hoàn toàn Change Tracker, kết nối SQLite được đóng mở nhanh chóng và an toàn tuyệt đối với đa luồng.

---

### Bước 2: Tự động làm sạch các trường nhập tay khi đổi sự kiện
*   **Yêu cầu:** Khi bảo vệ nhấn đổi ComboBox "Loại sự kiện" (hoặc qua các phím nóng F1-F4), hai thuộc tính `ManualCccd` và `ManualAddress` phải được xóa trống lập tức để tránh lộ thông tin của vị khách trước sang sự kiện của vị khách sau.
*   **Phương pháp thực hiện:**
    *   Mở tệp [SecurityKioskViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs).
    *   Cập nhật hàm `OnSelectedEventTypeChanged`:
        ```csharp
        partial void OnSelectedEventTypeChanged(string value)
        {
            // Reset fields on event type change
            Person = string.Empty;
            Description = string.Empty;
            BadgeNumber = string.Empty;
            VisitPurpose = "Liên hệ công tác";
            SelectedHostTeacherId = string.Empty;
            SelectedActiveVisitor = null;
            _scannedCccd = string.Empty;
            _scannedGender = string.Empty;
            _scannedAddress = string.Empty;
            ManualCccd = string.Empty;     // Dọn dẹp ô nhập tay CCCD
            ManualAddress = string.Empty;  // Dọn dẹp ô nhập tay Địa chỉ
        }
        ```
*   **Phản biện sư phạm và bảo mật:** Việc giữ nguyên thông tin nhập tay của phụ huynh trước khi chuyển sang sự cố hoặc khách tiếp theo là hành vi không nhất quán về UX, có nguy cơ làm rò rỉ dữ liệu cá nhân (PII) trên giao diện trực trực tiếp của bảo vệ. Việc dọn dẹp sạch sẽ trường tạm là yêu cầu bắt buộc của quy chuẩn v4.2.

---

## ═══ PHẦN II: CHECKSHEET KIỂM TRA CHO KIỂM THỬ (QA CHECKSHEET) ═══

- [ ] **Check-Clear-Manual-CCCD:** Nhập số CCCD thủ công vào ô nhập tay. Bấm `F2` để chuyển đổi sự kiện. Xác nhận ô nhập tay đã bị xóa trống.
- [ ] **Check-Clear-Manual-Address:** Nhập địa chỉ vào ô nhập địa chỉ thủ công. Bấm `F3` để chuyển đổi sự kiện. Xác nhận ô nhập địa chỉ đã bị xóa trống.
- [ ] **Check-DbContext-Class:** Đảm bảo không còn bất kỳ dòng khai báo `_db` hay `_securityService` nào ở phạm vi biến toàn lớp trong file `SecurityKioskViewModel.cs`.
- [ ] **Check-Using-Local:** Xác minh tất cả các thao tác đọc/ghi CSDL đều được bọc trong khối lệnh `using var db = new AppDbContext();`.
- [ ] **Check-Dispose-Cancellation:** Bộ dọn dẹp `Dispose()` hủy bỏ và giải phóng chính xác `_debounceCts` để tránh rò rỉ luồng chạy ngầm.

---

**TRƯỞNG BAN THIẾT KẾ DỰ ÁN QA SMART SCHOOL**
*(Đã ký và đóng dấu phê duyệt)*
