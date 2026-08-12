# KẾ HOẠCH NÂNG CẤP VÀ CẢI TIẾN CHI TIẾT CÔNG CỤ TOÁN HỌC (BẢN 4.1)

*Tài liệu thiết kế kỹ thuật, lập phương án tối ưu và checksheet kiểm thử dành cho Nhà phát triển và QA/QC*
*Được biên soạn bởi Trưởng ban thiết kế dự án, chuyên gia Phân tích Hệ thống, CSDL và Kiểm thử phần mềm chuyên nghiệp*

---

## 🎯 MỤC TIÊU & PHẠM VI
Đồng bộ hóa và cải tiến toàn bộ các vấn đề kỹ thuật/sư phạm đã phát hiện trong phân hệ **Công cụ Toán học** nhằm đảm bảo tính ổn định tối đa của hệ thống, tuân thủ tuyệt đối quy chuẩn sư phạm của chương trình giáo dục Việt Nam (GDPT 2018), hỗ trợ hiển thị tốt trên màn hình tương tác cảm ứng (Touch-friendly), và đồng bộ phông chữ thiết kế Segoe UI của QA SmartClass v4.1.

Tài liệu này cung cấp thiết kế chi tiết ở mức mã nguồn, phân tích tự phản biện kỹ thuật để đưa ra phương án tối ưu nhất, mô tả dữ liệu đầu vào/đầu ra và bảng kiểm tra (checksheet) từng bước để lập trình viên và kiểm thử viên thực hiện chính xác 100%.

---

## 📂 CẤU TRÚC THƯ MỤC & CÁC TỆP TIN ẢNH HƯỞNG
Lập trình viên cần định vị chính xác các tệp tin sau trước khi sửa đổi:
1.  **Vẽ đồ thị 3D (Solid Geometry):**
    *   [SolidGeometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/SolidGeometryTool.xaml.cs)
2.  **Game tính nhẩm nhanh (Mental Math):**
    *   [MentalMathTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/MentalMathTool.xaml.cs)
3.  **Máy tính khoa học (Scientific Calculator):**
    *   [CalculatorTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/CalculatorTool.xaml)
    *   [CalculatorTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/CalculatorTool.xaml.cs)
4.  **Công cụ Vectơ (Vector Tool):**
    *   [VectorTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/VectorTool.xaml)
5.  **Công cụ Cấp số (Sequence Tool):**
    *   [SequenceTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/SequenceTool.xaml.cs)

---

## ═══ CHI TIẾT 5 HẠNG MỤC NÂNG CẤP & CẢI TIẾN MÔN TOÁN ═══

