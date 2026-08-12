# BÁO CÁO ĐÁNH GIÁ CHUYÊN SÂU GIAO DIỆN HIỆU TRƯỞNG & BAN GIÁM HIỆU
**Đơn vị thực hiện:** Hội đồng Chuyên gia Đa ngành QA SmartSchool (17 Thành viên)  
**Phiên bản đánh giá:** Phân hệ Leadership - QA SmartClass v4.1  
**Ngày lập báo cáo:** 29/06/2026  

---

## ═══ TỔNG QUAN ĐÁNH GIÁ ═══

Để đáp ứng tối đa tính sư phạm, tính thân thiện người dùng và hiệu năng kỹ thuật theo bộ quy chuẩn **QA SmartClass v4.1**, Hội đồng Chuyên gia đa ngành gồm 17 thành viên đã tiến hành kiểm định, rà soát toàn diện phân hệ quản lý của **Hiệu trưởng & Ban Giám Hiệu (BGH)** (thư mục [Leadership](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership)).

Kết quả rà soát cho thấy giao diện Hiệu trưởng đã thiết lập được khía cạnh chức năng cơ bản tốt (quản lý thi đua, dự giờ, phê duyệt giáo án, xem thống kê). Tuy nhiên, hệ thống vẫn tồn tại nhiều lỗi kỹ thuật nghiêm trọng (lỗi font chữ tiếng Việt, lỗi logic hiển thị, lỗi đặt tên điều khiển XAML), lỗi sư phạm (thiếu chỉ dẫn sử dụng, ngôn ngữ tiếng Anh lẫn lộn) và các vấn đề về khả năng đáp ứng giao diện trên thiết bị trường học thực tế.

---

## ═══ PHẦN 1: ĐÁNH GIÁ CHI TIẾT THEO 7 YÊU CẦU QUY CHUẨN v4.1 ═══

### 1. Font chữ tiếng Việt (Typography & Encoding)
*   **Vấn đề nghiêm trọng:** Rất nhiều tệp tin logic `.cs` và `.xaml` bị lỗi mã hóa font chữ diacritics (lỗi ký tự unicode). Các từ tiếng Việt có dấu biến thành ký tự lạ hoặc dấu hỏi chấm `?` (ví dụ: `gi?ng`, `s?a`, `nh?p`, `Tr? v?`, `ḷng`, `l?i:`). Điều này cực kỳ phản sư phạm, làm giao diện hiển thị các hộp thoại thông báo bị lỗi chính tả nghiêm trọng.
*   **Mũi tên xu hướng chỉ số:** Trong biểu đồ so sánh xu hướng KPI, các ký tự chỉ báo hướng tăng/giảm (`▲` / `▼` / `―`) bị lỗi font mã hóa và hiển thị thành các dấu hỏi chấm (`?` và `¦`) gây khó hiểu cho người quản lý.
*   **Giải pháp:** Yêu cầu chuyển đổi mã hóa toàn bộ tệp nguồn sang **UTF-8 with BOM**, đồng thời chuẩn hóa font chữ hiển thị đồng bộ là **Segoe UI** cho giao diện chung và **Times New Roman** cho các nội dung học thuật/công thức.

### 2. Bố cục & Tính đáp ứng (Layout & Responsiveness)
*   **Thiếu ScrollViewer chống rách hình/mất dữ liệu:** Hầu hết các trang thiết kế như [AppUsageAnalyticsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/AppUsageAnalyticsView.xaml), [BackupRestoreView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/BackupRestoreView.xaml), [ClassObservationView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/ClassObservationView.xaml), [SystemSettingsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/SystemSettingsView.xaml) đều được đặt trong Grid tĩnh và **không có ScrollViewer bao ngoài**. Khi chạy trên màn hình độ phân giải thấp (1024x768 của máy chiếu cũ, hoặc 1366x768 của laptop giáo viên), các nút bấm bên dưới sẽ bị che khuất hoàn toàn mà không thể cuộn xuống, dẫn đến mất thông tin và không thể thao tác.
*   **Cứng hóa kích thước (Hardcoded Size):** Một số thẻ Border, Card trong `EmulationBoardPage.xaml` và `LeadershipDashboardWindow.xaml` có thuộc tính `Width` và `Height` cố định (ví dụ: `Width="250" Height="120"`). Điều này làm hỏng tính năng co giãn tự động (Responsive Layout) khi thay đổi kích thước cửa sổ.
*   **Giải pháp:** Bao bọc nội dung Page bằng `<ScrollViewer VerticalScrollBarVisibility="Auto">` và đổi kích thước cứng sang tỷ lệ động hoặc dùng `MinHeight`/`MinWidth` phối hợp `WrapPanel`.

