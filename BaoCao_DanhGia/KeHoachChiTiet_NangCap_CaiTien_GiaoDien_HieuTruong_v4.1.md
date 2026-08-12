# Kế hoạch Chi tiết Nâng cấp và Cải tiến Giao diện Hiệu trưởng & BGH (Leadership Module)
**Tài liệu hướng dẫn phát triển và kiểm thử - Chuẩn QA SmartClass v4.1**  

---

## ═══ TỔNG QUAN HỆ THỐNG & NHIỆM VỤ ═══
Tài liệu này đóng vai trò đặc tả kỹ thuật và lộ trình nâng cấp dành cho lập trình viên (coder) và chuyên gia kiểm thử (tester) nhằm sửa chữa triệt để toàn bộ các lỗi kỹ thuật, lỗi bố cục, mã hóa font chữ và tính sư phạm trong phân hệ **Hiệu trưởng & Ban Giám Hiệu (BGH)** thuộc dự án **QA SmartClass**. 

Yêu cầu thực hiện nghiêm ngặt, tuân thủ đúng cấu trúc chương trình hiện hữu và bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**.

---

## ═══ PHẦN 1: CÁC CẢI TIẾN KỸ THUẬT & CHỨC NĂNG CHI TIẾT (TECHNICAL UPGRADES) ═══

### 1. Đồng bộ mã hóa và sửa lỗi hiển thị Tiếng Việt (Unicode & Encoding Compliance)
*   **Vấn đề:** Các tệp tin code-behind C# và giao diện XAML bị lỗi mã hóa diacritics (lưu sai bảng mã) dẫn đến các ký tự tiếng Việt có dấu bị biến thành dấu hỏi `?` hoặc ký tự rác. Biểu đồ KPI bị lỗi hiển thị hướng xu hướng tăng/giảm thành `?` hoặc `¦`.
*   **Yêu cầu kỹ thuật:** 
    1.  Chuyển đổi mã hóa toàn bộ tệp nguồn trong thư mục `Leadership/` sang định dạng **UTF-8 với chữ ký BOM (Byte Order Mark)**.
    2.  Chỉnh sửa các chuỗi bị lỗi trong code.
*   **Mô tả Input/Output từng tệp tin cụ thể:**

#### 📑 Tệp 1: [ApprovalQueueViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/ViewModels/ApprovalQueueViewModel.cs)
*   *Dữ liệu đầu vào (Input):* File mã nguồn cũ bị lỗi ký tự.
*   *Phương pháp thực hiện:* Thay thế các dòng thông điệp hiển thị trạng thái bị lỗi chính tả.
*   *Kết quả đầu ra (Output):* Các chuỗi tiếng Việt chuẩn:
    *   Dòng 70: `StatusMessage = "Đã phê duyệt bài giảng.";`
    *   Dòng 86: `StatusMessage = "Vui lòng nhập lý do từ chối vào ô Ghi chú.";`
    *   Dòng 97: `lessonToUpdate.Status = "Draft"; // Trở về cho GV sửa`
    *   Dòng 105: `StatusMessage = "Đã trả bài giảng về nháp, yêu cầu GV sửa lỗi.";`

#### 📑 Tệp 2: [KpiDashboardPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/KpiDashboardPage.xaml.cs)
*   *Dữ liệu đầu vào (Input):* Dữ liệu xu hướng từ `ComparativeAnalyticsService`.
*   *Phương pháp thực hiện:* Thay thế các switch-case lỗi hiển thị ký tự xu hướng tăng/giảm thành các ký hiệu unicode chuẩn.
*   *Kết quả đầu ra (Output):* 
    *   Dòng 83: `TxtTrendScore.Text = acadComp.ScoreTrend switch { "Up" => "▲", "Down" => "▼", _ => "―" };`
    *   Dòng 103: `TxtTrendAttendance.Text = attComp.RateTrend switch { "Up" => "▲", "Down" => "▼", _ => "―" };`
    *   Dòng 125: `TxtTrendConduct.Text = emComp.ConductTrend switch { "Up" => "▲", "Down" => "▼", _ => "―" };`

---

