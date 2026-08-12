# HƯỚNG DẪN SỬ DỤNG CÔNG CỤ HỌC TẬP KANBAN BOARD

Tài liệu này hướng dẫn chi tiết cách sử dụng công cụ học tập **Kanban Board** kết hợp cùng bộ đếm **Pomodoro** trong hệ thống **QA SmartClass**. Tài liệu cung cấp quy trình thao tác trực quan từng bước cùng các kịch bản dữ liệu mẫu cụ thể giúp Giáo viên và Học sinh dễ dàng áp dụng vào thực tế giảng dạy và học tập.

---

## I. TỔNG QUAN VỀ PHƯƠNG PHÁP KANBAN & POMODORO

### 1. Kanban Board là gì?
**Kanban** (tiếng Nhật: かんばん - có nghĩa là "bảng trực quan") là phương pháp quản lý công việc được phát triển bởi Taiichi Ohno tại hãng Toyota để tối ưu hóa quy trình sản xuất. 
Trong môi trường giáo dục, Kanban giúp trực quan hóa toàn bộ bài tập, nhiệm vụ học tập dưới dạng các thẻ màu di chuyển qua 3 cột trạng thái:
*   **📋 TO DO (Cần làm) - Màu Xanh Dương:** Các nhiệm vụ chưa bắt đầu.
*   **⏳ DOING (Đang làm) - Màu Cam:** Những việc đang được tập trung giải quyết ngay lúc này. 
    *   *Quy tắc WIP (Work In Progress):* Để tránh quá tải và mất tập trung, mỗi học sinh/nhóm chỉ nên có tối đa **3 thẻ** ở cột này tại một thời điểm!
*   **✅ DONE (Hoàn thành) - Màu Xanh Lá:** Nơi tập hợp tất cả nhiệm vụ đã hoàn thành. Cột này càng nhiều thẻ sẽ càng kích thích cảm giác thành tựu và hứng thú học tập của các em.

### 2. Bộ đếm thời gian Pomodoro (⏱️ Tập trung)
Phương pháp quả cà chua Pomodoro giúp rèn luyện khả năng tập trung cao độ bằng cách chia thời gian làm việc thành các chu kỳ: **25 phút tập trung** làm việc, sau đó **nghỉ ngơi 5 phút**. Sự kết hợp giữa Kanban (quản lý đầu việc) và Pomodoro (quản lý thời gian) tạo nên bộ công cụ hoàn hảo hỗ trợ học sinh tự học và tự quản.

---

## II. QUY TRÌNH THAO TÁC 4 BƯỚC TRỰC QUAN

```
 +-------------------------------------------------------------------------------+
 | BƯỚC 1: KHỞI TẠO BẢNG & THÊM THẺ VIỆC                                         |
 | • Cách A: Chọn mẫu từ nút [🎯 Template ▼] trên Header để tải dữ liệu mẫu.      |
 | • Cách B: Gõ tiêu đề công việc vào ô [📝 Thêm thẻ mới] rồi nhấn Enter.        |
 | ➔ Kết quả: Thẻ xuất hiện ở cột TO DO với màu xanh dương nhạt dịu mắt.          |
 +-------------------------------------------------------------------------------+
                                         ↓
 +-------------------------------------------------------------------------------+
 | BƯỚC 2: CHỈNH SỬA CHI TIẾT THẺ                                                 |
 | • Nhấp trực tiếp vào phần tiêu đề in đậm của thẻ để sửa tên công việc.        |
 | • Nhấp vào ô màu xám nhạt bên dưới để ghi chú chi tiết (người làm, hạn chót).|
 | ➔ Kết quả: Nội dung tự động cập nhật, chữ to và đậm giúp dễ đọc từ xa.        |
 +-------------------------------------------------------------------------------+
                                         ↓
 +-------------------------------------------------------------------------------+
 | BƯỚC 3: TẬP TRUNG THỰC HIỆN (DOING) & BẬT ĐỒNG HỒ                             |
 | • Di chuyển thẻ sang cột DOING bằng cách kéo tay cầm [⋮⋮] hoặc bấm phím [▶].   |
 | • Bấm nút [▶] trên bộ đếm [⏱️ Tập trung] ở Header để đếm ngược 25 phút.       |
 | ➔ Kết quả: Thẻ chuyển sang màu cam. Hết 25 phút, chuông báo nhắc nghỉ ngơi.    |
 +-------------------------------------------------------------------------------+
                                         ↓
 +-------------------------------------------------------------------------------+
 | BƯỚC 4: HOÀN THÀNH (DONE) & LƯU TRỮ HỆ THỐNG                                  |
 | • Di chuyển thẻ sang cột DONE. Thẻ tự động chuyển sang màu xanh lá sinh động. |
 | • Âm thanh chúc mừng vang lên; hệ thống tự động lưu trữ trạng thái vào SQLite.|
 | ➔ Kết quả: Công việc được ghi nhận an toàn, mở lại app bài làm vẫn nguyên vẹn. |
 +-------------------------------------------------------------------------------+
```

