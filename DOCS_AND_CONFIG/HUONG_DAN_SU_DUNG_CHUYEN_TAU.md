# 📖 HƯỚNG DẪN TRỰC QUAN & DỮ LIỆU MẪU: CÔNG CỤ HỌC TẬP CHUYẾN TÀU NGỮ VĂN

Tài liệu này cung cấp hướng dẫn chi tiết, sơ đồ vận hành trực quan và các mẫu dữ liệu thực tế giúp **Giáo viên** và **Học sinh** làm quen và làm chủ công cụ học tập tương tác **Chuyến Tàu Ngữ Văn** (Literature Curriculum Tool) trên hệ thống QA SmartClass.

---

## 🗺️ 1. Quy Trình Vận Hành Thời Gian Thực (Sequence Diagram)

Sơ đồ dưới đây mô tả sự tương tác giữa Giáo viên (sử dụng bảng tương tác thông minh) và Học sinh trong một bài học thực tế sử dụng Chuyến Tàu Ngữ Văn:

```mermaid
sequenceDiagram
    autonumber
    actor GV as Giáo viên (Bảng tương tác)
    actor HS as Học sinh (Thiết bị cá nhân)
    database CSDL as Cơ sở dữ liệu (smartclass.db & JSON)

    Note over GV: Bước 1: Chuẩn bị & Lựa chọn bài học
    GV->>CSDL: Tải dữ liệu chặng hành trình từ JSON (Khối lớp 6 - 12)
    GV->>GV: Chọn tác phẩm giảng dạy (Ví dụ: "Qua Đèo Ngang")

    Note over GV, HS: Bước 2: Giảng dạy tương tác
    GV->>GV: Bật giọng đọc diễn cảm tích hợp (🔊 Audio)
    GV->>GV: Trình chiếu văn bản & dùng bút bôi đen (Highlight từ khóa)
    HS->>HS: Theo dõi văn bản, tự bôi đen từ khóa trên thiết bị cá nhân

    Note over HS, CSDL: Bước 3: Luyện tập trắc nghiệm cảm thụ
    HS->>HS: Chuyển sang Tab "Bài tập" & làm trắc nghiệm cảm thụ
    loop Chuỗi trả lời đúng (Streak)
        HS->>HS: Trả lời đúng liên tiếp -> Tăng Streak count
        Note over HS: Hiển thị nhãn lửa "🔥 Chuỗi đúng: X"
    end
    HS->>CSDL: Trả lời câu cuối -> Kích hoạt hiệu ứng chúc mừng (Fireworks Burst)

    Note over GV, CSDL: Bước 4: Kiểm tra & Sao lưu CSDL
    GV->>CSDL: Lọc Nhật ký hoạt động bảo mật theo ngày giờ
    GV->>CSDL: Nhấn "Đồng bộ & Sao lưu" -> Checkpoint WAL -> Backup CSDL an toàn
```

---

## 🖥️ 2. Bản Đồ Giao Diện Trực Quan (Visual Interface Layout)

