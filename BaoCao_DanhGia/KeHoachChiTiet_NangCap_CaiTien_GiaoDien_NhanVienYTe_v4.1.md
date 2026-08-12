# Kế hoạch Nâng cấp & Cải tiến Phân hệ Nhân viên Y tế (Health Room Module)
**Bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật QA SmartClass v4.1**  
*Ngày lập kế hoạch: 10 tháng 07 năm 2026*  

---

## ═══ MỤC TIÊU ═══
Hoàn thiện, nâng cấp mỹ thuật và sửa đổi toàn bộ các lỗi logic hệ thống, lỗi hiển thị tiếng Anh (localization leak) của phân hệ **Nhân viên Y tế (HealthRoom)**. Thiết lập các chỉ dẫn sư phạm từng bước trực quan, giải quyết triệt để lỗi thiết kế popup động bằng code-behind, hoàn thiện tính năng xuất báo cáo PDF và vẽ lại biểu đồ tăng trưởng chuẩn WHO. Đảm bảo toàn bộ mã nguồn được tối ưu hóa, an toàn dữ liệu và dễ dàng kiểm thử bởi đội ngũ QA/Tester.

---

## ═══ PHẦN I: TỰ PHẢN BIỆN CHUYÊN SÂU & QUYẾT ĐỊNH THIẾT KẾ ═══

Để tránh các sai sót trong quá trình hiện thực hóa và đảm bảo tính đồng bộ cao nhất, Trưởng ban thiết kế dự án cùng các chuyên gia hệ thống đã tiến hành phản biện các giải pháp kỹ thuật cốt lõi:

### 1. Phản biện về tính toán BMI Trẻ em khi thiếu Ngày sinh
*   **Vấn đề:** Thực thể `Student` trong DB không lưu ngày sinh đầy đủ. Logic cũ ước lượng tuổi `grade + 5` thông qua tên lớp là thiếu chính xác (học sinh đi học muộn, lớp mầm non, lớp tình thương không có số).
*   **Giải pháp phản biện 1:** Thêm cột `DateOfBirth` vào bảng `Student`.
    *   *Phản biện ngược:* Việc di chuyển CSDL (Database Migration) lớn đối với bảng `Student` đang liên kết với hơn 20 phân hệ khác (Android Client, Canteen POS, Sổ liên lạc,...) sẽ làm tăng rủi ro xung đột dữ liệu toàn hệ thống và phá vỡ tính tương thích ngược.
*   **Giải pháp phản biện 2 (Chọn lựa tối ưu):** Bổ sung trường `ExactAge` (kiểu số nguyên nullable) trực tiếp vào bảng `HealthRecord`. 
    *   *Đánh giá:* Khi khám sức khỏe, hệ thống tự động điền tuổi ước lượng làm gợi ý. Nhân viên y tế có thể chỉnh sửa tuổi chính xác trên form nhập liệu nếu học sinh đó đi học muộn/sớm. Giá trị này được lưu vào `HealthRecord.Notes` hoặc trường `ExactAge` mới. Quyết định này giúp không thay đổi schema của `Student` mà vẫn thu được dữ liệu tuổi lâm sàng chuẩn xác để so khớp phân vị BMI WHO.

### 2. Phản biện về việc Lưu trữ Ngôn ngữ trong CSDL
*   **Vấn đề:** DB lưu trạng thái sự cố y tế là tiếng Anh (`Injury`, `Active`, `Home`, `Pass`,...) dẫn đến việc binding hiển thị thô ra giao diện vi phạm quy chuẩn Việt hóa.
*   **Giải pháp phản biện 1:** Đổi toàn bộ các giá trị ghi vào DB thành tiếng Việt (VD: lưu "Chấn thương", "Cách ly tại nhà").
    *   *Phản biện ngược:* Lưu trữ tiếng Việt có dấu trong các trường trạng thái CSDL làm giảm hiệu năng tìm kiếm (Index), gây rủi ro về mặt tương thích bảng mã Unicode khi truy vấn báo cáo đa hệ thống và phá vỡ cấu trúc dữ liệu chuẩn của API.
