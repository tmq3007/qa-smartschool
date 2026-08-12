# KẾ HOẠCH NÂNG CẤP VÀ CẢI TIẾN CHI TIẾT CÔNG CỤ STEM (BẢN 4.1)

*Tài liệu thiết kế kỹ thuật và kiểm thử tích hợp toàn diện dành cho Nhà phát triển và QA/QC*
*Được biên soạn bởi Trưởng ban thiết kế dự án, chuyên gia Phân tích Hệ thống, CSDL và Kiểm thử phần mềm chuyên nghiệp*

---

## 🎯 MỤC TIÊU & PHẠM VI
Đồng bộ hóa toàn bộ 10 cải tiến kỹ thuật cho phân hệ **Công cụ STEM** nhằm đảm bảo tính ổn định tối đa của hệ thống, độ chính xác cao về mặt thuật toán toán học/hóa học, giao diện co giãn hoàn hảo (responsive) và tuân thủ tuyệt đối quy chuẩn sư phạm của chương trình giáo dục Việt Nam (GDPT 2018).

Tài liệu này cung cấp thiết kế chi tiết ở mức mã nguồn (mã giả hoặc đoạn code mẫu), tự phản biện kỹ thuật để tránh các lỗi tiềm ẩn, và bảng kiểm tra (checksheet) rõ ràng để coder không thể làm sai và tester dễ dàng nghiệm thu.

---

## 📂 CẤU TRÚC THƯ MỤC ẢNH HƯỞNG
Lập trình viên cần định vị chính xác các tệp tin sau trước khi sửa đổi:
1.  **Giao diện & Logic STEM chính:**
    *   [StemToolsPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml)
    *   [StemToolsPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs)
2.  **Trình vẽ đồ thị Desmos:**
    *   [GraphWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml.cs)
3.  **Bố cục Sơ đồ tư duy:**
    *   [MindmapTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MindmapTool.xaml)
4.  **Cơ sở dữ liệu & Cấu hình:**
    *   [DbManager.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Helpers/DbManager.cs)
    *   SQLite database file: `C:\Users\DELL\AppData\Roaming\QASmartClass\smartclass.db`

---

## ═══ PHẦN 1: CHI TIẾT 10 HẠNG MỤC CẢI TIẾN STEM ═══

### Hạng mục 1: Điều chỉnh thứ tự ưu tiên của toán tử lũy thừa (`^`) trong Máy tính khoa học
*   **Yêu cầu thiết kế:** Khắc phục lỗi ưu tiên toán tử. Khi học sinh nhập biểu thức chứa hàm lượng giác và lũy thừa như `sin(30)^2`, hệ thống phải thực hiện tính hàm lượng giác trước (kết quả `0.5`), sau đó mới áp dụng lũy thừa trên kết quả đó (kết quả `0.25`).
*   **Dữ liệu đầu vào:** Chuỗi biểu thức toán học (ví dụ: `"sin(30)^2"` hoặc `"cos(60)^3 + 2^3"`).
*   **Dữ liệu đầu ra:** Giá trị số thực (`double`) chính xác (ví dụ: `0.25` cho `sin(30)^2` ở chế độ độ).
*   **Phương pháp thực hiện:**
    Trong hàm `EvalScientific` của tệp [StemToolsPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs):
    1.  Di chuyển toàn bộ khối xử lý các hàm một ngôi lượng giác và logarit (`sin`, `cos`, `tan`, `sqrt`, `log10`, `ln` - sử dụng phương thức `EvalFunc`) lên đầu hàm, ngay sau khi chuẩn hóa ký tự (`×`, `÷`).
    2.  Giữ nguyên khối xử lý vòng lặp `while (expr.Contains("^"))` phía dưới khối xử lý hàm lượng giác.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Nếu chuyển hàm lên trước, các hàm lượng giác lồng nhau chứa lũy thừa như `sin(30^2)` có bị tính sai không?
    *   *Tối ưu:* Không. Khi hàm `EvalFunc` phát hiện chuỗi `sin(30^2)`, nó sẽ trích xuất phần bên trong dấu ngoặc là `"30^2"` và gọi đệ quy `EvalScientific("30^2")`. Cuộc gọi đệ quy này sẽ chạy khối xử lý lũy thừa ở dưới và trả về `900`. Sau đó `sin(900)` được tính toán chuẩn xác. Do đó tính đệ quy vẫn được bảo toàn.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Di chuyển khối hàm lên trước vòng lặp lũy thừa trong `EvalScientific`.
    - [ ] [ ] Kiểm tra kết quả `"sin(30)^2"` ở chế độ Deg ra đúng `0.25`.
    - [ ] [ ] Kiểm tra kết quả lượng giác lồng lũy thừa `"sin(30^2)"` ở chế độ Deg ra đúng `sin(900)`.

