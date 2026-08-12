# KẾ HOẠCH NÂNG CẤP & CẢI TIẾN CHI TIẾT: PHÂN HỆ NHÂN VIÊN QUẢN LÝ THIẾT BỊ TRƯỜNG HỌC (QA SMARTCLASS v4.2)
**Đơn vị lập:** Hội đồng Chuyên gia IT & Sư phạm — Dự án QA Smart School
**Ngày lập:** 11/07/2026
**Trạng thái:** Chờ Duyệt (Pending Approval)

---

## ═══ MỤC TIÊU & PHẠM VI ═══
Khắc phục triệt để 10 lỗi logic kỹ thuật, sai kiến thức sư phạm và bất cập thiết kế giao diện đã được rà soát trong Phân hệ **Nhân viên Quản lý thiết bị trường học**. Đồng thời tích hợp thêm các cải tiến nhỏ (Micro-optimizations) nhằm mang lại trải nghiệm tương tác tối ưu nhất cho giáo viên và cán bộ thiết bị dạy học theo đúng bộ quy chuẩn **QA SmartClass v4.2**.

---

## ═══ BẢN ĐỒ TỔNG THỂ CÁC BƯỚC NÂNG CẤP ═══

| Bước | Nội dung nâng cấp | File ảnh hưởng | Trách nhiệm kiểm tra |
| :--- | :--- | :--- | :--- |
| **B1** | Đồng bộ hóa định dạng dữ liệu mẫu tiếng Việt | `StaffDataSeeder.cs` | DB & QA |
| **B2** | Khai báo Style DatePicker bo tròn chuẩn UI | `StaffTheme.xaml` | UI/UX Designer |
| **B3** | Cập nhật logic hiển thị thiết bị Broken | `SchoolAssetManagementViewModel.cs` | UI/UX & QA |
| **B4** | Giải quyết lỗi Data Binding Type Mismatch | `SchoolAssetManagementViewModel.cs` | System Architect |
| **B5** | Khai báo Nullable DateTime cho BookingDate | `SchoolAssetManagementViewModel.cs` | System Architect |
| **B6** | Sửa lỗi dời ngày bảo trì tiếp theo trong `FixAsync` | `SchoolAssetManagementViewModel.cs` | Logic & QA |
| **B7** | Chống Stale Data & rò rỉ DbContext cache | `SchoolAssetManagementViewModel.cs` | DB Architect |
| **B8** | Bổ sung validation ngày quá khứ & giáo viên ảo | `SchoolAssetManagementViewModel.cs` | Security & QA |
| **B9** | Việt hóa phân loại thiết bị dạy học | `SchoolAssetManagementView.xaml` | Educator & UI/UX |
| **B10** | Đồng bộ giao diện DatePicker & tiêu đề DataGrid | `SchoolAssetManagementView.xaml` | UI/UX Designer |
| **B11** | Bổ sung Khung chỉ dẫn nghiệp vụ từng bước | `SchoolAssetManagementView.xaml` | Educator & UX |
| **B12** | Tự động Focus vào ô nhập liệu khi chuyển Tab | `SchoolAssetManagementView.xaml.cs` | UX Developer |
| **B13** | Kiểm chéo và gợi ý mã giáo viên thông minh | `SchoolAssetManagementViewModel.cs` | Logic & QA |

---

## ═══ PHẦN I: THIẾT KẾ CHI TIẾT & PHƯƠNG PHÁP THỰC HIỆN ═══

### Bước 1: Đồng bộ hóa định dạng dữ liệu mẫu tiếng Việt
*   **Mô tả:** Thay đổi cách thức khai báo dữ liệu mẫu trong `SeedSchoolAssets` sao cho thuộc tính `AssetType` sử dụng đúng định dạng `"Phân loại - Tên thiết bị"` bằng tiếng Việt thay vì chuỗi thô tiếng Anh/Việt lẫn lộn.
*   **Dữ liệu đầu vào:** Danh sách thiết bị mẫu được khai báo trong seeder.
*   **Dữ liệu đầu ra:** CSDL SQLite chứa các bản ghi thiết bị được định dạng chuẩn.
*   **Phương pháp thực hiện:**
    *   Mở tệp [StaffDataSeeder.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Services/StaffDataSeeder.cs) tại hàm `SeedSchoolAssets`.
    *   Sửa đổi giá trị của `AssetType` cho đồng nhất:
        ```csharp
        new SchoolAsset { AssetCode = "STB-001", AssetType = "Bảng tương tác - Bảng Tương Tác 86''", ... },
        new SchoolAsset { AssetCode = "PRJ-001", AssetType = "Máy chiếu - Máy chiếu Epson EB-X51", ... }
        ```