*   **Giải pháp phản biện 2 (Chọn lựa tối ưu):** Giữ nguyên các từ khóa chuẩn hóa tiếng Anh trong CSDL đóng vai trò là "Technical Tags" (nhãn kỹ thuật), nhưng bắt buộc viết các lớp `IValueConverter` (bộ chuyển đổi giá trị WPF) ở tầng hiển thị để dịch tự động sang tiếng Việt trước khi render lên UI. Cách này vừa đảm bảo dữ liệu chuẩn hóa, vừa đáp ứng yêu cầu Việt hóa 100% giao diện người dùng.

### 3. Phản biện về thiết kế Biểu đồ Tăng trưởng (Growth Chart)
*   **Vấn đề:** Logic cũ tự vẽ các cột Border động trong code-behind không có trục tọa độ, không có lưới tọa độ và thiếu đường phân vị WHO.
*   **Giải pháp phản biện 1:** Tích hợp thư viện đồ thị bên thứ ba như OxyPlot hay LiveCharts.
    *   *Phản biện ngược:* Thêm thư viện ngoài làm tăng kích thước bộ cài (Installer size), tăng rủi ro về mặt cấp phép phần mềm (Licensing) và xung đột thư viện với nền tảng Wpf .NET 8.0-windows hiện tại của trường học.
*   **Giải pháp phản biện 2 (Chọn lựa tối ưu):** Thiết kế biểu đồ tăng trưởng bằng một UserControl tùy biến dựa trên `Canvas` và `Grid` chuẩn của WPF. Sử dụng mã vẽ vector (DrawingVisual hoặc Shapes) để dựng:
    *   Trục đứng Y (chiều cao) hiển thị các mốc từ 80cm - 200cm.
    *   Trục ngang X (thời gian khám).
    *   Vẽ 3 đường đứt nét biểu thị đường phân vị WHO (p5 - Suy dinh dưỡng, p50 - Trung bình, p95 - Nguy cơ béo phì/Vượt chuẩn) dựa trên tuổi của học sinh để nhân viên y tế so sánh trực quan.

---

## ═══ PHẦN II: CHI TIẾT KẾ HOẠCH NÂNG CẤP THEO TỪNG PHÂN HỆ ═══

### 1. Phân hệ Cấp cứu & Sơ cứu Khẩn cấp (`EmergencyReportView`)
*   **Mô tả yêu cầu:** Việt hóa hoàn toàn trạng thái và sự cố. Thay thế bảng màu nóng kích thích lo lắng sang bảng màu dịu y tế. Tích hợp chỉ dẫn sơ cứu y tế từng bước.
*   **Dữ liệu đầu vào (Input):**
    *   Học sinh được chọn (`StudentId`, `FullName`).
    *   Loại sự cố (`IncidentType`: "Injury", "Illness", "Poisoning").
    *   Mô tả chi tiết và biện pháp sơ cứu (`Description`, `FirstAidApplied`).
*   **Dữ liệu đầu ra (Output):**
    *   Tạo bản ghi `EmergencyLog` trong DB với các trường chuẩn hóa.
    *   Gửi tin nhắn đẩy (Push Notification) đến phụ huynh thông qua `MobileApiService` được mã hóa nội dung.
    *   Render danh sách y tế hiển thị tiếng Việt hoàn toàn.
*   **Phương pháp thực hiện:**
    *   Thay đổi mã màu form ở [EmergencyReportView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/EmergencyReportView.xaml): dùng mã màu Teal `#E6F4F1` (nền card) và viền `#B2DFDB` thay cho đỏ hồng.
    *   Đăng ký bộ Converter `EmergencyStatusConverter` và `EmergencyTypeConverter` trong tài nguyên XAML để chuyển các giá trị `RequiresAttention`, `Injury`, `Illness` thành tiếng Việt trên ListView.
    *   Thêm một khối `Border` hướng dẫn quy trình sơ cứu nhanh 3 bước của Bộ Y tế dạng Expander ở chân trang.

### 2. Phân hệ Giám sát Dịch bệnh Học đường (`EpidemicMonitorView`)
*   **Mô tả yêu cầu:** Sửa lỗi binding `AlertVisibility`, bổ sung nhánh logic ẩn cảnh báo dịch, Việt hóa các trường Cách ly và Trạng thái, thiết kế lại biểu đồ thống kê.
*   **Dữ liệu đầu vào (Input):** 
    *   Danh sách `EpidemicCases` truy vấn từ database.