---

### Hạng mục 2: Khắc phục lỗi crash parser đối với các hàm bọc trong dấu ngoặc lũy thừa
*   **Yêu cầu thiết kế:** Khi học sinh viết đầy đủ dấu ngoặc như `(sin(30))^2`, hệ thống phải giải quyết trơn tru mà không báo lỗi "Lỗi biểu thức".
*   **Dữ liệu đầu vào:** Chuỗi biểu thức có dấu ngoặc ở ngoài hàm: `"(sin(30))^2"`.
*   **Dữ liệu đầu ra:** Giá trị số thực (`double`) kết quả: `0.25`.
*   **Phương pháp thực hiện:**
    Khi di chuyển xử lý hàm lượng giác lên trước lũy thừa như ở Hạng mục 1, chuỗi `(sin(30))^2` sẽ tự động được biến đổi thành `(0.5)^2` ở bước xử lý lượng giác.
    Sau đó, khối xử lý lũy thừa sẽ nhận diện base là `(0.5)` và số mũ là `2`.
    Trong khối `catch` khi tính `baseVal` bằng `DataTable.Compute`, đảm bảo loại bỏ dấu ngoặc ngoài nếu có:
    ```csharp
    catch
    {
        if (baseStr.StartsWith("(") && baseStr.EndsWith(")"))
            baseStr = baseStr[1..^1];
        baseVal = double.Parse(baseStr, CultureInfo.InvariantCulture);
    }
    ```
    Chuỗi `"0.5"` sẽ được parse thành công mà không gây crash hệ thống.
*   **Phản biện & Tối ưu hóa:**
    *   *Phòng ngừa lỗi:* Nếu `baseStr` sau khi bỏ ngoặc vẫn chứa ký tự không phải số (ví dụ do lỗi nhập liệu của học sinh), `double.Parse` sẽ ném ngoại lệ. Khối `try-catch` lớn ở `EvaluateExpression` sẽ bắt được và hiển thị `"Lỗi biểu thức"` thay vì làm treo ứng dụng.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Kiểm tra biểu thức `"(sin(30))^2"` trả về kết quả `0.25`.
    - [ ] [ ] Nhập biểu thức sai cú pháp như `(sin(30)))^2` để đảm bảo hệ thống báo `"Lỗi biểu thức"` một cách an toàn mà không bị crash.

---

### Hạng mục 3: Đồng bộ hiển thị nghiệm phức của Phương trình bậc 2 theo chuẩn GDPT 2018
*   **Yêu cầu thiết kế:** Theo chương trình GDPT 2018, học sinh cấp phổ thông chưa học số phức. Khi phương trình vô nghiệm trên tập số thực, màn hình kết quả phải ghi rõ ràng trạng thái vô nghiệm thực để tránh gây hiểu nhầm, đồng thời ghi chú nghiệm phức trong ngoặc để học sinh nâng cao tham khảo.
*   **Dữ liệu đầu vào:** Hệ số phương trình bậc 2 có $\Delta < 0$ (ví dụ: $a = 1, b = 2, c = 5 \Rightarrow \Delta = -16$).
*   **Dữ liệu đầu ra:**
    *   Nhãn hiển thị Delta: `Δ = b² - 4ac = -16` (chữ màu đỏ).
    *   Nhãn kết quả:
        `Phương trình vô nghiệm trên tập số thực`
        `(Nghiệm phức: x₁ = -1 + 2i, x₂ = -1 − 2i)`
