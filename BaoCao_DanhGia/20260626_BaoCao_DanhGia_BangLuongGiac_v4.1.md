# BÁO CÁO ĐÁNH GIÁ CHUYÊN SÂU CÔNG CỤ HỌC TẬP "BẢNG LƯỢNG GIÁC" (BẢN 4.1)

*Tài liệu thẩm định chất lượng giao diện, logic toán học và quy chuẩn sư phạm theo tiêu chuẩn QA SmartClass v4.1*
*Được thực hiện bởi Hội đồng Chuyên gia Đa ngành Dự án QA Smart School*

---

## 🎯 MỤC TIÊU ĐÁNH GIÁ
Đánh giá toàn diện và chi tiết phân hệ **Bảng Lượng Giác** (Tab **Tính toán**) dựa trên ảnh chụp giao diện thực tế. Đối chiếu trực tiếp với các tiêu chuẩn sư phạm của chương trình phổ thông Việt Nam (GDPT 2018), trải nghiệm tương tác thực tế trên màn hình cảm ứng lớp học (Smart Board/Touch), và các ràng buộc kỹ thuật của QA SmartClass v4.1. Từ đó chỉ ra các lỗi logic, lỗi sư phạm, điểm bất hợp lý trong thiết kế và đề xuất phương án cải tiến cụ thể.

---

## ═══ PHẦN 1: BẢNG TỔNG HỢP LỖI & ĐIỂM CẦN CẢI TIẾN ═══

