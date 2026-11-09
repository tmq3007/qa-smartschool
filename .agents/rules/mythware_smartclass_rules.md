---
trigger: always_on
---

# 🖥️ QUY CHUẨN PHÂN HỆ SMART CLASS CHUẨN MYTHWARE (PC & ANDROID SCREEN SHARE / MANAGEMENT RULES)

> **Phạm vi áp dụng:** Bộ quy tắc này **CHỈ ÁP DỤNG CHO PHÂN HỆ `SMART CLASS`** (`QASmartClass/Classroom/`, `QASmartClass/StudentClient/`, `QASmartClass/Forms/Form0_LoginSelection*`, và `QASmartClass.Android/`).
> **TUYỆT ĐỐI KHÔNG** dùng quy tắc này để sửa đổi, cắt gọn hay can thiệp vào Bảng viết tương tác **`SMART TOUCH`** (`Form2_MainDashboard` và `QASmartTouch.csproj`).

---

## 1. [RULE] QSC_SMARTTOUCH_IMMUNITY: Bất Khả Xâm Phạm Đối Với `SMART TOUCH`
1. **Giữ nguyên 100% `SMART TOUCH`:**
   * Mọi tác vụ thiết kế lại giao diện hoặc tinh gọn `QASmartClass` theo chuẩn Mythware **tuyệt đối không được sửa đổi** giao diện, thanh công cụ, học cụ STEM, mô hình 3D, hay luồng cảm ứng/bút viết của **`SMART TOUCH`** (`Form2_MainDashboard.*`, `Form2_*`, `WhiteboardCore/`, `Controls/`).
   * Bảng viết `SMART TOUCH` bên trong `QASmartClass` luôn giữ nguyên 100% tính năng và giao diện 
2. **Bảo toàn Chuyển đổi 3 Chế độ (`SMART CLASS` $\leftrightarrow$ `SMART TOUCH` $\leftrightarrow$ `DESKTOP`):**
   * Thanh chuyển đổi chế độ nổi (`FloatingModeBar`) luôn hoạt động xuyên suốt để Giáo viên có thể vừa phát màn hình (`Screen Broadcast`) vừa chuyển sang bảng viết `SMART TOUCH` hoặc `DESKTOP` giảng bài.

---

## 2. [RULE] QSC_MYTHWARE_CORE_SCOPE: Tập Trung 100% Vào Chia Sẻ & Quản Lý Màn Hình (Mythware Standard)
Phân hệ **`SMART CLASS`** bỏ qua toàn bộ các chức năng quản lý trường học/học vụ rườm rà (ERP/LMS) và chỉ tập trung vào **7 nhóm chức năng cốt lõi của Mythware Classroom Management**:
1. **📺 Chiếu màn hình Giáo viên (`Screen Broadcast`):**
   * Chiếu toàn màn hình (bao gồm cả khi Giáo viên đang viết trên `SMART TOUCH` hoặc thao tác ngoài `DESKTOP`), chiếu 1 cửa sổ ứng dụng hoặc vùng chọn xuống toàn bộ máy Học sinh (**Windows PC & Android**).
   * Hỗ trợ đóng băng hình chiếu (`Freeze`) và tùy chỉnh chất lượng luồng phát (Mượt FPS cao / Rõ nét độ phân giải cao).
2. **👨‍🎓 Học sinh trình bày (`Student Demonstration`):**
   * Chọn 1 máy Học sinh (PC hoặc Android) để phát màn hình của học sinh đó cho cả lớp cùng xem.
3. **🖥️ Giám sát lưới màn hình trực tiếp (`Live Screen Monitor Grid`):**
   * Hiển thị lưới ảnh màn hình thời gian thực (`Live Thumbnails`) của tất cả máy PC và thiết bị Android đang kết nối.
   * Phân biệt rõ biểu tượng thiết bị (`🪟 PC` / `🤖 Android`), tên máy/học sinh, địa chỉ IP và trạng thái (`Online`, `✋ Giơ tay`, `🔒 Đang khóa`).
4. **🖱️ Xem to & Điều khiển từ xa (`Remote View & Control`):**
   * Phóng to xem trực tiếp màn hình 1 học sinh, chụp ảnh màn hình (`Snapshot`), và điều khiển chuột/bàn phím từ xa.
5. **🔒 Khóa màn hình & Im lặng (`Black Screen / Silence Lock`):**
   * Khóa toàn bộ hoặc từng máy học sinh đang chọn bằng màn hình thông báo *"Giáo viên đang giảng bài - Vui lòng chú ý lắng nghe"* và mở khóa tức thì (`Unlock`).
