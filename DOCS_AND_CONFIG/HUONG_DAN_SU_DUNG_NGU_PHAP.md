# 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ NGỮ PHÁP TIẾNG ANH

Tài liệu này cung cấp hướng dẫn sử dụng trực quan, các mẫu dữ liệu thực tế và sơ đồ tương tác giúp **Giáo viên** và **Học sinh** khai thác tối đa mô-đun **Công cụ học tập Ngữ pháp tiếng Anh** trên hệ thống QA SmartClass.

---

## 🗺️ Sơ Đồ Tương Tác Hệ Thống (Workflow Diagram)

Dưới đây là quy trình tương tác thời gian thực giữa Giáo viên, Học sinh và Cơ sở dữ liệu SQLite trong quá trình học tập và kiểm tra:

```mermaid
sequenceDiagram
    autonumber
    actor GV as Giáo viên (Teacher Client)
    actor HS as Học sinh (Student Client)
    database DB as Cơ sở dữ liệu (SQLite)

    Note over GV, DB: 1. Khởi tạo & Tải dữ liệu
    DB-->>GV: Tải 12 thì & Ngân hàng đề thi động
    DB-->>HS: Tải câu hỏi & Công thức ngữ pháp

    Note over GV: 2. Giảng dạy tương tác (Tab 1)
    GV->>GV: Hover vào Thẻ ngữ pháp → Hiện Mini Toolbar
    GV->>HS: Bấm [🎯 Focus] → Đồng bộ phóng to thẻ của thì đó trên máy học sinh
    GV->>GV: Bấm [🖊️ Bảng trắng] → Chụp ảnh thẻ gửi vào Whiteboard để vẽ minh họa

    Note over HS: 3. Luyện tập & Thử thách (Tab 2 & 3)
    HS->>HS: Làm bài trắc nghiệm (Timer đếm ngược 15s)
    HS->>DB: Kết thúc 10 câu → Tự động lưu Lịch sử làm bài (GrammarQuizHistories)

    Note over GV: 4. Báo cáo & Đánh giá
    GV->>DB: Truy vấn bảng Lịch sử làm bài
    GV->>GV: Bấm [📊 Xuất lịch sử] → Lưu thành file CSV (Excel) UTF-8 BOM
```

---

## 👩‍🏫 DÀNH CHO GIÁO VIÊN (TEACHER GUIDE)

Giáo viên sử dụng công cụ để trực quan hóa bài giảng ngữ pháp, tập trung sự chú ý của học sinh và xuất dữ liệu điểm số.

### 1. Trực quan hóa bài giảng bằng Mini Toolbar (Hover Section Tool)
Khi giáo viên di chuyển chuột (Hover) vào bất kỳ thẻ thì nào trong tab **📜 Bảng ngữ pháp**, một Mini Toolbar sẽ xuất hiện ở góc trên bên phải của thẻ đó:

```
+--------------------------------------------------------+
| 📌 Simple Present — Hiện tại đơn       [🎯 Focus] [❌ Unfocus] [🖊️ Bảng trắng] |
| 🔧 S + V_inf / V(s/es)                                 |
+--------------------------------------------------------+
```

*   **Nút 🎯 Focus:** Khi giáo viên click nút này, hệ thống sẽ kích hoạt lệnh Socket gửi tới tất cả máy học sinh trong lớp. Trên màn hình của học sinh, toàn bộ các thẻ ngữ pháp khác sẽ tự động ẩn đi (chỉ hiển thị duy nhất nội dung của thẻ được chọn) và các tab bài tập sẽ tạm thời bị khóa. Tiêu đề lớp học của học sinh sẽ hiển thị phân biệt rõ ràng: `🎯 Ngữ pháp tiếng Anh - [Focus: <Tên Thì>]`.
*   **Nút ❌ Unfocus:** Hủy bỏ trạng thái Focus thẻ, hiển thị lại toàn bộ 12 thì ngữ pháp và các tab bài tập cho học sinh.
*   **Nút 🖊️ Bảng trắng:** Chụp màn hình thẻ công thức đó và dán trực tiếp vào **Bảng trắng kỹ thuật số** của lớp học để giáo viên viết vẽ, phân tích ví dụ trực tiếp bằng bút cảm ứng.

### 2. Xuất dữ liệu Lịch sử học tập (Excel/CSV Export)
*   **Bước 1:** Trên thanh tiêu đề góc phải của mô-đun, click nút **📊 Xuất lịch sử (Excel)**.
*   **Bước 2:** Cửa sổ lưu file hiện ra, chọn vị trí lưu và đặt tên file `.csv`.
*   **Bước 3:** Click **Save**. File sẽ được xuất dưới định dạng UTF-8 BOM, giáo viên có thể mở trực tiếp bằng Microsoft Excel để lấy điểm số của học sinh mà không bị lỗi hiển thị tiếng Việt.

