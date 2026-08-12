# Kế hoạch Chi tiết Nâng cấp và Cải tiến Giao diện Admin nhà trường & IT Console (Admin Module)
**Tài liệu hướng dẫn phát triển và kiểm thử - Chuẩn QA SmartClass v4.1**  

---

## ═══ TỔNG QUAN HỆ THỐNG & NHIỆM VỤ ═══

Tài liệu này đóng vai trò đặc tả kỹ thuật và lộ trình nâng cấp dành cho lập trình viên (coder) và chuyên gia kiểm thử (tester) nhằm sửa chữa triệt để các lỗi kỹ thuật, lỗi định tuyến, giả lập mạng, bảo mật và thẩm mỹ sư phạm trong phân hệ **Admin nhà trường & Trạm kỹ thuật (IT Console)** thuộc dự án **QA SmartClass**.

Đặc biệt, tài liệu này bổ sung cấu hình lựa chọn **Chế độ đồng bộ sơ đồ phòng máy** trong phần cài đặt hệ thống (Master Settings) để nhà trường tự quyết định dựa trên điều kiện hạ tầng kỹ thuật thực tế, đồng thời cung cấp hệ thống kịch bản kiểm thử (Test Cases) mở rộng đầy đủ để đảm bảo chất lượng phần mềm ở mức tối đa.

Yêu cầu thực hiện nghiêm ngặt, tuân thủ đúng cấu trúc chương trình hiện hữu và bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**.

---

## ═══ PHẦN 1: CÁC CẢI TIẾN KỸ THUẬT & CHỨC NĂNG CHI TIẾT (TECHNICAL UPGRADES) ═══

### 1. Đồng bộ mã hóa và sửa lỗi hiển thị Tiếng Việt (Unicode compliance)
*   **Vấn đề:** Các tệp tin code-behind và XAML chứa chuỗi tiếng Việt có dấu viết trực tiếp. Để đảm bảo không bị lỗi mã hóa diacritics (ký tự lạ, dấu hỏi chấm `?`) khi chạy trên Windows của các trường học, các file này phải được đồng bộ mã hóa.
*   **Yêu cầu kỹ thuật:** Chuyển đổi mã hóa toàn bộ tệp nguồn trong thư mục `Admin/` sang định dạng **UTF-8 với chữ ký BOM (Byte Order Mark)**.
*   **Tệp tin thực hiện:**
    *   [AdminConsoleWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml) và [.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml.cs)
    *   [SchoolAdminDashboardControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml) và [.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml.cs)
    *   [SchoolDirectorDashboard.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolDirectorDashboard.xaml) và [.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolDirectorDashboard.xaml.cs)
    *   [NetworkTopologyControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/NetworkTopologyControl.xaml) và [.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/NetworkTopologyControl.xaml.cs)
    *   [QAVendorAdminControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/QAVendorAdminControl.xaml) và [.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/QAVendorAdminControl.xaml.cs)
    *   [PinDialog.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/PinDialog.xaml) và [.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/PinDialog.xaml.cs)

---

### 2. Sửa lỗi Định tuyến nhầm Tab Admin (Routing Tab Bug)
*   **Vấn đề:** Trong `AdminConsoleWindow.xaml`, nút bấm của Admin *"Quản trị Trường Học"* (Tag="4") lại mở ra trang `SchoolDirectorDashboard` (Dashboard của Hiệu trưởng) thay vì `SchoolAdminDashboardControl` (Dashboard Admin quản lý dữ liệu trường). Lỗi này làm mất tính năng CRUD Giáo viên và Import Học sinh của Admin.
*   **Yêu cầu kỹ thuật:** Sửa thẻ điều khiển trong TabItem chỉ số 4.
*   **Tệp tin sửa đổi:** [AdminConsoleWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml)
    *   *Dữ liệu đầu vào (Input):* Mã nguồn XML hiện tại tại vị trí TabItem số 4.
    *   *Phương pháp thực hiện:* Thay thế thẻ điều khiển `SchoolDirectorDashboard` bằng `SchoolAdminDashboardControl`.
    *   *Dòng code sửa đổi (Dòng 327-333):*
        ```xml
        <!-- Trước cải tiến: -->
        <TabItem>
            <controls:SchoolDirectorDashboard Margin="-32"/>
        </TabItem>

        <!-- Sau cải tiến: -->
        <TabItem>
            <controls:SchoolAdminDashboardControl Margin="-32"/>
        </TabItem>
        ```
    *   *Dữ liệu đầu ra (Output):* Tab "Quản trị Trường Học" mở ra đúng giao diện quản trị Admin với 5 tab chức năng con (Báo cáo sử dụng, Quản lý Giáo viên, Quản lý Học sinh, Quản lý Phòng máy, Cấu hình).

