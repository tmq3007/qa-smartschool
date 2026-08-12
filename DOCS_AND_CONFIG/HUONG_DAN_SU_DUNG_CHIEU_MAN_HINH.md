# HƯỚNG DẪN SỬ DỤNG CHỨC NĂNG CHIA SẺ MÀN HÌNH & TRÌNH CHIẾU BÀI GIẢNG (V2.1)
*Tài liệu dành cho Giáo viên, Học sinh và Kỹ thuật viên Phòng máy*
*Cập nhật: 22/06/2026 — Phiên bản sau nâng cấp*

---

Giao diện **Chia sẻ màn hình (Screen Share)** trong hệ thống SmartClass là công cụ đắc lực giúp Giáo viên truyền phát trực tiếp slide bài giảng, hình ảnh minh họa, website học tập, hoặc các ứng dụng mô phỏng 3D từ máy tính của mình tới toàn bộ hoặc một nhóm máy tính bảng/máy tính cá nhân của học sinh trong lớp học.

> [!NOTE]
> Hệ thống hoạt động hoàn toàn thông qua mạng LAN nội bộ của nhà trường, không cần kết nối Internet, giúp tối ưu hóa tốc độ truyền tải và bảo mật thông tin lớp học.

---

## I. HƯỚNG DẪN DÀNH CHO GIÁO VIÊN (TEACHER GUIDE)

### Bước 1: Mở giao diện Trình chiếu
Tại màn hình chính của bảng điều khiển giáo viên, chọn biểu tượng **📡 Quảng bá / Chiếu màn hình** hoặc chọn Tab **"Chiếu màn hình"** ở phía bên trái.

### Bước 2: Thiết lập cấu hình nguồn phát (TV Panel bên phải)
Giáo viên tiến hành lựa chọn các thông số truyền phát phù hợp với bài học:

1.  **📺 Nguồn phát:**
    *   *Toàn màn hình (Mặc định):* Chiếu toàn bộ nội dung xuất hiện trên máy tính giáo viên (khuyên dùng khi dạy PowerPoint, Word).
    *   *Cửa sổ ứng dụng:* Chỉ chiếu 1 ứng dụng cụ thể (Ví dụ: Chỉ chiếu trình duyệt Chrome đang mở thí nghiệm ảo Desmos/PhET). Học sinh sẽ không nhìn thấy các ứng dụng khác của giáo viên.
    *   *Camera/Webcam:* Chiếu hình ảnh trực tiếp từ webcam giáo viên.
2.  **⚙️ Chất lượng & Tần số quét (Frame rate):**
    *   *Độ phân giải:* Chọn **1920×1080** (Full HD) cho bài giảng thông thường, hoặc **1280×720** (HD) nếu Wi-Fi phòng học yếu.
    *   *Frame rate:* 
        *   **Thấp (1 fps):** Tiết kiệm băng thông nhất, phù hợp slide tĩnh.
        *   **Trung bình (2 fps):** Mặc định — cân bằng giữa mượt mà và nhẹ mạng.
        *   **Cao (5 fps):** Phù hợp khi giải bài tập, rê chuột liên tục.
        *   **Mượt mà (15 fps):** Phù hợp video ngắn, mô phỏng 3D.
        *   **Rất mượt (30 fps):** ⚡ *MỚI* — Dùng khi chiếu video giáo dục hoặc game mô phỏng. *Lưu ý: Yêu cầu mạng LAN tốc độ cao (>100Mbps).*
    *   *Nén ảnh (Tối ưu băng thông):*
        *   **Thấp (40%):** Chất lượng ảnh hơi mờ nhưng cực nhẹ, dùng khi mạng Wi-Fi chập chờn.
        *   **Bình thường (65%):** Khuyên dùng cho hầu hết các tiết học.
        *   **Cao (85%):** Rất sắc nét, dùng khi trình chiếu văn bản chữ nhỏ hoặc các dòng code lập trình.
    *   *Bắt buộc xem (FORCE_WATCH):* 
        *   **Tích chọn:** Màn hình học sinh sẽ bị khóa cứng trong suốt quá trình chiếu, học sinh không thể tắt đi để làm việc riêng.
        *   **Bỏ tích:** Học sinh có thể nhấn nút "✕ Đóng" ở góc trên bên phải để ẩn màn hình chiếu và thao tác trên máy mình khi cần.
3.  **👥 Gửi đến (Đối tượng nhận):**
    *   *Tất cả học sinh:* Chiếu đến 100% học sinh trong lớp.
    *   *Nhóm cụ thể:* Chỉ chiếu cho Tổ 1, Tổ 2, v.v...
    *   *Chọn từng HS:* Tích chọn thủ công các học sinh cần theo dõi bài giảng.