### Hạng mục 1: Sửa lỗi vẽ đồ thị thiết diện 3D trong Desmos API (SolidGeometryTool)
*   **Mô tả yêu cầu:** Khắc phục lỗi khi nhấp "Xem đồ thị" cho các khối hình không gian (Hộp chữ nhật, Lập phương, Chóp tam giác/tứ giác đều, Lăng trụ, Trụ, Nón, Cầu). WebView2 hiển thị đồ thị Desmos trống trơn do lỗi cú pháp JavaScript truyền vào Desmos API sử dụng hàm `polygon(...)` không hợp chuẩn LaTeX của Desmos.
*   **Dữ liệu đầu vào:** Các tham số kích thước nhập vào từ ô TextBox (ví dụ: a = 3, b = 4, c = 5).
*   **Dữ liệu đầu ra:** Đồ thị Desmos hiển thị chính xác đa giác mặt cắt/thiết diện với nhãn điểm rõ ràng, không có lỗi script.
*   **Phương pháp thực hiện:**
    Tại tệp [SolidGeometryTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/SolidGeometryTool.xaml.cs):
    Thay thế toàn bộ các chuỗi `polygon((...))` bằng `\\operatorname{{polygon}}((...))` trong chuỗi nội dung JS `expJs` (tổng cộng có 9 vị trí ở các dòng 433, 448, 470, 484, 506, 521, 534, 537, 552).
    *Ví dụ dòng 433:*
    ```csharp
    // Trước khi sửa:
    calc.setExpression({{id:'r', latex:'polygon(({na},{nb}), ({ha},{nb}), ({ha},{hb}), ({na},{hb}))', color:'#37474F', fillOpacity:0.15, lineWidth:3}});
    
    // Sau khi sửa:
    calc.setExpression({{id:'r', latex:'\\operatorname{{polygon}}(({na},{nb}), ({ha},{nb}), ({ha},{hb}), ({na},{hb}))', color:'#37474F', fillOpacity:0.15, lineWidth:3}});
    ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc đổi tên hàm có làm lỗi bộ parser xuất SVG offline trong `GraphWindow.xaml.cs` không?
    *   *Phản biện đáp trả:* Không. Bộ parser SVG tìm kiếm chuỗi `'polygon'` bằng `indexOf('polygon')` hoặc biểu thức chính quy `/polygon/`. Chuỗi `\\operatorname{{polygon}}` vẫn chứa từ `'polygon'` nên logic trích xuất tọa độ điểm của bộ parser SVG offline vẫn hoạt động chính xác 100%.
*   **Bảng kiểm tra (Checksheet) cho Coder & Tester:**
    - [ ] [ ] Thay thế thành công cả 9 vị trí chứa `polygon` thành `\\operatorname{{polygon}}` trong `SolidGeometryTool.xaml.cs`.
    - [ ] [ ] Build dự án thành công không lỗi cú pháp.
    - [ ] [ ] Mở Solid Geometry Tool -> chọn "Hộp chữ nhật" -> Nhập a=3, b=4, c=5 -> Nhấn "Xem đồ thị" -> Xác nhận Parabol/Đa giác thiết diện hiển thị rõ ràng trên WebView2.
    - [ ] [ ] Chọn "Chóp tứ giác đều" -> Nhấn "Xem đồ thị" -> Xác nhận đa giác đáy (hình vuông) hiển thị chính xác.
    - [ ] [ ] Bấm nút "Xuất ảnh SVG" trong GraphWindow để xác nhận tính năng xuất offline hoạt động bình thường cho thiết diện.

---

### Hạng mục 2: Mở khóa phím thập phân và đồng bộ xử lý dấu phẩy trong Tính nhẩm nhanh (MentalMathTool)
*   **Mô tả yêu cầu:** Cho phép học sinh nhập đáp án số thập phân khi chơi game tính nhẩm nhanh trên màn hình tương tác cảm ứng. Đồng thời đảm bảo phím chấm thập phân gõ từ bàn phím cứng hoạt động trơn tru bất kể CultureInfo hệ thống là tiếng Anh (`.`) hay tiếng Việt (`,`).
*   **Dữ liệu đầu vào:** Nhấp nút `.` trên TouchNumPad ảo hoặc gõ `.` / `,` trên bàn phím vật lý.
*   **Dữ liệu đầu ra:** TextBox hiển thị đúng dấu phân tách thập phân và parse chính xác kết quả số thực của học sinh khi nhấn Enter.
*   **Phương pháp thực hiện:**
    1.  Tại [MentalMathTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/MentalMathTool.xaml.cs#L43):
        Thay đổi tham số `allowDecimal` thành `true` khi đính kèm bàn phím ảo:
        ```csharp
        // Trước:
        QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtAnswer, step: 1, allowDecimal: false, placement: System.Windows.Controls.Primitives.PlacementMode.Right);
        
        // Sau:
        QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtAnswer, step: 1, allowDecimal: true, placement: System.Windows.Controls.Primitives.PlacementMode.Right);
        ```
    2.  Tại hàm `TextBox_PreviewKeyDown` (dòng 495), đảm bảo chỉ cho phép duy nhất một dấu thập phân và chèn đúng ký tự phân tách của hệ thống hiện tại (`decSep` có thể là `,` ở Windows tiếng Việt):
        ```csharp
        if (e.Key == Key.Decimal || e.Key == Key.OemPeriod)
        {
            var textBox = (TextBox)sender;
            string decSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            int start = textBox.SelectionStart;
            int length = textBox.SelectionLength;
            string textWithoutSelection = textBox.Text.Remove(start, length);
            
            if (!textWithoutSelection.Contains(".") && !textWithoutSelection.Contains(","))
            {
                textBox.Text = textWithoutSelection.Insert(start, decSep);
                textBox.SelectionStart = start + decSep.Length;
            }
            e.Handled = true;
        }
        ```
    3.  Tại hàm `Answer_KeyDown` (dòng 309), khi parse đáp án để so khớp với kết quả, thực hiện chuyển đổi cả `,` thành `.` để parse bằng `CultureInfo.InvariantCulture` một cách đồng bộ:
        ```csharp
        string rawInput = txtAnswer.Text.Trim().Replace(',', '.');
        if (double.TryParse(rawInput, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double answer))
        {
             // so sánh đáp án như cũ
        }
        ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Có nên giới hạn không cho nhập dấu âm (`-`) không?
    *   *Phản biện đáp trả:* Không. Trong game tính nhẩm nhanh, các cấp độ khó hơn có thể sinh ra phép toán trừ có kết quả âm. Vì vậy vẫn cần giữ lại tính năng nhập số âm.