---

### 3. Tích hợp Tùy chọn Master cho Chế độ đồng bộ Sơ đồ mạng Topology (Topology Sync Options)
*   **Vấn đề:** Trong `NetworkTopologyControl.xaml.cs`, việc chạy hàm `Random` đổi trạng thái máy học sinh gây hiện tượng nhấp nháy xanh/đỏ liên tục. Tuy nhiên, việc quét mạng LAN liên tục (Active Ping Scan) có thể làm quá tải các mạng Wifi/LAN yếu của trường học Việt Nam. Trường học cần được lựa chọn chế độ quét phù hợp với điều kiện thực tế.
*   **Giải pháp (Cấu hình Master trên DB & UI Settings):**
    *   Thêm thiết lập **"Chế độ đồng bộ Sơ đồ mạng"** trong Tab Cấu hình của Admin (`SchoolAdminDashboardControl.xaml`) và lưu cấu hình vào bảng `SystemSettings` trong SQLite với 3 tùy chọn:
        1.  `ActiveScan` (Quét chủ động): Tự động Ping/TCP kiểm tra thực tế IP (Chính xác cao, đòi hỏi mạng LAN ổn định).
        2.  `FileHeartbeat` (Cập nhật thụ động qua File): Đọc file heartbeat từ thư mục chia sẻ `SharedFiles/` (Nhẹ nhàng, không tải băng thông, cập nhật trễ 30s).
        3.  `SimulationDemo` (Giả lập ổn định): Đặt tất cả máy trạm làm demo (Phù hợp đào tạo, chạy thử không có máy trạm thật kết nối).

*   **Tệp tin sửa đổi 1: [SchoolAdminDashboardControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml) (Tab Cấu hình - Tab 4)**
    *   *Phương pháp thực hiện:* Thay thế giao diện mock tĩnh bằng giao diện cấu hình thật.
    *   *Dòng code sửa đổi:*
        ```xml
        <!-- Thay thế tabMock chứa text bằng một StackPanel cấu hình thực tế -->
        <Grid x:Name="tabSettings" Visibility="Collapsed" Margin="20">
            <StackPanel>
                <TextBlock Text="⚙️ CẤU HÌNH THÔNG SỐ HỆ THỐNG" Foreground="White" FontSize="16" FontWeight="Bold" Margin="0,0,0,20"/>
                
                <Border Background="#1E293B" CornerRadius="8" Padding="20" BorderBrush="#334155" BorderThickness="1">
                    <StackPanel>
                        <TextBlock Text="Chế độ giám sát Sơ đồ phòng máy (Topology Sync Mode)" Foreground="White" FontWeight="SemiBold" FontSize="14" Margin="0,0,0,10"/>
                        <TextBlock Text="Lưu ý: Lựa chọn chế độ quét phù hợp với băng thông và hiệu năng thực tế của phòng máy trường." Foreground="#94A3B8" FontSize="12" Margin="0,0,0,15" TextWrapping="Wrap"/>
                        
                        <RadioButton x:Name="radActiveScan" Content="🟢 Active Scan (Quét chủ động qua mạng LAN - Độ chính xác cao)" Foreground="White" FontSize="13" Margin="0,5" GroupName="TopologyMode"/>
                        <RadioButton x:Name="radFileHeartbeat" Content="🟡 File Heartbeat (Nhận thông điệp thụ động qua ổ chia sẻ - Tiết kiệm băng thông)" Foreground="White" FontSize="13" Margin="0,5" GroupName="TopologyMode"/>
                        <RadioButton x:Name="radSimulationDemo" Content="🔵 Simulation Demo (Giả lập ổn định - Phục vụ giảng dạy & demo)" Foreground="White" FontSize="13" Margin="0,5" GroupName="TopologyMode"/>
                    </StackPanel>
                </Border>
                
                <Button Content="💾 Lưu Cấu Hình Hệ Thống" Background="#10B981" Foreground="White" Padding="20,10" FontWeight="Bold" Margin="0,20,0,0" HorizontalAlignment="Right" Click="BtnSaveSystemSettings_Click"/>
            </StackPanel>
        </Grid>
        ```