| STT | Phân loại | Hiện trạng giao diện | Lỗi / Điểm bất hợp lý chi tiết | Hệ quả sư phạm & Kỹ thuật | Giải pháp khắc phục đề xuất |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | **Logic Toán học & Sư phạm** | Nhập góc **$129.8^\circ$** nhưng kết quả hiển thị công thức là **$130^\circ$** (ví dụ: `sin(130°) = 0.7683`). | **LỖI LOGIC NẶNG:** Sử dụng giá trị góc làm tròn ($130^\circ$) làm nhãn hiển thị trong khi tính toán bằng góc thực ($129.8^\circ$). Thực tế $\sin(130^\circ) \approx 0.7660$ chứ không phải $0.7683$. | Học sinh bị rối loạn kiến thức, nghi ngờ tính chính xác của phần mềm. Giáo viên mất uy tín sư phạm khi bị học sinh phát hiện sai lệch toán học. | Đồng bộ hóa nhãn hiển thị góc với giá trị nhập thực tế: `sin(129.8°) = 0.7683`, tương tự cho tất cả các hàm khác. |
| **2** | **Logic hiển thị Radian** | Hàng cuối cùng ghi: `129.8° = 2.2654 rad = 2.265`. | **DƯ THỪA & THIẾU NHÃN:** Giá trị hiển thị lặp lại vô nghĩa (`2.2654` và `2.265`), không có đơn vị rõ ràng cho vế cuối cùng. | Giao diện thiếu chuyên nghiệp, học sinh không hiểu ý nghĩa của con số `= 2.265` là gì. | Sửa thành định dạng chuẩn: `129.8° = 2.2654 rad` (loại bỏ phần lặp dư thừa) hoặc hiển thị cả dạng phân số của $\pi$: `129.8° ≈ 0.72π rad (2.2654 rad)`. |
| **3** | **Sư phạm & Công thức** | Hộp công thức phía trên ghi: `tan = sin/cos • cot = cos/sin • 1 + tan² = 1/cos²`. | **THIẾU KÝ HIỆU BIẾN & THIẾU CÔNG THỨC CƠ BẢN:** <br>1. Viết công thức không kèm biến góc $\theta$ (sai quy chuẩn ký hiệu Toán học).<br>2. Thiếu công thức đối ngẫu: $1 + \cot^2\theta = 1/\sin^2\theta$. | Học sinh bắt chước cách viết tắt cẩu thả không kèm góc biến. Bộ công thức bị khuyết thiếu, không hệ thống. | Thay đổi thành ký hiệu toán học chuẩn dạng phân số Latex:<br>$\tan\theta = \frac{\sin\theta}{\cos\theta}$; $\cot\theta = \frac{\cos\theta}{\sin\theta}$<br>$1 + \tan^2\theta = \frac{1}{\cos^2\theta}$ ($\cos\theta \neq 0$)<br>$1 + \cot^2\theta = \frac{1}{\sin^2\theta}$ ($\sin\theta \neq 0$). |
| **4** | **Giao diện & Đồ họa** | Vòng tròn lượng giác bên phải có kích thước nhỏ, bao quanh bởi quá nhiều không gian trống (padding/margin lớn). | **BỐ CỤC MẤT CÂN ĐỐI:** Khung chứa màu trắng rộng nhưng vòng tròn hiển thị quá nhỏ, các nhãn trục (1, -1) và điểm $P$ rất bé. | Học sinh ngồi cuối lớp hoàn toàn không nhìn rõ các chuyển động, màu sắc và tọa độ trên vòng tròn lượng giác. | Tăng tỷ lệ hiển thị của vòng tròn lượng giác lên 1.5 lần, chiếm trọn 85% khung chứa màu trắng. Tăng font size của điểm $P$, góc $\theta$ và các giá trị biên của trục tọa độ. |
| **5** | **UX & Đồ họa trực quan** | Các đường thẳng lượng giác hình học (Đỏ, Xanh, Cam, Lục) trên đồ thị không có nhãn chỉ dẫn. | **THIẾU CHỈ DẪN TRỰC QUAN:** Người dùng không biết đường thẳng màu xanh đại diện cho $\cos$ hay đường màu cam đại diện cho $\tan$ nếu không đối chiếu bảng màu bên ngoài. | Giảm hiệu quả trực quan hóa hình học lượng giác. | Thêm nhãn chữ nhỏ đồng màu trực tiếp bên cạnh các đoạn thẳng trên vòng tròn: Chữ "sin" cạnh đường đỏ, "cos" cạnh đường xanh, "tan" cạnh đường cam, "cot" cạnh đường lục. |
| **6** | **Tương tác (Touch-friendly)** | Các nút bấm "Lịch sử tra cứu" và "Góc thường dùng" có kích thước nhỏ và nằm quá sát nhau. | **THIẾU TỐI ƯU CẢM ỨNG:** Thiết kế nút dẹt và khít chỉ phù hợp với tương tác chuột, rất dễ bấm nhầm trên màn hình tương tác cảm ứng lớp học. | Giáo viên/học sinh bị bấm đúp, bấm lệch nút trong lúc giảng bài, gây ức chế khi sử dụng. | Tăng chiều cao của các nút bấm lên tối thiểu 40px, thêm khoảng cách (margin) giữa các nút tối thiểu 8px để tránh chạm nhầm. |
| **7** | **Trải nghiệm Học đường** | Thiếu chỉ dẫn sử dụng từng bước ngay trên tab chính đang thao tác. | **THIẾU HƯỚNG DẪN ĐỊNH HƯỚNG (Quick Onboarding):** Người dùng phải chuyển sang tab khác để xem hướng dẫn, gây ngắt quãng trải nghiệm. | Giáo viên bỡ ngỡ trong lần đầu tiếp cận công cụ, thao tác sai thứ tự. | Thêm một biểu tượng trợ giúp nhỏ `(?)` bên cạnh tiêu đề "Bảng Lượng Giác", khi nhấp vào sẽ hiển thị Tooltip hướng dẫn 3 bước nhanh. |
| **8** | **Chuẩn hóa Đơn vị** | Nút lựa chọn đơn vị hiển thị là `Độ (*)` và `Radian (rad)`. | **SỬ DỤNG KÝ TỰ KHÔNG CHUẨN:** Dùng ký tự dấu sao `(*)` để biểu thị mặc định hoặc ký hiệu độ trông rất thiếu chuyên nghiệp. | Gây hiểu nhầm về mặt toán học (dấu sao thường là phép nhân hoặc chú thích). | Đổi nhãn nút thành: `Độ (°)` và `Radian (rad)` rõ ràng, trực quan. |
| **9** | **Nhãn số liệu đầu vào** | Số đo góc hiển thị số lớn **129.8** nhưng không đi kèm đơn vị đo ở ô nhập. | **THIẾU ĐƠN VỊ ĐẦU VÀO:** Người dùng dễ nhầm lẫn giá trị số này là độ hay radian nếu không nhìn xuống radio button. | Giảm tốc độ nhận thức thông tin. | Thêm đơn vị tự động ngay sau số hiển thị (Ví dụ: **129.8°** hoặc **129.8 rad**). |
| **10** | **Chức năng mở rộng** | Hiển thị `sec (Mở rộng)` và `csc (Mở rộng)` mặc định cho học sinh lớp 10-12. | **DƯ THỪA ĐỐI TƯỢNG PHỔ THÔNG:** Hàm secant và cosecant không nằm trong chương trình SGK GDPT 2018 của Việt Nam. | Gây quá tải nhận thức cho học sinh trung bình và yếu. | Thêm nút Toggle "Hiển thị Nâng cao (sec/csc)" để ẩn/hiện các giá trị mở rộng này theo nhu cầu giảng dạy của giáo viên. |

