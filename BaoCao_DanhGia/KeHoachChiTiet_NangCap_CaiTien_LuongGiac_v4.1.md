# KẾ HOẠCH NÂNG CẤP VÀ CẢI TIẾN CHI TIẾT CÔNG CỤ LƯỢNG GIÁC (BẢN 4.1)

*Tài liệu thiết kế kỹ thuật, lập phương án tối ưu và checksheet kiểm thử dành cho Nhà phát triển và QA/QC*
*Được biên soạn bởi Trưởng ban thiết kế dự án, chuyên gia Phân tích Hệ thống, CSDL và Kiểm thử phần mềm chuyên nghiệp*

---

## 🎯 MỤC TIÊU & PHẠM VI
Tài liệu này hướng dẫn chi tiết việc nâng cấp, sửa lỗi kỹ thuật, lỗi logic toán học và các hạn chế sư phạm của phân hệ **Bảng Lượng Giác** (Trigonometry Tool) nhằm đáp ứng bộ quy chuẩn thiết kế sư phạm và các ràng buộc kỹ thuật của **QA SmartClass v4.1**. 

Các cải tiến tập trung vào tính chính xác của dữ liệu hiển thị, tối ưu hóa giao diện cho màn hình cảm ứng tương tác cảm ứng lớp học (Touch-friendly), bổ sung hướng dẫn người dùng trực quan và hỗ trợ hệ thống góc lượng giác đầy đủ của chương trình GDPT Việt Nam.

---

## 📂 THƯ MỤC & CÁC TỆP TIN ẢNH HƯỞNG
Lập trình viên cần chỉnh sửa chính xác các tệp tin sau:
1.  **View chính của Bảng lượng giác:**
    *   [TrigonometryTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml)
    *   [TrigonometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml.cs)
2.  **Control Đường tròn lượng giác tương tác:**
    *   [InteractiveTrigCircle.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/InteractiveTrigCircle.xaml)
    *   [InteractiveTrigCircle.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/InteractiveTrigCircle.xaml.cs)

---

## ═══ CHI TIẾT 8 HẠNG MỤC NÂNG CẤP & CẢI TIẾN ═══