### 2. Sửa lỗi tính tỷ lệ Điểm danh (0.0% Attendance Rate Bug)
*   **Vấn đề:** Dashboard của Hiệu trưởng hiển thị tỷ lệ điểm danh hôm nay luôn là `0.0%` hoặc `N/A` mặc dù học sinh đã điểm danh đầy đủ. Nguyên nhân do code so khớp cứng chuỗi `"present"` viết thường trong khi CSDL lưu trữ `"Present"` hoặc `"Có mặt"`.
*   **Yêu cầu kỹ thuật:** Sửa logic truy vấn đếm số học sinh có mặt trong ngày.
*   **Tệp tin sửa đổi:** [LeadershipDashboardViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/ViewModels/LeadershipDashboardViewModel.cs)
    *   *Input:* `db.AttendanceRecords` truy vấn trạng thái hôm nay.
    *   *Phương pháp thực hiện:* Sửa biểu thức Lambda trong `CountAsync`.
    *   *Dòng code sửa đổi (Dòng 62):*
        ```csharp
        // Trước:
        var presentCount = await db.AttendanceRecords.CountAsync(a => a.Date == today && a.Status == "present");
        // Sau:
        var presentCount = await db.AttendanceRecords.CountAsync(a => a.Date == today && (a.Status == "Present" || a.Status == "Có mặt"));
        ```
    *   *Output:* Tỷ lệ chuyên cần trên Dashboard chính của BGH hiển thị chính xác (ví dụ: `96.8%`).

---

### 3. Khắc phục lỗi Lưu cài đặt hệ thống (Settings View Controls Mapping)
*   **Vấn đề:** Cấu hình hệ thống không lưu được IP máy chủ Server và trạng thái tự động sao lưu. Nguyên nhân là do code-behind gán sai tên TextBox (gọi `FindName("txtIPAddress")` trong khi XAML đặt tên là `txtServerIp`) và thiếu CheckBox `chkAutoBackup` trên file giao diện.
*   **Yêu cầu kỹ thuật:** 
    1.  Đồng bộ hóa tên các điều khiển (controls) giữa tệp XAML và tệp C# code-behind.
    2.  Bổ sung giao diện CheckBox cho thiết lập Tự động sao lưu.
    3.  Bổ sung logic lưu Năm học (`txtAcademicYear`) và Mã định danh (`txtSchoolId`) vào DB.
*   **Mô tả Input/Output từng tệp tin cụ thể:**

#### 📑 Tệp 1: [SystemSettingsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/SystemSettingsView.xaml)
*   *Phương pháp thực hiện:*
    *   Tìm thẻ TextBox nhập IP Address ở dòng 77, đổi `x:Name="txtServerIp"` thành `x:Name="txtIPAddress"`.
    *   Bổ sung CheckBox điều khiển tự động sao lưu ngay dưới tiêu đề "Quản lý Sao lưu Dữ liệu" ở dòng 25:
        ```xml
        <CheckBox x:Name="chkAutoBackup" Content="Tự động sao lưu cơ sở dữ liệu hàng ngày" FontSize="13" Margin="0,10,0,15" FontWeight="SemiBold"/>
        ```
*   *Output:* Giao diện đồng bộ, hiển thị thêm hộp kiểm cấu hình Backup tự động.

#### 📑 Tệp 2: [SystemSettingsView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/SystemSettingsView.xaml.cs)
*   *Phương pháp thực hiện:*
    *   Bổ sung nạp và lưu trữ `AcademicYear` và `SchoolId` vào phương thức `LoadSettingsFromDb` và `BtnSaveSettings_Click`.
    *   Bổ sung log lỗi vào khối `catch (Exception ex)` thay vì để trống.
*   *Dòng code sửa đổi:*
    *   *Dòng 47:* `catch (Exception ex) { Log.Error(ex, "Lỗi tải cấu hình ban đầu từ DB"); }`
    *   *Bổ sung lưu (Dòng 65):*
        ```csharp
        SaveSetting("AcademicYear", (FindName("txtAcademicYear") as TextBox)?.Text ?? "", "General");
        SaveSetting("SchoolId", (FindName("txtSchoolId") as TextBox)?.Text ?? "", "General");
        ```
*   *Output:* Dữ liệu cấu hình IP, Port, Auto Backup, Năm học, Mã trường học được ghi và tải lên chính xác từ bảng CSDL SQLite `SystemSettings`.

---

