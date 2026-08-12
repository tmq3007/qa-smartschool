# Kế hoạch Chi tiết Nâng cấp và Cải tiến Giao diện & Logic Phân hệ Quản lý Phụ trách Đoàn Đội (Youth Union Module)
**Tài liệu hướng dẫn phát triển và kiểm thử - Chuẩn QA SmartClass v4.1**  

---

## ═══ TỔNG QUAN HỆ THỐNG & NHIỆM VỤ ═══

Tài liệu này đóng vai trò là đặc tả kỹ thuật và lộ trình nâng cấp chi tiết dành cho lập trình viên (coder) và chuyên gia kiểm thử (tester) nhằm sửa chữa triệt để các lỗi logic, mã hóa, hiển thị tiếng Việt không dấu và thiếu hướng dẫn sử dụng trong phân hệ **Quản lý Phụ trách Đoàn Đội (Youth Union)**.

Yêu cầu thực hiện nghiêm ngặt, tuân thủ đúng cấu trúc chương trình hiện hữu và bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**.

---

## ═══ PHẦN I: KHẮC PHỤC LỖI LOGIC NGHIỆP VỤ & CƠ SỞ DỮ LIỆU (DATABASE & LOGIC BUGS) ═══

### 1. Đồng bộ hóa mã hóa loại thành viên và chức vụ (Accent Mismatch Resolution)
*   **Vấn đề:** DB lưu trữ chuỗi không dấu (ví dụ: `"Doan vien"`, `"Thanh vien"`) theo hằng số [YouthConstants.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/YouthConstants.cs). Tuy nhiên, UI [MemberListView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/MemberListView.xaml.cs) lại lưu chuỗi có dấu (`"Đoàn viên"`, `"Thành viên"`) trực tiếp vào DB, dẫn đến lỗi lọc danh sách trả về 0 phần tử và lỗi tính Đoàn phí.
*   **Yêu cầu kỹ thuật:** Sử dụng bảng ánh xạ (Mapping Dictionaries) hai chiều giữa DB (hằng số không dấu) và UI (chuỗi hiển thị có dấu). **Không được lưu trực tiếp chuỗi hiển thị của UI vào cơ sở dữ liệu**.
*   **Giải pháp chi tiết:**

#### 📑 Tệp 1: [MemberListView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/MemberListView.xaml.cs)
*   *Mô tả thực hiện:* Thêm các bộ từ điển ánh xạ tĩnh ở cấp độ lớp:
    ```csharp
    private static readonly Dictionary<string, string> MemberTypeMap = new()
    {
        { YouthConstants.YouthMemberTypes.DoanVien, "Đoàn viên" },
        { YouthConstants.YouthMemberTypes.DoiVien, "Đội viên" }
    };

    private static readonly Dictionary<string, string> PositionMap = new()
    {
        { YouthConstants.YouthPositions.ThanhVien, "Thành viên" },
        { YouthConstants.YouthPositions.BiThu, "Bí thư" },
        { YouthConstants.YouthPositions.PhoBT, "Phó Bí thư" },
        { YouthConstants.YouthPositions.UVBCH, "Ủy viên BCH" }
    };
    ```
*   *Áp dụng khi tải dữ liệu lên UI (Phương thức `ShowMemberForm`):*
    ```csharp
    // Gán dữ liệu combobox hiển thị bằng tiếng Việt có dấu
    cbType.SelectedItem = existing != null && MemberTypeMap.ContainsKey(existing.MemberType) 
        ? MemberTypeMap[existing.MemberType] : "Đoàn viên";

    cbPos.SelectedItem = existing != null && PositionMap.ContainsKey(existing.Position) 
        ? PositionMap[existing.Position] : "Thành viên";
    ```
