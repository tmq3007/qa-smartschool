# BÁO CÁO ĐÁNH GIÁ CHUYÊN SÂU GIAO DIỆN ADMIN NHÀ TRƯỜNG & IT CONSOLE
**Đơn vị thực hiện:** Hội đồng Chuyên gia Đa ngành QA SmartSchool (17 Thành viên)  
**Phiên bản đánh giá:** Phân hệ Admin - QA SmartClass v4.1  
**Ngày lập báo cáo:** 30/06/2026  

---

## ═══ TỔNG QUAN ĐÁNH GIÁ ═══

Để đáp ứng tối đa tính sư phạm, tính thân thiện người dùng và hiệu năng kỹ thuật theo bộ quy chuẩn **QA SmartClass v4.1**, Hội đồng Chuyên gia đa ngành gồm 17 thành viên đã tiến hành kiểm định, rà soát toàn diện phân hệ quản lý của **Admin nhà trường & Trạm kỹ thuật (IT Console)** (thư mục [Admin](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin)). 

Kết quả rà soát cho thấy phân hệ Admin đã xây dựng được các công cụ quản trị kỹ thuật mạnh mẽ như chẩn đoán mạng, xem log thời gian thực, quản lý license, sao lưu/khôi phục cơ sở dữ liệu có bảo vệ bằng mã PIN. Tuy nhiên, hệ thống vẫn đang tồn tại một số **lỗi logic nghiêm trọng** (lỗi định tuyến nhầm tab khiến Admin không thể quản lý giáo viên/học sinh, lỗi giả lập trạng thái mạng nhấp nháy liên tục), **lỗi sư phạm** (giao diện bảng điều khiển mang phong cách "hacker" neon gây mỏi mắt, thuật ngữ tiếng Anh chưa việt hóa triệt để) và **lỗi thiết kế giao diện** (thiếu thanh cuộn chống rách hình ở độ phân giải thấp).

---

## ═══ PHẦN 1: ĐÁNH GIÁ CHI TIẾT THEO 7 YÊU CẦU QUY CHUẨN v4.1 ═══

### 1. Font chữ tiếng Việt (Typography & Encoding)
*   **Tích cực:** Các tệp tin thiết kế chung đã sử dụng font chữ `Segoe UI` rõ ràng, đồng bộ cho các nhãn và trường nhập liệu.
*   **Hạn chế:** 
    *   Các hộp thoại thông báo trong [SchoolAdminDashboardControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml.cs) và [QAVendorAdminControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/QAVendorAdminControl.xaml.cs) sử dụng chuỗi tiếng Việt có dấu lồng trong code-behind. Dù đang được hiển thị đúng trên môi trường lập trình, cần kiểm soát chặt chẽ mã hóa tệp tin là **UTF-8 with BOM** để tránh lỗi hiển thị thành các ký tự lạ hoặc dấu hỏi chấm `?` khi phân phối phần mềm đến máy tính của nhà trường.
    *   Sơ đồ mạng trong [NetworkTopologyControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/NetworkTopologyControl.xaml) chưa hỗ trợ tùy chọn font chữ học thuật hoặc font chữ độ tương phản cao cho người dùng mắt yếu.

### 2. Bố cục & Tính đáp ứng (Layout & Responsiveness)
*   **Lỗi thiếu ScrollViewer gây rách hình:** Tệp [SchoolAdminDashboardControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml) sử dụng Grid tĩnh làm layout chính (dòng 153). Khi co giãn cửa sổ hoặc hiển thị trên laptop màn hình nhỏ của nhân viên kỹ thuật (độ phân giải 1366x768), các phần tử bên dưới DataGrid và nút chức năng (như nút "Thêm Giáo Viên", "Import Excel", "Lưu") sẽ bị che khuất và không thể cuộn tới.
*   **Cứng hóa kích thước cửa sổ (Hardcoded Size):** Cửa sổ chính [AdminConsoleWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml) thiết lập kích thước cố định `Height="800" Width="1200"` kết hợp `ResizeMode="NoResize" WindowStyle="None"`. Thiết kế này làm hỏng tính năng co giãn tự động và hoàn toàn không thể sử dụng trên màn hình máy chiếu phòng Lab cũ (độ phân giải tối đa 1024x768) hoặc máy tính bảng của BGH.
*   **Giải pháp:** Bọc Grid chính của dashboard trong `<ScrollViewer VerticalScrollBarVisibility="Auto">` và cho phép cửa sổ chính thay đổi kích thước linh hoạt bằng cách sử dụng `MinHeight`/`MinWidth` thay vì kích thước cứng.