*   **Dữ liệu đầu ra (Output):** 
    *   Trạng thái ẩn/hiện hợp lệ của Hộp cảnh báo (`Alert Box`).
    *   Nội dung cảnh báo hiển thị số ca bệnh SXH thực tế trong 7 ngày qua.
    *   Bảng dữ liệu hiển thị tiếng Việt: "Tại nhà", "Tại bệnh viện", "Đang điều trị", "Đã khỏi bệnh".
*   **Phương pháp thực hiện:**
    *   Khai báo `AlertVisibility` là một `DependencyProperty` hoặc kích hoạt `INotifyPropertyChanged` trong code-behind [EpidemicMonitorView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/EpidemicMonitorView.xaml.cs). Gán `DataContext = this` tại hàm khởi tạo.
    *   Cập nhật logic dập dịch:
        ```csharp
        if (dengueCases >= 3) {
            TxtAlertMessage.Text = $"Phát hiện {dengueCases} ca Sốt xuất huyết...";
            AlertVisibility = Visibility.Visible;
        } else {
            AlertVisibility = Visibility.Collapsed;
        }
        ```
    *   Viết `EpidemicStatusConverter` và `IsolationLocationConverter` để định dạng hiển thị tiếng Việt trên DataGrid.

### 3. Phân hệ Kiểm thực 3 Bước Canteen (`FoodSafetyView`)
*   **Mô tả yêu cầu:** Việt hóa kết quả kiểm thực ("Pass"/"Fail" thành "Đạt"/"Không đạt"). Tích hợp quy trình kiểm tra 3 bước của Bộ Y tế vào form thêm mới nhật ký.
*   **Dữ liệu đầu vào (Input):**
    *   Thực đơn ăn bán trú, Ngày kiểm thực, Người kiểm tra.
    *   Kết quả 3 bước checklist (Bước 1: Nhập nguyên liệu; Bước 2: Chế biến; Bước 3: Lưu mẫu 24h).
*   **Dữ liệu đầu ra (Output):**
    *   Bản ghi `FoodSafetyRecord` được lưu trữ.
    *   Tự động gửi email/tin nhắn đẩy cảnh báo khẩn cấp đến BGH nếu kết quả là "Fail" (Không đạt).
*   **Phương pháp thực hiện:**
    *   Cập nhật `FoodSafetyResultConverter` hiển thị kết quả kiểm thực dạng text màu xanh lá ("Đạt") hoặc đỏ ("Không đạt").
    *   Trong popup thêm nhật ký, bổ sung 3 CheckBox tương ứng với quy trình kiểm thực 3 bước của Bộ Y tế Việt Nam. Chỉ cho phép lưu là "Pass" nếu cả 3 CheckBox này được đánh dấu đạt yêu cầu.
    *   Nếu lưu kết quả là "Fail", gọi `NotificationService` gửi cảnh báo khẩn cấp tới BGH và Trưởng canteen.

### 4. Phân hệ Quản lý Kho thuốc & Vật tư Y tế (`MedicalInventoryView`)
*   **Mô tả yêu cầu:** Xây dựng cơ chế gộp kho thông minh khi trùng tên và hạn sử dụng. Cấu hình ngưỡng tồn an toàn cho từng vật tư thay vì hardcode số 10.
*   **Dữ liệu đầu vào (Input):**
    *   Tên vật tư, danh mục, số lượng nhập, đơn vị tính, hạn sử dụng.
*   **Dữ liệu đầu ra (Output):**
    *   Số lượng thuốc được cập nhật cộng dồn nếu trùng lặp, hoặc tạo bản ghi lô mới nếu khác hạn dùng.
    *   Cảnh báo đỏ hiển thị đúng theo định mức tồn kho tối thiểu cấu hình động.
*   **Phương pháp thực hiện:**
    *   Cập nhật [MedicalInventoryView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/MedicalInventoryView.xaml.cs) trong sự kiện thêm thuốc:
        ```csharp
        var existing = _db.MedicalSupplies.FirstOrDefault(s => s.Name.ToLower() == newSupply.Name.ToLower() && s.ExpiryDate.Date == newSupply.ExpiryDate.Date);
        if (existing != null) {
            existing.Quantity += newSupply.Quantity;
        } else {
            _db.MedicalSupplies.Add(newSupply);
        }
        _db.SaveChanges();
        ```
    *   Thêm cột `MinAlertQty` (kiểu int, mặc định là 10) vào bảng `MedicalSupplies` trong CSDL thông qua migration hoặc cập nhật schema DbContext.
    *   Cập nhật `QuantityAlertColorConverter` nhận tham số truyền vào là thực thể `MedicalSupply` để so sánh cột `Quantity` với `MinAlertQty` động của loại vật tư đó.

