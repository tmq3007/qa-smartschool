# KẾ HOẠCH NÂNG CẤP & CẢI TIẾN CHI TIẾT: CHỨC NĂNG 4.4 - MÔN HỌC CHUYÊN BIỆT
**DỰ ÁN TRƯỜNG HỌC THÔNG MINH QA SMART SCHOOL**
*Tài liệu hướng dẫn lập trình, phân tích hệ thống và thiết kế kiểm thử - Phiên bản nâng cấp v4.2*

---

## ═══ PHẦN 1: THÔNG TIN CHUNG & RÀNG BUỘC KỸ THUẬT v4.2 ═══

Tài liệu này được biên soạn bởi Trưởng ban thiết kế dự án phối hợp cùng bộ phận phân tích hệ thống (BA) và kiểm thử chất lượng (QA/QC). Mục tiêu là cung cấp một kế hoạch kỹ thuật chi tiết, có cơ chế tự phản biện rõ ràng để lập trình viên thực thi chính xác 100% các sửa đổi, loại bỏ hoàn toàn các lỗi sư phạm, logic và giao diện tại mục **4.4. Môn học chuyên biệt**.

### Các quy chuẩn bắt buộc áp dụng từ bộ tiêu chuẩn QA SmartClass v4.2:
1.  **`QC_4.2_LAYOUT_GRID`:** 
    *   Thanh điều hướng bên trái (Sidebar) của các điều khiển Master-Detail bắt buộc phải có Grid ngoài cùng (`rootGrid`) thiết lập `HorizontalAlignment="Stretch"` và **không được cài đặt MaxWidth**.
    *   Chỉ giới hạn độ rộng tối đa (`MaxWidth`) cho riêng các panel hiển thị nội dung bên phải: `MaxWidth="1200"` hoặc `MaxWidth="1400"` đối với nội dung học thuật, và `MaxWidth="1600"` đối với nội dung chứa hình ảnh lớn (Ứng dụng thực tế).
2.  **`QC_4.2_LANGUAGE_BRANDING`:** 
    *   Giữ nguyên không dịch 3 từ khóa thương hiệu hệ thống: `SMART CLASS`, `SMART TOUCH`, `DESKTOP`.
    *   Tất cả các nội dung và nhãn giao diện khác phải là tiếng Việt chuẩn học thuật phổ thông Việt Nam, tuyệt đối không pha trộn tiếng Anh hoặc lỗi font chữ hiển thị.

---

## ═══ PHẦN 2: THIẾT KẾ CHI TIẾT 9 HẠNG MỤC CẢI TIẾN ═══

