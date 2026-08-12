# Báo cáo Đánh giá Chuyên sâu: Cấu hình DB, Thư mục chia sẻ, Phân quyền & Bảo trì Hệ thống Admin
**Tài liệu thẩm định chất lượng chuyên biệt - Chuẩn QA SmartClass v4.1**  

---

## ═══ TỔNG QUAN HỆ THỐNG ═══

Báo cáo này tập trung đánh giá chuyên sâu các chức năng quản trị cấp cao trong phân hệ **Admin nhà trường & IT Console**, cụ thể bao gồm:
1.  **Cấu hình & Quản trị DB (Database Management):** Các chức năng Sao lưu (Backup), Khôi phục (Restore), Cài đặt gốc (Factory Reset) trong [AdminConsoleWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Admin/Views/AdminConsoleWindow.xaml.cs).
2.  **Cấu hình thư mục chia sẻ LAN (Shared Folder):** Cách thức cấu hình đường dẫn tài nguyên dùng chung trong [AppPaths.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Services/AppPaths.cs).
3.  **Hệ thống Phân quyền (RBAC Master Manager):** Giao diện thiết lập quyền truy cập cho nhân sự trường học tại [RolePermissionManagerView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/RolePermissionManagerView.xaml.cs).
4.  **Bảo trì hệ thống (Maintenance & Live Logs):** Tính năng theo dõi Log sự cố và giám sát mạng LAN.

Hội đồng Chuyên gia gồm 17 thành viên đã thực hiện rà soát nghiêm ngặt các mã nguồn và giao diện nêu trên dựa trên bộ quy chuẩn **QA SmartClass v4.1**.

---

## ═══ CÁC PHÁT HIỆN LỖI LẬP TRÌNH & ĐIỂM BẤT HỢP LÝ SƯ PHẠM ═══

### 1. Các lỗi Logic chương trình & Kỹ thuật nghiêm trọng (Technical & Logic Bugs)

*   **Lỗi đọc file cấu hình Học sinh (Notepad Hardcoded bug):**
    *   *Mã nguồn hiện tại (Dòng 229):* `var profile = Path.Combine(_baseDir, "Settings", "student_profile.json");`
    *   *Chi tiết lỗi:* Tên file cấu hình học sinh được lưu động theo mã máy và mã học sinh (Ví dụ: `student_profile_{hwId}_{studentCode}.json`). Việc gọi trực tiếp file cứng `student_profile.json` khiến nút bấm luôn báo lỗi *"File cấu hình chưa tồn tại"*, ngăn cản Admin xem và chỉnh sửa cấu hình học sinh trực tiếp.
*   **Lỗ hổng bảo mật: Ghi đè Database không cần mã PIN xác thực:**
    *   *Chi tiết lỗi:* Chức năng xóa sạch database (Factory Reset) yêu cầu nhập PIN qua `PinDialog` để bảo mật. Tuy nhiên, chức năng **Khôi phục từ Backup (Restore)** ghi đè trực tiếp lên file database hiện tại lại **KHÔNG** yêu cầu nhập mã PIN. Bất kỳ ai vào được màn hình IT Console đều có thể khôi phục một file cũ hoặc file hỏng để làm tê liệt hệ thống.
*   **Lỗ hổng leo thang quyền lực (Privilege Escalation):**
    *   *Chi tiết lỗi:* Quyền hạn của Admin Console được xác định bằng cách đọc file text thường `admin_role.txt` trong thư mục AppData. Bất kỳ học sinh hoặc giáo viên nào truy cập vào máy tính giáo viên đều có thể mở file này, sửa chữ `L3` thành `L1` (Vendor) để chiếm toàn quyền điều khiển hệ thống mà không cần nhập mật khẩu.
*   **Lỗi sập ứng dụng do khóa tiến trình khi Restore/Reset:**
    *   *Chi tiết lỗi:* Khi thực hiện Factory Reset hoặc Restore, hệ thống gọi `app.Database?.Dispose()` để đóng kết nối DB, sau đó thực hiện ghi đè hoặc xóa file. Tuy nhiên, nếu các UserControl khác (như Dashboard Admin hoặc Dashboard BGH) đang mở và nắm giữ các DbContext active, SQLite sẽ khóa file database (File Lock). Thao tác `File.Copy` hoặc `File.Delete` sẽ lập tức gây sập chương trình với ngoại lệ `IOException`.