### Bước 3: Bắt đầu trình chiếu
Nhấn nút **▶️ Bắt đầu** ở góc dưới bên phải (hoặc phím tắt **Ctrl + Shift + S**).
*   Đèn tín hiệu chuyển sang **🟢 Đang phát (LIVE)**.
*   Khung xem trước **Live Preview** ở giữa sẽ hiển thị hình thu nhỏ nội dung đang chiếu trên máy giáo viên.
*   Đồng hồ đếm giờ bắt đầu chạy.

### Bước 4: Tạm dừng / Tiếp tục
*   Nhấn nút **⏸️ Tạm dừng** để tạm ngưng gửi hình mới đến học sinh (hình hiện tại vẫn giữ nguyên).
*   Nhấn lại **▶️ Tiếp tục** để chiếu tiếp.

### Bước 5: Dừng trình chiếu
Nhấn nút **⏹️ Dừng** để kết thúc hoàn toàn phiên chiếu. Màn hình chiếu sẽ tự đóng trên tất cả máy học sinh.

---

## II. HƯỚNG DẪN DÀNH CHO HỌC SINH (STUDENT GUIDE)

Khi giáo viên bấm bắt đầu trình chiếu màn hình, máy tính/tablet của học sinh sẽ tự động nhận lệnh qua kết nối mạng:

1.  **Nhận thông báo:** Một thông báo xuất hiện ở góc màn hình: **"🖥️ Màn hình giáo viên — GV đang chia sẻ màn hình. Hãy theo dõi!"**.
2.  **Xem loading:** ⏳ *MỚI* — Biểu tượng "Đang tải bài giảng từ máy giáo viên..." hiển thị trong khi chờ frame đầu tiên.
3.  **Xem bài giảng:** Màn hình trình chiếu sẽ hiển thị đè lên trên tất cả các cửa sổ làm việc của học sinh. 
    *   Thanh tiêu đề xanh dương phía trên hiển thị dòng chữ: **"🖥️ Màn hình giáo viên đang chiếu"** kèm theo đồng hồ thời gian thực và thời gian đã xem ở góc phải.
4.  **Thao tác thoát:**
    *   *Nếu giáo viên KHÔNG chọn "Bắt buộc xem":* Học sinh có thể nhấn nút **✕ Đóng** màu đỏ ở góc trên bên phải để tắt màn hình trình chiếu bất cứ lúc nào.
    *   *Nếu giáo viên BẬT "Bắt buộc xem":* Nút **✕ Đóng** sẽ biến mất. Toàn bộ các phím tắt thoát (`Alt+F4`, `Alt+Tab`, phím `Windows`) đều bị vô hiệu hóa. Học sinh bắt buộc phải tập trung theo dõi màn hình giáo viên giảng bài.

---

## III. BẢNG VÍ DỤ CẤU HÌNH THỰC TIỄN (SAMPLE USE CASES)

Dưới đây là các ví dụ cấu hình mẫu giúp giáo viên tối ưu hóa trải nghiệm dạy học trong các hoàn cảnh thực tế:

| Tình huống giảng dạy | Nguồn phát | Độ phân giải | Tần số quét (FPS) | Mức độ nén ảnh | Bắt buộc xem (Force) | Hiệu quả thực tế đạt được |
| :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| **Giảng slide bài PowerPoint** | Toàn màn hình | 1920×1080 | Thấp (1 fps) | Bình thường (65%) | Có (Tích chọn) | Bài giảng sắc nét, chữ rõ ràng, học sinh tập trung tuyệt đối vào slide giáo viên. |
| **Trình chiếu code lập trình / Công thức Math phức tạp** | Cửa sổ ứng dụng (VS Code/Word) | 2560×1440 | Trung bình (2 fps) | Cao (85%) | Có (Tích chọn) | Các dòng chữ nhỏ, ký hiệu toán học hiển thị cực kỳ nét, không bị vỡ hình hay mờ viền. |
| **Chạy thí nghiệm ảo tương tác (PhET / Desmos)** | Cửa sổ ứng dụng (Chrome) | 1280×720 | Mượt mà (15 fps) | Thấp (40%) | Có (Tích chọn) | Các chuyển động con trỏ chuột, hoạt ảnh thí nghiệm chạy mượt mà, không bị giật lag, mạng LAN nhẹ. |
| **Chiếu video giáo dục / game mô phỏng** ⚡ MỚI | Toàn màn hình | 1920×1080 | Rất mượt (30 fps) | Thấp (40%) | Có (Tích chọn) | Video/game chạy mượt gần thời gian thực. Yêu cầu mạng LAN >100Mbps. |
| **Hướng dẫn nhóm học sinh yếu thực hành** | Toàn màn hình | 1280×720 | Cao (5 fps) | Bình thường (65%) | Không (Bỏ tích) | Chỉ gửi bài giảng đến các học sinh được chọn. Học sinh có thể đóng màn hình chiếu để tự làm bài ngay sau khi hiểu. |