---

## ═══ PHẦN 2: Ý KIẾN CHI TIẾT TỪ HỘI ĐỒNG 17 CHUYÊN GIA ═══

### 1. 🎨 Trưởng bộ phận thiết kế dự án QA Smart School
*   **Ý kiến:** Hệ thống UI cần được đồng bộ hóa với định danh phiên bản của hệ thống. Sidebar trái ghi "QA SMART CLASS v3.0", trong khi chúng ta đang áp dụng bộ quy chuẩn thiết kế sư phạm v4.1. Điều này cho thấy sự bất nhất trong việc đóng gói và kiểm soát phiên bản phần mềm.
*   **Giải pháp:** Yêu cầu cập nhật nhãn phiên bản hệ thống lên v4.1. Đồng thời, tái cấu trúc tỷ lệ phần trăm phân chia khung màn hình: giảm bớt chiều rộng của cột nhập liệu bên trái và tăng không gian hiển thị của đường tròn lượng giác trực quan bên phải để tạo sự cân đối thị giác.

### 2. 💻 Quản lý IT (IT Manager)
*   **Ý kiến:** Từ góc độ kỹ thuật hệ thống, cơ chế tương tác đầu vào cho giá trị góc hiện tại khá mập mờ. Nếu đây là một ứng dụng chạy trên màn hình tương tác không có bàn phím vật lý, việc chỉ có một ô nhập số mà không tích hợp một bộ **Virtual NumPad** (bàn phím số ảo chuyên dụng) hoặc một thanh trượt **Slider** để kéo thay đổi góc sẽ khiến việc nhập liệu cực kỳ bất tiện.
*   **Giải pháp:** Tích hợp bộ Slider mượt mà phía dưới số đo góc hoặc tự động kích hoạt TouchNumPad của hệ thống khi người dùng chạm vào số đo góc.

### 3. 🔍 Chuyên gia kiểm thử phần mềm (QA Tester)
*   **Ý kiến:** Tôi đã phát hiện lỗi nghiêm trọng về logic hiển thị công thức (rounding bug). Hàm hiển thị nhãn chuỗi toán học sử dụng biến góc đã bị làm tròn qua hàm `Math.round()` hoặc định dạng định kiểu nguyên (`int`), trong khi hàm tính toán giá trị lượng giác lại sử dụng góc thực kiểu `double`. Điều này dẫn đến sự lệch pha nghiêm trọng: $129.8^\circ$ biến thành $130^\circ$ trên nhãn, nhưng giá trị tính ra lại là $\sin(129.8^\circ) = 0.7683$ chứ không phải $\sin(130^\circ) = 0.7660$.
*   **Giải pháp:** Viết lại hàm format chuỗi hiển thị công thức: sử dụng chính xác giá trị góc thực tế với số chữ số thập phân được đồng bộ (ví dụ: lấy 1 chữ số thập phân tương ứng với giá trị đầu vào).