---

## III. BẢNG HƯỚNG DẪN THAO TÁC CHI TIẾT

| Bước | Hành động | Hướng dẫn thao tác cụ thể | Kết quả hiển thị trên giao diện |
| :--- | :--- | :--- | :--- |
| **Bước 1** | **Khởi tạo việc** | Chọn kịch bản mẫu từ menu **🎯 Template ▼** hoặc nhập tên việc vào ô nhập nhanh rồi click **➕ Thêm vào To Do** (hoặc nhấn `Enter`). | Thẻ việc mới được thêm vào cột **📋 TO DO** với màu xanh dương pastel. |
| **Bước 2** | **Ghi thông tin** | Click vào TextBox tiêu đề của thẻ để sửa nội dung. Click vào TextBox bên dưới để điền mô tả (thời hạn, người phụ trách, yêu cầu cần đạt...). | Thẻ được cập nhật nội dung tức thì. Tay cầm `⋮⋮` bảo vệ giúp thao tác chỉnh sửa không bị kéo trượt thẻ. |
| **Bước 3** | **Di chuyển thẻ** | **Thao tác chuột:** Click giữ chuột vào tay cầm **`⋮⋮`** ở bên trái thẻ và kéo thả sang cột khác.<br>**Thao tác chạm cảm ứng:** Bấm nút **`▶`** dưới đáy thẻ để chuyển sang cột tiếp theo, hoặc **`◀`** để lùi cột. | Thẻ tự động đổi màu sắc theo trạng thái cột (Xanh dương ở To Do $\rightarrow$ Cam ở Doing $\rightarrow$ Xanh lá ở Done) kèm hiệu ứng co giãn nhẹ (Scale Pop). |
| **Bước 4** | **Bật Pomodoro** | Khi thẻ ở cột **Doing**, bấm nút **▶** trên bộ đếm **⏱️ Tập trung** để bắt đầu chu kỳ 25 phút. Có thể bấm **⏸** để tạm dừng hoặc **🔄** để đặt lại. | Thời gian đếm ngược trực quan. Khi hết giờ, phát âm thanh báo động và hiện thông báo nhắc học sinh nghỉ 5 phút. |
| **Bước 5** | **Tự động lưu** | Không cần thực hiện gì. Hệ thống tự động lưu trạng thái khi thêm, xóa, di chuyển thẻ và khi TextBox mất focus (`LostFocus`). | Trạng thái bảng được ghi nhận lâu dài vào SQLite cục bộ và tự động tải lại khi mở app. |
| **Bước 6** | **Dọn sạch bảng** | Bấm nút **🗑️ Xóa bảng** ở góc trên bên phải để dọn dẹp toàn bộ thẻ việc khi kết thúc tuần học hoặc dự án. | Hiện thông báo xác nhận nếu có thẻ, hoặc tự động làm sạch nếu bảng trống. |

---

## IV. CÁC KỊCH BẢN VÀ DỮ LIỆU MẪU HỌC ĐƯỜNG THỰC TẾ

Dưới đây là các ví dụ cụ thể giúp Giáo viên và Học sinh áp dụng ngay vào các hoạt động hàng ngày trong nhà trường:

### 1. Kịch bản dành cho Học sinh: Kế hoạch ôn tập kiểm tra học kỳ (Cá nhân)
*Giúp học sinh tự sắp xếp lộ trình ôn tập cá nhân một cách khoa học.*

*   **📋 Cột TO DO (Cần làm):**
    *   *Thẻ 1:* **Giải đề thi thử Toán số 3**
        *   *Chi tiết:* "Làm đề trong sách ôn luyện, tự bấm giờ đúng 90 phút. Hạn chót: Thứ Tư."
    *   *Thẻ 2:* **Học thuộc 20 từ vựng Tiếng Anh Unit 6**
        *   *Chi tiết:* "Chủ đề: Lối sống xanh. Tập đặt câu với các từ mới."
    *   *Thẻ 3:* **Viết dàn ý bài văn Nghị luận xã hội**
        *   *Chi tiết:* "Đề bài: Tầm quan trọng của tinh thần tự học. Chuẩn bị 3 dẫn chứng thực tế."
*   **⏳ Cột DOING (Đang làm):**
    *   *Thẻ 4:* **Tóm tắt Lịch sử chương 3 bằng Sơ đồ tư duy**
        *   *Chi tiết:* "Giai đoạn lịch sử Việt Nam từ 1930 đến 1945. Vẽ trên giấy A3 nhiều màu sắc."
*   **✅ Cột DONE (Hoàn thành):**
    *   *Thẻ 5:* **Học thuộc các công thức Lý chương 4**
        *   *Chi tiết:* "Công thức phần Dao động và Sóng điện từ. Đã tự khảo bài đạt điểm tối đa."

---

### 2. Kịch bản dành cho Nhóm học sinh: Thực hiện dự án STEM "Trường học Xanh"
*Phân chia công việc minh bạch và theo dõi tiến độ phối hợp nhóm.*