### 4. Sửa lỗi kết xuất Đồ thị OxyPlot (Mismatched Axes Chart Bug)
*   **Vấn đề:** Đồ thị so sánh học kỳ trong `KpiDashboardPage.xaml` bị lỗi hiển thị rỗng hoặc treo do sử dụng `BarSeries` (thanh ngang) kết hợp CategoryAxis nằm ở vị trí Bottom (dọc chuyển ngang lỗi).
*   **Yêu cầu kỹ thuật:** Đổi toàn bộ loạt đồ thị sang `ColumnSeries` (thanh đứng) để khớp với trục danh mục Bottom.
*   **Tệp tin sửa đổi:** [KpiDashboardPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/KpiDashboardPage.xaml.cs)
    *   *Phương pháp thực hiện:* Thay thế các khai báo `BarSeries` và `BarItem` thành `ColumnSeries` và `ColumnItem`.
    *   *Dòng code sửa đổi:*
        *   Dòng 147-148:
            ```csharp
            var seriesHk1 = new ColumnSeries { Title = "Học kỳ 1", FillColor = OxyColor.FromRgb(59, 130, 246), XAxisKey = "CategoryAxis", YAxisKey = "ValueAxis" };
            var seriesHk2 = new ColumnSeries { Title = "Học kỳ 2", FillColor = OxyColor.FromRgb(16, 185, 129), XAxisKey = "CategoryAxis", YAxisKey = "ValueAxis" };
            ```
        *   Dòng 155-156:
            ```csharp
            seriesHk1.Items.Add(new ColumnItem(val1));
            seriesHk2.Items.Add(new ColumnItem(val2));
            ```
        *   Dòng 178-179:
            ```csharp
            var kpiSeriesHk1 = new ColumnSeries { Title = "Học kỳ 1", FillColor = OxyColor.FromRgb(99, 102, 241), XAxisKey = "KpiCategoryAxis", YAxisKey = "KpiValueAxis" };
            var kpiSeriesHk2 = new ColumnSeries { Title = "Học kỳ 2", FillColor = OxyColor.FromRgb(245, 158, 11), XAxisKey = "KpiCategoryAxis", YAxisKey = "KpiValueAxis" };
            ```
        *   Dòng 181-185:
            ```csharp
            kpiSeriesHk1.Items.Add(new ColumnItem(attComp.AttendanceRate1));
            kpiSeriesHk1.Items.Add(new ColumnItem(acadComp.PassRate1));
            kpiSeriesHk2.Items.Add(new ColumnItem(attComp.AttendanceRate2));
            kpiSeriesHk2.Items.Add(new ColumnItem(attComp.AttendanceRate2));
            ```
    *   *Output:* Biểu đồ cột đứng so sánh học lực và chuyên cần hiển thị chính xác, phân bổ đều theo các trục, không còn lỗi.

---

### 5. Tích hợp Biểu đồ Động thay cho Placeholder
*   **Vấn đề:** Biểu đồ hoạt động của App phụ huynh và tỷ lệ hệ điều hành trong `AppUsageAnalyticsView.xaml` chỉ là các TextBlock dòng chữ tĩnh, vi phạm quy chuẩn hiển thị dữ liệu thực v4.1.
*   **Yêu cầu kỹ thuật:** Sử dụng OxyPlot để dựng biểu đồ Line Chart và Pie Chart động nạp dữ liệu thật.
*   **Tệp tin sửa đổi:**
    1.  [AppUsageAnalyticsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/AppUsageAnalyticsView.xaml):
        *   Thêm namespace OxyPlot: `xmlns:oxy="http://oxyplot.org/wpf"`
        *   Thay thế TextBlock dòng 125 bằng: `<oxy:PlotView x:Name="ChartAppAccess" Background="Transparent" Height="230"/>`
        *   Thay thế TextBlock dòng 143 bằng: `<oxy:PlotView x:Name="ChartDeviceOS" Background="Transparent" Height="230"/>`
    2.  [AppUsageAnalyticsView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/AppUsageAnalyticsView.xaml.cs):
        *   *Input:* Đọc lịch sử đăng nhập/sử dụng app từ `db.EventLogs` (EventType = "Login" hoặc "AppStart").
        *   *Phương pháp thực hiện:* Gom nhóm log theo ngày (7 ngày gần nhất) đưa vào `LineSeries`. Đếm loại thiết bị (Android vs iOS) đưa vào `PieSeries`.
        *   *Output:* Kết xuất đồ thị đường và biểu đồ quạt mượt mà ngay khi tải trang.