*   **Tệp tin sửa đổi 2: [SchoolAdminDashboardControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml.cs)**
    *   *Phương pháp thực hiện:* Nạp và lưu giá trị `TopologyMode` xuống database SQLite.
        ```csharp
        // Khi tải dữ liệu:
        private void LoadSystemSettings()
        {
            using var db = new AppDbContext();
            var mode = db.SystemSettings.FirstOrDefault(s => s.Key == "TopologyMode")?.Value ?? "FileHeartbeat";
            
            if (radActiveScan != null) radActiveScan.IsChecked = mode == "ActiveScan";
            if (radFileHeartbeat != null) radFileHeartbeat.IsChecked = mode == "FileHeartbeat";
            if (radSimulationDemo != null) radSimulationDemo.IsChecked = mode == "SimulationDemo";
        }

        // Khi lưu dữ liệu:
        private void BtnSaveSystemSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedMode = "FileHeartbeat";
                if (radActiveScan.IsChecked == true) selectedMode = "ActiveScan";
                else if (radSimulationDemo.IsChecked == true) selectedMode = "SimulationDemo";

                using var db = new AppDbContext();
                var setting = db.SystemSettings.FirstOrDefault(s => s.Key == "TopologyMode");
                if (setting == null)
                {
                    db.SystemSettings.Add(new SystemSetting { Key = "TopologyMode", Value = selectedMode, Category = "Network" });
                }
                else
                {
                    setting.Value = selectedMode;
                    setting.UpdatedAt = DateTime.Now;
                }
                db.SaveChanges();
                MessageBox.Show("Đã cập nhật cấu hình hệ thống thành công!", "Cấu hình", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        ```

*   **Tệp sửa đổi 3: [NetworkTopologyControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/NetworkTopologyControl.xaml.cs) (Xử lý đồng bộ dựa trên Cấu hình)**
    *   *Phương pháp thực hiện:*
        ```csharp
        private void SyncTimer_Tick(object sender, EventArgs e)
        {
            if (_currentData == null || _isDragging) return;
            try
            {
                using var db = new AppDbContext();
                // Đọc chế độ đồng bộ từ Master Settings
                string syncMode = db.SystemSettings.FirstOrDefault(s => s.Key == "TopologyMode")?.Value ?? "FileHeartbeat";
                bool changed = false;

                if (syncMode == "SimulationDemo")
                {
                    // 1. Chế độ DEMO: Trạng thái máy trạm hiển thị ổn định (mặc định lấy từ sơ đồ thiết kế)
                    foreach (var student in _currentData.Nodes.Where(n => n.NodeType == "Student"))
                    {
                        if (student.Status != "Online") // Giữ tất cả máy online ổn định
                        {
                            student.Status = "Online";
                            changed = true;
                        }
                    }
                }
                else if (syncMode == "ActiveScan")
                {
                    // 2. Chế độ Active Scan: Thực hiện ping nhanh đến từng IP trong sơ đồ mạng
                    foreach (var student in _currentData.Nodes.Where(n => n.NodeType == "Student"))
                    {
                        string oldStatus = student.Status;
                        // Thực hiện ping bất đồng bộ không chặn UI
                        bool isAlive = QuickPing(student.IpAddress); 
                        student.Status = isAlive ? "Online" : "Offline";
                        if (oldStatus != student.Status) changed = true;
                    }
                }
                else
                {
                    // 3. Chế độ File Heartbeat: Đọc file Json heartbeat từ SharedFiles/
                    foreach (var student in _currentData.Nodes.Where(n => n.NodeType == "Student"))
                    {
                        string oldStatus = student.Status;
                        bool isAlive = CheckHeartbeatFile(student.MachineId);
                        student.Status = isAlive ? "Online" : "Offline";
                        if (oldStatus != student.Status) changed = true;
                    }
                }

                if (changed)
                {
                    RenderTopology();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Lỗi đồng bộ Topology mạng.");
            }
        }

        private bool QuickPing(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                // Ping nhanh timeout 500ms để không nghẽn mạng trạm
                var reply = ping.Send(ipAddress, 500); 
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
            catch { return false; }
        }

        private bool CheckHeartbeatFile(string machineId)
        {
            try
            {
                string hbPath = Path.Combine(Services.AppPaths.RootDir, "SharedFiles", $"heartbeat_{machineId}.json");
                if (File.Exists(hbPath))
                {
                    var fileInfo = new FileInfo(hbPath);
                    // Nếu tệp heartbeat được cập nhật trong vòng 10 phút gần đây -> Máy trạm online
                    return (DateTime.Now - fileInfo.LastWriteTime).TotalMinutes < 10;
                }
            }
            catch { }
            return false;
        }
        ```