### 4. 📐 Chuyên gia thiết kế giao diện phần mềm (UI/UX Expert)
*   **Ý kiến:** Vấn đề phối màu trên bảng kết quả khá ổn, các dải màu pastel nhẹ nhàng giúp phân biệt các hàm lượng giác. Tuy nhiên, nút "Đặt lại" màu tím đậm ở thanh tiêu đề có vị trí đứng đơn độc và khoảng cách quá sát tiêu đề "Bảng Lượng Giác", làm phá vỡ lưới căn lề (grid alignment).
*   **Giải pháp:** Di chuyển nút "Đặt lại" sang góc bên phải của thanh tiêu đề công cụ để đảm bảo tính đối xứng. Tăng cỡ chữ và độ tương phản của các nhãn trên vòng tròn lượng giác để đạt chuẩn tiếp cận WCAG 2.1.

### 5. ⚙️ Chuyên gia phân tích và thiết kế hệ thống (Systems Analyst)
*   **Ý kiến:** Hệ thống lưu trữ lịch sử tra cứu "Lịch sử tra cứu" lưu trữ các giá trị góc gần nhất: `129.8°`, `130.2°`... Thiết kế hiện tại thiếu một nút chức năng để "Xóa lịch sử". Nếu giáo viên muốn chuẩn bị một danh sách các góc đặc biệt cho bài giảng mới, họ không thể chủ động dọn dẹp các góc cũ đã tra cứu.
*   **Giải pháp:** Thêm một icon thùng rác nhỏ bên cạnh nhãn "Lịch sử tra cứu" để xóa nhanh danh sách góc đã tra cứu.

### 6. 🗄️ Chuyên gia cơ sở dữ liệu và thiết bị kết nối ngoại vi
*   **Ý kiến:** Thiết bị đầu vào ngoại vi như bút cảm ứng viết bảng (Stylus Pen) trên màn hình thông minh cần được hỗ trợ. Công cụ nên có tính năng nhận dạng chữ viết tay đơn giản cho số đo góc để giáo viên viết góc trực tiếp lên bảng và hệ thống tự nhận diện sang số thực.
*   **Giải pháp:** Thiết lập sự kiện nhận diện Ink Canvas tại vùng nhập liệu số đo góc để hỗ trợ tốt nhất cho bút thông minh.

### 7. 🛡️ Chuyên gia về bảo mật và an ninh mạng
*   **Ý kiến:** Mặc dù đây là công cụ học tập cục bộ, ô nhập góc vẫn có nguy cơ bị khai thác lỗi tràn bộ nhớ hoặc XSS nếu giáo viên/học sinh cố tình nhập vào các chuỗi ký tự đặc biệt hoặc mã độc script thay vì số.
*   **Giải pháp:** Áp dụng bộ lọc Regex chặt chẽ tại đầu vào: chỉ cho phép ký tự số `0-9` và duy nhất một dấu chấm/phẩy thập phân. Khống chế giới hạn góc nhập từ $-7200^\circ$ đến $7200^\circ$.

### 8. 🏫 Nhà giáo dục (Educator)
*   **Ý kiến:** Thiết kế sư phạm của bảng lượng giác này đang đi ngược lại nguyên lý phát triển nhận thức. Ở trung học phổ thông, học sinh học vòng tròn lượng giác để hiểu bản chất hình học của sin (trục tung), cos (trục hoành), tan và cot (trục phụ). Việc hiển thị vòng tròn lượng giác quá nhỏ và thiếu chú thích trục sẽ làm giảm vai trò của đồ thị trực quan, biến công cụ này thành một chiếc "máy tính bỏ túi" cơ học chỉ để tra số.
*   **Giải pháp:** Mở rộng đồ thị vòng tròn lượng giác, hiển thị rõ ràng trục tung là trục Sin, trục hoành là trục Cos, đường thẳng đứng bên phải là trục Tan, đường nằm ngang bên trên là trục Cot để học sinh dễ ghi nhớ bản chất hình học.

