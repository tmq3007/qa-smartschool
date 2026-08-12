# 📑 KẾ HOẠCH CHI TIẾT NÂNG CẤP & CẢI TIẾN HỆ THỐNG (PHIÊN BẢN 4.2.1)
*Định tuyến đường dẫn dữ liệu thích ứng DeepFreeze & Kháng lỗi thiếu WebView2 Runtime*

Tài liệu này được thiết kế chi tiết dưới góc nhìn của Trưởng ban thiết kế dự án, Chuyên gia phân tích hệ thống, CSDL và QA/QC nhằm hướng dẫn lập trình viên (Coder) thực hiện chính xác, không thể làm sai, và cung cấp checksheet rõ ràng cho kiểm thử viên (Tester).

---

## 📂 SƠ ĐỒ FILE ẢNH HƯỞNG TRONG DỰ ÁN
Lập trình viên cần định vị chính xác các tệp tin sau trước khi thực hiện nâng cấp:
1.  **Dịch vụ đường dẫn hệ thống (System Paths):**
    *   [AppPaths.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Services/AppPaths.cs)
2.  **Khởi chạy ứng dụng (App Startup):**
    *   [App.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/App.xaml.cs)
3.  **Trình vẽ đồ thị Desmos (STEM Math):**
    *   [GraphWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml)
    *   [GraphWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml.cs)

---

## ═══ CHI TIẾT 2 HẠNG MỤC NÂNG CẤP THÍCH ỨNG CHUYÊN SÂU ═══

### 🛠️ HẠNG MỤC 1: Di dời thư mục dữ liệu qua tham số dòng lệnh `--data-dir` (Thích ứng DeepFreeze)
*   **Yêu cầu thiết kế:**
    *   Học sinh hoặc kỹ thuật viên có thể truyền tham số `--data-dir="D:\QASmartData"` khi khởi chạy ứng dụng.
    *   Hệ thống phải tự động chuyển toàn bộ thư mục hoạt động (CSDL SQLite `smartclass.db`, các tệp tin cấu hình `settings.json`, tệp profile nháp, thư mục nhận file `ReceivedFiles` và nhật ký log) về thư mục mới này.
    *   Nếu không truyền tham số, hệ thống tự động fallback về thư mục `AppData/Local` mặc định.
*   **Dữ liệu đầu vào:** Mảng tham số dòng lệnh `string[] args` từ `OnStartup` (ví dụ: `["--student", "--data-dir=D:\QASmartData"]`).
*   **Dữ liệu đầu ra:** Cấu hình đường dẫn thư mục `AppPaths` được cập nhật tương ứng.
*   **Phương pháp thực hiện:**
    1.  Tại [AppPaths.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Services/AppPaths.cs), khai báo thuộc tính tĩnh:
        ```csharp
        public static string? DataDirOverride { get; set; }
        ```
    2.  Cập nhật thuộc tính `RootDir` và `DocumentsDir`:
        ```csharp
        public static string RootDir => DataDirOverride != null 
            ? DataDirOverride 
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QASmartClass");

        public static string DocumentsDir => DataDirOverride != null 
            ? Path.Combine(DataDirOverride, "Documents")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QASmartClass");
        ```
    3.  Tại [App.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/App.xaml.cs), trong hàm `OnStartup`:
        Trước khi gọi `AppSettings.Load()`, tiến hành parse tham số `--data-dir=`:
        ```csharp
        var args = Environment.GetCommandLineArgs();
        var dataDirArg = args.FirstOrDefault(a => a.StartsWith("--data-dir=", StringComparison.OrdinalIgnoreCase));
        if (dataDirArg != null)
        {
            var dir = dataDirArg.Substring("--data-dir=".Length).Trim('"');
            if (!string.IsNullOrEmpty(dir))
            {
                QASmartClass.Services.AppPaths.DataDirOverride = dir;
            }
        }
        ```
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc chuyển đường dẫn có ảnh hưởng đến kết nối SQLite cũ đang mở không?
    *   *Tự phản biện:* Không. Việc thiết lập `DataDirOverride` được thực thi ở dòng đầu tiên của `OnStartup`, trước khi `AppDbContext` được khởi tạo hoặc gọi `EnsureCreated()`. Nhờ đó CSDL SQLite và cấu hình sẽ luôn trỏ đúng đường dẫn mới nhất.
    *   *Quy chuẩn thiết kế v4.1:* Đảm bảo tạo thư mục vật lý đích bằng `EnsureDirectories()` ngay sau khi set override.