---

## IV. PHÍM TẮT (MỚI V2.1)

| Phím tắt | Chức năng | Ghi chú |
| :--- | :--- | :--- |
| **Ctrl + Shift + S** | Bắt đầu / Dừng chiếu màn hình | Phím tắt chính |
| **Ctrl + Shift + P** | Bật / tắt chế độ riêng tư | Ẩn nội dung nhạy cảm |

---

## V. HƯỚNG DẪN XỬ LÝ SỰ CỐ NHANH (TROUBLESHOOTING & FAQ)

### 1. Học sinh phản ánh màn hình bị đóng băng, không cập nhật slide mới?
*   *Nguyên nhân:* Có thể do giáo viên bấm **Tạm dừng** trên thanh công cụ hoặc kết nối Wi-Fi của học sinh bị rớt.
*   *Cách khắc phục:* 
    1. Giáo viên kiểm tra nút Tạm dừng có đang bị kích hoạt hay không.
    2. Giáo viên kiểm tra danh sách kết nối mạng ở mục "👥 Gửi đến" xem máy học sinh đó có biểu tượng màu xanh lá cây không. Nếu hiển thị màu xám, yêu cầu học sinh kết nối lại Wi-Fi của lớp học.

### 2. Máy học sinh hiển thị thông báo "401 Unauthorized" hoặc màn hình đen?
*   *Nguyên nhân:* Mã Token bảo mật giữa máy giáo viên và học sinh bị lệch (thường xảy ra nếu học sinh kết nối vào lớp học sau khi giáo viên đã bấm phát màn hình từ trước đó).
*   *Cách khắc phục:* Giáo viên chỉ cần bấm **Dừng** và bấm **Bắt đầu** phát lại màn hình. Hệ thống sẽ tự động cấp một Token bảo mật mới (dạng TK_xxxxxxxx) đồng bộ cho tất cả học sinh đang online.

### 3. Phòng học bị mất điện đột ngột hoặc máy giáo viên bị tắt, máy học sinh có bị khóa mãi mãi không?
*   *Nguyên nhân:* Tính năng `FORCE_WATCH` vô hiệu hóa bàn phím của học sinh.
*   *Cách khắc phục:* **Hoàn toàn AN TOÀN**. Hệ thống đã được tích hợp 7 cơ chế tự động bảo vệ (Safety Auto-Unlock). Khi mất kết nối với máy giáo viên quá **15 giây** (dual-channel: UDP + TCP), Client học sinh sẽ tự động đóng toàn bộ cửa sổ khóa và giải phóng Windows Keyboard Hook để học sinh sử dụng máy bình thường. Ngoài ra, nếu stream không kết nối từ đầu, nút đóng khẩn cấp sẽ xuất hiện sau 10 giây.

### 4. Hình ảnh chiếu bị giật / lag ở FPS cao?
*   *Nguyên nhân:* Băng thông mạng LAN không đủ cho cấu hình đã chọn.
*   *Cách khắc phục:* 
    1. Giảm FPS xuống 5fps hoặc 2fps.
    2. Giảm độ phân giải xuống 1280×720.
    3. Giảm nén ảnh xuống 40%.
    4. Kiểm tra switch mạng có hỗ trợ Gigabit không.

---

## VI. BẢNG TRANG WEB GIÁO DỤC ĐƯỢC PHÊ DUYỆT (WHITELIST)

Các trang web sau được hệ thống tự động cho phép khi sử dụng tab "Mở website":

| STT | Trang web | Mô tả |
| :---: | :--- | :--- |
| 1 | google.com | Tìm kiếm |
| 2 | youtube.com | Video giáo dục |
| 3 | wikipedia.org | Bách khoa toàn thư |
| 4 | kahoot.it | Quiz tương tác |
| 5 | quizlet.com | Flashcard học tập |
| 6 | hocmai.vn | Học trực tuyến VN |
| 7 | vietjack.com | Bài giải VN |
| 8 | loigiaihay.com | Lời giải hay VN |
| 9 | olm.vn | Online Math VN |
| 10 | violet.vn | Bài giảng điện tử VN |
| 11 | truonghoc247.com | Trường học 247 VN |
| 12 | toanhoc.org | Toán học VN |
| 13 | phet.colorado.edu | Thí nghiệm ảo PhET |
| 14 | *.edu.vn / *.edu | Mọi trang giáo dục |
| 15 | *.gov.vn | Mọi trang chính phủ VN |