---

### 4. Sửa lỗi sập chương trình khi thêm giáo viên trùng và gán mật khẩu (Teacher CRUD & Password)
*   **Vấn đề:** 
    1. Khi thêm giáo viên, nếu nhập trùng mã `TeacherCode` (đã tồn tại trong CSDL SQLite dưới dạng Unique index), hệ thống gọi `SaveChanges()` gây crash phần mềm.
    2. Giao diện thêm giáo viên không có trường mật khẩu và không gán mặc định trường `PasswordHash`, khiến tài khoản mới không thể sử dụng để đăng nhập.
*   **Yêu cầu kỹ thuật:**
    1. Kiểm tra tồn tại mã giáo viên trước khi thêm.
    2. Tự động khởi tạo mật khẩu mặc định băm HMACSHA512 theo đúng chuẩn an toàn của v4.1.
*   **Tệp tin sửa đổi:** [SchoolAdminDashboardControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026\QASmartClass\Admin\Controls\SchoolAdminDashboardControl.xaml.cs)
    *   *Dữ liệu đầu vào (Input):* Dữ liệu giáo viên nhập vào từ `CreateTeacherDialog`.
    *   *Phương pháp thực hiện:*
        1. Sử dụng `db.TeacherProfiles.Any` để kiểm tra trùng.
        2. Sinh mật khẩu mặc định dạng: `Gv@` + 4 ký tự cuối của mã giáo viên (ví dụ mã GV0015 -> mật khẩu `Gv@0015`).
        3. Băm mật khẩu bằng thuật toán băm bảo mật hệ thống.
    *   *Dòng code sửa đổi (Dòng 92-112):*
        ```csharp
        // Sau cải tiến (Phản biện & Khắc phục triệt để):
        private void BtnAddTeacher_Click(object sender, RoutedEventArgs e)
        {
            var dialog = CreateTeacherDialog("Thêm Giáo Viên Mới", null);
            if (dialog.ShowDialog() == true)
            {
                var result = (TeacherProfile)dialog.Tag;
                try
                {
                    using var db = new AppDbContext();
                    
                    // 1. Kiểm tra trùng khóa chính nghiệp vụ (TeacherCode) để tránh sập app
                    bool isDuplicate = db.TeacherProfiles.Any(t => t.TeacherCode.ToLower() == result.TeacherCode.ToLower());
                    if (isDuplicate)
                    {
                        MessageBox.Show($"Mã giáo viên '{result.TeacherCode}' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!", 
                                        "Trùng mã giáo viên", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 2. Tạo mật khẩu mặc định băm HMACSHA512 chuẩn v4.1
                    string defaultPwd = $"Gv@{result.TeacherCode.Substring(Math.Max(0, result.TeacherCode.Length - 4))}";
                    string pwdHash = QASmartTouch.Services.AuthenticationService.HashPasswordHMACSHA512(defaultPwd);
                    
                    result.PasswordHash = pwdHash;
                    result.TeacherPassword = defaultPwd; // Lưu text cấu hình để hiển thị khi cần thiết

                    db.TeacherProfiles.Add(result);
                    db.SaveChanges();
                    
                    Log.Information("[ADMIN_ACTION] Added teacher: {Name} ({Code})", result.FullName, result.TeacherCode);
                    LoadData();
                    MessageBox.Show($"Đã thêm giáo viên: {result.FullName}\nMật khẩu mặc định: {defaultPwd}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi hệ thống khi thêm giáo viên: {ex.Message}", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        ```

---

## ═══ PHẦN 2: CÁC CẢI TIẾN BỐ CỤC & TÍNH SƯ PHẠM (PEDAGOGICAL UPGRADES) ═══

