# 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ TÍNH NHẨM NHANH (MENTAL MATH TOOL)

Tài liệu này cung cấp hướng dẫn trực quan, sơ đồ quy trình, dữ liệu mẫu thực tế và hướng dẫn chi tiết dành cho **Giáo viên** và **Học sinh** khi sử dụng công cụ **Tính Nhẩm Nhanh** thuộc hệ thống trường học thông minh **QA SmartClass**.

---

## 🗺️ 1. Sơ Đồ Quy Trình Vận Hành Thời Gian Thực (Sequence Diagram)

Sơ đồ mô tả luồng tương tác giữa Học sinh gõ đáp án trên lớp, Hệ thống kiểm tra logic/tính điểm/lưu trữ và Giáo viên xuất báo cáo thống kê:

```mermaid
sequenceDiagram
    autonumber
    actor HS as Học sinh (Giao diện chơi)
    actor GV as Giáo viên (Quản lý)
    participant APP as ⚡ Công cụ Tính Nhẩm Nhanh
    database DB as Cơ sở dữ liệu SQLite (smartclass.db)

    Note over HS, APP: Bước 1: Khởi động & Đọc bảng xếp hạng
    HS->>APP: Nhấn tab "Tính Nhẩm Nhanh"
    APP->>DB: Đọc Top 5 thành tích cao nhất (GetTopProgress)
    DB-->>APP: Trả về danh sách thành tích
    APP->>HS: Hiển thị bảng hướng dẫn & Bảng xếp hạng Top 5

    Note over HS, APP: Bước 2: Luyện tập (30 câu hỏi)
    HS->>APP: Chọn Cấp độ (VD: Chuyên gia) -> Nhấn "▶ Bắt đầu"
    APP->>APP: Ẩn bảng hướng dẫn & Bảng xếp hạng. Hiện câu hỏi 1.
    loop Mỗi câu hỏi
        APP->>HS: Hiển thị biểu thức toán học (VD: 35² = ?)
        HS->>APP: Gõ đáp án (TextBox chỉ nhận số) -> Nhấn Enter
        ALT Đáp án ĐÚNG
            APP->>APP: Tăng điểm số, tăng Streak bốc lửa 🔥
            Note over APP: Streak >= 5: Đổi màu Cam<br/>Streak >= 10: Đổi màu Đỏ (Siêu cấp! 🔥🔥🔥)
            APP->>HS: Phát âm thanh đúng, hiển thị "✅ Đúng!" (Màu xanh)
        ELSE Đáp án SAI
            APP->>APP: Reset Streak về 0 (Màu mặc định)
            APP->>HS: Phát âm thanh sai, hiển thị "❌ Sai! Đáp án: 1225" (Màu đỏ)
        END
    end

    Note over HS, DB: Bước 3: Hoàn thành & Xếp hạng thành tích
    APP->>APP: Hết 30 câu -> Đếm tổng điểm & thời gian hoàn thành
    APP->>HS: Hiện Huy chương/Cúp unicode lớn (🏆, 🥇, 🥈, 📚, 💪) dựa trên điểm số
    APP->>DB: Lưu tiến trình vào DB đồng bộ (SaveProgress)
    APP->>DB: Truy vấn lại Top 5 cập nhật thời gian thực
    DB-->>APP: Trả về danh sách Top 5 mới nhất
    APP->>HS: Hiện lại Bảng hướng dẫn & Bảng xếp hạng đã cập nhật điểm mới

    Note over GV, DB: Bước 4: Thống kê của Giáo viên
    GV->>APP: Bấm nút "📥 Xuất CSV" trên thanh Score Bar
    APP->>DB: Gọi GetAllProgress("Tính nhẩm nhanh")
    DB-->>APP: Trả về toàn bộ lịch sử luyện tập
    APP->>GV: Ghi tệp tin CSV mã hóa UTF-8 BOM chống lỗi font tiếng Việt trên Microsoft Excel
```

---

## 👩‍🏫 2. DÀNH CHO GIÁO VIÊN: QUẢN LÝ & GHI NHẬN ĐIỂM SỐ LỚP HỌC

Giáo viên có thể sử dụng công cụ Tính Nhẩm Nhanh để tổ chức kiểm tra bài cũ (kiểm tra miệng) hoặc tổ chức thi đấu tốc độ tính nhẩm trực tiếp trên lớp.