### 9. 🎓 Nhà quản lý hiệu trưởng nhà trường
*   **Ý kiến:** Tôi đánh giá cao việc tích hợp các tab "Hướng dẫn & Ví dụ" và "Ứng dụng Thực tế". Điều này giúp giáo viên dễ dàng soạn giáo án tích hợp liên môn và giáo án STEM. Tuy nhiên, lỗi hiển thị sai công thức $\sin(130^\circ) = 0.7683$ cần phải được sửa ngay lập tức trước khi đưa phần mềm vào giảng dạy chính thức, nhằm tránh các khiếu nại về chất lượng đào tạo từ phụ huynh và xã hội.
*   **Giải pháp:** Yêu cầu phòng kỹ thuật kiểm định chất lượng (QC) chạy bộ test tự động kiểm tra toàn bộ giá trị lượng giác từ $0^\circ$ đến $360^\circ$ trước khi triển khai bản cập nhật.

### 10. 👥 Trưởng bộ môn của trường
*   **Ý kiến:** Trong phân phối chương trình Toán cấp THPT, các góc lượng giác đặc biệt như $210^\circ, 225^\circ, 240^\circ, 300^\circ, 315^\circ, 330^\circ$ được học sinh sử dụng rất thường xuyên. Nhưng danh sách "Góc thường dùng" của phần mềm lại bỏ qua hoàn toàn các góc ở góc phần tư thứ III và IV này.
*   **Giải pháp:** Bổ sung đầy đủ các góc đặc biệt thuộc góc phần tư III và IV vào danh sách "Góc thường dùng" để giáo viên không phải nhập tay thủ công các góc này.

### 11. 👩‍🏫 Giáo viên ưu tú với nhiều kinh nghiệm
*   **Ý kiến:** Học sinh thường rất dễ bị nhầm lẫn giữa đơn vị Độ và Radian. Khi tôi giảng bài, nếu tôi chuyển đơn vị từ Độ sang Radian, tôi muốn toàn bộ danh sách "Lịch sử tra cứu" và "Góc thường dùng" cũng phải tự động quy đổi đơn vị tương ứng (ví dụ: $30^\circ$ đổi thành $\pi/6$). Hiện tại các nút góc thường dùng vẫn cố định là độ.
*   **Giải pháp:** Khi chọn đơn vị Radian, danh sách góc thường dùng nên chuyển sang dạng phân số của $\pi$ (như $\pi/6, \pi/4, \pi/3, \pi/2...$) để học sinh làm quen với tư duy Radian.

### 12. 👦 Học sinh (Student)
*   **Ý kiến:** Em thấy vòng tròn lượng giác bên phải nhìn rất đẹp nhưng em không thể lấy tay chạm vào điểm $P$ để xoay vòng tròn được. Việc chỉ nhìn một hình vẽ đứng yên và bấm nút số khiến bài học lượng giác rất khô khan.
*   **Giải pháp:** Cho phép học sinh kéo thả điểm $P$ chạy vòng quanh đường tròn lượng giác bằng cảm ứng đa điểm, góc $\theta$ và các giá trị $\sin, \cos$ thay đổi theo thời gian thực một cách mượt mà.

### 13. 🧹 Nhân viên nhà trường (IT Support Staff)
*   **Ý kiến:** Giao diện tab "Tính toán" đang thiếu tính năng in ấn nhanh hoặc xuất ảnh vòng tròn lượng giác ra file để làm tài liệu học tập. Khi học sinh muốn lưu lại đồ thị một góc lượng giác cụ thể để chèn vào vở ghi điện tử, các em phải chụp ảnh toàn màn hình rồi cắt ghép rất thủ công.
*   **Giải pháp:** Thêm một nút "Xuất ảnh đồ thị" (dưới dạng PNG hoặc SVG) ngay dưới vòng tròn lượng giác để giáo viên và học sinh lưu trữ nhanh.

