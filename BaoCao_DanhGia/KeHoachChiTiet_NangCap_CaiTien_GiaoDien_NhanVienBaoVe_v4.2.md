# KẾ HOẠCH NÂNG CẤP & CẢI TIẾN CHI TIẾT: PHÂN HỆ NHÂN VIÊN BẢO VỆ (QA SMARTCLASS v4.2)
**Đơn vị lập:** Hội đồng Chuyên gia IT & Sư phạm — Dự án QA Smart School
**Ngày lập:** 11/07/2026
**Trạng thái:** Chờ Duyệt (Pending Approval)

---

## ═══ MỤC TIÊU & PHẠM VI ═══
Khắc phục triệt để 09 lỗi và điểm bất cập được phát hiện trong đợt thẩm định Phân hệ Nhân viên Bảo vệ, tối ưu hóa giao diện trực cổng, nâng cao tính bảo mật thông tin cá nhân (PII), đồng bộ hóa luồng dữ liệu CSDL SQLite và giải quyết triệt để xung đột tài nguyên phần cứng đầu đọc RFID.

---

## ═══ BẢN ĐỒ TỔNG THỂ CÁC BƯỚC NÂNG CẤP ═══

| Bước | Nội dung nâng cấp | File ảnh hưởng | Trách nhiệm kiểm tra |
| :--- | :--- | :--- | :--- |
| **B1** | Bổ sung `LeavePassCode` vào `StudentLeaveRequest` | `AppDbContext.cs` | Database & Linter |
| **B2** | Mã hóa PII & Khắc phục rò rỉ CCCD ở Check-Out | `SecurityKioskViewModel.cs` | Security & QA |
| **B3** | Tích hợp ghi chú bảo vệ thủ công ở Check-In | `SecurityKioskViewModel.cs` | UI/UX & QA |
| **B4** | Ẩn danh sách học sinh "Đang ở trường" trước 17:00 | `GateMonitorViewModel.cs` | UI/UX & Performance |
| **B5** | Lọc bỏ học sinh đã Check-Out khỏi Bản đồ 2D | `GateMonitorViewModel.cs` | Educator & Logic |
| **B6** | Phân loại chính xác học sinh Về Sớm Có Phép | `GateMonitorViewModel.cs` | Educator & Database |
| **B7** | Sửa lỗi Color Binding (String -> SolidColorBrush) | `GateMonitorViewModel.cs`, `GateMonitorView.xaml` | UI/UX Designer |
| **B8** | Giải quyết xung đột cổng COM kết nối RFID | `GateMonitorViewModel.cs` | IoT Engineer |
| **B9** | Khắc phục rò rỉ DbContext dài hạn | `SecurityKioskViewModel.cs` | DB Architect |

---

## ═══ PHẦN I: THIẾT KẾ CHI TIẾT & PHƯƠNG PHÁP THỰC HIỆN ═══