### 💡 Các bước tổ chức hoạt động thi đấu tính nhẩm trên lớp:
1.  **Phổ biến luật chơi:** Yêu cầu cả lớp chọn chung một cấp độ (Ví dụ: Cấp độ **"Vừa"** hoặc **"Chuyên gia"**).
2.  **Thi đấu đợt 1:** Cho 5 học sinh đầu tiên lên bảng tương tác hoặc máy tính thực hành. Nhấn **▶ Bắt đầu** để các em làm 30 câu.
3.  **Cạnh tranh Bảng xếp hạng:** Khi mỗi học sinh làm xong, **Bảng vàng thành tích (Top 5)** tự động cập nhật. Các học sinh tiếp theo sẽ vào chơi để cố gắng phá kỷ lục điểm số và thời gian của các bạn trước.
4.  **Tích hợp bài giảng:** Sử dụng **Bảng hướng dẫn & Mẹo Tính nhẩm** hiển thị ở màn hình chờ để dạy các em học sinh các quy luật toán học (ví dụ: mẹo nhân 11, bình phương đuôi 5) trước khi thi đấu.

### 📥 Hướng dẫn xuất và đọc bảng điểm Excel (Không bị lỗi tiếng Việt):
1.  Nhấp vào nút **📥 Xuất CSV** trên thanh công cụ màu xanh lá ở góc phải phía trên.
2.  Hộp thoại lưu tệp mở ra. Đặt tên file (ví dụ: `Diem_TinhNham_Lop6A.csv`) và chọn thư mục lưu (Desktop).
3.  **Kích đúp chuột mở trực tiếp file bằng Microsoft Excel**.
4.  Nhờ công nghệ lưu tệp **UTF-8 BOM (\uFEFF)** mà hệ thống đã tích hợp, Excel sẽ tự động hiển thị chính xác các cột tiếng Việt: `Thời gian`, `Tỷ lệ chính xác (%)`, `Cấp độ` (Hiển thị rõ ràng `"Chuyên gia"`, `"Khó (Nhẩm)"` không bị vỡ font hay biến thành ký tự lạ).

---

## 🧑‍🎓 3. DÀNH CHO HỌC SINH: MẸO TÍNH NHẨM NHANH & CHINH PHỤC CÚP VÀNG

Để lọt vào **Bảng vàng thành tích (Top 5)**, các em cần rèn luyện thành thạo các mẹo toán học đặc biệt dưới đây.

### ⚡ 3 Bí quyết nhẩm siêu tốc ở cấp độ "Chuyên gia":
1.  **Mẹo nhân một số với 11:**
    *   *Cách làm:* Lấy chữ số hàng chục cộng chữ số hàng đơn vị rồi viết kết quả vào giữa hai chữ số đó.
    *   *Ví dụ:* `35 × 11`. Ta lấy `3 + 5 = 8`. Viết `8` vào giữa số `3` và `5` -> Kết quả: `385`.
    *   *Ví dụ (có nhớ):* `78 × 11`. Ta lấy `7 + 8 = 15`. Viết `5` vào giữa, nhớ `1` vào số `7` phía trước (`7 + 1 = 8`) -> Kết quả: `858`.
2.  **Mẹo nhân một số chẵn với 5:**
    *   *Cách làm:* Chia đôi số chẵn đó rồi nhân với 10 (thêm chữ số 0 vào sau).
    *   *Ví dụ:* `84 × 5`. Ta lấy `84 ÷ 2 = 42`. Thêm số `0` vào sau -> Kết quả: `420`.
3.  **Mẹo bình phương số tận cùng là 5:**
    *   *Cách làm:* Lấy chữ số đầu tiên nhân với số liền sau của nó, rồi viết thêm đuôi `25` vào sau kết quả.
    *   *Ví dụ:* `65²` (tức là `65 × 65`). Lấy số đầu là `6` nhân với số liền sau là `7` (`6 × 7 = 42`). Viết thêm `25` vào sau -> Kết quả: `4225`.
    *   *Ví dụ:* `35²`. Lấy `3 × 4 = 12`. Viết thêm `25` vào sau -> Kết quả: `1225`.

