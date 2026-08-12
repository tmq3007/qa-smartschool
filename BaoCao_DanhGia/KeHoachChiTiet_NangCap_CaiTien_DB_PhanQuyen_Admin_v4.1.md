# Kế hoạch Chi tiết Nâng cấp & Cải tiến Cấu hình CSDL, Phân quyền, Thư mục chia sẻ & Bảo trì (Admin Module)
**Tài liệu hướng dẫn phát triển và kiểm thử - Chuẩn QA SmartClass v4.1**  

---

## ═══ TỔNG QUAN HỆ THỐNG & NHIỆM VỤ ═══

Tài liệu này đóng vai trò đặc tả kỹ thuật và lộ trình nâng cấp dành cho lập trình viên (coder) và chuyên gia kiểm thử (tester) nhằm khắc phục triệt để các lỗ hổng bảo mật leo thang đặc quyền, lỗi ghi đè dữ liệu không xác thực PIN, lỗi hiển thị sai file cấu hình học sinh, thiếu cấu hình đường dẫn thư mục chia sẻ LAN, và chuẩn hóa Việt hóa phân hệ quản trị trong dự án **QA SmartClass**.

Đặc biệt, tài liệu này bổ sung cấu hình lựa chọn **Chế độ bảo mật vai trò, PIN khôi phục CSDL và Thư mục lưu trữ Backup** trong phần cài đặt hệ thống (Master Settings) để nhà trường tự quyết định dựa trên điều kiện hạ tầng kỹ thuật thực tế, đồng thời cung cấp hệ thống kịch bản kiểm thử (Test Cases) mở rộng đầy đủ để đảm bảo chất lượng phần mềm ở mức tối đa.

Yêu cầu thực hiện nghiêm ngặt, tuân thủ đúng cấu trúc chương trình hiện hữu và bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**.

---

## ═══ PHẦN 1: CÁC CẢI TIẾN KỸ THUẬT & CHỨC NĂNG CHI TIẾT (TECHNICAL UPGRADES) ═══

### 1. Tùy chọn Master cho Xác thực PIN & Tạo bản lưu dự phòng trước khi Khôi phục CSDL
*   **Vấn đề:** Việc khôi phục (Restore) CSDL đè lên CSDL hiện tại rất nguy hiểm. Cần bắt buộc bảo vệ bằng mã PIN và tự động sao lưu dự phòng. Tuy nhiên, ở các phòng máy thử nghiệm biệt lập hoặc trong quá trình cài đặt hàng loạt nhanh, nhà trường có nhu cầu tắt xác thực PIN để tối ưu thời gian thao tác.
*   **Giải pháp (Cấu hình Master trên DB & UI Settings):**
    *   Thêm thiết lập **"Yêu cầu PIN khi Khôi phục CSDL"** lưu vào bảng `SystemSettings` trong SQLite:
        1. `PinRequired` (Bắt buộc xác thực): Hiện cửa sổ `PinDialog` trước khi Restore (Mặc định - Khuyên dùng).
        2. `PinBypass` (Bỏ qua xác thực): Cho phép Restore trực tiếp không cần PIN (Dành cho mạng nội bộ cô lập).