Dưới đây là sơ đồ bố cục giao diện của công cụ **Chuyến Tàu Ngữ Văn** được thiết kế trực quan, hỗ trợ tối đa tương tác cảm ứng (Touch-friendly):

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ 🚂 CHUYẾN TÀU NGỮ VĂN • Lớp 6 - Lớp 12                       [← Quay lại]    │
├──────────────────────────────────────────────────────────────────────────────┤
│ 🚄 Ga Hành Trình Lớp:  [ Lớp 6 ] [ Lớp 7 ] [ Lớp 8 ] ... [ Lớp 12 ]          │
├──────────────────────────────────────────────────────────────────────────────┤
│ 🏠 [🚂 Chuyến Tàu]   [📚 Chương Trình]   [📝 Kho Đề Thi]   [📊 Dashboard GV] │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  📖 BÀI HỌC: QUA ĐÈO NGANG (Bà Huyện Thanh Quan)                             │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ 📌 Tác giả: Bà Huyện Thanh Quan (Đầu thế kỷ XIX)                       │  │
│  │   [ 🔊 Phát giọng đọc mẫu ]   [ ⏸️ Tạm dừng ]                             │  │
│  │                                                                        │  │
│  │ 📖 Văn bản tác phẩm:                                                   │  │
│  │   "Bước tới Đèo Ngang, bóng xế tà,                                     │  │
│  │    Cỏ cây chen đá, lá chen hoa.                                        │  │
│  │    Lom khom dưới núi, tiều vài chú,                                    │  │
│  │    Lác đác bên sông, chợ mấy nhà..."                                   │  │
│  │                                                                        │  │
│  │ 💡 Hướng dẫn: Nhấp giữ kéo chuột để tô màu vàng (Highlight) từ khóa   │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  📝 LUYỆN TẬP TRẮC NGHIỆM               Tiến trình: 2/3      🔥 Chuỗi đúng: 3│
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ Câu 2: Biện pháp đảo ngữ trong 'Lom khom dưới núi... Lác đác bên sông' │  │
│  │        có tác dụng gì?                                                 │  │
│  │   (A) Nhấn mạnh sự vắng vẻ, nhỏ bé của con người trước thiên nhiên.    │  │
│  │   (B) Diễn tả sự tấp nập, nhộn nhịp của phiên chợ nghèo.               │  │
│  │   (C) Thể hiện lòng trung quân ái quốc của nữ sĩ.                      │  │
│  │                                                                        │  │
│  │   [ 💡 Gợi ý làm bài ]                        [ 📖 Xem lời giải chi tiết]│  │
│  └────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 👩‍🏫 3. DÀNH CHO GIÁO VIÊN: PHƯƠNG PHÁP & KỊCH BẢN GIẢNG DẠY

Giáo viên có thể sử dụng Chuyến Tàu Ngữ Văn trên màn hình tương tác thông minh tại lớp để tổ chức bài giảng sinh động qua các bước sau:

### 1. Dẫn nhập bài học trực quan (Warm-up)
*   **Chọn lớp học:** Trên thanh **Ga hành trình**, nhấp chọn khối lớp (ví dụ: `Lớp 7`). Hệ thống sẽ tự động vẽ sơ đồ đường ray kết nối các bài học của lớp 7 như những nhà ga liên hoàn.
*   **Chọn bài học:** Nhấp chọn ga `Bài 1: Thơ trữ tình trung đại` -> Nhấp tiếp vào tác phẩm `Qua Đèo Ngang`.

### 2. Tổ chức hoạt động đọc hiểu & phân tích văn bản
*   **Giọng đọc diễn cảm:** Nhấp chọn biểu tượng phát audio `🔊` ở góc văn bản tác phẩm. Hệ thống sẽ phát trực tiếp bản ngâm thơ chất lượng cao giúp học sinh cảm nhận nhịp điệu trầm buồn, hoài cổ của tác phẩm.
*   **Highlight từ khóa nghệ thuật:** Sử dụng ngón tay hoặc bút tương tác bôi đen trực tiếp lên văn bản (ví dụ: các từ láy *"lom khom"*, *"lác đác"* hoặc cụm từ *"ta với ta"*). Các từ này lập tức được tô màu vàng sáng nổi bật để ghi nhớ bài học.

### 3. Đồng bộ hóa dữ liệu & Quản trị lớp học
*   **Lọc Nhật ký an toàn (Logs):** Trong tab **Dashboard GV**, giáo viên có thể lọc nhật ký hệ thống bằng cách chọn khoảng thời gian trên hai ô `DatePicker` và nhấn **"Lọc Nhật ký"**. Hệ thống sẽ lọc bỏ các ký tự nguy hiểm và hiển thị danh sách hoạt động an toàn.
*   **Sao lưu dữ liệu (UAC-Safe Database Backup):** Nhấp nút **"Đồng bộ & Sao lưu"**. Hệ thống sẽ tự động thực hiện tiến trình:
    1.  Ghi toàn bộ dữ liệu đang lưu trong bộ nhớ tạm SQLite (WAL Checkpoint) vào đĩa cứng một cách toàn vẹn.
    2.  Nhân bản tệp tin CSDL `smartclass.db` vào thư mục an toàn của người dùng tại:
        `C:\Users\<Tên_User>\AppData\Local\QASmartClass\backup\smartclass_backup_yyyyMMdd_HHmmss.db`
        *(Đường dẫn này giúp tránh hoàn toàn lỗi từ chối ghi tệp tin do Windows UAC bảo vệ ổ đĩa hệ thống)*.
    3.  **Tự động dọn dẹp:** Hệ thống chỉ lưu giữ tối đa 5 bản sao lưu mới nhất để giải phóng không gian bộ đĩa cứng.

