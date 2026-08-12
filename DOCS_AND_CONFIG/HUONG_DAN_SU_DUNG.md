# 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: BÀI TẬP VỀ NHÀ (HOMEWORK)

Tài liệu này cung cấp hướng dẫn chi tiết, hình ảnh minh họa quy trình và các mẫu dữ liệu thực tế giúp **Giáo viên** và **Học sinh** sử dụng tối đa hiệu năng của mô-đun **4.4. Bài tập về nhà** trên hệ thống QA SmartClass.

---

## 🗺️ Quy Trình Tổng Quan Hệ Thống

Dưới đây là sơ đồ tương tác thời gian thực giữa Giáo viên và Học sinh trong luồng bài tập về nhà:

```mermaid
sequenceDiagram
    autonumber
    actor GV as Giáo viên (Teacher Client)
    actor HS as Học sinh (Student Client)
    database DB as Cơ sở dữ liệu (SQLite)

    Note over GV: Bước 1: Chuẩn bị nội dung
    rect rgb(235, 245, 255)
        GV->>GV: Tải file mẫu Word (.docx)<br/>Hoặc nhập tay trực tiếp trên Form
    end

    Note over GV, DB: Bước 2: Giao bài tập
    GV->>DB: Lưu thông tin bài tập vào CSDL
    GV->>HS: Gửi lệnh Socket TCP (CMD|ASSIGNMENT|...) kèm thông tin & file đính kèm

    Note over HS, DB: Bước 3: Học sinh nhận & làm bài
    HS->>DB: Lưu bài tập nhận được vào CSDL cục bộ
    loop Tự động lưu nháp (Mỗi 15 giây)
        HS->>HS: Tự động sao lưu bài đang viết ra file tạm
        Note over HS: Hiển thị trạng thái "✓ Đã lưu nháp lúc HH:mm:ss"
    end

    Note over HS, GV: Bước 4: Nộp bài
    HS->>HS: Kiểm tra chất lượng tệp tin (Loại bỏ các file trống 0-byte)
    HS->>GV: Gửi file bài làm qua Socket TCP
    HS->>DB: Ghi nhận lịch sử nộp bài thành công
    GV->>GV: Theo dõi danh sách và chấm điểm bài làm
```

---

## 👩‍🏫 DÀNH CHO GIÁO VIÊN (TEACHER GUIDE)

Giáo viên có hai phương thức để giao bài tập: **Giao thủ công trực tiếp** hoặc **Import từ file Word mẫu**.

### 1. Quy định định dạng File Word mẫu để Import tự động

Hệ thống hỗ trợ đọc tự động file Word (.docx). Để hệ thống nhận dạng chính xác các trường dữ liệu, file Word cần có định dạng như sau:

> [!IMPORTANT]
> - Giữ nguyên tên các nhãn trước dấu hai chấm (`:`).
> - Mỗi nhãn nằm trên một dòng riêng.
> - Hạn nộp phải đúng định dạng ngày giờ (`dd/MM/yyyy` hoặc `dd/MM/yyyy HH:mm`).

#### 📝 Ví dụ mẫu nội dung file Word:
```text
Môn học: Toán
Tiêu đề: Bài tập Đại số - Phương trình bậc hai
Nội dung: 
Giải các phương trình sau đây và biện luận nghiệm theo tham số m:
1) x^2 - 2(m-1)x + m^2 - 3 = 0
2) 3x^2 + 5x - 2 = 0
Hạn nộp: 25/06/2026 23:59
Ghi chú: Trình bày chi tiết các bước giải và kết luận.
```

---

### 2. Giao diện Form nhập liệu trực quan (Touch-friendly Dialog)

Khi nhấn nút **➕ Giao BTVN mới** hoặc Import lỗi hạn nộp, cửa sổ rộng `620x650` sẽ hiển thị giúp giáo viên dễ dàng tương tác cảm ứng:

| Trường thông tin | Loại Control | Hướng dẫn nhập liệu | Quy tắc xác thực (Validation) |
| :--- | :--- | :--- | :--- |
| **Môn học** | ComboBox | Chọn từ danh sách môn học tích hợp (GDPT 2018) hoặc tự gõ | Không được bỏ trống. Không chứa ký tự `\|` |
| **Tiêu đề** | TextBox | Gõ tiêu đề bài tập ngắn gọn (Cỡ chữ to 14px) | Tối đa **100 ký tự**. Không được bỏ trống |
| **Nội dung** | TextBox | Nhập yêu cầu, đề bài chi tiết (Có thanh cuộn) | Tối đa **2000 ký tự** |
| **Ngày hạn nộp** | DatePicker | Chọn ngày hết hạn nộp bài trên lịch (Cỡ lớn 36px) | Bắt buộc chọn. Phải là ngày trong tương lai |
| **Giờ hạn nộp** | ComboBox | Chọn giờ cụ thể (ví dụ: `17:00`, `23:59`) | Phải đúng định dạng giờ `HH:mm` |
| **File đính kèm** | TextBox + Button | Chọn thêm tài liệu đính kèm gửi tới học sinh | Hỗ trợ mọi định dạng file |

> [!WARNING]
> Nếu import file Word có định dạng ngày bị sai (ví dụ: `Hạn nộp: Ngày mai`), hệ thống sẽ hiển thị cảnh báo, bỏ trống ô **Ngày hạn nộp** và khóa nút **Lưu** cho tới khi giáo viên chọn lại thủ công một ngày hợp lệ.

---

## 🧑‍🎓 DÀNH CHO HỌC SINH (STUDENT GUIDE)

Giao diện học sinh được tối ưu hóa tối đa với 3 thẻ (Tab) chính:

```
┌────────────────────────────────────────────────────────────────────────┐
│  [📩 Bài tập nhận được]     [📝 Soạn bài làm]     [📤 Lịch sử nộp bài]   │
└────────────────────────────────────────────────────────────────────────┘
```

### 1. 📩 Bài tập nhận được (Thẻ mặc định)
- Học sinh xem danh sách bài tập được giao.
- **Cảnh báo hạn nộp động bằng màu sắc sư phạm:**
  -  **Màu xanh lá (`#2E7D32`):** Còn hạn trên 24 giờ (Thoải mái thời gian).
  -  **Màu cam (`#E65100`):** Còn hạn dưới 24 giờ (Cần chú ý làm bài).
  -  **Màu đỏ (`#C62828`):** Còn hạn dưới 2 giờ hoặc Đã quá hạn (Cực kỳ khẩn cấp).

---

### 2. 📝 Soạn bài làm trực tiếp (Thẻ soạn thảo)
- Học sinh viết bài luận, bài văn, hoặc câu trả lời trực tiếp trong phần mềm.
- **Tự động lưu nháp thông minh (Autosave):**
  - Hệ thống tự động lưu nháp mỗi **15 giây** hoặc **ngay lập tức** khi học sinh nhấp chuyển Tab.
  - Đèn chỉ thị hiển thị trạng thái: `✓ Đã lưu nháp lúc 15:32:05` cạnh số từ để tạo sự an tâm tuyệt đối.
  - Nếu xảy ra sự cố sập nguồn hoặc tắt máy đột ngột, khi mở lại phần mềm sẽ hiện thông báo khôi phục bản thảo cũ.

---

### 3. 📤 Nộp file bài làm (Nộp đính kèm & kéo thả)
Học sinh có thể chọn nộp nhiều file (như ảnh chụp vở bài tập, file PDF, file Word).

> [!TIP]
> **Quy tắc gộp cảnh báo tệp trống:**
> - Nếu học sinh vô tình chọn các file 0-byte (file trống không có dữ liệu), hệ thống sẽ gộp tất cả tên các file đó và hiển thị đúng **1 hộp thoại cảnh báo duy nhất**.
> - Chỉ những file có dung lượng hợp lệ (> 0-byte) mới được tiến hành gửi lên máy giáo viên, giúp tránh làm nghẽn đường truyền mạng.

---

## 📊 DỮ LIỆU MẪU THỰC TẾ (SAMPLE DATA EXAMPLES)

Dưới đây là 3 mẫu bài tập thực tế thường gặp đã được tích hợp sẵn vào Cơ sở dữ liệu của phần mềm để kiểm tra và vận hành thử nghiệm:

### 📐 Mẫu 1: Môn Toán học (Cấp THCS/THPT)
*   **Tiêu đề:** `Bài tập về nhà: Giải hệ phương trình và căn thức`
*   **Môn học:** `Toán`
*   **Nội dung:**
    ```text
    Học sinh thực hiện làm các bài tập sau vào vở hoặc soạn bài trên hệ thống:

    Bài 1: Giải hệ phương trình sau:
      a) 2x + y = 5
         3x - y = 5
      b) x - 2y = -1
         2x + y = 3

    Bài 2: Rút gọn biểu thức A = (√x / (√x - 1)) - (1 / (x - √x)) với x > 0 và x ≠ 1.
    ```
*   **Ghi chú:** `Chụp ảnh lời giải chi tiết hoặc nộp file ảnh/PDF đính kèm.`

### ✍️ Mẫu 2: Môn Ngữ Văn (Nghị luận xã hội)
*   **Tiêu đề:** `Viết đoạn văn nghị luận về tình mẫu tử`
*   **Môn học:** `Ngữ Văn`
*   **Nội dung:**
    ```text
    Đề bài: Viết một đoạn văn (khoảng 200 chữ) trình bày suy nghĩ của em về vai trò của tình mẫu tử trong cuộc sống của mỗi con người.

    💡 Gợi ý:
    - Mở đoạn: Giới thiệu vấn đề nghị luận (tình mẫu tử là gì, tầm quan trọng).
    - Thân đoạn: Phân tích biểu hiện của tình mẫu tử, ý nghĩa đối với sự trưởng thành của con người, liên hệ thực tế.
    - Kết đoạn: Khái quát lại giá trị của tình mẫu tử và bài học nhận thức, hành động.
    ```
*   **Ghi chú:** `Học sinh có thể soạn bài trực tiếp trên tab 'Soạn bài' của phần mềm hoặc tải file Word bài làm lên.`

### 🇬🇧 Mẫu 3: Môn Tiếng Anh (GDPT 2018)
*   **Tiêu đề:** `Unit 9 Homework: Essay about Protecting the Environment`
*   **Môn học:** `Tiếng Anh`
*   **Nội dung:**
    ```text
    Write a short paragraph (120 - 150 words) about things you can do to protect the environment in your local area.

    Key vocabulary to use:
    - reduce, reuse, recycle
    - energy-saving light bulbs
    - public transport
    - single-use plastic
    ```
*   **Ghi chú:** `Soạn trực tiếp hoặc tải file word/PDF của em lên.`

## 📐 HƯỚNG DẪN & DỮ LIỆU MẪU: CÔNG CỤ GIẢI HỆ PHƯƠNG TRÌNH BẬC NHẤT 2 ẨN

Công cụ **Giải hệ phương trình bậc nhất 2 ẩn** hỗ trợ giáo viên giảng dạy minh họa trực quan và giúp học sinh tự học, đối chiếu lời giải chi tiết thông qua 3 phương pháp (Cramer, Thế, Cộng đại số) kết hợp đồ thị tọa độ.

### 1. Hướng dẫn trực quan các tính năng chính