*   *Áp dụng khi lưu dữ liệu từ UI xuống DB (Phương thức `btnSave.Click`):*
    ```csharp
    // Chuyển đổi ngược lại chuỗi có dấu sang không dấu của DB
    var selectedTypeStr = cbType.SelectedItem?.ToString() ?? "Đoàn viên";
    var dbType = MemberTypeMap.FirstOrDefault(x => x.Value == selectedTypeStr).Key ?? YouthConstants.YouthMemberTypes.DoanVien;

    var selectedPosStr = cbPos.SelectedItem?.ToString() ?? "Thành viên";
    var dbPos = PositionMap.FirstOrDefault(x => x.Value == selectedPosStr).Key ?? YouthConstants.YouthPositions.ThanhVien;

    if (isEdit)
    {
        existing!.MemberType = dbType;
        existing.Position = dbPos;
    }
    else
    {
        newMember.MemberType = dbType;
        newMember.Position = dbPos;
    }
    ```

#### 📑 Tệp 2: [FeeTrackerView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/FeeTrackerView.xaml.cs)
*   *Vấn đề:* Đang so khớp cứng `m.MemberType == "Doan vien"`. Vì trước đó DB bị trộn lẫn cả `"Đoàn viên"` và `"Doan vien"`, các câu lệnh lọc danh sách đóng phí bị lỗi.
*   *Mô tả thực hiện:* Chuyển đổi các câu lệnh truy vấn lọc thành viên để chấp nhận cả hai trường hợp hoặc chuẩn hóa về hằng số:
    ```csharp
    // Thay đổi truy vấn đếm đoàn viên cần đóng phí (Dòng 39, 78, 153):
    var members = allMembers.Where(m => m.Status == "Active" && 
        (m.MemberType == YouthConstants.YouthMemberTypes.DoanVien || m.MemberType == "Đoàn viên")).ToList();
    ```

#### 📑 Tệp 3: [YouthUnionService.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Services/YouthUnionService.cs)
*   *Vấn đề:* Phương thức `SyncApprovedRecruitmentsAsync` đang lưu cứng `MemberType = "Đoàn viên"` (có dấu), lệch chuẩn với hằng số DB.
*   *Mô tả thực hiện (Dòng 268-270):*
    ```csharp
    // Trước:
    MemberType = "Đoàn viên",
    JoinDate = DateTime.Today,
    Position = "Thành viên",
    // Sau:
    MemberType = YouthConstants.YouthMemberTypes.DoanVien, // "Doan vien"
    JoinDate = DateTime.Today,
    Position = YouthConstants.YouthPositions.ThanhVien, // "Thanh vien"
    ```

### 2. Tùy chọn Cấu hình Master Chế độ Mã hóa (Master Db Encoding Mode Option)
Để nhà trường chủ động quyết định dựa trên điều kiện thực tế (tích hợp hay chạy độc lập) và năng lực kỹ thuật:
*   **Bổ sung khóa cấu hình:** `YouthUnionDbEncodingMode` vào cấu hình Master hệ thống.
*   **Cơ chế hoạt động:**
    *   **Chế độ 0 (Legacy Compatibility - Khuyên dùng cho trường cần liên kết phần mềm cũ):** CSDL SQLite lưu chuỗi **không dấu chuẩn** (`"Doan vien"`, `"Thanh vien"`). Giao diện sử dụng bộ ánh xạ `Mapping Dictionary` hai chiều để hiển thị tiếng Việt có dấu. Chế độ này tương thích hoàn toàn với hệ thống cơ sở dữ liệu hành chính cũ và các API xuất khẩu dữ liệu cấp Phòng/Sở sử dụng chuẩn ASCII.
    *   **Chế độ 1 (Native Unicode - Khuyên dùng cho trường chạy độc lập):** CSDL SQLite lưu trực tiếp chuỗi **tiếng Việt có dấu** (`"Đoàn viên"`, `"Thành viên"`). Giao diện hiển thị trực tiếp mà không cần trung chuyển qua bộ chuyển đổi chuỗi. Chế độ này giúp các quản trị viên cơ sở dữ liệu dễ dàng viết các câu truy vấn SQL (Ad-hoc query) hoặc kết xuất báo cáo nhanh mà không cần lập trình phần mềm để chuyển đổi dữ liệu.