*   **Phương pháp thực hiện:**
    Tại hàm `SolveQuadratic` trong [StemToolsPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs):
    Sửa khối `else` của điều kiện kiểm tra `delta` thành:
    ```csharp
    else
    {
        double real = -b / (2 * a);
        double imag = Math.Sqrt(-delta) / (2 * Math.Abs(a));
        txtQuadResult.Text = $"Phương trình vô nghiệm trên tập số thực\n(Nghiệm phức: x₁ = {FormatUiNumber(real, "G4")} + {FormatUiNumber(imag, "G4")}i, x₂ = {FormatUiNumber(real, "G4")} − {FormatUiNumber(imag, "G4")}i)";
        txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // Màu đỏ cảnh báo
        txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
    }
    ```
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Nhập hệ số $a=1, b=2, c=5$, kiểm tra chữ kết quả có màu đỏ.
    - [ ] [ ] Xác nhận kết quả hiển thị đúng cụm từ: `"Phương trình vô nghiệm trên tập số thực"`.

---

### Hạng mục 4: Tự động tách hệ số đầu trong bộ Cân bằng phương trình hóa học
*   **Yêu cầu thiết kế:** Cho phép học sinh nhập các phương trình chứa sẵn hệ số cân bằng (ví dụ: nhập `2H2` hoặc `3O2` do vô tình hoặc do muốn kiểm tra lại). Hệ thống phải tự động bóc tách các chữ số đứng đầu chất phản ứng trước khi kiểm tra định dạng chữ hoa đầu tiên của ký hiệu hóa học.
*   **Dữ liệu đầu vào:** Chuỗi công thức hóa học chứa hệ số đứng đầu: `"2H2"`.
*   **Dữ liệu đầu ra:** Vượt qua kiểm thử chữ cái hoa đầu tiên và phân tích cấu trúc nguyên tử thành công (CuSO4.5H2O hoặc 2H2).
*   **Phương pháp thực hiện:**
    Sửa đổi hàm `BalanceChem_Click` trong [StemToolsPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs):
    Trước khi kiểm tra `!char.IsUpper(compound[0])`, chèn đoạn mã trích xuất công thức thực tế (loại bỏ hệ số đầu):
    ```csharp
    string valCompound = compound;
    int coeffLength = 0;
    while (coeffLength < valCompound.Length && char.IsDigit(valCompound[coeffLength]))
    {
        coeffLength++;
    }
    if (coeffLength > 0)
    {
        valCompound = valCompound[coeffLength..];
    }

    if (valCompound.Length == 0)
    {
        ShowChemError($"⚠️ Công thức \"{compound}\" không hợp lệ.\nKhông tìm thấy công thức hóa học.");
        return;
    }

    if (!char.IsUpper(valCompound[0]))
    {
        ShowChemError($"⚠️ Công thức \"{compound}\" không hợp lệ.\nCông thức hóa học phải bắt đầu bằng ký hiệu nguyên tố (chữ hoa).\nVD: Fe, NaCl, H2SO4");
        return;
    }
    ```
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao không sửa trực tiếp biến `compound`?
    *   *Tối ưu:* Giữ nguyên tên gốc `compound` để thông báo lỗi hiển thị chính xác chuỗi học sinh đã nhập, chỉ dùng `valCompound` cho mục đích kiểm tra logic.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Nhập phương trình `"2H2 + O2 -> 2H2O"` và kiểm tra cân bằng thành công.
    - [ ] [ ] Nhập phương trình `"3Fe + 2O2 -> Fe3O4"` và kiểm tra cân bằng thành công.

---

