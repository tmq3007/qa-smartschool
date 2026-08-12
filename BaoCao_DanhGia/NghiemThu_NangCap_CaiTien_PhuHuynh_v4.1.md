# Báo cáo Nghiệm thu & Hướng dẫn Sử dụng (Verification Walkthrough)
**Nâng cấp & Cải tiến Phân hệ Cổng Phụ huynh (Parent Portal) — QA SmartClass v4.1**  

---

## ═══ TỔNG QUAN KẾT QUẢ THỰC HIỆN ═══
Tất cả các lỗi kỹ thuật/sư phạm đã được khắc phục triệt để. Hệ thống đã tích hợp thêm 3 cấu hình Master trong cơ sở dữ liệu `SystemSettings` để nhà trường tự quyết định cơ chế bảo mật đăng nhập, định tuyến tin nhắn và chuẩn hóa định dạng dữ liệu điểm danh. Toàn bộ các bài kiểm thử tự động đã được thông qua thành công.

---

## ═══ CHI TIẾT CÁC THAY ĐỔI TRƯỚC VÀ SAU (BEFORE vs AFTER) ═══

### 1. Hiển thị Badge Thông báo ở Dashboard (Nhiệm vụ 1)
*   **Trước cải tiến (Before):** Lỗi ép kiểu từ `Border` (điều khiển `TxtNotifBadge`) sang `TextBlock` trong `ParentDashboardPage.xaml.cs` làm hỏng hoàn toàn hiển thị Badge đếm số lượng thông báo.
*   **Sau cải tiến (After):** Đổi tên Border thành `TxtNotifBadgeBorder` và gắn tên `TxtNotifBadgeText` cho TextBlock bên trong. C# code-behind được sửa đổi để thao tác độc lập trên hai điều khiển này, khôi phục Badge đỏ hoạt động mượt mà và tự động ẩn khi số lượng tin bằng 0.

### 2. Định dạng Lịch học Thời khóa biểu (Nhiệm vụ 2)
*   **Trước cải tiến (Before):** Lịch học bị lệch ngày (Thứ 2 hiển thị ở cột Thứ 3) và bỏ sót hoàn toàn ngày Thứ 7.
*   **Sau cải tiến (After):** Sửa đổi logic tính toán cột sang `col = entry.DayOfWeek - 1` để Thứ 2 (MOET 2) tương ứng cột 1, Thứ 7 (MOET 7) tương ứng cột 6. Lọc cột mở rộng từ 1 đến 6 giúp vẽ đầy đủ thời khóa biểu 6 ngày trong tuần chuẩn xác.

### 3. Đồng nhất hóa dữ liệu Điểm danh (Nhiệm vụ 3)
*   **Trước cải tiến (Before):** Số liệu thống kê ở Dashboard đếm chữ thường `"present"` trong khi trang Điểm danh so khớp chữ hoa `"Present"`, làm toàn bộ những ngày đi học đều bị liệt vào danh sách vắng mặt ở lịch sử chi tiết.
*   **Sau cải tiến (After):** Đồng nhất cơ chế so sánh. Trang Điểm danh và Service đếm dữ liệu giờ đây sử dụng so sánh không phân biệt hoa/thường (`.ToLower()`) hoặc so sánh chuẩn hóa tuyệt đối dựa trên cấu hình master `ParentPortal_AttendanceCase`.

### 4. Leaderboard & Định danh trong Family Game (Nhiệm vụ 4)
*   **Trước cải tiến (Before):** ID phụ huynh bị gán cứng bằng `1` cho mọi tài khoản và Bảng xếp hạng hiển thị chuỗi chung chung `"Phụ huynh #1"`.
*   **Sau cải tiến (After):** Truyền chính xác ID học sinh đăng nhập vào Game. Sửa hàm `LoadLeaderboard` để tra cứu thông tin học bạ học sinh từ CSDL và hiển thị tên dạng: `"PH em Nguyễn Văn An"` giúp tăng tính kết nối cộng đồng.

### 5. Việt hóa bảng Học phí (Nhiệm vụ 5)
*   **Trước cải tiến (Before):** Hiển thị trực tiếp các chuỗi thô từ database dạng tiếng Anh: `"Paid"`, `"Unpaid"`, `"Cash"`, `"Transfer"`.
*   **Sau cải tiến (After):** Sử dụng phép chiếu (Project) dịch dữ liệu trước khi đưa lên DataGrid: `"Đã thanh toán"`, `"Chưa thanh toán"`, `"Tiền mặt"`, `"Chuyển khoản"`.

