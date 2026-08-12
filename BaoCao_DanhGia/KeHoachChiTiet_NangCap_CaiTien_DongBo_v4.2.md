# 📑 KẾ HOẠCH CHI TIẾT NÂNG CẤP & CẢI TIẾN HỆ THỐNG (PHIÊN BẢN 4.2)
*Đồng bộ hóa cấu trúc hệ thống, phản biện tối ưu hóa và áp dụng quy chuẩn sư phạm QA SmartClass v4.1*

Tài liệu này được thiết kế chi tiết dưới góc nhìn của Trưởng ban thiết kế dự án, Chuyên gia phân tích hệ thống, CSDL và QA/QC nhằm hướng dẫn lập trình viên (Coder) thực hiện chính xác, không thể làm sai, và cung cấp checksheet rõ ràng cho kiểm thử viên (Tester).

---

## 📂 SƠ ĐỒ FILE ẢNH HƯỞNG TRONG DỰ ÁN
Lập trình viên cần chú ý các tệp tin sau trước khi thực hiện nâng cấp:
1.  **Dự án Kiểm thử (Tests):** 
    *   [QASmartClass.Tests.csproj](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass.Tests/QASmartClass.Tests.csproj)
    *   [V73StudentIdentityTests.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass.Tests/V73StudentIdentityTests.cs)
    *   [V82MonitorPageRealTimeTests.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass.Tests/V82MonitorPageRealTimeTests.cs)
2.  **Kênh truyền mạng (Network):**
    *   [NetworkDiscoveryService.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Services/NetworkDiscoveryService.cs)
    *   [StudentNetworkClient.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass.Tests/V49NetworkResilienceTests.cs)
3.  **Giao diện Học sinh (Student Interface):**
    *   [StudentShell.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentShell.xaml)
    *   [StudentShell.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentShell.xaml.cs)

---

## ═══ CHI TIẾT 5 HẠNG MỤC NÂNG CẤP CHUYÊN SÂU ═══

### 🛠️ HẠNG MỤC 1: Sửa lỗi cô lập SQLite trong môi trường kiểm thử (SQLite Test Isolation)
*   **Yêu cầu thiết kế:** Thiết lập cơ chế tạo cơ sở dữ liệu vật lý cô lập ngẫu nhiên cho từng tệp test class. Loại bỏ hoàn toàn sự tranh chấp quyền ghi file khi chạy kiểm thử tự động đồng thời nhiều luồng.
*   **Dữ liệu đầu vào:**
    *   Đường dẫn CSDL mặc định: `LocalApplicationData\QASmartClass\smartclass.db`.
    *   Cơ chế đổi tên động: `smartclass_test_v73_{GUID}.db` đặt tại thư mục tạm thời của hệ thống.
*   **Dữ liệu đầu ra:**
    *   Tệp cơ sở dữ liệu SQLite tạm thời được tạo thành công với cấu trúc bảng (schema) được migrate hoàn chỉnh.
    *   Tệp tạm thời và các file đồng hành `.db-wal`, `.db-shm` được dọn dẹp sạch sẽ sau khi test xong.