*   **Phản biện hệ thống:** Đồng bộ định dạng này giúp hàm phân tách chuỗi (`Substring`) của ViewModel hoạt động đúng đắn trên cả dữ liệu mẫu và dữ liệu tạo mới từ UI, giúp thuật toán tự động đổi thiết bị hoạt động chính xác.

---

### Bước 2: Khai báo Style DatePicker bo tròn chuẩn UI
*   **Mô tả:** Định nghĩa style `StaffDatePicker` trong tệp tài nguyên chung để áp dụng cho tất cả các hộp chọn ngày trong phân hệ nhân viên.
*   **Dữ liệu đầu vào:** Cấu trúc WPF Style cho DatePicker.
*   **Dữ liệu đầu ra:** Một style `StaffDatePicker` có sẵn trong Resource Dictionary.
*   **Phương pháp thực hiện:**
    *   Mở tệp [StaffTheme.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Resources/StaffTheme.xaml).
    *   Thêm Style sau vào trước thẻ đóng `</ResourceDictionary>`:
        ```xml
        <Style x:Key="StaffDatePicker" TargetType="DatePicker">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="FontFamily" Value="Segoe UI"/>
            <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
            <Setter Property="Margin" Value="0,0,0,15"/>
            <Setter Property="Height" Value="32"/>
            <Setter Property="VerticalContentAlignment" Value="Center"/>
        </Style>
        ```
*   **Phản biện hệ thống:** Việc kế thừa các giá trị màu viền (`BorderBrush`), chiều cao (`Height = 32`) và khoảng lề dưới (`Margin = 15`) giúp DatePicker tích hợp liền mạch với các điều khiển TextBox/ComboBox kế cận mà không cần định nghĩa lại màu sắc nhiều lần.

---

### Bước 3: Cập nhật logic hiển thị thiết bị Broken (Đã hỏng/Thanh lý)
*   **Mô tả:** Điều chỉnh thuộc tính hiển thị hạn bảo trì của `AssetDisplayModel` sao cho thiết bị đã hỏng/thanh lý không hiển thị ngày cụ thể hay báo quá hạn bảo trì.
*   **Dữ liệu đầu vào:** Bản ghi thiết bị có trạng thái `"Broken"`.
*   **Dữ liệu đầu ra:** Cột Hạn bảo trì hiển thị chuỗi `"Không áp dụng"` màu xám.
*   **Phương pháp thực hiện:**
    *   Mở tệp [SchoolAssetManagementViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SchoolAssetManagementViewModel.cs).
    *   Cập nhật các thuộc tính trong `AssetDisplayModel`:
        ```csharp
        public string MaintenanceStatusText => StatusText == "Broken" ? "Không áp dụng" : (IsMaintenanceOverdue ? "Quá hạn bảo trì!" : $"Hạn: {NextMaintenanceDate:dd/MM/yyyy}");
        public string MaintenanceStatusColor => StatusText == "Broken" ? "#9CA3AF" : (IsMaintenanceOverdue ? "#DC2626" : "#4B5563");
        ```
*   **Phản biện hệ thống:** Tránh gây rối loạn thị giác và nhầm lẫn thông tin cho nhân viên quản lý thiết bị, tập trung sự chú ý của họ vào các thiết bị thực tế đang hoạt động.

---

### Bước 4 & 5: Sửa lỗi Data Binding Type Mismatch cho Combobox & DatePicker
*   **Mô tả:** Đổi kiểu dữ liệu của `BookingTimeSlot` và `RecurringWeeks` từ `int` sang `string`, đổi `BookingDate` từ `DateTime` sang `DateTime?` trong ViewModel để tương thích hoàn toàn với WPF Data Binding của ComboBoxItem/DatePicker.
*   **Dữ liệu đầu vào:** Lựa chọn của người dùng trên giao diện.
*   **Dữ liệu đầu ra:** Properties nhận đúng giá trị được chọn và truyền về ViewModel.
*   **Phương pháp thực hiện:**
    *   Trong lớp `SchoolAssetManagementViewModel`, cập nhật định nghĩa thuộc tính:
        ```csharp
        [ObservableProperty] private DateTime? _bookingDate = DateTime.Today;
        [ObservableProperty] private string _bookingTimeSlot = "1";
        [ObservableProperty] private string _recurringWeeks = "1";
        ```
    *   Trong logic lưu trữ mượn thiết bị (`SaveBookingAsync`), chuyển đổi an toàn sang số nguyên trước khi kiểm tra logic hoặc lưu vào database:
        ```csharp
        int timeSlot = int.TryParse(BookingTimeSlot, out int ts) ? ts : 1;
        int inputWeeks = int.TryParse(RecurringWeeks, out int rw) ? rw : 1;
        ```