---

### 🛠️ HẠNG MỤC 2: Tích hợp giao diện Fallback UI cứu hộ WebView2 Runtime
*   **Yêu cầu thiết kế:**
    *   Khi học sinh hoặc giáo viên mở `GraphWindow` vẽ đồ thị Desmos, nếu máy trạm chưa cài đặt WebView2 Runtime, hệ thống không được crash hay văng lỗi cảnh báo của HĐH.
    *   Ẩn WebView2 bị lỗi và hiển thị một giao diện đồ họa WPF đẹp mắt (Fallback Grid) cảnh báo lỗi thiếu thư viện kèm nút bấm hướng dẫn tải trực tiếp installer từ Microsoft.
*   **Dữ liệu đầu vào:** Sự kiện ném ra ngoại lệ `Exception` khi gọi `webView.EnsureCoreWebView2Async()`.
*   **Dữ liệu đầu ra:** Thay đổi trạng thái hiển thị (`Visibility`) giữa `webView` (Collapsed) và `fallbackGrid` (Visible).
*   **Phương pháp thực hiện:**
    1.  Tại [GraphWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml), sửa cấu hình phần thân WebView2 bằng Grid bao bọc:
        ```xml
        <Grid Grid.Row="1">
            <wv2:WebView2 x:Name="webView" DefaultBackgroundColor="White"/>
            
            <!-- Fallback UI cứu hộ khi thiếu WebView2 -->
            <Grid x:Name="fallbackGrid" Visibility="Collapsed" Background="#F5F7FA" Padding="40">
                <Border Background="White" CornerRadius="16" BorderBrush="#E5E9F0" BorderThickness="1"
                        MaxWidth="600" MaxHeight="420" VerticalAlignment="Center" HorizontalAlignment="Center">
                    <Border.Effect>
                        <DropShadowEffect BlurRadius="15" ShadowDepth="2" Color="#CCCCCC" Opacity="0.4"/>
                    </Border.Effect>
                    <StackPanel Padding="30" VerticalAlignment="Center" HorizontalAlignment="Center">
                        <TextBlock Text="⚠️ LỖI KHỞI TẠO WEBVIEW2" FontSize="20" FontWeight="Bold" Foreground="#D32F2F" HorizontalAlignment="Center" Margin="0,0,0,15"/>
                        <TextBlock Text="Không tìm thấy thành phần Microsoft Edge WebView2 Runtime trên máy tính này." FontSize="14" Foreground="#333333" TextWrapping="Wrap" HorizontalAlignment="Center" TextAlignment="Center" Margin="0,0,0,10"/>
                        <TextBlock Text="Đây là thư viện hệ thống bắt buộc dùng để vẽ đồ thị Desmos và mô phỏng STEM. Vui lòng thực hiện tải và cài đặt để kích hoạt tính năng." FontSize="13" Foreground="#666666" TextWrapping="Wrap" HorizontalAlignment="Center" TextAlignment="Center" Margin="0,0,0,25"/>
                        
                        <Button x:Name="btnDownload" Content="📥 Tải WebView2 Runtime (Microsoft)" Padding="20,12" Background="#1976D2" Foreground="White" FontWeight="Bold" FontSize="14" BorderThickness="0" Cursor="Hand" Click="DownloadWebView2_Click" HorizontalAlignment="Center"/>
                        
                        <TextBlock Text="Hoặc liên hệ Kỹ thuật viên phòng máy trạm để được cài đặt offline." FontSize="12" Foreground="#999999" HorizontalAlignment="Center" Margin="0,15,0,0"/>
                    </StackPanel>
                </Border>
            </Grid>
        </Grid>
        ```
    2.  Tại [GraphWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml.cs), cập nhật hàm `LoadHtml`:
        ```csharp
        private void LoadHtml(string html)
        {
            Loaded += async (_, _) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                    if (webView.CoreWebView2 != null && webView.CoreWebView2.Settings != null)
                    {
#if !DEBUG
                        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#else
                        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#endif
                    }
                    webView.CoreWebView2.NavigateToString(html);
                }
                catch (Exception ex)
                {
                    webView.Visibility = Visibility.Collapsed;
                    fallbackGrid.Visibility = Visibility.Visible;
                    Log.Error("WebView2 environment initialization failed: {Err}", ex.Message);
                }
            };
        }

        private void DownloadWebView2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://go.microsoft.com/fwlink/p/?LinkId=2124703", // Link trực tiếp tải WebView2 Bootstrapper
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to open browser for WebView2 download: {Err}", ex.Message);
            }
        }
        ```
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao không tự tải ngầm WebView2 installer về cài đặt?
    *   *Tự phản biện:* Máy tính trường học thường bị khóa quyền quản trị (Admin) và không có kết nối Internet trực tiếp. Việc tự động tải ngầm sẽ gây lỗi mạng và tốn băng thông. Hiển thị thông báo cứu hộ kèm giải pháp cài đặt offline thông qua Kỹ thuật viên là phương án sư phạm và kỹ thuật thực tế nhất.