*   **Mô tả kỹ thuật cho Lập trình viên:**
    *   Tất cả các dịch vụ đọc/ghi của `YouthUnionService` phải kiểm tra giá trị của `AppConfig.Load().YouthUnionDbEncodingMode` trước khi thực thi truy vấn hoặc lưu dữ liệu để áp dụng ánh xạ tương ứng.

---

## ═══ PHẦN II: KHẮC PHỤC LỖI RFID & KHÓA LUỒNG GIAO DIỆN (RFID THREAD BLOCKAGE) ═══

### 1. Loại bỏ hộp thoại Modal gây nghẽn luồng quét thẻ tự động
*   **Vấn đề:** Khi quét thẻ lỗi trong [AttendanceView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/AttendanceView.xaml.cs), `MessageBox.Show` được kích hoạt làm treo toàn bộ luồng giao diện và luồng cổng COM đọc thẻ RFID.
*   **Yêu cầu kỹ thuật:** Loại bỏ hoàn toàn hộp thoại modal trong luồng quét thẻ. Thay thế bằng khu vực hiển thị trạng thái động (Status Bar) trực quan trên giao diện và phát âm thanh tương ứng (Bíp thành công / Tít tít báo lỗi).
*   **Giải pháp chi tiết:**

#### 📑 Tệp 1: [AttendanceView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/AttendanceView.xaml)
*   *Mô tả thực hiện:* Thêm khu vực thông báo trạng thái quét thẻ RFID động ở vị trí phù hợp (trên Grid điểm danh):
    ```xml
    <!-- Đặt ngay phía trên DataGrid điểm danh -->
    <Border x:Name="BdrRfidStatus" Background="#F8FAFC" BorderBrush="#E2E8F0" BorderThickness="1" CornerRadius="8" Padding="12" Margin="0,0,0,15">
        <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
            <TextBlock x:Name="IconRfidStatus" Text="📡" FontSize="18" Margin="0,0,10,0" VerticalAlignment="Center"/>
            <TextBlock x:Name="TxtRfidStatus" Text="Trạng thái đầu đọc RFID: Đang chờ quét thẻ..." FontSize="14" FontWeight="SemiBold" Foreground="#475569" VerticalAlignment="Center"/>
        </StackPanel>
    </Border>
    ```

#### 📑 Tệp 2: [AttendanceView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/AttendanceView.xaml.cs)
*   *Mô tả thực hiện:* Sửa phương thức `ProcessCardScanAsync` để cập nhật trạng thái lên màn hình và phát âm thanh cảnh báo thay vì gọi `MessageBox.Show`.
*   *Dòng code sửa đổi:*
    *   *Trường hợp 1: Không khớp đoàn viên nào (Dòng 144-148):*
        ```csharp
        if (matchedMember == null)
        {
            TxtRfidStatus.Text = $"Quét thất bại: Thẻ '{cardUid}' không khớp với Đoàn viên nào!";
            TxtRfidStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Màu đỏ
            BdrRfidStatus.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Nền đỏ nhạt
            BdrRfidStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165));
            System.Media.SystemSounds.Hand.Play(); // Phát âm thanh cảnh báo lỗi
            return;
        }
        ```
    *   *Trường hợp 2: Không thuộc danh sách buổi sinh hoạt hiện tại (Dòng 175-178):*
        ```csharp
        else
        {
            TxtRfidStatus.Text = $"Cảnh báo: Đoàn viên '{matchedMember.StudentName}' không có trong danh sách buổi sinh hoạt này!";
            TxtRfidStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            BdrRfidStatus.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
            BdrRfidStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165));
            System.Media.SystemSounds.Hand.Play();
        }
        ```
    *   *Trường hợp 3: Quét thành công (Dòng 155-174):*
        ```csharp
        if (row != null)
        {
            if (!row.IsPresent)
            {
                row.IsPresent = true;
                row.Note = "Quét thẻ RFID";
                // ... (Logic cập nhật DB) ...
                UpdateStats();
                
                // Trực quan hóa thành công trên UI
                TxtRfidStatus.Text = $"Quét thành công: {matchedMember.StudentName} ({matchedMember.ClassName})";
                TxtRfidStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74)); // Màu xanh lá
                BdrRfidStatus.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); // Nền xanh nhạt
                BdrRfidStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208));
                System.Media.SystemSounds.Beep.Play(); // Phát tiếng bíp báo thành công
            }
            else
            {
                // Học sinh quét thẻ lại để kiểm tra
                TxtRfidStatus.Text = $"Thông tin: {matchedMember.StudentName} đã được điểm danh trước đó.";
                TxtRfidStatus.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6)); // Màu cam
                BdrRfidStatus.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)); // Nền cam nhạt
                BdrRfidStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138));
                System.Media.SystemSounds.Asterisk.Play(); // Âm thanh thông tin
            }
        }
        ```