### Hạng mục 5: Ngăn chặn lỗi chồng lấp nhãn (Overlap) trên trục X của Đồ thị Thống kê
*   **Yêu cầu thiết kế:** Khi số lượng điểm dữ liệu lớn hơn 8, nhãn hiển thị tên dưới cột phải được phân bổ so le thành hai hàng (hàng chẵn nằm trên, hàng lẻ nằm dưới), tăng chiều rộng nhãn tối đa để tránh bị cắt chữ thành dấu ba chấm, và căn giữa chính xác theo từng cột vẽ.
*   **Dữ liệu đầu vào:** Danh sách dữ liệu thống kê từ 9 dòng trở lên.
*   **Dữ liệu đầu ra:** Đồ thị vẽ các cột rõ ràng, nhãn trục hoành phân hàng so le không chồng chéo, chữ hiển thị đầy đủ.
*   **Phương pháp thực hiện:**
    Cập nhật hàm `DrawChart` (cho cả Bar chart `type == 0` và Line chart `type == 2`) trong [StemToolsPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs):
    1.  **Với biểu đồ cột (Bar chart):**
        ```csharp
        if (showLabels)
        {
            double labelY = baseY + 4;
            double labelW = barW + 4;
            if (values.Count > 8)
            {
                labelW = (barW + 8) * 2 - 4; // Tăng gấp đôi độ rộng cho phép
                if (i % 2 == 1) labelY += 14; // Đẩy dòng lẻ xuống 14px
            }
            var nameTb = new TextBlock 
            { 
                Text = names[i], 
                FontSize = 9, 
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), 
                MaxWidth = labelW, 
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(nameTb, x + barW / 2 - labelW / 2); // Căn giữa
            Canvas.SetTop(nameTb, labelY);
            chartCanvas.Children.Add(nameTb);
        }
        ```
    2.  **Với biểu đồ đường (Line chart):** áp dụng logic dịch `labelY` tương tự dựa trên `step * 2 - 4` cho độ rộng nhãn `Width`.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Nạp dữ liệu thống kê mẫu có 10 dòng (ví dụ: điểm kiểm tra học sinh).
    - [ ] [ ] Kiểm tra nhãn tên học sinh hiển thị so le 2 dòng và không bị đè lên nhau.

---

### Hạng mục 6: Đồng bộ hóa trạng thái nút "Ghi vòng" (Lap) của Đồng hồ bấm giờ
*   **Yêu cầu thiết kế:** Để tăng tính nhất quán của UX, khi học sinh bấm "Tạm dừng" đồng hồ, nút "Ghi vòng" (btnStopwatchLap) phải bị vô hiệu hóa (IsEnabled = false) vì không thể ghi vòng khi thời gian không chạy. Khi bấm "Tiếp tục", nút này được kích hoạt trở lại.
*   **Dữ liệu đầu vào:** Thao tác nhấp chuột vào nút Start/Pause.
*   **Dữ liệu đầu ra:** Nút Lap thay đổi trạng thái IsEnabled tương ứng.
*   **Phương pháp thực hiện:**
    Tại hàm `Stopwatch_Start` trong [StemToolsPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs):
    ```csharp
    if (_stopwatch.IsRunning)
    {
        // Pause
        _stopwatch.Stop();
        _stopwatchTimer?.Stop();
        btnStopwatchStart.Content = "▶ Tiếp tục";
        btnStopwatchStart.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        btnStopwatchLap.IsEnabled = false; // Vô hiệu hóa nút Lap khi tạm dừng
    }
    else
    {
        // Start/Resume
        _stopwatch.Start();
        _stopwatchTimer?.Start();
        btnStopwatchStart.Content = "⏸ Tạm dừng";
        btnStopwatchStart.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
        btnStopwatchLap.IsEnabled = true; // Kích hoạt nút Lap khi chạy
    }
    ```
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Bấm Start -> Nút Lap sáng.
    - [ ] [ ] Bấm Pause -> Nút Lap mờ đi và không thể click.

---