```
┌────────────────────────────────────────────────────────────────────────┐
│                        [📐 Tiêu đề: Giải Hệ PT]                         │
├──────────────────────────────────────┬─────────────────────────────────┤
│ 📝 Nhập hệ số                        │ 📋 Kết quả                      │
│   • Live Preview: { ax + by = c      │   • Định thức Cramer: D, Dx, Dy │
│                    a'x + b'y = c'    │   • Nghiệm: x = ..., y = ...    │
│   • PT1: [ 2 ]x + [ 3 ]y = [ 8 ]     │   • Phân loại: Nghiệm duy nhất  │
│   • PT2: [ 1 ]x + [ -1]y = [ 1 ]     │   • Nút [📈 Xem đồ thị]          │
│   • [⚡ Ví dụ nhanh (Presets)]        │                                 │
├──────────────────────────────────────┴─────────────────────────────────┤
│ 📐 Giải chi tiết (Từng bước chi tiết theo Cramer & Thử lại)            │
├────────────────────────────────────────────────────────────────────────┤
│ 📖 Lý thuyết phương pháp giải & Phân loại hình học                      │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Live Preview Hệ phương trình:** Ngay khi nhập các hệ số vào ô TextBox, khung xem trước sẽ cập nhật công thức toán học chuẩn hóa với dấu ngoặc nhọn lớn `{`. Dấu phép tính cộng/trừ tự động điều chỉnh đẹp mắt (ví dụ: nhập $b_1 = -3$ hiển thị $2x - 3y = 8$).
*   **Ví dụ nhanh (Presets):** Cho phép giáo viên nhấp nhanh các nút ví dụ soạn sẵn (Hệ có nghiệm, vô nghiệm, vô số nghiệm, số âm, số lớn) để giảng bài lập tức mà không mất thời gian gõ số.
*   **Nút Xem đồ thị (Graph):** Mở cửa sổ trực quan hóa hai đường thẳng. Điểm giao nhau (nghiệm) được tô màu đỏ và đánh nhãn tọa độ rõ ràng. 
    > [!TIP]
    > **Chế độ ngoại tuyến (Offline):** Khi mất kết nối mạng, hệ thống tự động chuyển sang chế độ vẽ SVG cục bộ. Đồ thị vẫn hiển thị đầy đủ hai đường thẳng và tọa độ giao điểm giúp buổi học không bị gián đoạn.
*   **Nộp bài giải (Submit):** Khi mở công cụ bên trong phần mềm học sinh (`StudentShell`), một nút **Nộp Bài Giải 📤** sẽ xuất hiện ở góc trên bên phải. Học sinh có thể nhấn nút này để gửi trực tiếp hệ phương trình và kết quả nghiệm lên máy của giáo viên.

---

### 2. Các mẫu dữ liệu & Ví dụ thực tế giảng dạy

Dưới đây là các bộ dữ liệu mẫu điển hình thường gặp trong chương trình Đại số Lớp 9 được tích hợp sẵn dưới dạng Presets ví dụ nhanh:

#### Mẫu 2.1: Hệ phương trình có nghiệm duy nhất (Cơ bản SGK)
*   **Hệ số phương trình 1:** $a_1 = 2$, $b_1 = 3$, $c_1 = 8$
*   **Hệ số phương trình 2:** $a_2 = 1$, $b_2 = -1$, $c_2 = 1$
*   **Nghiệm kết quả:** $x = 2.2$, $y = 1.2$.
*   **Minh họa hình học:** Hai đường thẳng cắt nhau tại điểm duy nhất có tọa độ $(2.2; 1.2)$.

#### Mẫu 2.2: Hệ phương trình vô nghiệm (Đường thẳng song song)
*   **Hệ số phương trình 1:** $a_1 = 1$, $b_1 = 2$, $c_1 = 3$
*   **Hệ số phương trình 2:** $a_2 = 2$, $b_2 = 4$, $c_2 = 5$
*   **Nghiệm kết quả:** Hệ vô nghiệm (Định thức $D = 0$, nhưng $D_x = 2 \neq 0$).
*   **Minh họa hình học:** Hai đường thẳng song song tuyệt đối, không có giao điểm.

#### Mẫu 2.3: Hệ phương trình vô số nghiệm (Đường thẳng trùng nhau)
*   **Hệ số phương trình 1:** $a_1 = 1$, $b_1 = 2$, $c_1 = 3$
*   **Hệ số phương trình 2:** $a_2 = 2$, $b_2 = 4$, $c_2 = 6$
*   **Nghiệm kết quả:** Hệ có vô số nghiệm (Định thức $D = 0$, $D_x = 0$, $D_y = 0$).
*   **Minh họa hình học:** Hai đường thẳng trùng khít lên nhau, mọi điểm trên đường thẳng đều là nghiệm.

#### Mẫu 2.4: Hệ phương trình chứa hệ số âm và thập phân
*   **Hệ số phương trình 1:** $a_1 = -1.5$, $b_1 = 2.5$, $c_1 = 4.5$
*   **Hệ số phương trình 2:** $a_2 = 3.5$, $b_2 = -1.5$, $c_2 = 1.5$
*   **Nghiệm kết quả:** $x = 1.61538$, $y = 2.76923$.

---

## 📐 HƯỚNG DẪN & DỮ LIỆU MẪU: CÔNG CỤ GIẢI PHƯƠNG TRÌNH BẬC 2

Công cụ **Giải phương trình bậc 2** hỗ trợ giáo viên thực hiện các bài giảng trực quan, tương tác sinh động về đồ thị Parabol, biệt thức $\Delta$ và hệ thức Vi-ét, đồng thời giúp học sinh tự kiểm tra và đối chiếu các bước giải chi tiết tại lớp học cũng như khi làm bài tập về nhà.

### 1. Sơ đồ giao diện trực quan và các chức năng chính

```text
┌────────────────────────────────────────────────────────────────────────┐
│               📐 Tiêu đề: Giải Phương Trình Bậc 2                      │
├──────────────────────────────────────┬─────────────────────────────────┤
│ 📝 Nhập hệ số                        │ 📋 Kết quả                      │
│   • Ô nhập: [ a ]x² + [ b ]x + [ c ] │   • Biệt thức: Δ = b² - 4ac     │
│   • Hỗ trợ: Nhập phân số (ví dụ: 1/2)│   • Nghiệm: x1 = ..., x2 = ...  │
│   • Checkbox: [v] Hiện nghiệm phức   │   • Nhân tử: a(x - x1)(x - x2)  │
│   • [⚡ Ví dụ nhanh (Presets)]        │   • Đỉnh Parabol: I(x_V; y_V)   │
│                                      │   • Nút [📈 Xem đồ thị Parabol]  │
├──────────────────────────────────────┴─────────────────────────────────┤
│ 📝 Các bước giải chi tiết (Xác định hệ số -> Tính Δ -> Nghiệm -> Vi-ét)│
├────────────────────────────────────────────────────────────────────────┤
│ 📖 Lý thuyết - Phương trình bậc 2 (Hiển thị dạng thẻ đa cột)           │
├────────────────────────────────────────────────────────────────────────┤
│ 📜 Lịch sử giải (Cho phép Click-to-copy kết quả nhanh chóng)            │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Tính biệt thức $\Delta$ thời gian thực**: Ngay khi thay đổi hệ số trong các ô $a, b, c$, kết quả biệt thức $\Delta$ và nghiệm tương ứng sẽ được cập nhật lập tức mà không cần nhấn nút tính toán.
*   **Hộp kiểm nghiệm phức (THPT)**:
    *   **Tắt (Mặc định)**: Phục vụ học sinh THCS (Lớp 9). Khi phương trình vô nghiệm thực, giao diện chỉ hiển thị `"Vô nghiệm thực"` và rút gọn các bước giải (không hiển thị số phức $i$).
    *   **Bật**: Phục vụ học sinh THPT. Hiển thị 2 nghiệm phức liên hợp dạng $x_1 = u + vi, x_2 = u - vi$ cùng ký hiệu số ảo $i$ tương ứng.
*   **Xem đồ thị Parabol (Desmos / Offline SVG)**: Bấm nút **Xem đồ thị** để trực quan hóa hình học Parabol $y = ax^2 + bx + c$.
    *   Hệ thống tự động vẽ **Đỉnh Parabol (I)**, **Trục đối xứng (Dashed Line)**, **Các giao điểm với trục hoành (Nghệm)**, và **Giao điểm với trục tung (0; c)**.
    *   *Chế độ ngoại tuyến (Offline)*: Khi mất mạng, đồ thị vẫn được vẽ bằng hệ thống SVG tự động dự phòng, hỗ trợ tính toán và hiển thị đầy đủ hình dạng Parabol cùng tọa độ đỉnh, trục đối xứng.
*   **Phân tích nhân tử & Đỉnh Parabol chuẩn hóa**:
    *   Phép toán phân tích nhân tử được tự động rút gọn tối ưu dấu toán học: ví dụ hiển thị dạng `(x + 2)` thay vì `(x − (-2))`, giúp học sinh dễ chép trực tiếp vào vở làm bài.
    *   Tọa độ đỉnh Parabol tự động khử giá trị `-0.0` để hiển thị đúng ký hiệu số học tiêu chuẩn (ví dụ: `I(0; 3)` thay vì `I(-0; 3)`).

---

### 2. Các kịch bản giảng dạy & Bộ dữ liệu mẫu thực tế

Dưới đây là các bộ dữ liệu mẫu điển hình theo chương trình Toán phổ thông, hỗ trợ giáo viên thực hiện nhanh các kịch bản bài giảng:

| Loại Phương Trình | Hệ số nhập mẫu | Biệt thức $\Delta$ | Loại Nghiệm & Kết Quả | Ý nghĩa Sư phạm / Hình học |
| :--- | :--- | :--- | :--- | :--- |
| **Có 2 nghiệm thực phân biệt** | $a = 1$<br/>$b = -5$<br/>$c = 6$ | $\Delta = 1 > 0$ | $x_1 = 3$<br/>$x_2 = 2$<br/>Nhân tử: $(x - 3)(x - 2)$ | Parabol cắt trục hoành tại 2 điểm $(3; 0)$ và $(2; 0)$. Nhãn nhân tử tối giản đẹp mắt. |
| **Phương trình có nghiệm kép** | $a = 1$<br/>$b = -6$<br/>$c = 9$ | $\Delta = 0$ | $x_1 = x_2 = 3$<br/>Nhân tử: $(x - 3)^2$ | Parabol tiếp xúc với trục hoành tại đỉnh duy nhất có tọa độ $I(3; 0)$. |
| **Vô nghiệm thực (Cơ bản - THCS)** | $a = 1$<br/>$b = 2$<br/>$c = 5$ | $\Delta = -16 < 0$ | Nghiệm: `"Vô nghiệm thực"` | Học sinh lớp 9 chỉ cần biết phương trình không có nghiệm thực. Parabol nằm hoàn toàn phía trên trục hoành. |
| **Có nghiệm phức (Nâng cao - THPT)** | $a = 1$<br/>$b = 2$<br/>$c = 5$<br/>*(Bật Hiện nghiệm phức)* | $\Delta = -16 < 0$ | $x_1 = -1 + 2i$<br/>$x_2 = -1 - 2i$ | Phục vụ học sinh THPT làm quen số ảo. Phần ảo được lấy trị tuyệt đối chính xác ($2$ và $-2$, không bị lỗi dấu dạng $+ -2i$). |
| **Hệ số a âm (Parabol hướng xuống)** | $a = -1$<br/>$b = 4$<br/>$c = -3$ | $\Delta = 4 > 0$ | $x_1 = 1$<br/>$x_2 = 3$<br/>Nhân tử: $-(x - 1)(x - 3)$ | Đỉnh Parabol là điểm cực đại $I(2; 1)$. Bề lõm Parabol quay xuống dưới ($\cap$). |
| **Mẹo tính nhanh: $a + b + c = 0$** | $a = 2$<br/>$b = 3$<br/>$c = -5$ | $\Delta = 49 > 0$ | $x_1 = 1$<br/>$x_2 = -2.5$ | Học sinh áp dụng nhẩm nhanh nghiệm: $x_1 = 1$, $x_2 = c/a = -5/2$. |
| **Mẹo tính nhanh: $a - b + c = 0$** | $a = 1$<br/>$b = 5$<br/>$c = 4$ | $\Delta = 9 > 0$ | $x_1 = -1$<br/>$x_2 = -4$ | Học sinh áp dụng nhẩm nhanh nghiệm: $x_1 = -1$, $x_2 = -c/a = -4$. |
| **Hệ số phân số và thập phân** | $a = 0.5$ (hoặc `1/2`)<br/>$b = -3$<br/>$c = 2$ | $\Delta = 5 > 0$ | $x_1 = 5.23607$<br/>$x_2 = 0.76393$ | Chứng minh khả năng phân tích biểu thức toán học (phân số `1/2`) của bộ Parser `ParsingHelper`. |

---

## 🔍 HƯỚNG DẪN & DỮ LIỆU MẪU: CÔNG CỤ HỌC TẬP SỐ NGUYÊN TỐ

Công cụ **Số nguyên tố** được thiết kế bám sát chương trình Số học Lớp 6 (Bộ sách GDPT mới 2018), hỗ trợ giáo viên thực hành giảng dạy và học sinh tự học thông qua 5 phân hệ tương tác: Kiểm tra số nguyên tố, Phân tích thừa số nguyên tố, Sàng Eratosthenes trực quan, Tính toán ƯCLN & BCNN (Phân tích thừa số & Euclid), và Thư viện ứng dụng thực tế.

### 1. Sơ đồ tính năng trực quan của công cụ

```text
┌────────────────────────────────────────────────────────────────────────┐
│                        [🔍 Tiêu đề: Số Nguyên Tố]                      │
├────────────────────────────────────────────────────────────────────────┤
│  [🔍 Kiểm tra]  [🧮 Phân tích]  [📊 Sàng Eratosthenes]  [🔗 ƯCLN & BCNN]  [🌍 Ứng dụng] │
├────────────────────────────────────────────────────────────────────────┤
│ 📝 Nhập số đầu vào (Mini Touch-Pad)  │ 📋 Kết quả & Các bước giải chi tiết    │
│   • Ô nhập số Check: [ n ]           │   • Kết luận: Số NT / Hợp số           │
│   • Ô nhập phân tích: [ n ]          │   • Số NT kề trước & kề sau            │
│   • Nhập ƯCLN/BCNN: [ A ], [ B ]     │   • Sơ đồ phân tích thừa số           │
│   • [🔄 Reset dữ liệu]                │   • Thuật toán Euclid & Ước số         │
│   • Nút [📈 Xem đồ thị phân bố]       │   • 6 Thẻ ảnh Ứng dụng thực tế         │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Mini Numeric Touchpad:** Tất cả các ô nhập liệu đều được trang bị Bàn phím số cảm ứng nổi (`TouchNumPad`) phù hợp cho học sinh thao tác trực tiếp trên bảng thông minh hoặc máy tính bảng mà không cần bàn phím cứng.
*   **Hỗ trợ an toàn hiệu năng:** Hệ thống tự động phát hiện số lớn. Khi đếm số lượng số nguyên tố, nếu $n \le 100.000$, hệ thống đếm chính xác; nếu $n > 100.000$, hệ thống tự động chuyển sang ước lượng theo Định lý Số nguyên tố để tránh treo ứng dụng.
*   **Thiết kế Empty State:** Khi chưa nhập số hoặc sau khi nhấn nút **🔄 Reset**, ứng dụng hiển thị các tấm thiệp hướng dẫn sử dụng nhanh đẹp mắt, giúp người dùng dễ dàng hiểu nhiệm vụ cần thực hiện.

---

### 2. Các mẫu dữ liệu thực tế kiểm nghiệm giảng dạy

Dưới đây là các bộ số gợi ý giúp giáo viên dễ dàng thực hành minh họa tại lớp học:

#### Mẫu 2.1: Biên toán học & Số nguyên tố nhỏ nhất (Học sinh dễ nhầm lẫn)
*   **Nhập số cần kiểm tra:** $n = 1$
    *   *Kết quả:* `❌ 1 KHÔNG phải số nguyên tố`. Số NT tiếp theo là `2` (Không hiển thị số NT trước vì 1 là biên nhỏ nhất).
*   **Nhập số cần kiểm tra:** $n = 2$
    *   *Kết quả:* `✅ 2 là SỐ NGUYÊN TỐ`. Vị trí thứ `1` trong dãy số nguyên tố. Số NT tiếp theo là `3`.

#### Mẫu 2.2: Phân tích thừa số nguyên tố (Toán 6 SGK)
*   **Số cần phân tích:** $n = 72$
    *   *Công thức kết quả:* $72 = 2^3 \times 3^2$
    *   *Các bước giải trực quan:*
        *   $72 \div 2 = 36$
        *   $36 \div 2 = 18$
        *   $18 \div 2 = 9$
        *   $9 \div 3 = 3$
        *   $3 \div 3 = 1$
    *   *Số lượng ước:* Có $12$ ước số.
    *   *Danh sách ước số:* $1, 2, 3, 4, 6, 8, 9, 12, 18, 24, 36, 72$.

#### Mẫu 2.3: Tìm ƯCLN & BCNN bằng Phân tích thừa số (Số nhỏ \(\le 1.000.000\))
*   **Số A:** $12$; **Số B:** $18$
    *   *ƯCLN(12, 18):* $6$
    *   *BCNN(12, 18):* $36$
    *   *Phương pháp Phân tích thừa số nguyên tố hiển thị trên giao diện:*
        *   Phân tích: $12 = 2^2 \times 3$; $18 = 2 \times 3^2$
        *   Thừa số nguyên tố chung: $2, 3$
        *   ƯCLN = tích thừa số chung với số mũ nhỏ nhất: $2^1 \times 3^1 = 6$
        *   BCNN = tích thừa số chung & riêng với số mũ lớn nhất: $2^2 \times 3^2 = 36$
    *   *Thuật toán Euclid bổ trợ:*
        *   $18 = 12 \times 1 + 6$
        *   $12 = 6 \times 2 + 0$
        *   $\Rightarrow ƯCLN = 6$.

#### Mẫu 2.4: Trường hợp hai số nguyên tố cùng nhau
*   **Số A:** $8$; **Số B:** $9$
    *   *ƯCLN(8, 9):* $1$ (Hai số không có thừa số nguyên tố chung)
    *   *BCNN(8, 9):* $72$

---

### 3. Chi tiết 6 Ứng dụng thực tế của Số Nguyên Tố

Tab **🌍 Ứng dụng** tích hợp sẵn 6 tấm thiệp đa phương tiện (ảnh minh họa độ phân giải cao kèm văn bản mô tả cụ thể bằng tiếng Việt/tiếng Anh tương thích ngôn ngữ hệ thống):

1.  **🔐 Mật mã học & Bảo mật RSA (`app_prime_1`):** Là xương sống của ngành an ninh mạng. Khi truyền dữ liệu nhạy cảm, thuật toán mã hóa khóa công khai RSA chọn hai số nguyên tố cực lớn $p$ và $q$ để tạo ra khóa. Việc nhân $p \times q$ diễn ra trong phần mili-giây, nhưng việc giải mã ngược lại từ tích số để tìm ra $p$ và $q$ ban đầu sẽ tốn hàng ngàn năm tính toán của các máy tính mạnh nhất.
2.  **🦟 Chu kỳ sinh trưởng của Ve sầu Magicicada (`app_prime_2`):** Trong tự nhiên, loài ve sầu Magicicada ở Bắc Mỹ có chu kỳ ngủ đông dưới đất là 13 hoặc 17 năm. Việc chọn chu kỳ là các số nguyên tố giúp chúng tránh gặp phải chu kỳ sinh trưởng của các loài săn mồi (thường có chu kỳ vòng đời ngắn như 2, 3, 4, 6 năm), đảm bảo sự sinh tồn của giống loài.
3.  **⚙️ Thiết kế Bánh răng cơ khí (`app_prime_3`):** Trong kỹ thuật chế tạo máy, để hai bánh răng ăn khớp với nhau bền bỉ nhất, các kỹ sư thường chọn số răng của một trong hai bánh là số nguyên tố (ví dụ: bánh nhỏ 13 răng, bánh lớn 48 răng). Điều này đảm bảo rằng mỗi răng của bánh này sẽ tiếp xúc tuần tự lần lượt với tất cả các răng của bánh kia, giảm thiểu mài mòn cục bộ đồng bộ.
4.  **🌐 An toàn kết nối Internet HTTPS/SSL (`app_prime_4`):** Khi truy cập các trang web ngân hàng, thương mại điện tử bảo mật thông qua giao thức HTTPS, các chứng chỉ số SSL/TLS sử dụng số nguyên tố để tạo cặp khóa mã hóa phiên làm việc, đảm bảo thông tin cá nhân và thẻ tín dụng không bị tin tặc đánh cắp.
5.  **🎵 Nhịp điệu & Cấu trúc đa điệu âm nhạc (`app_prime_5`):** Trong âm nhạc đương đại, các nhạc sĩ thử nghiệm ghép các vòng lặp tiết tấu có nhịp phách độ dài là các số nguyên tố khác nhau (ví dụ: vòng lặp trống 5 phách đi kèm vòng lặp piano 7 phách). Do 5 và 7 là số nguyên tố, hai nhạc cụ này sẽ chỉ đồng pha trùng khớp lại sau mỗi $5 \times 7 = 35$ phách, tạo ra một cấu trúc âm thanh biến đổi phong phú và không bị lặp lại nhàm chán.
6.  **📡 Phân tán tín hiệu Mảng Ăng-ten (`app_prime_6`):** Trong kỹ thuật truyền thông không dây, khoảng cách lắp đặt giữa các phần tử phát sóng trong mảng ăng-ten được thiết kế dựa trên các khoảng cách số nguyên tố. Sự sắp xếp này giúp triệt tiêu các búp sóng phụ gây nhiễu và định hướng sóng tập trung tối đa vào thiết bị di động của người dùng.

---

Tài liệu được phân phối trực tiếp kèm theo gói cài đặt sản phẩm QA SmartClass.
Mọi thắc mắc xin liên hệ phòng Quản lý CNTT hoặc Ban quản trị Hệ thống.

---

## 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ SÁCH GIÁO KHOA ĐIỆN TỬ

Công cụ **Sách giáo khoa điện tử tương tác** hỗ trợ Giáo viên trình chiếu giảng dạy trên bảng thông minh và giúp Học sinh tự đọc, nghiên cứu bài học trực quan trong môi trường số hóa an toàn, không quảng cáo.

### 1. Hướng dẫn trực quan giao diện thư viện sách (Textbook Library)

```text
┌────────────────────────────────────────────────────────────────────────┐
│               📖 THƯ VIỆN SÁCH GIÁO KHOA TƯƠNG TÁC [📚 588 sách]        │
├────────────────────────────────────────────────────────────────────────┤
│ 📘 Lớp:  [Tất cả] [Lớp 1] [Lớp 2] ... [Lớp 12]                          │
├────────────────────────────────────────────────────────────────────────┤
│ 🔍 [Tìm kiếm...]   Bộ sách: [Tất cả] [Cánh Diều] [KNTT] [CTST]             │
│                     Loại:   [Tất cả] [SGK] [SBT] [VBT] [SGV] [CĐ] [OT] [TH]│
│                     Môn:    [Tất cả môn học v]                         │
├────────────────────────────────────────────────────────────────────────┤
│ 📜 Đang hiển thị: 12/588 sách  •  SGK: 6  •  SBT: 4  •  VBT: 2...      │
├────────────────────────────────────────────────────────────────────────┤
│ ┌───────────────────┐ ┌───────────────────┐ ┌───────────────────┐      │
│ │   [Ảnh bìa SGK]   │ │   [Ảnh bìa SBT]   │ │   [Ảnh bìa CĐ]    │      │
│ │ 📖 SGK     (Lớp 1)│ │ ✍️ SBT     (Lớp 1)│ │ ✨ Chuyên đề(Lớp 11)│      │
│ │  Tiếng Việt 1 - 1 │ │ VBT Toán 1 - Tập 1│ │ Chuyên đề Toán 11 │      │
│ │  [ 📕 Cánh Diều ] │ │ [ 📕 Cánh Diều ]  │ │ [ 📘 KNTT ]       │      │
│ │    [ Xem ngay ]   │ │    [ Xem ngay ]   │ │    [ Xem ngay ]   │      │
│ └───────────────────┘ └───────────────────┘ └───────────────────┘      │
└────────────────────────────────────────────────────────────────────────┘
```

#### 👩‍🏫 Các tính năng chính dành cho Giáo viên trên bảng tương tác:
*   **Thanh lọc Lớp (Grade Tabs):** Nhấp chọn nhanh lớp học từ 1 đến 12 để lọc nhanh sách tương ứng. Tab đang chọn sẽ chuyển sang màu xanh dương chủ đạo `#4A90E2` và chữ in đậm.
*   **Bộ lọc Bộ sách (Series Tabs):** Lọc theo 3 bộ sách GDPT 2018 hiện hành: `Cánh Diều`, `Kết nối tri thức` (KNTT), `Chân trời sáng tạo` (CTST).
*   **Bộ lọc Loại tài liệu (Type Tabs):** Hỗ trợ đầy đủ các loại tài liệu bổ trợ như:
    *   `📖 SGK`: Sách giáo khoa chính quy.
    *   `✍️ SBT`: Sách bài tập.
    *   `📝 VBT`: Vở bài tập.
    *   `👨‍🏫 SGV`: Sách giáo viên.
    *   `✨ Chuyên đề`: Chuyên đề học tập chuyên sâu.
    *   `🎓 Ôn thi`: Tài liệu ôn thi tốt nghiệp, thi HSG.
    *   `🛠️ Thực hành`: Sách/Vở thực hành thí nghiệm.
*   **ComboBox Môn học (Subject) & Sắp xếp (Sort):** Hỗ trợ tự động cập nhật danh sách môn học động và sắp xếp danh mục sách theo bảng chữ cái A-Z, Z-A hoặc theo bộ sách/môn học.
*   **Card bìa sách kích thước lớn (Card Size 210x260px):** Ảnh bìa to rõ nét, không bị vỡ hình. Trên mỗi card tích hợp sẵn Nhãn loại sách, Nhãn lớp màu cam nổi bật và Nút **Xem ngay** kích thước lớn dễ dàng nhấp chọn.

---

### 2. Hướng dẫn sử dụng Trình xem sách tương tác (Book Viewer)

Khi nhấn **Xem ngay**, trình xem sách chuyên dụng sẽ được kích hoạt toàn màn hình với thiết kế thanh điều hướng tối giản và an toàn:

```text
┌────────────────────────────────────────────────────────────────────────┐
│ 📚 [Tên cuốn sách đang mở]  • [Môn học] • [Lớp]     Trang: [◀] [═══O═══] [▶]  x / y  🔄 🏠 ✖ │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│                      [ Nội dung trang sách giáo khoa ]                 │
│                 (Hiển thị sạch - Đã tự động lọc bỏ quảng cáo)          │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

#### 🧑‍🎓 Các tính năng và phím tắt thông minh dành cho Học sinh & Giáo viên:
*   **Thanh trượt Slider (Width 400px):** Được thiết kế kích thước cực lớn để dễ dàng kéo trượt bằng ngón tay trên màn hình cảm ứng để lật trang nhanh.
*   **Bảo mật học đường an toàn:**
    *   *Chặn link ngoài:* Hệ thống chỉ cho phép tải nội dung từ tên miền `hoc10.vn`. Nếu học sinh click vào link dẫn ra ngoài (ví dụ facebook, youtube, game trực tuyến), trình duyệt sẽ lập tức chặn lại và hiển thị cảnh báo bảo mật.
    *   *Chặn popup:* Mọi hành vi tự động mở cửa sổ mới hoặc tab quảng cáo đều bị vô hiệu hóa triệt để.
*   **Hệ thống phím tắt nhanh trên bàn phím/bút trình chiếu:**
    *   `←` (Mũi tên trái) / `Page Up`: Quay lại trang trước.
    *   `→` (Mũi tên phải) / `Page Down`: Đi tới trang tiếp theo.
    *   `Home`: Về trang bìa (trang 1).
    *   `End`: Đi đến trang cuối cùng của sách.
    *   `F5`: Làm mới (tải lại) trang nếu gặp sự cố mạng.
    *   `Esc` / `✖`: Đóng nhanh trình đọc sách để quay lại thư viện.

---

### 3. Ví dụ mẫu dữ liệu tích hợp sẵn trong Cơ sở dữ liệu

Dưới đây là một số ví dụ dữ liệu sách điển hình trong file cơ sở dữ liệu `books.json` giúp giáo viên và học sinh thử nghiệm tính năng lật trang thông minh:

#### 📖 Ví dụ 1: Sách Giáo Khoa Tiếng Việt 1 - Tập 1 (Cánh Diều)
*   **Mã số sách:** `cd-tieng-viet-1-1`
*   **Tên sách:** `Tiếng Việt 1 - Tập 1`
*   **Nhà xuất bản:** `NXB Đại học Sư phạm` (Đã chuẩn hóa chính xác thông tin sư phạm).
*   **Trang bắt đầu (StartPage):** `1`
*   **Tổng số trang:** `100`
*   **Đường dẫn gốc:** `https://www.hoc10.vn/doc-sach/tieng-viet-1-1/1/1`
*   **Kịch bản kiểm tra:** Khi mở sách, thanh trượt hiển thị trang `1 / 100`. Nhấn nút `▶` hoặc phím mũi tên phải, trang sách chuyển tiếp sang trang 2, URL WebView tải chính xác địa chỉ kết thúc bằng `/2/`.

#### ✍️ Ví dụ 2: Vở bài tập Toán 1 - Tập 1 (Cánh Diều) - Lật trang tương đối
*   **Mã số sách:** `cd-vbt-toan-1-tap-1`
*   **Tên sách:** `VBT Toán 1 - Tập 1`
*   **Nhà xuất bản:** `NXB Đại học Sư phạm`
*   **Trang bắt đầu (StartPage):** `38` (Quyển sách này nằm từ trang 38 của tập tài liệu trực tuyến).
*   **Tổng số trang:** `60`
*   **Đường dẫn gốc:** `https://www.hoc10.vn/doc-sach/vo-bai-tap-toan-1-tap-1/5/38`
*   **Kịch bản kiểm tra:** 
    *   Khi vừa mở sách, thanh trượt hiển thị chính xác trang `1 / 60` (mặc dù URL tải thực tế kết thúc bằng `/38`).
    *   Nhấp lật sang trang thứ `5`, thanh trượt hiển thị đúng `5 / 60`. Hệ thống tự động tính toán URL gửi tới WebView là:
        $$\text{Trang đích} = \text{StartPage} + \text{Trang yêu cầu} - 1 = 38 + 5 - 1 = 42$$
        WebView tải đúng địa chỉ kết thúc bằng `/42/`, đảm bảo học sinh xem đúng trang số 5 của Vở bài tập mà không bị lỗi trang trắng 404.

---

## 🚂 IV. HƯỚNG DẪN & DỮ LIỆU MẪU: CÔNG CỤ HỌC TẬP CHUYẾN TÀU NGỮ VĂN

> [!NOTE]
> Để xem tài liệu hướng dẫn đầy đủ nhất kèm theo sơ đồ vận hành, kịch bản sư phạm cho giáo viên, học sinh và các bảng dữ liệu mẫu chi tiết của các khối lớp, vui lòng tham khảo tệp tin: [HUONG_DAN_SU_DUNG_CHUYEN_TAU.md](file:///d:/JOB/QA%20SmartClass%20-062026/HUONG_DAN_SU_DUNG_CHUYEN_TAU.md).

Công cụ **Chuyến Tàu Ngữ Văn** hỗ trợ việc dạy và học các tác phẩm văn học thông qua sơ đồ ga hành trình trực quan từ lớp 6 đến lớp 12, kết hợp âm thanh đọc diễn cảm, highlight từ khóa bài học, theo dõi chuỗi câu đúng (Streak), hiệu ứng chúc mừng sinh động, bộ lọc nhật ký an toàn và sao lưu dữ liệu tự động.

### 1. Hướng dẫn giao diện trực quan (Visual Interface Layout)

```text
┌────────────────────────────────────────────────────────────────────────┐
│ 🚂 CHUYẾN TÀU NGỮ VĂN • Lớp 6 - Lớp 12                 [← Quay lại]    │
├────────────────────────────────────────────────────────────────────────┤
│ 🚄 Ga Hành Trình Lớp: [ Lớp 6 ] [ Lớp 7 ] [ Lớp 8 ] ... [ Lớp 12 ]     │
├────────────────────────────────────────────────────────────────────────┤
│ 📖 Chi tiết bài học: Qua Đèo Ngang (Bà Huyện Thanh Quan)               │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │ 📌 Tác giả: Bà Huyện Thanh Quan (Đầu thế kỷ XIX)                   │ │
│ │   [ Giáo viên/học sinh bôi đen văn bản để highlight màu vàng sáng ] │ │
│ │ 📖 Bài thơ:                                                        │ │
│ │   "Bước tới Đèo Ngang, bóng xế tà,..."                             │ │
│ └────────────────────────────────────────────────────────────────────┘ │
├────────────────────────────────────────────────────────────────────────┤
│ 📝 Bài tập: Tiến trình 2 / 3 bài                      🔥 Chuỗi đúng: 3 │
│ ┌────────────────────────────────────────────────────────────────────┐ │
│ │ Câu 2: Câu thơ 'Một mảnh tình riêng, ta với ta' diễn tả điều gì?    │ │
│ │   (A) Tình cảm bạn bè thân thiết tri kỷ của tác giả                │ │
│ │   (B) Nỗi cô đơn tuyệt đối của tác giả giữa trời non nước           │ │
│ │   (C) Sự vô ngã trong triết lý Phật giáo của nhà thơ               │ │
│ └────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Highlight văn bản đồng bộ:** Tất cả nội dung văn bản lý thuyết (Tác giả, Định nghĩa, Tác phẩm, Thơ), câu hỏi trắc nghiệm, và phần giải thích đáp án đều được thiết lập ở chế độ bôi đen tương tác. Giáo viên/học sinh có thể click chuột trái kéo quét để tô vàng các từ khóa quan trọng trong tiết học.
*   **Nhãn ngọn lửa Streak:** Hiển thị trực tiếp chuỗi làm bài trắc nghiệm đúng liên tiếp `🔥 Chuỗi đúng: X` trên góc thẻ bài tập để khích lệ tinh thần đua điểm của học sinh.

---

### 2. Hướng dẫn dành cho Giáo viên (Teacher Guide)

*   **Tương tác giảng bài trên bảng thông minh:**
    *   Sử dụng tab **Ga hành trình** để lọc nhanh các tác phẩm theo khối lớp (6-12).
    *   Nhấp chọn một tác phẩm (ví dụ: *Tây Tiến*), click biểu tượng `🔊` để phát giọng đọc diễn cảm thơ kháng chiến to, rõ ràng.
    *   Dùng tay hoặc bút trình chiếu bôi đen các từ ngữ nghệ thuật đặc sắc (ví dụ: *"dốc lên khúc khuỷu dốc thăm thẳm"*) để hệ thống tự động bôi màu vàng `#FFF59D` nổi bật.
*   **Quản trị hệ thống & Bảo mật:**
    *   **Tra cứu logs an ninh:** Nhập khoảng thời gian trên hai ô chọn ngày `DatePicker` và nhấn **"Lọc Nhật ký"** để tra cứu lịch sử hoạt động bảo mật.
    *   **Sao lưu dữ liệu:** Bấm nút **"Đồng bộ & Sao lưu"** ở tab Thống kê. Hệ thống tự động checkpoint SQLite WAL và nhân bản file database ra thư mục người dùng an toàn `AppData\Local\QASmartClass\backup` để tránh lỗi ghi file trên hệ điều hành Windows bị khóa quyền Admin.
    *   **Retention Policy:** Hệ thống tự động dọn dẹp và chỉ giữ lại 5 tệp tin sao lưu gần nhất để tối ưu hóa bộ nhớ ổ đĩa.

---

### 3. Hướng dẫn dành cho Học sinh (Student Guide)

*   **Luyện tập trắc nghiệm cảm thụ:**
    *   Nhấp chuyển sang tab **📝 Bài tập** để bắt đầu luyện tập trắc nghiệm sau khi đã đọc hiểu thẻ khái niệm tác phẩm.
    *   Cố gắng trả lời đúng liên tiếp để tích lũy **Streak (Chuỗi đúng)**. Khi đạt chuỗi đúng $\ge 3$, ngọn lửa `🔥 Chuỗi đúng` sẽ bùng sáng kèm âm thanh kích lệ.
    *   Khi hoàn thành câu hỏi cuối cùng của chặng, hãy quan sát hiệu ứng pháo hoa chúc mừng (Particle Burst) rực rỡ màu sắc bay tỏa khắp màn hình trong 1.2 giây để ghi nhận kết quả.
    *   Nhấp chọn nút **"Lời giải"** để bôi đen, tham khảo các phân tích chi tiết của giáo viên.

---

### 4. Các ví dụ mẫu dữ liệu tích hợp sẵn (Sample Curriculum Data)

Giáo viên và học sinh có thể trải nghiệm đầy đủ các tính năng thông qua 3 tác phẩm mẫu đại diện cho các khối lớp học:

#### 🌊 Ví dụ 1: Bài học "Thánh Gióng" (Lớp 6 - Thể loại Truyền thuyết)
*   **Mã chương:** `g06_ch01`
*   **Nội dung khái niệm tiêu biểu:**
    *   *Chi tiết kỳ lạ của Gióng:* Đứa trẻ lên ba không biết nói biết cười bỗng dưng xin đi đánh giặc.
    *   *Gióng vươn vai chiến đấu:* Tượng trưng cho sức mạnh quật khởi phi thường của dân tộc.
*   **Xử lý hình ảnh:** Tác phẩm này sử dụng ảnh minh họa `thanh_giong.png`. Nếu file ảnh bị thiếu trong thư mục cài đặt, giao diện sẽ hiển thị một khung Placeholder màu tím nhạt có book emoji `📚` kèm dòng chữ `"Đang cập nhật hình ảnh tư liệu"`, đảm bảo tính mỹ thuật sư phạm.

#### ⛰️ Ví dụ 2: Bài học "Qua Đèo Ngang" (Lớp 7 - Thơ trữ tình trung đại)
*   **Mã chương:** `g07_ch01`
*   **Câu hỏi trắc nghiệm ôn tập:**
    *   *Câu hỏi:* Biện pháp đảo ngữ trong *"Lom khom dưới núi tiều vài chú / Lác đác bên sông chợ mấy nhà"* có tác dụng gì?
    *   *Đáp án đúng:* Đưa từ láy lên đầu câu để nhấn mạnh sự vắng vẻ, nhỏ bé của con người trước thiên nhiên.
    *   *Lời giải chi tiết:* Đảo ngữ làm nổi bật cảm giác cô đơn của nữ thi sĩ Bà Huyện Thanh Quan giữa Đèo Ngang hoang vắng buổi chiều tà.

#### 🪖 Ví dụ 3: Bài học "Tây Tiến" - Quang Dũng (Lớp 12 - Thơ kháng chiến)
*   **Mã chương:** `g12_ch01`
*   **Giọng đọc diễn cảm tích hợp:** Giáo viên có thể nhấp chọn phát audio ngâm thơ mẫu trực tiếp từ tệp tin âm thanh online có chất lượng phòng thu rõ nét.

---

## 🚦 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ RADAR TIẾNG ỒN (NOISE MONITOR)

Công cụ **Radar Tiếng Ồn (Noise Monitor)** hỗ trợ Giáo viên quản lý trật tự lớp học tự động bằng âm thanh và hình ảnh trực quan sinh động. Công cụ áp dụng các cơ chế game hóa (Gamification) để học sinh tự giác giữ im lặng, tạo không khí học tập kỷ luật và hào hứng.

### 1. Sơ đồ giao diện trực quan và các vùng chức năng chính

```text
┌────────────────────────────────────────────────────────────────────────┐
│ 🚦 Radar Tiếng Ồn (Noise Monitor)                                      │
│   Tự động đo mức độ ồn và nhắc nhở lớp học trật tự                      │
├────────────────────────────────────────────────────────────────────────┤
│                                            ┌─────────────────────────┐ │
│                                            │ Chuỗi trật tự: 25 🔥    │ │
│                                            │ ⭐⭐⭐⭐⭐               │ │
│                                            └─────────────────────────┘ │
│                          (((  Wave 3  )))                              │
│                        ((   Wave 2   ))                                │
│                       (     Wave 1     )                               │
│                            ┌───────┐                                   │
│                            │  😀   │ <--- Khuôn mặt cảm xúc             │
│                            └───────┘                                   │
│                                                                        │
│                              45 dB                                     │
│                     Lớp học đang rất trật tự                           │
├────────────────────────────────────────────────────────────────────────┤
│ Ngưỡng Báo Động: [75 dB]                                               │
│ [====================O=============] (Trượt để thay đổi ngưỡng)         │
│                                                                        │
│ [🔊 Tự động Suỵt: BẬT]  [🎯 Định Chuẩn Lại]  [⏸ Tạm Dừng Radar]          │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Vòng sóng lan tỏa Radar (Wave Rings):** 3 vòng sóng đồng tâm lan tỏa quanh khuôn mặt cảm xúc. Kích thước và độ mờ (Opacity) của sóng tự động biến đổi động theo cường độ âm thanh (Decibel) thực tế của lớp học.
*   **Khuôn mặt cảm xúc biểu cảm:** Tự động thay đổi trạng thái theo mức độ ồn:
    *   `😀` màu Xanh lá (`#4CAF50`): Lớp học yên tĩnh, trật tự dưới ngưỡng.
    *   `😐` màu Cam (`#FF9800`): Lớp học bắt đầu ồn, chớm chạm ngưỡng cảnh báo.
    *   `😡` màu Đỏ (`#F44336`): Lớp học quá ồn, vượt ngưỡng cho phép.
*   **Bảng thi đua trật tự (Gamification Panel):** Nằm ở góc trên bên phải, ghi nhận nỗ lực của tập thể lớp thông qua điểm Chuỗi trật tự 🔥 và Ngôi sao vàng ⭐.

---

### 2. Hướng dẫn sử dụng chi tiết dành cho Giáo viên

#### 🎯 Quy trình Định chuẩn phòng học (Calibration)
Mỗi phòng học có một độ ồn nền khác nhau (do tiếng quạt trần, tiếng xe cộ ngoài đường, tiếng gió). Khi khởi chạy công cụ hoặc khi điều kiện phòng học thay đổi (ví dụ mở cửa sổ, bật thêm điều hòa), giáo viên nên thực hiện định chuẩn lại:
1.  Nhấp nút **🎯 Định Chuẩn Lại** (hoặc nhấn tổ hợp phím `Ctrl + R`).
2.  Yêu cầu cả lớp giữ im lặng tuyệt đối trong vòng **3 giây** khi màn hình hiển thị thanh tiến trình đo đạc.
3.  Hệ thống tự động ghi nhận mức ồn nền trung bình và thiết lập ngưỡng báo động tối ưu ở mức: $$\text{Ngưỡng tối ưu} = \text{Tiếng ồn nền} + 20\text{ dB}$$ (Giới hạn tự động trong khoảng 50 dB đến 90 dB).

#### 🔊 Chế độ Tự động Suỵt (Auto-Shush)
*   **Khi Bật (Xanh lá):** Nếu tiếng ồn lớp học vượt ngưỡng báo động, phần mềm sẽ tự động tổng hợp và phát tiếng *"Suỵt (Shhh!)"* êm dịu thông qua thuật toán NAudio (Bandpass filter và đường bao ADSR mịn màng), giúp học sinh tự điều tiết lại âm lượng mà không cần giáo viên phải lên tiếng nhắc nhở.
*   **Khi Tắt (Đỏ nhạt):** Phần mềm chỉ thay đổi khuôn mặt sang `😡` và hiện trạng thái cảnh báo màu đỏ, hoàn toàn giữ im lặng để tránh làm phiền đến bài giảng của giáo viên.

#### ⌨️ Các phím tắt từ xa (Window Hotkeys)
Để rảnh tay di chuyển quanh lớp học và sử dụng bút trình chiếu từ xa, giáo viên có thể thao tác nhanh qua các phím tắt bàn phím:
*   `Space` (Phím cách): Tạm dừng / Tiếp tục chạy Radar.
*   `Ctrl + R`: Kích hoạt định chuẩn lại phòng học ngay lập tức.
*   `Ctrl + S`: Bật hoặc Tắt nhanh chế độ tự động phát tiếng nhắc nhở "Suỵt".

---

### 3. Quy tắc Game hóa thi đua dành cho Học sinh

Để bài học giữ trật tự trở nên thú vị, học sinh sẽ cùng nhau thi đua theo nhóm tập thể lớp học:

> [!TIP]
> **Quy tắc tích lũy điểm Chuỗi và nhận Sao vàng:**
> -  **Duy trì trạng thái Xanh lá (Quiet):** Cứ mỗi **10 giây** liên tục phòng học giữ trật tự dưới ngưỡng, Chuỗi trật tự sẽ tăng lên `+1 🔥`.
> -  **Chuyển sang trạng thái Vàng (Warning):** Điểm Chuỗi bị **đóng băng (freeze)**, không tăng thêm nhưng cũng không bị mất đi. Đây là thời gian vàng để cả lớp cùng nhắc nhau nói khẽ lại.
> -  **Chạm trạng thái Đỏ (Too Loud):** Điểm chuỗi lập tức bị **reset về `0 🔥`** và toàn bộ số sao vàng tích lũy sẽ bị thu hồi.
> -  **Mở khóa Ngôi sao:** Cứ đạt được 5 chuỗi 🔥 sẽ quy đổi được `1 ⭐` (Tối đa 5 Ngôi sao ứng với chuỗi 25 🔥).

#### 🎉 Hiệu ứng Vinh danh Lớp học xuất sắc (Celebration Effect)
Khi lớp học duy trì sự tập trung xuất sắc đạt chuỗi `25 🔥` (5 ⭐):
1.  Bảng vinh danh Acrylic xanh dịu mát **🎉 LỚP HỌC XUẤT SẮC! 🎉** sẽ tự động hiển thị từ trung tâm màn hình với hoạt ảnh phóng to mượt mà (`BackEase`).
2.  Hệ thống loa lớp học sẽ phát ra âm thanh chuông chúc mừng 3 nốt nhạc thăng hoa liên tiếp **Đô - Mi - Sol (C5 - E5 - G5)** được tổng hợp kỹ thuật số êm dịu, mang lại cảm giác thành tựu lớn cho học sinh. Bảng vinh danh sẽ tự động ẩn đi sau 4 giây để lớp tiếp tục học tập.

---

### 📊 Dữ liệu tham khảo mức độ tiếng ồn (Sample Noise Levels)

Dưới đây là các mức độ ồn mẫu được khuyến nghị thiết lập cho từng hoạt động học tập thực tế tại lớp:

| Hoạt động lớp học | Mức độ ồn thực tế | Trạng thái hiển thị | Ngưỡng đề xuất | Cách xử lý sư phạm của hệ thống |
| :--- | :--- | :--- | :---: | :--- |
| **Lớp tự học, kiểm tra** | $35 - 45\text{ dB}$ | `😀` Green | $60\text{ dB}$ | Giữ im lặng tuyệt đối. Nếu 1 học sinh nói to vượt quá 60 dB, hệ thống lập tức phát tiếng "Suỵt" nhắc nhở nhỏ nhẹ. |
| **Giáo viên giảng bài** | $45 - 55\text{ dB}$ | `😀` Green | $70\text{ dB}$ | Tiếng giáo viên giảng bài ở mức vừa phải. Điểm chuỗi 🔥 liên tục tăng nếu học sinh trật tự lắng nghe. |
| **Thảo luận nhóm nhỏ** | $55 - 65\text{ dB}$ | `😐` Yellow | $80\text{ dB}$ | Cho phép học sinh trao đổi bài vừa phải. Điểm chuỗi được đóng băng, giữ nguyên động lực thi đua. |
| **Hoạt động tự do, ra chơi** | $70 - 85\text{ dB}$ | `😡` Red | $90\text{ dB}$ | Mức độ ồn ào tự nhiên. Giáo viên nên nhấn phím `Space` để tạm dừng Radar tránh tiếng hú loa hoặc reset chuỗi không mong muốn. |

> [!WARNING]
> **Xử lý lỗi chặn quyền thiết bị (OS Privacy Setting):**
> - Nếu hệ thống Windows chặn quyền truy cập Microphone của ứng dụng (lỗi bảo mật `0x80070005`), phần mềm sẽ hiển thị hộp thoại cảnh báo tiếng Việt thân thiện: *"Lỗi quyền truy cập Microphone! Vui lòng cho phép ứng dụng truy cập Microphone trong cài đặt Windows (Cài đặt > Quyền riêng tư > Micrô) và thử lại."*
> - Giáo viên chỉ cần làm theo hướng dẫn mở quyền trong Windows là phần mềm tự động kết nối lại phần cứng mượt mà.

---

## 🧩 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ HỌC TẬP SUDOKU LOGIC

Công cụ học tập **Sudoku Logic** hỗ trợ phát triển tư duy định lượng, rèn luyện não bộ, nâng cao khả năng phân tích chuỗi và loại trừ logic cho học sinh mọi lứa tuổi từ Tiểu học đến Trung học phổ thông thông qua các bảng lưới số linh hoạt.

### 1. Sơ đồ giao diện trực quan và các vùng chức năng chính

```text
┌────────────────────────────────────────────────────────────────────────┐
│ 🧩 Sudoku – Tư Duy Logic                            [🔄 Ván mới] [💡 Gợi ý] [✅ Kiểm tra] │
├──────────────────────────────────────┬─────────────────────────────────┤
│ ┌───┬───┬───┬───┬───┬───┬───┬───┬───┐    │ 📐 Kích thước bảng              │
│ │ 5 │ 3 │   │   │ 7 │   │   │   │   │    │  ( ) 4 x 4 (Dễ)                 │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    │  ( ) 6 x 6 (Trung bình)         │
│ │ 6 │   │   │ 1 │ 9 │ 5 │   │   │   │    │  (•) 9 x 9 (Khó)                │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    ├─────────────────────────────────┤
│ │   │ 9 │ 8 │   │   │   │   │ 6 │   │    │ 🎮 Trạng thái                   │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    │  • Đang chơi...                 │
│ │ 8 │   │   │   │ 6 │   │   │   │ 3 │    │  • 📊 12/81 ô đã điền (14%)     │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    ├─────────────────────────────────┤
│ │ 4 │   │   │ 8 │   │ 3 │   │   │ 1 │    │ 🔢 Nhập nhanh                   │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    │  [1] [2] [3] [4] [5]            │
│ │ 7 │   │   │   │ 2 │   │   │   │ 6 │    │  [6] [7] [8] [9] [✕]            │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    ├─────────────────────────────────┤
│ │   │ 6 │   │   │   │   │ 2 │ 8 │   │    │ 💡 Hướng dẫn                    │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    │  • Mỗi hàng: số 1-9 không lặp   │
│ │   │   │   │ 4 │ 1 │ 9 │   │   │ 5 │    │  • Mỗi cột: số 1-9 không lặp   │
│ ├───┼───┼───┼───┼───┼───┼───┼───┼───┤    │  • Ô vuông nhỏ 3x3: số 1-9      │
│ │   │   │   │   │ 8 │   │   │ 7 │ 9 │    │  • Click ô trống -> gõ số       │
│ └───┴───┴───┴───┴───┴───┴───┴───┴───┘    │                                 │
└──────────────────────────────────────┴─────────────────────────────────┘
```

*   **Lưới số Sudoku tương tác:** Các ô đề bài (`isGiven`) có chữ đậm màu đen, nền xám nhạt và được khóa bảo vệ. Các ô cần điền có chữ màu xanh dương sắc nét trên nền trắng nổi bật.
*   **Bảng điều khiển trực quan (Bên phải):** Chứa các tùy chọn cấp độ bảng số, báo cáo tiến độ thời gian thực, bàn phím nhập nhanh cho bảng tương tác cảm ứng và bảng hướng dẫn tự động cập nhật động theo kích thước lưới thực tế.
*   **Thanh công cụ trên đầu:**
    *   **🔄 Ván mới:** Tạo ngay một bảng Sudoku ngẫu nhiên mới với nghiệm duy nhất.
    *   **💡 Gợi ý:** Tự động giải và điền chính xác 1 ô trống (ưu tiên ô đang được chọn hoặc ô trống đầu tiên từ trên xuống). Ô gợi ý sẽ được tô xanh lá cây nhạt và tự động chuyển sang chế độ chỉ đọc để giúp học sinh đi đúng hướng.
    *   **✅ Kiểm tra:** So khớp bảng chơi thời gian thực, tô màu đỏ nhạt các ô điền sai luật hoặc trùng lặp, và báo xanh hoàn thành khi giải đúng 100% bảng.

---

### 2. Hướng dẫn dành cho Giáo viên (Teacher Guide)

*   **Lựa chọn cấp độ giảng dạy phù hợp theo Khối lớp:**
    *   **Cấp độ Dễ ($4 \times 4$):** Dành cho học sinh Tiểu học (Khối 1 - 3). Lưới gồm 4 ô vuông nhỏ kích thước $2 \times 2$. Giáo viên sử dụng để làm quen với các khái niệm hàng, cột và phân biệt số cơ bản từ 1 đến 4.
    *   **Cấp độ Vừa ($6 \times 6$):** Dành cho học sinh cuối Tiểu học hoặc đầu THCS (Khối 4 - 6). Lưới gồm 6 ô vuông nhỏ kích thước $3 \times 2$. Học sinh bắt đầu rèn luyện tư duy loại trừ trung bình.
    *   **Cấp độ Khó ($9 \times 9$):** Dành cho học sinh THCS và THPT. Bảng Sudoku tiêu chuẩn với các ô vuông nhỏ kích thước $3 \times 3$, yêu cầu học sinh tập trung phân tích nhiều lớp logic để giải.
*   **Tương tác sư phạm & Hướng dẫn suy luận:**
    *   Giáo viên khuyến khích học sinh sử dụng phím **Gợi ý** khi các em gặp bế tắc, giúp học sinh tìm ra một mắt xích quan trọng để tiếp tục tự giải thay vì giáo viên phải giải hộ.
    *   Nhờ cơ chế **cooldown chống thử sai 1.5 giây**, giáo viên hoàn toàn yên tâm học sinh không thể bấm chuột liên tục để dò số. Hãy hướng dẫn các em quan sát bảng số, giải thích lý do vì sao điền số đó trước khi cho các em nhấn nút **Kiểm tra**.

---

### 3. Hướng dẫn dành cho Học sinh (Student Guide)

*   **Thao tác bàn phím siêu nhanh (Premium UX):**
    *   **Di chuyển tiêu điểm bằng phím mũi tên:** Sử dụng 4 phím mũi tên `↑`, `↓`, `←`, `→` trên bàn phím để di chuyển vùng chọn màu xanh lam nhạt qua lại trên lưới cực kỳ nhanh. Khi chạm biên, tiêu điểm sẽ tự động cuộn vòng sang biên đối diện (wrap-around).
    *   **Gõ số đè trực tiếp:** Click hoặc di chuyển con trỏ tới ô số, gõ trực tiếp chữ số muốn điền. Ô số sẽ tự động bôi đen toàn bộ văn bản để thay số mới lập tức mà không yêu cầu bạn phải nhấn phím `Backspace` hay `Delete` để xóa số cũ trước.
    *   **Sử dụng Numpad nhập nhanh:** Thích hợp khi học trên bảng thông minh hoặc máy tính bảng. Click chọn ô trống và bấm trực tiếp các nút số `[1] - [9]` trên bảng điều khiển bên phải. Bấm nút `[✕]` màu đỏ để xóa nhanh số trong ô.
*   **Trải nghiệm phần thưởng chiến thắng:**
    *   Khi bạn hoàn thành chính xác toàn lưới, toàn bộ bảng chơi sẽ đổi nền xanh lá nhạt dịu mát, âm thanh chúc mừng vang lên và pháo hoa Confetti rực rỡ sắc màu sẽ bay tỏa khắp màn hình trong 3 giây để vinh danh chiến thắng của bạn!

---

### 4. Bảng Presets dữ liệu mẫu & Chỉ số cấp độ học tập

Dưới đây là thông số tham khảo về các cấp độ Sudoku tích hợp sẵn trong hệ thống phục vụ thiết kế bài học:

| Cấp độ bảng số | Kích thước ô vuông nhỏ | Số lượng ô trống cần điền | Thời gian giải khuyến nghị | Mục tiêu phát triển tư duy |
| :--- | :--- | :---: | :---: | :--- |
| **4 × 4 (Dễ)** | $2 \times 2$ | 6 ô trống | $30 - 60$ giây | Làm quen khái niệm ma trận số, hàng, cột. Rèn phản xạ sắp xếp. |
| **6 × 6 (Vừa)** | $3 \times 2$ | 14 ô trống | $2 - 5$ phút | Phát triển tư duy loại trừ theo khối chữ nhật. Phân tích chuỗi số từ 1 đến 6. |
| **9 × 9 (Khó)** | $3 \times 3$ | 45 ô trống | $10 - 25$ phút | Rèn luyện khả năng tập trung cao độ, kỹ năng loại trừ chéo hàng và cột, tư duy thùy trán. |

#### 💡 Ví dụ về quy trình giải một ô Sudoku mẫu (Kỹ thuật Quét dòng/cột):
*   **Đề bài:** Lưới $9 \times 9$, ô vuông nhỏ trên cùng bên trái (khối 3x3 thứ nhất) đang thiếu số `5`.
*   **Dữ liệu:**
    *   Hàng 1 đã có số `5` ở cột 8.
    *   Hàng 2 đã có số `5` ở cột 5.
    *   Cột 1 đã có số `5` ở hàng 9.
*   **Quy trình suy luận loại trừ:**
    *   Vì hàng 1 có số `5`, số `5` của khối thứ nhất không thể nằm ở hàng 1.
    *   Vì hàng 2 có số `5`, số `5` của khối thứ nhất không thể nằm ở hàng 2.
    *   Nếu trong khối thứ nhất, hàng 3 chỉ còn duy nhất ô giao với cột 2 hoặc cột 3 trống, học sinh lập tức xác định và điền được số `5` vào ô đó một cách chính xác tuyệt đối.

### 5. Các ứng dụng thực tế sinh động của Sudoku & Ma trận Latin

Để học tập không chỉ dừng lại ở lý thuyết trò chơi, phần mềm tích hợp thẻ **🌍 Ứng dụng thực tế** hiển thị 6 ứng dụng khoa học quan trọng của Sudoku và ma trận Latin trong cuộc sống thực tiễn:

1. **🔐 Mật mã học & Bảo mật thông tin:** Sử dụng các cấu trúc ma trận Latin và Sudoku để thiết kế các thuật toán mã hóa mật mã khóa công khai nâng cao, tạo chuỗi số giả ngẫu nhiên và phân phối khóa dữ liệu bảo mật.
2. **📅 Lập lịch & Phân bổ tài nguyên:** Giải bài toán sắp xếp thời khóa biểu cho giáo viên, phân chia phòng thi học kỳ và xếp lịch bay của phi công hàng không, loại bỏ các xung đột về tài nguyên và thời gian chéo.
3. **🧬 Giải trình tự DNA & Tin sinh học:** Tối ưu thiết kế các mẫu thử nghiệm sinh học, gộp mẫu xét nghiệm DNA diện rộng dựa trên các ràng buộc ô Sudoku để giảm số lần xét nghiệm thực tế mà vẫn định danh gen chính xác.
4. **📡 Mã sửa sai & Phân phát sóng:** Thiết kế mã sửa sai (Error-Correcting Codes) trong truyền dẫn kỹ thuật số và quy hoạch vị trí, kênh tần số trong truyền thông không dây để tối ưu băng thông và tránh nhiễu chéo.
5. **🌾 Thiết kế thực nghiệm nông nghiệp:** Phân chia sơ đồ các ô đất trồng thử nghiệm nông nghiệp. Bố trí hạt giống theo ma trận Latin giúp loại bỏ các yếu tố nhiễu do độ màu mỡ của đất và hướng chiếu mặt trời không đều.
6. **🤖 Trí tuệ nhân tạo & Giải thuật CSP:** Rèn luyện và đánh giá hiệu năng giải thuật Thỏa mãn ràng buộc (CSP) của AI. Sudoku được coi là mô hình thử nghiệm lý tưởng cho các giải thuật duyệt đồ thị và suy luận tự động.

---

## 📏 HƯỚNG DẪN & DỮ LIỆU MẪU: CÔNG CỤ ĐỔI ĐƠN VỊ (UNIT CONVERTER)

Công cụ **Đổi đơn vị** hỗ trợ quy đổi nhanh giữa các đơn vị đo lường vật lý phổ biến và đặc thù trong chương trình học phổ thông. Công cụ cung cấp một môi trường tương tác giúp học sinh hiểu sâu hơn về tỷ lệ chuyển đổi, các giá trị giới hạn vật lý và lịch sử quy đổi một cách trực quan.

### 1. Sơ đồ giao diện và các chức năng chính

```text
┌────────────────────────────────────────────────────────────────────────┐
│               📏 TIÊU ĐỀ: ĐỔI ĐƠN VỊ — UNIT CONVERTER                   │
├────────────────────────────────────────────────────────────────────────┤
│ 🚄 Chọn loại: [📏 C.dài] [⚖️ K.lượng] [🌡️ N.độ] [📐 D.tích] ... [💾 D.liệu]│
├──────────────────────────────────────┬─────────────────────────────────┤
│ 🔄 Quy đổi chính                     │ 📋 Bảng quy đổi nhanh           │
│   • Chọn TỪ: [ m         ] v         │   • Hiển thị 10 đơn vị cơ bản   │
│   • Ô nhập:  [ 1.5       ]           │   • Dạng chuyển đổi xuôi - ngược│
│   • Nút đổi: [     🔄     ]           │                                 │
│   • Chọn SANG:[ cm        ] v         ├─────────────────────────────────┤
│   • Kết quả: [ 150       ] (Read-Only)│ 📜 Lịch sử quy đổi              │
│                                      │   • Lưu 15 giao dịch gần nhất   │
│   • Công thức: 1 m = 100 cm          │   • Click để copy nhanh         │
├──────────────────────────────────────┴─────────────────────────────────┤
│ 📖 Hướng dẫn chi tiết  •  🌍 Ứng dụng thực tế (6 thẻ ảnh động)          │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Tách biệt tab đa năng**:
    *   **Tab 1 (Quy đổi)**: Nơi thực hiện tính toán chính, xem bảng tra cứu nhanh và theo dõi lịch sử.
    *   **Tab 2 (Hướng dẫn)**: Cung cấp hướng dẫn sử dụng nhanh, các ví dụ toán/lý mẫu và mẹo sao chép kết quả.
    *   **Tab 3 (Ứng dụng thực tế)**: Hiển thị 6 thẻ ảnh minh họa độ phân giải cao, thay đổi theo ngôn ngữ hệ thống (`_VN` hoặc `_EN`), giải thích ý nghĩa thực tiễn của từng đại lượng vật lý.
*   **TextBox kết quả thông minh**: Ô kết quả `txtToValue` được thiết kế dạng Read-Only ngăn chặn học sinh nhập đè phá hỏng kết quả, nhưng vẫn cho phép bôi đen chọn và sử dụng phím tắt `Ctrl+C` để copy từng phần giá trị.
*   **Chống spam bộ nhớ lịch sử**: Lịch sử chỉ lưu khi người dùng bấm phím `Enter` trong ô nhập liệu, hoặc khi ô nhập mất tiêu điểm (`LostFocus`), hoặc khi người dùng hoán đổi đơn vị/thay đổi hộp ComboBox.

---

### 2. Các kịch bản giảng dạy & Bộ dữ liệu mẫu thực tế

Dưới đây là các bộ dữ liệu mẫu điển hình theo chương trình môn Vật lý & Công nghệ phổ thông để giáo viên minh họa:

| Phân loại đo lường | Đơn vị nguồn | Đơn vị đích | Giá trị nhập | Kết quả hiển thị | Nhãn công thức / Cảnh báo | Ý nghĩa Vật lý / Sư phạm |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Nhiệt độ (Biên)** | K | °C | `0` | `-273,15` | `0 K = -273,15 °C` | Độ không tuyệt đối - trạng thái nhiệt động tối thiểu của vật chất. |
| **Nhiệt độ (Lỗi)** | °C | K | `-300` | *Trống* | `Nhiệt độ không thể thấp hơn độ không tuyệt đối!` | Đưa ra thông báo chữ đỏ cảnh báo lỗi logic vật lý. |
| **Chiều dài (Lỗi)** | m | cm | `-10` | *Trống* | `Chiều dài không thể nhận giá trị âm!` | Ngăn chặn các đại lượng hình học/vật lý âm. |
| **Khối lượng** | tấn | kg | `1,5` | `1500` | `1 tấn = 1000 kg` | Quy đổi đơn vị đo lường nông/lâm nghiệp thực tiễn Việt Nam. |
| **Năng lượng (Số mũ)**| eV | J | `1` | `1,6020 · 10⁻¹⁹` | `1 eV = 1,6020 · 10⁻¹⁹ J` | Định dạng số mũ Unicode sư phạm cho hằng số vật lý cực nhỏ. |
| **Dữ liệu số** | GB | MB | `2` | `2048` | `1 GB = 1024 MB` | Hệ nhị phân máy tính (bội số của 1024). |

---

## 🧪 HƯỚNG DẪN & DỮ LIỆU MẪU: CÔNG CỤ THANG PH (PH SCALE TOOL)

Công cụ **Thang pH** hỗ trợ trực quan hóa nồng độ ion axit/bazơ và tính toán pH tương ứng. Công cụ là trợ thủ đắc lực trong các bài học về dung dịch, sự điện ly môn Hóa học THPT (Lớp 11) và Khoa học tự nhiên THCS (Lớp 8).

### 1. Sơ đồ giao diện và các chức năng chính

```text
┌────────────────────────────────────────────────────────────────────────┐
│               🧪 TIÊU ĐỀ: THANG pH (pH SCALE)             [🔄 Đặt lại]  │
├────────────────────────────────────────────────────────────────────────┤
│  [📊 Thang pH]  [🧮 Tính pH]  [📖 Hướng dẫn]                             │
├────────────────────────────────────────────────────────────────────────┤
│ 📝 Nhập nồng độ ion                   │ 📋 Kết quả phân tích           │
│   • Ô nhập [H⁺]: [ 0.001     ] mol/L   │   • Giá trị pH: pH = 3,0       │
│                                       │   • Giá trị pOH: pOH = 11,0    │
│   • Hoặc kéo Slider pH:               │   • Nồng độ [OH⁻]: 1,0 · 10⁻¹¹ │
│     [═══O══════════════════════]      │   • Phân loại: 🔴 AXIT         │
│     pH = 3,0 (0 → 14)                 │   • Chỉ báo màu sắc: pH ≈ 3    │
│                                       ├────────────────────────────────┤
│   • Nút chọn nhanh (Presets):         │ 📈 Xem đồ thị pH Graph         │
│     [HCl] [Giấm] [Máu] [NaOH]...      │   • Hàm số y = -log10(x)       │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Đồng bộ hai chiều Slider & TextBox**:
    *   Học sinh kéo Slider pH từ 0 đến 14 $\Rightarrow$ TextBox nồng độ $[H^+]$ tự động cập nhật công thức ngược $[H^+] = 10^{-pH}$.
    *   Học sinh nhập nồng độ $[H^+]$ vào TextBox $\Rightarrow$ Slider tự động di chuyển về vị trí pH tương ứng.
*   **Cảnh báo ngoài khoảng đo chuẩn (0 - 14)**: Khi học sinh nhập các dung dịch axit siêu đặc (ví dụ $[H^+] = 10 \text{ M} \Rightarrow pH = -1$) hoặc bazơ siêu đặc ($[OH^-] = 10 \text{ M} \Rightarrow pH = 15$), hệ thống tự động xuất hiện cảnh báo màu vàng cam: `⚠️ Nồng độ đặc biệt: ngoài khoảng đo chuẩn (0 - 14)`.
*   **Xem đồ thị logarit**: Nút liên kết mở đồ thị biểu diễn hàm số $y = -\log_{10}(x)$ trên hệ tọa độ trực quan, đánh dấu chính xác tọa độ điểm pH hiện tại để học sinh dễ hình dung bản chất toán học.

---

### 2. Các kịch bản giảng dạy & Bộ dữ liệu mẫu thực tế

Dưới đây là bảng ví dụ nồng độ ion và thang pH của các chất phổ biến trong đời sống:

| Tên dung dịch mẫu | Nồng độ $[H^+]$ nhập mẫu (mol/L) | Độ pH tính ra | Độ pOH tính ra | Nồng độ $[OH^-]$ tương ứng (mol/L) | Phân loại môi trường | Cảnh báo hệ thống / Trực quan |
| :--- | :--- | :---: | :---: | :--- | :--- | :--- |
| **HCl đậm đặc** | `10` | `-1` | `15` | `1,000 · 10⁻¹⁵` | 🔴 Axit đặc | `⚠️ Nồng độ đặc biệt: ngoài khoảng đo chuẩn (0 - 14)` |
| **Dịch vị dạ dày** | `0.1` | `1` | `13` | `1,000 · 10⁻¹³` | 🔴 Axit mạnh | Khối màu chỉ báo màu đỏ đậm. |
| **Nước chanh** | `0.01` | `2` | `12` | `1,000 · 10⁻¹²` | 🔴 Axit mạnh | Chỉ báo màu đỏ cam. |
| **Giấm ăn** | `0.001` (hoặc `1e-3`) | `3` | `11` | `1,000 · 10⁻¹¹` | 🔴 Axit vừa | Chỉ báo màu cam. |
| **Cà phê** | `0.00001` (hoặc `1e-5`) | `5` | `9` | `1,000 · 10⁻⁹` | 🔴 Axit yếu | Chỉ báo màu vàng. |
| **Sữa tươi** | `3.16e-7` | `6,5` | `7,5` | `3,162 · 10⁻⁸` | 🔴 Axit yếu | Chỉ báo màu xanh lá nhạt. |
| **Nước tinh khiết** | `1e-7` | `7` | `7` | `1,000 · 10⁻⁷` | 🟢 Trung tính | Khối màu xanh lá cây đậm. |
| **Máu người** | `3.98e-8` | `7,4` | `6,6` | `2,512 · 10⁻⁷` | 🔵 Kiềm yếu | Chỉ báo hơi chuyển xanh lam. |
| **Baking soda** | `1e-9` | `9` | `5` | `1,000 · 10⁻⁵` | 🔵 Kiềm yếu | Chỉ báo xanh dương nhạt. |
| **Thuốc tẩy** | `1e-10` | `10` | `4` | `1,000 · 10⁻⁴` | 🔵 Kiềm vừa | Chỉ báo xanh dương đậm. |
| **Vôi tôi** | `3.16e-13` | `12,5` | `1,5` | `3,162 · 10⁻²` | 🔵 Kiềm mạnh | Chỉ báo màu tím nhạt. |
| **NaOH đậm đặc** | `1e-14` | `14` | `0` | `1,000 · 10⁰` | 🔵 Kiềm mạnh | Chỉ báo màu tím sậm. |

---

## ⚛️ HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ BẢNG TUẦN HOÀN HÓA HỌC (PERIODIC TABLE TOOL)

Công cụ học tập **Bảng tuần hoàn Hóa học** là một ứng dụng tương tác và giả lập hóa học trực quan sinh động, hỗ trợ đắc lực cho chương trình giáo dục phổ thông môn Hóa học (THCS & THPT). Công cụ giúp học sinh dễ dàng làm quen với 118 nguyên tố hóa học, các quy luật tuần hoàn, cấu hình electron, tính tan, tính hoạt động hóa học và các phản ứng mô phỏng thực tế.

### 1. Sơ đồ giao diện trực quan và các vùng chức năng chính

```text
┌────────────────────────────────────────────────────────────────────────┐
│ ⚛️ BẢNG TUẦN HOÀN CÁC NGUYÊN TỐ HÓA HỌC                   [🔍 Tìm kiếm]  │
├──────────────────────────────────────┬─────────────────────────────────┤
│ LƯỚI BẢNG TUẦN HOÀN (118 NGUYÊN TỐ)  │ 📋 BẢNG ĐIỀU KHIỂN CHI TIẾT     │
│  H  [Nhóm IA - VIIIA]             He  │  • Chọn nguyên tố hiển thị     │
│  Li Be            B  C  N  O  F  Ne  │  • Cấu hình Electron 3D        │
│  Na Mg            Al Si P  S  Cl Ar  │  • Thuộc tính Vật lý & Hóa học │
│  K  Ca [Kim loại chuyển tiếp]   ...  │  • Nút mở rộng tính năng:      │
│  Rb Sr            ...                │    [🔬 Giả lập Phản ứng]        │
│  Cs Ba            ...                │    [⚖️ So sánh Kim loại]       │
│  Fr Ra            ...                │    [🧪 Bảng Tính tan]          │
│                                      │    [📜 Quy tắc Cấu hình]       │
├──────────────────────────────────────┴─────────────────────────────────┤
│ 📖 Hướng dẫn sử dụng  •  🏭 Ứng dụng thực tế (6 ví dụ hình ảnh trực quan) │
└────────────────────────────────────────────────────────────────────────┘
```

*   **Bảng tuần hoàn tương tác (Main Grid):**
    *   Các nguyên tố được phân loại màu sắc chuyên nghiệp theo nhóm chất: Kim loại kiềm (đỏ nhạt), Kim loại kiềm thổ (cam), Phi kim (xanh lá), Khí hiếm (tím), Á kim (vàng đất), Halogen (xanh lục), Kim loại chuyển tiếp (xanh dương nhạt).
    *   Mỗi ô nguyên tố hiển thị đầy đủ: Số hiệu nguyên tử (Z), Kí hiệu hóa học, Tên nguyên tố, Khối lượng nguyên tử trung bình.
*   **Hộp Tìm kiếm & Lọc nhanh (Search Panel):** Cho phép gõ tên, kí hiệu hóa học hoặc số hiệu nguyên tử để định vị nguyên tố ngay trên lưới.
*   **Các module tính năng nâng cao:**
    *   **Giả lập Phản ứng (SimulationWindow):** Mô phỏng trực quan các phản ứng hóa học vô cơ, cân bằng phương trình và giải thích hiện tượng.
    *   **So sánh Kim loại (CompareWindow):** So sánh trực quan các tính chất vật lý (nhiệt độ nóng chảy, độ cứng, độ dẫn điện) của nhiều kim loại khác nhau.
    *   **Dãy hoạt động hóa học (ReactivityWindow):** Giả lập thí nghiệm kim loại tác dụng với nước, axit HCl để xếp hạng mức độ hoạt động.
    *   **Bảng tính tan (SolubilityWindow):** Bảng tra cứu tương tác tính tan của các muối và hydroxit phổ biến trong nước.
    *   **Quy tắc electron (RulesWindow):** Minh họa nguyên lý Aufbau, Hund và Pauli trong phân bố electron vào các orbital.

---

### 2. Hướng dẫn sử dụng chi tiết dành cho Giáo viên (Teacher Guide)

*   **Tổ chức các bài giảng minh họa trực quan:**
    *   **Bài giảng Cấu hình electron (Lớp 10):** Sử dụng module **Quy tắc Cấu hình** (`RulesWindow`) để học sinh tự tay xếp các electron vào từng ô lượng tử (orbital) $s, p, d, f$. Giải thích trực quan tại sao phân lớp $4s$ lại có mức năng lượng thấp hơn $3d$ theo nguyên lý Aufbau.
    *   **Bài giảng Tính tan & Phản ứng trao đổi ion (Lớp 11):** Sử dụng **Bảng tính tan tương tác** (`SolubilityWindow`) để hướng dẫn học sinh tra nhanh kết tủa. Chọn cation (ví dụ $Fe^{3+}$) và anion (ví dụ $OH^-$) để hệ thống tự động đánh dấu kết tủa màu nâu đỏ $Fe(OH)_3$ kèm hiện tượng thực tế.
    *   **Bài giảng Dãy điện hóa & Hoạt động kim loại (Lớp 12):** Sử dụng module **Dãy hoạt động** (`ReactivityWindow`) để chiếu mô phỏng thí nghiệm thả Natri ($Na$), Sắt ($Fe$) và Đồng ($Cu$) vào nước. Học sinh sẽ thấy $Na$ phản ứng mãnh liệt, sủi bọt khí mạnh và bốc cháy, $Fe$ không phản ứng ở nhiệt độ thường, còn $Cu$ trơ hoàn toàn.

---

### 3. Hướng dẫn dành cho Học sinh (Student Guide)

*   **Khám phá chi tiết một nguyên tố hóa học:**
    *   Nhấp đúp chuột vào bất kỳ nguyên tố nào trên bảng tuần hoàn để mở **Cửa sổ Chi tiết Nguyên tố** (`ElementDetailWindow`).
    *   Khám phá cấu trúc nguyên tử qua mô hình Bohr chuyển động động, biểu đồ năng lượng ion hóa, thông số bán kính nguyên tử và độ âm điện.
    *   Xem lịch sử phát hiện nguyên tố và các ứng dụng thực tế quan trọng nhất của nguyên tố đó trong đời sống.
*   **Thực hành Cân bằng và Giả lập Phản ứng:**
    *   Bấm nút **🔬 Giả lập Phản ứng** để mở cửa sổ thí nghiệm ảo.
    *   Chọn các chất tham gia phản ứng từ danh sách có sẵn. Phần mềm sẽ tự động cân bằng hệ số tỉ lượng và chạy mô phỏng chuyển động hạt của phản ứng.
    *   Nhấp vào nút **Hiện tượng thực tế** để đọc mô tả chi tiết về sự đổi màu dung dịch, hiện tượng sủi bọt khí hoặc kết tủa tạo thành.

---

### 4. Các kịch bản và Dữ liệu giả lập mẫu (Sample Simulation Data)

Dưới đây là một số kịch bản phản ứng mẫu mà giáo viên và học sinh có thể thực hành giả lập trực tiếp trên hệ thống:

#### 🧪 Kịch bản 1: Phản ứng oxi hóa khử cơ bản (Sắt tác dụng với axit)
*   **Chất tham gia:** Sắt ($Fe$) và Axit Clohiđric ($HCl$)
*   **Phương trình phản ứng tự động cân bằng:**
    $$Fe + 2HCl \rightarrow FeCl_2 + H_2\uparrow$$
*   **Hiện tượng mô phỏng sinh động:** Kim loại Sắt tan dần, dung dịch chuyển sang màu xanh rêu nhạt của ion $Fe^{2+}$, đồng thời sủi bọt khí không màu ($H_2$) liên tục bám quanh đinh sắt và thoát ra ngoài.

#### 🧪 Kịch bản 2: Phản ứng thủy phân muối tạo kết tủa và khí đồng thời (Hydrolysis)
Đây là một phản ứng phức tạp trong chương trình chuyên hóa và ôn thi HSG, dùng để minh họa tính chất lưỡng tính và phản ứng trao đổi ion đặc biệt.
*   **Chất tham gia:** Sắt(III) Clorua ($FeCl_3$) và Natri Cacbonat ($Na_2CO_3$) trong môi trường nước ($H_2O$)
*   **Phương trình phản ứng tự động cân bằng:**
    $$2Fe^{3+} + 3CO_3^{2-} + 3H_2O \rightarrow 2Fe(OH)_3\downarrow + 3CO_2\uparrow$$
*   **Hiện tượng mô phỏng sinh động:** Dung dịch muối $FeCl_3$ màu vàng nâu khi trộn với dung dịch $Na_2CO_3$ lập tức xuất hiện kết tủa keo màu nâu đỏ của Sắt(III) hydroxit $Fe(OH)_3$, đồng thời sủi bọt khí Cacbon đioxit $CO_2$ mạnh mẽ do quá trình thủy phân tương hỗ tạo ra axit yếu không bền $H_2CO_3$ phân hủy ngay lập tức.

#### 📊 Bảng Presets thông số tra cứu nhanh của 6 kim loại tiêu biểu:

| Kí hiệu nguyên tố | Nhiệt độ nóng chảy (°C) | Độ âm điện (Pauling) | Bán kính nguyên tử (pm) | Độ dẫn điện (MS/m) | Tính chất hóa học đặc trưng |
| :---: | :---: | :---: | :---: | :---: | :--- |
| **Na** | $97,8$ | $0,93$ | $186$ | $21,0$ | Kim loại kiềm siêu hoạt động, phản ứng nổ với nước ở nhiệt độ thường. |
| **Al** | $660,3$ | $1,61$ | $143$ | $37,7$ | Kim loại lưỡng tính, bền trong không khí nhờ lớp màng oxit $Al_2O_3$ bảo vệ. |
| **Fe** | $1538$ | $1,83$ | $126$ | $10,0$ | Kim loại chuyển tiếp nhiều hóa trị ($+2, +3$), dễ bị ăn mòn trong không khí ẩm. |
| **Cu** | $1085$ | $1,90$ | $128$ | $59,6$ | Kim loại hoạt động yếu, dẫn điện tốt thứ hai sau Bạc. Không tác dụng với HCl. |
| **Au** | $1064$ | $2,54$ | $144$ | $41,0$ | Kim loại quý tộc cực kỳ trơ, không phản ứng với hầu hết các axit (trừ nước cường toan). |
| **U** | $1132$ | $1,38$ | $138$ | $3,4$ | Kim loại chuyển tiếp thuộc nhóm Actini, có tính phóng xạ và phân hạch hạt nhân. |

---

### 5. 6 ứng dụng thực tế sinh động của các nguyên tố hóa học

Để giúp bài học trở nên sinh động và gắn liền với thực tiễn, tab **Ứng dụng thực tế** tích hợp 6 ví dụ trực quan bằng hình ảnh:

1.  **🚀 Hydro (H) - Năng lượng vũ trụ & Tương lai:** Hydro lỏng là nguồn nhiên liệu đẩy tối ưu cho tên lửa vũ trụ nhờ nhiệt trị cháy cực cao. Ngoài ra, công nghệ pin nhiên liệu Hydro đang mở ra kỷ nguyên xe hơi sạch hoàn toàn không phát thải cacbon.
2.  **💻 Silic (Si) - Trái tim của Công nghệ số:** Silic là chất bán dẫn quan trọng bậc nhất, cấu tạo nên các vi mạch tích hợp (chip vi xử lý) trong máy tính, điện thoại thông minh và là nền tảng của pin năng lượng mặt trời.
3.  **🎈 Heli (He) - Khí khinh khí cầu & Làm lạnh MRI:** Heli siêu nhẹ, không cháy nên cực kỳ an toàn để bơm khinh khí cầu. Ở dạng lỏng, Heli giữ vai trò chất làm lạnh siêu việt cho nam châm siêu dẫn của máy chụp cộng hưởng từ (MRI) y tế.
4.  **🏗️ Sắt (Fe) - Khung xương của mọi Công trình:** Sắt và các hợp kim của sắt (thép) là vật liệu xây dựng và cơ khí chế tạo máy phổ biến nhất hành tinh nhờ độ bền cơ học cao, tính dẻo, khả năng chịu lực vượt trội và giá thành rẻ.
5.  **👑 Vàng (Au) - Trang sức & Lá chắn Vũ trụ:** Ngoài ứng dụng trang sức xa xỉ, Vàng có tính dẫn điện tuyệt vời và không bị oxy hóa. Màng mỏng bằng vàng được sử dụng trên mũ phi hành gia và vệ tinh để làm lá chắn phản xạ tia hồng ngoại từ mặt trời.
6.  **⚛️ Urani (U) - Nguồn Năng lượng hạt nhân:** Urani là nhiên liệu chính trong các nhà máy điện hạt nhân. Quá trình phân hạch nguyên tử Urani-235 giải phóng năng lượng nhiệt khổng lồ, được chuyển hóa thành nguồn điện năng vô tận phục vụ nhân loại.

---

Tài liệu được phân phối trực tiếp kèm theo gói cài đặt sản phẩm QA SmartClass.
Mọi thắc mắc xin liên hệ phòng Quản lý CNTT hoặc Ban quản trị Hệ thống.



## PHỤ LỤC: CẤU HÌNH HỆ THỐNG PHÁT ÂM VÀ LUYỆN ÂM TIẾNG ANH (DÀNH CHO QUẢN TRỊ VIÊN IT PHÒNG MÁY)

Công cụ học tập Từ vựng theo chủ đề có tính năng **"Luyện âm"** sử dụng công cụ nhận dạng giọng nói tích hợp của Windows (`System.Speech.Recognition`). Để chức năng này hoạt động ổn định và chính xác trên máy tính phòng học:

### 1. Yêu cầu hệ điều hành
* Hệ điều hành Windows 10/11 (Không được sử dụng các bản Windows Lite/Windows rút gọn bị lược bỏ dịch vụ Speech).
* Thiết bị cần được kết nối và cấu hình **Microphone** hoạt động tốt.

### 2. Cài đặt Gói Ngôn ngữ Tiếng Anh (en-US)
Công cụ bắt buộc phải có gói ngôn ngữ tiếng Anh Mỹ (`en-US`) để làm cơ sở nhận diện phát âm:
1. Mở **Settings** (Windows + I) -> Chọn **Time & Language** -> Chọn **Language & Region** (hoặc **Language**).
2. Nhấp chọn **Add a language** -> Tìm kiếm và chọn **English (United States)**.
3. **Quan trọng:** Tích chọn vào mục **Speech** (Nhận dạng giọng nói) và nhấp **Install** để tải về.
4. Đợi quá trình tải và cài đặt hoàn tất (yêu cầu máy có kết nối mạng lúc cài đặt).

### 3. Cấu hình Micrô mặc định
1. Click chuột phải vào biểu tượng loa ở khay hệ thống -> Chọn **Sound settings**.
2. Tại mục **Input**, chọn thiết bị Microphone đang sử dụng và đặt làm thiết bị ghi âm mặc định (**Default Input Device**).
3. Đảm bảo âm lượng đầu vào của Micro được thiết lập phù hợp (từ 80-90%) để máy nhận diện rõ giọng đọc của học sinh.