---

## ═══ PHẦN III: VIỆT HÓA TOÀN DIỆN & CHUẨN HÓA THIẾT KẾ V4.1 (LOCALIZATION & DESIGN) ═══

### 1. Sửa lỗi hiển thị Tiếng Việt không dấu trên Dashboard
*   **Tệp tin sửa đổi:** [YouthUnionDashboard.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/YouthUnionDashboard.xaml.cs)
*   **Mô tả thực hiện:** Cập nhật các chuỗi không dấu thành Tiếng Việt chuẩn mực sư phạm.
    *   *Dòng 47:* `TxtActiveMembers.Text = $"Đang sinh hoạt: {active}";`
    *   *Dòng 49:* `TxtCompletedAct.Text = $"Đã hoàn thành: {completedActs}";`
    *   *Dòng 51:* `TxtFeePaid.Text = $"Đã đóng: {feeStats.Paid}/{total}";`

### 2. Thiết lập Converter dịch trạng thái Tiếng Anh sang Tiếng Việt chuẩn sư phạm
*   **Vấn đề:** Trạng thái hoạt động, đoàn phí, kế hoạch hiển thị trực tiếp chuỗi DB tiếng Anh như `"Completed"`, `"Paid"`, `"Draft"` trên UI.
*   **Yêu cầu kỹ thuật:** Viết các Converter lớp con kế thừa `IValueConverter` để chuyển ngữ tự động trên DataGrid và Badge hiển thị.
*   **Giải pháp chi tiết:**

#### 📑 Tệp 1: [MemberListView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/MemberListView.xaml) & [MemberListView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/MemberListView.xaml.cs)
*   *Mô tả thực hiện:* Thêm Converter cho Chức danh (`PositionConverter`) và Loại thành viên (`MemberTypeConverter`):
    ```csharp
    public class PositionConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string pos)
            {
                return pos switch {
                    "Thanh vien" => "Thành viên",
                    "Bi thu" => "Bí thư",
                    "Pho BT" => "Phó Bí thư",
                    "UV BCH" => "Ủy viên BCH",
                    _ => pos
                };
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
    ```

#### 📑 Tệp 2: [FeeTrackerView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/FeeTrackerView.xaml.cs)
*   *Mô tả thực hiện:* Việt hóa các trạng thái Đoàn phí hiển thị trong DataGrid.
    *   *Dòng 51:* `FeeDisplay = fee?.Status == "Paid" ? "Đã đóng" : "Chưa đóng",`
    *   *Dòng 58:* `if (statusFilter == "Paid") display = display.Where(x => x.FeeDisplay == "Đã đóng").ToList();`
    *   *Dòng 59:* `else if (statusFilter == "Unpaid") display = display.Where(x => x.FeeDisplay == "Chưa đóng").ToList();`
    *   *Dòng 63:* `var paid = display.Count(x => x.FeeDisplay == "Đã đóng");`

---

## ═══ PHẦN IV: BỔ SUNG CHỈ DẪN SỬ DỤNG TỪNG BƯỚC (STEP-BY-STEP UX GUIDES) ═══

Bắt buộc bổ sung Expander chỉ dẫn nghiệp vụ vào đầu các tệp XAML:

### 1. [MemberListView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/MemberListView.xaml)
*   "• **Bước 1:** Sử dụng thanh tìm kiếm hoặc bộ lọc lớp/trạng thái để khoanh vùng Đoàn viên - Đội viên cần xử lý."
*   "• **Bước 2:** Bấm đúp vào dòng dữ liệu học sinh hoặc bấm nút '✏️' để cập nhật thông tin chi tiết (Số điện thoại, Email, Số sổ Đoàn)."
*   "• **Bước 3:** Sử dụng chức năng 'Nhập Excel' / 'Xuất Excel' để đồng bộ danh sách học sinh hàng loạt nhanh chóng."

### 2. [FeeTrackerView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/FeeTrackerView.xaml)
*   "• **Bước 1:** Chọn Học kỳ/Kỳ thu phí cần thu trên thanh công cụ."
*   "• **Bước 2:** Lọc trạng thái 'Chưa đóng' để hiển thị các đoàn viên còn nợ phí."
*   "• **Bước 3:** Nhấn nút '✏️' hoặc chọn nhanh học sinh nộp tiền, đổi trạng thái sang 'Đã đóng' và lưu lại để cập nhật quỹ ngân sách."

*(Thực hiện bổ sung Card hướng dẫn tương tự cho tất cả các View còn lại)*

---

## ═══ THIẾT KẾ PHẢN BIỆN & PHÒNG VỆ KỸ THUẬT (TECHNICAL DEFENSE & CRITIQUE) ═══

1.  **Phản biện Chế độ Cấu hình mã hóa DB:** Việc cho phép cấu hình `DbEncodingMode` linh hoạt trên Master giải quyết triệt để sự đắn đo giữa việc tích hợp API và tính tối giản của CSDL. Các trường học liên kết với hệ thống phần mềm giáo dục cũ của Sở sẽ sử dụng **Chế độ 0 (ASCII/Legacy)**, trong khi các trường xây dựng cơ sở dữ liệu độc lập mới sẽ sử dụng **Chế độ 1 (Unicode)** để giảm tải logic chuyển đổi cho CPU.
2.  **Ràng buộc Kiểm định Form:** Ngăn chặn việc nhập số điện thoại sai định dạng mạng Việt Nam hoặc Email sai chuẩn RFC 5322 thông qua các Regex kiểm tra nghiêm ngặt trước khi lưu.

---

## ═══ BỘ CHECKSHEET KIỂM THỬ KHẮC PHỤC (TEST CHECKSHEET FOR QA/TESTER) ═══

### 📋 Checksheet 1: Kiểm thử logic chức năng & Cơ sở dữ liệu
*   [x] **Lọc danh sách Đoàn viên (Chế độ 0 - ASCII):** Cấu hình hệ thống sang Chế độ 0. Thêm Đoàn viên mới. Kiểm tra xem bộ lọc Loại thành viên hoạt động chính xác (không trả về danh sách rỗng).
*   [x] **Lọc danh sách Đoàn viên (Chế độ 1 - Unicode):** Cấu hình hệ thống sang Chế độ 1. Thêm Đoàn viên mới. Xác nhận bản ghi lưu trong DB SQLite có chứa chuỗi `"Đoàn viên"` có dấu, bộ lọc UI chạy mượt mà.
*   [x] **Đồng bộ danh sách đóng phí:** Xác nhận đoàn viên thêm mới xuất hiện đầy đủ ở trang đóng phí của Học kỳ tương ứng.
*   [x] **Kiểm tra quỹ chi tiêu:** Tạo giao dịch chi tiêu âm hoặc chi vượt số dư. Hệ thống phải ném ngoại lệ và hủy ghi dữ liệu để bảo vệ số dư.
*   [x] **Ngăn chặn Double-Click (Race Condition):** Bấm liên tiếp nút "Lưu" hoặc "Bình chọn" nhanh. Kiểm tra xem hệ thống có cơ chế Disable nút tức thời để tránh ghi đúp bản ghi hay không.

