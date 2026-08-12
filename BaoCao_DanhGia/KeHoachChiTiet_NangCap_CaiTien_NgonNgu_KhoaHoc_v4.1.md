# KẾ HOẠCH NÂNG CẤP VÀ CẢI TIẾN CHI TIẾT CÔNG CỤ NGÔN NGỮ & KHOA HỌC (BẢN 4.1)

*Tài liệu thiết kế kỹ thuật, lập phương án tối ưu và checksheet kiểm thử dành cho Nhà phát triển và QA/QC*
*Được biên soạn dưới góc nhìn đa vai trò: Trưởng bộ phận thiết kế dự án QA Smart School, Quản lý IT, Chuyên gia kiểm thử, Chuyên gia thiết kế giao diện, Chuyên gia phân tích hệ thống, CSDL, Bảo mật, các Nhà giáo dục, Hiệu trưởng, Giáo viên ưu tú và Học sinh*

---

## 🎯 MỤC TIÊU & PHẠM VI
Đồng bộ hóa và cải tiến toàn bộ các vấn đề kỹ thuật/sư phạm đã phát hiện trong phân hệ **Công cụ học tập môn Ngôn ngữ và Khoa học** nhằm đảm bảo tính ổn định tối đa của hệ thống, tuân thủ tuyệt đối quy chuẩn sư phạm của chương trình giáo dục Việt Nam (GDPT 2018), hỗ trợ hiển thị tốt trên màn hình tương tác cảm ứng (Touch-friendly), đồng bộ phông chữ thương hiệu cao cấp (Inter và Outfit), và giải quyết triệt để các lỗi logic lập trình/dịch thuật.

Tài liệu này cung cấp thiết kế chi tiết ở mức mã nguồn, phân tích tự phản biện kỹ thuật để đưa ra phương án tối ưu nhất, mô tả dữ liệu đầu vào/đầu ra và bảng kiểm tra (checksheet) từng bước để lập trình viên và kiểm thử viên thực hiện chính xác 100%.

---

## 📂 CẤU TRÚC THƯ MỤC & CÁC TỆP TIN ẢNH HƯỞNG
Lập trình viên cần định vị chính xác các tệp tin sau trước khi sửa đổi:
1.  **Phân hệ Ngôn ngữ (Language Tools):**
    *   [IpaTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Language/IpaTool.xaml.cs) (Bảng phiên âm IPA)
    *   [GrammarTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Language/GrammarTool.xaml) (Bảng ngữ pháp tiếng Anh)
    *   [IrregularVerbsTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Language/IrregularVerbsTool.xaml) (Động từ bất quy tắc)
    *   [VocabularyTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Language/VocabularyTool.xaml) (Lật thẻ nhớ từ vựng)
2.  **Phân hệ Khoa học (Science Tools):**
    *   [MolecularSolver.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/MolecularSolver.cs) (Bộ giải sinh học phân tử)
    *   [MolecularGeneticsTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/MolecularGeneticsTool.xaml.cs) (Giao diện sinh học phân tử)
    *   [PedigreeSolver.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/PedigreeSolver.cs) (Phân tích phả hệ di truyền)
    *   [PhScaleTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/PhScaleTool.xaml.cs) (Thang đo độ pH)
    *   [GeneticsTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/GeneticsTool.xaml.cs) (Di truyền học & Sơ đồ Punnett)
    *   [UnitConverterTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/UnitConverterTool.xaml.cs) (Quy đổi đơn vị)
    *   [CircuitTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/CircuitTool.xaml.cs) (Mạch điện vật lý ảo)
    *   [BoilingFreezingTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/BoilingFreezingTool.xaml.cs) (Nhiệt độ sôi & đông đặc)
    *   [ConstantsTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/ConstantsTool.xaml.cs) (Bảng hằng số vật lý/hóa học)
3.  **Tệp cấu hình chung & Thương hiệu (Design System):**
    *   [DS.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/DS.cs) (Hằng số thiết kế)
    *   [InterOutfitFonts.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Resources/InterOutfitFonts.xaml) (Tài nguyên phông chữ hệ thống)

---

## ═══ CHI TIẾT 5 HẠNG MỤC CẢI TIẾN & ĐÁNH GIÁ CHUYÊN SÂU ═══