*   **Tệp tin sửa đổi:** [AdminConsoleWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml.cs)
    *   *Phương pháp thực hiện:* Đọc cấu hình `RestorePinMode` từ `SystemSettings` trước khi yêu cầu PIN.
    *   *Dòng code cải tiến:*
        ```csharp
        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var pinSetting = db.SystemSettings.Find("RestorePinMode");
                    string pinMode = pinSetting?.Value ?? "PinRequired";

                    if (pinMode == "PinRequired")
                    {
                        var pinDialog = new PinDialog();
                        if (pinDialog.ShowDialog() != true)
                        {
                            Log.Information("Restore backup aborted: PIN verification failed.");
                            return;
                        }
                    }
                }

                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn file Backup để khôi phục",
                    Filter = "Database Files (*.db)|*.db|All Files (*.*)|*.*",
                    InitialDirectory = GetBackupDirectory()
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFile = openFileDialog.FileName;
                    
                    var result = MessageBox.Show(
                        $"Bạn có chắc chắn muốn khôi phục từ file backup này không?\n\n{Path.GetFileName(selectedFile)}\n\nDữ liệu hiện tại sẽ bị ghi đè hoàn toàn!",
                        "Xác nhận Khôi phục", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        var app = (QASmartTouch.App)Application.Current;
                        var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;

                        // Sao lưu dự phòng khẩn cấp trước khi khôi phục
                        if (File.Exists(dbPath))
                        {
                            var autoBackupName = $"smartclass_AUTOBACKUP_BEFORE_RESTORE_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                            var autoBackupPath = Path.Combine(GetBackupDirectory(), autoBackupName);
                            File.Copy(dbPath, autoBackupPath, true);
                        }

                        app.Database?.Dispose(); // Giải phóng CSDL hiện tại để mở khóa file
                        File.Copy(selectedFile, dbPath, true);
                        
                        MessageBox.Show("Đã khôi phục dữ liệu thành công. Phần mềm sẽ tự động đóng, vui lòng khởi động lại.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khôi phục Backup: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        ```

---

### 2. Tùy chọn Master cho Chế độ Bảo mật Vai trò Admin (Role Security Options)
*   **Vấn đề:** Đọc plaintext file `admin_role.txt` dễ bị hack leo thang đặc quyền. Nhưng trong môi trường cài đặt ổ cứng ảo phòng máy (Diskless/BootROM) hoặc chạy test tự động, việc khóa cứng DPAPI cấp máy có thể gây lỗi xác thực khi nhân bản ảnh đĩa (Image OS).
*   **Giải pháp (Cấu hình Master trên DB & UI Settings):**
    *   Thêm thiết lập **"Chế độ bảo mật cấu hình Vai trò"** lưu vào SQLite:
        1. `DPAPI_Encrypted` (Mã hóa DPAPI): File `admin_role.txt` được mã hóa nhị phân bằng chứng chỉ Windows (An toàn tuyệt đối - Mặc định).
        2. `Plaintext` (Văn bản thường): File `admin_role.txt` được lưu dạng chuỗi chữ thường dễ sửa (Phù hợp phòng máy ảo nhân bản BootROM).
*   **Tệp tin sửa đổi:** [AdminConsoleWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml.cs)
    *   *Phương pháp thực hiện:*
        ```csharp
        private static AdminRole DetectRole()
        {
            try
            {
                var roleFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, "admin_role.txt");
                if (File.Exists(roleFile))
                {
                    using var db = new AppDbContext();
                    var secureSetting = db.SystemSettings.Find("RoleSecurityMode");
                    string secureMode = secureSetting?.Value ?? "DPAPI_Encrypted";

                    if (secureMode == "DPAPI_Encrypted")
                    {
                        byte[] encryptedBytes = File.ReadAllBytes(roleFile);
                        byte[] decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                            encryptedBytes, null, System.Security.Cryptography.DataProtectionScope.LocalMachine);
                        string roleName = System.Text.Encoding.UTF8.GetString(decryptedBytes).Trim().ToUpperInvariant();
                        return ParseRole(roleName);
                    }
                    else
                    {
                        // Chế độ Plaintext cho môi trường phòng máy BootROM
                        string roleName = File.ReadAllText(roleFile).Trim().ToUpperInvariant();
                        return ParseRole(roleName);
                    }
                }
            }
            catch { }
            return AdminRole.L3_Teacher; // Mặc định an toàn
        }

        private static AdminRole ParseRole(string roleName)
        {
            return roleName switch
            {
                "L1" or "VENDOR" or "L1_VENDOR" => AdminRole.L1_Vendor,
                "L2" or "SCHOOL" or "L2_SCHOOLADMIN" => AdminRole.L2_SchoolAdmin,
                _ => AdminRole.L3_Teacher
            };
        }
        ```