### 📋 Checksheet 2: Kiểm thử luồng quét thẻ RFID & Thiết bị ngoại vi
*   [x] **Kiểm thử thẻ không hợp lệ (Không Modal):** Quét thẻ lạ. Xác nhận không có MessageBox. UI đổi nền sang màu đỏ nhạt, báo lỗi, phát âm thanh lỗi. Cổng COM vẫn mở bình thường.
*   [x] **Kiểm thử điểm danh thành công:** Quét thẻ đúng. UI đổi nền xanh lá, hiển thị tên học sinh, phát âm thanh bíp ngắn, tích chọn "Có mặt" tự động.
*   [x] **Kiểm thử quét lặp thẻ:** Quét lại thẻ đã có mặt. Xác nhận UI báo trạng thái màu cam và phát âm thanh thông tin.
*   [x] **Kiểm thử khả năng chịu tải (Rapid Swiping):** Quét liên tục 10 thẻ trong 3 giây. Xác nhận hệ thống xử lý ổn định, không bỏ sót thẻ và không ném ngoại lệ hàng đợi (buffer overflow).
*   [x] **Mất kết nối cổng COM giữa chừng:** Rút đầu đọc RFID trong lúc đang chạy. Kiểm tra xem phần mềm có tự động bắt lỗi (Catch SerialException), hiển thị cảnh báo đỏ "Đầu đọc ngắt kết nối" và không làm sập ứng dụng hay không.

### 📋 Checksheet 3: Kiểm thử giao diện, Việt hóa & Sư phạm
*   [x] **Mã hóa tiếng Việt có dấu:** Mở tất cả các View, xác nhận không còn ký tự lạ (`?`, ``) trên nhãn và lưới dữ liệu.
*   [x] **Hiển thị tiếng Việt trên DataGrid:** Đảm bảo các cột Chức vụ, Loại thành viên, Trạng thái hiển thị tiếng Việt có dấu đầy đủ (`Bí thư`, `Đoàn viên`, `Đang hoạt động`) thay vì giá trị DB gốc.
*   [x] **Co giãn bố cục:** Co nhỏ cửa sổ ứng dụng về mức tối thiểu, kiểm tra xem thanh cuộn ScrollViewer có xuất hiện tự động và không có thông tin hay nút bấm nào bị che khuất hoặc tràn lề.
*   [x] **Kiểm tra độ phủ Chỉ dẫn sử dụng:** Mở lần lượt 11 trang giao diện của phân hệ. Xác nhận mỗi trang đều xuất hiện khung "Hướng dẫn nghiệp vụ từng bước" màu xanh/xanh dương nhẹ ở vị trí dễ nhìn.

---

## ═══ BỔ SUNG: DANH SÁCH 25 KỊCH BẢN KIỂM THỬ CHI TIẾT (EXTENDED TEST CASES) ═══

Dưới đây là đặc tả các Kịch bản kiểm thử (Test Case) chi tiết để đội ngũ QA kiểm thử thủ công và tự động, đảm bảo coder không thể làm sai:

### 1. Nhóm kịch bản về RFID & Điểm danh (RFID & Attendance)

| ID | Tên kịch bản | Dữ liệu đầu vào (Input) | Điều kiện mong đợi (Expected Output) | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| **TC-RFID-01** | Quét thẻ rỗng/nhiễu | Chuỗi nhận được chứa ký tự rác hoặc rỗng (`"\n"`, `""`) | Hệ thống lọc nhiễu, bỏ qua và giữ nguyên trạng thái chờ. Không báo lỗi. | [x] |
| **TC-RFID-02** | Quét thẻ không thuộc DB | Thẻ UID: `"999999"` không tồn tại trong hệ thống | BdrRfidStatus nền đỏ, hiện báo lỗi thẻ không hợp lệ. Phát âm thanh lỗi, không chặn luồng quét. | [x] |
| **TC-RFID-03** | Điểm danh thành công | Thẻ UID của học sinh Nguyễn Văn A (Đoàn viên lớp 10A1) | BdrRfidStatus nền xanh lá, hiện tên và lớp Nguyễn Văn A. Phát âm thanh bíp thành công. Checkbox "Có mặt" của Nguyễn Văn A đổi thành `true`. | [x] |
| **TC-RFID-04** | Điểm danh lặp | Quét lại thẻ của Nguyễn Văn A | BdrRfidStatus nền màu cam, thông báo đã điểm danh trước đó. Phát âm thanh thông tin. Không ghi đè dữ liệu. | [x] |
| **TC-RFID-05** | Quét thẻ sai buổi sinh hoạt | Đoàn viên thuộc lớp 11A2 quét thẻ trong buổi sinh hoạt riêng của khối 10 | BdrRfidStatus nền đỏ, hiện cảnh báo học sinh không thuộc danh sách buổi sinh hoạt này. | [x] |
| **TC-RFID-06** | Mất kết nối cổng COM đột ngột | Rút cáp USB đầu đọc RFID ra khỏi máy tính khi đang hiển thị trang | Hệ thống bắt được sự kiện ngắt kết nối, hiện thông báo "Ngắt kết nối đầu đọc RFID", nút quét thẻ bị mờ đi (Disable). | [x] |
| **TC-RFID-07** | Quét dồn dập (Load Test) | Quét liên tục 15 thẻ khác nhau với giãn cách 200ms/thẻ | Hệ thống đưa vào hàng đợi xử lý tuần tự mà không bị treo UI. 15 học sinh được tích điểm danh đúng thứ tự. | [x] |