### 5. Phân hệ Hồ sơ Sức khỏe Học sinh (`HealthRecordView`)
*   **Mô tả yêu cầu:** Xây dựng tệp XAML riêng cho popup nhập khám mới sức khỏe để đồng bộ thiết kế cao cấp. Hoàn thiện tính năng xuất PDF. Nâng cấp biểu đồ tăng trưởng có lưới tọa độ và đường phân vị WHO. Bổ sung Banner cảnh báo dị ứng nổi bật.
*   **Dữ liệu đầu vào (Input):**
    *   Các số liệu đo đạc sức khỏe: Chiều cao, Cân nặng, Thị lực mắt trái, Mắt phải, Tiền sử bệnh.
*   **Dữ liệu đầu ra (Output):**
    *   Tệp báo cáo PDF hồ sơ sức khỏe được xuất ra ổ đĩa local.
    *   Biểu đồ tăng trưởng vẽ chuẩn xác các cột dữ liệu kèm các đường cong WHO.
*   **Phương pháp thực hiện:**
    *   Tạo file mới [HealthRecordInputDialog.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/HealthRecordInputDialog.xaml) chứa layout Grid hiện đại, áp dụng bộ tài nguyên style nút, ô nhập liệu của hệ thống.
    *   Gọi `PdfExportService` để xuất dữ liệu khám sức khỏe học sinh ra tệp PDF chất lượng cao, định dạng tài liệu đẹp mắt.
    *   Vẽ lưới tọa độ trên Canvas biểu đồ, sử dụng tọa độ vẽ các đường cong chuẩn của WHO (phân vị p5, p50, p95) tương ứng với độ tuổi học sinh từ 6 - 18 tuổi.
    *   Bổ sung một khối thông báo dị ứng `Border` có màu nền đỏ nhấp nháy (`BlinkAnimation`) ở đầu khung chi tiết để cảnh báo tức thời tiền sử dị ứng nguy hại của học sinh được chọn.

---

## ═══ ĐỀ XUẤT THAY ĐỔI MÃ NGUỒN (PROPOSED CHANGES) ═══

### 1. Phân hệ Giao diện & Popups (UI Components)

#### [NEW] [HealthRecordInputDialog.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/HealthRecordInputDialog.xaml)
*   Định nghĩa cửa sổ nhập liệu chuẩn hóa. Chứa các trường nhập: Học sinh (ComboBox lọc theo tên lớp), Chiều cao, Cân nặng, Thị lực 2 mắt, Tuổi thực tế (ExactAge), Tiền sử dị ứng, Bệnh mãn tính.
*   Áp dụng các style tài nguyên của QA SmartClass: `WindowStyle`, `TextBoxStyle`, `PrimaryButtonStyle`.

#### [NEW] [HealthRecordInputDialog.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/HealthRecordInputDialog.xaml.cs)
*   Xử lý logic xác thực đầu vào (Validate): Chiều cao phải từ 50-250cm, cân nặng 5-150kg, thị lực 0.0-10.0.
*   Lưu kết quả khám mới vào DB và cập nhật lại grid hồ sơ sức khỏe.

#### [MODIFY] [HealthRecordView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/HealthRecordView.xaml)
*   Bổ sung Banner cảnh báo dị ứng (`Border` nhấp nháy đỏ) nằm phía trên khung chi tiết sức khỏe học sinh.
*   Bố trí lại Canvas vẽ biểu đồ tăng trưởng chiều cao để có đủ không gian hiển thị các trục và đường chỉ số chuẩn.

#### [MODIFY] [HealthRecordView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Views/HealthRecordView.xaml.cs)
*   Thay thế hàm `BtnAdd_Click` cũ (khởi tạo cửa sổ động) bằng việc mở hộp thoại chuẩn:
    ```csharp
    var dlg = new HealthRecordInputDialog(_db);
    if (dlg.ShowDialog() == true) { LoadRecords(); }
    ```