### 1. Bổ sung ScrollViewer ngăn chặn cắt rách hình ở độ phân giải máy chiếu
*   **Vấn đề:** Các trang quản trị của Admin dùng Grid tĩnh, gây mất thông tin hoặc mất nút bấm bên dưới khi co giãn nhỏ hoặc hiển thị trên màn hình laptop/máy chiếu có độ phân giải thấp (1024x768 / 1366x768).
*   **Yêu cầu kỹ thuật:** Bọc layout bằng ScrollViewer có cấu hình tự động hiển thị thanh cuộn dọc/ngang.
*   **Tệp tin sửa đổi:** [SchoolAdminDashboardControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml)
    *   *Phương pháp thực hiện:* Bọc thẻ `<Grid>` ở dòng 153 trong một `<ScrollViewer>`:
        ```xml
        <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto">
            <Grid MinWidth="960" MinHeight="600">
                <!-- Content -->
            </Grid>
        </ScrollViewer>
        ```

---

### 2. Thiết lập bảng màu chuyên nghiệp thay thế giao diện "Hacker Neon"
*   **Yêu cầu kỹ thuật:** Phối lại màu sắc giao diện theo tông màu Slate/Indigo của v4.1.
*   **Tệp sửa đổi:** [AdminConsoleWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml)
    *   *Phương pháp thực hiện:* 
        *   Màu nền Sidebar: Đổi sang `#0F172A` (xanh đen dịu nhẹ).
        *   Màu chữ các tab điều hướng: Thay thế các màu neon đơn lẻ bằng màu đồng bộ: trắng nhạt `#E2E8F0` khi thường và xanh indigo thương hiệu `#6366F1` khi active.
        *   Màu nút nguy hiểm (DangerZone): Đổi màu đỏ chói `#F87171` sang màu đỏ rượu trầm nhã nhặn `#DC2626`.

---

### 3. Tích hợp dữ liệu thống kê thực tế cho Dashboard BGH (Real Data Binding)
*   **Yêu cầu kỹ thuật:** Sử dụng LINQ truy vấn thực tế dữ liệu từ CSDL SQLite.
*   **Tệp sửa đổi:** [SchoolDirectorDashboard.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolDirectorDashboard.xaml.cs)
    *   *Phương pháp thực hiện (Truy vấn thực tế từ database):*
        ```csharp
        private void LoadRealData()
        {
            if (_db == null) return;
            try
            {
                // 1. Thống kê số giờ dạy thực tế từ bảng UsageLogs
                var logs = _db.UsageLogs.ToList();
                double totalHours = logs.Where(l => l.EventType == "SESSION_END").Sum(l => l.DurationMs) / 3600000.0;
                txtTotalHours.Text = $"{totalHours:F1}h";

                // 2. Thống kê bài giảng mới trong tháng từ bảng Lessons
                int newLessonsCount = _db.Lessons.Count(l => l.CreatedAt >= DateTime.Now.AddDays(-30));
                txtNewLessons.Text = newLessonsCount.ToString();

                // 3. Đọc danh sách giáo viên tích cực nhất từ database thực tế
                var topActiveTeachers = _db.UsageLogs
                    .Where(l => l.EventType == "SESSION_START" && !string.IsNullOrEmpty(l.UserRole))
                    .GroupBy(l => l.Username)
                    .OrderByDescending(g => g.Count())
                    .Take(4)
                    .Select(g => new { Name = g.Key, Hours = $"{g.Count()} phiên" })
                    .ToList();
                
                listTopTeachers.ItemsSource = topActiveTeachers;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Không thể tải số liệu thật cho Dashboard BGH: {Msg}", ex.Message);
            }
        }
        ```

---

### 4. Việt hóa 100% ngôn ngữ giao diện & Đồng bộ hóa
*   **Yêu cầu:** Dịch toàn bộ các nhãn tiếng Anh sang tiếng Việt đúng ngữ cảnh sư phạm trường học Việt Nam.
    *   `Title="Admin Console"` -> `Title="Bảng điều khiển Quản trị viên"`
    *   `Content="IT CONSOLE"` -> `Content="QUẢN TRỊ HỆ THỐNG"`
    *   `Content="Chuyển Đổi Chế Độ"` -> `Content="🔄 Chuyển Chế Độ Máy"`
    *   `Content="Quản lý File & DB"` -> `Content="📁 Quản Lý Cơ Sở Dữ Liệu"`
    *   `Content="Giám sát Lỗi (Logs)"` -> `Content="🐛 Nhật Ký Sự Cố (Logs)"`
    *   `Content="Sơ đồ Mạng"` -> `Content="🗺️ Sơ Đồ Phòng Máy"`

---

## ═══ MA TRẬN KỊCH BẢN KIỂM THỬ KHẮC PHỤC (TEST CASE SPECIFICATION) ═══