### 2. Nhóm kịch bản về Quản lý Đoàn viên & Lọc Dữ liệu (Member Management & Filters)

| ID | Tên kịch bản | Dữ liệu đầu vào (Input) | Điều kiện mong đợi (Expected Output) | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| **TC-MEM-01** | Lọc theo loại thành viên (Mode 0) | Chọn loại lọc: `"Đoàn viên"` | Hiển thị chính xác các đoàn viên trong grid. RunTotal đếm đúng số lượng. (Không trả về rỗng nhờ bộ ánh xạ). | [x] |
| **TC-MEM-02** | Thêm mới Đoàn viên từ UI | Họ tên: `"Trần Thị B"`, Loại: `"Đội viên"`, Chức vụ: `"Bí thư"` | Bản ghi ghi xuống SQLite có `MemberType = "Doi vien"`, `Position = "Bi thu"` (Mode 0) HOẶC `"Đội viên"`, `"Bí thư"` (Mode 1) theo cấu hình Master. | [x] |
| **TC-MEM-03** | Nhập Excel sai tên lớp | Tệp Excel có học sinh ghi lớp `"12A_Sửa"` không tồn tại trong danh mục | Hệ thống hiển thị hộp thoại chỉ rõ: "Lỗi dòng 5: Tên lớp không hợp lệ." và cho phép lựa chọn bỏ qua dòng này hoặc hủy toàn bộ tiến trình. | [x] |
| **TC-MEM-04** | Nhập Excel thiếu dữ liệu bắt buộc | Cột "Họ và Tên" dòng 8 bị trống | Báo lỗi cụ thể dòng số 8 thiếu Họ tên và bỏ qua dòng này để nhập tiếp các dòng khác. | [x] |
| **TC-MEM-05** | Kiểm tra trùng lặp Số sổ Đoàn | Thêm đoàn viên mới có số sổ trùng với đoàn viên đã có | Hệ thống hiển thị cảnh báo: "Số sổ Đoàn này đã tồn tại!" và từ chối lưu. | [x] |
| **TC-MEM-06** | Kiểm định định dạng Email | Nhập email: `"abc@school"` hoặc `"abc.com"` | Hiển thị cảnh báo định dạng Email không hợp lệ (Ví dụ: doanvien@school.edu.vn). | [x] |
| **TC-MEM-07** | Kiểm định số điện thoại | Nhập SĐT: `"012345"` (quá ngắn) hoặc `"abc1234567"` | Hiển thị cảnh báo số điện thoại phải chứa đúng 10 chữ số. | [x] |

### 3. Nhóm kịch bản về Đoàn phí & Quản lý Tài chính (Fees & Budgets)