*   **Thiếu cấu hình Thư mục chia sẻ (Shared Folder Path):**
    *   *Chi tiết lỗi:* Thư mục `SharedFiles` để nhận heartbeat từ máy học sinh và gửi bài giảng đang được chỉ định cứng trong code trỏ về AppData Local của máy giáo viên. Hệ thống hoàn toàn thiếu giao diện để Admin cấu hình đường dẫn này sang một phân vùng ổ đĩa dùng chung hoặc một đường dẫn mạng LAN (`\\Server\SharedFiles`), làm hệ thống không chạy được trên mạng LAN thực tế.

### 2. Các lỗi về Font chữ, Bố cục & Thẩm mỹ Sư phạm (UI/UX & Pedagogical Issues)

*   **Bảng màu "Hacker" neon gây mỏi mắt (Anti-pedagogical color scheme):**
    *   Màn hình Live Logs sử dụng chữ xanh neon sáng trên nền đen đậm `#020617` mô phỏng Terminal. Lối phối màu tương phản cực mạnh này gây nhức mỏi mắt nhanh chóng cho kỹ thuật viên và không phù hợp với chuẩn màu dịu nhẹ học đường của v4.1.
*   **Bố cục rời rạc, trống trải (Wasted Space Layout):**
    *   Vùng quản lý lưu trữ (Storage Tab) chỉ có 5 nút bấm xếp dọc một cách đơn điệu, để lại khoảng trống khổng lồ vô nghĩa ở bên phải. Giao diện thiếu sự chuyên nghiệp và chưa tạo được thiện cảm (WOW factor) cho người quản lý.
*   **Ngôn ngữ nửa Tây nửa Ta (Mixed Language Terms):**
    *   Phần cấu hình phân quyền sử dụng các nhãn tiếng Anh thô ghép với tiếng Việt: `"BGH (Leadership)"`, `"Giáo viên (Teacher)"`, `"Bảo vệ (Security)"`, `"TeacherHub"`, `"HealthRoom"`, `"Kitchen"`, `"SendPush"`, `"EditSystem"`. Điều này gây khó hiểu cho nhân sự nhà trường (đặc biệt là nhân viên bếp ăn hoặc bảo vệ khi tự xem phân quyền của mình).
*   **Thiếu hướng dẫn sử dụng từng bước (Missing User Guidance):**
    *   Vùng khôi phục dữ liệu hoặc Reset cài đặt gốc (Danger Zone) không có cảnh báo quy trình. Người dùng có thể nhấn nhầm nút dẫn đến mất dữ liệu mà không có hướng dẫn tự phục hồi.

---

## ═══ Ý KIẾN CHUYÊN BẠT TỪ HỘI ĐỒNG CHUYÊN GIA ═══

*   **Trưởng bộ phận thiết kế dự án (Project Design Lead):**
    > *"Cấu hình DB và phân quyền là trái tim của hệ thống quản trị. Việc lộ lọt leo thang quyền truy cập qua file text thường `admin_role.txt` là lỗi thiết kế sơ đẳng. Hệ thống phân quyền cần phải được mã hóa bằng DPAPI hoặc lưu trong SQLite có mật khẩu bảo vệ."*
*   **Quản lý IT (IT Manager):**
    > *"Không thể chấp nhận việc đường dẫn thư mục chia sẻ LAN `SharedFiles` bị gán cứng vào AppData của User hiện tại. Trường học cần cấu hình đường dẫn này đến ổ cứng mạng (NAS) hoặc một máy chủ trung tâm để tất cả máy trạm của học sinh có thể gửi dữ liệu về."*
*   **Chuyên gia bảo mật và an ninh mạng (Security Expert):**
    > *"Chức năng khôi phục (Restore) database bắt buộc phải đi qua lớp bảo vệ PIN của `PinDialog` tương tự như Factory Reset. Ghi đè database mà không kiểm tra chữ ký file hoặc mã PIN là một lỗ hổng bảo mật nghiêm trọng."*
*   **Nhà giáo dục & Nhà khoa học giáo dục (Educators):**
    > *"Phần phân quyền nhân sự dùng quá nhiều tiếng Anh. Cần dịch nghĩa rõ ràng các quyền nâng cao như 'EditSystem' thành 'Cài đặt và bảo trì hệ thống', 'SendPush' thành 'Gửi thông báo toàn trường' để giáo viên dễ dàng kiểm soát quyền hạn."*
