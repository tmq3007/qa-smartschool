# 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ LẬT THẺ NHỚ (MEMORY GAME)

Tài liệu này cung cấp hướng dẫn sử dụng trực quan, sơ đồ quy trình vận hành, dữ liệu mẫu thực tế và ý nghĩa sư phạm dành cho **Giáo viên** và **Học sinh** khi sử dụng công cụ **Lật Thẻ Nhớ (Memory Game)** thuộc hệ thống trường học thông minh **QA SmartClass**.

---

## 🗺️ 1. Sơ Đồ Quy Trình Vận Hành Thời Gian Thực (Sequence Diagram)

Sơ đồ dưới đây mô tả luồng tương tác giữa Học sinh lật thẻ trên lớp, Hệ thống kiểm tra so khớp/tính điểm/lưu trữ và cơ sở dữ liệu SQLite:

```mermaid
sequenceDiagram
    autonumber
    actor HS as Học sinh (Giao diện)
    participant APP as 🃏 Công cụ Lật Thẻ Nhớ
    database DB as Cơ sở dữ liệu SQLite (smartclass.db)

    Note over HS, APP: Bước 1: Khởi tạo ván game
    HS->>APP: Mở tab "Lật Thẻ Nhớ" / Chọn chủ đề
    APP->>APP: Gọi StopTimers() dừng timer cũ (nếu có)
    APP->>APP: Trộn ngẫu nhiên 8 cặp thẻ (16 thẻ bài)
    APP->>HS: Hiển thị lưới thẻ 4x4 úp kín (❓) & Reset đồng hồ 0s

    Note over HS, APP: Bước 2: Tương tác lật thẻ & So khớp
    HS->>APP: Di chuột qua thẻ -> Hover Effect nổi bật viền sáng
    HS->>APP: Click lật Thẻ thứ 1
    APP->>APP: Hiện text in đậm, phát âm tiếng Anh (TTS) nếu là từ vựng/nguyên tố
    HS->>APP: Click lật Thẻ thứ 2 (Tăng số Lượt chơi)
    APP->>APP: Khóa lưới tạm thời (_isLocked = true)
    
    ALT Đáp án TRÙNG KHỚP (Ví dụ: "Fe" & "Iron")
        APP->>APP: Phát âm thanh đúng, đổi màu nền thẻ thành xanh lá nhạt
        APP->>APP: Đổi trạng thái thẻ thành "matched"
        APP->>APP: Mở khóa lưới ngay lập tức
    ELSE Đáp án KHÔNG KHỚP (Ví dụ: "H" & "Oxygen")
        APP->>APP: Phát âm thanh sai, chạy ngầm _flipTimer 800ms
        APP-->>HS: Giữ nguyên trạng thái hiển thị 2 thẻ lật để ghi nhớ
        Note over APP: Nếu HS bấm "Ván mới" lúc này,<br/>_flipTimer cũ sẽ bị hủy tức thì!
        APP->>APP: Hết 800ms -> Tự động lật úp 2 thẻ (❓) & Mở khóa lưới
    END

    Note over HS, DB: Bước 3: Hoàn thành & Lưu kết quả
    APP->>APP: Tìm đủ 8 cặp -> Dừng _gameTimer
    APP->>HS: Hiển thị lời chúc mừng & tổng số lượt / số giây thực tế
    APP->>DB: Lưu tiến trình vào DB (SaveProgress)
```

---

## 👩‍🏫 2. DÀNH CHO GIÁO VIÊN: VẬN HÀNH TRÊN LỚP HỌC

Giáo viên có thể sử dụng công cụ Lật Thẻ Nhớ để tổ chức các trò chơi khởi động tiết học, ôn tập kiến thức cũ hoặc thi đấu nhóm.

### 💡 Các bước tổ chức trò chơi ghép cặp trên bảng tương tác:
1.  **Chuẩn bị thiết bị:** Phóng to cửa sổ ứng dụng QA SmartClass. Lưới thẻ bài `UniformGrid` 4x4 đã được tối ưu hóa kích thước (`125x95` pixel) và font chữ in đậm nét lớn (`13-24pt Segoe UI Bold`) giúp học sinh ngồi cuối lớp vẫn nhìn thấy rất rõ ràng.
2.  **Chia nhóm thi đấu:** Chia lớp thành 2 đội. Mỗi đội cử đại diện lên bảng tương tác lật thẻ lần lượt.
3.  **Luật chơi cạnh tranh (Speedrun):**
    *   Học sinh thi đấu tìm ra 8 cặp thẻ trong thời gian ngắn nhất (⏱️) và số lượt lật ít nhất (📊).
    *   Khi thắng cuộc, thời gian làm bài thực tế của học sinh sẽ hiển thị chuẩn xác (ví dụ: *28 giây*) và được ghi nhận trực tiếp vào cơ sở dữ liệu để đánh giá điểm số.