*   **📋 Cột TO DO (Cần làm):**
    *   *Thẻ 1:* **[Nam] In ấn tài liệu báo cáo STEM**
        *   *Chi tiết:* "In file Word báo cáo và đóng tập bìa kiếng gửi thầy cô chấm điểm."
    *   *Thẻ 2:* **[Hoa & Tuấn] Quay video thuyết trình mô hình**
        *   *Chi tiết:* "Quay tại phòng thực hành lý. Hoa dựng video xuất file MP4."
*   **⏳ Cột DOING (Đang làm):**
    *   *Thẻ 3:* **[Minh] Lắp ráp mô hình tấm pin mặt trời mini**
        *   *Chi tiết:* "Hàn dây điện kết nối tấm pin năng lượng với hệ thống đèn LED."
*   **✅ Cột DONE (Hoàn thành):**
    *   *Thẻ 4:* **[Lan] Lên ý tưởng thiết kế & Bản vẽ phác thảo**
        *   *Chi tiết:* "Đã thống nhất thiết kế 3D và được giáo viên hướng dẫn phê duyệt."

---

### 3. Kịch bản dành cho Giáo viên: Quản lý công việc giảng dạy và chuẩn bị bài giảng
*Giáo viên tự sắp xếp và theo dõi các đầu việc dạy học trong tuần.*

*   **📋 Cột TO DO (Cần làm):**
    *   *Thẻ 1:* **Thiết kế slide bài giảng tương tác môn Hóa**
        *   *Chi tiết:* "Bài: Hóa học hữu cơ. Thêm video thí nghiệm ảo và câu hỏi trắc nghiệm tương tác."
    *   *Thẻ 2:* **Chấm bài kiểm tra giữa kỳ lớp 11A2**
        *   *Chi tiết:* "Tổng số 45 bài tự luận môn Ngữ Văn. Hạn trả bài: Thứ Sáu."
*   **⏳ Cột DOING (Đang làm):**
    *   *Thẻ 3:* **Chuẩn bị dụng cụ thí nghiệm Vật lý lớp 10**
        *   *Chi tiết:* "Bài: Đo gia tốc rơi tự do. Liên hệ phòng Lab mượn bộ cổng quang điện và đồng hồ hiện số."
*   **✅ Cột DONE (Hoàn thành):**
    *   *Thẻ 4:* **Gửi phiếu bài tập cuối tuần lên ứng dụng lớp học**
        *   *Chi tiết:* "Đã đăng file PDF bài tập tuần 12 lên hệ thống để phụ huynh nhắc nhở học sinh làm bài."

---

### 4. Kịch bản dành cho Ban cán sự lớp: Kế hoạch trực nhật & trang trí lớp (Tự quản lớp học)
*Giúp ban cán sự điều hành công việc tập thể lớp trực quan.*

*   **📋 Cột TO DO (Cần làm):**
    *   *Thẻ 1:* **[Tổ 3] Giặt và treo lại rèm cửa lớp học**
        *   *Chi tiết:* "Thực hiện vào sáng Chủ nhật để đầu tuần lớp sạch sẽ."
    *   *Thẻ 2:* **[Ban văn thể] Vẽ trang trí bảng lớp chủ đề 20/11**
        *   *Chi tiết:* "Vẽ phấn màu trang trí tri ân Thầy Cô."
*   **⏳ Cột DOING (Đang làm):**
    *   *Thẻ 3:* **[Tổ 2] Sắp xếp và phân loại tủ sách dùng chung**
        *   *Chi tiết:* "Gom sách cũ, chia ngăn truyện tranh, sách khoa học, tạp chí."
*   **✅ Cột DONE (Hoàn thành):**
    *   *Thẻ 4:* **[Lớp trưởng] Phân công lịch trực nhật tuần học mới**
        *   *Chi tiết:* "Đã lập danh sách trực nhật và dán lên bảng tin lớp."

---

## V. CÁC QUY TẮC VÀNG ĐỂ ÁP DỤNG KANBAN THÀNH CÔNG

Để bảng Kanban phát huy tối đa hiệu quả sư phạm, giáo viên nên nhắc nhở học sinh tuân thủ 3 quy tắc sau:
1.  **Tôn trọng WIP Limit:** Tuyệt đối không kéo quá 3 thẻ việc vào cột **Doing**. Việc tập trung giải quyết dứt điểm từng công việc giúp nâng cao hiệu suất và chất lượng học tập rõ rệt.
2.  **Họp nhóm nhanh 5 phút (Daily Standup):** Khi làm dự án nhóm, các thành viên nên dành 5 phút đầu giờ để đứng trước bảng Kanban, chia sẻ những việc đã làm xong, những việc sẽ làm hôm nay, và những khó khăn cần nhóm hỗ trợ.
3.  **Tự giác và trung thực:** Học sinh tự chịu trách nhiệm về tiến độ của mình trên bảng. Tinh thần tự giác là yếu tố quyết định sự thành công của phương pháp học tập tự quản Agile.