*   **Chuyên gia thiết kế giao diện (UI/UX Designer):**
    > *"Bố cục của tab Quản lý lưu trữ cần được quy hoạch lại. Thay vì thả nổi các nút bấm đơn lẻ, hãy nhóm chúng vào các nhóm trực quan như '📁 Công cụ File', '💾 Quản trị Database' và '⚠️ Vùng Nguy Hiểm' để người dùng dễ định vị chức năng."*
*   **Chuyên gia CSDL & kết nối ngoại vi (DB Specialist):**
    > *"Cần thiết kế một luồng sao lưu tự động (Auto-backup) trước khi thực hiện cả Restore lẫn Factory Reset. Khi phát hiện file db bị khóa (File Lock), chương trình phải đưa ra thông báo rõ ràng yêu cầu đóng các tác vụ đang mở thay vì sập ngang."*

---

## ═══ BẢNG SO SÁNH TRƯỚC VÀ SAU CẢI TIẾN ĐỀ XUẤT ═══

| Tính năng | Hiện trạng (Lỗi phát hiện) | Đề xuất nâng cấp tối ưu (v4.1) | Ràng buộc kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Xác thực Console** | Đọc file text plaintext `admin_role.txt` cực kỳ mất an toàn. | Mã hóa dữ liệu phân quyền hoặc lưu trữ thông tin Role trong SQLite bảo mật. | Bảo mật dữ liệu hệ thống |
| **Bảo vệ PIN khi Restore** | Không yêu cầu nhập mã PIN khi khôi phục database. | Bắt buộc mở `PinDialog` xác thực trước khi tiến hành ghi đè dữ liệu. | Ràng buộc bảo mật v4.1 |
| **Đọc profile Học sinh** | Hardcode file `student_profile.json` gây lỗi file không tồn tại. | Đọc động theo `AppPaths.StudentProfileFile` (chứa hwId của thiết bị). | Logic nghiệp vụ |
| **Thư mục chia sẻ LAN** | Gán cứng đường dẫn cục bộ trong thư mục AppData. | Thêm mục cấu hình Đường dẫn thư mục chia sẻ LAN lưu vào SQLite. | Khả năng triển khai thực tế |
| **Phối màu Console Log** | Màu đen-xanh neon kiểu Hacker gây mỏi mắt. | Chuyển sang màu xám Slate đậm, chữ trắng nhạt hài hòa, dịu mắt. | Thẩm mỹ sư phạm v4.1 |
| **Ngôn ngữ Phân quyền** | Dùng tiếng Anh thô (TeacherHub, SendPush...). | Việt hóa 100% sang tiếng Việt dễ hiểu đối với giáo viên/nhân viên. | Chuẩn hóa ngôn ngữ |

---

## ═══ CHECKSHEET KIỂM THỬ KHẮC PHỤC DỰ KIẾN (TEST CHECKSHEET) ═══

*   [ ] **Test 1 (Bảo mật Role):** Thử tạo file `admin_role.txt` thủ công ngoài AppData và sửa giá trị, xác nhận hệ thống không cho phép leo thang quyền lên L1/L2 mà không qua bước đăng nhập bảo mật.
*   [ ] **Test 2 (Xác thực PIN Restore):** Bấm nút "Khôi phục từ Backup", xác nhận hộp thoại PIN xuất hiện yêu cầu nhập đúng PIN mới cho chọn file khôi phục.
*   [ ] **Test 3 (Đọc Profile động):** Học sinh đăng nhập -> Bấm nút "Mở file Cấu hình Học sinh" trên IT Console -> Mở đúng file `student_profile_{hwId}.json` bằng Notepad.
*   [ ] **Test 4 (Cấu hình Shared Folder):** Thay đổi đường dẫn thư mục chia sẻ sang ổ `D:\QA_SharedFiles` -> Bấm lưu -> Kiểm tra máy học sinh gửi heartbeat về đúng ổ D.
*   [ ] **Test 5 (Khóa file DB):** Thử mở đồng thời trang thống kê BGH và bấm Factory Reset, xác nhận ứng dụng đóng an toàn kết nối trước hoặc thông báo người dùng đóng các tab hoạt động để tránh sập app.
*   [ ] **Test 6 (Việt hóa Phân quyền):** Duyệt danh sách quyền hạn trong `RolePermissionManagerView`, kiểm tra 100% nhãn hộp kiểm (Checkboxes) hiển thị đúng tiếng Việt chuẩn.

---

*Báo cáo được đệ trình bởi Hội đồng Chuyên gia QA SmartClass.*