---

### 3. Tùy chọn Master cho Thư mục Lưu trữ Backup CSDL (Custom Backup Path Option)
*   **Vấn đề:** Đường dẫn sao lưu mặc định lưu tại MyDocuments trong ổ C. Khi máy tính giáo viên bị lỗi cài lại Windows, toàn bộ file backup ổ C sẽ bị mất. Nhà trường cần cấu hình chuyển thư mục backup sang ổ đĩa dữ liệu (D:, E:) hoặc ổ đĩa mạng LAN.
*   **Yêu cầu kỹ thuật:** Thêm ô cấu hình `BackupFolderPath` động trên UI và lưu vào SQLite.
*   **Tệp tin sửa đổi:** [SchoolAdminDashboardControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml.cs)
    *   *Phương pháp thực hiện:*
        ```csharp
        private string GetBackupDirectory()
        {
            using var db = new AppDbContext();
            var backupSetting = db.SystemSettings.Find("BackupFolderPath");
            return backupSetting != null && !string.IsNullOrEmpty(backupSetting.Value)
                ? backupSetting.Value
                : QASmartClass.Services.AppPaths.BackupsDir;
        }
        ```

---

### 4. Tích hợp UI Cấu hình Master trên Tab Cấu hình Admin
*   **Mã nguồn sửa đổi:** [SchoolAdminDashboardControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml)
*   **Chi tiết giao diện XAML bổ sung (Đặt trong Grid `tabSettings`):**
    ```xml
    <!-- Nhóm Cấu hình Bảo mật & Sao lưu -->
    <Border Background="#1E293B" CornerRadius="8" Padding="20" BorderBrush="#334155" BorderThickness="1" Margin="0,15,0,0">
        <StackPanel>
            <TextBlock Text="🔒 Thiết lập Bảo mật &amp; Bảo trì CSDL" Foreground="White" FontWeight="SemiBold" FontSize="14" Margin="0,0,0,15"/>
            
            <!-- Chế độ bảo mật vai trò Admin -->
            <TextBlock Text="Chế độ bảo mật cấu hình vai trò Admin" Foreground="#E2E8F0" FontSize="13" Margin="0,0,0,5"/>
            <RadioButton x:Name="radRoleSecureDPAPI" Content="🛡️ DPAPI Encrypted (Mã hóa bảo vệ cấp thiết bị - Khuyên dùng)" Foreground="White" FontSize="12" Margin="0,3" GroupName="RoleSecurityGroup" IsChecked="True"/>
            <RadioButton x:Name="radRolePlaintext" Content="🔓 Plaintext Config (Văn bản thường - Phù hợp phòng Lab ảo Diskless)" Foreground="White" FontSize="12" Margin="0,3,0,15" GroupName="RoleSecurityGroup"/>

            <!-- Yêu cầu PIN khi Restore -->
            <TextBlock Text="Xác thực quyền khi khôi phục CSDL (Restore DB)" Foreground="#E2E8F0" FontSize="13" Margin="0,0,0,5"/>
            <RadioButton x:Name="radRestorePinRequired" Content="🔑 Yêu cầu nhập mã PIN bảo mật" Foreground="White" FontSize="12" Margin="0,3" GroupName="RestorePinGroup" IsChecked="True"/>
            <RadioButton x:Name="radRestorePinBypass" Content="⚡ Khôi phục trực tiếp không cần PIN (Thuận tiện cài nhanh)" Foreground="White" FontSize="12" Margin="0,3,0,15" GroupName="RestorePinGroup"/>

            <!-- Thư mục lưu trữ Backup -->
            <TextBlock Text="Thư mục lưu trữ File sao lưu CSDL (Backups Directory)" Foreground="#E2E8F0" FontSize="13" Margin="0,0,0,5"/>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBox x:Name="txtBackupFolder" Padding="8" Background="#0F172A" Foreground="White" BorderBrush="#334155" Height="36" VerticalContentAlignment="Center"/>
                <Button Grid.Column="1" Content="📁 Chọn Thư Mục" Style="{StaticResource ActionButton}" Margin="10,0,0,0" Height="36" Padding="15,0" Click="BtnBrowseBackupFolder_Click"/>
            </Grid>
        </StackPanel>
    </Border>
    ```