6. **🌐 Mở Website & Quản lý nguồn (`Web Push & Power Control`):**
   * Đẩy nhanh 1 đường dẫn URL tự mở trên máy học sinh; hỗ trợ Tắt máy (`Shutdown`), Khởi động lại (`Restart`), Đăng xuất (`Logoff`) cho dàn máy PC.
7. **📤 Phát File / 📥 Thu Bài & 💬 Tương tác nhanh (`File Transfer, Hand-Raise & Chat`):**
   * Gửi tài liệu xuống các máy học sinh, thu bài làm về máy Giáo viên, gửi thông báo nổi và nhận tín hiệu **✋ Giơ tay** từ PC/Android.

---

## 3. [RULE] QSC_COMMENT_NOT_DELETE: Ẩn Bằng Comment — Không Xóa Mã Nguồn Cũ
* Khi ẩn hoặc tách các trang ERP/LMS cũ (Thời khóa biểu, Soạn giáo án cũ, Kiểm tra Quiz, Bài tập về nhà, Thư viện, Canteen, Nhân sự, Ban giám hiệu, Hồ sơ học bạ, Cổng đăng nhập 11 vai trò...), **BẮT BUỘC dùng comment** (`// [MYTHWARE_LITE]` trong C# hoặc `<!-- [MYTHWARE_LITE] ... -->` trong XAML/XML).
* **Tuyệt đối không xóa trắng** các hàm hoặc tệp mã nguồn cũ để có thể khôi phục hoặc tham khảo lại bất cứ lúc nào.

---

## 4. [RULE] QSC_ZERO_DB_DEPENDENCY: Kết Nối Mạng LAN Trực Tiếp (Zero-Config LAN/Wi-Fi)
1. **Cắm mạng là chạy (Zero-Config Discovery):**
   * Máy Giáo viên và Học sinh (PC & Android) tự động tìm thấy nhau qua **UDP Broadcast (`Cổng 29876`)** hoặc nhập trực tiếp địa chỉ IP máy Giáo viên để kết nối vào **TCP Control (`29877`)**, **Screen Broadcast (`29878`)**, và **File Transfer (`29879`)**.
2. **Không phụ thuộc Database Lớp học / Thời khóa biểu:**
   * Mọi thiết bị PC và Android vừa kết nối mạng LAN/Wi-Fi phải xuất hiện ngay lập tức trên Lưới giám sát màn hình (`Live Screen Grid`) của Giáo viên mà **không cần** tạo lớp học trong SQLite, không cần xếp thời khóa biểu hay đăng nhập tài khoản học sinh.

---

## 5. [RULE] QSC_CLEAN_SLATE_UI: Chuẩn Giao Diện Mới Cho PC & Android
1. **Giao diện Máy Giáo Viên (`ClassroomShell`):**
   * Thiết kế trên **1 cửa sổ duy nhất** theo phong cách Mythware hiện đại:
     * **Top Command Ribbon:** Thanh công cụ ngang trên cùng chứa các nút chức năng lớn, rõ ràng, bấm ăn ngay bằng 1 chạm trên màn hình 86 inch.
     * **Center Live Screen Matrix:** Toàn bộ không gian chính giữa dành cho Lưới màn hình trực tiếp của Học sinh (PC & Android), kèm bộ lọc (`Tất cả` / `Chỉ PC` / `Chỉ Android`), chọn nhanh (`Chọn tất cả` / `Bỏ chọn`) và thanh trượt phóng to/thu nhỏ ô màn hình.
     * **Bottom Status Bar:** Hiển thị IP máy Giáo viên, trạng thái phát màn hình, tổng số máy PC & Android đang kết nối và số học sinh đang giơ tay.
2. **Giao diện Máy Học Sinh PC (`StudentShell`) & Android (`QASmartClass.Android`):**
   * Giao diện tối giản, tập trung vào: **Tự động dò & Kết nối IP Giáo viên** $\rightarrow$ **Xem màn hình Giáo viên phát sang (Full-screen / Zoom)** + **Truyền màn hình thiết bị về máy Giáo viên (`ScreenCaptureService` / `MediaProjection`)** + **Khóa màn hình khi có lệnh** + **Thanh công cụ gọn nhẹ (`✋ Giơ tay`, `💬 Chat`, `📁 Nhận/Nộp File`)**.