*   Nâng cấp hàm vẽ biểu đồ tăng trưởng trên Canvas: vẽ các đường phân vị p5, p50, p95 của WHO làm hình nền đồ thị, sau đó vẽ các cột chiều cao của học sinh chồng lên để dễ dàng đối chiếu.
*   Hiện thực hóa phương thức xuất báo cáo PDF (`BtnExport_Click`) bằng cách tích hợp trực tiếp với dịch vụ tạo tài liệu PDF y tế học sinh.

### 2. Phân hệ Dịch vụ & Converters (Services & Localization)

#### [NEW] [MedicalValueConverters.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/HealthRoom/Converters/MedicalValueConverters.cs)
*   Định nghĩa các bộ chuyển đổi giá trị WPF đáp ứng Việt hóa 100%:
    *   `IncidentTypeConverter`: Injury -> "Tai nạn / Chấn thương", Illness -> "Ốm / Mệt đột xuất", Poisoning -> "Nghi ngờ ngộ độc".
    *   `EmergencyStatusConverter`: RequiresAttention -> "Chờ xử lý", Resolved -> "Đã sơ cứu xong".
    *   `EpidemicStatusConverter`: Active -> "Đang điều trị", Recovered -> "Đã khỏi bệnh".
    *   `IsolationLocationConverter`: Home -> "Cách ly tại nhà", Hospital -> "Cách ly tại bệnh viện".
    *   `FoodSafetyResultConverter`: Pass -> "Đạt tiêu chuẩn", Fail -> "Không đạt tiêu chuẩn".

#### [MODIFY] [AppDbContext.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Data/AppDbContext.cs)
*   Thêm trường `MinAlertQty` (kiểu `int`, mặc định `10`) vào thực thể `MedicalSupply` để hỗ trợ cấu hình động ngưỡng tồn kho an toàn cho từng loại vật tư.
*   Thêm trường `ExactAge` (kiểu `int?`) vào thực thể `HealthRecord` để lưu tuổi chính xác của học sinh khi khám.

---

## ═══ BỘ CHECKSHEET KIỂM THỬ TOÀN DIỆN (COMPREHENSIVE TEST CASES) ═══

Coder và Tester bắt buộc phải chạy qua toàn bộ danh sách kiểm tra sau để nghiệm thu phân hệ y tế nâng cấp:

### 1. Kiểm thử Việt hóa & Hiển thị (Localization & UI Checks)
*   `[ ]` **TC_MED_LANG_01 (Việt hóa Cấp cứu):** Mở màn hình Cấp cứu. Nhập và lưu một sự cố mới. Xác nhận trên ListView hiển thị cột Loại sự cố là "Tai nạn / Chấn thương" (không phải "Injury") và cột Trạng thái là "Chờ xử lý" (không phải "RequiresAttention").
*   `[ ]` **TC_MED_LANG_02 (Việt hóa Dịch bệnh):** Truy vấn danh sách ca dịch bệnh. Xác nhận cột "Cách ly tại" hiển thị "Cách ly tại nhà" hoặc "Cách ly tại bệnh viện" (không phải "Home"/"Hospital").
*   `[ ]` **TC_MED_LANG_03 (Việt hóa Kiểm thực):** Xác nhận cột kết quả của danh sách kiểm thực thức ăn hiển thị là "Đạt tiêu chuẩn" (màu xanh lá) hoặc "Không đạt tiêu chuẩn" (màu đỏ in đậm).