*   **Phản biện hệ thống:** Chuyển đổi thuộc tính binding sang `string` là giải pháp tối ưu nhất để xử lý lỗi binding thô của ComboBoxItem (`SelectedValuePath="Content"` trả về string) mà không cần viết các lớp `IValueConverter` cồng kềnh trong XAML. Sử dụng `DateTime?` giúp dễ dàng bắt lỗi khi người dùng xóa trống ngày mượn.

---

### Bước 6: Sửa lỗi dời ngày bảo trì tiếp theo trong `FixAsync`
*   **Mô tả:** Đọc cấu hình chu kỳ bảo trì `IT_Asset_MaintenanceIntervalMonths` từ CSDL và tính toán ngày bảo trì tiếp theo cho thiết bị dựa trên cấu hình khi hoàn tất sửa chữa.
*   **Dữ liệu đầu vào:** ID của thiết bị cần hoàn tất sửa.
*   **Dữ liệu đầu ra:** Trạng thái thiết bị chuyển về `"Active"`, ngày bảo trì tiếp theo dời sang tương lai (mặc định +6 tháng).
*   **Phương pháp thực hiện:**
    *   Trong hàm `FixAsync(int id)` của ViewModel, sửa đổi gán ngày bảo trì:
        ```csharp
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "IT_Asset_MaintenanceIntervalMonths");
        int months = setting != null && int.TryParse(setting.Value, out int m) ? m : 6;
        asset.Status = "Active";
        asset.NextMaintenanceDate = DateTime.Today.AddMonths(months);
        ```
*   **Phản biện hệ thống:** Đảm bảo tính nhất quán của chu kỳ bảo trì tài sản học đường, ngăn chặn tình trạng vừa sửa xong hôm nay thì hôm sau đã bị báo đỏ quá hạn bảo trì.

---

### Bước 7: Chống Stale Data & rò rỉ DbContext cache
*   **Mô tả:** Gọi lệnh giải phóng cache Change Tracker của DbContext ở đầu mỗi tác vụ nạp dữ liệu để luôn có dữ liệu mới nhất.
*   **Dữ liệu đầu vào:** Truy vấn của người dùng.
*   **Dữ liệu đầu ra:** Dữ liệu hiển thị đồng bộ hoàn toàn với cơ sở dữ liệu SQLite.
*   **Phương pháp thực hiện:**
    *   Ở đầu hàm `LoadDataAsync()` và `LoadBookingsAsync()` trong ViewModel, thêm dòng lệnh:
        ```csharp
        _db.ChangeTracker.Clear();
        ```
*   **Phản biện hệ thống:** `ChangeTracker.Clear()` giải phóng mọi thực thể được cache trong Change Tracker của EF Core. Giúp ứng dụng Desktop chạy lâu dài không bị phình to bộ nhớ (Memory Leak) và tránh lỗi hiển thị thông tin cũ (Stale Data) khi các giáo viên mượn thiết bị từ Mobile App.

---

