# Kế hoạch Nâng cấp & Cải tiến Cổng Phụ huynh (Parent Portal) - Bản Cập nhật Cấu hình Master & Mở rộng Test Case
**Phân hệ Cổng Phụ huynh — Bộ tiêu chuẩn QA SmartClass v4.1**  
*Ngày cập nhật kế hoạch: 30 tháng 06 năm 2026*  

---

## ═══ MỤC TIÊU ═══
Khắc phục triệt để các lỗi logic kỹ thuật, đồng thời bổ sung các tùy chọn cấu hình cấp trường (Master Configurations) trong cơ sở dữ liệu `SystemSettings` để nhà trường tự quyết định phương thức vận hành dựa trên điều kiện kỹ thuật thực tế. Mở rộng bộ checksheet kiểm thử chi tiết ở từng bước đảm bảo tính bao phủ (Test coverage) cho lập trình viên.

---

## ═══ CÁC TÙY CHỌN CẤU HÌNH HỆ THỐNG (MASTER CONFIGURATIONS) ═══

Để đáp ứng điều kiện thực tế của từng trường học, hệ thống sẽ đọc cấu hình từ bảng `SystemSettings` trong cơ sở dữ liệu để điều chỉnh luồng nghiệp vụ của Cổng Phụ huynh:

| ID Cấu hình (Key) | Mô tả cấu hình | Giá trị mặc định | Giá trị tùy chọn |
| :--- | :--- | :--- | :--- |
| `ParentPortal_AuthSecureLevel` | Mức độ bảo mật khi đăng nhập dành cho phụ huynh | `"Simple"` | `"Simple"` (Chỉ cần Mã HS + SĐT)<br>`"High"` (Yêu cầu thêm mã PIN/Mật khẩu) |
| `ParentPortal_MessageRouting` | Định tuyến tin nhắn từ phụ huynh gửi lên | `"Homeroom"` | `"Homeroom"` (Tự động gửi cho GVCN lớp)<br>`"General"` (Gửi cho tài khoản "GV" dùng chung) |
| `ParentPortal_AttendanceCase` | Cách xử lý định dạng viết hoa/thường của trạng thái điểm danh | `"CaseInsensitive"` | `"CaseInsensitive"` (Không phân biệt hoa/thường)<br>`"Strict"` (So khớp tuyệt đối hoa/thường chuẩn hóa) |

---

## ═══ CHI TIẾT KẾ HOẠCH NÂNG CẤP & CẢI TIẾN ═══

