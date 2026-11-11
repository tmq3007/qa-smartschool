---
trigger: always_on
---

# 🖥️ QUY CHUẨN PHÂN HỆ SMART CLASS CHUẨN VEYON & MYTHWARE (WINDOWS PC & ANDROID STUDENT PARITY RULES)

> **Phạm vi áp dụng:** Bộ quy tắc này **CHỈ ÁP DỤNG CHO PHÂN HỆ `SMART CLASS`** (`QASmartClass/Classroom/`, `QASmartClass/StudentClient/`, `QASmartClass/Forms/Form0_LoginSelection*`, và `QASmartClass.Android/`).
> **TUYỆT ĐỐI KHÔNG** dùng quy tắc này để sửa đổi, cắt gọn hay can thiệp vào Bảng viết tương tác **`SMART TOUCH`** (`Form2_MainDashboard` và `QASmartTouch.csproj`).

---

## 1. [RULE] QSC_SMARTTOUCH_IMMUNITY: Bất Khả Xâm Phạm Đối Với `SMART TOUCH`
1. **Giữ nguyên 100% `SMART TOUCH`:**
   * Mọi tác vụ thiết kế lại giao diện hoặc nâng cấp `QASmartClass` theo chuẩn Veyon/Mythware **tuyệt đối không được sửa đổi** giao diện, thanh công cụ, học cụ STEM, mô hình 3D, hay luồng cảm ứng/bút viết của **`SMART TOUCH`** (`Form2_MainDashboard.*`, `Form2_*`, `WhiteboardCore/`, `Controls/`).
2. **Bảo toàn Chuyển đổi 3 Chế độ (`SMART CLASS` $\leftrightarrow$ `SMART TOUCH` $\leftrightarrow$ `DESKTOP`):**
   * Thanh chuyển đổi chế độ nổi (`FloatingModeBar`) và Thanh điều khiển chia sẻ màn hình nổi (`FloatingBroadcastBarWindow`) luôn hoạt động xuyên suốt để Giáo viên vừa phát màn hình vừa giảng bài trên `SMART TOUCH` hoặc `DESKTOP`.

---