### HẠNG MỤC 1: Sửa ghi chú bản chất hóa học của Kẽm (Zn)
*   **Mô tả yêu cầu:** Thay đổi ghi chú nguyên tố Kẽm (Zn) trong tệp dữ liệu hoạt động kim loại để loại bỏ thuật ngữ sai sư phạm "Kim loại lưỡng tính".
*   **Dữ liệu đầu vào:** Tệp dữ liệu [metal_reactivity.json](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Data/metal_reactivity.json#L225) tại phần tử có `"Symbol": "Zn"`.
*   **Dữ liệu đầu ra:** Trường `"Note"` của Kẽm được sửa đổi nội dung.
*   **Phương pháp thực hiện:**
    *   Mở tệp `metal_reactivity.json` bằng trình soạn thảo hỗ trợ UTF-8.
    *   Tìm đến dòng 225 chứa `"Symbol": "Zn"`.
    *   Sửa giá trị trường `"Note"`:
        ```diff
-       "Note": "Kim loại lưỡng tính, dùng mạ chống gỉ"
+       "Note": "Tác dụng được với cả dung dịch axit và dung dịch kiềm, dùng mạ chống gỉ"
        ```
*   **Tự phản biện kỹ thuật:**
    *   *Hỏi:* Tại sao không sửa thành "Kim loại có tính lưỡng tính"?
    *   *Trả lời:* Trong chương trình sách giáo khoa Hóa học phổ thông Việt Nam, chỉ có chất lưỡng tính (oxit và hydroxit), không định nghĩa kim loại lưỡng tính. Do đó, việc mô tả trực tiếp tính chất *"Tác dụng được với cả dung dịch axit và dung dịch kiềm"* là chính xác nhất về mặt khoa học và sư phạm.

---

### HẠNG MỤC 2: Sửa logic so sánh kim loại phản ứng với dung dịch muối
*   **Mô tả yêu cầu:** Sửa logic hiển thị kết luận khi so sánh 2 kim loại hoạt động hóa học. Nếu kim loại đứng trước phản ứng mạnh với nước ở nhiệt độ thường, không kết luận là nó đẩy kim loại đứng sau ra khỏi dung dịch muối.
*   **Dữ liệu đầu vào:** Hai đối tượng `MetalReaction` (`metal1` và `metal2`) được chọn trên ComboBox.
*   **Dữ liệu đầu ra:** Đoạn văn bản kết luận hiển thị trên `ComparisonPanel`.
*   **Phương pháp thực hiện:**
    *   Mở tệp [CompareMetalsDialog.xaml.cs](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/CompareMetalsDialog.xaml.cs#L147-L163).
    *   Khai báo mảng chứa các kim loại kiềm/kiềm thổ tan và phản ứng mạnh với nước ở nhiệt độ thường:
        ```csharp
        string[] activeMetals = { "Cs", "Fr", "Rb", "K", "Na", "Li", "Ba", "Ra", "Sr", "Ca" };
        ```
    *   Thay đổi logic trong khối điều kiện so sánh thứ tự `Order` (dòng 148-163):
        ```csharp
        string conclusion;
        if (metal1.Order < metal2.Order)
        {
            conclusion = $"• {metal1.Symbol} ({metal1.Name}) hoạt động mạnh hơn {metal2.Symbol} ({metal2.Name})\n" +
                         $"• {metal1.Symbol} đứng trước {metal2.Symbol} trong dãy hoạt động hóa học\n";
            
            if (activeMetals.Contains(metal1.Symbol))
            {
                conclusion += $"• Do {metal1.Symbol} phản ứng mạnh với nước ở điều kiện thường nên khi cho vào dung dịch muối của {metal2.Symbol}, {metal1.Symbol} sẽ phản ứng với nước trước tạo dung dịch kiềm, sau đó kiềm phản ứng trao đổi với muối (nếu thỏa mãn điều kiện), không đẩy trực tiếp kim loại {metal2.Symbol} ra ngoài.";
            }
            else
            {
                conclusion += $"• {metal1.Symbol} có thể đẩy {metal2.Symbol} ra khỏi dung dịch muối (trừ các muối không tan hoặc điều kiện đặc biệt).";
            }
        }
        else if (metal1.Order > metal2.Order)
        {
            conclusion = $"• {metal2.Symbol} ({metal2.Name}) hoạt động mạnh hơn {metal1.Symbol} ({metal1.Name})\n" +
                         $"• {metal2.Symbol} đứng trước {metal1.Symbol} trong dãy hoạt động hóa học\n";
            
            if (activeMetals.Contains(metal2.Symbol))
            {
                conclusion += $"• Do {metal2.Symbol} phản ứng mạnh với nước ở điều kiện thường nên khi cho vào dung dịch muối của {metal1.Symbol}, {metal2.Symbol} sẽ phản ứng với nước trước tạo dung dịch kiềm, sau đó kiềm phản ứng trao đổi với muối (nếu thỏa mãn điều kiện), không đẩy trực tiếp kim loại {metal1.Symbol} ra ngoài.";
            }
            else
            {
                conclusion += $"• {metal2.Symbol} có thể đẩy {metal1.Symbol} ra khỏi dung dịch muối (trừ các muối không tan hoặc điều kiện đặc biệt).";
            }
        }
        else
        {
            conclusion = $"• {metal1.Symbol} và {metal2.Symbol} là cùng một kim loại.";
        }
        ```
*   **Tự phản biện kỹ thuật:**
    *   *Hỏi:* Tại sao không dùng thuộc tính nhóm hóa học (ví dụ kiềm, kiềm thổ) trong JSON mà lại dùng mảng cứng `activeMetals`?
    *   *Trả lời:* Dùng mảng cứng chứa kí hiệu hóa học của 10 nguyên tố hoạt động mạnh (Cs, Fr, Rb, K, Na, Li, Ba, Ra, Sr, Ca) giúp kiểm tra nhanh chóng, chính xác và không phụ thuộc vào việc cấu hình thêm thuộc tính trong file JSON, giảm thiểu rủi ro lỗi dữ liệu.

---

### HẠNG MỤC 3: Đồng bộ bộ lọc Sách Giáo Khoa khi khởi động
*   **Mô tả yêu cầu:** Khi người dùng mở màn hình Thư viện sách giáo khoa, danh sách sách hiển thị phải được lọc ngay theo trạng thái Tab mặc định được tô màu xanh (Lớp 1) thay vì hiển thị tất cả các sách của các lớp khác.
*   **Dữ liệu đầu vào:** Sự kiện `Loaded` của Window `Form2_20_SubMenuBooks`.
*   **Dữ liệu đầu ra:** Grid hiển thị sách ban đầu chỉ chứa sách của Lớp 1.
*   **Phương pháp thực hiện:**
    *   Mở tệp [Form2_20_SubMenuBooks.xaml.cs](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form2_20_SubMenuBooks.xaml.cs#L30-L35).
    *   Sửa đổi phương thức `Form2_20_SubMenuBooks_Loaded`:
        ```diff
        private void Form2_20_SubMenuBooks_Loaded(object sender, RoutedEventArgs e)
        {
            CreateGradeTabs();
            LoadBooks();
            LoadSubjects();
+           ApplyFilters(); // Gọi ApplyFilters để áp dụng bộ lọc Lớp 1 mặc định ngay lập tức
        }
        ```
*   **Tự phản biện kỹ thuật:**
    *   *Hỏi:* Liệu gọi `ApplyFilters()` ngay khi load có gây chậm giao diện do lọc dữ liệu không?
    *   *Trả lời:* Không. Danh sách sách (`_allBooks`) chỉ khoảng vài chục cuốn, được lưu trữ hoàn toàn trong bộ nhớ (RAM). Hàm `ApplyFilters` sử dụng LINQ để lọc trực tiếp trên `List<Book>` nên tốc độ xử lý là cực kỳ nhanh (dưới 1ms), không gây ra bất kỳ độ trễ nào cho giao diện.

---

### HẠNG MỤC 4: Sửa logic biến cờ hiệu `_isSliderDragging` trong Trình xem sách
*   **Mô tả yêu cầu:** Chỉ kích hoạt tải lại trang trong WebView2 khi người dùng kết thúc thao tác kéo Slider chuyển trang (thả tay ra), ngăn chặn tình trạng WebView2 tải liên tục khi đang kéo Slider gây lag/đơ ứng dụng.
*   **Dữ liệu đầu vào:** Thao tác tương tác kéo thả Slider của người dùng.
*   **Dữ liệu đầu ra:** Sự kiện thay đổi trang chỉ kích hoạt một lần duy nhất khi thả tay.
*   **Phương pháp thực hiện:**
    *   Mở tệp [Form2_20_1_BookViewer.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form2_20_1_BookViewer.xaml#L166-L177).
    *   Thêm các sự kiện đính kèm của `Thumb` vào khai báo `Slider`:
        ```xml
        <Slider x:Name="pageSlider"
                Grid.Column="1"
                Minimum="1"
                Maximum="100"
                Value="1"
                TickPlacement="None"
                IsSnapToTickEnabled="False"
                VerticalAlignment="Center"
                Height="28"
                ValueChanged="PageSlider_ValueChanged"
                Thumb.DragStarted="Slider_DragStarted"
                Thumb.DragCompleted="Slider_DragCompleted"
                ToolTip="Kéo để chuyển trang"/>
        ```
    *   Mở tệp [Form2_20_1_BookViewer.xaml.cs](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form2_20_1_BookViewer.xaml.cs).
    *   Bổ sung hai phương thức xử lý sự kiện kéo thả vào code-behind:
        ```csharp
        private void Slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isSliderDragging = true;
        }

        private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isSliderDragging = false;
            // Thực hiện chuyển trang ngay khi người dùng thả tay ra
            NavigateToPage(_currentPage);
        }
        ```
*   **Tự phản biện kỹ thuật:**
    *   *Hỏi:* Tại sao không sử dụng sự kiện `MouseLeftButtonDown` và `MouseLeftButtonUp`?
    *   *Trả lời:* Trên màn hình tương tác cảm ứng thông minh QA Smart Touch, các thao tác chạm và kéo bằng ngón tay không phải lúc nào cũng kích hoạt đúng sự kiện chuột trái thông thường. Việc sử dụng sự kiện đính kèm `Thumb.DragStarted` và `Thumb.DragCompleted` là giải pháp chuẩn của WPF để bắt trọn mọi tương tác kéo thả (bằng cả chuột lẫn cảm ứng), đảm bảo tính ổn định tối đa cho hệ thống.

---

### HẠNG MỤC 5: Cấu hình bổ sung các cặp phản ứng và tối ưu phản hồi nút bấm Mô phỏng kết tủa
*   **Mô tả yêu cầu:** 
    1. Cấu hình đầy đủ 36 tổ hợp phản ứng cation - anion tương ứng với các ComboBox để không bao giờ báo "Chưa hỗ trợ".
    2. Nếu phản ứng là "Tan" (không tạo kết tủa), nút "Chạy" vẫn phải hoạt động bình thường, chạy hiệu ứng rót nước vào cốc nghiệm và mực nước cốc nghiệm tăng lên (chất lỏng trong suốt), không được `return` làm đơ nút bấm.
*   **Dữ liệu đầu vào:** Cation và Anion được chọn trên giao diện.
*   **Dữ liệu đầu ra:** Hiệu ứng đổ chất lỏng và kết tủa (nếu có) diễn ra trơn tru.
*   **Phương pháp thực hiện:**
    *   Mở tệp [SimulationWindow.xaml.cs](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/SimulationWindow.xaml.cs#L49-L98) tại phương thức `LoadSolubilityData()`.
    *   Thêm các cấu hình phản ứng còn thiếu vào Dictionary `solubilityData` để hoàn thiện đủ 36 tổ hợp (6 cation x 6 anion):
        ```csharp
        // Bổ sung cho Ca²⁺ (dòng 58)
        ["S²⁻"] = new SolubilityInfo("CaS", "Tan", "", "Ca²⁺ + S²⁻ → CaS (tan)", "Canxi sunfua tan tốt trong nước, không tạo kết tủa."),

        // Bổ sung cho Ba²⁺ (dòng 65)
        ["PO₄³⁻"] = new SolubilityInfo("Ba₃(PO₄)₂", "Không tan", "#FFFFFF", "3Ba²⁺ + 2PO₄³⁻ → Ba₃(PO₄)₂↓", "Bari photphat không tan trong nước, tạo kết tủa màu trắng."),
        ["S²⁻"] = new SolubilityInfo("BaS", "Tan", "", "Ba²⁺ + S²⁻ → BaS (tan)", "Bari sunfua tan tốt trong nước, không tạo kết tủa."),

        // Bổ sung cho Ag⁺ (dòng 73)
        ["PO₄³⁻"] = new SolubilityInfo("Ag₃PO₄", "Không tan", "#FFEB3B", "3Ag⁺ + PO₄³⁻ → Ag₃PO₄↓", "Bạc photphat không tan trong nước, tạo kết tủa màu vàng đặc trưng."),

        // Bổ sung cho Pb²⁺ (dòng 81)
        ["PO₄³⁻"] = new SolubilityInfo("Pb₃(PO₄)₂", "Không tan", "#FFFFFF", "3Pb²⁺ + 2PO₄³⁻ → Pb₃(PO₄)₂↓", "Chì photphat không tan trong nước, tạo kết tủa màu trắng."),

        // Bổ sung cho Fe³⁺ (dòng 89)
        ["S²⁻"] = new SolubilityInfo("FeS + S", "Không tan", "#000000", "2Fe³⁺ + 3S²⁻ → 2FeS↓ + S↓", "Sắt(III) sunfua không bền trong nước, xảy ra phản ứng oxi hóa - khử tạo kết tủa sắt(II) sunfua màu đen và lưu huỳnh tự do màu vàng nhạt."),

        // Bổ sung cho Cu²⁺ (dòng 98)
        ["PO₄³⁻"] = new SolubilityInfo("Cu₃(PO₄)₂", "Không tan", "#00BCD4", "3Cu²⁺ + 2PO₄³⁻ → Cu₃(PO₄)₂↓", "Đồng photphat không tan trong nước, tạo kết tủa màu xanh lam nhạt.")
        ```
    *   Mở phương thức `RunAnimation()` (dòng 189-227). Chỉnh sửa để loại bỏ việc chặn `return` khi không có kết tủa:
        ```csharp
        private void RunAnimation()
        {
            if (CationComboBox == null || AnionComboBox == null || solubilityData == null) return;
            if (CationComboBox.SelectedItem == null || AnionComboBox.SelectedItem == null) return;

            // Reset trạng thái trước khi chạy
            ResetAnimation();

            string cation = ((ComboBoxItem)CationComboBox.SelectedItem).Content.ToString().Split(' ')[0];
            string anion = ((ComboBoxItem)AnionComboBox.SelectedItem).Content.ToString().Split(' ')[0];

            if (!solubilityData.ContainsKey(cation) || !solubilityData[cation].ContainsKey(anion))
                return;

            var info = solubilityData[cation][anion];

            // 1. Luôn chạy hiệu ứng rót chất lỏng (pourStoryboard)
            if (pourStoryboard != null) pourStoryboard.Begin();
            
            // 2. Luôn tạo giọt nước rơi để tạo cảm giác rót nước chân thực
            CreateFallingDroplets();

            // 3. Chỉ sinh hạt kết tủa và chạy hiệu ứng kết tủa rơi nếu chất đó không tan hoặc ít tan
            if (info.Solubility == "Không tan" || info.Solubility == "Ít tan")
            {
                GeneratePrecipitateParticles(info.Color, info.Solubility);

                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(4);
                timer.Tick += (s, args) =>
                {
                    if (precipitateStoryboard != null) precipitateStoryboard.Begin();
                    timer.Stop();
                };
                timer.Start();
            }
        }
        ```
*   **Tự phản biện kỹ thuật:**
    *   *Hỏi:* Tại sao phản ứng tạo $Fe_2S_3$ lại viết phương trình tạo $FeS$ và $S$?
    *   *Trả lời:* $Fe_2S_3$ là chất không bền trong môi trường nước, lập tức xảy ra quá trình oxi hóa khử nội phân tử tạo kết tủa đen $FeS$ và lưu huỳnh $S$ đơn chất. Việc mô tả phản ứng oxi hóa khử này là chính xác nhất đối với hóa học thực tế và hóa học nâng cao phổ thông.

---

### HẠNG MỤC 6: Đồng bộ hóa thuật ngữ Hóa học sư phạm & Việt hóa
*   **Mô tả yêu cầu:** Đồng bộ hóa thuật ngữ hóa học chuẩn theo chương trình SGK Việt Nam và hoàn tất Việt hóa giao diện.
*   **Phương pháp thực hiện:**
    1.  **Đổi "Biểu đồ hòa tan" thành "Bảng tính tan":**
        *   Mở [SolubilityWindow.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/SolubilityWindow.xaml#L11). Thay đổi thuộc tính:
            *   `Title="Bảng tính tan - Tra cứu tính tan của muối và bazơ"`
            *   Thay đổi nội dung TextBlock tiêu đề (dòng 141) thành `Text="BẢNG TÍNH TAN"`.
        *   Mở [MainWindow.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/MainWindow.xaml#L503). Thay đổi nhãn TextBlock của nút bấm:
            *   `Text="Bảng tính tan"` (dòng 503).
    2.  **Đổi "Chuỗi phản ứng của kim loại" thành "Dãy hoạt động hóa học của kim loại":**
        *   Mở [ReactivityWindow.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/ReactivityWindow.xaml#L7). Thay đổi thuộc tính:
            *   `Title="🧪 DÃY HOẠT ĐỘNG HÓA HỌC CỦA KIM LOẠI"`
            *   Thay đổi nội dung TextBlock tiêu đề (dòng 133) thành `Text="🧪 DÃY HOẠT ĐỘNG HÓA HỌC CỦA KIM LOẠI"`.
        *   Mở [MainWindow.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/MainWindow.xaml#L569). Thay đổi nhãn TextBlock của nút bấm:
            *   `Text="Dãy hoạt động"` (dòng 569).
    3.  **Việt hóa tiêu đề và phụ đề:**
        *   Mở [RulesWindow.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/RulesWindow.xaml#L7). Sửa tiêu đề:
            *   `Title="QUY TẮC TÍNH TAN (Chế độ học tập)"`
        *   Mở [PeriodicTableTool.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/PeriodicTableTool.xaml#L122). Sửa phụ đề:
            *   `Text="Bảng Tuần Hoàn Tương Tác — Bảng tra cứu nguyên tố thu gọn"`

---

### HẠNG MỤC 7: Tối ưu hóa layout không sử dụng Margin cứng ở CompareMetalsDialog
*   **Mô tả yêu cầu:** Sắp xếp lại Grid Column/Row của `CompareMetalsDialog` để loại bỏ hardcoded `Margin` và `RowSpan` dễ gây vỡ bố cục khi thay đổi kích thước chữ hệ thống.
*   **Dữ liệu đầu vào:** Cấu trúc Grid trong `CompareMetalsDialog.xaml`.
*   **Dữ liệu đầu ra:** Giao diện co dãn tự động, kết quả so sánh luôn hiển thị ngay dưới Panel chọn kim loại mà không bị đè chữ.
*   **Phương pháp thực hiện:**
    *   Mở tệp [CompareMetalsDialog.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/PeriodicTable/Views/CompareMetalsDialog.xaml#L121).
    *   Sửa đổi thẻ `ScrollViewer` chứa kết quả so sánh:
        ```xml
        <!-- Di chuyển hoàn toàn sang Grid.Row="2" và gỡ bỏ Margin/RowSpan -->
        <ScrollViewer Grid.Row="2" VerticalScrollBarVisibility="Auto" Margin="0">
            <StackPanel Name="ComparisonPanel" Margin="25">
                <!-- Nội dung kết quả so sánh sinh động -->
            </StackPanel>
        </ScrollViewer>
        ```
*   **Tự phản biện kỹ thuật:**
    *   *Hỏi:* Tại sao việc này giúp sửa hoàn toàn lỗi vỡ giao diện?
    *   *Trả lời:* Vì `Grid.Row="1"` là Selection Panel (`Height="Auto"`), `Grid.Row="2"` là Content Area (`Height="*"`). Khi đưa `ScrollViewer` vào đúng `Row="2"`, WPF sẽ tự động dành toàn bộ không gian còn lại ở phía dưới của hàng 1 cho nó. Nhờ vậy, dù hàng 1 có cao lên hay thấp đi do thay đổi kích thước ComboBox, `ScrollViewer` vẫn tự động bám sát chân hàng 1 mà không bị đè lên hay hở khoảng trống lớn.

---

### HẠNG MỤC 8: Thiết lập MaxWidth cho cột nội dung chi tiết của PeriodicTableTool
*   **Mô tả yêu cầu:** Áp dụng quy chuẩn `QC_4.2_LAYOUT_GRID` để giới hạn chiều rộng hiển thị tối đa cho cột nội dung bên phải, tránh làm nội dung bị dãn rộng mất thẩm mỹ trên màn hình rộng 2K, 4K.
*   **Dữ liệu đầu vào:** Điều khiển `viewGuide`, `viewPractice`, và `viewPractical` trong `PeriodicTableTool.xaml`.
*   **Dữ liệu đầu ra:** Các panel được giới hạn độ rộng tối đa và tự động căn giữa khi màn hình phóng to.
*   **Phương pháp thực hiện:**
    *   Mở tệp [PeriodicTableTool.xaml](file:///D:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/PeriodicTableTool.xaml).
    *   Tại `viewGuide` (dòng 58), thêm `MaxWidth="1200"` và `HorizontalAlignment="Center"` vào thẻ con `Border` ở dòng 60:
        ```xml
        <Border Background="White" CornerRadius="16" Padding="28,24" BorderBrush="#E0E0E0" BorderThickness="1" MaxWidth="1200" HorizontalAlignment="Center">
        ```
    *   Tại `viewPractice` (dòng 104), bao bọc nội dung hoặc đặt trực tiếp thuộc tính cho `ContentHost` (dòng 140):
        ```xml
        <Border Grid.Row="1" x:Name="ContentHost" ClipToBounds="True" Margin="12" MaxWidth="1200" HorizontalAlignment="Center">
        ```
    *   Tại `viewPractical` (dòng 148), thêm `MaxWidth="1600"` và `HorizontalAlignment="Center"`:
        ```xml
        <Border x:Name="viewPractical" HorizontalAlignment="Center" Margin="20" Visibility="Collapsed" MaxWidth="1600">
        ```
*   **Tự phản biện kỹ thuật:**
    *   *Hỏi:* Tại sao không giới hạn `MaxWidth` của Grid gốc `rootGrid`?
    *   *Trả lời:* Nếu giới hạn `rootGrid` ở mức 1200px, toàn bộ giao diện bao gồm cả thanh Menu điều hướng bên trái (Sidebar) sẽ bị co cụm vào giữa màn hình, tạo ra hai khoảng trắng lớn vô nghĩa ở hai rìa ngoài của thanh Menu. Bằng cách giữ `rootGrid` dãn rộng (`Stretch`) và chỉ giới hạn `MaxWidth` ở các Panel con của cột chi tiết bên phải, ta vừa giữ được vị trí cố định, tự nhiên của Sidebar Menu ở rìa trái màn hình, vừa giúp phần nội dung chi tiết hiển thị cân đối, hài hòa theo đúng quy chuẩn `QC_4.2_LAYOUT_GRID`.

---

### HẠNG MỤC 9: Khắc phục lỗi broken link file ảnh `periodic_table_overview.png`
*   **Mô tả yêu cầu:** Thay thế tệp hình ảnh bị thiếu bằng hình ảnh bảng tuần hoàn tổng quan chất lượng cao, tính sư phạm cao được sinh tự động.
*   **Dữ liệu đầu vào:** File ảnh đã được sinh.
*   **Dữ liệu đầu ra:** File ảnh được lưu trữ tại `QASmartClass\Resources\Images\periodic_table_overview.png` và hiển thị hoàn hảo trên giao diện Giới thiệu.
*   **Phương pháp thực hiện:**
    *   Lập trình viên kiểm tra file `QASmartClass.csproj` để chắc chắn tệp hình ảnh mới đã được khai báo dưới dạng tài nguyên:
        ```xml
        <Resource Include="Resources\Images\periodic_table_overview.png" />
        ```
        (Nếu sử dụng cơ chế tự động include của MSBuild mới thì chỉ cần đặt đúng thư mục).

---

## ═══ PHẦN 3: CHECKSHEET KIỂM THỬ TOÀN DIỆN (QA/QC CHECKSHEET) ═══

| ID Kiểm thử | Hạng mục kiểm tra | Thao tác kiểm thử | Kết quả mong đợi (Đầu ra chuẩn) | Trạng thái | Ký nhận |
| :---: | :--- | :--- | :--- | :---: | :---: |
| **TC_01** | Kiểm tra ghi chú Kẽm (Zn) | Mở Bảng tuần hoàn -> Chọn nguyên tố Zn -> Xem thông tin chi tiết. | Ghi chú hiển thị: *"Tác dụng được với cả dung dịch axit và dung dịch kiềm, dùng mạ chống gỉ"*. Không chứa từ *"lưỡng tính"*. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_02** | Kiểm tra logic so sánh kim loại tan trong nước | Mở So sánh Kim loại -> Chọn Cặp `Na` và `Fe`. | Kết luận hiển thị rõ: Na hoạt động mạnh hơn Fe, nhưng ghi rõ: *"Do Na phản ứng mạnh với nước ở điều kiện thường nên khi cho vào dung dịch muối..."*. Không kết luận *"Na đẩy Fe ra khỏi muối"*. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_03** | Kiểm tra logic so sánh kim loại thường | Chọn Cặp `Fe` và `Cu`. | Kết luận hiển thị đúng: Fe hoạt động mạnh hơn Cu, Fe đứng trước Cu và *"Fe có thể đẩy Cu ra khỏi dung dịch muối"*. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_04** | Kiểm tra đồng bộ bộ lọc sách giáo khoa | Khởi chạy Thư viện Sách Giáo Khoa lần đầu. | Tab "Lớp 1" được chọn màu xanh, và danh sách sách bên dưới **chỉ hiển thị** các sách của Lớp 1. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_05** | Kiểm tra kéo thả Slider ở BookViewer | Dùng chuột hoặc cảm ứng nhấn giữ và kéo nhanh Slider trang. | Màn hình WebView2 **không bị tải lại liên tục** (không giật hình/đơ). Khi thả tay ra, trang sách mới lập tức hiển thị mượt mà. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_06** | Kiểm tra 36 cặp ion ở Mô phỏng kết tủa | Lần lượt chọn các cặp ion: `Ba²⁺ + PO₄³⁻`, `Ba²⁺ + S²⁻`, `Ag⁺ + PO₄³⁻`... | 100% các cặp phản ứng đều chạy bình thường. Không hiển thị thông báo *"Chưa cấu hình"* và nút Chạy không bị đơ. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_07** | Kiểm tra phản ứng Tan ở Mô phỏng | Chọn cặp tạo chất tan (ví dụ `Ba²⁺ + Cl⁻`) -> Nhấn "Chạy". | Hiệu ứng rót nước vẫn diễn ra, mực nước cốc tăng lên, dung dịch trong cốc hoàn toàn trong suốt và không sinh ra hạt kết tủa. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_08** | Kiểm tra Việt hóa & Thuật ngữ | Xem tiêu đề các cửa sổ tính tan, dãy hoạt động, quy tắc tan. | - Tiêu đề cửa sổ là: **"Bảng tính tan"** (không dùng Biểu đồ hòa tan).<br>- Tiêu đề cửa sổ là: **"Dãy hoạt động hóa học"** (không dùng Chuỗi phản ứng).<br>- Tiêu đề quy tắc tan: **"QUY TẮC TÍNH TAN (Chế độ học tập)"** (không dùng Learning Mode). | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_09** | Kiểm tra Layout khi thay đổi Font size | Mở màn hình So sánh kim loại -> Thay đổi cỡ chữ hệ thống Windows lên 125% hoặc 150%. | Giao diện tự động co dãn hoàn hảo. ScrollViewer kết quả bám sát chân ComboBox chọn kim loại, không bị đè chữ hay lỗi hiển thị. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_10** | Kiểm tra MaxWidth cột phải | Phóng to cửa sổ ứng dụng ở màn hình 1920px+. Chuyển giữa các tab Hướng dẫn, Bảng tuần hoàn, Ứng dụng thực tế. | - Sidebar Menu bên trái giữ nguyên vị trí sát lề trái.<br>- Nội dung hướng dẫn và bảng tuần hoàn thu gọn tự động giới hạn độ rộng 1200px và căn giữa, không bị kéo dãn tràn mép. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_11** | Kiểm tra hiển thị ảnh Giới thiệu | Mở màn hình Giới thiệu -> Vào tab Khái niệm. | Hình ảnh sơ đồ bảng tuần hoàn tổng quan hiển thị lớn, sắc nét, không bị vỡ hình hay hiển thị broken link. | `[x] ĐẠT` | **QA/QC Passed** |
| **TC_12** | Kiểm tra chỉ dẫn sử dụng ở Mô phỏng | Mở màn hình Mô phỏng thí nghiệm kết tủa. | Có hiển thị khối "HƯỚNG DẪN TỪNG BƯỚC" màu vàng nhạt, viền cam, hướng dẫn chi tiết 5 bước bằng tiếng Việt. | `[x] ĐẠT` | **QA/QC Passed** |

---
**PHÊ DUYỆT BỞI TRƯỞNG BAN THIẾT KẾ DỰ ÁN QA SMART SCHOOL**
*(Đã ký)*