4.  **Tích hợp âm thanh:** Nếu trong lớp cần sự yên tĩnh, giáo viên chỉ cần tắt âm thanh hệ thống (chuyển sang `🔇`). Công cụ lật thẻ nhớ sẽ tự động tắt các tiếng bíp đúng/sai nhờ tính năng đồng bộ hóa âm thanh lớp học mới.

---

## 🧑‍🎓 3. DÀNH CHO HỌC SINH: HƯỚNG DẪN THAO TÁC & VÍ DỤ MẪU

### 🎮 Hướng dẫn thao tác từng bước:
*   **Bước 1: Chọn chủ đề học tập:** Ở bảng điều khiển bên phải, nhấp vào ComboBox **Chủ đề** và chọn 1 trong 4 chủ đề.
*   **Bước 2: Di chuột nhận diện:** Di con trỏ chuột qua các thẻ màu xanh lá đậm. Thẻ nào có thể tương tác sẽ sáng lên và nổi viền nhạt để báo hiệu.
*   **Bước 3: Lật tìm cặp tương ứng:** Click chuột trái để lật thẻ thứ nhất, ghi nhớ từ/ký hiệu lật ra. Tiếp tục click lật thẻ thứ hai:
    *   *Nếu đúng:* Hai thẻ đổi sang màu xanh lá cây nhạt và cố định hiển thị.
    *   *Nếu sai:* Hai thẻ sẽ hiển thị trong 0.8 giây để em ghi nhớ vị trí, sau đó tự động úp lại hình dấu hỏi (`❓`).
*   **Bước 4: Chiến thắng:** Lật mở thành công cả 16 thẻ để hoàn thành trò chơi và ghi tên mình vào bảng xếp hạng.

---

## 📊 4. DỮ LIỆU MẪU & VÍ DỤ MINH HỌA CÁC CHỦ ĐỀ

Dưới đây là danh sách dữ liệu mẫu 8 cặp thẻ tương ứng với mỗi chủ đề học tập đã được chuẩn hóa sư phạm:

### 🔢 Chủ đề 0: Phép tính (Toán học tính nhẩm nhanh)
*Ý nghĩa:* Giúp học sinh rèn luyện bảng cửu chương nâng cao và phản xạ số học. Đáp án của các phép tính là duy nhất, không bị trùng lặp giá trị gây nhiễu.

| Thẻ Câu hỏi (Q) | Thẻ Đáp án (A) | Ghi chú / Cách nhẩm |
| :--- | :--- | :--- |
| `7 × 8` | `56` | Bảng cửu chương 7 |
| `9 × 6` | `54` | Bảng cửu chương 9 |
| `12 × 5` | `60` | Phép nhân số tròn chục |
| `15 × 3` | `45` | Nhẩm nhanh $15 \times 3$ |
| `8 × 4` | `32` | Bảng cửu chương 8 |
| `11 × 7` | `77` | Phép nhân số đối xứng |
| `6 × 8` | `48` | Đã sửa đổi tránh trùng đáp án `54` của ván trước |
| `13 × 4` | `52` | Nhẩm nhanh $13 \times 4$ |

---

### 🧪 Chủ đề 1: Nguyên tố (Hóa học phổ thông mới GDPT 2018)
*Ý nghĩa:* Học sinh học thuộc ký hiệu hóa học và tên gọi IUPAC tiếng Anh chuẩn xác. Khi lật mở tên nguyên tố, hệ thống sẽ phát âm giọng đọc tiếng Anh bản xứ.

| Ký hiệu Hóa học (Q) | Tên gọi IUPAC tiếng Anh (A) | Phiên âm quốc tế đọc mẫu (TTS) |
| :--- | :---: | :--- |
| `H` | `Hydrogen` | /ˈhaɪ.drə.dʒən/ |
| `O` | `Oxygen` | /ˈɒk.sɪ.dʒən/ |
| `Fe` | `Iron` | /aɪən/ (Đã sửa từ "Sắt" cũ sang tiếng Anh IUPAC) |
| `Au` | `Gold` | /ɡəʊld/ (Đã sửa từ "Vàng" cũ sang tiếng Anh IUPAC) |
| `Na` | `Sodium` | /ˈsəʊ.di.əm/ (Đã sửa từ "Natri" cũ sang tiếng Anh IUPAC) |
| `Cl` | `Chlorine` | /ˈklɔː.riːn/ (Đã sửa từ "Clo" cũ sang tiếng Anh IUPAC) |
| `Ca` | `Calcium` | /ˈkæl.si.əm/ (Đã sửa từ "Canxi" cũ sang tiếng Anh IUPAC) |
| `C` | `Carbon` | /ˈkɑː.bən/ |