### 3. Màu sắc & Thẩm mỹ Sư phạm (Color Palette)
*   **Lỗi phối màu "Hacker" phản sư phạm:** Giao diện [AdminConsoleWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml) và [QAVendorAdminControl.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/QAVendorAdminControl.xaml) lạm dụng tông màu tối đậm `#0F172A` kết hợp với bảng màu neon rực rỡ (vàng `#FACC15`, tím `#A78BFA`, xanh lá `#34D399`, hồng `#F472B6`, đỏ `#F43F5E`). Lối thiết kế này mang cảm giác của một màn hình lập trình "hacker", gây mỏi mắt nghiêm trọng khi làm việc lâu và hoàn toàn lạc lõng trong không gian sư phạm của nhà trường.
*   **Màu sắc cảnh báo nguy hiểm:** Nút "Xóa sạch Database (Factory Reset)" sử dụng sắc đỏ quá chói (#F87171), gây cảm giác hoang mang cho nhân viên kỹ thuật. Cần phối lại màu sang tông màu ấm dịu, chuyên nghiệp và hài hòa hơn theo bảng màu chuẩn của QA SmartClass v4.1.

### 4. Logic chức năng chương trình (Functional Logic Bugs)
*   **Lỗi nghiêm trọng 1: Định tuyến sai Dashboard Admin (Routing Tab Bug):** Trong tệp [AdminConsoleWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml#L327-L333), Tab Item thứ 4 dành cho "Quản trị Trường Học" lại đang chứa control `<controls:SchoolDirectorDashboard Margin="-32"/>` (Dashboard của Hiệu trưởng/BGH) thay vì `<controls:SchoolAdminDashboardControl/>` (Dashboard thực tế của Admin kỹ thuật). Lỗi định tuyến này khiến Admin nhà trường khi bấm vào tab "Quản trị Trường Học" không thể tiếp cận các chức năng cốt lõi như CRUD Giáo viên, tìm kiếm và Import học sinh, mà chỉ nhìn thấy các biểu đồ thống kê tĩnh.
*   **Lỗi nghiêm trọng 2: Mạng Topology nhấp nháy giả lập (Flashing Network Topology Bug):** Trong tệp [NetworkTopologyControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/NetworkTopologyControl.xaml.cs#L429-L463), timer đồng bộ mạng định kỳ 5 giây (`SyncTimer_Tick`) đang thực hiện một hàm Random đổi trạng thái Online/Offline của học sinh:
    ```csharp
    student.Status = rand.NextDouble() > 0.1 ? "Online" : "Offline";
    ```
    Việc liên tục đổi trạng thái giả lập này khiến sơ đồ mạng nhấp nháy xanh/đỏ liên tục sau mỗi 5 giây, tạo ra các cảnh báo lỗi giả (False Alarms), làm nhiễu thông tin giám sát và gây ức chế cực lớn cho IT Manager. Sơ đồ mạng cần phản ánh đúng trạng thái thực tế từ danh sách kết nối của `NetworkService`.
*   **Lỗi nghiêm trọng 3: Thiếu kiểm tra trùng khóa và thiếu tạo mật khẩu giáo viên (Teacher CRUD Crash Bug):** 
    *   Trong [SchoolAdminDashboardControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolAdminDashboardControl.xaml.cs#L92-L112), chức năng thêm mới giáo viên gọi `db.TeacherProfiles.Add(result)` mà không có bất kỳ bước kiểm tra trùng mã `TeacherCode` trong CSDL. Do trường `TeacherCode` được cấu hình duy nhất (`IsUnique`), hành vi này sẽ gây sập chương trình (Crash) do lỗi `DbUpdateException` khi người dùng nhập trùng mã.
    *   Hơn nữa, khi tạo mới giáo viên từ giao diện, trường `PasswordHash` và `TeacherPassword` hoàn toàn bị để trống (không được gán mật khẩu mặc định như bên học sinh). Điều này khiến giáo viên mới được tạo không bao giờ đăng nhập được vào hệ thống.

### 5. Hình ảnh minh họa & Đồ thị trực quan (Data Visualization)
*   **Tích cực:** Đã có giao diện sơ đồ mạng trực quan biểu diễn vị trí các máy trạm dưới dạng canvas kéo thả được.
*   **Hạn chế:** 
    *   Trong [SchoolDirectorDashboard.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Controls/SchoolDirectorDashboard.xaml.cs#L27-L48), biểu đồ cột thể hiện tần suất sử dụng theo tuần (`chartUsage`) và danh sách Giáo viên tích cực (`listTopTeachers`) đang sử dụng dữ liệu tĩnh được gán cứng (Fake Data): `"Tuần 1", "Tuần 2", "Hoàng Duy", "Mai Ngọc"...` thay vì truy vấn thực tế từ bảng `UsageLogs` trong database.
    *   Việc xuất báo cáo PDF khi nhấn "Xuất Báo Cáo" (dòng 50) cũng lấy dữ liệu mẫu giả lập thay vì lấy dữ liệu thực tế của lớp học, làm mất đi tính chính xác và giá trị thực tế của hệ thống quản lý dữ liệu.

### 6. Chỉ dẫn sử dụng từng bước (Step-by-step User Guidance)
*   **Thiếu hướng dẫn ngăn ngừa sai sót:**
    *   Chức năng "Import Excel" trong quản lý học sinh không cung cấp liên kết tải tệp Excel biểu mẫu (`template.xlsx`), cũng như không có hướng dẫn cấu trúc các cột (MãHS, HọTên, Lớp, Trường) ngay trên UI. Điều này dẫn đến tỷ lệ lỗi import rất cao (lỗi Loi_14).
    *   Chức năng "Khôi phục từ Backup" và "Xóa sạch Database (Factory Reset)" là các thao tác có độ nguy hiểm cao nhưng giao diện chưa tích hợp tài liệu hướng dẫn nhanh hoặc wizard từng bước cảnh báo các hậu quả mất mát dữ liệu trước khi thực hiện.

### 7. Ngôn ngữ & Việt hóa (Localization & Consistency)
*   **Pha trộn tiếng Anh - tiếng Việt thô:** Giao diện hiển thị rất nhiều nhãn tiếng Anh chưa được dịch nghĩa như: `Title="Admin Console"`, `IT CONSOLE`, `Telemetry`, `Switcher`, `L1 - QA Vendor (Full)`, `L2 - Quản trị Trường`, `Uptime`, `Avg Uptime`, `Active`, `Expired`...
*   **Thuật ngữ thiếu thống nhất:** Giao diện lúc dùng chữ "Hủy", lúc dùng chữ "HỦY" (viết hoa toàn bộ), tiêu đề dialog tạo License lúc dùng chữ "Tạo License Mới" lúc lại dùng "Tạo License". Sự thiếu nhất quán này làm giảm đi tính mẫu mực của một sản phẩm giáo dục.

---

## ═══ PHẦN 2: Ý KIẾN CHI TIẾT TỪ HỘI ĐỒNG 17 CHUYÊN GIA ═══

### 1. 💼 Trưởng bộ phận thiết kế dự án QA Smart School (Project Design Lead)
> "Việc gắn nhầm `SchoolDirectorDashboard` vào Tab Quản trị Trường Học của Admin Console là một lỗi nghiêm trọng trong khâu kiểm soát tích hợp hệ thống. Nó khiến giao diện Admin mất đi 50% tính năng quản trị thực tế (CRUD Giáo viên, Import Học sinh)."

### 2. 💻 Quản lý IT (IT Manager)
> "Tôi không thể chấp nhận sơ đồ mạng cứ nhấp nháy xanh đỏ liên tục sau mỗi 5 giây chỉ vì code chạy hàm Random. Nó làm chúng tôi mất khả năng phán đoán máy trạm nào thực sự bị mất kết nối mạng LAN để xuống kiểm tra trực tiếp."

### 3. 🔍 Chuyên gia kiểm thử phần mềm (QA Tester)
> "Tỷ lệ bao phủ kiểm thử tự động cho phân hệ Admin quá thấp. Lỗi trùng mã giáo viên gây crash phần mềm hay việc gán thiếu mật khẩu mặc định cho giáo viên đều là các lỗi cơ bản lẽ ra phải bị chặn từ khâu viết unit test."

### 4. 🎨 Chuyên gia thiết kế giao diện (UI/UX Designer)
> "Phần mềm giáo dục cần trang trọng và dịu mắt. Tông màu tối với các màu neon sặc sỡ kiểu 'hacker' trong Admin Console gây mệt mỏi thị giác rất nhanh. Thiết kế này cần đổi sang một giao diện sáng tinh tế hoặc chế độ tối dịu mắt (soft dark mode) phù hợp môi trường học đường."

### 5. ⚙️ Chuyên gia phân tích và thiết kế hệ thống (System Analyst)
> "Hệ thống định nghĩa vai trò L1, L2, L3 rất rõ ràng trong `AdminConsoleWindow.xaml.cs`. Tuy nhiên việc ánh xạ luồng nghiệp vụ trên UI bị sai lệch lớn khi đưa Dashboard BGH thế chỗ của bảng điều khiển Admin kỹ thuật."

### 6. 🗄️ Chuyên gia về cơ sở dữ liệu và thiết bị kết nối ngoại vi (DB & Connectivity Specialist)
> "SQLite WAL hoạt động ổn định nhưng việc lưu mật khẩu giáo viên dạng trống hoặc thiếu cơ chế đồng bộ mật khẩu mặc định khi import sẽ làm tê liệt chức năng xác thực lớp học. Sơ đồ mạng cần lấy thông tin IP thực tế từ card mạng thay vì hiển thị cứng '192.168.x.x'."

### 7. 🛡️ Chuyên gia về bảo mật và an ninh mạng (Security Expert)
> "Tôi đánh giá cao việc băm PIN bằng SHA-256 có muối và mã hóa tệp cấu hình bằng DPAPI tránh dịch ngược. Nhưng chức năng đổi trạng thái chuyển đổi chế độ ép buộc (Force Mode) cần yêu cầu xác thực Admin PIN thay vì chỉ dùng một Overlay xác nhận đơn giản để tránh học sinh lợi dụng chuyển đổi máy."

### 8. 🏫 Nhà giáo dục (Educator)
> "Ngôn ngữ hiển thị của Admin nhà trường phải hoàn toàn thuần Việt và mẫu mực sư phạm. Việc để tiêu đề nửa Anh nửa Việt ('Admin Console - QA SmartClass') thể hiện sự thiếu chỉn chu trong thiết kế sản phẩm dùng trong nhà trường."

### 9. 🎓 Nhà Quản lý hiệu trưởng nhà trường (School Principal)
> "Khi tôi muốn xem Dashboard của mình, dữ liệu hiển thị phải là dữ liệu thật từ các lớp học để tôi nắm bắt tình hình của trường. Việc gán cứng số liệu giả định (như 85% sử dụng, 142 bài giảng mới) khiến chúng tôi không thể đưa ra quyết định quản lý chính xác."

### 10. 👥 Trưởng bộ môn của trường (Head of Department)
> "Chúng tôi cần danh sách giáo viên trong tổ bộ môn được hiển thị chính xác theo môn học. Lỗi không lưu được thông tin giáo viên khi thêm mới khiến việc phân nhóm chuyên môn gặp nhiều trở ngại."

### 11. 👩‍🏫 Giáo viên ưu tú với nhiều kinh nghiệm (Elite Teacher)
> "Khi Admin thêm tài khoản cho tôi, tôi cần nhận được thông tin mật khẩu mặc định rõ ràng để đăng nhập. Việc tạo tài khoản không gán mật khẩu làm chúng tôi mất rất nhiều thời gian nhờ hỗ trợ kỹ thuật cài lại."

### 12. 👦 Học sinh (Student)
> "Khi nhà trường cập nhật danh sách lớp bằng cách nhập file Excel, thỉnh thoảng tên của tụi em bị lỗi font chữ hiển thị trên màn hình do mã hóa file Excel không đúng chuẩn Unicode."

### 13. 🧹 Nhân viên nhà trường - Kỹ thuật viên (IT Support Staff)
> "Tôi cần một nút tải file Excel mẫu để tôi copy thông tin học sinh vào đúng định dạng trước khi import. Hiện tại không có file mẫu khiến tôi phải đoán tên cột, cứ import vào là báo lỗi 'Import Error' rất mất thời gian."

### 14. 🎮 Gamer giỏi (Pro Gamer)
> "Trải nghiệm trên sơ đồ mạng bị giật cục khi kéo thả các nút máy trạm do canvas vẽ lại liên tục (Redraw) mỗi khi trạng thái online/offline ngẫu nhiên thay đổi. Cần tối ưu hóa rendering canvas và áp dụng hiệu ứng chuyển động mượt mà khi di chuyển vị trí máy."

### 15. 🏢 Cán bộ quản lý của phòng giáo dục (District Admin)
> "Mẫu báo cáo PDF xuất ra từ trạm Admin phải đảm bảo đúng quy chuẩn của Bộ Giáo dục và Đào tạo. Việc sử dụng dữ liệu giả lập để tạo báo cáo xuất khẩu là hành vi vi phạm nguyên tắc quản lý giáo dục."

### 16. 🏫 Chuyên viên của sở giáo dục (Provincial Specialist)
> "Sở yêu cầu tính minh bạch cao trong dữ liệu sử dụng phần mềm. Việc giả lập số liệu báo cáo của Hiệu trưởng là không được phép. Yêu cầu lập trình viên kết nối trực tiếp cơ sở dữ liệu thực tế vào biểu đồ."

### 17. 🔬 Nhà khoa học giáo dục (Educational Scientist)
> "Giao diện Admin kỹ thuật tuy đòi hỏi nhiều thông tin hệ thống nhưng vẫn cần giảm tải nhận thức (Cognitive Load). Việc loại bỏ tông màu neon tương phản mạnh và bổ sung chỉ dẫn từng bước sẽ giúp nhân viên kỹ thuật giảm bớt áp lực tâm lý khi vận hành hệ thống."

---

## ═══ PHẦN 3: BẢNG ĐỐI CHIẾU TRƯỚC VS SAU CẢI TIẾN ═══

| Phân hệ / Tiêu chí | Trạng thái Hiện tại (Lỗi v4.1) | Giải pháp Đề xuất (Sau cải tiến) | Lợi ích Sư phạm & Kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Định tuyến Dashboard** | Tab "Quản trị Trường Học" liên kết nhầm control `SchoolDirectorDashboard` (BGH). | Thay đổi trong XAML sang `<controls:SchoolAdminDashboardControl/>`. | Admin thực hiện được CRUD Giáo viên và Import/Tìm kiếm Học sinh. |
| **Đồng bộ mạng Topology** | Trạng thái máy trạm đổi ngẫu nhiên liên tục bằng hàm Random gây nhấp nháy xanh đỏ. | Đọc dữ liệu kết nối thực tế từ `app.NetworkService.Clients`. | Phản ánh chính xác 100% tình trạng kết nối mạng trạm thời gian thực. |
| **CRUD Giáo viên** | Thêm mới gây crash nếu trùng mã `TeacherCode`; mật khẩu giáo viên bị để trống. | Bổ sung hàm kiểm tra trùng mã trước khi lưu; tự động tạo mật khẩu băm HMACSHA512. | Tránh sập ứng dụng và cho phép giáo viên mới tạo đăng nhập được ngay. |
| **Thẩm mỹ Giao diện** | Giao diện tối màu phối màu neon tương phản mạnh kiểu "hacker console". | Thay đổi sang giao diện sáng chuyên nghiệp hoặc soft-dark mode dịu mắt. | Giảm thiểu mỏi mắt, phù hợp môi trường học thuật, trang nhã. |
| **Dữ liệu Thống kê** | Sử dụng dữ liệu mẫu gán cứng cho biểu đồ và danh sách giáo viên tích cực. | Viết truy vấn SQL/LINQ đếm dữ liệu thực tế từ bảng `UsageLogs` và `TeacherProfiles`. | Báo cáo chính xác hiệu suất dạy học thực tế của trường. |
| **Khả năng co giãn** | Cố định kích thước cửa sổ `1200x800` và thiếu `ScrollViewer` ở màn hình Dashboard. | Thêm `ScrollViewer` vào root layout; thiết lập tỷ lệ co giãn động phối hợp `MinWidth`. | Hiển thị trọn vẹn thông tin trên mọi màn hình máy chiếu phòng Lab và máy tính cũ. |
| **Chỉ dẫn sử dụng** | Không có liên kết tải file mẫu Excel import và thiếu wizard chỉ dẫn khôi phục/reset dữ liệu. | Thêm nút tải file mẫu `template.xlsx`; bổ sung tooltip và hộp thoại hướng dẫn chi tiết. | Giảm 90% lỗi định dạng file khi import và tránh mất dữ liệu do bấm nhầm nút. |
| **Ngôn ngữ hiển thị** | Trộn lẫn nhiều từ tiếng Anh (`IT Console`, `Telemetry`, `Uptime`, `Active`). | Việt hóa 100% các thuật ngữ hệ thống theo đúng văn cảnh sư phạm trường học. | Đảm bảo tính nhất quán, trang trọng và thuần Việt trong trường học. |

---

## ═══ PHẦN 4: CHECKSHEET KIỂM THỬ KHẮC PHỤC (VERIFICATION CHECKSHEET) ═══

*   [ ] **Kiểm tra Định tuyến:** Nhấp vào tab "Quản trị Trường Học" trong Admin Console, xác nhận hiển thị đúng giao diện có danh sách Giáo viên và nút "Import Excel".
*   [ ] **Kiểm tra Sơ đồ Mạng:** Mở sơ đồ Topology, quan sát trong 1 phút để đảm bảo các máy trạm giữ nguyên trạng thái kết nối ổn định (không tự động nhấp nháy xanh/đỏ).
*   [ ] **Kiểm tra Thêm Giáo viên Trùng:** Thử thêm mới giáo viên với mã `TeacherCode` đã tồn tại, xác nhận hệ thống hiển thị thông báo cảnh báo "Mã giáo viên đã tồn tại" thay vì bị crash.
*   [ ] **Kiểm tra Đăng nhập Giáo viên mới:** Thêm giáo viên mới, sử dụng thông tin đó để đăng nhập chế độ Giáo viên, xác minh đăng nhập thành công bằng mật khẩu mặc định được sinh ra.
*   [ ] **Kiểm tra Đồ thị Thực:** Ghi nhận 1 phiên sử dụng mới trong phần mềm, kiểm tra biểu đồ thống kê tăng tương ứng, xác nhận không còn dữ liệu tĩnh gán cứng.
*   [ ] **Kiểm tra Hiển thị độ phân giải thấp:** Chuyển màn hình về độ phân giải 1024x768, xác nhận xuất hiện thanh cuộn và cho phép nhấn các nút ở đáy DataGrid dễ dàng.
*   [ ] **Kiểm tra Tải file mẫu:** Nhấn nút "Import Excel" trong tab Học sinh, xác nhận có tùy chọn "Tải file mẫu Excel" và tải về thành công tệp có đúng cấu trúc cột.
*   [ ] **Kiểm tra Việt hóa:** Rà soát toàn bộ giao diện sidebar, header và các thông báo lỗi để đảm bảo không còn xuất hiện từ tiếng Anh thô.

---

## ═══ KẾT LUẬN CỦA HỘI ĐỒNG ═══

Hội đồng Chuyên gia đánh giá giao diện Admin nhà trường của dự án QA SmartClass **CHƯA ĐẠT tiêu chuẩn thiết kế sư phạm và ràng buộc kỹ thuật bản v4.1**. Các lỗi logic định vị nhầm trang và giả lập nhấp nháy trạng thái mạng làm ảnh hưởng nghiêm trọng đến hiệu năng sử dụng của trạm IT.

**Khuyến nghị:** Đội ngũ lập trình cần ưu tiên khắc phục ngay lỗi định tuyến tab và kết nối dữ liệu mạng thực tế vào sơ đồ Topology trước đợt kiểm định tiếp theo.

*Hội đồng Chuyên gia thống nhất ký duyệt báo cáo.*