---

## ═══ BẢNG KIỂM TRA CHO CODER & QA (CHECKSHEET) ═══

### 📋 CHECKSHEET 1: KIỂM THỬ ĐỊNH TUYẾN DỮ LIỆU (`--data-dir`)
- [ ] **Bước 1 (Coder):** Thêm static property `DataDirOverride` vào [AppPaths.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Services/AppPaths.cs).
- [ ] **Bước 2 (Coder):** Cập nhật thuộc tính `RootDir` và `DocumentsDir` để nhận giá trị `DataDirOverride` nếu có.
- [ ] **Bước 3 (Coder):** Cấu hình dòng lệnh trong `OnStartup` của [App.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/App.xaml.cs) để parse tham số `--data-dir=`.
- [ ] **Bước 4 (QA):** Khởi chạy ứng dụng với tham số: `QASmartClass.exe --student --data-dir="D:\TestQASmartData"`.
- [ ] **Bước 5 (QA):** Kiểm tra xem thư mục `D:\TestQASmartData` có tự động được sinh ra cùng các thư mục con: `Settings`, `Logs`, `Documents` hay không.
- [ ] **Bước 6 (QA):** Đăng nhập thử học sinh, nộp bài, xác nhận tệp tin nháp `.tmp` và CSDL SQLite `smartclass.db` được lưu chính xác trong phân vùng `D:\TestQASmartData` thay vì AppData cũ.
- [ ] **Bước 7 (QA):** Khởi chạy lại ứng dụng không truyền tham số, đảm bảo hệ thống tự động fallback về thư mục AppData cục bộ an toàn.

### 📋 CHECKSHEET 2: KIỂM THỬ KHÁNG LỖI WEBVIEW2 RUNTIME
- [ ] **Bước 1 (Coder):** Cập nhật cấu hình Grid bao bọc WebView2 và thêm `fallbackGrid` vào [GraphWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml).
- [ ] **Bước 2 (Coder):** Bọc khối `try-catch` khi gọi `EnsureCoreWebView2Async` trong [GraphWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml.cs).
- [ ] **Bước 3 (Coder):** Định nghĩa hàm xử lý sự kiện click nút bấm `DownloadWebView2_Click`.
- [ ] **Bước 4 (QA):** Giả lập lỗi thiếu WebView2 bằng cách sửa tạm thời mã nguồn để ném ra ngoại lệ `new Exception("Simulated WebView2 error")` ngay dòng `EnsureCoreWebView2Async()`.
- [ ] **Bước 5 (QA):** Mở Máy tính khoa học -> Nhập hàm và bấm vẽ đồ thị.
- [ ] **Bước 6 (QA):** Xác nhận cửa sổ đồ thị mở ra bình thường, WebView2 bị ẩn đi và hiển thị giao diện cứu hộ màu trắng đỏ chuyên nghiệp.
- [ ] **Bước 7 (QA):** Bấm thử nút "Tải WebView2 Runtime", xác nhận trình duyệt web của HĐH mở ra đúng link tải của Microsoft.
- [ ] **Bước 8 (QA):** Khôi phục mã nguồn, kiểm tra chạy thử trên máy đã cài WebView2 Runtime, xác nhận đồ thị Desmos hiển thị bình thường không bị ảnh hưởng.