---

### 🇬🇧 Chủ đề 2: Từ vựng (Tiếng Anh giao tiếp lớp học)
*Ý nghĩa:* Học sinh rèn luyện liên tưởng nghĩa từ vựng Anh - Việt và học phát âm từ vựng tiếng Anh.

| Thẻ tiếng Anh (Q) | Thẻ tiếng Việt (A) | Phát âm hỗ trợ (TTS) |
| :--- | :--- | :--- |
| `school` | `trường` | Đọc phát âm từ `"school"` |
| `book` | `sách` | Đọc phát âm từ `"book"` |
| `teacher` | `giáo viên` | Đọc phát âm từ `"teacher"` |
| `student` | `học sinh` | Đọc phát âm từ `"student"` |
| `water` | `nước` | Đọc phát âm từ `"water"` |
| `friend` | `bạn bè` | Đọc phát âm từ `"friend"` |
| `family` | `gia đình` | Đọc phát âm từ `"family"` |
| `house` | `nhà` | Đọc phát âm từ `"house"` |

---

### 📐 Chủ đề 3: Công thức (Toán học & Vật lý Việt Nam hiện hành)
*Ý nghĩa:* Giúp học sinh ghi nhớ các công thức khoa học cơ bản theo đúng chuẩn quy ước ký hiệu đại lượng của Sách giáo khoa Việt Nam.

| Thẻ Công thức (Q) | Thẻ Tên Đại lượng / Định lý (A) | Quy chuẩn sư phạm Việt Nam |
| :--- | :--- | :--- |
| `S = v×t` | `Quãng đường` | Công thức chuyển động thẳng đều |
| `F = m×a` | `Lực` | Định luật II Newton |
| `P = U×I` | `Công suất` | Công suất dòng điện xoay chiều/một chiều ($P = U \times I$) |
| `E = m×c²` | `Năng lượng` | Thuyết tương đối Einstein ($E = mc^2$) |
| `U = I×R` | `Hiệu điện thế` | Định luật Ohm (Đã sửa từ `V = IR` thành hiệu điện thế $U$) |
| `D = m/V` | `Khối lượng riêng` | Ký hiệu khối lượng riêng là D (Đã sửa từ $\rho = m/V$) |
| `a² + b² = c²` | `Định lý Pythagore` | Đã sửa thành biểu thức toán học hoàn chỉnh |
| `pH` | `−log[H⁺]` | Công thức tính nồng độ pH hóa học |

---

## 💾 5. MẪU DỮ LIỆU THỐNG KÊ GHI NHẬN HỆ THỐNG

### 📂 Dữ liệu lưu trong Cơ sở dữ liệu SQLite (`UserProgress` table)
Khi học sinh hoàn thành trò chơi, các dữ liệu sẽ được ghi nhận vào cơ sở dữ liệu `smartclass.db` như sau:

| Id | GameName | Score (Số lượt lật) | Total (Tổng số cặp) | DurationSeconds (Thời gian thực) | Difficulty (Môn học) | CreatedAt (Ngày chơi) |
| :--- | :--- | :---: | :---: | :---: | :--- | :--- |
| 501 | Memory Game | 14 | 8 | 24 | Phép tính | 2026-06-18T08:16:12 |
| 502 | Memory Game | 18 | 8 | 35 | Nguyên tố | 2026-06-18T08:17:45 |
| 503 | Memory Game | 12 | 8 | 19 | Từ vựng | 2026-06-18T08:18:20 |
| 504 | Memory Game | 22 | 8 | 42 | Công thức | 2026-06-18T08:19:05 |

---

## 📈 6. Ý NGHĨA KHOA HỌC GIÁO DỤC CỦA TRÒ CHƠI

1.  **Rèn luyện Trí nhớ không gian (Spatial Memory):** Trò chơi buộc học sinh phải ghi nhớ tọa độ hình học 4x4 của các thẻ bài, giúp cải thiện khả năng tập trung, liên tưởng hình ảnh và ghi nhớ ngắn hạn.
2.  **Liên kết Đa giác quan (Multisensory Learning):** Học sinh vừa nhìn ký hiệu hóa học (thị giác), vừa nghe giọng phát âm tiếng Anh chuẩn IUPAC của máy tính (thính giác) và click chuột tương tác lật bài (vận động). Nghiên cứu giáo dục học chỉ ra việc kết hợp đa giác quan giúp học sinh ghi nhớ từ vựng lâu hơn 60% so với cách học thuộc lòng thông thường.
3.  **Tối ưu hóa học tập cá nhân:** Học sinh có thể theo dõi sự cải thiện của bản thân qua số lượt lật tối thiểu và thời gian hoàn thành nhanh nhất, thúc đẩy ý chí tự học.