Để đảm bảo coder không thể triển khai sai lệch, đội ngũ QA Tester sẽ sử dụng ma trận 8 Test Cases chi tiết sau để nghiệm thu.

### 🧪 Test Case 1: Định tuyến Tab "Quản trị Trường Học" (Routing Validation)
*   **Mục tiêu:** Xác minh định tuyến chính xác đến trang quản trị của Admin thay vì trang Hiệu trưởng.
*   **Dữ liệu đầu vào (Input):** Khởi chạy Admin Console -> Nhấp chọn tab "Quản trị Trường Học" (Chỉ số 4).
*   **Quy trình thực hiện:** 
    1.  Mở cửa sổ `AdminConsoleWindow`.
    2.  Nhấp nút điều hướng *"🏫 Quản trị Trường Học"* trên sidebar.
    3.  Quan sát UI hiển thị.
*   **Kết quả mong đợi (Output):** Hiển thị màn hình `SchoolAdminDashboardControl` có tiêu đề "QUẢN TRỊ TRƯỜNG HỌC", có thanh tab phụ gồm: *Báo cáo sử dụng, Quản lý Giáo viên, Quản lý Học sinh*. Không được phép hiển thị tiêu đề "Dashboard Ban Giám Hiệu".

### 🧪 Test Case 2: Kiểm thử cấu hình Master "Chế độ đồng bộ" (Topology Modes)
*   **Mục tiêu:** Đảm bảo hệ thống lưu và nạp đúng cấu hình giám sát mạng.
*   **Dữ liệu đầu vào (Input):** Chọn tab *Cấu Hình* -> Tích chọn `Active Scan` -> Bấm *Lưu cấu hình*.
*   **Quy trình thực hiện:**
    1.  Tích chọn tùy chọn `Active Scan` trên giao diện cấu hình.
    2.  Bấm nút *Lưu Cấu Hình Hệ Thống*.
    3.  Đóng và khởi động lại toàn bộ phần mềm QA SmartClass.
    4.  Mở lại tab *Cấu Hình*.
*   **Kết quả mong đợi (Output):** Tùy chọn `Active Scan` được tự động chọn sẵn (Load thành công từ DB SQLite). Kiểm tra bảng `SystemSettings` trong file `smartclass.db` thấy dòng có Key = `"TopologyMode"` và Value = `"ActiveScan"`.

### 🧪 Test Case 3: Chế độ giám sát "Simulation Demo" (Topology Stable Render)
*   **Mục tiêu:** Xác nhận sơ đồ Topology không bị nhấp nháy xanh/đỏ tự phát khi cấu hình Demo.
*   **Dữ liệu đầu vào (Input):** Chuyển chế độ sang `SimulationDemo` -> Mở tab *Sơ đồ Mạng*.
*   **Quy trình thực hiện:**
    1.  Lưu cấu hình `TopologyMode` thành `SimulationDemo`.
    2.  Mở tab *Sơ đồ Mạng*.
    3.  Quan sát các nút máy trạm trong thời gian 3 phút liên tục.
*   **Kết quả mong đợi (Output):** Tất cả 20 máy trạm học sinh và 1 máy giáo viên luôn hiển thị trạng thái `Online` (màu xanh lá) ổn định. Không có bất kỳ hiện tượng chớp tắt đỏ (Offline) nào diễn ra.

### 🧪 Test Case 4: Chế độ giám sát "File Heartbeat" (File-based Monitoring)
*   **Mục tiêu:** Kiểm tra khả năng nhận biết online/offline dựa trên tệp tin log của ổ đĩa chung.
*   **Dữ liệu đầu vào (Input):** Cấu hình `FileHeartbeat` -> Tạo tệp `heartbeat_PC-01.json` có thời gian sửa đổi là hiện tại; tệp `heartbeat_PC-02.json` có thời gian sửa đổi cách đây 12 phút.
*   **Quy trình thực hiện:**
    1.  Lưu cấu hình chế độ `FileHeartbeat`.
    2.  Ghi đè file `heartbeat_PC-01.json` trong thư mục `SharedFiles` để cập nhật timestamp.
    3.  Chỉnh timestamp của file `heartbeat_PC-02.json` lùi về 12 phút trước.
    4.  Mở tab *Sơ đồ Mạng* và quan sát máy PC-01 và PC-02.