### Hạng mục 1: Lỗi chặn đột biến dịch khung (Frameshift) trong Đột biến Gen (MolecularGenetics)
*   **Góc nhìn Chuyên gia Kiểm thử & Phân tích Hệ thống:** 
    Trong tệp [MolecularSolver.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/MolecularSolver.cs#L188), hàm kiểm tra tính hợp lệ `IsValidDNA` chứa đoạn mã giới hạn:
    ```csharp
    if (clean.Length % 3 != 0)
    {
        errorMessage = "Độ dài chuỗi DNA không chia hết cho 3.";
        return false;
    }
    ```
    Đoạn mã này ép buộc chuỗi DNA nhập vào của cả mạch gốc (`wild`) và mạch đột biến (`mutant`) đều phải chia hết cho 3. 
*   **Sai sót kiến thức sư phạm sinh học:**
    Trong sinh học di truyền THPT, đột biến dịch khung (Frameshift mutation) xảy ra khi **thêm hoặc mất 1 hoặc 2 nucleotit** (hoặc một số lượng nucleotit không chia hết cho 3). Việc ép buộc mạch đột biến phải chia hết cho 3 đã chặn đứng khả năng học sinh nhập các ca đột biến dịch khung thực tế. Toàn bộ logic phân tích đột biến dịch khung ở hàm `AnalyzeMutation` (từ dòng 268 đến 273) trở thành **mã chết (dead code)**, không bao giờ được thực thi vì dữ liệu đầu vào đã bị chặn từ bước validate.
*   **Phương pháp khắc phục tối ưu:**
    1.  Tại [MolecularSolver.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/MolecularSolver.cs#L188): Loại bỏ hoàn toàn ràng buộc `% 3 != 0` ra khỏi hàm kiểm tra hợp lệ chung `IsValidDNA`.
    2.  Hỗ trợ dịch mã "mềm": Trong hàm dịch mã `Translate` (dòng 110), nếu chiều dài chuỗi mRNA dư ra 1 hoặc 2 ký tự ở cuối, hệ thống chỉ dịch đến bộ ba hoàn chỉnh cuối cùng và bỏ qua phần dư thừa (giống hệt cơ chế hoạt động của ribosome trong sinh học thực tế), thay vì báo lỗi.
    ```csharp
    // Thay đổi vòng lặp trong Translate:
    for (int i = startIdx; i <= mrna.Length - 3; i += 3) { ... } // Vòng lặp cũ đã tự động bỏ qua phần dư thừa ở cuối, rất an toàn!
    ```

---

### Hạng mục 2: Lỗi xáo trộn dịch thuật & Mất đồng bộ trong 7 Công cụ Khoa học (Practical Applications)
*   **Góc nhìn UI/UX & Quản lý thiết kế:**
    Trong 7 công cụ Khoa học bao gồm `BoilingFreezingTool`, `CircuitTool`, `ConstantsTool`, `ElectronConfigTool`, `GeneticsTool`, `PhScaleTool`, và `UnitConverterTool`, các mục ứng dụng thực tế (Practical Applications) bị xáo trộn nghiêm trọng giữa bản dịch tiếng Anh (EN) và tiếng Việt (VN) từ mục 1 đến mục 6.
*   **Chi tiết lỗi lệch pha tiêu biểu:**
    *   **Thang pH (`PhScaleTool`):**
        *   Mục 1 (Icon 🍔 - Burger): Tiếng Việt ghi `"Dạ dày & Tiêu hóa"` nhưng tiếng Anh lại dịch thành `"Swimming Pool Maintenance"` (Bảo trì bể bơi).
        *   Mục 4 (Icon 🏊 - Bơi): Tiếng Việt ghi `"Xử lý nước & Bể bơi"` nhưng tiếng Anh lại dịch thành `"Gastric Acid & Antacids"` (Axit dạ dày).
        *   Mục 5 (Icon 🌧 - Mưa): Tiếng Việt ghi `"Mưa axit & Môi trường"` nhưng tiếng Anh lại dịch thành `"Food Canning Preservation"` (Đóng hộp thực phẩm).
    *   **Nhiệt độ sôi (`BoilingFreezingTool`):**
        *   Mục 1 (Icon 🧪): Tiếng Việt ghi `"Bảo quản bằng Nitơ lỏng"` nhưng tiếng Anh lại dịch thành `"Antifreeze in Car Radiators"`.
        *   Mục 3 (Icon 🔩): Tiếng Việt ghi `"Luyện kim & Đúc khuôn"` nhưng tiếng Anh lại dịch thành `"Food Freeze-Drying (Lyophilization)"`.
    *   **Quy đổi đơn vị (`UnitConverterTool`):**
        *   Mục 1 (Icon 📍): Tiếng Việt ghi `"Bản đồ & Định vị"` nhưng tiếng Anh lại dịch thành `"International Cargo Logistics"`.
        *   Mục 2 (Icon ⚖): Tiếng Việt ghi `"Vận tải & Giao nhận"` nhưng tiếng Anh lại dịch thành `"Baking Recipe Conversions"`.
*   **Hậu quả sư phạm:**
    Khi người dùng đổi ngôn ngữ hệ thống sang tiếng Anh, hình ảnh minh họa (ví dụ ảnh dạ dày `app_phscale_1_EN.png`) hiển thị cùng tiêu đề bể bơi `"Swimming Pool"`, gây nhiễu loạn kiến thức nghiêm trọng và phá vỡ tính chuyên nghiệp của giao diện tương tác thông minh.
*   **Phương pháp khắc phục tối ưu:**
    Lập trình viên cần tiến hành cấu trúc lại mảng `items` trong code-behind của cả 7 công cụ trên để đảm bảo cấu trúc EN và VN song song 1-1 theo đúng thứ tự logic của Icon và Hình ảnh.
    *Ví dụ chuẩn hóa cho `PhScaleTool.xaml.cs` L460:*
    ```csharp
    // Item 1 (🍔):
    Title = isVN ? "Dạ dày & Tiêu hóa (pH 1.5 - 3.5)" : "Gastric Acid & Antacids",
    Description = isVN 
        ? "Dịch vị dạ dày có tính axit cực mạnh giúp tiêu hóa thức ăn, tiêu diệt vi khuẩn có hại..." 
        : "Neutralize excess stomach acid (pH 1.5 - 3.5) using alkaline antacid tablets to relieve heartburn."
    ```

---

### Hạng mục 3: Lỗi dịch thuật bất nhất & Trùng lặp trong Bảng phiên âm IPA (IpaTool)
*   **Góc nhìn Nhà giáo dục & Chuyên gia dịch thuật:**
    Trong tệp [IpaTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Language/IpaTool.xaml.cs#L315), phần ứng dụng thực tế của IPA có các lỗi nghiêm trọng sau:
    1.  **Trùng lặp chủ đề:** Mục 3 (`Icon="🩺"`, Title = `"Trị liệu & Chữa phát âm"`) và Mục 8 (`Icon="🏥"`, Title = `"Trị liệu phát âm"`) trùng lặp hoàn toàn về mặt bản chất sư phạm và cả hai đều dịch sang tiếng Anh là `"Speech Therapy"`.
    2.  **Lệch dịch thuật thô thiển:**
        *   Mục 4 (Icon 🌐): Tiếng Việt ghi `"Học tập đa ngôn ngữ"`, mô tả về việc dùng IPA để học tiếng nước ngoài ngoài tiếng Anh. Nhưng tiếng Anh lại ghi `"Vocal & Actor Training"`, mô tả về việc huấn luyện ca sĩ và diễn viên phát âm kịch bản!
        *   Mục 5 (Icon 🤖): Tiếng Việt ghi `"Công nghệ Nhận diện giọng nói"`, mô tả về việc dạy AI và máy học. Nhưng tiếng Anh lại dịch thành `"Spelling & Dictation"`, mô tả về việc viết chính tả và từ vựng!
        *   Mục 6 (Icon 🏫): Tiếng Việt ghi `"Soạn bài giảng & Sách giáo khoa"`, mô tả việc giáo viên thiết kế giáo án. Nhưng tiếng Anh lại ghi `"Linguistic Research"`, mô tả về nghiên cứu ngôn ngữ học của các học giả!
*   **Phương pháp khắc phục:**
    1.  Loại bỏ bớt 1 mục trùng lặp (Mục 8) để giữ lại duy nhất 7 mục ứng dụng chất lượng cao.
    2.  Viết lại bản dịch tiếng Anh khớp 100% với nội dung sư phạm tiếng Việt của từng mục.
        *   Mục 4 (Học tập đa ngôn ngữ): Tiếng Anh đổi thành `"Multilingual Learning"` và mô tả việc tiếp cận các ngôn ngữ mới.
        *   Mục 5 (Nhận diện giọng nói): Tiếng Anh đổi thành `"Speech Recognition Technology"`.
        *   Mục 6 (Soạn bài giảng): Tiếng Anh đổi thành `"Lesson Planning & Curriculum"`.

---

### Hạng mục 4: Lỗi thiết kế phông chữ (Typography Inconsistency) đè lên thương hiệu Inter/Outfit
*   **Góc nhìn Chuyên gia thiết kế giao diện (UI/UX):**
    Hệ thống QA SmartClass v4.1 thiết lập hệ thống phông chữ cao cấp đồng bộ tại tệp [InterOutfitFonts.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Resources/InterOutfitFonts.xaml):
    *   Phông chữ chính: `Inter` (Font chữ hiện đại, sắc nét, hỗ trợ cực tốt Unicode tiếng Việt có dấu).
    *   Phông chữ tiêu đề/tab: `Outfit` (Độc đáo, bắt mắt).
    Tuy nhiên, trong các file thiết kế giao diện XAML như `GrammarTool.xaml`, `IrregularVerbsTool.xaml`, và `VocabularyTool.xaml`, lập trình viên lại gõ cứng thuộc tính `FontFamily="Segoe UI"` tại thẻ góc hoặc Grid gốc.
*   **Tác động giao diện:**
    Việc khai báo cứng `FontFamily="Segoe UI"` trong XAML đã hoàn toàn **đè lên (override)** bộ nạp phông chữ thông minh từ `InterOutfitFonts.xaml`. Toàn bộ giao diện các công cụ này bị ép quay về hiển thị bằng phông chữ Segoe UI cũ, phá vỡ tính đồng bộ thẩm mỹ cao cấp của hệ thống.
*   **Đề xuất cải tiến:**
    Xóa bỏ toàn bộ các khai báo cứng `FontFamily="Segoe UI"` trong các file XAML phân hệ Ngôn ngữ và Khoa học, cho phép các controls tự động kế thừa phông chữ `Inter` và `Outfit` từ bộ Styles dùng chung của hệ thống.

---

### Hạng mục 5: Lỗi thiếu sót logic di truyền liên kết X trội (PedigreeSolver)
*   **Góc nhìn Nhà khoa học giáo dục & Chuyên gia di truyền:**
    Trong tệp phân tích phả hệ [PedigreeSolver.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Science/PedigreeSolver.cs#L158), đối với quy luật di truyền liên kết nhiễm sắc thể giới tính X trội (`XLinkedDominant`), hàm kiểm tra mâu thuẫn di truyền chỉ đang kiểm tra hai trường hợp:
    1.  Bố bị bệnh ($X^A Y$) thì con gái phải bị bệnh ($X^A X^a$ hoặc $X^A X^A$).
    2.  Bố mẹ bình thường ($X^a Y \times X^a X^a$) thì không thể sinh con bị bệnh ($X^A Y$ hoặc $X^A X^a$).
*   **Lỗi logic di truyền sót:**
    Quy luật X trội quy định: Con trai ($X^A Y$) bị bệnh phải nhận nhiễm sắc thể $X^A$ từ mẹ. Do đó, **mẹ của con trai bị bệnh bắt buộc phải bị bệnh** ($X^A X^a$ hoặc $X^A X^A$). Nếu con trai bị bệnh nhưng người mẹ hoàn toàn bình thường ($X^a X^a$) thì đây là một mâu thuẫn di truyền tuyệt đối.
    Mã nguồn hiện tại hoàn toàn bỏ sót trường hợp này nếu người cha bị bệnh (khi đó người cha bị bệnh, người mẹ bình thường, sinh con trai bị bệnh -> Hệ thống không báo lỗi mâu thuẫn vì bố mẹ không đồng thời bình thường).
*   **Phương pháp khắc phục:**
    Bổ sung điều kiện kiểm tra mâu thuẫn di truyền X trội cho con trai trong `CheckContradictions`:
    ```csharp
    // Trong nhánh else if (mode == InheritanceMode.XLinkedDominant)
    if (m.Sex == "Nam" && m.IsAffected && !mom.IsAffected)
    {
        m.ContradictionWarning = "Mâu thuẫn di truyền: Con trai bị bệnh trội liên kết X phải có mẹ bị bệnh.";
    }
    ```

---

## ═══ CHỈ DẪN SƯ PHẠM VÀ HƯỚNG DẪN SỬ DỤNG TỪNG BƯỚC ═══

Để giáo viên và học sinh giảm thiểu sai sót khi sử dụng các công cụ học tập, giao diện cần tích hợp các banner chỉ dẫn từng bước (Guided Steps) trực quan:

### 1. Bảng phiên âm IPA (IpaTool)
*   **Bước 1:** Học sinh chọn nhóm âm cần học (Nguyên âm đơn/đôi, Phụ âm) được phân biệt rõ ràng bằng màu sắc pastel dịu mắt.
*   **Bước 2:** Rê chuột vào ký hiệu âm để xem từ ví dụ minh họa khẩu hình.
*   **Bước 3:** Nhấp chuột trái vào thẻ âm để tự động sao chép ký hiệu vào bộ nhớ tạm (Clipboard) để viết bài tập, hệ thống sẽ nhấp nháy dòng phản hồi `📋 Đã chép!` màu xanh lục.

### 2. Mô phỏng Đột biến Gen (MolecularGeneticsTool)
*   **Bước 1:** Nhập chuỗi DNA gốc vào ô **Mạch gốc** (hệ thống tự động chuẩn hóa chữ thường thành chữ hoa và chuyển đổi ký tự C thành X theo chuẩn SGK Việt Nam).
*   **Bước 2:** Nhập chuỗi DNA sau đột biến vào ô **Mạch đột biến**.
*   **Bước 3:** Nhấn nút **Phân tích**. Hệ thống sẽ tự động đối chiếu số liên kết hyđrô thay đổi, chỉ ra vị trí nucleotit bị biến đổi và dịch mã ra chuỗi axit amin tương ứng để kết luận chính xác dạng đột biến (thế, thêm, mất hoặc dịch khung).

---

## 🏁 BẢNG KIỂM THỬ TÍCH HỢP HỒI QUY (QA INTEGRATION TESTING)

QA/QC kiểm thử bắt buộc phải chạy qua các bước kiểm thử sau để nghiệm thu hệ thống sau nâng cấp:

| Bước | Tên bài kiểm thử (Test Case) | Dữ liệu đầu vào (Input) | Luồng thực hiện (Steps) | Kết quả mong đợi (Expected Output) | Trạng thái |
|:---:|---|---|---|---|:---:|
| **1** | Kiểm tra Đột biến Dịch khung | Mạch gốc: `ATG-AXG-GGG`<br>Mạch đột biến: `ATG-AXX-GGG-G` | 1. Nhập hai mạch vào công cụ.<br>2. Nhấn Phân tích. | Hệ thống chấp nhận mạch đột biến có độ dài 10 nu và kết luận: `"Đột biến dịch khung do thêm 1 nucleotit"`. | `[ ] Chưa test` |
| **2** | Kiểm tra Phân dịch pH | Chuyển ngôn ngữ sang tiếng Anh (EN). | 1. Nhấp thẻ Practical Apps của pH.<br>2. Xem Item 1 (🍔) và Item 4 (🏊). | - Item 1 hiển thị Title: `"Gastric Acid & Antacids"`.<br>- Item 4 hiển thị Title: `"Swimming Pool Maintenance"`. | `[ ] Chưa test` |
| **3** | Kiểm tra trùng lặp âm IPA | Nhấp thẻ Practical Apps của IPA. | 1. Đọc danh sách ứng dụng.<br>2. Đếm số mục ứng dụng. | - Danh sách có 7 mục ứng dụng duy nhất (không còn trùng lặp Speech Therapy ở mục 8). | `[ ] Chưa test` |
| **4** | Kiểm tra phả hệ X trội | Tạo phả hệ: Bố bệnh ($X^A Y$), Mẹ thường ($X^a X^a$), sinh Con trai bệnh ($X^A Y$). | 1. Thiết lập phả hệ trên sơ đồ.<br>2. Chọn chế độ X trội (`X-linked Dominant`). | Hệ thống lập tức tô đỏ con trai và báo lỗi: `"Mâu thuẫn di truyền: Con trai bị bệnh trội liên kết X phải có mẹ bị bệnh"`. | `[ ] Chưa test` |
| **5** | Kiểm tra phông chữ | Mở bất kỳ công cụ ngôn ngữ nào. | 1. Xem giao diện trên màn hình.<br>2. Kiểm tra font chữ hiển thị thực tế. | Phông chữ các nhãn hiển thị đúng kiểu font `Inter` sắc nét, không bị Segoe UI đè. | `[ ] Chưa test` |

---

*Tài liệu đã được phê duyệt kỹ thuật bởi Trưởng ban thiết kế dự án QA Smart School. Yêu cầu bộ phận lập trình và kiểm thử phối hợp thực hiện nghiêm túc.*