### Hạng mục 7: Nâng cao tính tương thích của Trình vẽ đồ thị Desmos bằng ranh giới từ `\b`
*   **Yêu cầu thiết kế:** Loại bỏ hoàn toàn các biểu thức chính quy Javascript sử dụng negative lookbehind `(?<![a-zA-Z])` trong WebView2 để tránh lỗi cú pháp Javascript trên các phiên bản WebView2 cũ. Thay thế bằng ranh giới từ `\b` chuẩn hóa của ES3.
*   **Dữ liệu đầu vào:** Chuỗi biểu thức LaTeX chứa biến tự do: `"y = x^2"`.
*   **Dữ liệu đầu ra:** Hàm Javascript `evaluateLatex` trong trang đồ thị phân tích và vẽ thành công parabol mà không ném lỗi cú pháp.
*   **Phương pháp thực hiện:**
    Tại hàm `BuildHtml` bên trong tệp [GraphWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml.cs):
    Chỉnh sửa các chuỗi regex thay thế biến `x`, `n`, `t` thành:
    ```javascript
    // Replace x and n variables using word boundaries
    if (xVal !== null && xVal !== undefined) {
        expr = expr.replace(/\bx\b/g, '(' + xVal + ')');
    }
    if (nVal !== null && nVal !== undefined) {
        expr = expr.replace(/\bn\b/g, '(' + nVal + ')');
    }
    // Replace dynamic slider variables
    for (var sliderVar in sliders) {
        var val = (typeof sliders[sliderVar].value === 'number') ? sliders[sliderVar].value : sliders[sliderVar].start;
        var regex = new RegExp('\\b' + sliderVar + '\\b', 'g');
        expr = expr.replace(regex, '(' + val + ')');
    }
    // Fallback for t variable
    expr = expr.replace(/\bt\b/g, '(0)');
    ```
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc sử dụng `\b` có làm thay thế nhầm chữ cái `x` trong tên hàm như `sin` hay `cos` không?
    *   *Tối ưu:* Không. Ranh giới từ `\b` yêu cầu ký tự xung quanh `x` phải là ký tự không phải từ (non-word character) như khoảng trắng, toán tử hoặc dấu ngoặc. Trong `sin`, chữ `n` đứng liền sau `i`, nên không khớp với `\b`. Giải pháp này an toàn tuyệt đối.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Thay thế tất cả 4 vị trí lookbehind bằng `\b` trong [GraphWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/GraphWindow.xaml.cs).
    - [ ] [ ] Mở đồ thị, vẽ thử đồ thị chứa hàm lượng giác và xác nhận WebView2 hiển thị đồ thị bình thường.

---

### Hạng mục 8: Cải tiến đơn vị quy đổi vàng sang Troy Ounce trong Quy đổi đơn vị
*   **Yêu cầu thiết kế:** Đồng bộ hóa kiến thức thực tế toàn cầu về giao dịch vàng. Bổ sung đơn vị troy ounce (`troy oz` = 31.1035g) và cập nhật preset Ounce vàng dùng đơn vị này.
*   **Dữ liệu đầu vào:** Preset "10 Ounce vàng".
*   **Dữ liệu đầu ra:** Kết quả quy đổi 10 troy oz ra đúng 311.035g.
*   **Phương pháp thực hiện:**
    1.  Trong [StemToolsPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/StemToolsPage.xaml.cs), tại định nghĩa từ điển `Units`, cập nhật nhóm Khối lượng:
        `("troy oz", 0.0311035)`
    2.  Sửa dòng preset:
        `private void Preset_GoldOunce(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "troy oz", "g", "10");`
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Thêm `"troy oz"` vào từ điển `Units`.
    - [ ] [ ] Bấm preset Ounce Vàng và xác nhận kết quả quy đổi ra gam hiển thị `311,035` thay vì `283,495` như cũ.

---

### Hạng mục 9: Đồng bộ ký hiệu Công suất `P = A / t` theo chuẩn sách giáo khoa Việt Nam
*   **Yêu cầu thiết kế:** Loại bỏ ký hiệu công suất `P = W / t` (dễ nhầm $W$ là cơ năng/động năng/thế năng). Chuyển sang ký hiệu chuẩn của Bộ Giáo dục Việt Nam: `P = A / t` (với $A$ là công thực hiện).
*   **Dữ liệu đầu vào:** CSDL chứa bản ghi công thức công suất.
*   **Dữ liệu đầu ra:** Khi học sinh xem bảng công thức Vật lý, công thức Công suất hiển thị là `P = A / t` và mô tả ghi rõ `A: công, t: thời gian`.
*   **Phương pháp thực hiện:**
    1.  Trong tệp [DbManager.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Helpers/DbManager.cs), tại hàm `SeedStemFormulas`, sửa phần tử:
        `("Physics", "Công suất", "P = A / t", "A: công, t: thời gian"),`
    2.  Để cập nhật các máy trạm đã khởi tạo CSDL từ trước, trong hàm khởi tạo tĩnh `static DbManager()`, thêm lệnh SQL Update chạy sau khi khởi tạo bảng:
        ```csharp
        using (var cmdUpdate = new SqliteCommand("UPDATE StemFormulas SET Formula = 'P = A / t', Description = 'A: công, t: thời gian' WHERE Formula = 'P = W / t' AND Name = 'Công suất';", conn))
        {
            cmdUpdate.ExecuteNonQuery();
        }
        ```
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Thay thế dữ liệu seeding trong `SeedStemFormulas`.
    - [ ] [ ] Thêm câu lệnh SQL `UPDATE` vào constructor tĩnh `DbManager()`.
    - [ ] [ ] Mở ứng dụng, vào thẻ Hằng số & Công thức -> kiểm tra phần Công suất đã hiển thị đúng `P = A / t`.

