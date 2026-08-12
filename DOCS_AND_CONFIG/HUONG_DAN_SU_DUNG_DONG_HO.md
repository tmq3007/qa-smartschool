# 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: ĐỒNG HỒ TẬP TRUNG (FOCUS CLOCK)

Tài liệu này cung cấp hướng dẫn trực quan, sơ đồ quy trình và các mẫu dữ liệu thực tế giúp **Giáo viên** và **Học sinh** sử dụng tối đa hiệu năng của công cụ **Đồng hồ tập trung** để nâng cao năng suất tự học, kiểm soát thời gian làm bài nhóm và tạo lập kỷ luật trong lớp học.

---

## 🗺️ 1. Quy Trình Vận Hành Thời Gian Thực (Sequence Diagram)

Sơ đồ mô tả tương tác giữa Giáo viên (sử dụng màn hình tương tác thông minh) và Học sinh trong một phiên tập trung học tập trên lớp:

```mermaid
sequenceDiagram
    autonumber
    actor GV as Giáo viên (Bảng tương tác)
    actor HS as Học sinh (Thiết bị cá nhân)
    database DB as Cơ sở dữ liệu (smartclass.db)

    Note over GV: Bước 1: Thiết lập mục tiêu học tập
    GV->>GV: Chọn preset 25 phút (Toán học)<br/>Hoặc kéo xoay viền tròn đặt giờ lẻ
    GV->>GV: Chọn âm thanh nền "Tiếng mưa" và âm lượng 40%

    Note over GV, HS: Bước 2: Bắt đầu phiên (Focus Sync)
    GV->>GV: Nhấn "▶ Bắt đầu"
    GV->>HS: Kích hoạt "🎯 Focus HS" (Hệ thống khóa màn hình HS)
    HS->>HS: Màn hình chuyển sang trạng thái tập trung đếm ngược cùng GV

    Note over HS: Bước 3: Tập trung làm bài
    loop Đếm ngược thời gian thực
        HS->>HS: Chạy đếm ngược từng giây & vẽ tiến trình viền tròn
    end

    Note over GV, DB: Bước 4: Hoàn thành & Ghi nhận hiệu suất
    GV->>GV: Hết giờ -> Phát chuông Exclamation hệ thống
    GV->>DB: Telemetry tự động lưu sự kiện "FOCUS_SESSION_COMPLETED"
    GV->>GV: Tự động chuyển sang nghỉ ngơi (☕ Nghỉ giải lao)
```

---

## 👩‍🏫 2. DÀNH CHO GIÁO VIÊN: QUẢN LÝ THỜI GIAN TRÊN LỚP HỌC

Giáo viên có thể sử dụng đồng hồ tập trung trên bảng tương tác thông minh của lớp học để điều phối hoạt động giảng dạy.

### 💡 Các bước áp dụng thực tế:
1.  **Giao bài tập/Hoạt động:** Phổ biến luật làm bài (Ví dụ: Làm bài tập toán số học trang 42 trong 15 phút).
2.  **Cài đặt thời gian nhanh:** Nhấp vào các nút **Preset tập trung** (Ví dụ: `15 phút`).
3.  **Điều khiển lớp học:** Nhấp chọn nút **▶ Bắt đầu**. Đồng thời nhấn nút **🎯 Focus HS** trên thanh công cụ hỗ trợ giảng dạy để buộc tất cả máy học sinh trong lớp hiển thị đồng hồ này, ngăn các em mở các ứng dụng khác chơi game.
4.  **Hết giờ:** Khi chuông báo hết giờ vang lên, yêu cầu học sinh dừng bút và nhấp nút **▶ Bắt đầu nghỉ** để kích hoạt 5 phút thư giãn.

> [!TIP]
> **Khuyên dùng từ Giáo viên ưu tú:**
> -   Không nên mở âm thanh nền quá to (chỉ nên đặt ở mức 30-40%). Âm thanh nền nhẹ nhàng giúp học sinh át đi tiếng ồn xung quanh trong lớp học mà không gây mất tập trung.
> -   Đặt mục tiêu rõ ràng cho học sinh trước khi bấm giờ: *"Chúng ta có 25 phút tập trung cao độ, ai hoàn thành phiên học và ghi nhận lịch sử vào hệ thống sẽ được tích điểm thi đua!"*

---

## 🧑‍🎓 3. DÀNH CHO HỌC SINH: RÈN LUYỆN TỰ HỌC (POMODORO)

Học sinh có thể sử dụng đồng hồ tập trung để tự học ở thư viện, ở phòng tự học hoặc làm bài tập về nhà.

### 🎮 Trải nghiệm kéo thả & Âm thanh trực quan:
*   **Đặt giờ thủ công bằng tay (Kéo xoay viền elip):**
    *   Học sinh nhấp chuột (hoặc chạm ngón tay nếu dùng màn hình cảm ứng) vào **đường viền màu tiến trình** và kéo xoay theo chiều kim đồng hồ để tăng số phút, ngược chiều để giảm số phút.
    *   *Lưu ý:* Vùng trung tâm đồng hồ (nơi có chữ hiển thị số phút) được bảo vệ chống click nhầm để tránh thời gian bị thay đổi đột ngột khi học sinh vô tình chạm tay trúng.