*   **Phương pháp thực hiện:**
    1.  Trong phương thức khởi tạo của lớp test (ví dụ: `V73StudentIdentityTests`), sinh một GUID duy nhất:
        ```csharp
        var uniqueId = Guid.NewGuid().ToString("N");
        _dbFile = Path.Combine(AppPaths.RootDir, $"smartclass_test_v73_{uniqueId}.db");
        _versionFile = Path.Combine(AppPaths.RootDir, $"db_version_test_v73_{uniqueId}.txt");
        AppPaths.DatabaseFile = _dbFile;
        AppPaths.DbVersionFile = _versionFile;
        ```
    2.  Gọi `DbMigrator.Migrate(new AppDbContext(), CURRENT_VERSION)` để tạo đầy đủ bảng và dữ liệu mẫu (Seed).
    3.  Trong phương thức `Dispose()`, giải phóng các kết nối đang mở trước khi xóa file:
        ```csharp
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbFile)) File.Delete(_dbFile);
        if (File.Exists(_versionFile)) File.Delete(_versionFile);
        ```
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao không dùng SQLite In-Memory (`DataSource=:memory:`) để chạy nhanh hơn?
    *   *Tối ưu:* SQLite In-Memory không hỗ trợ đầy đủ các tính năng khóa tệp tin thực tế, cơ chế Write-Ahead Logging (WAL) và kiểm tra lỗi khóa đa luồng (SQLite Error 5/26). Việc tạo file db vật lý ngẫu nhiên đảm bảo tính trung thực 100% so với môi trường vận hành thực tế tại phòng máy trường học.
    *   *Ràng buộc kỹ thuật QA v4.1:* Bắt buộc thực hiện `ClearAllPools()` trước khi `Delete` để tránh ném ngoại lệ `IOException` do Windows giữ lock file.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Đã thêm sinh GUID cho đường dẫn tệp tin CSDL và tệp phiên bản trong Constructor.
    - [ ] [ ] Đã gọi `DbMigrator.Migrate` để khởi tạo cấu trúc dữ liệu trước khi chạy bất kỳ test case nào.
    - [ ] [ ] Đã gọi `Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools()` trong hàm `Dispose`.
    - [ ] [ ] Kiểm tra thư mục tạm thời sau khi kết thúc test, không còn tồn tại file `.db-wal` hay `.db-shm` rác.

---

### 🛠️ HẠNG MỤC 2: Sửa lỗi Reflection Mismatch trong UI Tests
*   **Yêu cầu thiết kế:** Đồng bộ các tham chiếu Reflection trong tệp [V82MonitorPageRealTimeTests.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass.Tests/V82MonitorPageRealTimeTests.cs) để tương thích hoàn toàn với cấu trúc lớp sản xuất `MonitorPage` mới.
*   **Dữ liệu đầu vào:**
    *   Trường trong sản xuất: `private static readonly string[] BannedApps` (Mảng tĩnh chỉ đọc).
    *   Trường trong kiểm thử cũ: Ép kiểu sang `List<string>`.
*   **Dữ liệu đầu ra:**
    *   Hệ thống biên dịch thành công và vượt qua 100% test case trong `V82MonitorPageRealTimeTests`.
*   **Phương pháp thực hiện:**
    1.  Tìm trường Reflection `BannedApps` trong code test, đổi kiểu ép từ `List<string>` thành mảng `string[]`:
        ```csharp
        var bannedAppsField = typeof(MonitorPage).GetField("BannedApps", BindingFlags.NonPublic | BindingFlags.Static);
        var bannedApps = (string[])bannedAppsField.GetValue(null);
        Assert.Contains("Minecraft", bannedApps);
        ```
    2.  Loại bỏ kiểm thử Reflection vào trường `_networkEventQueue` do logic chính đã được tối ưu hóa xử lý đồng bộ trực tiếp qua `DispatcherTimer`. Thay thế bằng kiểm thử trực tiếp logic xử lý tin nhắn `OnStudentMessage`.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Có nên giữ hàng đợi queue để xử lý bất đồng bộ tránh lag giao diện?
    *   *Tối ưu:* Hàng đợi làm tăng độ phức tạp và độ trễ phản hồi màn hình. Logic mới tự động giải mã Base64 trên ThreadPool trước khi đưa dữ liệu ảnh tĩnh vào `WriteableBitmap.CopyPixels` trên luồng UI. Đây là giải pháp tối ưu nhất giúp loại bỏ hàng đợi mà UI vẫn mượt mà.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Đã đổi kiểu ép Reflection của `BannedApps` thành `string[]`.
    - [ ] [ ] Đã loại bỏ tham chiếu tới trường `_networkEventQueue` không còn tồn tại.
    - [ ] [ ] Chạy lệnh `dotnet test --filter QASmartClass.Tests.V82MonitorPageRealTimeTests` đạt trạng thái Đạt (Passed).

---