### 🎮 Rèn luyện phản xạ & Đổi màu lửa Streak:
*   Hãy gõ đáp án thật nhanh bằng các phím số trên bàn phím (hoặc bàn phím ảo bên phải màn hình nếu dùng máy tính bảng). TextBox **chỉ nhận số** nên các em không sợ gõ nhầm chữ. Nhấn **Enter** để nộp bài.
*   Hãy trả lời đúng liên tục để kích hoạt **Streak bốc lửa 🔥**:
    *   Khi đạt chuỗi đúng 5 câu liên tiếp: Chuỗi thắng chuyển màu Cam và hiển thị: `🔥 5 (Nóng bỏng! 🔥)`.
    *   Khi đạt chuỗi đúng 10 câu liên tiếp: Chuỗi thắng chuyển màu Đỏ và hiển thị: `🔥 10 (Siêu cấp! 🔥🔥🔥)`.
*   Cố gắng đạt trên **28 câu đúng** để rinh ngay chiếc **Cúp vàng vô địch 🏆** danh giá xuất hiện ở cuối màn hình nhé!

---

## 📊 4. DỮ LIỆU MẪU VÀ VÍ DỤ THỰC TẾ (PRACTICAL EXAMPLES)

### 📂 Dữ liệu mẫu tệp xuất CSV (Mở trong Excel)

Dưới đây là bảng dữ liệu thực tế mẫu khi giáo viên xuất lịch sử làm bài của một học sinh ra file CSV và mở bằng Excel:

| STT | Thời gian | Số câu đúng | Tổng số câu | Tỷ lệ chính xác (%) | Thời gian hoàn thành (giây) | Cấp độ |
| :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| 1 | 2026-06-18 08:05:12 | 30 | 30 | 100.0 | 28.5 | Chuyên gia |
| 2 | 2026-06-18 08:00:45 | 28 | 30 | 93.3 | 32.0 | Chuyên gia |
| 3 | 2026-06-18 07:55:10 | 25 | 30 | 83.3 | 45.2 | Khó |
| 4 | 2026-06-18 07:48:30 | 29 | 30 | 96.7 | 18.4 | Vừa |
| 5 | 2026-06-18 07:42:15 | 30 | 30 | 100.0 | 12.1 | Dễ |

*   *Nhận xét:* Học sinh đạt tỷ lệ 100.0% ở cấp độ "Dễ" chỉ mất 12.1 giây. Cấp độ "Chuyên gia" đòi hỏi thời gian nhẩm lâu hơn (28.5 giây) nhưng đạt điểm tuyệt đối 30/30 nhờ áp dụng thành thạo 3 mẹo toán học.

### 💾 Dữ liệu mẫu lưu trong Cơ sở dữ liệu SQLite (`smartclass.db`)

Bảng `UserProgress` lưu trữ các bản ghi dưới dạng bảng quan hệ có cấu trúc như sau:

| Id | GameName | Score | Total | DurationSeconds | Difficulty | CreatedAt | IsEndless |
| :--- | :--- | :---: | :---: | :---: | :--- | :--- | :---: |
| 412 | Tính nhẩm nhanh | 30 | 30 | 28.5 | Chuyên gia | 2026-06-18T08:05:12.124 | 0 |
| 413 | Luyện IQ & Logic | 14 | 15 | 185.0 | Khó | 2026-06-18T08:06:00.567 | 0 |
| 414 | Tính nhẩm nhanh | 25 | 30 | 45.2 | Khó | 2026-06-18T07:55:10.892 | 0 |

---

## 📈 5. Ý NGHĨA SƯ PHẠM VÀ KHOA HỌC GIÁO DỤC

*   **Kích thích phát triển hai bán cầu não:** Phép tính toán nhẩm nhanh yêu cầu sự kết hợp giữa phân tích số học của bán cầu não trái và khả năng tưởng tượng hình ảnh/quy luật của bán cầu não phải (tưởng tượng quy trình ghép số, chia đôi số).
*   **Tạo phản xạ toán học tự nhiên:** Giúp các em học sinh không bị lệ thuộc vào máy tính bỏ túi hoặc nháp giấy đối với các phép toán đơn giản thường gặp trong cuộc sống hàng ngày.
*   **Rèn luyện sự tập trung và kiên trì:** Phiên làm bài 30 câu hỏi liên tục với đồng hồ đếm giây thúc đẩy học sinh duy trì sự tập trung cao độ, rèn luyện tính kiên nhẫn khi xử lý các chuỗi số học dài.