*   **Bảng kiểm tra (Checksheet) cho Coder & Tester:**
    - [ ] [ ] Đổi `allowDecimal` thành `true` trong code-behind của MentalMathTool.
    - [ ] [ ] Kiểm tra trên màn hình cảm ứng (hoặc giả lập): Focus vào TextBox đáp án -> Xác nhận nút `.` trên bàn phím ảo hiển thị và nhấn được.
    - [ ] [ ] Bàn phím vật lý: Gõ phím chấm `.` trên phần phím chữ hoặc phím Decimal trên phím số -> TextBox hiển thị đúng dấu phân tách thập phân tương ứng với Windows (ví dụ: dấu phẩy `,` nếu máy chạy vi-VN).
    - [ ] [ ] Nhập đáp án thập phân (ví dụ: `2.5` hoặc `2,5`) -> Xác nhận hệ thống parse chính xác, không crash và so sánh đúng kết quả.

---

### Hạng mục 3: Thay đổi biểu tượng lịch sử và chống cắt cụt layout Máy tính khoa học (CalculatorTool)
*   **Mô tả yêu cầu:**
    1.  Đồng bộ hóa biểu tượng nút Lịch sử trong tệp XAML để khớp với hướng dẫn sư phạm: Đổi emoji kẹp giấy `📋` thành quyển sổ mở `📖` hoặc sổ tay `📒`.
    2.  Khắc phục lỗi danh sách lịch sử tính toán bị cắt cụt (clipping) bên dưới do giao diện đặt trong StackPanel có chiều cao cố định bị tràn khi mở lịch sử.
*   **Dữ liệu đầu vào:** Nhấn nút Lịch sử `btnCalcHistory`.
*   **Dữ liệu đầu ra:**
    *   Nút bấm đổi sang icon `📖`.
    *   Bảng lịch sử tính toán `calcHistoryPanel` hiển thị đè lên phần phím bấm một cách thẩm mỹ giống như một lớp phủ (overlay), không làm biến dạng hay phình to chiều cao của máy tính, có thanh cuộn dọc hoạt động độc lập khi danh sách lịch sử dài.