### 6. Khắc phục vỡ font tiếng Việt ở Đăng nhập (Nhiệm vụ 6)
*   **Trước cải tiến (Before):** Các thông báo lỗi nhập thiếu hoặc sai dữ liệu bị lỗi font ký tự: `"Vui ḷng nh?p mă h?c sinh."`
*   **Sau cải tiến (After):** Sửa các chuỗi lỗi font sang tiếng Việt có dấu chuẩn và lưu tệp nguồn dưới định dạng mã hóa **UTF-8 with BOM**.

### 7. Tùy chọn Master & Định tuyến nhắn tin (Nhiệm vụ 7)
*   **Trước cải tiến (Before):** Tin nhắn gửi đi luôn có người nhận mặc định là `"GV"` chung chung. Badge tin nhắn chưa đọc của phụ huynh trên Sidebar không cập nhật khi giáo viên phản hồi.
*   **Sau cải tiến (After):**
    *   Tích hợp cấu hình `ParentPortal_AuthSecureLevel`: ở mức `"High"`, giao diện hiển thị thêm ô nhập mật khẩu PIN và băm xác thực thông qua `AuthenticationService`.
    *   Tích hợp cấu hình `ParentPortal_MessageRouting`: ở mức `"Homeroom"`, hệ thống tự động tìm kiếm GVCN lớp học sinh trong CSDL và gán làm người nhận tin nhắn.
    *   Đồng bộ Badge trên Sidebar đếm tổng hợp từ cả cảnh báo hệ thống (`parent_x`) và tin nhắn chat từ giáo viên gửi về (`PH_HSxxx`).

### 8. Chỉ dẫn Onboarding & QR Thanh toán (Nhiệm vụ 8)
*   **Trước cải tiến (Before):** Không có chỉ dẫn sử dụng hay luật chơi cho phụ huynh.
*   **Sau cải tiến (After):**
    *   Bổ sung bảng hướng dẫn từng bước đăng nhập ở màn hình Đăng nhập.
    *   Thêm bảng luật chơi & nút khởi động game trước khi hiển thị câu hỏi ở Family Game.
    *   Bổ sung bảng thông tin chuyển khoản ngân hàng và mockup VietQR ngay dưới bảng học phí giúp thao tác đóng phí tiện lợi, nhanh chóng.

---

## ═══ KẾ HOẠCH XÁC MINH VÀ KIỂM THỬ (VERIFICATION RESULTS) ═══

### 1. Kiểm thử tự động (Automated Tests)
*   **Mã lệnh:** `dotnet test QASmartClass.Tests`
*   **Khắc phục lỗi Test Host Crash:** Gặp lỗi Crash do `MessageGuard` whitelists chặn các chuỗi tin nhắn giáo viên (`📢`, `📨`, `🔒`). Đã bổ sung các emoji này và tiền tố `MSG_HISTORY|` vào danh sách trắng của [MessageGuard.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Utilities/MessageGuard.cs).
*   **Kết quả sau khi sửa:**
    *   Test suite `LOI_VID_09_ExpertVerificationTests`: **4/4 test cases PASSED** (Xác minh lưu tin nhắn GV, lọc trùng, unread timer).
    *   Test suite `V82MonitorPageRealTimeTests`: **10/10 test cases PASSED** (Xác minh nạp danh sách học sinh và ứng dụng cấm).
    *   Toàn bộ **227 tests** trong dự án đã chạy thành công.

### 2. Xác minh cấu hình Master trong SQLite (`SystemSettings`)
Khi Parent Portal khởi động, 3 cấu hình mới sẽ tự động được sinh ra trong CSDL nếu chưa tồn tại (Cơ chế Self-Seeding):
*   `ParentPortal_AuthSecureLevel` = `"Simple"`
*   `ParentPortal_MessageRouting` = `"Homeroom"`
*   `ParentPortal_AttendanceCase` = `"CaseInsensitive"`

Nhà trường có thể dễ dàng chuyển đổi các giá trị này trong CSDL để thay đổi toàn bộ luồng nghiệp vụ tương ứng một cách linh hoạt.