---

## ═══ PHẦN 2: CÁC NÂNG CẤP BỐ CỤC & TÍNH SƯ PHẠM (PEDAGOGICAL UPGRADES) ═══

### 1. Bổ sung ScrollViewer ngăn chặn cắt rách hình (Layout Resilience)
*   **Yêu cầu:** Màn hình máy chiếu trường học thường có độ phân giải thấp (1024x768). Để đảm bảo chữ không bị che khuất và nút bấm không bị cắt lề, **bắt buộc** phải bọc lưới Grid của các tệp XAML sau vào thẻ `<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto">`:
    *   `AppUsageAnalyticsView.xaml`
    *   `BackupRestoreView.xaml`
    *   `ClassObservationView.xaml`
    *   `EmulationBoardPage.xaml`
    *   `ProfessionalTopicView.xaml`
    *   `ReportExportPage.xaml`
    *   `RolePermissionManagerView.xaml`
    *   `SchoolCalendarView.xaml`
    *   `SchoolEventCalendarView.xaml`
    *   `StaffPerformanceTrackerView.xaml`
    *   `SystemAuditLogView.xaml`
    *   `SystemDataExporterView.xaml`
    *   `SystemSettingsView.xaml`

### 2. Việt hóa Tiêu đề Giao diện (Localization & Consistency)
*   **Yêu cầu:** Sửa đổi thuộc tính `Title="..."` của các Page trong XAML sang tiếng Việt hoàn chỉnh.
*   **Bảng ánh xạ cụ thể:**
    1.  `AppUsageAnalyticsView.xaml`: Title="Thống kê Tương tác Mobile App"
    2.  `BackupRestoreView.xaml`: Title="Sao lưu và Khôi phục CSDL"
    3.  `ClassObservationView.xaml`: Title="Dự giờ Đánh giá Giáo viên" (Sửa lỗi viết không dấu `Du gio`)
    4.  `EmulationBoardPage.xaml`: Title="Bảng Vàng Thi Đua Trường Học"
    5.  `KpiDashboardPage.xaml`: Title="Chỉ số KPI và So sánh Hiệu năng"
    6.  `LeadershipDashboardWindow.xaml`: Title="QA SmartSchool - Bảng điều khiển Ban Giám Hiệu"
    7.  `PrincipalDashboardPage.xaml`: Title="Tổng quan Số liệu Nhà trường"
    8.  `ReportExportPage.xaml`: Title="Xuất Báo cáo Kết quả Học tập"
    9.  `SchoolEventCalendarView.xaml`: Title="Lịch Sự kiện & Lễ hội"
    10. `StaffPerformanceTrackerView.xaml`: Title="Theo dõi Hiệu suất Giảng dạy"
    11. `SystemDataExporterView.xaml`: Title="Trích xuất Dữ liệu Định dạng Excel"
    12. `SystemSettingsView.xaml`: Title="Cấu hình Hệ thống & Cài đặt chung"

### 3. Bổ sung Chỉ dẫn Sử dụng (User Guides & Safety Measures)
*   **BackupRestoreView.xaml:**
    *   Thêm một Border hướng dẫn từng bước nhỏ gọn, màu nền vàng nhạt `#FEF9C3` ở đầu vùng khôi phục:
        *   "⚠️ **HƯỚNG DẪN KHÔI PHỤC AN TOÀN:** Bước 1: Chọn bản sao lưu trong danh sách | Bước 2: Bấm nút 'Khôi Phục Dữ Liệu' | Bước 3: Nhập mật khẩu quản trị viên để xác thực."
*   **ClassObservationView.xaml:**
    *   Bổ sung Tooltip cho các tiêu chuẩn rubric tiết dạy để giải nghĩa các cột KHBD (Kế hoạch bài dạy), TCH (Tổ chức hoạt động), HHS (Hoạt động học sinh), HTGV (Hỗ trợ giáo viên), KTG (Kiểm tra đánh giá) theo mẫu Công văn 5555.