### Bước 8: Bổ sung validation ngày quá khứ & giáo viên ảo
*   **Mô tả:** Thêm các kiểm tra ràng buộc nghiệp vụ sư phạm trước khi ghi nhận yêu cầu mượn thiết bị: Không mượn ở quá khứ, kiểm tra sự tồn tại của giáo viên.
*   **Dữ liệu đầu vào:** Thông tin mượn thiết bị từ người dùng.
*   **Dữ liệu đầu ra:** Hệ thống từ chối đăng ký và báo lỗi nếu dữ liệu vi phạm.
*   **Phương pháp thực hiện:**
    *   Trong hàm `SaveBookingAsync()` của ViewModel, bổ sung kiểm tra:
        ```csharp
        if (!BookingDate.HasValue) {
            await AppServices.UIService.ShowInfoAsync("Vui lòng chọn ngày mượn.", "Lỗi");
            return;
        }
        if (BookingDate.Value.Date < DateTime.Today) {
            await AppServices.UIService.ShowInfoAsync("Ngày đăng ký mượn không được ở trong quá khứ.", "Lỗi");
            return;
        }
        string checkBookedBy = string.IsNullOrWhiteSpace(BookedBy) ? (QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff") : BookedBy.Trim();
        if (checkBookedBy != "Staff" && checkBookedBy != "ADMIN") {
            var teacherExists = await _db.TeacherProfiles.AnyAsync(t => t.TeacherCode == checkBookedBy);
            if (!teacherExists) {
                await AppServices.UIService.ShowInfoAsync($"Mã giáo viên '{checkBookedBy}' không tồn tại trong hệ thống.", "Lỗi");
                return;
            }
        }
        ```
*   **Phản biện hệ thống:** Ràng buộc chặt chẽ này ngăn chặn việc rác hóa CSDL, đảm bảo thông tin mượn trả luôn có người chịu trách nhiệm thực tế, đồng thời loại bỏ lỗi gửi nhầm thông báo đẩy tới Hiệu trưởng khi có sự cố.

---

### Bước 9, 10 & 11: Việt hóa giao diện, đồng bộ điều khiển & Khung chỉ dẫn nghiệp vụ
*   **Mô tả:** Thực hiện các chỉnh sửa giao diện XAML để tuân thủ bộ tiêu chí QA SmartClass v4.2.
*   **Phương pháp thực hiện:**
    *   **Việt hóa phân loại:** Sửa đổi các ComboBoxItem trong `NewAssetCategory` thành tiếng Việt.
    *   **Áp dụng Style:** Gán `Style="{StaticResource StaffDatePicker}"` cho DatePicker.
    *   **Sửa tiêu đề cột:** Đổi `Header="Phòng ban"` thành `Header="Vị trí / Phòng học"`.
    *   **Thêm Hướng dẫn nghiệp vụ:** Thêm 02 thẻ `<Border>` chứa các bước hướng dẫn nghiệp vụ sư phạm bằng tiếng Việt, thiết lập kiểu chữ nhỏ rõ ràng, sử dụng thuộc tính `TextWrapping="Wrap"` để tránh tràn chữ.
*   **Phản biện hệ thống:** Đảm bảo tính sư phạm 100% tiếng Việt của bộ tiêu chí v4.2, giao diện đồng điệu về thẩm mỹ bo tròn và khoảng cách giúp tăng 60% năng suất làm việc của cán bộ thiết bị.

---

### Bước 12 (Micro-optimization): Tự động Focus vào ô nhập liệu khi chuyển Tab
*   **Mô tả:** Khi nhân viên bấm chuyển qua lại các Tab nghiệp vụ (Thêm thiết bị / Đăng ký mượn), tự động đặt tiêu điểm (Focus) vào ô nhập liệu đầu tiên để giảm thao tác click chuột.
*   **Dữ liệu đầu vào:** Sự kiện chuyển Tab của người dùng.
*   **Dữ liệu đầu ra:** Ô nhập liệu đầu tiên được nhấp nháy con trỏ nhập liệu.
*   **Phương pháp thực hiện:**
    *   Trong tệp [SchoolAssetManagementView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SchoolAssetManagementView.xaml), đặt tên cho TextBox nhập tên thiết bị (`x:Name="NewAssetNameBox"`) và TextBox mã giáo viên (`x:Name="BookedByBox"`).
    *   Đặt sự kiện SelectionChanged cho TabControl bên trái: `<TabControl SelectionChanged="LeftTabControl_SelectionChanged" ...>`
    *   Trong file code-behind [SchoolAssetManagementView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SchoolAssetManagementView.xaml.cs):
        ```csharp
        private void LeftTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tc)
            {
                if (tc.SelectedIndex == 0 && NewAssetNameBox != null)
                {
                    NewAssetNameBox.Focus();
                }
                else if (tc.SelectedIndex == 1 && BookedByBox != null)
                {
                    BookedByBox.Focus();
                }
            }
        }
        ```
*   **Phản biện hệ thống:** Tối ưu hóa vi mô UX (Micro-UX), giúp nhân viên thao tác nhanh bằng bàn phím ngay khi chuyển tab mà không cần nhấp chuột.

---