---

## ═══ MA TRẬN KỊCH BẢN KIỂM THỬ KHẮC PHỤC MỞ RỘNG (EXTENDED TEST CASES) ═══

Để coder không thể làm sai lệch nghiệp vụ, đội ngũ kiểm thử QA Tester sẽ sử dụng 10 Test Cases chi tiết sau để nghiệm thu.

### 🧪 Test Case 1: Thẩm định mã hóa vai trò L1 bằng DPAPI (DPAPI Role Encryption)
*   **Mục tiêu:** Xác nhận ghi và đọc bảo mật file `admin_role.txt` thành công.
*   **Quy trình thực hiện:** 
    1. Vào Cấu hình -> Chọn `DPAPI Encrypted` -> Lưu.
    2. Thiết lập vai trò L1 cho thiết bị thông qua hàm `SaveRoleSecurely("L1")`.
    3. Mở file `admin_role.txt` bằng Notepad ngoài Windows Explorer.
*   **Kết quả mong đợi:** File `admin_role.txt` chứa chuỗi mã hóa nhị phân (Ký tự lạ không đọc được). Khởi động lại IT Console, hệ thống giải mã thành công hiển thị vai trò *"L1 — QA Vendor"*.

### 🧪 Test Case 2: Kiểm thử Leo thang quyền bất hợp pháp (Exploit Prevention Test)
*   **Mục tiêu:** Xác minh tính chống sửa đổi file vai trò.
*   **Quy trình thực hiện:**
    1. Chế độ bảo mật vai trò đang đặt là `DPAPI Encrypted`.
    2. Mở file `admin_role.txt` bằng Notepad, sửa hoặc ghi chữ `"L1"` bằng tay rồi lưu lại.
    3. Khởi động lại IT Console.
*   **Kết quả mong đợi:** Hệ thống phát hiện dữ liệu lỗi mã hóa (không thể giải mã bằng khóa DPAPI), ghi Log cảnh báo và tự động hạ quyền của người dùng xuống `L3_Teacher IT` (Ẩn hoàn toàn tab School Admin và Vendor).

### 🧪 Test Case 3: Thẩm định Chế độ vai trò Plaintext (BootROM lab mode)
*   **Mục tiêu:** Xác minh chế độ đọc file text thường hoạt động đúng khi cấu hình Plaintext.
*   **Quy trình thực hiện:**
    1. Vào Cấu hình -> Chọn `Plaintext Config` -> Lưu.
    2. Ghi chữ `"L2"` vào file `admin_role.txt` bằng Notepad và lưu lại.
    3. Khởi động lại IT Console.
*   **Kết quả mong đợi:** Hệ thống đọc trực tiếp, hiển thị vai trò *"L2 — Quản trị Trường"* và mở đúng các tab tương ứng.

### 🧪 Test Case 4: Khôi phục CSDL bắt buộc xác thực PIN (Restore PIN Required)
*   **Mục tiêu:** Đảm bảo tính bảo mật khi khôi phục cơ sở dữ liệu.
*   **Quy trình thực hiện:**
    1. Cấu hình chọn `Yêu cầu nhập mã PIN bảo mật` -> Lưu.
    2. Bấm nút "Khôi phục từ Backup" trên giao diện Storage.
*   **Kết quả mong đợi:** Hộp thoại PIN Dialog hiển thị yêu cầu nhập mã PIN. Nếu nhập sai hoặc tắt cửa sổ PIN, OpenFileDialog không được phép mở ra.