*   **ApprovalQueueView.xaml:**
    *   Thêm TextBlock ghi chú ở vùng Ghi chú phê duyệt: "💡 *Hiệu trưởng lưu ý: Khi Từ chối bài giảng, bắt buộc phải ghi rõ lý do chuyên môn cần sửa đổi vào ô Ghi chú dưới đây.*"

---

## ═══ BIỆN PHÁP PHẢN BIỆN & CHỐNG SAI SÓT (DEFENSIVE PROGRAMMING) ═══

1.  **Chống rỗng dữ liệu khi lưu cấu hình:** Trong `SystemSettingsView.xaml.cs`, bổ sung khối kiểm định giá trị nhập vào cho TextBox IP và Port. Nếu IP trống hoặc Port không nằm trong dải `1024 - 65535`, hiển thị cảnh báo lỗi và hủy lưu.
2.  **Mật khẩu bảo vệ khi Restore:** Để tránh Hiệu trưởng bấm nhầm nút khôi phục làm xóa dữ liệu, tại sự kiện click `BtnRestore_Click`, trước khi gọi `backupService.Restore`, lập trình viên phải thêm một cửa sổ nhập mật khẩu Admin (`StaffSession.CurrentUser?.Role == "Admin"` hoặc nhập cứng mật khẩu an toàn dự phòng của trường).
3.  **Validation điểm số dự giờ:** Giới hạn điểm số nhập vào của từng tiêu chí dự giờ từ 1 đến 4 điểm. Nếu nhập ngoài dải điểm hoặc ký tự chữ, hiển thị thông báo lỗi lập tức.

---

## ═══ BỘ CHECKSHEET KIỂM THỬ KHẮC PHỤC (TEST CHECKSHEET FOR QA) ═══

### 📋 Checksheet 1: Kiểm thử chức năng (Functionality Test)
*   [ ] **Đo lường Điểm danh:** Kiểm tra Dashboard xem chuyên cần hôm nay hiển thị đúng số thực (Ví dụ: `98.0%`) sau khi đã điểm danh 1 vài học sinh trong DB, không hiện `0.0%`.
*   [ ] **Lưu cài đặt:** Đổi IP máy chủ thành `192.168.1.50`, bấm Lưu cấu hình -> Khởi động lại trang -> Kiểm tra IP hiển thị có đúng là `192.168.1.50` và không báo lỗi null.
*   [ ] **Đồ thị OxyPlot:** Mở KpiDashboardPage, xác nhận hai đồ thị phân phối học lực và chuyên cần hiển thị các cột đứng song song mượt mà, không bị biến dạng hay treo ứng dụng.
*   [ ] **Tự động dọn dẹp:** Thực thi thử dọn dẹp dữ liệu từ nút bấm ở cài đặt hệ thống, kiểm tra log xem có dòng cảnh báo hoặc thông tin I/O nào được ghi lại.

### 📋 Checksheet 2: Kiểm thử Giao diện & Sư phạm (UI/UX Test)
*   [ ] **Độ bao phủ Font:** Kiểm tra toàn bộ 5 tệp tin ViewModel và View đã sửa mã hóa, đảm bảo hiển thị đúng dấu tiếng Việt (không chứa ký tự lạ hay dấu hỏi `?`).
*   [ ] **Ký hiệu xu hướng:** Xác nhận chỉ báo biến động hiển thị đúng `▲`, `▼`, `―` thay vì dấu `?`.
*   [ ] **Tính cuộn trang:** Co giãn cửa sổ ứng dụng BGH về kích thước cực tiểu, kiểm tra xem có cuộn xuống được các phần tử bên dưới cùng không.
*   [ ] **Đồ thị Live Analytics:** Mở trang thống kê AppUsageAnalytics, xác nhận hiển thị đồ thị đường và biểu đồ quạt thực tế của OxyPlot thay vì các chuỗi placeholder chữ.
*   [ ] **Việt hóa Title:** Mở từng trang trong phân hệ Leadership, kiểm tra xem thanh tiêu đề của Page có hiển thị đúng chuỗi tiếng Việt đã định nghĩa không.
*   [ ] **Thông báo hướng dẫn:** Xác minh các khối hướng dẫn sử dụng và Tooltip chú thích xuất hiện rõ ràng, dễ đọc trên giao diện.

---

*Tài liệu đã được Hội đồng Chuyên gia thông qua, bàn giao cho đội ngũ phát triển.*