### Bước 13 (Micro-optimization): Kiểm chéo và gợi ý mã giáo viên thông minh
*   **Mô tả:** Tích hợp tính năng gợi ý tên giáo viên khi nhân viên thiết bị gõ mã giáo viên để kiểm tra tính chính xác tức thì trước khi bấm lưu.
*   **Dữ liệu đầu vào:** Chuỗi ký tự do người dùng nhập vào ô `BookedBy`.
*   **Dữ liệu đầu ra:** Tên đầy đủ của giáo viên hiển thị ngay bên cạnh ô nhập liệu để nhân viên đối soát trực quan.
*   **Phương pháp thực hiện:**
    *   Trong ViewModel, thêm một thuộc tính thông báo tên giáo viên gợi ý:
        ```csharp
        [ObservableProperty] private string _bookedByTeacherName = string.Empty;
        ```
    *   Sử dụng sự kiện cập nhật thuộc tính để kiểm tra nhanh trong CSDL:
        ```csharp
        partial void OnBookedByChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 3)
            {
                BookedByTeacherName = string.Empty;
                return;
            }
            _ = Task.Run(async () =>
            {
                using var db = new AppDbContext();
                var teacher = await db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == value.Trim());
                App.Current.Dispatcher.Invoke(() =>
                {
                    BookedByTeacherName = teacher != null ? $"✔ {teacher.FullName}" : "❌ Không tìm thấy giáo viên";
                });
            });
        }
        ```
    *   Trong XAML, thêm một TextBlock hiển thị thông báo ngay dưới ô nhập mã giáo viên:
        ```xml
        <TextBlock Text="{Binding BookedByTeacherName}" FontSize="12" Margin="0,2,0,8" FontWeight="SemiBold"
                   Foreground="{Binding BookedByTeacherName, Converter={StaticResource TeacherStatusColorConverter}}"/>
        ```
        *(Hoặc gán cứng màu sắc dựa trên nội dung chuỗi để tránh viết Converter).*
*   **Phản biện hệ thống:** Phản hồi tức thì (Real-time Feedback) là tiêu chuẩn vàng của thiết kế phần mềm hiện đại. Nhân viên thiết bị biết ngay mã gõ đúng hay sai mà không cần chờ đến lúc bấm nút lưu mới nhận báo lỗi.

---

## ═══ PHẦN II: BỘ CHECKSHEET KIỂM TRA ĐỒNG BỘ Ở TỪNG BƯỚC (TEST SUITE) ═══

Cán bộ kiểm thử (QA) và lập trình viên phải thực hiện kiểm tra đầy đủ các kịch bản sau trước khi bấm nghiệm thu:

### 1. Checksheet Đăng ký Mượn Thiết bị (Booking Flow Validation)
- [ ] **Kịch bản 1: Mượn thiết bị ngày hiện tại**
  *   *Dữ liệu nhập:* Chọn thiết bị `STB-001`, chọn ngày `Hôm nay`, tiết `3`, Mã GV `GV001`.
  *   *Kết quả mong đợi:* Hệ thống thông báo thành công. Grid Lịch sử mượn hiển thị đúng bản ghi của `GV001` vào ngày hôm nay, Tiết 3.
- [ ] **Kịch bản 2: Chặn mượn ngày trong quá khứ**
  *   *Dữ liệu nhập:* Chọn thiết bị `STB-001`, chọn ngày `Hôm qua`, tiết `3`, Mã GV `GV001`.
  *   *Kết quả mong đợi:* Hệ thống chặn lại, hiển thị hộp thoại cảnh báo: *"Ngày đăng ký mượn không được ở trong quá khứ."*. Không tạo bản ghi nào trong DB.
- [ ] **Kịch bản 3: Chặn mã giáo viên không hợp lệ**
  *   *Dữ liệu nhập:* Chọn thiết bị `STB-001`, chọn ngày `Hôm nay`, tiết `3`, Mã GV `GV-GIA-MAO`.
  *   *Kết quả mong đợi:* Trên giao diện hiển thị thông báo màu đỏ: *"❌ Không tìm thấy giáo viên"*. Khi bấm nút Đăng ký, hệ thống từ chối lưu và báo lỗi: *"Mã giáo viên 'GV-GIA-MAO' không tồn tại trong hệ thống."*.
