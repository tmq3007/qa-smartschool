# QA SmartClass — Android Student Client

Ứng dụng Android độc lập dành cho Máy tính bảng (Tablet) và Điện thoại của Học sinh trong hệ thống **QA SmartClass v4.2**.

---

## 🌟 Tính Năng Chính
1. **Tự động quét & tìm lớp (Zero-Config Discovery):**
   - Lắng nghe gói tin UDP Beacon từ máy Giáo viên qua cổng `29876`.
   - Học sinh mở app là tự động tìm thấy tên lớp, tên giáo viên và kết nối ngay, không cần gõ IP.
2. **Khóa màn hình Kiosk Mode (Device Owner):**
   - Khi giáo viên bắt đầu phát bài giảng hoặc khóa màn hình, app tự động kích hoạt chế độ Kiosk toàn màn hình, ngăn học sinh thoát ra chơi game hoặc mở ứng dụng khác.
3. **Giám sát tập trung (Focus Reporter):**
   - Theo dõi ứng dụng chạy ngầm và báo cáo định kỳ 3 giây/lần về cho Giáo viên biết học sinh có đang chú ý bài học hay không.
4. **Nhịp tim chống kẹt (Heartbeat Watchdog):**
   - Tự động duy trì kiểm tra kết nối với giáo viên. Nếu mạng ngắt hoặc kết thúc lớp học quá 10 giây, máy sẽ tự động giải phóng khóa màn hình an toàn.
5. **Giao diện tương tác toàn diện:**
   - Nhận hình ảnh bài giảng phát sóng trực tiếp từ giáo viên.
   - Làm bài kiểm tra Quiz trắc nghiệm tương tác.
   - Giơ tay phát biểu, gửi câu hỏi và nhận bài tập về máy.

---

## 🛠️ Hướng Dẫn Build File APK

### Cách 1: Sử dụng Android Studio (Đơn giản nhất)
1. Mở **Android Studio**.
2. Chọn **Open** và trỏ đến thư mục `d:\Document\_Projects\qa-smartschool\QASmartClass.Android`.
3. Chờ Gradle đồng bộ (Sync) lần đầu.
4. Chọn menu **Build > Build Bundle(s) / APK(s) > Build APK(s)**.
5. File `.apk` hoàn thành sẽ nằm trong: `QASmartClass.Android\app\build\outputs\apk\debug\app-debug.apk`.

### Cách 2: Chạy script tự động trên PowerShell
Chạy file script đi kèm:
```powershell
.\build-apk.ps1
```
Script sẽ tự động tìm JDK 17/21 trên máy tính, gọi Gradle Wrapper và đóng gói ra file APK.

---

## 📱 Cài Đặt Lên Thiết Bị Android
1. Chép file `app-debug.apk` hoặc `app-release.apk` vào máy tính bảng/điện thoại Android.
2. Mở file để cài đặt (cho phép cài đặt ứng dụng từ nguồn không xác định nếu có hỏi).
3. Đảm bảo thiết bị Android kết nối cùng mạng WiFi với máy tính Giáo viên.
4. Mở app **QA SmartClass Student** và tham gia lớp học.