*   **Lựa chọn Nhạc nền (Ambient Sounds):**
    *   Nhấp chọn **🌧️ (Tiếng mưa)** hoặc **🌊 (Sóng biển)** để kích thích sóng não tập trung sâu.
    *   Sử dụng thanh kéo volume 🔊 để tăng giảm mức âm lượng cho vừa tai. Nhạc nền được tối ưu tự động phát lặp lại vô hạn cho đến khi tắt hoặc nhấn Đặt lại.

---

## 📊 4. DỮ LIỆU MẪU CẤU HÌNH & VÍ DỤ THỰC TẾ (PRACTICAL EXAMPLES)

Dưới đây là 3 mẫu cấu hình thời gian chuẩn sư phạm đã được lập trình sẵn và ví dụ áp dụng thực tiễn trong trường học:

### 📐 Ví dụ 1: Phiên tập trung Pomodoro truyền thống (25 phút học - 5 phút nghỉ)
*   **Mục đích sử dụng:** Tự ôn tập kiến thức cuối kỳ, giải đề thi thử.
*   **Cách thiết lập:**
    *   Thời gian tập trung: Nhấp chọn nút preset **25 phút** (Vòng tiến trình sáng xanh lục).
    *   Thời gian nghỉ: Nhấp chọn preset **5 phút** (Nút sáng cam).
    *   Nhạc nền đề xuất: Chọn **🌧️ Tiếng mưa** (Tạo tiếng ồn trắng, tăng 25% khả năng tập trung).
*   **Đầu ra Telemetry trong database (Ví dụ):**
    ```json
    {
      "EventType": "FOCUS_SESSION_COMPLETED",
      "EventData": "{\"FocusMinutes\":25,\"BreakMinutes\":5,\"SessionCount\":1}",
      "DurationMs": 1500000,
      "Timestamp": "2026-06-17T20:30:00"
    }
    ```

### 🔬 Ví dụ 2: Phiên kiểm tra 15 phút đầu giờ (15 phút học - 3 phút nghỉ)
*   **Mục đích sử dụng:** Giáo viên tổ chức làm bài kiểm tra nhanh trực tuyến đầu giờ.
*   **Cách thiết lập:**
    *   Thời gian tập trung: Nhấp chọn preset **15 phút**.
    *   Thời gian nghỉ: Nhấp chọn preset **3 phút** (Cho học sinh thư giãn mắt).
    *   Nhạc nền đề xuất: **Tắt nhạc nền** (Đảm bảo phòng thi im lặng tuyệt đối).
*   **Cảnh báo màu sắc viền tiến trình:**
    *   **Còn trên 50% thời gian (15:00 - 07:30):** Viền tiến trình hiển thị màu xanh lá lục `#4CAF50`.
    *   **Còn từ 50% đến 15% (07:30 - 02:15):** Viền tự động chuyển sang màu Cam `#FF9800` (Học sinh chú ý tăng tốc).
    *   **Còn dưới 15% thời gian (dưới 02:15):** Viền chuyển sang màu Đỏ cảnh báo `#F44336` (Khẩn cấp sắp hết giờ).

### 📖 Ví dụ 3: Phiên đọc sách tích lũy (45 phút học - 10 phút nghỉ)
*   **Mục đích sử dụng:** Đọc sách thư viện, nghiên cứu chuyên đề, làm bài thực hành nhóm lớn.
*   **Cách thiết lập:**
    *   Thời gian tập trung: Nhấp chọn preset **45 phút**.
    *   Thời gian nghỉ: Nhấp chọn preset **10 phút** (Nghỉ dài hơn để não hồi phục).
    *   Nhạc nền đề xuất: Chọn **🐦 Tiếng chim** (Thư giãn, giảm stress).

---

## 📈 5. NGHIÊN CỨU SƯ PHẠM: HIỆU QUẢ CỦA PHƯƠNG PHÁP POMODORO
*   **Tăng khả năng hoàn thành công việc:** Việc phân nhỏ thời gian học thành các phiên 25 phút giúp học sinh không bị áp lực "phải hoàn thành một lượng bài tập khổng lồ cùng một lúc", từ đó giảm hành vi trì hoãn học tập.
*   **Bảo vệ sức khỏe học đường:** Việc bắt buộc nghỉ 5 phút giữa các phiên giúp học sinh đứng dậy vận động nhẹ, giãn cơ và thư giãn mắt, giảm tỉ lệ cận thị học đường và cong vẹo cột sống do ngồi sai tư thế quá lâu.
*   **Phân tích hiệu suất giảng dạy:** Bằng cách theo dõi các bản ghi `"FOCUS_SESSION_COMPLETED"` trong tệp cơ sở dữ liệu `smartclass.db`, Ban giám hiệu có thể lập báo cáo chất lượng tự học của học sinh theo từng lớp để điều chỉnh thời khóa biểu hợp lý hơn.