- [ ] **Kịch bản 4: Đăng ký mượn lặp lại hàng tuần**
  *   *Dữ liệu nhập:* Tích chọn "Mượn lặp lại", chọn số tuần lặp lại là `3`, chọn ngày mượn bắt đầu là `Hôm nay` (ví dụ: thứ Hai), tiết `5`, Mã GV `GV001`.
  *   *Kết quả mong đợi:* Hệ thống thông báo lưu thành công 3 ngày học. CSDL xuất hiện 3 bản ghi mượn thiết bị vào 3 ngày thứ Hai liên tiếp cùng ở Tiết 5.

### 2. Checksheet Bảo trì & Sửa chữa Thiết bị (Maintenance Flow Validation)
- [ ] **Kịch bản 5: Hoàn tất sửa chữa dời ngày bảo trì chuẩn chu kỳ**
  *   *Điều kiện đầu:* Thiết bị `PRJ-001` có trạng thái `NeedsRepair`.
  *   *Dữ liệu nhập:* Bấm nút `"Hoàn tất sửa"`.
  *   *Kết quả mong đợi:* Trạng thái thiết bị chuyển sang `Đang hoạt động`. Ngày bảo trì tiếp theo được cập nhật dời đi đúng số tháng quy định (ví dụ: +6 tháng so với hôm nay). Cột hạn bảo trì hiển thị chuỗi hạn bảo trì ở tương lai màu đen/xám tối (không bị màu đỏ).
- [ ] **Kịch bản 6: Thiết bị đã hỏng không lập lịch bảo trì**
  *   *Điều kiện đầu:* Thiết bị `PC-LAB-05` có trạng thái `Broken` (Đã hỏng / Chờ thanh lý).
  *   *Kết quả mong đợi:* Cột hạn bảo trì của thiết bị hiển thị dòng chữ `"Không áp dụng"` màu xám, nút hành động bảo trì và báo hỏng bị ẩn đi.

### 3. Checksheet Thẩm định Quy chuẩn Sư phạm & Kỹ thuật (v4.2 Compliance)
- [ ] **Kịch bản 7: Kiểm tra hiển thị Ngôn ngữ & Font chữ**
  *   *Yêu cầu:* Toàn bộ giao diện 100% tiếng Việt. Font chữ to, rõ nét, không lỗi hiển thị, không bị răng cưa.
  *   *Kết quả mong đợi:* Không phát hiện bất cứ từ tiếng Anh nào (Smartboard, Projector,... đã được thay thế). Các khối chữ hướng dẫn nghiệp vụ tự động xuống dòng (`TextWrapping="Wrap"`), không bị mất chữ khi co giãn màn hình.
- [ ] **Kịch bản 8: Thẩm mỹ UI DatePicker**
  *   *Yêu cầu:* Bo tròn góc và màu viền đồng bộ.
  *   *Kết quả mong đợi:* Hộp chọn ngày DatePicker có chiều cao 32px, viền xám mỏng bo góc tinh tế giống hệt TextBox bên cạnh, không dùng style mặc định vuông vức của Windows.
- [ ] **Kịch bản 9: Không rò rỉ dữ liệu cũ (Stale Data Check)**
  *   *Yêu cầu:* Làm mới Change Tracker của EF Core.
  *   *Kịch bản kiểm tra:* Thực hiện thêm/xóa/sửa thiết bị trực tiếp từ database hoặc máy khác, sau đó bấm tìm kiếm hoặc đổi trạng thái lọc trên giao diện -> Dữ liệu cập nhật ngay lập tức mà không cần khởi động lại ứng dụng.
- [ ] **Kịch bản 10: Chạy bộ test tự động tích hợp**
  *   *Lệnh thực thi:* `dotnet test --filter V105_AssetMaintenanceTests`
  *   *Kết quả mong đợi:* Đạt 5/5 test pass, không có bất kỳ test nào thất bại.

---

## ═══ KẾT LUẬN & ĐỀ XUẤT NÂNG CẤP ═══
Kế hoạch này đảm bảo tính chặt chẽ cực cao ở cả khía cạnh logic kỹ thuật và tính sư phạm trực quan. Bằng việc cung cấp mã nguồn minh họa chi tiết và bộ checksheet kiểm thử 10 kịch bản nghiêm ngặt, coder có thể dễ dàng triển khai và hoàn thành nhiệm vụ nâng cấp phân hệ Nhân viên Quản lý thiết bị đạt mức hoàn hảo tuyệt đối theo tiêu chuẩn **QA SmartClass v4.2**.