### 🧪 Test Case 5: Khôi phục CSDL bỏ qua mã PIN (Restore PIN Bypass)
*   **Mục tiêu:** Xác nhận khả năng bỏ qua xác thực PIN khi bảo trì hàng loạt.
*   **Quy trình thực hiện:**
    1. Cấu hình chọn `Khôi phục trực tiếp không cần PIN` -> Lưu.
    2. Bấm nút "Khôi phục từ Backup".
*   **Kết quả mong đợi:** OpenFileDialog chọn file hiện ra trực tiếp mà không hiển thị cửa sổ nhập PIN.

### 🧪 Test Case 6: Sao lưu khẩn cấp trước khi Restore (Emergency Restore Backup)
*   **Mục tiêu:** Đảm bảo không mất mát dữ liệu hiện tại khi Restore đè.
*   **Quy trình thực hiện:**
    1. Bấm nút Khôi phục từ Backup -> Nhập mã PIN -> Chọn file Backup mong muốn -> Bấm đồng ý ghi đè.
    2. Kiểm tra thư mục sao lưu Backup.
*   **Kết quả mong đợi:** Xuất hiện tệp CSDL dự phòng khẩn cấp có tiền tố `smartclass_AUTOBACKUP_BEFORE_RESTORE_*.db` lưu trữ trạng thái CSDL ngay trước khi copy file mới đè lên.

### 🧪 Test Case 7: Thiết lập Thư mục Backup tùy chỉnh (Custom Backup Path redirection)
*   **Mục tiêu:** Xác nhận hướng thư mục lưu trữ Backup thành công.
*   **Quy trình thực hiện:**
    1. Tạo thư mục `D:\SmartClassBackups` trên ổ đĩa.
    2. Tại UI cấu hình, gõ đường dẫn hoặc bấm chọn thư mục này -> Lưu cấu hình.
    3. Nhấn "Khôi phục từ Backup" hoặc chạy sao lưu tự động.
*   **Kết quả mong đợi:** Thư mục lưu mặc định của OpenFileDialog trỏ thẳng về `D:\SmartClassBackups`. File backup khẩn cấp trước khôi phục được lưu đúng tại đây.

### 🧪 Test Case 8: Khắc phục lỗi mở File cấu hình Học sinh (Dynamic Student Profile File)
*   **Mục tiêu:** Đảm bảo mở đúng file JSON học sinh động.
*   **Quy trình thực hiện:**
    1. Cho một học sinh mã `HS009` đăng nhập trên máy trạm để tạo file `student_profile_{hwId}_HS009.json` trong Settings.
    2. Đăng nhập Admin Console -> Bấm nút "Mở file Cấu hình Học sinh".
*   **Kết quả mong đợi:** Notepad mở lên hiển thị đúng nội dung của file `student_profile_{hwId}_HS009.json`, không báo lỗi file không tồn tại.

### 🧪 Test Case 9: Thẩm định Bảng màu Logs Slate tối (Live Logs Palette Test)
*   **Mục tiêu:** Đảm bảo thẩm mỹ logs dịu mắt, chống mỏi mắt.
*   **Quy trình thực hiện:** Mở tab Live Logs, bấm "Tải lại Log".
*   **Kết quả mong đợi:** Log hiển thị trong TextBox có màu nền Slate xám tối `#1E293B`, màu chữ trắng tuyết `#F8FAFC`, không có màu đen-xanh neon gây mỏi mắt.

### 🧪 Test Case 10: Thẩm định Việt hóa giao diện Phân quyền (RBAC Localization Test)
*   **Mục tiêu:** Xác minh giao diện Phân quyền Việt hóa 100%.
*   **Quy trình thực hiện:** Mở trang quản trị Phân quyền của BGH. Soát lỗi chính tả trên tất cả các hộp kiểm quyền hạn.
*   **Kết quả mong đợi:** 100% văn bản checkbox hiển thị tiếng Việt rõ ràng, có chú thích giải nghĩa chi tiết cho các quyền nâng cao khi di chuột vào (Tooltip).

---

*Tài liệu đã được Hội đồng Chuyên gia thông qua, bàn giao cho đội ngũ phát triển.*
