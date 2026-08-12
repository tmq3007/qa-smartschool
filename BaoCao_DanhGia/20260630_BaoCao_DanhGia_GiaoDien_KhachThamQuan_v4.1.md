# BÁO CÁO THẨM ĐỊNH & ĐÁNH GIÁ CHI TIẾT GIAO DIỆN BÁO CÁO VÀ NHẬT KÝ KHÁCH THAM QUAN
**Áp dụng Bộ quy chuẩn Thiết kế Sư phạm và Ràng buộc Kỹ thuật QA SmartClass v4.1**  
*Ngày báo cáo: 30 tháng 06 năm 2026*  
*Mã tài liệu: BC-TD-VTR-4.1*  

---

## ═══ THÀNH PHẦN HỘI ĐỒNG THẨM ĐỊNH (17 CHUYÊN GIA) ═══

Hội đồng Chuyên gia Dự án **QA Smart School** gồm 17 thành viên đại diện cho các góc nhìn từ Kỹ thuật hệ thống, An ninh mạng, Thiết kế giao diện (UI/UX), Giáo dục học, Nhà quản lý và Người dùng cuối (Giáo viên, Học sinh, Cán bộ quản lý giáo dục) đã tiến hành rà soát, đánh giá thực tế phân hệ **Nhật ký Bảo vệ & Báo cáo khách tham quan** (thông qua các tệp tin [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml), [SecurityKioskViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs) và [ReportsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/ReportsView.xaml)).

---

## ═══ PHẦN I: TỔNG HỢP CÁC LỖI LOGIC HỆ THỐNG & ĐIỂM YẾU GIAO DIỆN CẦN KHẮC PHỤC ═══

Qua quá trình chạy thử nghiệm, kiểm thử mã nguồn và rà soát giao diện XAML/C#, Hội đồng đã phát hiện các lỗi logic nghiêm trọng và các điểm chưa tối ưu sau đây:

### 1. Lỗi thiết kế cơ sở dữ liệu phi cấu trúc đối với thông tin thẻ CCCD (Unstructured CCCD Data Storage) - Nghiêm trọng
*   **Mô tả lỗi:** 
    *   Trong tệp [SecurityKioskViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs#L56-L80), chức năng quét mã QR CCCD tự động bóc tách các trường dữ liệu quan trọng như Số CCCD, Họ tên, Giới tính, Địa chỉ.
    *   Tuy nhiên, thay vì lưu trữ các thông tin này vào các cột thuộc tính riêng biệt trong bảng `SecurityLogs` để phục vụ báo cáo và truy vấn, hệ thống lại thực hiện cộng chuỗi thô và đẩy toàn bộ vào cột `Description`:
        ```csharp
        Description = $"Khách: {name} - CCCD: {cccd} - Giới tính: {gender} - Địa chỉ: {address}";
        ```
    *   **Hậu quả:** 
        1.  **Mất khả năng báo cáo cấu trúc:** Không thể thống kê số lượng khách theo địa phương cư trú, theo giới tính hoặc tra cứu nhanh lịch sử ra vào của một số CCCD cụ thể mà không dùng lệnh quét chuỗi `LIKE %...%` rất chậm và dễ sai sót.
        2.  **Vi phạm chuẩn hóa CSDL:** Việc lưu nhiều thông tin thuộc tính vào một cột duy nhất vi phạm nghiêm trọng dạng chuẩn 1 (1NF) trong thiết kế cơ sở dữ liệu.

### 2. Sự cô lập thông tin khách tham quan trên giao diện Báo cáo (Missing Visitor Analytics in Reports Dashboard) - Nghiêm trọng
*   **Mô tả lỗi:** 
    *   Giao diện [ReportsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/ReportsView.xaml) được đặt tên là "Báo Cáo Tổng Hợp Nghiệp Vụ" và hiển thị các số liệu thống kê về Doanh thu Canteen, Sự cố và Lượt quét thẻ cổng.
    *   Tuy nhiên, lượt quét thẻ cổng này chỉ truy vấn sự kiện của học sinh/giáo viên (`GateCheckIn` và `GateCheckOut` trong `EventLogs`). Toàn bộ dữ liệu khách tham quan ra vào trường ghi nhận ở bảng `SecurityLogs` hoàn toàn **không xuất hiện** trên dashboard báo cáo này.
    *   **Hậu quả:** Ban Giám hiệu và các đoàn thanh tra sư phạm khi xem trang Báo cáo Tổng hợp không thể nắm bắt được số lượng khách đến liên hệ công tác trong ngày/tuần/tháng, thời gian lưu trú trung bình của khách, hoặc mục đích khách ghé thăm trường.

### 3. Lỗi nhấp nháy viền hộp nhập liệu khi không focus (Inverse Focus Blinking Bug) - Lỗi UI/UX gây ức chế
*   **Mô tả lỗi:** 
    *   Trong tệp [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml#L27-L59), style `BlinkingTextBoxStyle` thiết lập hiệu ứng nhấp nháy đổi màu viền sang sắc cam đậm `#EA580C` liên tục theo chu kỳ `0.8s` khi thuộc tính `IsFocused` có giá trị là **False** (nghĩa là khi người dùng **không** để con trỏ chuột ở đó).
    *   **Hậu quả:** Hộp quét QR CCCD sẽ nhấp nháy liên tục suốt cả ngày khi bảo vệ đang làm việc khác hoặc đang quan sát danh sách lịch sử. Hiệu ứng nhấp nháy liên tục này tạo ra sự mỏi mắt cực độ cho cán bộ bảo vệ trực ca, đồng thời tạo cảm giác giao diện đang ở trạng thái báo động đỏ/lỗi hệ thống liên tục, vi phạm nguyên tắc giảm tải nhận thức của chuẩn v4.1.

### 4. Thiếu hụt trường thông tin nghiệp vụ cốt lõi (Missing Core Business Fields) - Lỗi phân tích hệ thống
*   **Mô tả lỗi:** Biểu mẫu ghi nhận sự kiện của khách trong [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml) và bảng `SecurityLog` thiếu các trường nghiệp vụ quan trọng:
    *   **Cán bộ/Giáo viên tiếp đón (Host Person):** Khách vào trường gặp ai?
    *   **Mục đích chuyến thăm (Visit Purpose):** Gặp phụ huynh, liên hệ công tác tuyển sinh, giao hàng, bảo trì thiết bị, v.v.
    *   **Số thẻ khách cấp phát (Visitor Badge Number):** Thẻ số mấy để đối chiếu khi ra cổng.
    *   **Hậu quả:** Thiếu tính liên kết dữ liệu, bảo vệ không thể kiểm soát khách đang ở khu vực nào trong trường và gặp ai, gây sơ hở lớn về an ninh trường học.

### 5. Lỗi co giãn gây rách hình và che khuất thông tin (Lack of ScrollViewer & Text Wrapping)
*   **Mô tả lỗi:** 
    *   Trong [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml#L188-L256), cột bên phải chứa `DataGrid` lịch sử ghi nhận được đặt trong một `Border` tĩnh không có `ScrollViewer` bọc ngoài. Khi hiển thị trên màn hình laptop độ phân giải thấp (1366x768), phần dưới của DataGrid sẽ bị che khuất và người dùng không thể cuộn xem dữ liệu cũ.
    *   Cột **Chi tiết** trong DataGrid (hiển thị chuỗi thông tin CCCD rất dài) không được cấu hình tự động xuống dòng (`TextWrapping="Wrap"`). Nội dung địa chỉ và chi tiết sự kiện bị cắt ngang (clipped) khiến bảo vệ không đọc được đầy đủ thông tin khách tham quan.

### 6. Thiếu chỉ dẫn sử dụng từng bước trực quan (No Step-by-Step UX Guide)
*   **Mô tả lỗi:** Giao diện đăng ký khách tham quan đòi hỏi bảo vệ phải thao tác theo thứ tự (Đặt con trỏ vào ô nhập liệu -> Quét CCCD -> Chọn loại sự kiện -> Lưu). Tuy nhiên, giao diện **hoàn toàn không có khung hướng dẫn sử dụng nhanh** hoặc wizard hướng dẫn từng bước để giảm thiểu sai sót cho cán bộ bảo vệ lớn tuổi.

### 7. Lỗi Hardcoded giá trị cực đại của biểu đồ sự cố (Hardcoded ProgressBar Maximum) - Lỗi logic hiển thị
*   **Mô tả lỗi:** Trong [ReportsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/ReportsView.xaml#L152), thanh tiến trình ProgressBar hiển thị số lượng sự cố theo mức độ nghiêm trọng thiết lập giá trị tĩnh `Maximum="50"`. Nếu số lượng sự cố vượt quá 50, thanh ProgressBar sẽ hiển thị sai tỷ lệ hoặc tràn giao diện hiển thị. Giá trị này cần được tính động bằng tổng số lượng sự cố thực tế.

---

## ═══ PHẦN II: Ý KIẾN CHI TIẾT TỪ HỘI ĐỒNG 17 CHUYÊN GIA ═══

### 1. 💼 Trưởng bộ phận thiết kế dự án QA Smart School (Project Design Lead)
> *"Sự thiếu kết nối giữa Nhật ký bảo vệ và trang Báo cáo Tổng hợp là một lỗi nghiêm trọng về cấu trúc sản phẩm. Khách tham quan hay các đoàn thanh tra cần nhìn thấy bức tranh tổng thể về lưu lượng người ra vào trường ngay trên trang Báo cáo thay vì phải lục tìm trong nhật ký thô của bảo vệ."*

### 2. 💻 Quản lý IT (IT Manager)
> *"Mã nguồn quét mã QR CCCD cần tối ưu hóa. Nếu bảo vệ quét liên tục hoặc thiết bị đọc gửi dữ liệu lỗi, luồng xử lý chuỗi trong `OnQrInputBufferChanged` có thể bị lỗi IndexOutOfRangeException do chia tách chuỗi '|' thiếu phần tử. Cần bọc trong khối try-catch an toàn."*

### 3. 🔍 Chuyên gia kiểm thử phần mềm (QA Tester)
> *"Việc không kiểm tra trạng thái đối xứng (CheckIn mà chưa CheckOut) dẫn đến lỗi logic: Một vị khách có thể được bấm 'Khách vào cổng' nhiều lần liên tục mà không có cảnh báo nào từ hệ thống. Chúng tôi cần một cơ chế cảnh báo nếu khách đã vào trường quá 8 tiếng chưa ra."*

### 4. 🎨 Chuyên gia thiết kế giao diện (UI/UX Designer)
> *"Style nhấp nháy viền cam khi TextBox MẤT focus là một lỗi thiết kế UI ngược đời và phản sư phạm. Nó làm bảo vệ bị phân tâm liên tục. Hơn nữa, font chữ mặc định trên DataGrid cần đồng bộ sang InterFont tương tự các phân hệ học tập khác."*

### 5. ⚙️ Chuyên gia phân tích và thiết kế hệ thống (System Analyst)
> *"Cần định nghĩa lại thực thể `VisitorSession` trong hệ thống. Một phiên viếng thăm phải có trạng thái rõ ràng (Đang trong trường, Đã rời trường). Hiện tại chúng ta chỉ lưu các dòng nhật ký đơn lẻ (CheckIn, CheckOut riêng biệt) khiến việc phân tích thời gian lưu trú trung bình là bất khả thi."*

### 6. 🗄️ Chuyên gia về cơ sở dữ liệu và thiết bị kết nối ngoại vi (DB & Connectivity Specialist)
> *"Việc nhồi nhét Số CCCD, Giới tính, Địa chỉ của khách vào cột `Description` dưới dạng chuỗi ghép là một thảm họa cơ sở dữ liệu. Nó phá vỡ tính toàn vẹn của dữ liệu và triệt tiêu khả năng tối ưu hóa chỉ mục (Indexing) phục vụ tìm kiếm sau này."*

### 7. 🛡️ Chuyên gia về bảo mật và an ninh mạng (Security Expert)
> *"Dữ liệu CCCD gắn chip chứa thông tin cá nhân cực kỳ nhạy cảm (PII). Việc lưu trữ địa chỉ chi tiết và số định danh công dân dưới dạng văn bản thô (Plain Text) không mã hóa trong SQLite vi phạm nghiêm trọng Luật An ninh mạng và Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân."*

### 8. 🏫 Nhà giáo dục (Educator)
> *"Môi trường sư phạm đòi hỏi sự trang trọng. Khi khách đến trường, họ cần cảm nhận được sự chuyên nghiệp ngay từ khâu đón tiếp. Giao diện đăng ký khách cần gọn gàng, thuần Việt và thể hiện tính hiếu khách của nhà trường."*

### 9. 🎓 Nhà Quản lý hiệu trưởng nhà trường (School Principal)
> *"Tôi cần biết chính xác hôm nay có bao nhiêu đoàn khách đến trường làm việc, thuộc những đơn vị nào để tiện chỉ đạo đón tiếp. Biểu đồ thống kê khách tham quan theo tháng sẽ giúp tôi đánh giá tần suất giao lưu hợp tác quốc tế và trong nước của trường."*

### 10. 👥 Trưởng bộ môn của trường (Head of Department)
> *"Nên có tính năng thông báo tự động. Khi khách quét thẻ CCCD ở cổng và chọn gặp giáo viên A của tổ bộ môn, hệ thống nên gửi một thông báo đẩy (Push Notification) đến điện thoại hoặc tài khoản TeacherHub của giáo viên đó để họ chuẩn bị tiếp khách."*

### 11. 👩‍🏫 Giáo viên ưu tú với nhiều kinh nghiệm (Elite Teacher)
> *"Nhiều khi chúng tôi hẹn phụ huynh đến trao đổi tình hình học tập của học sinh. Nếu bảo vệ phải gõ tay thông tin phụ huynh quá lâu sẽ gây trễ giờ họp. Việc quét CCCD rất tốt nhưng hệ thống cần cho phép giáo viên đăng ký lịch hẹn trước để rút ngắn thời gian làm thủ tục tại cổng."*

### 12. 👦 Học sinh (Student)
> *"Chúng em muốn trường học luôn an toàn. Nếu hệ thống ghi nhận chính xác những ai vào trường và cấp thẻ đeo có màu sắc phân biệt cho khách (ví dụ: thẻ màu vàng), chúng em sẽ dễ dàng nhận biết người lạ để cảnh giác."*

### 13. 🧹 Nhân viên nhà trường - Cán bộ Văn phòng (School Staff)
> *"Tôi muốn hệ thống có nút 'Xuất Excel nhật ký khách' theo ngày hoặc tuần để làm báo cáo hành chính gửi Ban Giám hiệu thay vì phải ngồi chép tay từ màn hình máy tính của bảo vệ."*

### 14. 🎮 Gamer giỏi (Pro Gamer - Gamification Expert)
> *"Trải nghiệm quét thẻ CCCD quá tẻ nhạt. Khi quét thành công, màn hình nên hiển thị một hiệu ứng chuyển động mở khóa (Unlock animation) với màu xanh lá cây dịu nhẹ kèm một âm thanh xác nhận vui tai, thay vì chỉ hiện một pop-up thông báo tĩnh bắt bấm OK."*

### 15. 🏢 Cán bộ quản lý của phòng giáo dục (District Admin)
> *"Dữ liệu báo cáo khách ra vào trường cần được chuẩn hóa để khi phòng giáo dục kiểm tra công tác an ninh trường học định kỳ, nhà trường có thể kết xuất số liệu nhanh chóng, chính xác theo đúng biểu mẫu quy định."*

### 16. 🏫 Chuyên viên của sở giáo dục (Provincial Specialist)
> *"Báo cáo xuất ra phải có đầy đủ tiêu đề quốc hiệu, tên trường, ngày tháng và phần ký tên của người lập biểu, hiệu trưởng duyệt theo đúng thể thức văn bản hành chính Việt Nam."*

### 17. 🔬 Nhà khoa học giáo dục (Educational Scientist)
> *"Việc số hóa công tác quản lý khách giúp giảm thiểu rác thải giấy tờ (sổ ký tên truyền thống), giáo dục học sinh về lối sống xanh và ứng dụng công nghệ số trong cuộc sống hàng ngày. Thiết kế giao diện cần thân thiện, tránh cảm giác giám sát nặng nề."*

---

## ═══ PHẦN III: BẢNG ĐỐI CHIẾU TRƯỚC VS SAU CẢI TIẾN ═══

| Phân hệ / Tiêu chí | Trạng thái Hiện tại (Lỗi v4.1) | Giải pháp Đề xuất (Sau cải tiến) | Lợi ích Sư phạm & Kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Cấu trúc lưu trữ dữ liệu** | Gom tất cả thông tin CCCD, giới tính, địa chỉ vào một chuỗi thô ở cột `Description`. | Tách thành các trường riêng: `CccdNumber`, `Address`, `Gender`, `HostPerson`, `HostDepartment`. | Tối ưu hóa truy vấn CSDL, cho phép lọc báo cáo chi tiết theo thuộc tính khách. |
| **Báo cáo tổng hợp** | Hoàn toàn không có dữ liệu khách tham quan trên giao diện `ReportsView`. | Bổ sung biểu đồ phân bổ khách tham quan và thẻ đếm `Tổng số khách trong ngày`. | Giúp Ban Giám hiệu kiểm soát toàn diện lưu lượng người ra vào trường. |
| **Hiệu ứng TextBox quét QR** | Nhấp nháy viền màu cam liên tục khi mất focus gây mỏi mắt và phân tâm. | Loại bỏ hiệu ứng nhấp nháy khi mất focus; đổi thành viền xanh dương nhẹ khi có focus. | Giảm tải mỏi mắt cho bảo vệ trực ca, chuẩn hóa trải nghiệm UI chuyên nghiệp. |
| **Trường thông tin nghiệp vụ** | Thiếu thông tin người tiếp đón, mục đích làm việc và số thẻ khách được cấp. | Bổ sung hộp chọn `Mục đích`, textbox `Cán bộ tiếp đón` và `Số thẻ khách`. | Tăng cường an ninh trường học, dễ dàng định vị vị trí và mục đích của khách. |
| **Hiển thị văn bản (Wrapping)** | Cột chi tiết DataGrid bị cắt chữ do không tự động xuống dòng khi chuỗi địa chỉ quá dài. | Cấu hình `ElementStyle` with `TextWrapping="Wrap"` cho cột DataGrid. | Đảm bảo hiển thị đầy đủ 100% thông tin địa chỉ khách tham quan. |
| **Khả năng co giãn giao diện** | Thiếu `ScrollViewer` ở danh sách lịch sử gây mất nút và khuất hình ở màn hình nhỏ. | Bọc layout danh sách lịch sử trong một `ScrollViewer` chuyên dụng. | Hiển thị tốt trên các màn hình máy tính cũ tại phòng bảo vệ (1024x768). |
| **Chỉ dẫn sử dụng** | Không có khung hướng dẫn nghiệp vụ từng bước cho nhân viên bảo vệ. | Thêm hộp chỉ dẫn nhanh `💡 Chỉ dẫn nghiệp vụ bảo vệ` ở góc dưới form nhập liệu. | Giúp bảo vệ lớn tuổi dễ dàng vận hành hệ thống, hạn chế lỗi thao tác. |
| **An toàn thông tin khách** | Lưu thô toàn bộ thông tin CCCD nhạy cảm dưới dạng Plain Text trong SQLite. | Mã hóa trường `CccdNumber` và `Address` bằng thuật toán AES-256 (khóa bảo vệ qua DPAPI). | Tuân thủ Nghị định 13/2023/NĐ-CP về bảo vệ bí mật dữ liệu cá nhân. |

---

## ═══ PHẦN IV: CHECKSHEET KIỂM THỬ KHẮC PHỤC (VERIFICATION CHECKSHEET) ═══

*   [ ] **Kiểm tra bóc tách CCCD:** Quét thẻ CCCD gắn chip, xác nhận các ô nhập liệu `Số CCCD`, `Họ tên`, `Địa chỉ` được điền tự động vào các ô tương ứng, không còn gộp chung vào ô Mô tả.
*   [ ] **Kiểm tra nhấp nháy TextBox:** Kiểm tra ô quét QR khi không có focus, xác nhận viền hộp nhập liệu giữ nguyên màu xám mặc định, không còn nhấp nháy màu cam.
*   [ ] **Kiểm tra tích hợp Báo cáo:** Mở trang `Báo cáo tổng hợp`, xác nhận xuất hiện biểu đồ thống kê khách tham quan và thẻ đếm số lượng khách hiện có trong trường.
*   [ ] **Kiểm tra thông tin tiếp đón:** Thêm khách mới, xác nhận bắt buộc chọn `Mục đích viếng thăm` và nhập `Cán bộ tiếp đón` mới cho phép lưu.
*   [ ] **Kiểm tra hiển thị địa chỉ dài:** Nhập địa chỉ khách dài 150 ký tự, kiểm tra trên DataGrid lịch sử, xác nhận dòng tự động giãn độ cao và xuống dòng hiển thị trọn vẹn địa chỉ.
*   [ ] **Kiểm tra thanh cuộn màn hình nhỏ:** Thu nhỏ độ phân giải màn hình về 1024x768, xác nhận xuất hiện thanh cuộn đứng bên phải để cuộn xem toàn bộ danh sách lịch sử khách.
*   [ ] **Kiểm tra chỉ dẫn sử dụng:** Xác nhận có bảng chỉ dẫn từng bước (1. Đặt trỏ chuột -> 2. Quét thẻ -> 3. Chọn mục đích -> 4. Bấm Lưu) hiển thị rõ ràng trên giao diện nhập liệu.
*   [ ] **Kiểm tra bảo mật dữ liệu:** Truy cập trực tiếp tệp SQLite bằng công cụ DB Browser, kiểm tra bảng `SecurityLogs`, xác nhận cột Số CCCD và Địa chỉ hiển thị dưới dạng chuỗi mã hóa không thể đọc thô.

---

## ═══ KẾT LUẬN CỦA HỘI ĐỒNG THẨM ĐỊNH ═══

Hội đồng Chuyên gia đánh giá giao diện Báo cáo và Nhật ký khách tham quan của dự án QA SmartSchool **CHƯA ĐẠT tiêu chuẩn thiết kế sư phạm và ràng buộc kỹ thuật bản v4.1**. Các lỗi về nhấp nháy viền TextBox gây mỏi mắt, lưu trữ dữ liệu CCCD phi cấu trúc và thiếu thống kê khách trên trang Báo cáo Tổng hợp làm giảm hiệu năng quản lý an ninh của nhà trường.

**Khuyến nghị:** Đội ngũ phát triển cần thực hiện bóc tách cấu trúc cơ sở dữ liệu cho thông tin CCCD và loại bỏ hiệu ứng nhấp nháy viền cam TextBox trước kỳ kiểm định tiếp theo.

*Hội đồng Chuyên gia thống nhất ký duyệt báo cáo.*