### 2. Kiểm thử Logic Nghiệp vụ & Cơ sở Dữ liệu (Logic & Database Checks)
*   `[ ]` **TC_MED_LOGIC_01 (Sửa lỗi Binding cảnh báo dịch):** Khởi động trang giám sát dịch bệnh. Thêm 3 ca nhiễm Sốt xuất huyết trong 7 ngày qua. Xác nhận Alert Box tự động hiển thị với đúng thông báo dịch màu đỏ sư phạm.
*   `[ ]` **TC_MED_LOGIC_02 (Dập dịch thành công):** Giảm số ca nhiễm Sốt xuất huyết trong tuần xuống dưới 3 ca (bằng cách sửa ngày phát bệnh lùi về trước 10 ngày hoặc chuyển trạng thái sang "Recovered"). Xác nhận Alert Box tự động ẩn đi (`Visibility.Collapsed`).
*   `[ ]` **TC_MED_LOGIC_03 (Gộp kho thuốc trùng lặp):** Nhập thêm 20 viên "Paracetamol 500mg" có cùng hạn sử dụng với lô đã tồn tại trong kho dược. Xác nhận CSDL chỉ cập nhật tăng số lượng của dòng cũ thêm 20 viên, không tạo thêm dòng mới.
*   `[ ]` **TC_MED_LOGIC_04 (Ngưỡng cảnh báo tồn động):** Đặt ngưỡng tối thiểu của máy đo huyết áp là `2` và số lượng tồn kho hiện tại là `3` => Xác nhận số lượng hiển thị màu đen bình thường. Giảm số lượng xuống `1` => Xác nhận số lượng lập tức chuyển sang màu đỏ in đậm báo động.
*   `[ ]` **TC_MED_LOGIC_05 (Tính toán BMI học sinh mầm non/lớp tình thương):** Nhập kết quả khám cho học sinh thuộc lớp mang tên `"Mầm Non Lớn"` (không có số lớp). Xác nhận hệ thống gợi ý nhập tuổi chính xác thay vì mặc định tính tuổi 15 (grade 10), đảm bảo kết quả phân loại BMI chuẩn xác.

### 3. Kiểm thử Trải nghiệm Người dùng & Mỹ thuật (UI/UX & Pedagogical Checks)
*   `[ ]` **TC_MED_UX_01 (Đồng bộ Popup nhập liệu):** Bấm nút "Khám mới" ở màn hình Hồ sơ sức khỏe. Xác nhận cửa sổ nhập liệu hiển thị với thiết kế bo góc, font chữ Segoe UI đồng bộ, màu nền xanh ngọc nhẹ dịu mắt, không sử dụng cửa sổ mặc định của Windows.
*   `[ ]` **TC_MED_UX_02 (Đồ thị tăng trưởng chiều cao):** Mở hồ sơ sức khỏe một học sinh. Xác nhận Canvas biểu đồ hiển thị đầy đủ vạch chia chiều cao trục đứng, thời gian trục ngang, có đường lưới mờ và hiển thị 3 đường cong WHO để tham chiếu trực quan.
*   `[ ]` **TC_MED_UX_03 (Cảnh báo dị ứng nổi bật):** Chọn một học sinh có ghi chú dị ứng nặng (ví dụ: "Dị ứng Penicillin"). Xác nhận trên đầu bảng chi tiết xuất hiện Banner đỏ nhấp nháy nổi bật cảnh báo dị ứng y khoa.
*   `[ ]` **TC_MED_UX_04 (Tính năng Xuất PDF thực tế):** Bấm nút "Xuất báo cáo PDF" hồ sơ sức khỏe học sinh. Xác nhận tệp PDF được lưu thành công, mở ra hiển thị đầy đủ logo trường, thông tin học sinh, biểu đồ tăng trưởng và chữ ký nhân viên y tế dạng tài liệu in ấn chuẩn mực.

---

## ═══ KẾ HOẠCH XÁC MINH (VERIFICATION PLAN) ═══

### Kiểm thử tự động (Automated Unit Tests)
*   Viết 4 phương thức unit test trong project `QASmartClass.Tests` để tự động kiểm thử:
    1.  Hàm chuyển đổi ngôn ngữ của các bộ Converter y tế.
    2.  Thuật toán tính toán phân vị BMI trẻ em theo chuẩn WHO dựa trên tuổi thực tế và giới tính.
    3.  Logic gộp số lượng vật tư y tế khi trùng tên và hạn sử dụng ở tầng service.
    4.  Logic kiểm tra kích thước file xuất PDF báo cáo sức khỏe hợp lệ lớn hơn 0 bytes.
*   Lệnh thực thi kiểm thử tự động: `dotnet test QASmartClass.Tests --filter Category=Medical`

### Kiểm thử thủ công (Manual Verification)
*   Mời nhân viên y tế học đường thực tế sử dụng thử nghiệm nhập liệu trên máy tính cảm ứng QA SmartTouch để kiểm tra độ nhạy, tính dễ dùng của chỉ dẫn sơ cứu từng bước, tính trực quan của biểu đồ và banner cảnh báo dị ứng trước khi ký duyệt nghiệm thu đưa vào vận hành thực tế.