### 3. Màu sắc & Thẩm mỹ Sư phạm (Color Palette)
*   **Tích cực:** Giao diện sử dụng tông màu chủ đạo dịu mắt (nền `#F8FAFC`, bề mặt trắng `White`, xanh dương thương hiệu `#3B82F6` kết hợp xanh lá `#10B981` cho thi đua), đảm bảo tính chuyên nghiệp.
*   **Hạn chế:** Các nút bấm cảnh báo nguy cơ và lỗi hệ thống (như nút Khôi phục hệ thống trong `BackupRestoreView.xaml`) sử dụng màu đỏ chói `#F44336` quá gay gắt, dễ tạo tâm lý căng thẳng cho giáo viên. Cần điều chỉnh sang sắc đỏ ấm dịu hơn (như `#EF4444` hoặc `#DC2626`) theo chuẩn palette v4.1.

### 4. Logic chức năng chương trình (Functional Logic Bugs)
*   **Lỗi tính toán Điểm danh (0.0% Attendance Bug):** Trong [LeadershipDashboardViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/ViewModels/LeadershipDashboardViewModel.cs#L62), code kiểm tra trạng thái điểm danh học sinh viết:
    ```csharp
    var presentCount = await db.AttendanceRecords.CountAsync(a => a.Date == today && a.Status == "present");
    ```
    Trong khi đó CSDL thực tế và view [PrincipalDashboardPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/PrincipalDashboardPage.xaml.cs#L40) lại lưu và so sánh trạng thái dưới dạng `"Present"` (chữ P hoa) hoặc `"Có mặt"` (tiếng Việt). Lỗi so khớp chuỗi viết thường `"present"` này khiến tỷ lệ chuyên cần trên Dashboard chính luôn bị tính ra bằng **0.0%** hoặc **N/A**, làm mất tính chính xác của hệ thống báo cáo.
*   **Lỗi đặt tên sai điều khiển XAML (Crash/Null Settings Bug):** Trong [SystemSettingsView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/SystemSettingsView.xaml.cs#L37-L43), code-behind gọi:
    ```csharp
    FindName("txtIPAddress") // Để gán IP_Address
    FindName("chkAutoBackup") // Để gán trạng thái Auto Backup
    ```
    Tuy nhiên, trong tệp thiết kế [SystemSettingsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/SystemSettingsView.xaml), TextBox nhập IP lại được đặt tên là `txtServerIp`, và hoàn toàn **không có checkbox nào tên là chkAutoBackup**. Lỗi không khớp tên này khiến tính năng lưu/tải cấu hình IP và sao lưu tự động của trạm Hiệu trưởng hoàn toàn bị vô hiệu hóa (hoặc trả về Null).
*   **Lỗi liên kết trục đồ thị OxyPlot (Mismatched Axes Chart Bug):** Trong `KpiDashboardPage.xaml.cs`, đồ thị phân phối học lực và chuyên cần sử dụng `BarSeries` (vẽ thanh ngang) nhưng trục thể hiện danh mục lại cấu hình ở vị trí dưới đáy `AxisPosition.Bottom`, còn trục giá trị lại đặt bên trái `AxisPosition.Left`. Sự chồng chéo trục này khiến OxyPlot kết xuất đồ thị bị rỗng, méo mó hoặc treo giao diện. Đúng ra phải sử dụng `ColumnSeries` (thanh cột đứng) để phù hợp với trục danh mục đặt ở đáy.
*   **Nuốt ngoại lệ CSDL (Empty Catch Blocks):** Hàm `LoadSettingsFromDb` trong `SystemSettingsView.xaml.cs` chứa khối `catch { }` trống rỗng. Việc không log lỗi hoặc thông báo khiến các sự cố truy cập database SQLite bị che giấu hoàn toàn, gây khó khăn cho việc gỡ lỗi.

### 5. Hình ảnh minh họa & Đồ thị trực quan (Data Visualization)
*   **Lỗi dùng nội dung giả (Placeholders):** Trong [AppUsageAnalyticsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/AppUsageAnalyticsView.xaml#L125), phần đồ thị tương tác và tỷ lệ hệ điều hành bị thiết kế dạng tĩnh giả lập bằng TextBlock chữ:
    ```xml
    <TextBlock Text="[Biểu đồ Line Chart: Số lượt truy cập App mỗi ngày]" .../>
    <TextBlock Text="[Biểu đồ Pie Chart: 65% Android - 35% iOS]" .../>
    ```
    Điều này vi phạm nghiêm trọng quy chuẩn thiết kế sư phạm v4.1 (Yêu cầu hiển thị biểu đồ thật trực quan, sinh động để Hiệu trưởng đánh giá khách quan tiến trình tương tác của nhà trường).

### 6. Chỉ dẫn sử dụng từng bước (Step-by-step User Guidance)
*   **Thiếu hướng dẫn ngăn ngừa sai sót:** Các tính năng cực kỳ quan trọng và nhạy cảm như **Khôi phục hệ thống (Restore)** (ghi đè cơ sở dữ liệu), **Xóa dữ liệu cũ (Data Retention)**, **Phê duyệt hàng đợi giáo án (Approval Queue)**, **Tạo lịch dự giờ** đều trống rỗng hướng dẫn. Người dùng (Hiệu trưởng thường lớn tuổi, ít rành công nghệ) rất dễ thao tác sai dẫn đến mất mát dữ liệu hoặc đánh giá sai lệch.
*   **Giải pháp:** Bổ sung Tooltip, biểu tượng trợ giúp `❔`, hoặc các khối thông tin hướng dẫn từng bước (Step-by-step Wizard) để người dùng hiểu quy trình.

### 7. Ngôn ngữ & Việt hóa (Localization & Consistency)
*   **Tiêu đề nửa Tây nửa Ta:** Nhiều cửa sổ hiển thị tiêu đề hoàn toàn bằng tiếng Anh như: `Title="App Usage Analytics"`, `Title="System Settings"`, `Title="School Event Calendar"`, `Title="Staff Performance Tracker"`, `Title="Data Exporter"`. Cửa sổ chính hiển thị: `Title="QA SmartSchool - Executive Dashboard"`.
*   **Tiêu đề không dấu thiếu nghiêm túc:** Trang dự giờ hiển thị `Title="Du gio"`. Về mặt sư phạm, việc viết tiếng Việt không dấu thể hiện sự thiếu chỉn chu và cẩu thả của phần mềm giáo dục quốc gia.
*   **Giải pháp:** Thay đổi toàn bộ thuộc tính `Title` và các Header cột sang tiếng Việt chuẩn hóa theo đúng ngữ cảnh sư phạm.

---

## ═══ PHẦN 2: Ý KIẾN CHI TIẾT TỪ HỘI ĐỒNG 17 CHUYÊN GIA ═══

### 1. 💼 Trưởng bộ phận thiết kế dự án QA Smart School (Project Design Lead)
> "Khung sườn của phân hệ Leadership rất đầy đủ. Tuy nhiên, việc để lọt các lỗi đặt tên điều khiển (`txtIPAddress` vs `txtServerIp`) và thiếu CheckBox `chkAutoBackup` chứng tỏ khâu thiết kế chi tiết chưa được kiểm duyệt kỹ. Các lỗi này làm tê liệt hoàn toàn chức năng cấu hình của Hiệu trưởng."

### 2. 💻 Quản lý IT (IT Manager)
> "Chức năng Sao lưu & Khôi phục dữ liệu SQLite là bắt buộc phải có, nhưng việc thiếu chỉ dẫn từng bước cứu hộ và nút bấm Khôi phục màu đỏ quá chói dễ gây hoảng loạn. Hơn nữa, việc nuốt lỗi database trong các khối `catch {}` trống của phần cài đặt hệ thống là lỗi tối kỵ trong vận hành kỹ thuật."

### 3. 🔍 Chuyên gia kiểm thử phần mềm (QA Tester)
> "Tỷ lệ bao phủ kiểm thử đối với giao diện Hiệu trưởng rất thấp. Lỗi so khớp chuỗi điểm danh `"present"` viết thường là một lỗi ngớ ngẩn đáng lẽ phải bị phát hiện từ khâu Unit Test. Chúng tôi cần bổ sung ngay các testcase kiểm định dữ liệu biên cho phần nhập cổng mạng và định dạng ngày tháng."

### 4. 🎨 Chuyên gia thiết kế giao diện (UI/UX Designer)
> "Việc thiết kế thiếu `ScrollViewer` trên hầu hết các trang giao diện của Hiệu trưởng là một lỗi nghiêm trọng. Máy chiếu ở phòng hội đồng trường học thường có độ phân giải rất thấp (1024x768). Nếu không có thanh cuộn, giao diện sẽ bị cắt đứt mất các nút bấm lưu dữ liệu. Các biểu đồ placeholder dạng chữ `[Biểu đồ...]` cần được thay thế bằng biểu đồ thật sử dụng thư viện đồ họa trực quan."

### 5. ⚙️ Chuyên gia phân tích và thiết kế hệ thống (System Analyst)
> "Sự thiếu đồng bộ giữa cơ sở dữ liệu (lưu trạng thái điểm danh là 'Present'/'Có mặt') và tầng logic nghiệp vụ (so sánh 'present') chứng tỏ tài liệu đặc tả dữ liệu chưa được truyền đạt tốt đến lập trình viên. Cần chuẩn hóa lại các hằng số trạng thái trong hệ thống."

### 6. 🗄️ Chuyên gia cơ sở dữ liệu và thiết bị kết nối ngoại vi (DB & Connectivity Specialist)
> "Cấu hình cổng mạng mặc định `Port` trong hệ thống cài đặt là 29877, trong khi tài liệu v4.1 quy định cổng truyền nhận dữ liệu đồng bộ là `29879`. Hơn nữa, việc lưu trữ thông tin IP máy chủ và Port bị lỗi không lưu được từ UI do sai tên TextBox sẽ làm hỏng kết nối đồng bộ phòng máy LAN."

### 7. 🛡️ Chuyên gia về bảo mật và an ninh mạng (Security Expert)
> "Tính năng Khôi phục hệ thống (Restore) cho phép Hiệu trưởng ghi đè CSDL SQLite trực tiếp. Đây là thao tác có rủi ro bảo mật cực cao. Cần bổ sung cơ chế xác nhận mật khẩu cấp 2 (Admin password) trước khi cho phép ghi đè cơ sở dữ liệu để tránh hành vi phá hoại hoặc sơ suất vô tình."

### 8. 🏫 Nhà giáo dục (Educator)
> "Phần mềm dành cho nhà trường phải mẫu mực về mặt ngôn ngữ. Việc tiêu đề trang viết không dấu `Title="Du gio"` hay sử dụng tiếng Anh xen lẫn (`App Usage Analytics`) là không thể chấp nhận được trong môi trường sư phạm."

### 9. 🎓 Quản lý Hiệu trưởng nhà trường (School Principal)
> "Tôi mở Dashboard thấy chuyên cần ngày nào cũng báo 0% hoặc N/A làm tôi rất lo lắng, tưởng rằng giáo viên không thực hiện điểm danh, hóa ra là do lỗi phần mềm. Các chỉ số trực quan rất quan trọng với chúng tôi để đưa ra quyết định chỉ đạo kịp thời, mong sớm khắc phục."

### 10. 👥 Trưởng bộ môn của trường (Head of Department)
> "Phần lịch dự giờ tổ chuyên môn hiển thị lịch trình tốt, nhưng khi thực hiện dự giờ, phiếu dự giờ rubric đánh giá theo Công văn 5555 của Bộ GD&ĐT cần có các mô tả tiêu chí rõ ràng cho từng thang điểm 1, 2, 3, 4 để người dự giờ dễ chấm điểm, tránh chấm theo cảm tính."

### 11. 👩‍🏫 Giáo viên ưu tú với nhiều kinh nghiệm (Elite Teacher)
> "Hàng đợi phê duyệt giáo án giúp chúng tôi gửi bài giảng lên BGH duyệt rất nhanh. Nhưng phần phản hồi từ chối của BGH thỉnh thoảng bị lỗi font chữ hiển thị trên giao diện của chúng tôi, khiến chúng tôi không đọc được lý do cần sửa đổi là gì."

### 12. 👦 Học sinh (Student)
> "Thầy Hiệu trưởng chiếu bảng thi đua tuần lên màn hình lớn của trường, nhưng chữ và số bị đè lên nhau ở một số góc do khung hiển thị bị cố định kích thước, mong các thầy cô lập trình sửa lại cho đẹp hơn ạ."

### 13. 🧹 Nhân viên nhà trường - Kỹ thuật viên (IT Support Staff)
> "Tôi là người trực tiếp hướng dẫn các thầy cô BGH sử dụng. Việc không có chỉ dẫn từng bước bằng tiếng Việt làm tôi phải tốn rất nhiều thời gian hỗ trợ trực tiếp. Đặc biệt là nút 'Chạy dọn dẹp dữ liệu ngay', nếu bấm nhầm có thể làm mất sạch log lịch sử phục vụ thanh tra."

### 14. 🎮 Gamer giỏi (Pro Gamer)
> "Biểu đồ so sánh xu hướng 6 tháng gần nhất nên áp dụng hiệu ứng chuyển động mượt mà (Transition animation) thay vì render tĩnh lập tức. Việc hiển thị dấu hỏi chấm `?` ở cột biến động làm mất hết tính thẩm mỹ và độ 'nhạy' của giao diện."

### 15. 🏢 Cán bộ quản lý Phòng Giáo dục (District Admin)
> "Phần phân hệ xuất báo cáo MOET PDF/Excel cần tuân thủ tuyệt đối biểu mẫu của Bộ. Đoạn code báo cáo đang lấy cứng năm học `2025-2026`, điều này sẽ gây lỗi dữ liệu khi bước sang các năm học tiếp theo."

### 16. 🏫 Chuyên viên Sở Giáo dục (Provincial Specialist)
> "Sở đánh giá cao việc tích hợp đánh giá dự giờ theo Công văn 5555 của Bộ vào phần mềm. Nhưng việc tính điểm trung bình tiết dạy bằng cách nhân trọng số cứng trong code cần được cấu hình hóa để đề phòng Bộ thay đổi quy định trọng số trong tương lai."

### 17. 🔬 Nhà khoa học giáo dục (Educational Scientist)
> "Hiệu trưởng là người chịu nhiều áp lực quản lý. Một giao diện tốt cho Hiệu trưởng phải giảm thiểu tải nhận thức (Cognitive Load). Việc hiển thị quá nhiều bảng biểu thiếu trực quan và ngôn ngữ hỗn hợp sẽ làm tăng độ mệt mỏi tinh thần khi vận hành."

---

## ═══ PHẦN 3: BẢNG ĐỐI CHIẾU TRƯỚC VS SAU CẢI TIẾN ═══

| Phân hệ / Tiêu chí | Trạng thái Hiện tại (Lỗi v4.1) | Giải pháp Đề xuất (Sau cải tiến) | Lợi ích Sư phạm & Kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Tính toán Điểm danh** | So sánh cứng `"present"` viết thường, kết quả Dashboard luôn báo **0.0%** chuyên cần. | Sửa thành `a.Status == "Present" \|\| a.Status == "Có mặt"`. | Hiển thị chính xác tỷ lệ chuyên cần thời gian thực của toàn trường. |
| **Cài đặt Hệ thống** | Sai tên TextBox (`txtIPAddress` vs `txtServerIp`), thiếu CheckBox `chkAutoBackup`. | Đồng bộ tên TextBox trong XAML/C#, bổ sung CheckBox cấu hình tự động sao lưu. | Kích hoạt lại tính năng cấu hình IP trạm và tự động sao lưu dữ liệu. |
| **Biểu đồ Analytics** | Chứa TextBlock giả lập chữ `[Biểu đồ Line Chart...]`. | Tích hợp biểu đồ trực quan (LiveCharts/OxyPlot) hiển thị dữ liệu thực tế. | Giúp Hiệu trưởng theo dõi trực quan số liệu tương tác thực tế của phụ huynh. |
| **Đồ thị OxyPlot** | Trục danh mục dưới đáy, trục giá trị bên trái nhưng dùng `BarSeries` gây lỗi hiển thị. | Đổi sang sử dụng `ColumnSeries` cho cột đứng, hoặc chuyển trục danh mục sang bên trái. | Biểu đồ so sánh học kỳ hiển thị chính xác, sắc nét, không bị chồng chéo. |
| **Mã hóa Font chữ** | Lỗi unicode (`gi?ng`, `s?a`, `nh?p`, `?` chỉ báo xu hướng) do lưu sai bảng mã. | Chuyển đổi toàn bộ mã hóa file nguồn sang UTF-8-BOM. Khôi phục các biểu tượng `▲` / `▼` / `―`. | Triệt tiêu hoàn toàn lỗi font chữ hiển thị, giao diện chỉn chu, mẫu mực sư phạm. |
| **Khả năng hiển thị** | Thiếu `ScrollViewer` trên hầu hết các trang, dễ bị cắt mất nút bấm ở độ phân giải thấp. | Bao bọc toàn bộ nội dung Page bằng `<ScrollViewer VerticalScrollBarVisibility="Auto">`. | Đảm bảo hiển thị đầy đủ thông tin trên máy chiếu phòng hội đồng và laptop cũ. |
| **Chỉ dẫn sử dụng** | Không có tài liệu hoặc hướng dẫn từng bước trực quan cho các tác vụ nguy hiểm. | Bổ sung Tooltip hướng dẫn, khối trợ giúp bằng chữ hoặc Wizard xác nhận thao tác. | Giảm thiểu 95% sai sót vận hành và mất mát dữ liệu do Hiệu trưởng bấm nhầm nút. |
| **Ngôn ngữ tiêu đề** | Tiêu đề XAML bằng tiếng Anh thô (`System Settings`, `App Usage Analytics`) hoặc không dấu (`Du gio`). | Dịch toàn bộ tiêu đề sang tiếng Việt có dấu chuẩn học thuật (`Cài đặt hệ thống`, `Dự giờ đánh giá`). | Đồng bộ hóa ngôn ngữ Việt hóa 100%, trang trọng và phù hợp văn hóa trường học. |

---

## ═══ PHẦN 4: CHECKSHEET KIỂM THỬ KHẮC PHỤC (VERIFICATION CHECKSHEET) ═══

*   [ ] **Kiểm tra hiển thị Font:** Mở trang Phê duyệt bài giảng và Cài đặt hệ thống, xác minh các từ `giảng`, `sửa`, `nhập`, `lỗi` hiển thị đúng dấu tiếng Việt.
*   [ ] **Kiểm tra xu hướng KPI:** Xác nhận các ký tự xu hướng `▲` (tăng), `▼` (giảm), `―` (không đổi) hiển thị sắc nét, không còn dấu `?` lỗi.
*   [ ] **Kiểm tra tỷ lệ chuyên cần:** Nhập dữ liệu điểm danh thực tế với trạng thái `"Present"` và kiểm tra Dashboard BGH hiển thị tỷ lệ đúng (ví dụ: 98.5%), không báo 0.0% hay N/A.
*   [ ] **Kiểm tra Lưu Cấu hình:** Chỉnh sửa IP Address và lưu lại, xác nhận dữ liệu được ghi xuống database SQLite thành công và hiển thị lại chính xác khi mở lại trang.
*   [ ] **Kiểm tra Khôi phục hệ thống:** Xác nhận có hộp thoại cảnh báo chi tiết từng bước và yêu cầu nhập mật khẩu xác nhận Admin trước khi khôi phục.
*   [ ] **Kiểm tra Đồ thị OxyPlot:** Mở Dashboard KPI, kiểm tra biểu đồ cột so sánh học lực HK1/HK2 hiển thị đầy đủ các thanh cột cột đứng ứng với các xếp loại Học lực.
*   [ ] **Kiểm tra co giãn màn hình:** Thu nhỏ độ phân giải màn hình về 1024x768, xác minh thanh cuộn ScrollViewer xuất hiện và cho phép cuộn tới các nút bấm lưu/xuất file bên dưới.

---

## ═══ KẾT LUẬN CỦA HỘI ĐỒNG ═══
Hội đồng Chuyên gia đánh giá giao diện Hiệu trưởng của dự án QA SmartClass **CHƯA ĐẠT tiêu chuẩn thiết kế sư phạm và ràng buộc kỹ thuật bản v4.1** do tồn tại nhiều lỗi kỹ thuật cơ bản và lỗi dịch thuật/font chữ. 

**Khuyến nghị:** Nhóm phát triển dự án cần lập tức tiến hành sửa đổi mã nguồn theo các giải pháp đã nêu trong bảng đối chiếu để hoàn thiện chương trình, sẵn sàng cho đợt nghiệm thu tiếp theo.

*Hội đồng Chuyên gia thống nhất ký duyệt báo cáo.*