### Nhiệm vụ 1: Sửa lỗi đúc kiểu (Casting Bug) hiển thị Badge thông báo ở Dashboard
*   **Yêu cầu:** Khắc phục lỗi ép kiểu sai từ `Border` thành `TextBlock` trong tệp code-behind, khôi phục hiển thị số lượng thông báo chưa đọc.
*   **Dữ liệu đầu vào:** Số lượng thông báo chưa đọc (`unread`) từ `NotificationService`.
*   **Dữ liệu đầu ra:** Badge hiển thị số màu đỏ nổi bật trên màn hình tổng quan, ẩn hoàn toàn khi `unread == 0`.
*   **Phương pháp thực hiện:**
    1.  Trong [ParentDashboardPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/ParentPortal/Views/ParentDashboardPage.xaml), đổi tên điều khiển `<Border x:Name="TxtNotifBadge">` thành `TxtNotifBadgeBorder`.
    2.  Đặt tên cho `<TextBlock>` bên trong Border đó là `TxtNotifBadgeText`.
    3.  Trong [ParentDashboardPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/ParentPortal/Views/ParentDashboardPage.xaml.cs#L52), cập nhật code tìm kiếm:
        *   Tìm `TxtNotifBadgeBorder` dạng `Border` để ẩn/hiện.
        *   Tìm `TxtNotifBadgeText` dạng `TextBlock` để gán số lượng.

---

### Nhiệm vụ 2: Hiệu chỉnh lệch lịch học và bỏ sót ngày Thứ 7 ở Thời khóa biểu
*   **Yêu cầu:** Sửa logic gán cột grid thời khóa biểu để Thứ 2 về đúng cột Thứ 2, Thứ 7 không bị bỏ sót.
*   **Dữ liệu đầu vào:** Lịch học từ bảng `TimetableEntries` có thuộc tính `DayOfWeek` (2 = Thứ 2, ..., 7 = Thứ 7).
*   **Dữ liệu đầu ra:** Tiết học hiển thị đúng ngày trong tuần trên Grid giao diện.
*   **Phương pháp thực hiện:**
    1.  Tại [ParentTimetablePage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/ParentPortal/Views/ParentTimetablePage.xaml.cs#L88), sửa công thức tính cột: `int col = entry.DayOfWeek - 1;`.
    2.  Sửa điều kiện lọc giới hạn cột: `if (col < 1 || col > 6 || row < 1 || row > 5) continue;` (cho phép các cột từ 1 đến 6 hoạt động, tương ứng Thứ 2 đến Thứ 7).

---

### Nhiệm vụ 3: Đồng nhất hóa định dạng dữ liệu Điểm danh & Tích hợp cấu hình Master
*   **Yêu cầu:** Khắc phục xung đột so sánh chuỗi viết thường và viết hoa giữa Dashboard và trang Điểm danh chi tiết, dựa vào cấu hình `ParentPortal_AttendanceCase`.
*   **Dữ liệu đầu vào:** Danh sách `AttendanceRecords` có trường `Status` không nhất quán hoa/thường; cấu hình `ParentPortal_AttendanceCase`.
*   **Dữ liệu đầu ra:** Số liệu thống kê tổng quan khớp 100% với danh sách ngày vắng mặt chi tiết.
*   **Phương pháp thực hiện:**
    1.  Lấy cấu hình `ParentPortal_AttendanceCase` từ DB (mặc định nếu lỗi là `"CaseInsensitive"`).
    2.  Nếu cấu hình là `"CaseInsensitive"`:
        *   Lọc ngày vắng: `a.Status.ToLower() != "present"`
        *   Trong `ParentAuthService.cs`: Đếm số ngày bằng `.ToLower() == "present"`, `.ToLower() == "absent"`, `.ToLower() == "late"`.
    3.  Nếu cấu hình là `"Strict"`:
        *   Lọc ngày vắng: `a.Status != "Present"`
        *   Đếm số ngày bằng `a.Status == "Present"`, `a.Status == "Absent"`, `a.Status == "Late"`.

---

### Nhiệm vụ 4: Đồng bộ ID phụ huynh thực tế và nâng cấp Leaderboard trong Family Game
*   **Yêu cầu:** Xóa bỏ mã cứng `ParentId = 1`, liên kết điểm số với ID thực tế của học sinh và hiển thị tên phụ huynh tương ứng trên Bảng xếp hạng.
*   **Dữ liệu đầu vào:** Thông tin `Student` hiện tại, bảng điểm `FamilyGameSessions`.
*   **Dữ liệu đầu ra:** Phiên chơi game lưu đúng ID phụ huynh, Bảng xếp hạng hiển thị danh tính rõ ràng dạng "PH em [Tên học sinh]".
*   **Phương pháp thực hiện:**
    1.  Trong [ParentShell.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/ParentPortal/ParentShell.xaml.cs#L69), truyền `_student.Id` thay vì `1` làm `parentId` khi tạo `FamilyGamePage`.
    2.  Trong [FamilyGamePage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/ParentPortal/Views/FamilyGamePage.xaml.cs#L40), sửa hàm `LoadLeaderboard` thực hiện truy vấn cơ sở dữ liệu để tìm thông tin học sinh tương ứng với `ParentId` (là `StudentId` liên kết 1-1) và lấy ra tên phụ huynh (`ParentName`) thực tế:
        ```csharp
        var student = _db.Students.Find(pid);
        string name = student != null ? $"PH em {student.FullName}" : $"Phụ huynh #{pid}";
        ```

---

### Nhiệm vụ 5: Việt hóa hoàn toàn bảng thông tin Học phí
*   **Yêu cầu:** Chuyển đổi dữ liệu tiếng Anh thô từ database thành tiếng Việt trước khi đưa lên DataGrid.
*   **Dữ liệu đầu vào:** Danh sách `TuitionRecords` từ database.
*   **Dữ liệu đầu ra:** Bảng học phí 100% tiếng Việt trên giao diện.
*   **Phương pháp thực hiện:**
    1.  Tại [ParentTuitionPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/ParentPortal/Views/ParentTuitionPage.xaml.cs#L26), chiếu (Project) dữ liệu sang danh sách đối tượng mới (anonymous type) và dịch các chuỗi trạng thái:
        *   `Status`: `"Paid"` -> `"Đã thanh toán"`, `"Unpaid"` -> `"Chưa thanh toán"`, `"Overdue"` -> `"Quá hạn"`.
        *   `PaymentMethod`: `"Cash"` -> `"Tiền mặt"`, `"Transfer"` -> `"Chuyển khoản"`, còn lại giữ nguyên.

---

### Nhiệm vụ 6: Khắc phục lỗi mã hóa font ký tự tiếng Việt (UTF-8 BOM)
*   **Yêu cầu:** Sửa các chuỗi bị vỡ chữ ở trang Đăng nhập và lưu file đúng chuẩn UTF-8.
*   **Dữ liệu đầu vào:** Tệp code [ParentLoginPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/ParentPortal/Views/ParentLoginPage.xaml.cs).
*   **Dữ liệu đầu ra:** Thông báo lỗi tiếng Việt hiển thị chính xác, rõ ràng.
*   **Phương pháp thực hiện:**
    1.  Sửa tất cả các ký tự bị lỗi hiển thị thành tiếng Việt chuẩn có dấu:
        *   `"Vui ḷng nh?p mă h?c sinh."` -> `"Vui lòng nhập mã học sinh."`
        *   `"Vui ḷng nh?p s? di?n tho?i ph? huynh."` -> `"Vui lòng nhập số điện thoại phụ huynh."`
        *   `"Không t́m th?y h?c sinh v?i mă này."` -> `"Không tìm thấy học sinh với mã này."`
        *   `"S? di?n tho?i không kh?p v?i h? so."` -> `"Số điện thoại không khớp với hồ sơ."`
    2.  Lưu tệp tin với tùy chọn mã hóa **UTF-8 with BOM**.

---

### Nhiệm vụ 7: Tích hợp cấu hình Master vào cơ chế Đăng nhập & Định tuyến tin nhắn Nhắn tin
*   **Yêu cầu:** Hỗ trợ đăng nhập PIN bảo mật cao và tự động chọn GVCN hoặc GV chung dựa trên cấu hình master.
*   **Dữ liệu đầu vào:** Bảng `SystemSettings`, bảng `Students`, tệp `ParentLoginPage.xaml.cs` và `ParentMessagesPage.xaml.cs`.
*   **Phương pháp thực hiện:**
    1.  **Xác thực mật khẩu/PIN (Đăng nhập):**
        *   Đọc cấu hình `ParentPortal_AuthSecureLevel` từ DB.
        *   Nếu giá trị là `"High"`: Trong giao diện đăng nhập hiển thị thêm ô nhập mã PIN/Mật khẩu và tiến hành băm so khớp với trường `PasswordHash` của bảng `Students` thay vì chỉ so khớp SĐT.
        *   Nếu giá trị là `"Simple"`: Chỉ so khớp Mã HS + SĐT phụ huynh (luồng cũ).
    2.  **Định tuyến tin nhắn:**
        *   Đọc cấu hình `ParentPortal_MessageRouting` từ DB.
        *   Nếu là `"Homeroom"`: Tự động tra cứu GVCN trong DB:
            ```csharp
            var gvcn = _db.ClassRosters.FirstOrDefault(r => r.ClassName == _student.ClassName && r.IsActive)?.TeacherName ?? "GV";
            ```
            và gán `ReceiverId = gvcn;`.
        *   Nếu là `"General"`: Gán `ReceiverId = "GV";` (luồng chung).
    3.  **Đồng bộ Badge trên Sidebar:**
        *   Hàm `UpdateBadge` đếm tin nhắn chưa đọc gửi tới `PH_{StudentCode}` (từ GV) cộng với thông báo gửi tới `parent_{StudentId}` (từ hệ thống) để cập nhật tổng số Badge.

---

### Nhiệm vụ 8: Bổ sung chỉ dẫn sử dụng (Onboarding) và Hướng dẫn thanh toán học phí
*   **Yêu cầu:** Thêm hướng dẫn nhập liệu, thông tin tài khoản ngân hàng chuyển khoản kèm QR Code mockup và luật chơi Family Game.
*   **Phương pháp thực hiện:**
    1.  **Trang Đăng nhập:** Bổ sung nhãn gợi ý nhỏ bên dưới các TextBox đăng nhập.
    2.  **Trang Học phí:** Thiết kế vùng thông tin tài khoản chuyển khoản của trường ngay dưới DataGrid, kèm theo ảnh mockup VietQR cho phụ huynh quét thanh toán nhanh.
    3.  **Trang Family Game:** Thêm panel luật chơi hiển thị đầu tiên trước khi vào quiz, kèm nút "Bắt đầu chơi".

---

## ═══ BỘ CHECKSHEET KIỂM THỬ TOÀN DIỆN (COMPREHENSIVE TEST CASES) ═══

Để coder không thể làm sai, bộ checksheet kiểm thử phải bao phủ tất cả các kịch bản biên (Edge Cases), logic hệ thống và kiểm thử tích hợp:

### 1. Checks đăng nhập & Phân quyền bảo mật (Nhiệm vụ 6 & 7)
*   `[ ]` **TC_AUTH_01 (Trường trống):** Để trống Mã học sinh hoặc Số điện thoại. Kết quả mong đợi: Hiển thị lỗi tiếng Việt có dấu chuẩn: "Vui lòng nhập mã học sinh." hoặc "Vui lòng nhập số điện thoại phụ huynh."
*   `[ ]` **TC_AUTH_02 (Không tìm thấy HS):** Nhập sai mã học sinh. Kết quả mong đợi: Hiển thị lỗi: "Không tìm thấy học sinh với mã này."
*   `[ ]` **TC_AUTH_03 (Sai SĐT):** Nhập đúng mã học sinh nhưng sai SĐT phụ huynh. Kết quả mong đợi: Hiển thị lỗi: "Số điện thoại không khớp với hồ sơ."
*   `[ ]` **TC_AUTH_04 (Cấu hình High):** Đặt `ParentPortal_AuthSecureLevel` = `"High"`. Kiểm tra xem ô mật khẩu/PIN có hiển thị không. Thử nhập sai mật khẩu => Phải báo lỗi và không cho đăng nhập.
*   `[ ]` **TC_AUTH_05 (Cấu hình Simple):** Đặt `ParentPortal_AuthSecureLevel` = `"Simple"`. Kiểm tra xem ô nhập mật khẩu có ẩn đi không. Thử đăng nhập chỉ bằng Mã HS + SĐT => Phải đăng nhập thành công.

### 2. Checks màn hình Tổng quan & Cảnh báo (Nhiệm vụ 1 & 3)
*   `[ ]` **TC_DASH_01 (Ép kiểu Badge):** Khi có thông báo chưa đọc, kiểm tra xem Badge màu đỏ có hiển thị số chính xác không. Ứng dụng không được crash ngầm.
*   `[ ]` **TC_DASH_02 (Không có thông báo):** Đánh dấu tất cả thông báo là đã đọc. Kết quả mong đợi: Badge trên Sidebar và Dashboard phải ẩn hoàn toàn (`Visibility = Collapsed`).
*   `[ ]` **TC_DASH_03 (Cảnh báo sớm):** Học sinh thuộc danh sách cảnh báo (IsAtRisk = true). Kết quả mong đợi: PanelWarning màu đỏ xuất hiện nổi bật trên Dashboard kèm theo nội dung lý do rõ ràng.
*   `[ ]` **TC_DASH_04 (Không có cảnh báo):** Học sinh học lực tốt, chuyên cần tốt. Kết quả mong đợi: PanelWarning ẩn hoàn toàn.

### 3. Checks Thời khóa biểu chi tiết (Nhiệm vụ 2)
*   `[ ]` **TC_TKB_01 (Kiểm tra lệch cột):** Đối chiếu môn học Thứ 2 trong CSDL. Trên giao diện, môn học đó phải xuất hiện ở đúng cột "Thứ 2" (Cột 1), không được lệch sang cột "Thứ 3".
*   `[ ]` **TC_TKB_02 (Thời khóa biểu Thứ 7):** Tạo một tiết học vào sáng thứ 7 trong CSDL. Kết quả mong đợi: Tiết học đó phải hiển thị đầy đủ và rõ ràng trên Grid thời khóa biểu ở cột "Thứ 7" (Cột 6).
*   `[ ]` **TC_TKB_03 (Thời khóa biểu trống):** Lớp học chưa được thiết lập TKB trong CSDL. Kết quả mong đợi: Hiển thị bảng cảnh báo màu đỏ sư phạm: "⚠️ Thời khóa biểu lớp hiện chưa được cập nhật. Vui lòng liên hệ Giáo viên chủ nhiệm..." để phụ huynh không bị hiểu nhầm.

### 4. Checks Lịch sử Điểm danh (Nhiệm vụ 3)
*   `[ ]` **TC_ATT_01 (Case-Insensitive):** Đặt cấu hình `ParentPortal_AttendanceCase` = `"CaseInsensitive"`. Trong CSDL lưu trạng thái viết thường `"present"`. Kết quả mong đợi: Trang Lịch sử điểm danh không hiển thị ngày này trong danh sách ngày vắng mặt. Số ngày có mặt trên Dashboard hiển thị chính xác.
*   `[ ]` **TC_ATT_02 (Strict Case):** Đặt cấu hình `ParentPortal_AttendanceCase` = `"Strict"`. CSDL lưu trạng thái viết thường `"present"`. Kết quả mong đợi: Phải coi đây là vắng (nếu quy chuẩn yêu cầu viết hoa `"Present"`).
*   `[ ]` **TC_ATT_03 (Việt hóa trạng thái):** Bản ghi điểm danh có trạng thái `"absent"` hoặc `"excused"`. Kết quả mong đợi: Bảng danh sách ngày vắng mặt hiển thị nhãn dịch tương ứng: "Vắng không phép" hoặc "Vắng có phép" thay vì hiển thị chữ tiếng Anh thô.

### 5. Checks Bảng điểm & Biểu đồ tiến bộ (Nhiệm vụ 4)
*   `[ ]` **TC_GRADE_01 (Màu sắc điểm số):** Điểm môn học >= 8.0 hiển thị màu xanh lá. Điểm từ 5.0 đến 7.9 hiển thị màu xanh dương. Điểm < 5.0 hiển thị màu đỏ.
*   `[ ]` **TC_GRADE_02 (Biểu đồ cột ngang):** Thay đổi kích thước cửa sổ ứng dụng (Zoom/Resize). Biểu đồ tiến bộ của con phải co giãn mượt mà, không bị tràn viền hay che khuất chữ.
*   `[ ]` **TC_GRADE_03 (So sánh trung bình lớp):** Vạch kẻ màu xanh lam đại diện cho điểm Trung bình lớp phải hiển thị đúng vị trí tương đối so với cột điểm của học sinh.

### 6. Checks Học phí & Hướng dẫn thanh toán (Nhiệm vụ 5 & 8)
*   `[ ]` **TC_FEES_01 (Việt hóa học phí):** Bảng học phí hiển thị trạng thái tiếng Việt ("Đã thanh toán" / "Chưa thanh toán" / "Quá hạn") thay vì "Paid" / "Unpaid" / "Overdue".
*   `[ ]` **TC_FEES_02 (Thông tin chuyển khoản):** Bảng hướng dẫn chuyển khoản hiển thị chính xác: Số tài khoản, Ngân hàng thụ hưởng và Cú pháp chuyển khoản chuẩn sư phạm.
*   `[ ]` **TC_FEES_03 (Mockup QR Code):** Có ảnh hiển thị biểu tượng VietQR rõ nét bên cạnh hướng dẫn để hỗ trợ thao tác nhanh.

### 7. Checks Family Game tương tác (Nhiệm vụ 4 & 8)
*   `[ ]` **TC_GAME_01 (Onboarding):** Bảng hướng dẫn luật chơi hiển thị đầu tiên khi phụ huynh mở trang. Quiz chỉ bắt đầu khi bấm "Bắt đầu chơi".
*   `[ ]` **TC_GAME_02 (Định danh Bảng xếp hạng):** Chơi game đạt điểm số cao. Xem leaderboard. Danh tính của lượt chơi phải hiển thị dạng: "PH em [Tên học sinh]", không được hiển thị "Phụ huynh #1" chung chung cho mọi tài khoản.
*   `[ ]` **TC_GAME_03 (Không trùng lặp):** Lượt chơi thứ 2 lưu đúng số điểm mới, không ghi đè điểm của phụ huynh khác.

### 8. Checks Hộp thư & Nhận thông báo Nhắn tin (Nhiệm vụ 7)
*   `[ ]` **TC_MSG_01 (Định tuyến GVCN):** Đăng nhập phụ huynh lớp 10A3. Bấm nhắn tin và gửi. Kiểm tra trong CSDL: Bản ghi tin nhắn mới phải có `ReceiverId` chứa tên GVCN lớp 10A3 (ví dụ: "Cô Lan").
*   `[ ]` **TC_MSG_02 (Nhận thông báo chat):** Giáo viên gửi tin nhắn phản hồi tới phụ huynh qua CSDL. Kết quả mong đợi: Cổng phụ huynh hiển thị Badge thông báo tin nhắn chưa đọc màu đỏ ngay trên Sidebar, số đếm tăng lên chính xác.

---

## ═══ KẾ HOẠCH XÁC MINH & KIỂM THỬ (VERIFICATION PLAN) ═══

### Kiểm thử tự động (Automated Tests)
*   Chạy toàn bộ Integration Tests để đảm bảo logic của các service nền không bị ảnh hưởng:
    `dotnet test QASmartClass.Tests`

### Kiểm thử thủ công (Manual Verification)
*   Thực hiện toàn bộ các test case thuộc checksheet trên môi trường Local trước khi tiến hành đóng gói cài đặt.