---

### Hạng mục 10: Thiết kế Responsive Toolbar cho Sơ đồ tư duy loại bỏ hoàn toàn đè nút
*   **Yêu cầu thiết kế:** Khắc phục lỗi giao diện `Loi_58`. Đảm bảo các nút của thanh công cụ sơ đồ tư duy không đè lên nhau ở độ phân giải nhỏ bằng cách sử dụng `Grid` 2 cột và `WrapPanel` tự động xuống dòng cho nhóm công cụ bên trái.
*   **Dữ liệu đầu vào:** Bố cục XAML của `MindmapTool.xaml`.
*   **Dữ liệu đầu ra:** Giao diện Toolbar tự động co giãn dòng, không đè nút "Đổi cấu trúc" lên chữ "Văn học".
*   **Phương pháp thực hiện:**
    Sửa đổi cấu trúc thanh công cụ trong [MindmapTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MindmapTool.xaml):
    Thay thế `DockPanel` cũ bằng cấu trúc `Grid`:
    ```xml
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Nhóm nút chức năng bên trái: Sử dụng WrapPanel để tự động xuống dòng -->
        <WrapPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
            <TextBlock Text="🧠 Sơ đồ tư duy" FontSize="16" FontWeight="Bold" Foreground="#333" VerticalAlignment="Center" Margin="0,0,16,0"/>
            <Button x:Name="btnAddCenter" Content="🎯 Chủ đề trung tâm" .../>
            <!-- Các nút tạo node, emoji và danh sách template mẫu... -->
            <TextBlock Text="Template mẫu:" .../>
            <Button Content="📚 Văn học" Click="Template_Literature" .../>
            <Button Content="🔬 STEM" Click="Template_STEM" .../>
            <Button Content="📋 Dự án" Click="Template_Project" .../>
            <Button Content="🏛️ Lịch sử" Click="Template_History" .../>
            <Button Content="📐 Toán học" Click="Template_Math" .../>
            <Button Content="🗓️ Kế hoạch" Click="Template_Weekly" .../>
            <Border Height="24" Width="1" Background="#D0D0D0" Margin="4,0"/>
            <Button Content="📂 Thư viện 20 mẫu" Click="OpenTemplateGallery_Click" .../>
        </WrapPanel>

        <!-- Nhóm nút tác vụ bên phải: Giữ nguyên cố định bên phải -->
        <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center" Margin="12,0,0,0">
            <Button x:Name="btnToggleLayout" Content="🔀 Đổi cấu trúc" Click="ToggleLayout_Click" .../>
            <Button x:Name="btnToggleStyle" Content="🖌️ Đổi nét vẽ" Click="ToggleStyle_Click" .../>
            <Button x:Name="btnChangeColor" Content="🎨 Đổi màu" Click="ChangeColor_Click" .../>
            <Button x:Name="btnSave" Content="💾 Lưu" Click="Save_Click" .../>
            <Button x:Name="btnLoad" Content="📂 Mở" Click="Load_Click" .../>
            <Button x:Name="btnExport" Content="📸 Xuất ảnh" Click="Export_Click" .../>
            <Button x:Name="btnClearAll" Content="🗑️ Xóa tất cả" Click="ClearAll_Click" .../>
        </StackPanel>
    </Grid>
    ```
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Thay thế `DockPanel` bằng `Grid` and `WrapPanel` trong `MindmapTool.xaml`.
    - [ ] [ ] Kéo nhỏ cửa sổ ứng dụng và xác nhận các nút template tự động wrap xuống dòng dưới mà không bị đè lấn sang các nút bên phải.

---

## ═══ PHẦN 2: BẢNG KHỞI TẠO VÀ CẬP NHẬT CƠ SỞ DỮ LIỆU (SQLITE DB SPEC) ═══