> [!IMPORTANT]
> **Khuyên dùng sư phạm cho Giáo viên:**
> *   Hãy khuyến khích học sinh thi đua chuỗi câu đúng (Streak). Đặt mục tiêu: *"Nhóm nào đạt chuỗi đúng 🔥 Streak từ 4 câu trở lên trong bài học Thánh Gióng sẽ nhận được 1 điểm cộng tinh thần!"*
> *   Khi học sinh làm xong câu hỏi cuối, hệ thống sẽ kích hoạt pháo hoa chúc mừng. Hãy hướng sự chú ý của cả lớp lên bảng tương tác để tuyên dương thành tích của học sinh đó.

---

## 🧑‍🎓 4. DÀNH CHO HỌC SINH: HƯỚNG DẪN TỰ LẬP LUYỆN & KHÁM PHÁ

Học sinh có thể mở công cụ này trên thiết bị cá nhân tại lớp học hoặc ở nhà để tự học và luyện tập trắc nghiệm cảm thụ:

### 1. Đọc hiểu lý thuyết & Khám phá tư liệu hình ảnh
*   **Đọc văn bản lý thuyết:** Đọc phần giới thiệu tác giả và tác phẩm. Tương tự giáo viên, học sinh cũng có thể kéo bôi đen các định nghĩa, đặc sắc nghệ thuật để tự tạo highlight ghi nhớ của riêng mình.
*   **Xem ảnh tư liệu minh họa (Gallery):**
    *   Học sinh nhấp chọn tác phẩm để xem tranh ảnh minh họa (ví dụ: Tranh vẽ Thánh Gióng cưỡi ngựa sắt, trận chiến Sơn Tinh - Thủy Tinh).
    *   *Lưu ý:* Nếu tệp tin ảnh bị thất lạc trong thư mục cài đặt, giao diện học sinh sẽ không bị lỗi vỡ khung mà hiển thị một thẻ Placeholder màu tím thanh nhã kèm dòng chữ: `📚 Đang cập nhật hình ảnh tư liệu` để đảm bảo trải nghiệm thẩm mỹ không bị gián đoạn.

### 2. Chinh phục thử thách trắc nghiệm (Streak & Fireworks)
*   **Trả lời câu hỏi:** Chuyển sang phần bài tập và nhấp chọn đáp án trắc nghiệm (A, B, C, D).
*   **Bùng cháy ngọn lửa Streak:** 
    *   Mỗi khi em trả lời đúng liên tiếp, nhãn **Chuỗi đúng** ở góc trên sẽ tăng dần: `🔥 Chuỗi đúng: 1`, `🔥 Chuỗi đúng: 2`, `🔥 Chuỗi đúng: 3`...
    *   Chuỗi đúng càng cao thể hiện khả năng cảm thụ tác phẩm của em càng tốt.
*   **Xem lời giải chi tiết:** Nếu gặp câu khó hoặc trả lời sai, hãy nhấp chọn nút **"Lời giải"**. Lời giải phân tích sâu sắc từ các thầy cô sẽ giúp em sửa sai ngay lập tức.
*   **Bùng nổ pháo hoa chúc mừng:** Khi hoàn thành câu hỏi cuối cùng của bài học, màn hình sẽ bùng nổ hiệu ứng pháo hoa rực rỡ sắc màu bay tỏa khắp giao diện trong vòng 1.2 giây để chúc mừng nỗ lực của em!

---

## 📊 5. CÁC VÍ DỤ DỮ LIỆU MẪU ĐIỂN HÌNH (DETAILED SAMPLE CURRICULUM DATA)

Dưới đây là bảng tổng hợp dữ liệu mẫu chất lượng cao đã được tích hợp sẵn trong công cụ học tập Chuyến Tàu Ngữ Văn để giáo viên và học sinh trải nghiệm trực quan:

| Khối lớp | Tên tác phẩm (Mã chương) | Nội dung khái niệm chính | Ví dụ Câu hỏi & Đáp án | Lời giải chi tiết (Explanation) |
| :--- | :--- | :--- | :--- | :--- |
| **Lớp 6** | **Thánh Gióng**<br/>(`g06_ch01`) | **Thể loại truyền thuyết:** Truyện kể dân gian có yếu tố kỳ ảo về nhân vật lịch sử.<br/><br/>**Ý nghĩa vươn vai:** Sức mạnh quật khởi phi thường của dân tộc trước giặc ngoại xâm. | **Câu hỏi:** Chi tiết Gióng 3 tuổi không nói nhưng bỗng lên tiếng khi nghe tin giặc ngoại xâm có ý nghĩa gì?<br/><br/>**Đáp án:** Tinh thần yêu nước tiềm ẩn trong mỗi người Việt — khi Tổ quốc nguy biến sẽ bùng cháy mạnh mẽ. | Gióng chọn lần đầu tiên nói là nói lời xin đi đánh giặc. Điều này thể hiện lòng yêu nước luôn ẩn sâu trong tim mỗi người dân, bất cứ lúc nào đất nước cần họ sẽ đứng lên đấu tranh. |
| **Lớp 6** | **Cô bé bán diêm**<br/>(`g06_ch03`) | **Nghệ thuật tương phản:** Đối lập giữa cảnh đói rét, đơn độc ngoài phố và ánh đèn ấm áp bên trong cửa sổ.<br/><br/>**5 lần quẹt diêm:** Ước mơ về hơi ấm, thức ăn, gia đình sum vầy. | **Câu hỏi:** Năm lần quẹt diêm của cô bé bán diêm thể hiện những ước mơ gì theo thứ tự?<br/><br/>**Đáp án:** Sưởi ấm (lò sưởi) → Ăn no (ngỗng quay) → Vui chơi (cây thông) → Tình thương (người bà) → Được ở cùng bà mãi mãi. | Mỗi lần quẹt diêm tương ứng với ước mơ cháy bỏng trong cơn đói rét: lò sưởi mang hơi ấm, ngỗng quay xua đói khát, cây thông Noel mang niềm vui trẻ thơ, người bà mang tình yêu thương gia đình, và quẹt hết bao diêm để níu giữ bà đi cùng em đến cõi vĩnh hằng. |
| **Lớp 7** | **Qua Đèo Ngang**<br/>(`g07_ch01`) | **Đảo ngữ nghệ thuật:** *"Lom khom dưới núi..."* đưa từ láy lên đầu nhằm tạo cảm xúc thưa thớt, vắng lặng.<br/><br/>**Nỗi lòng tác giả:** *"Ta với ta"* thể hiện nỗi cô đơn tuyệt đối giữa đất trời Đèo Ngang. | **Câu hỏi:** Cụm từ 'ta với ta' ở câu cuối bài thơ 'Qua Đèo Ngang' có ý nghĩa gì?<br/><br/>**Đáp án:** Chỉ một mình tác giả — ta (chủ thể) đối diện với ta (bóng mình) giữa trời non nước cô đơn. | Khác với *"ta với ta"* trong thơ Nguyễn Khuyến (chỉ hai người bạn thân), cụm từ này trong thơ Bà Huyện Thanh Quan chỉ có duy nhất một người tự đối diện với chính mình, bộc lộ nỗi cô đơn tột cùng của nữ sĩ trước thiên nhiên rộng lớn. |
| **Lớp 7** | **Cốm**<br/>(`g07_ch03`) | **Đặc sản ẩm thực thanh tao:** Cốm Vòng làm từ hạt lúa non ngậm sữa bọc trong lá sen thơm dịu.<br/><br/>**Nghệ thuật thưởng thức:** Ăn cốm phải ăn thong thả, từng chút để ngẫm nghĩ hương đồng nội. | **Câu hỏi:** Theo tác giả Thạch Lam, làm thế nào để cảm nhận hết hương vị thanh nhã của món Cốm Vòng?<br/><br/>**Đáp án:** Phải ăn thong thả, từng chút một, nhai chậm rãi để ngẫm nghĩ và cảm nhận sự hòa quyện của lúa non và hương sen. | Cốm không phải là thức quà ăn để no, mà là thức quà thưởng thức nghệ thuật. Do đó cần nhai chậm, thong thả để chất ngọt dẻo của nếp non quyện cùng hương thơm dịu mát của lá sen lan tỏa trong khoang miệng. |
| **Lớp 12** | **Tây Tiến**<br/>(`g12_ch01`) | **Cảm hứng lãng mạn & Bi tráng:** Vẻ đẹp hào hùng kiêu dũng của người lính Tây Tiến vượt đèo dốc hiểm trở nhưng cũng mang đậm chất thơ lãng mạn, bi tráng của thời kháng chiến chống Pháp. | **Câu hỏi:** Vẻ đẹp của người lính Tây Tiến trong câu thơ 'Tây Tiến đoàn binh không mọc tóc / Quân xanh màu lá dữ oai hùm' mang tính chất gì?<br/><br/>**Đáp án:** Tính chất bi tráng — tuy gian khổ, bệnh tật làm rụng tóc, da xanh xao nhưng tâm hồn vẫn oai phong, anh dũng. | Dù hiện thực kháng chiến vô cùng khắc nghiệt khiến các chiến sĩ bị sốt rét rừng rụng tóc, da xanh mét, nhưng tác giả không gọi họ là bệnh binh mà là "đoàn binh không mọc tóc", "dữ oai hùm" thể hiện tinh thần kiêu dũng, hiên ngang vượt lên cái chết. |