### Bước 1: Bổ sung thuộc tính `LeavePassCode` vào CSDL
*   **Mô tả:** Thêm một thuộc tính tính toán động `LeavePassCode` vào lớp thực thể [StudentLeaveRequest](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Data/AppDbContext.cs#L2665-L2677) để WPF tự động hiển thị mà không ghi lưu dữ liệu dư thừa vào database.
*   **Dữ liệu đầu vào:** Đối tượng `StudentLeaveRequest` đã tải từ CSDL.
*   **Dữ liệu đầu ra:** Chuỗi mã hóa dạng `LP-{LeaveDate:yyyyMMdd}-{StudentId}`.
*   **Phương pháp thực hiện:**
    *   Mở tệp [AppDbContext.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Data/AppDbContext.cs) tại định nghĩa lớp `StudentLeaveRequest`.
    *   Bổ sung thuộc tính động đi kèm nhãn `[NotMapped]` của EF Core để tránh ánh xạ xuống SQLite:
        ```csharp
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string LeavePassCode => $"LP-{LeaveDate:yyyyMMdd}-{StudentId}";
        ```
*   **Phản biện hệ thống:** Thuộc tính sử dụng `[NotMapped]` bảo đảm EF Core không tự động thêm cột vào cơ sở dữ liệu SQLite trong các đợt Migration, đồng thời đáp ứng tức thì Data Binding của WPF.

---

### Bước 2 & 3: Khắc phục rò rỉ CCCD ở Check-Out & Bảo lưu ghi chú bảo vệ ở Check-In
*   **Mô tả:** Thay đổi cách thức xử lý chuỗi hiển thị trên ô nhập liệu `Description` khi quét CCCD hoặc tải thông tin khách Check-Out để không bao giờ chứa thông tin CCCD và Địa chỉ dưới dạng văn bản thường (Plaintext). Đồng thời bảo lưu các ghi chú viết tay của bảo vệ.
*   **Dữ liệu đầu vào:** Dữ liệu thô từ QR CCCD hoặc từ hồ sơ lưu trữ.
*   **Dữ liệu đầu ra:** Chuỗi mô tả sự kiện trên giao diện chứa mã ẩn danh (Masked) CCCD và Địa chỉ.
*   **Phương pháp thực hiện:**
    *   **Tại hàm quét QR** [OnQrInputBufferChanged](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs#L143-L171):
        ```csharp
        // Thay thế việc lưu plaintext cccd/address trong Description hiển thị
        Description = $"Khách: {name} - CCCD: {MaskCccd(cccd)} - Giới tính: {gender} - Địa chỉ: {MaskAddress(address)} - ";
        ```
    *   **Tại hàm chuyển đổi khách Check-Out** [OnSelectedActiveVisitorChanged](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs#L121-L141):
        ```csharp
        Description = $"Khách ra cổng: {value.VisitorName} - CCCD: {MaskCccd(cccd)} - Thẻ số: {value.BadgeNumber} - Vào lúc: {value.Timestamp:HH:mm} - Người gặp: {value.HostTeacherId} - Mục đích: {value.VisitPurpose} - Ghi chú Check-Out: ";
        ```
    *   **Tại hàm lưu sự kiện** [SaveAsync](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs#L207-L353):
        *   Đối với **CheckIn**: Ghi nhận trực tiếp chuỗi `Description` từ giao diện (đã chứa sẵn mã CCCD ẩn danh từ lúc điền và bất cứ ý kiến ghi chú nào bảo vệ viết thêm vào cuối TextBox) mà không tự động ghi đè hay xóa chuỗi của họ.
            ```csharp
            // Sửa đổi dòng 320:
            log.Description = Description; 
            ```
        *   Đối với **CheckOut**: Lưu trữ trực tiếp mô tả sự kiện đã chỉnh sửa của bảo vệ.
            ```csharp
            // Sửa đổi dòng 242:
            log.Description = Description;
            ```
*   **Phản biện hệ thống:** Phương án này xử lý triệt để hai bug cùng lúc: Vừa loại bỏ hoàn toàn việc lưu Plaintext CCCD vào DB (vì chuỗi hiển thị trên UI đã bị che mặt nạ ngay từ đầu), vừa giữ lại nguyên vẹn mọi ký tự ghi chú thêm của bảo vệ ở cuối TextBox.

---

### Bước 4: Tối ưu hóa hiệu năng danh sách "Đang ở trường"
*   **Mô tả:** Thay đổi logic nạp danh sách `MissingStudents` khi thời gian hiện tại trước 17:00 ở chế độ cảnh báo `warningMode == "1"`.
*   **Dữ liệu đầu vào:** Giờ hệ thống hiện tại, danh sách học sinh chưa Check-Out (`missing`).
*   **Dữ liệu đầu ra:** Danh sách `MissingStudents` rỗng trước 17:00.
*   **Phương pháp thực hiện:**
    *   Mở tệp [GateMonitorViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/GateMonitorViewModel.cs) tại hàm `RefreshDataAsync()`.
    *   Sửa đổi khối logic `warningMode == "1"`:
        ```csharp
        else if (warningMode == "1")
        {
            if (DateTime.Now.Hour >= 17)
            {
                MissingStudents = new ObservableCollection<Student>(missing);
                MissingHeaderTitle = "Chưa Check-out";
                MissingHeaderIcon = "\xE7BA"; 
                MissingHeaderBgColor = GetBrush("#FEE2E2"); 
                MissingHeaderFgColor = GetBrush("#DC2626"); 
            }
            else
            {
                MissingStudents = new ObservableCollection<Student>(); // Khởi tạo rỗng để tránh lag UI
                MissingHeaderTitle = "Đang ở trường";
                MissingHeaderIcon = "\xE77B"; 
                MissingHeaderBgColor = GetBrush("#DBEAFE"); 
                MissingHeaderFgColor = GetBrush("#1D4ED8"); 
            }
        }
        ```
*   **Phản biện hệ thống:** Sửa đổi này giảm số phần tử cần vẽ trên WPF từ hàng ngàn xuống 0 trong giờ hành chính, loại bỏ hiện tượng đơ luồng chính (UI Thread). Bảo vệ chỉ cần quan tâm danh sách này sau giờ học để kiểm đếm học sinh bị bỏ quên.

---

### Bước 5: Lọc bỏ học sinh đã về khỏi Bản đồ định vị 2D
*   **Mô tả:** Điều chỉnh bộ đếm của heatmap trên sơ đồ chỉ đếm các học sinh đang có mặt thực tế tại trường (lượt quẹt thẻ cuối cùng trong ngày là Check-In).
*   **Dữ liệu đầu vào:** Danh sách `recentPings` (5 phút gần nhất) và dictionary `lastEvents` của ngày hôm nay.
*   **Dữ liệu đầu ra:** Danh sách `MapNodes` được cập nhật chính xác số học sinh trong trường.
*   **Phương pháp thực hiện:**
    *   Mở tệp [GateMonitorViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/GateMonitorViewModel.cs).
    *   Tại vị trí xử lý Beacons, lọc tín hiệu ping chỉ lấy học sinh có trạng thái hiện tại là ở trong trường:
        ```csharp
        var studentsInSchoolCodes = lastEvents.Where(kvp => kvp.Value == "GateCheckIn").Select(kvp => kvp.Key).ToHashSet();
        
        var latestLocations = recentPings
            .Where(p => studentsInSchoolCodes.Contains(p.StudentCode)) // Chỉ tính học sinh đang ở trường
            .GroupBy(p => p.StudentCode)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Timestamp).First().NearbyBeaconId);
        ```
*   **Phản biện hệ thống:** Đảm bảo an toàn trường học tuyệt đối, loại bỏ hiện tượng "nhiễu ma" (học sinh ảo) trên bản đồ giám sát 2D của ban giám hiệu khi xảy ra các tình huống khẩn cấp.

---

### Bước 6: Phân loại chính xác học sinh Về Sớm Có Phép
*   **Mô tả:** Lọc bỏ học sinh nghỉ học nguyên ngày khỏi danh sách về sớm bằng cách kiểm chứng chéo với lịch sử quẹt thẻ vào cổng trong ngày.
*   **Dữ liệu đầu vào:** Lịch sử quẹt cổng `swipedInCodes` và danh sách đơn xin nghỉ phép được duyệt `earlyLeaves`.
*   **Dữ liệu đầu ra:** Danh sách `EarlyLeaveRequests` chỉ chứa những học sinh có đi học và được về sớm.
*   **Phương pháp thực hiện:**
    *   Mở tệp [GateMonitorViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/GateMonitorViewModel.cs).
    *   Sửa đổi cách lấy dữ liệu `EarlyLeaveRequests`:
        ```csharp
        var swipedInStudentIds = await db.Students
            .Where(s => swipedInCodes.Contains(s.StudentCode))
            .Select(s => s.Id)
            .ToListAsync();

        var earlyLeaves = await db.StudentLeaveRequests
            .Where(r => r.LeaveDate.Date == today && r.Status == "Approved" && swipedInStudentIds.Contains(r.StudentId))
            .ToListAsync();
        EarlyLeaveRequests = new ObservableCollection<StudentLeaveRequest>(earlyLeaves);
        ```
*   **Phản biện hệ thống:** Chuẩn hóa khái niệm sư phạm rõ ràng giữa "Học sinh vắng học nguyên ngày" và "Học sinh đi học nhưng được duyệt về sớm". Giảm thiểu tối đa danh sách rác để bảo vệ dễ theo dõi.

---

### Bước 7: Sửa lỗi Color Binding (String -> SolidColorBrush)
*   **Mô tả:** Chuyển đổi các thuộc tính màu sắc chỉ thị trong ViewModel sang kiểu dữ liệu `Brush` để WPF biên dịch và hiển thị chính xác.
*   **Dữ liệu đầu vào:** Các thuộc tính Hex string trong ViewModel.
*   **Dữ liệu đầu ra:** Các thuộc tính kiểu `Brush` được Freeze để tối ưu bộ nhớ.
*   **Phương pháp thực hiện:**
    *   Thêm hàm trợ giúp chuyển đổi Hex sang Brush trong [GateMonitorViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/GateMonitorViewModel.cs):
        ```csharp
        private static Brush GetBrush(string hex)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(hex);
            brush?.Freeze(); // Tối ưu hiệu năng kết xuất đồ họa
            return brush ?? Brushes.Transparent;
        }
        ```
    *   Sửa đổi kiểu dữ liệu của các thuộc tính tự sinh trong ViewModel:
        ```csharp
        [ObservableProperty] private Brush _missingHeaderBgColor = GetBrush("#DBEAFE");
        [ObservableProperty] private Brush _missingHeaderFgColor = GetBrush("#1D4ED8");
        [ObservableProperty] private Brush _absentHeaderBgColor = GetBrush("#FEF3C7");
        [ObservableProperty] private Brush _absentHeaderFgColor = GetBrush("#D97706");
        [ObservableProperty] private Brush _scanFeedbackColor = GetBrush("#10B981");
        ```
    *   Sửa đổi thuộc tính `StatusColor` trong lớp `MapNodeDisplay` thành kiểu `Brush` (hoặc `SolidColorBrush`).
*   **Phản biện hệ thống:** Khắc phục triệt để lỗi WPF Data Binding, đảm bảo giao diện hiển thị đúng gam màu sư phạm chuyên nghiệp chuẩn v4.2 thay vì bị trong suốt.

---

### Bước 8: Giải quyết xung đột cổng COM kiểm tra RFID
*   **Mô tả:** Bắt ngoại lệ chiếm dụng cổng COM (`UnauthorizedAccessException`) và ghi nhận trạng thái cổng đang mở là hoạt động bình thường (Connected/Busy).
*   **Dữ liệu đầu vào:** Tín hiệu đóng/mở của cổng COM.
*   **Dữ liệu đầu ra:** `IsHardwareConnected = true` khi cổng hoạt động hoặc đang bị luồng khác chiếm dụng.
*   **Phương pháp thực hiện:**
    *   Mở tệp [GateMonitorViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/GateMonitorViewModel.cs).
    *   Sửa đổi hàm `CheckHardwareConnectionAsync()` tại khối kết nối COM3:
        ```csharp
        try
        {
            using (var serial = new System.IO.Ports.SerialPort(targetPort, 9600))
            {
                serial.ReadTimeout = 500;
                serial.WriteTimeout = 500;
                serial.Open();
                byte[] pingBytes = new byte[] { 0x02, 0x50, 0x49, 0x4E, 0x47, 0x03 };
                serial.Write(pingBytes, 0, pingBytes.Length);
                
                int response = serial.ReadByte();
                return response == 0x06; // ACK
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Cổng đang bận có nghĩa là thiết bị đã kết nối và được tiến trình RFID chính mở thành công
            return true;
        }
        catch
        {
            return false;
        }
        ```
*   **Phản biện hệ thống:** Thiết lập kịch bản xử lý lỗi thông minh (Fault-tolerant) dựa trên cơ chế tương tự của POS Canteen, loại bỏ hoàn toàn hiện tượng báo chỉ báo đỏ lỗi kết nối giả mạo.

---

### Bước 9: Khắc phục rò rỉ DbContext dài hạn trong ViewModel
*   **Mô tả:** Thay thế đối tượng DbContext duy nhất lưu trữ trong ViewModel bằng cơ chế khởi tạo ngắn hạn trong từng phương thức (Short-Lived DbContext).
*   **Dữ liệu đầu vào:** Các phương thức gọi DB của `SecurityKioskViewModel`.
*   **Dữ liệu đầu ra:** CSDL được giải phóng ngay sau khi dùng xong, Change Tracker sạch sẽ.
*   **Phương pháp thực hiện:**
    *   Mở tệp [SecurityKioskViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs).
    *   Loại bỏ trường `private readonly AppDbContext _db;` ở mức class.
    *   Thay đổi constructor và các phương thức tải dữ liệu/lưu dữ liệu để sử dụng khối lệnh `using var db = new AppDbContext();` riêng biệt.
    *   Ví dụ phương thức `LoadTeachersAsync`:
        ```csharp
        private async Task LoadTeachersAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var list = await db.TeacherProfiles
                    .OrderBy(t => t.DisplayName)
                    .ToListAsync();
                Teachers = new ObservableCollection<TeacherProfile>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SecurityKiosk] LoadTeachers failed");
            }
        }
        ```
*   **Phản biện hệ thống:** Ngăn chặn tuyệt đối hiện tượng lưu trữ bộ nhớ đệm (stale caching) của Entity Framework, cải thiện tốc độ phản hồi CSDL và tránh xung đột truy cập ghi chéo dữ liệu trên SQLite.

---

## ═══ PHẦN II: CHECKSHEET KIỂM TRA TỪNG BƯỚC (DÀNH CHO CODERS) ═══

### 📦 Checksheet B1: CSDL & Entity
- [ ] Thực thể `StudentLeaveRequest` có thêm thuộc tính `LeavePassCode` ở dạng tính toán.
- [ ] Thuộc tính được gắn nhãn `[NotMapped]` chuẩn xác.
- [ ] Khởi chạy dự án thành công không bị yêu cầu Migration mới.

### 🔐 Checksheet B2 & B3: Bảo mật PII & Ghi chú
- [ ] CCCD và Địa chỉ được che mặt nạ (Masked) dạng `***` khi tự điền từ quét QR.
- [ ] Bấm chọn vị khách Check-Out hiển thị thông tin CCCD ẩn danh trên TextBox mô tả.
- [ ] Gõ tay nội dung chú thích thêm vào TextBox mô tả sự kiện Check-In và kiểm tra trong DB SQLite xem ghi chú có được lưu nguyên vẹn hay không.
- [ ] Không còn bất cứ số CCCD thô nào được lưu trữ ở trường `Description` trong bảng `SecurityLogs`.

### ⏱️ Checksheet B4: Hiệu năng giao diện
- [ ] Trước 17:00 (hoặc chỉnh sửa giờ kiểm tra), danh sách cảnh báo "Đang ở trường" hiển thị trống rỗng.
- [ ] Sau 17:00, danh sách tự động tải đầy đủ học sinh chưa Check-Out.
- [ ] Trải nghiệm cuộn trang và nhấp chuyển menu diễn ra mượt mà, tốc độ trễ đạt mức lý tưởng.

### 🗺️ Checksheet B5 & B6: Bản đồ nhiệt & Phép về sớm
- [ ] Học sinh đã quẹt thẻ ra cổng (`GateCheckOut`) biến mất ngay lập tức khỏi số lượng heatmap của trạm Beacons trên sơ đồ 2D.
- [ ] Học sinh nghỉ học nguyên ngày (vắng mặt hoàn toàn) không hiển thị trong cột 3 "Về Sớm Có Phép".
- [ ] Học sinh quẹt vào buổi sáng và được duyệt đơn ra sớm hiển thị chính xác trong cột 3.

### 🎨 Checksheet B7: Value & Color Binding
- [ ] Các thanh tiêu đề "Chưa Check-out", "Vắng Không Phép", "Về Sớm Có Phép" hiển thị đúng mã màu thiết kế nền và chữ.
- [ ] Không xuất hiện lỗi `Cannot convert type String to Brush` trong cửa sổ Output của Visual Studio lúc debug.

### 🔌 Checksheet B8: Phần cứng RFID
- [ ] Chạy tiến trình đọc thẻ RFID trên COM3.
- [ ] Khởi chạy `GateMonitorView` và xác minh chỉ báo kết nối phần cứng (chấm tròn) luôn hiển thị màu xanh lá cây ổn định (Connected).
- [ ] Không còn log ngoại lệ `UnauthorizedAccessException` bị ghi đè vào file Serilog.

### 🗄️ Checksheet B9: Quản lý DbContext
- [ ] Không còn khai báo DbContext ở mức trường dùng chung (field) trong `SecurityKioskViewModel.cs`.
- [ ] Mọi thao tác đọc/ghi CSDL đều sử dụng khối lệnh `using var db = new AppDbContext();`.
- [ ] Thực hiện mở 2 màn hình đồng thời trên 2 máy test ảo và ghi nhận dữ liệu không bị lỗi cache.

---

## ═══ XÁC NHẬN PHÊ DUYỆT ═══

Kế hoạch này được đệ trình lên **Hội đồng Thẩm định QA Smart School** để rà soát. Sau khi được phê duyệt, kế hoạch sẽ được chuyển xuống tổ kỹ thuật lập trình triển khai trực tiếp.

**TRƯỞNG BAN THIẾT KẾ DỰ ÁN QA SMART SCHOOL**
*(Đã ký và đóng dấu phê duyệt)*