### 14. 🎮 Gamer giỏi (Pro Gamer - Đánh giá UX Flow & Micro-interactions)
*   **Ý kiến:** Flow tương tác của tính năng này chưa tạo được sự hứng thú (engagement). Khi thay đổi góc, đồ thị nhảy bụp một phát sang vị trí mới mà không có hiệu ứng chuyển cảnh mượt mà (smooth transition/animation).
*   **Giải pháp:** Thêm hiệu ứng di chuyển mượt (interpolated animation) cho điểm $P$ chạy dọc đường tròn và hiệu ứng thanh kết quả lượng giác co giãn động (progress bar animation) khi thay đổi giá trị.

### 15. 🏢 Cán bộ quản lý của phòng giáo dục
*   **Ý kiến:** Giao diện cần tuân thủ luật giáo dục và ngôn ngữ chuẩn quốc gia. Phần mềm dùng từ tiếng Anh xen kẽ như `rad`, `sec`, `csc`. Mặc dù có mở rộng nhưng cần chú thích rõ ràng bằng tiếng Việt hoặc tuân thủ chuẩn ký hiệu trong SGK mới.
*   **Giải pháp:** Thêm phần dịch nghĩa hoặc chú thích thuật ngữ toán học ở tab "Hướng dẫn & Ví dụ".

### 16. 🏫 Chuyên viên của sở giáo dục
*   **Ý kiến:** Việc đưa các ứng dụng thực tế vào tab riêng là điểm sáng lớn. Tuy nhiên, tính năng tính toán này nên hỗ trợ hiển thị giá trị lượng giác dưới dạng số vô tỷ căn thức (ví dụ: $\sin(120^\circ) = \sqrt{3}/2$ thay vì chỉ hiển thị số thập phân $0.8660$). Điều này giúp học sinh đối chiếu trực tiếp với các bài kiểm tra tự luận trên giấy.
*   **Giải pháp:** Thêm dòng hiển thị giá trị chính xác dạng căn thức (Ví dụ: `sin(120°) = √3/2 ≈ 0.8660`).

### 17. 🔬 Nhà khoa học giáo dục (Educational Scientist)
*   **Ý kiến:** Để giảm tải nhận thức (cognitive load) cho học sinh theo lý thuyết của John Sweller, chúng ta cần tránh việc học sinh phải phân tán sự chú ý giữa bảng số liệu bên dưới và đồ thị hình học bên trên. Việc đồng bộ màu sắc giữa các dòng kết quả (Sin đỏ, Cos xanh...) và các đoạn thẳng tương ứng trên vòng tròn lượng giác là một điểm cộng lớn, nhưng cần làm rõ nét hơn nữa bằng cách cho phép nhấp vào dòng kết quả thì đoạn thẳng tương ứng trên đồ thị sẽ nhấp nháy phát sáng.
*   **Giải pháp:** Triển khai hiệu ứng tương tác chéo (Cross-highlighting) giữa bảng số liệu và đồ thị hình học lượng giác.

---

## ═══ PHẦN 3: KẾ HOẠCH HÀNH ĐỘNG & CHECKSHEET KHẮC PHỤC ═══

Lập trình viên và bộ phận QA/QC cần thực hiện khắc phục theo 3 giai đoạn sau để nâng cấp công cụ đạt chuẩn QA SmartClass v4.1:

### 📋 GIAI ĐOẠN 1: SỬA LỖI LOGIC TOÁN HỌC & SƯ PHẠM (Thực hiện ngay)
- [ ] **1.1 Đồng bộ góc thực tế trên nhãn hiển thị:**
  *   *Mã nguồn:* Sửa đổi hàm format chuỗi hiển thị công thức kết quả. Thay thế biến góc làm tròn bằng biến góc thực tế.
  *   *Ví dụ sửa đổi:* `txtSinResult.Text = $"sin({actualAngle}°) = {sinValue:F4}"` thay vì `sin({roundedAngle}°)`.