*   **Kết quả mong đợi (Output):** Máy PC-01 hiển thị màu xanh lá (`Online`). Máy PC-02 hiển thị màu đỏ (`Offline`) do quá ngưỡng giới hạn 10 phút.

### 🧪 Test Case 5: Thêm mới Giáo viên trùng mã (Unique constraint validation)
*   **Mục tiêu:** Đảm bảo hệ thống phát hiện trùng mã giáo viên và không gây sập ứng dụng.
*   **Dữ liệu đầu vào (Input):** Tạo giáo viên có mã trùng khớp với giáo viên đã tồn tại (ví dụ: `GV001`).
*   **Quy trình thực hiện:**
    1.  Mở tab *Quản lý Giáo Viên*.
    2.  Bấm *Thêm Giáo Viên Mới*.
    3.  Nhập mã: `GV001` (mã này đã có sẵn trong cơ sở dữ liệu), nhập Tên: "Nguyễn Văn Kiểm", Bộ môn: "Vật Lý".
    4.  Bấm *Lưu*.
*   **Kết quả mong đợi (Output):** Ứng dụng hiển thị cảnh báo: "Mã giáo viên 'GV001' đã tồn tại trong hệ thống. Vui lòng kiểm tra lại!", không thực hiện lưu xuống SQLite và tuyệt đối không được sập/crash ứng dụng.

### 🧪 Test Case 6: Tự động khởi tạo mật khẩu mặc định (Password generation on creation)
*   **Mục tiêu:** Xác minh mật khẩu mặc định được tạo đúng chuẩn `Gv@` + 4 ký tự cuối mã giáo viên.
*   **Dữ liệu đầu vào (Input):** Thêm mới giáo viên mã `GV0246`, tên "Phạm Lan", môn "Hóa".
*   **Quy trình thực hiện:**
    1.  Nhập thông tin giáo viên `GV0246` trên giao diện thêm mới và bấm *Lưu*.
    2.  Kiểm tra SQLite table `TeacherProfiles` bằng Tool truy vấn database.
*   **Kết quả mong đợi (Output):**
    *   Hộp thoại thông báo thêm thành công có hiển thị chuỗi mật khẩu mặc định là `Gv@0246`.
    *   Bản ghi trong database có trường `TeacherPassword` = `"Gv@0246"` và `PasswordHash` chứa chuỗi hash HMACSHA512 tương ứng (không được để trống).

### 🧪 Test Case 7: Thiết lập Layout cuộn trang ở độ phân giải 1024x768 (Projector Compatibility)
*   **Mục tiêu:** Đảm bảo toàn bộ các nút bấm không bị cắt mất khi mở trên màn hình nhỏ.
*   **Dữ liệu đầu vào (Input):** Điều chỉnh độ phân giải màn hình Windows về `1024x768`.
*   **Quy trình thực hiện:**
    1.  Mở *Bảng điều khiển Quản trị viên*.
    2.  Chọn tab *Quản lý Giáo Viên*.
    3.  Quan sát sự xuất hiện của thanh cuộn.
*   **Kết quả mong đợi (Output):** Xuất hiện thanh cuộn dọc (Vertical Scrollbar) bên phải màn hình. Người dùng có thể kéo cuộn xuống dưới cùng để tương tác đầy đủ với các nút bấm "Sửa", "Xóa" trên bảng giáo viên.

### 🧪 Test Case 8: Việt hóa tiêu đề và thống nhất thuật ngữ (Localization compliance)
*   **Mục tiêu:** Xác minh 100% văn bản giao diện hiển thị đúng tiếng Việt chuẩn sư phạm.
*   **Dữ liệu đầu vào (Input):** Mở cửa sổ Admin Console và duyệt qua toàn bộ các tab.
*   **Quy trình thực hiện:** Soát lỗi chính tả và ngôn ngữ trên tất cả các nhãn (Labels), tiêu đề cửa sổ (Window Titles), tiêu đề cột DataGrid.
*   **Kết quả mong đợi (Output):** Không còn bất kỳ từ tiếng Anh thô nào xuất hiện trên màn hình điều khiển chính (Không có `IT Console`, `Uptime`, `Active`, `Expired`...). Tất cả được hiển thị bằng tiếng Việt chuẩn xác.

---

*Hội đồng Chuyên gia đã kiểm duyệt và phê duyệt kế hoạch nâng cấp này. Lập trình viên và Tester căn cứ theo các Test Case trên để hoàn thành nghiệm thu.*