Để hỗ trợ việc đồng bộ hóa dữ liệu công thức, cơ sở dữ liệu SQLite tại máy học sinh sẽ được tự động cập nhật như sau:

### 1. Script thiết lập chế độ WAL & Synchronous
Để tăng tốc truy xuất và ngăn lỗi DB Lock khi chạy đa luồng:
```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
```

### 2. Dịch chuyển bản ghi Công thức (Công suất)
Câu lệnh SQL sửa đổi dữ liệu cũ:
```sql
UPDATE StemFormulas 
SET Formula = 'P = A / t', 
    Description = 'A: công, t: thời gian' 
WHERE Formula = 'P = W / t' 
  AND Name = 'Công suất';
```

---

## ═══ PHẦN 3: BỘ KIỂM THỬ TÍCH HỢP TOÀN DIỆN (QA INTEGRATION TESTING) ═══

QA/QC kiểm thử bắt buộc phải chạy qua 8 bước kiểm thử hồi quy sau để ký duyệt nghiệm thu:

| Bước | Tên bài kiểm thử (Test Case) | Dữ liệu đầu vào (Input) | Luồng thực hiện (Steps) | Kết quả đầu ra mong đợi (Expected Output) | Trạng thái |
|:---:|---|---|---|---|:---:|
| **1** | Máy tính khoa học - Độ ưu tiên | Biểu thức `"sin(30)^2"` ở chế độ Độ | 1. Nhập biểu thức.<br>2. Nhấn `=`. | Kết quả hiển thị: `0,25` (phép tính không bị crash hoặc ra 0.98). | `[ ] Chưa test` |
| **2** | Máy tính khoa học - Ngoặc phức tạp | Biểu thức `"(sin(30))^2"` ở chế độ Độ | 1. Nhập biểu thức.<br>2. Nhấn `=`. | Kết quả hiển thị: `0,25`. Không báo "Lỗi biểu thức". | `[ ] Chưa test` |
| **3** | Cân bằng hóa học - Hệ số đầu | Chuỗi phản ứng `"2H2 + O2 -> 2H2O"` | 1. Nhập chuỗi.<br>2. Bấm Cân bằng. | Hệ thống cân bằng chính xác và hiển thị: `2H2 + O2  →  2H2O`. | `[ ] Chưa test` |
| **4** | Phương trình bậc 2 - Vô nghiệm thực | Hệ số $a=1, b=2, c=5$ | 1. Nhập hệ số.<br>2. Xem nhãn kết quả. | - Delta hiển thị: `-16` (màu đỏ).<br>- Kết quả: `"Phương trình vô nghiệm trên tập số thực (Nghiệm phức: x₁ = -1 + 2i, x₂ = -1 − 2i)"` | `[ ] Chưa test` |
| **5** | Đồ thị thống kê - Tránh đè nhãn | Danh sách thống kê gồm 10 học sinh | 1. Nhập 10 học sinh.<br>2. Vẽ biểu đồ cột và biểu đồ đường. | - Các nhãn tên trục hoành hiển thị so le dòng trên dòng dưới rõ ràng.<br>- Các chữ hiển thị đầy đủ, không đè chồng. | `[ ] Chưa test` |
| **6** | Đồng hồ bấm giờ - Khóa phím Lap | stopwatch đang hoạt động rồi dừng | 1. Bấm Bắt đầu (nút Lap bật).<br>2. Bấm Tạm dừng. | Nút Lap chuyển sang màu xám vô hiệu hóa (IsEnabled = false). | `[ ] Chưa test` |
| **7** | Trình vẽ đồ thị Desmos | Biểu thức `"y = sin(x)"` hoặc `"y = x^2"` | 1. Mở đồ thị Desmos.<br>2. Nhập biểu thức. | Đồ thị vẽ bình thường, không ném lỗi Javascript cú pháp regex. | `[ ] Chưa test` |
| **8** | Bảng công thức - Sửa ký hiệu | Truy cập danh sách công thức Vật lý | 1. Mở thẻ Công thức.<br>2. Chọn Vật lý. | Công thức công suất hiển thị đúng dạng `P = A / t`. | `[ ] Chưa test` |