---

## 🧑‍🎓 DÀNH CHO HỌC SINH (STUDENT GUIDE)

Giao diện học sinh được thiết kế trực quan, dễ thao tác cảm ứng và có cơ chế chấm điểm nhanh chóng.

### 1. 📜 Học bảng ngữ pháp (Tab 1)
*   Học sinh xem danh sách 12 thì được phân chia mã màu rõ rệt: **Màu xanh dương** (Hiện tại), **Màu xanh lá** (Quá khứ), **Màu cam** (Tương lai).
*   **Trạng thái Focus Thẻ:** Khi giáo viên bấm Focus vào một thẻ (ví dụ: *Present Continuous*), học sinh sẽ chỉ nhìn thấy duy nhất thẻ đó trên màn hình, các thẻ khác tạm thời ẩn đi và các tab bài tập tạm thời bị khóa. Tiêu đề lớp học hiển thị rõ rệt: `🎯 Ngữ pháp tiếng Anh - [Focus: Present Continuous]`. Khi giáo viên bỏ Focus, hệ thống trả lại giao diện 12 thì đầy đủ.
*   Giao diện tự động căn chỉnh ở giữa màn hình, cỡ chữ to rõ nét nên học sinh ở bàn cuối lớp vẫn dễ dàng quan sát trên máy chiếu hoặc bảng tương tác.

### 2. ✏️ Quiz điền từ (Tab 2)
*   **Bước 1:** Nhấn nút **Bắt đầu (10 câu)**. Ô nhập liệu sẽ xuất hiện.
*   **Bước 2:** Gõ câu trả lời vào ô trống. Nếu dùng thiết bị cảm ứng, bàn phím ảo sẽ tự động mở.
*   **Bước 3:** Nhấn **Enter** trên bàn phím để nộp bài.
*   *Lưu ý:* Ngay khi nhấn Enter, ô nhập liệu sẽ chuyển màu xám và khóa tương tác trong `1.5 giây` để hiển thị kết quả đúng/sai kèm âm thanh phản hồi (Ting/Tèng). Bạn không cần lo lắng nếu lỡ gõ thừa khoảng trắng vì hệ thống sẽ tự động lọc bỏ khoảng trắng thừa.

### 3. 🎯 Trắc nghiệm thì & Đồng hồ đếm ngược 15 giây (Tab 3)
*   **Bước 1:** Nhấn nút **Bắt đầu (10 câu)**. Đồng hồ đếm ngược hình ProgressBar màu đỏ ở góc phải bắt đầu chạy giật lùi từ 15s về 0s.
*   **Bước 2:** Chọn 1 trong 4 đáp án bên dưới.
*   *Lưu ý chống hack:* Bạn chỉ được chọn duy nhất một lần. Ngay khi click, toàn bộ 4 đáp án sẽ bị khóa (Disabled) và đồng hồ đếm ngược lập tiếp dừng lại. Nếu hết 15 giây bạn không chọn, hệ thống tự động tính là **Sai** và chuyển câu.

---

## 💡 TAB ỨNG DỤNG THỰC TẾ (PRACTICAL APPLICATIONS)

Hệ thống cung cấp thêm một tab chuyên biệt mô tả **6 lĩnh vực cốt lõi** ứng dụng ngữ pháp tiếng Anh trong đời sống, giúp chương trình sinh động và gắn liền với thực tiễn.

> [!NOTE]
> Giao diện tự động phát hiện ngôn ngữ hệ thống: Khi thiết lập hệ thống là tiếng Việt, các thẻ sẽ tự động tải các hình ảnh có hậu tố `_VN` và văn bản tiếng Việt. Khi thiết lập là tiếng Anh, hệ thống sẽ tải hình ảnh `_EN` và văn bản tiếng Anh.

### Sơ đồ 6 ứng dụng thực tế trên giao diện:

| STT | Ứng dụng thực tế | Mô tả ứng dụng | Hình ảnh tương ứng |
| :--- | :--- | :--- | :--- |
| 1 | **Viết luận & Email** | Hỗ trợ soạn thảo email công việc, bài luận học thuật và báo cáo nghiên cứu chính xác, logic. | `app_grammar_1_VN.png` / `_EN.png` |
| 2 | **Giao tiếp hàng ngày** | Diễn đạt mốc thời gian rõ ràng, giúp hội thoại tự nhiên, tránh hiểu lầm đáng tiếc. | `app_grammar_2_VN.png` / `_EN.png` |
| 3 | **Phỏng vấn xin việc** | Phối hợp thì quá khứ (Past Simple) và hiện tại hoàn thành (Present Perfect) để trình bày kinh nghiệm. | `app_grammar_3_VN.png` / `_EN.png` |
| 4 | **Kỳ thi quốc tế** | Chinh phục các bài thi chuẩn hóa quốc tế (IELTS, TOEFL, TOEIC) nhờ nền tảng ngữ pháp vững chắc. | `app_grammar_4_VN.png` / `_EN.png` |
| 5 | **Thuyết trình dự án** | Trình bày dự án chuyên nghiệp, kết nối mạch lạc các mốc thời gian phát triển trong quá khứ và tương lai. | `app_grammar_5_VN.png` / `_EN.png` |
| 6 | **Đọc báo & Tin tức** | Hiểu sâu sắc các sắc thái ngữ nghĩa của tin tức toàn cầu thông qua việc nắm rõ cấu trúc ngữ pháp. | `app_grammar_6_VN.png` / `_EN.png` |

---

## 💾 DỮ LIỆU MẪU & VÍ DỤ CỤ THỂ (SAMPLE DATA & EXAMPLES)

### 1. Mẫu dữ liệu các thì (Bảng SQLite `GrammarTenses`)

Dưới đây là 3 dòng dữ liệu mẫu đại diện cho 3 mốc thời gian trong cơ sở dữ liệu:

| Id | Name | NameVi | Structure | Example | Signal | Color |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | Simple Present | Hiện tại đơn | `S + V_inf / V(s/es)` | I go to school every day. / She goes to school. | always, usually, every day, often | `#1565C0` (Blue) |
| **5** | Simple Past | Quá khứ đơn | `S + V2/ed` | I went to Hanoi last year. | yesterday, last night, ago, in 2025 | `#2E7D32` (Green) |
| **9** | Simple Future | Tương lai đơn | `S + will + V_inf` | I will call you tomorrow. | tomorrow, next week, in the future | `#E65100` (Orange) |

### 2. Mẫu dữ liệu ngân hàng câu hỏi (Bảng SQLite `GrammarQuestions`)

*   **Dạng câu hỏi điền từ (`Category = "FillBlank"`):**
    *   *QuestionText:* `We ___ (study) English for 3 years.`
    *   *Hint:* `Chia động từ`
    *   *AnswersJson:* `["have studied", "have been studying"]` *(Hỗ trợ nhiều đáp án)*

*   **Dạng câu hỏi trắc nghiệm (`Category = "MultipleChoice"`):**
    *   *QuestionText:* `"She is studying now."`
    *   *Hint:* `Xác định thì phù hợp`
    *   *AnswersJson:* `["Hiện tại tiếp diễn"]`
    *   *OptionsJson:* `["Hiện tại đơn", "Hiện tại tiếp diễn", "Quá khứ tiếp diễn", "Hiện tại hoàn thành"]`

### 3. Ví dụ phân biệt lỗi ngữ pháp thường gặp

> [!WARNING]
> **Lỗi nhầm lẫn thì Hiện tại hoàn thành và Quá khứ đơn:**
> *   *Sai:* `I worked at this company since 2020.` (Sử dụng Quá khứ đơn với từ nhận biết "since").
> *   *Đúng:* `I have worked at this company since 2020.` (Hiện tại hoàn thành diễn tả hành động bắt đầu trong quá khứ và vẫn tiếp diễn ở hiện tại).
> *   *Đúng:* `I worked at this company in 2020.` (Quá khứ đơn diễn tả hành động đã kết thúc hoàn toàn trong quá khứ).

### 4. Ví dụ file Lịch sử thi xuất ra Excel (CSV Format)

Khi giáo viên bấm xuất lịch sử, file `.csv` nhận được sẽ có nội dung hiển thị trong Excel như sau:

| ID | Mã Học Sinh | Tên Học Sinh | Khối | Loại Đề | Số Câu Đúng | Tổng Số Câu | Điểm Số | Thời Gian (Giây) | Ngày Hoàn Thành |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | HS_TEST | Học sinh thử nghiệm | Khối 6 | Điền từ | 8 | 10 | 8.0 | 45.3 | 2026-06-18 19:15:30 |
| **2** | HS_TEST | Học sinh thử nghiệm | Khối 6 | Trắc nghiệm | 10 | 10 | 10.0 | 32.1 | 2026-06-18 19:20:12 |
| **3** | HS_TEST | Học sinh thử nghiệm | Khối 6 | Trắc nghiệm | 5 | 10 | 5.0 | 50.8 | 2026-06-18 19:22:45 |