- [ ] **1.2 Chuẩn hóa hiển thị Radian:**
  *   *Sửa đổi:* Loại bỏ phần lặp lại vô nghĩa `= 2.265`. Format kết quả radian về 4 chữ số thập phân ổn định: `129.8° = 2.2654 rad`.
- [ ] **1.3 Sửa đổi hộp công thức gợi ý:**
  *   *XAML:* Thay thế chuỗi công thức tĩnh thành hệ ký hiệu toán học có kèm biến góc $\theta$.
  *   *Nội dung mới:* $\sin^2\theta + \cos^2\theta = 1$ • $\tan\theta = \sin\theta/\cos\theta$ • $\cot\theta = \cos\theta/\sin\theta$ • $1 + \tan^2\theta = 1/\cos^2\theta$ • $1 + \cot^2\theta = 1/\sin^2\theta$.
- [ ] **1.4 Ẩn/Hiện các hàm mở rộng:**
  *   *UI:* Thêm một checkbox hoặc nút Toggle "Hiển thị sec/csc" ở góc dưới bảng kết quả. Mặc định ẩn 2 dòng kết quả này để chống nhiễu sư phạm.

### 📋 GIAI ĐOẠN 2: TÁI CẤU TRÚC LAYOUT VÀ TỐI ƯU CẢM ỨNG (Ngắn hạn)
- [ ] **2.1 Phóng to vòng tròn lượng giác:**
  *   *XAML:* Điều chỉnh `Width` và `Height` của đồ thị vòng tròn lượng giác bên phải tăng lên 1.5 lần. Giảm padding của Border bao quanh đồ thị xuống còn 10px.
- [ ] **2.2 Thêm nhãn trực tiếp trên đồ thị hình học:**
  *   *Đồ họa:* Vẽ bổ sung các nhãn văn bản (Text Block) nhỏ đồng màu chuyển động bám sát các đoạn thẳng lượng giác trên đồ thị (sin, cos, tan, cot).
- [ ] **2.3 Tối ưu kích thước nút bấm cảm ứng:**
  *   *XAML:* Đặt `MinHeight="40"` và `Margin="4"` cho toàn bộ các nút bấm góc đặc biệt trong "Góc thường dùng" và các nút trong "Lịch sử tra cứu".
- [ ] **2.4 Đồng bộ nút Đơn vị:**
  *   *Sửa đổi:* Đổi nhãn `Độ (*)` thành `Độ (°)`. Khi thay đổi đơn vị sang `Radian`, tự động cập nhật danh sách góc đặc biệt sang dạng radian tương ứng ($\pi/6, \pi/4...$).

### 📋 GIAI ĐOẠN 3: TƯƠNG TÁC NÂNG CAO & CHỈ DẪN NGƯỜI DÙNG (Trung hạn)
- [ ] **3.1 Tích hợp kéo thả cảm ứng đa điểm:**
  *   *Mã nguồn:* Bổ sung sự kiện `ManipulationDelta` hoặc `MouseMove/TouchMove` trên điểm $P$, cho phép giáo viên kéo điểm $P$ chạy quanh vòng tròn để thay đổi góc trực tiếp.
- [ ] **3.2 Thêm chỉ dẫn sử dụng nhanh (Quick Guide):**
  *   *UI:* Thiết kế một nút trợ giúp `(?)` nhỏ trên thanh tiêu đề. Khi nhấp vào, hiển thị một cửa sổ hội thoại overlay mờ hướng dẫn 3 bước cơ bản bằng tiếng Việt rõ ràng.
- [ ] **3.3 Xuất giá trị chính xác dạng căn thức:**
  *   *Mã nguồn:* Tích hợp thuật toán chuyển đổi số thập phân sang phân số căn thức đối với các góc đặc biệt (như $\sqrt{3}/2, \sqrt{2}/2, 1/2...$) và hiển thị song song trên bảng kết quả.