### 🛠️ HẠNG MỤC 3: Nâng cấp bảo mật truyền tải lệnh qua TCP (HMAC-SHA256 & Timestamp)
*   **Yêu cầu thiết kế:** Ngăn chặn tuyệt đối việc học sinh sử dụng phần mềm thứ ba để đánh chặn, giả mạo các lệnh điều khiển (như khóa màn hình, mở web) gửi từ máy giáo viên qua mạng LAN.
*   **Dữ liệu đầu vào:**
    *   Bản tin gốc (Plaintext): `CMD|LOCK|ALL`.
    *   Mã khóa phiên ngẫu nhiên (`SessionKey` dạng HEX 64 ký tự) được sinh ra trên máy giáo viên khi bắt đầu lớp học.
*   **Dữ liệu đầu ra:**
    *   Bản tin đóng gói bảo mật: `CMD|LOCK|ALL|ts=1719284200|sig=HEX_HMAC_SHA256`.
*   **Phương pháp thực hiện:**
    1.  Khi bắt đầu phiên dạy học, máy giáo viên tự động sinh khóa ngẫu nhiên và đóng gói vào UDP Beacon truyền đi.
    2.  Khi gửi một lệnh TCP `CMD|...`, tích hợp thêm dấu thời gian Unix `ts` hiện tại.
    3.  Tính toán chữ ký `sig = HMAC_SHA256(command_body + ts, SessionKey)`.
    4.  Phía học sinh khi nhận bản tin:
        *   Tách chữ ký và kiểm tra timestamp. Nếu `|UnixTime_GV - UnixTime_HS| > 5` giây: Từ chối thực thi lệnh (Ngăn chặn Replay Attack).
        *   Tính toán lại chữ ký bằng `SessionKey` nhận được từ UDP Beacon trước đó. Nếu chữ ký không trùng khớp: Bỏ qua bản tin và ghi nhật ký cảnh báo bảo mật.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao không sử dụng HTTPS / TLS cho đơn giản?
    *   *Tối ưu:* Giao thức TLS yêu cầu cài đặt chứng chỉ số (SSL Certificate) trên từng máy học sinh. Điều này cực kỳ phức tạp để bảo trì trong mạng LAN trường học (không kết nối Internet). Cơ chế ký HMAC-SHA256 kết hợp timestamp siêu nhẹ, tốc độ xử lý nhanh (<1ms) và an toàn tuyệt đối trước các đòn tấn công giả mạo phổ biến.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Máy giáo viên sinh động `SessionKey` ngẫu nhiên và truyền phát qua UDP Beacon.
    - [ ] [ ] Bản tin TCP gửi đi bắt buộc chứa tham số `ts` (timestamp) và `sig` (chữ ký).
    - [ ] [ ] Client học sinh kiểm tra độ lệch thời gian (không quá 5 giây) trước khi xử lý chữ ký.
    - [ ] [ ] Viết test case tự động giả lập bản tin sai signature, xác nhận client học sinh từ chối thực thi lệnh.

---

### 🛠️ HẠNG MỤC 4: Thiết kế Màn hình chia đôi (Split-Screen Mode) trên Client Học sinh
*   **Yêu cầu thiết kế:** Cung cấp trải nghiệm học tập tương tác chủ động (Active Learning). Học sinh có thể vừa theo dõi slide bài giảng của giáo viên, vừa làm bài tập hoặc ghi chép ở nửa màn hình còn lại.
*   **Dữ liệu đầu vào:**
    *   Luồng ảnh màn hình giáo viên nhận qua TCP/WebSockets.
    *   Trạng thái nhấp nút Toggle (Toàn màn hình / Chia đôi) trên UI học sinh.
*   **Dữ liệu đầu ra:**
    *   Grid giao diện của `StudentShell.xaml` tự động thay đổi độ rộng giữa hai cột: `100% / 0%` (Xem bài giảng) sang `50% / 50%` (Chia đôi).