---

## 🛠️ 6. PHỤ LỤC: CẤU TRÚC TỆP DỮ LIỆU JSON ĐỂ GIÁO VIÊN TỰ SOẠN THÊM BÀI

Hệ thống cho phép giáo viên tự tạo thêm các bài học mới hoặc cập nhật nội dung bằng cách viết thêm tệp tin JSON và đặt vào thư mục `Resources/LiteratureData/Phase4_Data/`.

### 📝 Định dạng chuẩn của một tệp tin bài học JSON (`Gxx_CHxx.json`):

```json
{
  "metadata": {
    "chapterId": "g06_ch03",
    "chapterName": "Bài 3: Yêu thương và chia sẻ",
    "grade": 6,
    "totalProblems": 2,
    "totalSections": 1,
    "estimatedHours": 2
  },
  "sections": [
    {
      "sectionId": "s_g6_ch2_0",
      "sectionName": "Tên Tác Phẩm Ví Dụ",
      "sgkUrl": "https://url-sach-giao-khoa.vn",
      "audioUrl": "https://url-file-am-thanh-doc-dien-cam.mp3",
      "concepts": [
        {
          "name": "Tiêu đề khái niệm lý thuyết",
          "theorem": "Nội dung định nghĩa hoặc tóm tắt lý thuyết để bôi đen highlight.",
          "imageUrls": ["Resources/LiteratureData/Images/ten_file_anh.png"]
        }
      ],
      "problems": [
        {
          "id": "p1",
          "question": "Nội dung câu hỏi trắc nghiệm?",
          "difficulty": "Easy",
          "options": [
            "Đáp án đúng (luôn viết đáp án đúng ở vị trí khớp với trường answer)",
            "Đáp án sai thứ nhất",
            "Đáp án sai thứ hai",
            "Đáp án sai thứ ba"
          ],
          "answer": "Đáp án đúng (phải trùng khớp chính xác từng ký tự với một tùy chọn trong mảng options)",
          "hints": ["Gợi ý gợi nhớ cho học sinh làm bài."],
          "explanation": "Lời giải thích chi tiết tại sao đáp án đó đúng để học sinh tham khảo."
        }
      ]
    }
  ]
}
```

> [!CAUTION]
> **Quy tắc an toàn khi biên soạn dữ liệu:**
> *   Không chèn các ký tự đặc biệt nguy hiểm như `<`, `>`, `'`, `;` vào trong các trường nội dung câu hỏi hoặc phương án trả lời để tránh lỗi bảo mật chuỗi.
> *   Nếu có ký tự đặc biệt, hệ thống sẽ tự động lọc bỏ (Sanitize) thông qua hàm an toàn trước khi hiển thị lên màn hình.