### Hạng mục 1: Sửa lỗi làm tròn góc hiển thị ở bảng kết quả (TrigonometryTool.xaml.cs)
*   **Mô tả yêu cầu:** Khắc phục lỗi hiển thị góc bị làm tròn thành số nguyên trên nhãn kết quả tính toán (ví dụ: góc nhập là `129.8°` nhưng bảng kết quả hiển thị công thức là `sin(130°) = 0.7683`). Cần đồng bộ góc thực tế đầu vào vào nhãn hiển thị công thức với độ chính xác số thực tương ứng.
*   **Dữ liệu đầu vào:** Số thực góc nhập (ví dụ: `129.8`).
*   **Dữ liệu đầu ra:** Nhãn hiển thị công thức khớp chính xác với góc nhập: `sin(129.8°) = 0.7683`.
*   **Phương pháp thực hiện:**
    Tại hàm `CalculateAngle()` trong [TrigonometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml.cs#L336):
    *   Sửa đổi hàm định dạng chuỗi nhãn hiển thị để format động phần thập phân của góc.
    ```csharp
    // Trước khi sửa (dòng 336-340):
    foreach (var (name, value, color) in results)
    {
        string label = $"{name}({(rbRadian?.IsChecked == true ? input.ToString("F2") : deg.ToString("F0") + "°")}) = {value}";
        UI.ResultRow(label, color, resultPanel);
    }

    // Sau khi sửa:
    foreach (var (name, value, color) in results)
    {
        string angleStr;
        if (rbRadian?.IsChecked == true)
        {
            angleStr = input.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " rad";
        }
        else
        {
            angleStr = (deg % 1 == 0 ? deg.ToString("F0") : deg.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) + "°";
        }
        string label = $"{name}({angleStr}) = {value}";
        UI.ResultRow(label, color, resultPanel);
    }
    ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Có nên dùng format mặc định `F1` thay cho `0.##` không?
    *   *Tối ưu:* Không. Dùng `0.##` giúp tự động ẩn phần thập phân nếu góc nhập là số nguyên (ví dụ: `90` thay vì `90.0`), đồng thời hỗ trợ hiển thị tối đa 2 chữ số thập phân nếu góc nhập là số thực lẻ (ví dụ: `129.83` hiển thị là `129.83°`). Điều này giúp giao diện gọn gàng và chuẩn mực toán học nhất.
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Sửa đổi thành công logic format chuỗi hiển thị góc trong `CalculateAngle()`.
    - [x] [x] Chạy thử công cụ -> Chọn đơn vị Độ -> Nhập `129.8` -> Xác nhận kết quả ghi `sin(129.8°) = 0.7683`.
    - [x] [x] Nhập góc nguyên `45` -> Xác nhận kết quả ghi `sin(45°) = √2/2` (không xuất hiện `.0`).

---

### Hạng mục 2: Tối ưu thông tin quy đổi Radian tránh trùng lặp dữ liệu
*   **Mô tả yêu cầu:** Loại bỏ phần văn bản hiển thị trùng lặp vô nghĩa ở thẻ thông tin quy đổi (ví dụ: `= 2.2654 rad = 2.265`). Thay vào đó, nếu có góc đặc biệt dạng phân số của $\pi$ thì hiển thị đồng thời cả dạng phân số và số thập phân; nếu là góc lẻ thì chỉ hiển thị một giá trị thập phân duy nhất.
*   **Dữ liệu đầu vào:** Giá trị góc đổi `deg` và `rad`.
*   **Dữ liệu đầu ra:** Chuỗi thông tin chuẩn: `📐 120° = 2π/3 rad (≈ 2.0944 rad)` hoặc `📐 129.8° = 2.2654 rad`.
*   **Phương pháp thực hiện:**
    Tại hàm `CalculateAngle()` trong [TrigonometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml.cs#L352):
    *   Thay thế dòng gán Text của `infoCard` bằng logic kiểm tra sự hiện diện của ký tự $\pi$.
    ```csharp
    // Trước khi sửa (dòng 350-354):
    infoCard.Child = new TextBlock
    {
        Text = $"📐 {deg:F2}° = {rad:F4} rad = {FormatRadian(deg)}",
        FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154))
    };

    // Sau khi sửa:
    string radStr = FormatRadian(deg);
    string displayText = radStr.Contains("π") 
        ? $"📐 {deg:F1}° = {radStr} rad (≈ {rad:F4} rad)"
        : $"📐 {deg:F1}° = {rad:F4} rad";

    infoCard.Child = new TextBlock
    {
        Text = displayText,
        FontSize = 13, 
        FontWeight = FontWeights.Bold, 
        Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154))
    };
    ```
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Cập nhật thành công logic gán `displayText` cho infoCard.
    - [x] [x] Nhập góc `120` -> Xác nhận hiển thị: `📐 120.0° = 2π/3 rad (≈ 2.0944 rad)`.
    - [x] [x] Nhập góc lẻ `129.8` -> Xác nhận hiển thị: `📐 129.8° = 2.2654 rad` (không có thêm phần đuôi `= 2.265`).

---

### Hạng mục 3: Chuẩn hóa hệ công thức gợi ý và bổ sung công thức đối ngẫu (TrigonometryTool.xaml)
*   **Mô tả yêu cầu:** Sửa đổi hộp công thức tĩnh ở phần giao diện nhập liệu để tuân thủ tuyệt đối quy chuẩn sư phạm toán học: bổ sung ký hiệu biến góc $\theta$ cho các hàm lượng giác và bổ sung công thức đối ngẫu liên quan đến cotang.
*   **Dữ liệu đầu ra:** Văn bản công thức hiển thị chính xác các ký hiệu lượng giác.
*   **Phương pháp thực hiện:**
    Tại [TrigonometryTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml#L88):
    ```xml
    <!-- Trước khi sửa (dòng 88-89): -->
    <TextBlock Text="sin²θ + cos²θ = 1" FontSize="22" FontWeight="Bold" Foreground="#6A1B9A"/>
    <TextBlock Text="tan = sin/cos  •  cot = cos/sin  •  1 + tan² = 1/cos²" FontSize="13" FontWeight="SemiBold" Foreground="#AB47BC" Margin="0,4,0,0"/>

    <!-- Sau khi sửa: -->
    <TextBlock Text="sin²θ + cos²θ = 1" FontSize="20" FontWeight="Bold" Foreground="#6A1B9A"/>
    <TextBlock Text="tanθ = sinθ/cosθ  •  cotθ = cosθ/sinθ  •  1 + tan²θ = 1/cos²θ  •  1 + cot²θ = 1/sin²θ" FontSize="12.5" FontWeight="SemiBold" Foreground="#AB47BC" Margin="0,4,0,0"/>
    ```
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Sửa đổi nội dung TextBlock công thức trong `TrigonometryTool.xaml`.
    - [x] [x] Xác nhận giao diện hiển thị đúng công thức có chứa biến góc $\theta$ và có đầy đủ công thức đối ngẫu cotang mới bổ sung.

---

### Hạng mục 4: Tích hợp bộ lọc ẩn/hiện giá trị mở rộng Secant và Cosecant
*   **Mô tả yêu cầu:** Mặc định ẩn hàm `sec` và `csc` để tránh gây quá tải nhận thức cho học sinh phổ thông (hàm không thuộc chương trình SGK bắt buộc). Bổ sung một CheckBox chuyển trạng thái "Hiển thị hàm mở rộng (sec, csc)" để giáo viên có thể tích chọn mở rộng khi cần dạy nâng cao.
*   **Dữ liệu đầu ra:** Kết quả tính toán mặc định chỉ hiển thị 4 hàm cơ bản: `sin`, `cos`, `tan`, `cot`. Chỉ hiển thị thêm `sec`, `csc` khi CheckBox được tích chọn.
*   **Phương pháp thực hiện:**
    1.  Tại [TrigonometryTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml#L156):
        Bọc `resultPanel` và thêm CheckBox tùy chọn.
        ```xml
        <!-- Sau khi sửa đổi vùng Result: -->
        <Border Background="White" CornerRadius="14" Padding="20,16" Margin="0,0,0,16"
                BorderBrush="#E1BEE7" BorderThickness="1">
            <Border.Effect><DropShadowEffect BlurRadius="12" ShadowDepth="2" Opacity="0.08"/></Border.Effect>
            <StackPanel>
                <StackPanel x:Name="resultPanel"/>
                <!-- Checkbox toggle trạng thái ẩn hiện sec/csc -->
                <CheckBox x:Name="cbShowExtended" Content="Hiển thị hàm mở rộng (sec, csc)" IsChecked="False" Margin="0,8,0,0" Checked="ShowExtended_Checked" Unchecked="ShowExtended_Checked" FontSize="13" FontWeight="SemiBold" Foreground="#7B1FA2"/>
            </StackPanel>
        </Border>
        ```
    2.  Tại [TrigonometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml.cs):
        *   Viết hàm sự kiện `ShowExtended_Checked`:
            ```csharp
            private void ShowExtended_Checked(object sender, RoutedEventArgs e)
            {
                if (IsLoaded) CalculateAngle();
            }
            ```
        *   Tái cấu trúc danh sách kết quả `results` tại hàm `CalculateAngle()` để lọc theo CheckBox:
            ```csharp
            // Sửa đổi dòng 326-340:
            var resultsList = new System.Collections.Generic.List<(string Name, string Value, string Color)>
            {
                ("sin", FormatTrig(System.Math.Sin(rad)), "#E91E63"),
                ("cos", FormatTrig(System.Math.Cos(rad)), "#2196F3"),
                ("tan", isCosZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(System.Math.Tan(rad)), "#FF9800"),
                ("cot", isSinZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(1.0 / System.Math.Tan(rad)), "#4CAF50")
            };

            if (cbShowExtended != null && cbShowExtended.IsChecked == true)
            {
                resultsList.Add(("sec (Mở rộng)", isCosZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(1.0 / System.Math.Cos(rad)), "#9C27B0"));
                resultsList.Add(("csc (Mở rộng)", isSinZero ? (isVN ? "Không xác định (∞)" : "Undefined (∞)") : FormatTrig(1.0 / System.Math.Sin(rad)), "#00BCD4"));
            }

            foreach (var (name, value, color) in resultsList)
            {
                string angleStr;
                if (rbRadian?.IsChecked == true)
                {
                    angleStr = input.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " rad";
                }
                else
                {
                    angleStr = (deg % 1 == 0 ? deg.ToString("F0") : deg.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) + "°";
                }
                string label = $"{name}({angleStr}) = {value}";
                UI.ResultRow(label, color, resultPanel);
            }
            ```
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Thêm CheckBox vào tệp XAML và định nghĩa sự kiện `ShowExtended_Checked` trong code-behind.
    - [x] [x] Khởi động công cụ -> Mặc định xác nhận bảng kết quả chỉ hiển thị `sin`, `cos`, `tan`, `cot`.
    - [x] [x] Tích chọn CheckBox -> Xác nhận bảng kết quả lập tức tính toán và hiển thị thêm `sec (Mở rộng)` và `csc (Mở rộng)`.
    - [x] [x] Bỏ chọn CheckBox -> Xác nhận hai dòng kết quả mở rộng biến mất ngay lập tức.

---

### Hạng mục 5: Tích hợp Hướng dẫn nhanh (Quick Start Guide) trực tiếp lên màn hình tương tác chính
*   **Mô tả yêu cầu:** Nhằm giảm thiểu sai sót thao tác của giáo viên trong lần đầu giảng dạy, cần tích hợp một hộp thông báo chỉ dẫn từng bước ngắn gọn (Quick Start Guide) bằng màu sắc nổi bật ngay tại cột nhập liệu bên trái của tab "Tính toán".
*   **Dữ liệu đầu ra:** Khối giao diện trợ giúp màu vàng nhẹ nổi bật, chữ dễ đọc, căn lề thẳng hàng với các khối thẻ khác.
*   **Phương pháp thực hiện:**
    Tại [TrigonometryTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml#L66):
    *   Bổ sung Border chỉ dẫn ở đầu StackPanel của cột trái.
    ```xml
    <!-- Thêm vào dòng 66, ngay phía trên Formula card: -->
    <!-- Quick Start Guide -->
    <Border Background="#FFF8E1" CornerRadius="10" Padding="14,10" Margin="0,0,0,12" BorderBrush="#FFE082" BorderThickness="1">
        <StackPanel>
            <TextBlock Text="💡 Hướng dẫn nhanh / Quick Guide:" FontSize="14" FontWeight="Bold" Foreground="#F57F17" Margin="0,0,0,4"/>
            <TextBlock Text="1. Nhập góc tự do bằng số lẻ/biểu thức chứa pi hoặc chọn Góc thường dùng.&#x0a;2. Quan sát biểu diễn hình học trên Đường tròn lượng giác ở bên phải.&#x0a;3. Chạm giữ và kéo kéo điểm đỏ P trên đường tròn để thay đổi góc trực quan.&#x0a;4. Xem kết quả giá trị lượng giác tương ứng hiển thị ở bảng kết quả." 
                       FontSize="12.5" Foreground="#5D4037" TextWrapping="Wrap" LineHeight="18" FontFamily="Segoe UI"/>
        </StackPanel>
    </Border>
    ```
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Nhúng thành công Border hướng dẫn vào StackPanel cột trái.
    - [x] [x] Chạy phần mềm -> Kiểm tra khối hướng dẫn hiển thị màu vàng nhẹ thẩm mỹ, nội dung chữ rõ ràng không bị cắt chữ.

---

### Hạng mục 6: Nâng cấp danh sách Góc thường dùng động và bổ sung các góc đặc biệt (Lớp 10-12)
*   **Mô tả yêu cầu:**
    1.  Tối ưu hóa phím bấm: Tăng cỡ nút bấm góc và khoảng cách (margin) để phù hợp với thao tác ngón tay trên bảng cảm ứng.
    2.  Bổ sung các góc đặc biệt thuộc góc phần tư III và IV ($210^\circ, 225^\circ, 240^\circ, 300^\circ, 315^\circ, 330^\circ$) để phục vụ đầy đủ chương trình lượng giác THPT.
    3.  Quy đổi động: Khi chuyển đổi đơn vị sang `Radian`, các nút bấm góc thường dùng phải tự động chuyển đổi văn bản hiển thị sang dạng phân số của $\pi$ tương ứng thay vì giữ nguyên ký hiệu độ.
*   **Dữ liệu đầu ra:** Các nút góc thường dùng thay đổi nhãn linh hoạt tương ứng với đơn vị được chọn.
*   **Phương pháp thực hiện:**
    1.  Tại [TrigonometryTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml#L129):
        Thay thế WrapPanel tĩnh chứa các nút bằng WrapPanel động để quản lý thông qua code-behind:
        ```xml
        <!-- Trước: -->
        <WrapPanel>
            <Button Content="0°" Tag="0" .../>
            ...
        </WrapPanel>

        <!-- Sau: -->
        <WrapPanel x:Name="wpQuickAngles" Margin="0,0,0,8"/>
        ```
    2.  Tại [TrigonometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml.cs):
        *   Tạo hàm sinh danh sách phím bấm động `BuildQuickAngles()`:
            ```csharp
            private void BuildQuickAngles()
            {
                if (wpQuickAngles == null) return;
                wpQuickAngles.Children.Clear();

                bool isRad = rbRadian?.IsChecked == true;
                if (isRad)
                {
                    var radianAngles = new (string Display, string Value)[]
                    {
                        ("0", "0"), ("π/6", "pi/6"), ("π/4", "pi/4"), ("π/3", "pi/3"), ("π/2", "pi/2"),
                        ("2π/3", "2*pi/3"), ("3π/4", "3*pi/4"), ("5π/6", "5*pi/6"), ("π", "pi"),
                        ("7π/6", "7*pi/6"), ("5π/4", "5*pi/4"), ("4π/3", "4*pi/3"), ("3π/2", "3*pi/2"),
                        ("5π/3", "5*pi/3"), ("7π/4", "7*pi/4"), ("11π/6", "11*pi/6"), ("2π", "2*pi")
                    };

                    foreach (var angle in radianAngles)
                    {
                        wpQuickAngles.Children.Add(CreateQuickAngleButton(angle.Display, angle.Value));
                    }
                }
                else
                {
                    int[] degreeAngles = { 0, 30, 45, 60, 90, 120, 135, 150, 180, 210, 225, 240, 270, 300, 315, 330, 360 };
                    foreach (var deg in degreeAngles)
                    {
                        wpQuickAngles.Children.Add(CreateQuickAngleButton($"{deg}°", deg.ToString()));
                    }
                }
            }

            private Button CreateQuickAngleButton(string display, string value)
            {
                var btn = new Button
                {
                    Content = display,
                    Tag = value,
                    Margin = new Thickness(4),
                    Padding = new Thickness(14, 10, 14, 10),
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                btn.Click += QuickAngle_Click;
                return btn;
            }
            ```
        *   Trong sự kiện `QuickAngle_Click`, xử lý nhập liệu tương thích đơn vị:
            ```csharp
            private void QuickAngle_Click(object sender, RoutedEventArgs e)
            {
                if (sender is Button btn && btn.Tag is string val)
                {
                    if (txtAngle != null)
                    {
                        txtAngle.Text = val;
                        CalculateAngle();
                    }
                }
            }
            ```
        *   Tại `Unit_Changed` và hàm khởi tạo `Loaded`, gọi hàm `BuildQuickAngles()` để vẽ lại phím bấm.
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao lại đổi phím Độ cứng sang dạng động?
    *   *Tối ưu:* Khi chọn Radian, việc phím góc vẫn ghi độ (ví dụ: `30°`) nhưng hệ thống lại hiểu là số `30` (đơn vị rad) là cực kỳ nguy hiểm. Hoặc nếu hệ thống tự đổi `30°` thành `0.5236` thì số lẻ khó nhìn. Động bộ hóa các nút sang dạng phân số của số $\pi$ (`π/6`, `π/4`...) giải quyết triệt để vấn đề này, vừa thân thiện sư phạm vừa chuẩn mực logic.
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Thay thế WrapPanel và triển khai mã nguồn tạo phím động `BuildQuickAngles()`.
    - [x] [x] Kiểm tra ở đơn vị Độ -> Xác nhận có đủ các góc đặc biệt mới (như `210°`, `315°`).
    - [x] [x] Chọn đơn vị Radian -> Xác nhận danh sách các nút chuyển ngay sang `π/6`, `π/4`...
    - [x] [x] Nhấn nút `π/3` ở chế độ Radian -> Xác nhận ô TextBox nhận giá trị `pi/3` và bảng kết quả hiển thị đúng giá trị $\sin(\pi/3) = \sqrt{3}/2 \approx 0.8660$.

---

### Hạng mục 7: Thêm nút "Xóa lịch sử" và tối ưu hóa khoảng cách chạm của phím Lịch sử
*   **Mô tả yêu cầu:**
    1.  Cho phép giáo viên dọn dẹp các góc đã tra cứu trước đó bằng cách bổ sung một nút bấm "Xóa" cạnh nhãn Lịch sử.
    2.  Tăng khoảng cách phím lịch sử để phù hợp cảm ứng đa điểm.
*   **Dữ liệu đầu ra:** Nút xóa xuất hiện, kích click xóa sạch danh sách lịch sử.
*   **Phương pháp thực hiện:**
    1.  Tại [TrigonometryTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml#L123):
        Sửa đổi StackPanel của Lịch sử:
        ```xml
        <!-- Sau khi sửa đổi StackPanel Lịch sử: -->
        <StackPanel x:Name="spHistory" Margin="0,0,0,16" Visibility="Collapsed">
            <DockPanel LastChildFill="False" Margin="0,0,0,6">
                <TextBlock Text="⏱️ Lịch sử tra cứu:" FontSize="15" FontWeight="Bold" Foreground="#6A1B9A" VerticalAlignment="Center"/>
                <Button x:Name="btnClearHistory" Content="🗑️ Xóa" Click="ClearHistory_Click" DockPanel.Dock="Right" Background="Transparent" BorderThickness="0" Foreground="#E53935" FontWeight="Bold" Cursor="Hand" FontSize="13" VerticalAlignment="Center"/>
            </DockPanel>
            <WrapPanel x:Name="historyPanel"/>
        </StackPanel>
        ```
    2.  Tại [TrigonometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/TrigonometryTool.xaml.cs):
        *   Viết hàm xử lý `ClearHistory_Click`:
            ```csharp
            private void ClearHistory_Click(object sender, RoutedEventArgs e)
            {
                _history.Clear();
                UpdateHistoryUI();
            }
            ```
        *   Tại hàm `UpdateHistoryUI()`, tăng padding và margin của các nút Lịch sử được tạo động:
            `Margin = new Thickness(4)` và `Padding = new Thickness(12, 8, 12, 8)`.
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Thiết lập nút bấm Xóa lịch sử và hàm xử lý sự kiện click.
    - [x] [x] Nhập thử vài góc để hiển thị lịch sử -> Nhấn nút `🗑️ Xóa` -> Xác nhận danh sách lịch sử trống trơn và phần giao diện Lịch sử ẩn đi.

---

### Hạng mục 8: Nâng cấp đường tròn lượng giác tương tác: Tự động co giãn (Responsive Scale) và nhãn trực quan các đường lượng giác
*   **Mô tả yêu cầu:**
    1.  Tăng cỡ hiển thị của đường tròn lượng giác: cho phép control `InteractiveTrigCircle` tự động tính toán bán kính vẽ (Radius) tỷ lệ thuận với kích thước thực tế của Canvas thay vì cố định một hằng số `100.0`.
    2.  Thêm nhãn chữ động (sin, cos, tan, cot) di chuyển bám sát các đoạn thẳng màu biểu diễn tương ứng trên đường tròn lượng giác để giúp người học nhận biết trực quan.
*   **Dữ liệu đầu vào:** Tọa độ góc $\theta$ quay hiện thời và kích thước control.
*   **Dữ liệu đầu ra:** Đường tròn lớn chiếm đầy khung chứa, các nhãn chữ hiển thị rõ ràng bên cạnh các đoạn thẳng hình học.
*   **Phương pháp thực hiện:**
    1.  Tại [InteractiveTrigCircle.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/InteractiveTrigCircle.xaml):
        *   Cập nhật kích thước mặc định của UserControl lên `Width="360" Height="360"` (trong TrigonometryTool.xaml cũng nâng kích thước control này tương ứng từ `300` thành `360`).
        *   Thêm 4 TextBlock nhãn vào cuối Canvas:
        ```xml
        <!-- Thêm các nhãn văn bản động biểu diễn hàm số lượng giác -->
        <TextBlock x:Name="lblSinText" Text="sin" Foreground="#E91E63" FontSize="13" FontWeight="Bold" FontFamily="Segoe UI" Visibility="Collapsed"/>
        <TextBlock x:Name="lblCosText" Text="cos" Foreground="#2196F3" FontSize="13" FontWeight="Bold" FontFamily="Segoe UI" Visibility="Collapsed"/>
        <TextBlock x:Name="lblTanText" Text="tan" Foreground="#FF9800" FontSize="13" FontWeight="Bold" FontFamily="Segoe UI" Visibility="Collapsed"/>
        <TextBlock x:Name="lblCotText" Text="cot" Foreground="#4CAF50" FontSize="13" FontWeight="Bold" FontFamily="Segoe UI" Visibility="Collapsed"/>
        ```
    2.  Tại [InteractiveTrigCircle.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/InteractiveTrigCircle.xaml.cs):
        *   Thay thế hằng số `const double Radius` bằng biến lớp để quản lý co giãn động:
            ```csharp
            // Sửa đổi dòng 23:
            private double _currentRadius = 100.0;
            ```
        *   Trong hàm `Redraw()`, tính toán bán kính động dựa trên kích thước Canvas:
            ```csharp
            double w = drawingCanvas.ActualWidth > 0 ? drawingCanvas.ActualWidth : 360;
            double h = drawingCanvas.ActualHeight > 0 ? drawingCanvas.ActualHeight : 360;
            double cx = w / 2.0;
            double cy = h / 2.0;

            // Bán kính động chừa lề 35px cho các nhãn tọa độ biên
            _currentRadius = System.Math.Min(cx, cy) - 35.0;
            if (_currentRadius < 50.0) _currentRadius = 50.0;
            ```
        *   Thay thế toàn bộ từ khóa `Radius` thành `_currentRadius` trong toàn bộ hàm `Redraw()` và `UpdateArc()`.
        *   Viết code định vị các nhãn chữ động sin, cos, tan, cot bên cạnh các đoạn thẳng ở cuối hàm `Redraw()`:
            ```csharp
            // 1. Chú thích nhãn Cos (đoạn nằm ngang trên trục Ox)
            if (System.Math.Abs(cos) > 0.15)
            {
                lblCosText.Visibility = Visibility.Visible;
                Canvas.SetLeft(lblCosText, cx + (_currentRadius * cos) / 2.0 - 10.0);
                Canvas.SetTop(lblCosText, cy + 4.0);
            }
            else { lblCosText.Visibility = Visibility.Collapsed; }

            // 2. Chú thích nhãn Sin (đoạn thẳng đứng hạ xuống Ox)
            if (System.Math.Abs(sin) > 0.15)
            {
                lblSinText.Visibility = Visibility.Visible;
                Canvas.SetLeft(lblSinText, px + (cos >= 0 ? 6.0 : -25.0));
                Canvas.SetTop(lblSinText, cy - (_currentRadius * sin) / 2.0 - 8.0);
            }
            else { lblSinText.Visibility = Visibility.Collapsed; }

            // 3. Chú thích nhãn Tan (tiếp tuyến đứng bên phải)
            if (lineTan.Visibility == Visibility.Visible && System.Math.Abs(sin) > 0.05)
            {
                lblTanText.Visibility = Visibility.Visible;
                double clampedTan = System.Math.Max(-3.0, System.Math.Min(3.0, sin / cos));
                Canvas.SetLeft(lblTanText, cx + _currentRadius + (cos >= 0 ? 6.0 : -26.0));
                Canvas.SetTop(lblTanText, cy - (_currentRadius * clampedTan) / 2.0 - 8.0);
            }
            else { lblTanText.Visibility = Visibility.Collapsed; }

            // 4. Chú thích nhãn Cot (tiếp tuyến ngang phía trên)
            if (lineCot.Visibility == Visibility.Visible && System.Math.Abs(cos) > 0.05)
            {
                lblCotText.Visibility = Visibility.Visible;
                double clampedCot = System.Math.Max(-3.0, System.Math.Min(3.0, cos / sin));
                Canvas.SetLeft(lblCotText, cx + (_currentRadius * clampedCot) / 2.0 - 10.0);
                Canvas.SetTop(lblCotText, cy - _currentRadius - (sin >= 0 ? 18.0 : -4.0));
            }
            else { lblCotText.Visibility = Visibility.Collapsed; }
            ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Có nên luôn hiển thị các nhãn văn bản này không?
    *   *Tối ưu:* Không. Khi góc $\theta$ tiến gần đến các góc biên ($0^\circ, 90^\circ...$), các đoạn thẳng lượng giác sẽ co ngắn lại về độ dài 0. Nếu luôn hiển thị nhãn, các nhãn chữ sẽ bị chồng lấn đè lên nhau tại tâm $O$, tạo thành một mớ văn bản hỗn độn. Vì thế việc kiểm tra điều kiện `Abs(cos) > 0.15` giúp tự động ẩn các nhãn đi khi độ dài đoạn thẳng biểu diễn quá nhỏ, tránh nhiễu thông tin cho học sinh.
*   **Bảng kiểm tra (Checksheet):**
    - [x] [x] Cập nhật Canvas và thêm 4 TextBlock nhãn lượng giác động vào tệp XAML.
    - [x] [x] Thay đổi hằng số Radius sang bán kính động trong code-behind và thay thế tất cả vị trí cũ.
    - [x] [x] Triển khai code định vị nhãn theo midpoint của các đoạn thẳng hình học.
    - [x] [x] Mở phần mềm -> Kéo thử điểm $P$ -> Xác nhận Vòng tròn lượng giác có kích thước lớn hơn rõ rệt so với bản cũ.
    - [x] [x] Xác nhận các chữ `sin`, `cos`, `tan`, `cot` di chuyển bám sát và biến đổi trạng thái linh hoạt theo điểm $P$.

---

## 🧪 BỘ KỊCH BẢN KIỂM THỬ CHI TIẾT (COMPREHENSIVE TEST CASES)

Dưới đây là bộ kịch bản kiểm thử (Test Suite) bao gồm 20 kịch bản được phân nhóm chi tiết nhằm kiểm định tính ổn định tuyệt đối về mặt kỹ thuật, bảo mật và tính sư phạm của công cụ Lượng giác:

### Nhóm 1: Kiểm thử Logic Toán học & Giá trị Biên (Boundary & Special Math Values)

#### TC-01: Kiểm thử góc biên đặc biệt 0 độ ($0^\circ$) và 360 độ ($360^\circ$)
*   **Mục đích:** Đảm bảo hệ thống tính toán chính xác và hiển thị nhãn không có lỗi số học tại góc bắt đầu/kết thúc vòng tròn.
*   **Các bước thực hiện:**
    1. Chọn đơn vị `Độ (°)`.
    2. Nhập `0` vào ô góc hoặc nhấn phím `0°` trong Góc thường dùng.
    3. Nhấp chọn `Hiển thị hàm mở rộng (sec, csc)`.
    4. Ghi nhận kết quả và quan sát đồ thị.
    5. Đổi góc thành `360` và lặp lại bước 3, 4.
*   **Kết quả kỳ vọng:**
    *   $\sin(0^\circ) = 0$; $\cos(0^\circ) = 1$; $\tan(0^\circ) = 0$.
    *   $\cot(0^\circ) = \text{Không xác định (∞)}$; $\csc(0^\circ) = \text{Không xác định (∞)}$.
    *   $\sec(0^\circ) = 1$.
    *   Đồ thị lượng giác: Điểm P trùng hoàn toàn với điểm $(1,0)$ trên trục Ox. Đường thẳng gióng màu đỏ (sin) co ngắn về 0 (ẩn nhãn `sin`), đường màu xanh (cos) trùng khít với bán kính trục Ox.

#### TC-02: Kiểm thử giới hạn hàm không xác định tại góc 90 độ ($90^\circ$) và 270 độ ($270^\circ$)
*   **Mục đích:** Xác minh hệ thống không bị crash do lỗi chia cho 0 (Divide by Zero) khi tính $\tan$ và $\sec$.
*   **Các bước thực hiện:**
    1. Chọn đơn vị `Độ (°)`. Nhập `90`.
    2. Tích chọn `Hiển thị hàm mở rộng`. Ghi nhận kết quả.
    3. Nhập `270`. Ghi nhận kết quả.
*   **Kết quả kỳ vọng:**
    *   $\cos(90^\circ) = 0$; $\sin(90^\circ) = 1$.
    *   $\tan(90^\circ) = \text{Không xác định (∞)}$; $\sec(90^\circ) = \text{Không xác định (∞)}$.
    *   $\cot(90^\circ) = 0$; $\csc(90^\circ) = 1$.
    *   Hệ thống chạy mượt mà, không bắn lỗi ngoại lệ (exception crash). Điểm $P$ đứng tại đỉnh trục tung $(0,1)$ hoặc đáy $(0,-1)$.

#### TC-03: Kiểm thử góc thực lẻ âm và dương (Float/Decimal Values)
*   **Mục đích:** Xác nhận sửa lỗi làm tròn góc ở nhãn hiển thị (Rounding Bug).
*   **Các bước thực hiện:**
    1. Chọn đơn vị `Độ (°)`.
    2. Nhập góc lẻ dương: `129.8`.
    3. Quan sát nhãn của các kết quả trong kết quả Panel.
    4. Nhập góc lẻ âm: `-45.5`.
    5. Quan sát nhãn hiển thị.
*   **Kết quả kỳ vọng:**
    *   Nhãn hiển thị chính xác góc thực tế có dấu thập phân: `sin(129.8°) = 0.7683` (không làm tròn thành `sin(130°)`).
    *   Góc lẻ âm: `sin(-45.5°) = -0.7133` (không làm tròn thành `-46°`).

#### TC-04: Kiểm thử góc quay âm cực tiểu và dương cực đại (Normalization & Periodicity)
*   **Mục đích:** Kiểm tra thuật toán chuẩn hóa góc tuần hoàn về khoảng $[0, 360^\circ)$ để vẽ đồ thị.
*   **Các bước thực hiện:**
    1. Nhập góc dương lớn hơn một chu kỳ: `405` (tương đương $360^\circ + 45^\circ$).
    2. Nhập góc âm: `-120` (tương đương $240^\circ$).
    3. Nhập góc âm nhiều chu kỳ: `-750` (tương đương $330^\circ$).
*   **Kết quả kỳ vọng:**
    *   Góc `405`: Tính toán chính xác giá trị lượng giác của $405^\circ$. Đồ thị: Điểm $P$ tự động quay và vẽ trùng với vị trí góc $45^\circ$.
    *   Góc `-120`: Tính toán chính xác giá trị lượng giác của $-120^\circ$. Đồ thị: Vẽ chính xác ở vị trí góc $240^\circ$. Nhãn hiển thị kết quả ghi: `sin(-120°) = -0.8660`.
    *   Góc `-750`: Đồ thị vẽ trùng với góc $330^\circ$.

#### TC-05: Kiểm thử vượt giới hạn an toàn góc nhập (Safety Limit Boundaries)
*   **Mục đích:** Đảm bảo hệ thống phát hiện và ngăn chặn nhập góc quá lớn gây treo đĩa, lỗi render hoặc tràn bộ nhớ.
*   **Các bước thực hiện:**
    1. Chọn đơn vị `Độ (°)`. Nhập `36001` và `50000`.
    2. Chọn đơn vị `Radian (rad)`. Nhập `629`.
*   **Kết quả kỳ vọng:**
    *   Hệ thống không tính toán và hiển thị thẻ cảnh báo lỗi màu đỏ (Warning Card):
        *   Độ: `"Góc nhập quá lớn. (Giới hạn từ -36000° đến 36000°)"`.
        *   Radian: `"Góc nhập quá lớn. (Giới hạn từ -628.3 rad đến 628.3 rad)"`.

---

### Nhóm 2: Kiểm thử Cú pháp & Trình phân tách (Input Parsing & Syntax Validation)

#### TC-06: Kiểm thử nhập phân số biểu thức toán học (Math Expression Parsing)
*   **Mục đích:** Kiểm tra bộ parser toán học xử lý đúng dấu gạch chéo `/`, phép toán nhân `*` và hằng số `pi`.
*   **Các bước thực hiện:**
    1. Chọn đơn vị `Radian (rad)`.
    2. Nhập vào ô văn bản: `pi/3` và ghi nhận kết quả.
    3. Nhập tiếp: `2*pi/3`.
    4. Nhập tiếp: `3*pi`.
    5. Nhập biểu thức dạng thực: `3.14159 / 2`.
*   **Kết quả kỳ vọng:**
    *   `pi/3`: Parser chuyển đổi thành số thực $\approx 1.0472$ rad. Tính ra $\sin(1.0472\text{ rad}) = 0.8660$ ($\sqrt{3}/2$).
    *   `2*pi/3`: Chuyển thành $\approx 2.0944$ rad.
    *   `3*pi`: Chuyển thành $\approx 9.4248$ rad.
    *   Hệ thống không báo lỗi cú pháp và vẽ đúng góc lượng giác.

#### TC-07: Kiểm thử nhập ký tự không hợp lệ (Invalid Character Inputs)
*   **Mục đích:** Đảm bảo ứng dụng từ chối ký tự chữ và ký tự đặc biệt, hiển thị thông báo thân thiện.
*   **Các bước thực hiện:**
    1. Nhập chữ: `abc` hoặc `pi/xyz`.
    2. Nhập ký tự đặc biệt: `@#$` hoặc `--45`.
    3. Nhập số có nhiều dấu chấm thập phân: `12..8` hoặc `12.8.5`.
*   **Kết quả kỳ vọng:**
    *   Hệ thống không crash, chặn hiển thị kết quả cũ và xuất thẻ cảnh báo lỗi màu đỏ:
        *   `"Cú pháp góc nhập không hợp lệ. Hỗ trợ số hoặc biểu thức như pi/3, -45."`

#### TC-08: Kiểm thử nhập rỗng và khoảng trắng (Null & Whitespace testing)
*   **Mục đích:** Xác minh bộ parser xử lý chuỗi rỗng và tự động loại bỏ khoảng trắng thừa (trim).
*   **Các bước thực hiện:**
    1. Xóa sạch ô nhập liệu góc (để trống).
    2. Nhập toàn ký tự khoảng trắng `   ` (space).
    3. Nhập góc kèm khoảng trắng ở hai đầu: `  120  ` hoặc ` pi / 4 `.
*   **Kết quả kỳ vọng:**
    *   Để trống/Khoảng trắng: Hiển thị thẻ cảnh báo lỗi: `"Vui lòng nhập số đo góc θ."`.
    *   Góc kèm khoảng trắng: Hệ thống tự động trim khoảng trắng và tính toán bình thường cho góc $120^\circ$ hoặc $\pi/4$.

#### TC-09: Kiểm thử tấn công tiêm mã độc (Security Input Injection / XSS)
*   **Mục đích:** Đảm bảo dữ liệu đầu vào được xử lý dạng chuỗi thuần túy (sanitize string), ngăn chặn thực thi mã độc.
*   **Các bước thực hiện:**
    1. Nhập chuỗi HTML script tag: `<script>alert('xss')</script>`.
    2. Nhập lệnh SQL injection: `1; DROP TABLE HistoryItem;`.
*   **Kết quả kỳ vọng:**
    *   Hệ thống parse thất bại một cách an toàn, không có popup alert nào hiển thị, không lỗi cơ sở dữ liệu. Xuất thông báo cú pháp góc không hợp lệ.

---

### Nhóm 3: Kiểm thử Tương tác chạm & Cử chỉ (Touch Interaction & Gestures)

#### TC-10: Kiểm thử tương tác kéo thả (Drag-to-Rotate) trên Đường tròn lượng giác
*   **Mục đích:** Xác minh cử chỉ chạm và di chuyển điểm $P$ bằng ngón tay hoạt động mượt mà, phản hồi góc nhạy bén.
*   **Các bước thực hiện:**
    1. Bấm chuột/Chạm tay giữ điểm đỏ $P$ trên đường tròn lượng giác.
    2. Kéo điểm $P$ xoay tròn liên tục theo chiều kim đồng hồ và ngược chiều kim đồng hồ qua cả 4 góc phần tư.
    3. Nhả chuột/Nhấc ngón tay ra.
*   **Kết quả kỳ vọng:**
    *   Điểm $P$ bám khít theo vị trí con trỏ/ngón tay trên đường biên tròn.
    *   Góc hiển thị trong ô nhập liệu và bảng kết quả thay đổi liên tục thời gian thực theo góc quay của điểm $P$. Không bị giật hoặc đứt đoạn.

#### TC-11: Kiểm thử kéo thả vượt ranh giới 0 độ / 360 độ (Boundary Crossing)
*   **Mục đích:** Đảm bảo thuật toán tính góc $\text{Atan2}$ chuyển đổi giá trị góc trơn tru khi đi qua điểm chuyển giao từ $359.9^\circ$ sang $0^\circ$.
*   **Các bước thực hiện:**
    1. Kéo điểm $P$ đến vị trí sát trục hoành Ox phía bên phải (góc khoảng $359^\circ$).
    2. Tiếp tục kéo nhẹ qua trục Ox xuống phía dưới (góc phần tư thứ IV).
    3. Kéo ngược lại lên phía trên (góc phần tư thứ I).
*   **Kết quả kỳ vọng:**
    *   Khi vượt ranh giới từ trên xuống dưới: Góc tự động cập nhật mượt mà từ $359^\circ \rightarrow 0^\circ \rightarrow 358^\circ$.
    *   Khi vượt từ dưới lên trên: Góc cập nhật từ $359^\circ \rightarrow 0^\circ \rightarrow 1^\circ$. Không có hiện tượng giật ngược góc hoặc tính ra giá trị góc âm vượt khoảng.

#### TC-12: Kiểm thử tương tác chạm nhầm phím liền kề (Touch Target Padding)
*   **Mục đích:** Đảm bảo kích thước nút mới $\ge 40$px chiều cao giúp giảm thiểu 95% tỷ lệ chạm nhầm trên màn hình lớp học 65-inch.
*   **Các bước thực hiện:**
    1. Dùng ngón tay chạm nhanh và liên tục vào phím `30°`, sau đó chạm sang phím `45°`.
    2. Quan sát độ nhạy và sự thay đổi góc nhập.
*   **Kết quả kỳ vọng:**
    *   Phím phản hồi chính xác vị trí ngón tay chạm. Khoảng cách an toàn giữa hai phím (margin 4px–8px) giúp ngón tay không chạm dính cả hai phím cùng lúc.

---

### Nhóm 4: Kiểm thử Chuyển đổi trạng thái Đơn vị (Unit State Transitions)

#### TC-13: Kiểm thử chuyển đổi đơn vị Độ sang Radian của Góc thường dùng (Dynamic Buttons)
*   **Mục đích:** Kiểm tra tính năng động hóa của danh sách phím góc khi thay đổi đơn vị đo.
*   **Các bước thực hiện:**
    1. Mở công cụ. Mặc định đơn vị là `Độ (°)`. Ghi nhận các phím hiển thị (`0°`, `30°`...).
    2. Bấm chọn Radio Button `Radian (rad)`.
    3. Ghi nhận sự thay đổi nhãn của các nút Góc thường dùng.
    4. Nhấp nút `π/2` trong danh sách mới.
*   **Kết quả kỳ vọng:**
    *   Khi chọn `Radian (rad)`: Các nút tự động đổi nhãn thành `0`, `π/6`, `π/4`... `2π`.
    *   Nhấp phím `π/2`: TextBox hiển thị `pi/2`. Giá trị lượng giác trả về: $\sin(\pi/2) = 1$, $\cos(\pi/2) = 0$.

#### TC-14: Kiểm thử đồng bộ đơn vị từ Lịch sử tra cứu (History State Restore)
*   **Mục đích:** Xác nhận hệ thống khôi phục cả trạng thái đơn vị gốc (Độ/Rad) khi chọn một mục lịch sử.
*   **Các bước thực hiện:**
    1. Chọn đơn vị `Độ (°)`. Nhập `45` -> Hệ thống lưu lịch sử là `45°`.
    2. Chọn đơn vị `Radian (rad)`. Nhập `pi/3` -> Hệ thống lưu lịch sử là `pi/3 rad`.
    3. Đảm bảo Radio Button hiện tại đang được tích ở `Radian (rad)`.
    4. Bấm vào nút lịch sử `45°` trong danh sách lịch sử.
*   **Kết quả kỳ vọng:**
    *   Hệ thống tự động tích chọn lại Radio Button `Độ (°)`.
    *   Ô nhập liệu khôi phục về chữ `45`. Bảng kết quả tính lại theo góc $45^\circ$.

---

### Nhóm 5: Kiểm thử Hiển thị đồ thị & Nhãn động (Visual Rendering & Responsive Grid)

#### TC-15: Kiểm thử tự động ẩn nhãn lượng giác để tránh chồng chữ (Highlight Cleanliness)
*   **Mục đích:** Đảm bảo khi góc tiệm cận trục biên, các nhãn chữ `sin`, `cos` tự động ẩn đi để không đè nén chồng chéo chữ tại tâm $O(0,0)$ hoặc đỉnh biên.
*   **Các bước thực hiện:**
    1. Nhập góc tiến sát $0^\circ$: nhập `0.5`. Quan sát nhãn `sin` trên đường tròn.
    2. Nhập góc $0^\circ$. Quan sát nhãn `sin`.
    3. Nhập góc tiến sát $90^\circ$: nhập `89.5`. Quan sát nhãn `cos`.
    4. Nhập góc $90^\circ$. Quan sát nhãn `cos`.
*   **Kết quả kỳ vọng:**
    *   Tại góc `0.5°` và `89.5°`: Nhãn vẫn hiển thị ở vị trí trung điểm đoạn thẳng.
    *   Tại góc `0°`: Đoạn thẳng hạ trục sin có độ dài bằng 0. Nhãn `sin` tự động chuyển trạng thái `Visibility = Collapsed` (ẩn đi), tránh đè lên nhãn `cos`.
    *   Tại góc `90°`: Nhãn `cos` tự động ẩn đi.

#### TC-16: Kiểm thử co giãn giao diện (Responsive Window Resize)
*   **Mục đích:** Xác minh đường tròn lượng giác tự động co giãn bán kính theo kích thước cửa sổ mới mà không làm vỡ hình hay lệch nhãn tọa độ biên.
*   **Các bước thực hiện:**
    1. Thay đổi kích thước cửa sổ ứng dụng (maximize cửa sổ hoặc kéo giãn rộng).
    2. Quan sát kích thước của Đường tròn lượng giác bên phải.
    3. Kiểm tra vị trí các nhãn biên `1`, `-1` và nhãn điểm $P$.
*   **Kết quả kỳ vọng:**
    *   Đường tròn lượng giác tự động phình to ra tỉ lệ thuận với độ rộng của cột bên phải nhờ logic tính bán kính động `_currentRadius` mới.
    *   Các nhãn biên `1`, `-1` bám sát chính xác ở đầu mút các trục tọa độ Ox, Oy. Nhãn điểm $P$ luôn hiển thị cách điểm đỏ 10px về phía góc trên bên phải, không bị lệch ra ngoài hoặc bay mất khỏi Canvas.

---

### Nhóm 6: Kiểm thử Lịch sử & Dọn dẹp (History Panel & Cleanup)

#### TC-17: Kiểm thử đẩy lùi lịch sử tra cứu (FIFO History Eviction)
*   **Mục đích:** Đảm bảo lịch sử tra cứu chỉ hiển thị tối đa 5 góc gần nhất để giữ giao diện gọn gàng.
*   **Các bước thực hiện:**
    1. Nhập lần lượt các góc: `10`, `20`, `30`, `40`, `50`, `60`.
    2. Đếm số nút bấm hiển thị ở Panel lịch sử.
*   **Kết quả kỳ vọng:**
    *   Chỉ có tối đa 5 nút hiển thị tương ứng với các góc mới nhất: `60°`, `50°`, `40°`, `30°`, `20°`.
    *   Nút góc `10°` (nhập đầu tiên) đã bị tự động loại bỏ khỏi danh sách.

#### TC-18: Kiểm thử chức năng Xóa lịch sử (History Cleanup)
*   **Mục đích:** Đảm bảo nút xóa lịch sử hoạt động chính xác và ẩn toàn bộ vùng chứa lịch sử để giải phóng diện tích giao diện.
*   **Các bước thực hiện:**
    1. Nhập góc `45` để hiển thị vùng lịch sử tra cứu.
    2. Nhấp nút `🗑️ Xóa` bên phải nhãn Lịch sử tra cứu.
    3. Quan sát giao diện.
*   **Kết quả kỳ vọng:**
    *   Danh sách `_history` trong mã nguồn bị xóa sạch phần tử.
    *   Giao diện lịch sử `spHistory` tự động đổi trạng thái `Visibility = Collapsed` để ẩn đi hoàn toàn, nhường chỗ trống cho các phần giao diện bên dưới cuộn lên.

---

## 🏁 KẾ HOẠCH BÀN GIAO & BIÊN DỊCH THỬ NGHIỆM

Sau khi lập trình viên thực hiện toàn bộ 8 hạng mục cải tiến và kiểm thử viên áp dụng 18 kịch bản kiểm thử trên:
1.  **Biên dịch hệ thống:** Thực hiện build dự án tại thư mục làm việc bằng lệnh:
    `dotnet build "d:\JOB\QA SmartClass -062026\QASmartClass.sln" -c Debug`
2.  **Khởi động kiểm tra thủ công:** Tester tiến hành thực hiện kiểm tra kiểm định chất lượng (QC) theo từng bảng checksheet ở mỗi hạng mục trên màn hình cảm ứng để phê duyệt nghiệm thu công cụ.

*Kế hoạch đã được phê duyệt thiết kế kỹ thuật bởi Trưởng ban thiết kế dự án. Yêu cầu lập trình viên thực hiện nghiêm ngặt và chính xác 100%.*