*   **Phương pháp thực hiện:**
    1.  Trong [StudentShell.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentShell.xaml), thiết lập bố cục Grid chính có 2 cột:
        ```xml
        <Grid.ColumnDefinitions>
            <ColumnDefinition x:Name="colBroadcast" Width="*"/>
            <ColumnDefinition x:Name="colWorkspace" Width="0"/>
        </Grid.ColumnDefinitions>
        ```
    2.  Thêm nút bấm dạng Toggle Button trên thanh công cụ sử dụng Path SVG trực quan.
    3.  Trong [StudentShell.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentShell.xaml.cs), bắt sự kiện click:
        *   Nếu chế độ chia đôi kích hoạt: Set `colWorkspace.Width = new GridLength(1, GridUnitType.Star)`.
        *   Nếu tắt: Set `colWorkspace.Width = new GridLength(0)`.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Màn hình chia đôi làm hình ảnh bài giảng bị thu nhỏ khó đọc?
    *   *Quy chuẩn Sư phạm:* Áp dụng thuộc tính `Stretch="Uniform"` cho Image trình chiếu để bảo toàn tỷ lệ màn hình của giáo viên không bị méo. Đồng thời bổ sung phím tắt `Ctrl +` và `Ctrl -` cho phép học sinh phóng to/thu nhỏ vùng bài giảng.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Nút Toggle Split-screen hiển thị sắc nét bằng ảnh vectơ SVG trên thanh tiêu đề.
    - [ ] [ ] Thay đổi kích thước Grid diễn ra mượt mà, không gây giật màn hình hoặc đơ luồng xử lý ảnh.
    - [ ] [ ] Tỷ lệ ảnh bài giảng được giữ nguyên (Uniform) khi ở chế độ chia đôi, không bị bóp méo chiều ngang.

---

### 🛠️ HẠNG MỤC 5: Ngăn chặn vượt rào bằng Task Manager khi Khóa máy
*   **Yêu cầu thiết kế:** Tăng cường lớp phòng thủ bảo mật. Vô hiệu hóa khả năng tắt tiến trình ứng dụng client học sinh thông qua Task Manager của Windows khi trong phiên khóa máy hoặc thi cử.
*   **Dữ liệu đầu vào:**
    *   Trạng thái khóa máy (`IsLocked = true` hoặc `IsForceWatch = true`).
*   **Dữ liệu đầu ra:**
    *   Registry key `DisableTaskMgr` được cập nhật tương ứng trong Registry của người dùng.
*   **Phương pháp thực hiện:**
    1.  Viết lớp Helper can thiệp Registry tại nhánh `HKEY_CURRENT_USER`:
        ```csharp
        public static void SetTaskManagerEnabled(bool enabled)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                if (enabled)
                    key.DeleteValue("DisableTaskMgr", false);
                else
                    key.SetValue("DisableTaskMgr", 1, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch (Exception ex) { Log.Warning("Không thể thay đổi Registry Task Manager: {Err}", ex.Message); }
        }
        ```
    2.  Khi nhận lệnh `LOCK`, gọi `SetTaskManagerEnabled(false)`. Khi nhận lệnh `UNLOCK`, gọi `SetTaskManagerEnabled(true)`.
    3.  **Ràng buộc an toàn vận hành:** Đăng ký hàm khôi phục Task Manager trong sự kiện `SessionEnding` (khi tắt máy/logoff) để đảm bảo không vô hiệu hóa Task Manager vĩnh viễn của hệ điều hành.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc can thiệp Registry có bị phần mềm diệt virus chặn hoặc yêu cầu quyền Administrator không?
    *   *Tối ưu:* Can thiệp vào nhánh `HKEY_CURRENT_USER` (HKCU) hoàn toàn được Windows cho phép ở mức quyền người dùng thông thường, không yêu cầu quyền Admin (UAC prompt), đảm bảo phần mềm hoạt động trơn tru trên mọi tài khoản học sinh.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Viết phương thức ghi Registry HKCU an toàn có xử lý ngoại lệ `try-catch`.
    - [ ] [ ] Đảm bảo Registry được khôi phục về giá trị mặc định khi đóng ứng dụng thông thường hoặc khi tắt máy.
    - [ ] [ ] Kiểm tra thủ công: Khi máy ở trạng thái khóa, nhấn `Ctrl+Alt+Del`, nút bấm "Task Manager" phải bị ẩn hoặc không thể nhấn được.