## 2. [RULE] QSC_DUAL_PLATFORM_STUDENT_PARITY: Đồng Bộ Kép Máy Học Sinh Windows PC & Android
Mọi tính năng điều hành lớp học từ máy Giáo viên (`ClassroomShell` / `MonitorPage`) **BẮT BUỘC** phải thiết kế giao thức mạng (`CMD|...`) và hành vi tương ứng cho **CẢ 2 NỀN TẢNG MÁY HỌC SINH**:
1. **Máy Học sinh Windows PC (`QASmartClass/StudentClient/Views/StudentShell.xaml.cs` — C# WPF .NET 9)**
2. **Máy Học sinh Android (`QASmartClass.Android/` — Native Kotlin Android SDK + Coroutines)**
   * **Chuẩn công nghệ Android bắt buộc:** Sử dụng **Native Kotlin** (`QASmartClass.Android`) để truy cập trực tiếp các API hệ thống Android (`MediaProjection` quay màn hình, `WindowManager` Lock Overlay, `PackageManager` mở ứng dụng, `AudioManager` tắt tiếng, Raw TCP/UDP Socket) mà không qua tầng trung gian nặng nề.
   * Mỗi khi thêm/sửa một lệnh `CMD|...` trên máy Giáo viên, lập trình viên/AI **bắt buộc** kiểm tra và cập nhật hàm xử lý tại cả `StudentShell.xaml.cs` (Windows) lẫn `MainActivity.kt` / Services (Android).

---

## 3. [RULE] QSC_VEYON_MYTHWARE_FEATURE_MATRIX: Ma Trận 10 Nhóm Chức Năng Cốt Lõi (Giáo Viên $\rightarrow$ Windows PC & Android)

| # | Nhóm Chức năng (Veyon / Mythware) | Lệnh Giao thức (`TCP 29877`) | Hành vi trên Học sinh **Windows PC** | Hành vi trên Học sinh **Android** |
|---|---|---|---|---|
| 1 | **🖥️ Giám sát màn hình trực tiếp (`Live Monitoring Grid`)** | `SCREEN_FRAME` / `CMD|REQUEST_SCREENSHOT` | Chụp màn hình Desktop nén JPEG gửi định kỳ & theo yêu cầu về lưới Veyon | Dùng `MediaProjection` (`ScreenCaptureService.kt`) hoặc chụp View gửi ảnh JPEG về lưới Veyon |
| 2 | **📺 Chiếu bài giảng (`Fullscreen / Window / Region Broadcast`)** | `CMD|SCREEN_BROADCAST_START` / `STOP` / `UPDATE` | Mở cửa sổ nhận luồng MJPEG/Frame (`FORCE_WATCH` full-screen hoặc cửa sổ) | Hiển thị luồng hình ảnh bài giảng trực tiếp trên `imgBroadcastStream` + chế độ Full-screen |
| 3 | **👨‍🎓 Học sinh trình bày (`Student Demo Relay`)** | `CMD|STUDENT_DEMO_START` / `STOP` | Máy HS được chọn đẩy luồng màn hình tốc độ cao lên GV để GV phát lại cho cả lớp | Máy Android được chọn tăng tốc `ScreenCaptureService` đẩy luồng lên GV để chiếu cho cả lớp |
| 4 | **🔒 Khóa màn hình & 🔇 Im lặng (`Screen Lock & Mute`)** | `CMD|LOCK\|1`, `CMD|UNLOCK`, `CMD|BLACK_SCREEN\|1`, `CMD|MUTE_AUDIO\|1` | Phủ cửa sổ khóa Topmost toàn màn hình (chặn Alt+Tab/Taskbar) + tắt tiếng hệ thống | Bật `lockOverlay` toàn màn hình + `OverlayLockFallback` + `AudioManager` đưa âm lượng Media về 0 |
| 5 | **👁️ Quan sát & 🖱️ Điều khiển từ xa (`Remote View & Control`)** | `CMD|REMOTE_CONTROL_START`, `CMD|MOUSE_EVENT`, `CMD|KEY_EVENT` | Xem nét cao 1-1; thực thi thao tác chuột/phím từ xa bằng `SendInput` Win32 | Xem nét cao 1-1 màn hình Android; hiển thị con trỏ chỉ dẫn trực quan (`Laser/Touch Pointer`) của GV trên màn hình (hoặc `AccessibilityService` nếu bật) |
| 6 | **🌐 Mở Website (`Open Website`)** | `CMD|OPEN_URL\|<url>` | Mở trình duyệt mặc định (`Process.Start` với `UseShellExecute = true`) | Mở trình duyệt bằng `Intent(Intent.ACTION_VIEW, Uri.parse(url))` |
| 7 | **🚀 Chạy Ứng dụng (`Start Application`)** | `CMD|START_APP\|<app_or_pkg>` | Khởi chạy đường dẫn `.exe` hoặc tên lệnh Windows (`calc`, `notepad`, `mspaint`...) | Khởi chạy theo `packageName` (`getLaunchIntentForPackage`) hoặc tên ứng dụng gợi ý |
| 8 | **💬 Tin nhắn & ✋ Giơ tay (`Text Message, Chat & Hand-Raise`)** | `CMD|MESSAGE\|...`, `CMD|HAND_ACK`, `CMD|HAND_DOWN` | Hiện Popup thông báo nổi từ Giáo viên + khung Chat + trạng thái Giơ tay | Hiện Dialog/Banner thông báo từ Giáo viên + âm báo + Tab Chat & Giơ tay |
| 9 | **📁 Truyền File & Thu bài (`File Transfer & Collect`)** | `CMD|FILE_OFFER\|...` / HTTP/TCP File | Nhận file lưu vào thư mục `SmartClass_Received` và mở nhanh; nộp file bài làm | Tải file từ Giáo viên về `Downloads/QASmartClass` và mở bằng `FileProvider`; chọn file/ảnh nộp bài |
| 10 | **⚡ Quản lý Nguồn & Phiên (`Power / Session Control`)** | `CMD|REBOOT_PC`, `CMD|SHUTDOWN_PC`, `CMD|LOGOFF_PC`, `CMD|KICK` | Thực thi `shutdown /r`, `shutdown /s`, `shutdown /l` hoặc ngắt kết nối khỏi lớp | Với Android: Đăng xuất phiên lớp học (`LOGOFF`/`KICK`), khóa thiết bị (`DevicePolicyManager.lockNow()` nếu cấp quyền) hoặc đóng ứng dụng an toàn |

---

## 4. [RULE] QSC_COMMENT_NOT_DELETE: Ẩn Bằng Comment — Không Xóa Mã Nguồn Cũ
* Khi ẩn hoặc tách các trang ERP/LMS cũ, **BẮT BUỘC dùng comment** (`// [MYTHWARE_LITE]` trong C#/Kotlin hoặc `<!-- [MYTHWARE_LITE] ... -->` trong XAML/XML).
* **Tuyệt đối không xóa trắng** các hàm hoặc tệp mã nguồn cũ để có thể khôi phục hoặc tham khảo lại bất cứ lúc nào.

---

## 5. [RULE] QSC_ZERO_DB_DEPENDENCY: Kết Nối Mạng LAN Trực Tiếp (Zero-Config LAN/Wi-Fi)
1. **Cắm mạng là chạy (Zero-Config Discovery):**
   * Máy Giáo viên và Học sinh (Windows PC & Android) tự động tìm thấy nhau qua **UDP Broadcast (`Cổng 29876`)** hoặc nhập trực tiếp địa chỉ IP + Mã lớp để kết nối vào **TCP Control (`29877`)**, **Screen Broadcast (`29878` / HTTP Stream)**, và **File Transfer (`29879`)**.
2. **Phân biệt Phòng học & Chống vào nhầm lớp:**
   * Hỗ trợ chọn đúng Phòng học/Lớp học khi có nhiều giáo viên cùng dạy trên 1 mạng LAN, hiển thị thẻ xác nhận tên Giáo viên + Lớp trước khi vào, và cho phép cả Học sinh tự rời lớp lẫn Giáo viên mời ra (`CMD|KICK`) nếu vào nhầm.