| ID | Tên kịch bản | Dữ liệu đầu vào (Input) | Điều kiện mong đợi (Expected Output) | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| **TC-FIN-01** | Thu phí nhanh từ UI | Chọn học sinh Nguyễn Văn A, chọn "Đổi sang Đã đóng" | Đổi trạng thái từ "Unpaid" sang "Paid". Trường `PaidDate` tự động cập nhật ngày hôm nay. Số dư quỹ tăng tương ứng. | [x] |
| **TC-FIN-02** | Lập dự chi vượt quá quỹ | Số dư quỹ: 1.000.000đ. Tạo giao dịch chi tiêu: 1.200.000đ | Hệ thống ném ngoại lệ `InvalidOperationException`, chặn lưu dữ liệu và cảnh báo số dư quỹ không đủ. | [x] |
| **TC-FIN-03** | Trùng lặp mã giao dịch | Bấm lưu chi tiêu liên tiếp 2 lần thật nhanh | Nút "Lưu" tự động mờ đi (Disable) ngay sau lần bấm đầu tiên. Giao dịch chỉ được ghi nhận một lần duy nhất. | [x] |
| **TC-FIN-04** | Chuyển đổi trạng thái đóng phí hàng loạt | Chọn toàn bộ chi đoàn 10A1, chọn "Đã đóng phí HK1" | Toàn bộ thành viên lớp 10A1 được cập nhật trạng thái "Paid". Doanh thu hiển thị trên Dashboard cập nhật tức thời. | [x] |

### 4. Nhóm kịch bản về Biểu quyết & Khen thưởng (Voting & Awards)

| ID | Tên kịch bản | Dữ liệu đầu vào (Input) | Điều kiện mong đợi (Expected Output) | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| **TC-VOT-01** | Kiểm tra Bỏ phiếu ẩn danh | Học sinh bỏ phiếu cho Ứng cử viên A | Bản ghi trong `YouthVotes` lưu `VoterId = 0` (đảm bảo tính ẩn danh), đồng thời `YouthVoterRegistry` ghi nhận `VoterId = học sinh` để tránh bỏ phiếu lần 2. | [x] |
| **TC-VOT-02** | Bỏ phiếu lần 2 | Học sinh đã có tên trong `YouthVoterRegistry` cố gắng bỏ phiếu lại | Hệ thống trả về `false`, từ chối nhận phiếu và thông báo "Bạn đã thực hiện biểu quyết này rồi". | [x] |
| **TC-VOT-03** | Khóa phiên biểu quyết | Quản trị viên đóng phiên biểu quyết | Trạng thái đổi sang "Closed". Tất cả học sinh truy cập sau thời điểm này đều không thể bỏ phiếu, nút "Bỏ phiếu" bị ẩn. | [x] |
| **TC-VOT-04** | Đề xuất khen thưởng tập thể | Chọn đề xuất loại tập thể cho lớp 12A1 | Yêu cầu nhập lý do. Khi phê duyệt, hệ thống cập nhật trạng thái "Approved" và ghi nhận thông tin quyết định khen thưởng. | [x] |

### 5. Nhóm kịch bản về Chuyển đổi Master & Đồng bộ (Master Switching & Migration)

| ID | Tên kịch bản | Dữ liệu đầu vào (Input) | Điều kiện mong đợi (Expected Output) | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| **TC-MST-01** | Thay đổi chế độ mã hóa giữa kỳ | Đang chạy Chế độ 0 (ASCII), chuyển cấu hình sang Chế độ 1 (Unicode) | Hệ thống tự động kích hoạt bộ chuyển đổi ngầm (Data Migrator) chuẩn hóa các bản ghi cũ từ không dấu sang có dấu để đảm bảo tính nhất quán dữ liệu mà không làm mất thông tin. | [x] |
| **TC-MST-02** | Nhập danh sách Excel khi đang ở Chế độ 1 | Tệp Excel có chữ có dấu ("Đoàn viên", "Bí thư") | Hệ thống lưu trực tiếp chuỗi có dấu vào SQLite mà không qua bộ ánh xạ. Bộ lọc hoạt động tốt. | [x] |

---

*Tài liệu hướng dẫn nâng cấp đã được phê duyệt chính thức bởi Trưởng ban thiết kế dự án. Bàn giao cho bộ phận coder và QA kiểm thử.*