*   **Phương pháp thực hiện:**
    1.  Trong [CalculatorTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/CalculatorTool.xaml):
        *   Dòng 61: Sửa `Content="📋"` thành `Content="📖"`.
    2.  **Phương án tái cấu trúc Layout tránh clipping:**
        Bọc khu vực phím bấm và lịch sử tính toán vào một Grid chung có cấu trúc xếp lớp (Overlap) thay vì StackPanel nối tiếp.
        *Chi tiết XAML đề xuất chỉnh sửa (ở vùng chứa Grid trái từ dòng 46):*
        ```xml
        <Border Grid.Column="0" CornerRadius="14" Padding="0" BorderBrush="#D0D5DD" BorderThickness="1" HorizontalAlignment="Stretch">
            <Border.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
                    <GradientStop Color="#F8F9FA" Offset="0"/>
                    <GradientStop Color="#FFFFFF" Offset="0.3"/>
                </LinearGradientBrush>
            </Border.Background>

            <!-- Thay thế StackPanel gốc bằng Grid để quản lý chiều cao chặt chẽ -->
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/> <!-- Header DockPanel -->
                    <RowDefinition Height="Auto"/> <!-- Màn hình hiển thị số -->
                    <RowDefinition Height="*"/>    <!-- Khu vực phím bấm & Lịch sử -->
                </Grid.RowDefinitions>

                <DockPanel Grid.Row="0" Margin="16,12,16,0">
                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal">
                        <Border x:Name="borderUnit" Background="#1565C0" CornerRadius="6" Padding="10,5" VerticalAlignment="Center" Margin="0,0,10,0" Cursor="Hand" MouseLeftButtonDown="ToggleUnit_Click">
                            <TextBlock x:Name="txtCalcUnit" Text="DEG" FontSize="13" Foreground="White" FontWeight="Bold"/>
                        </Border>
                        <!-- Đổi emoji 📋 thành 📖 -->
                        <Button x:Name="btnCalcHistory" Content="📖" FontSize="16" Padding="8,4" Background="Transparent" BorderThickness="0" Cursor="Hand" Click="CalcHistory_Click" ToolTip="{DynamicResource Calc_HistoryTooltip}"/>
                        <Button Content="🗑️" FontSize="16" Background="Transparent" BorderThickness="0" Cursor="Hand" Padding="8,4" Click="CalcClearAll_Click" ToolTip="{DynamicResource Calc_ClearTooltip}"/>
                    </StackPanel>
                    <TextBlock Text="{DynamicResource Calc_Title}" FontSize="24" FontWeight="Bold" Foreground="#333"/>
                </DockPanel>

                <Border Grid.Row="1" Background="#1E293B" CornerRadius="10" Margin="16,12,16,0" Padding="20,16">
                    <StackPanel>
                        <TextBox x:Name="txtCalcInput" FontSize="24" FontFamily="Segoe UI" Background="Transparent" BorderThickness="0" Foreground="#94A3B8" TextAlignment="Right" Padding="0,0,0,4" CaretBrush="#94A3B8" KeyDown="CalcInput_KeyDown" ToolTip="{DynamicResource Calc_InputTooltip}"/>
                        <Viewbox Height="70" HorizontalAlignment="Right" VerticalAlignment="Center">
                            <TextBlock x:Name="txtCalcDisplay" Text="0" FontSize="56" FontWeight="Bold" Foreground="White" TextAlignment="Right" FontFamily="Segoe UI"/>
                        </Viewbox>
                    </StackPanel>
                </Border>

                <!-- Khu vực phím bấm & Lịch sử xếp chồng (Z-Index) -->
                <Grid Grid.Row="2" Margin="0,0,0,0">
                    <!-- Bàn phím máy tính -->
                    <UniformGrid x:Name="calcButtons" Columns="6" Margin="16,12,16,20" Panel.ZIndex="0"/>

                    <!-- Bảng Lịch sử tính toán (Overlay) đè lên bàn phím khi mở -->
                    <Border x:Name="calcHistoryPanel" Visibility="Collapsed" Background="#F8F9FA" Padding="12" Margin="16,12,16,20" CornerRadius="8" BorderBrush="#D0D5DD" BorderThickness="1" Panel.ZIndex="1" VerticalAlignment="Stretch">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="*"/>
                                <RowDefinition Height="Auto"/>
                            </Grid.RowDefinitions>
                            <ScrollViewer Grid.Row="0" VerticalScrollBarVisibility="Auto" Margin="0,0,0,8">
                                <StackPanel x:Name="calcHistoryList"/>
                            </ScrollViewer>
                            <Button Grid.Row="1" Content="Đóng lịch sử" Background="#E2E8F0" Foreground="#475569" Padding="10,6" BorderThickness="0" Cursor="Hand" Click="CalcHistory_Click" HorizontalAlignment="Right">
                                <Button.Resources>
                                    <Style TargetType="Border">
                                        <Setter Property="CornerRadius" Value="4"/>
                                    </Style>
                                </Button.Resources>
                            </Button>
                        </Grid>
                    </Border>
                </Grid>
            </Grid>
        </Border>
        ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao lại dùng cơ chế overlay đè lên bàn phím thay vì đẩy bàn phím xuống?
    *   *Tối ưu:* Do máy tính có chiều cao cố định tổng thể khoảng 600px. Nếu đẩy bàn phím xuống, bàn phím (với các nút cao 64px) bắt buộc phải co lại hoặc bị đẩy xuống dưới cùng gây biến dạng hoàn toàn bố cục nút hoặc bị cắt khuất chân nút. Dùng cơ chế Overlay (Z-Index) giải quyết triệt để lỗi thiết kế, người dùng chỉ cần nhấn "Đóng lịch sử" hoặc biểu tượng quyển sổ một lần nữa để quay lại bàn phím, tương tự máy tính iOS/Android chuẩn.
*   **Bảng kiểm tra (Checksheet) cho Coder & Tester:**
    - [ ] [ ] Kiểm tra emoji của nút lịch sử hiển thị hình quyển sổ `📖`.
    - [ ] [ ] Nhấn nút lịch sử -> Xác nhận bảng lịch sử hiển thị mượt mà đè lên lưới phím.
    - [ ] [ ] Nhập nhiều phép tính để tạo danh sách lịch sử dài -> Xác nhận ScrollViewer xuất hiện thanh cuộn và cuộn xem được toàn bộ danh sách, không bị tràn hay cắt chữ.
    - [ ] [ ] Nhấn nút "Đóng lịch sử" hoặc icon quyển sổ một lần nữa -> Bảng ẩn đi và lưới phím hiển thị bình thường.

---

### Hạng mục 4: Chuẩn hóa phông chữ đồng bộ trong VectorTool
*   **Mô tả yêu cầu:** Loại bỏ hoàn toàn phông chữ không hợp lệ `Times New Roman` khỏi tệp XAML để tuân thủ thiết kế đồng bộ của dự án sử dụng `Segoe UI`.
*   **Dữ liệu đầu vào:** Nạp trang hướng dẫn sử dụng công cụ vectơ.
*   **Dữ liệu đầu ra:** Văn bản hướng dẫn hiển thị sắc nét bằng phông chữ Segoe UI chuẩn.
*   **Phương pháp thực hiện:**
    Trong tệp [VectorTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/VectorTool.xaml):
    *   Sửa dòng 196: Loại bỏ thuộc tính `FontFamily="Times New Roman"`.
    ```diff
    - <ScrollViewer x:Name="contentGuide" VerticalScrollBarVisibility="Auto" Visibility="Collapsed" FontFamily="Times New Roman">
    + <ScrollViewer x:Name="contentGuide" VerticalScrollBarVisibility="Auto" Visibility="Collapsed">
    ```
*   **Bảng kiểm tra (Checksheet) cho Coder & Tester:**
    - [ ] [ ] Xóa bỏ chuỗi `FontFamily="Times New Roman"` trong `VectorTool.xaml`.
    - [ ] [ ] Mở Vector Tool -> Nhấn tab "Hướng dẫn" -> Xác nhận toàn bộ phông chữ của văn bản hướng dẫn hiển thị theo font `Segoe UI` đồng bộ của hệ thống.

---

### Hạng mục 5: Chuẩn hóa phông chữ file HTML xuất bài giải của Cấp số cộng/nhân (SequenceTool)
*   **Mô tả yêu cầu:** Khi giáo viên/học sinh xuất bài giải chi tiết ra file HTML, các khối công thức toán phải sử dụng phông chữ hỗ trợ Unicode tiếng Việt tốt, không dùng `Consolas` (vì Consolas không hỗ trợ tốt dấu tiếng Việt và ký hiệu toán học đặc biệt, gây lỗi hiển thị ô vuông hoặc mất ký tự).
*   **Dữ liệu đầu vào:** Thao tác nhấn xuất bài giải chi tiết trong SequenceTool.
*   **Dữ liệu đầu ra:** File HTML được xuất ra, mở trên trình duyệt hiển thị các công thức toán với phông chữ đẹp mắt, không lỗi hiển thị ký tự đặc biệt hay ký tự tiếng Việt.
*   **Phương pháp thực hiện:**
    Tại tệp [SequenceTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/SequenceTool.xaml.cs):
    *   Sửa dòng 1527: Thay thế `font-family: Consolas, monospace;` bằng `font-family: 'Segoe UI', 'Courier New', monospace;`.
    ```diff
    - sb.AppendLine(".formula { ... font-family: Consolas, monospace; ... }");
    + sb.AppendLine(".formula { ... font-family: 'Segoe UI', 'Courier New', monospace; ... }");
    ```
*   **Bảng kiểm tra (Checksheet) cho Coder & Tester:**
    - [ ] [ ] Cập nhật thành công dòng code CSS trong `SequenceTool.xaml.cs`.
    - [ ] [ ] Mở Cấp số cộng/nhân -> Chọn xuất bài giải chi tiết -> Chọn lưu tệp HTML.
    - [ ] [ ] Mở tệp HTML bằng trình duyệt -> Xác nhận phông chữ các công thức hiển thị chuẩn xác, không bị lỗi font hay lỗi ký tự tiếng Việt có dấu.

---

## 🏁 KẾ HOẠCH BÀN GIAO & NGHIỆM THU

Sau khi lập trình viên hoàn thành việc cập nhật mã nguồn theo đúng mô tả trên:
1.  **Biên dịch hệ thống:** Tiến hành xây dựng lại dự án `QASmartClass.sln` bằng lệnh:
    `dotnet build "d:\JOB\QA SmartClass -062026\QASmartClass.sln" -c Debug`
2.  **Xác nhận checksheet:** Tester tiến hành thực hiện kiểm tra thủ công toàn bộ các bước kiểm tra (Checksheet) trong tài liệu này và ký xác nhận hoàn thành vào báo cáo kiểm thử.

*Kế hoạch đã được phê duyệt kỹ thuật bởi Trưởng ban thiết kế dự án. Yêu cầu lập trình viên thực hiện nghiêm ngặt.*
