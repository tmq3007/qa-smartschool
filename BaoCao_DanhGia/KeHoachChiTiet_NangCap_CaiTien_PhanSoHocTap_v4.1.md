# Kế hoạch Nâng cấp và Cải tiến Công cụ Phân số Học tập (FractionTool) - Bản v4.1

Kế hoạch thiết kế kỹ thuật, lập phương án tối ưu và checksheet kiểm thử dành cho Nhà phát triển và QA/QC nhằm đồng bộ hóa công cụ **Phân Số Học Tập** với quy chuẩn sư phạm và kỹ thuật **QA SmartClass v4.1**.

---

## User Review Required

> [!IMPORTANT]
> **Thay đổi cấu trúc lớp dữ liệu dùng chung (PracticalAppItem):**
> Lớp `PracticalAppItem` nằm trong thư mục [Models](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Models/PracticalAppItem.cs) được chia sẻ bởi 16 công cụ khác nhau trong hệ thống (bao gồm Toán, Ngôn ngữ, Khoa học, STEM...). 
> Việc bổ sung các thuộc tính học thuật mới bắt buộc phải gán giá trị mặc định là chuỗi rỗng `""` để tránh lỗi biên dịch (Compilation Error) hoặc lỗi tham chiếu rỗng (NullReferenceException) ở các công cụ khác chưa nâng cấp dữ liệu.

> [!WARNING]
> **Loại bỏ hoàn toàn MessageBox.Show:**
> Phải chuyển đổi toàn bộ 14 vị trí sử dụng `MessageBox.Show` trong [FractionTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml.cs) sang cơ chế Banner thông báo nội bộ (Inline Status/Error Box). Điều này giúp ứng dụng mượt mà trên màn hình cảm ứng tương tác lớn và không làm gián đoạn kịch bản kiểm thử tự động UI.

---

## Open Questions

> [!NOTE]
> **Lưu trữ Điểm số Cao (High Score):**
> Hiện tại điểm số đang được lưu vào file text phẳng `fraction_highscore.txt`. Chúng tôi đề xuất mã hóa điểm số này bằng thuật toán mã hóa đối xứng AES-256 sử dụng khóa máy trạm (Machine Key) hoặc chuyển hẳn vào cơ sở dữ liệu SQLite `smartclass.db`. Hãy xác nhận bạn đồng ý với phương án tích hợp SQLite cục bộ để đảm bảo an toàn dữ liệu học sinh.

---

## Proposed Changes

### [Models]
#### [MODIFY] [PracticalAppItem.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Models/PracticalAppItem.cs)
*   Thêm các thuộc tính mới để chứa dữ liệu học thuật phong phú, lấp đầy khoảng trống thị giác.
*   Cung cấp giá trị khởi tạo mặc định để giữ tính tương thích ngược.

### [Controls]
#### [MODIFY] [PracticalAppViewer.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Controls/PracticalAppViewer.xaml)
*   Tái cấu trúc cột trái (Grid.Column="0" của Preview): Chia làm 2 hàng (Hàng 1 co giãn tự do cho hình ảnh, Hàng 2 hiển thị Thẻ khái niệm toán học trực quan `panelMathFormula`).
*   Tái cấu trúc cột phải (Grid.Column="1" của Preview): Thêm các Card thông tin bo tròn với màu sắc chuyên nghiệp để chứa Phân tích Toán học, Câu hỏi thảo luận và Nhiệm vụ thực hành.

#### [MODIFY] [PracticalAppViewer.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Controls/PracticalAppViewer.xaml.cs)
*   Bổ sung logic cập nhật các trường thông tin mới khi thay đổi đề mục được chọn.
*   Tự động ẩn/hiển thị linh hoạt các Card thông tin tùy thuộc vào việc dữ liệu có bị rỗng hay không.

### [Views/Math]
#### [MODIFY] [FractionTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml)
*   Đồng bộ hóa font chữ học thuật bằng cách gán `FontFamily="Segoe UI"` tại thẻ root `<controls:BaseToolControl>`.
*   Bổ sung điều khiển thông báo lỗi inline dưới dạng Border ẩn/hiện thay thế cho các popup cảnh báo.

#### [MODIFY] [FractionTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml.cs)
*   Cập nhật dữ liệu học thuật chi tiết cho cả 8 ứng dụng thực tế của phân số trong `LoadPracticalApps()`.
*   Chuyển đổi toàn bộ `MessageBox.Show` sang hiển thị thông báo inline tại Panel thông báo lỗi mới.
*   Nâng cấp cơ chế lưu trữ điểm số cao chống sửa đổi điểm bằng mã hóa băm dữ liệu đơn giản.

---

## ═══ CHI TIẾT 6 HẠNG MỤC NÂNG CẤP & CẢI TIẾN PHÂN SỐ ═══

### Hạng mục 1: Nâng cấp Lớp dữ liệu dùng chung (PracticalAppItem.cs)
*   **Mô tả yêu cầu:** Bổ sung các trường thông tin học thuật vào model dữ liệu để truyền tải tri thức sâu rộng, giải quyết triệt để lỗi trống thông tin hiển thị.
*   **Dữ liệu đầu vào:** Thuộc tính đối tượng C#.
*   **Dữ liệu đầu ra:** Các trường: `Formula`, `MathAnalysis`, `DiscussionQuestion`, `InteractiveTask` có giá trị mặc định là chuỗi rỗng `""`.
*   **Phương pháp thực hiện:**
    Tại tệp [PracticalAppItem.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Models/PracticalAppItem.cs):
    ```csharp
    namespace QASmartClass.LearningTools.Models
    {
        public class PracticalAppItem
        {
            public string Icon { get; set; } = "🌍";
            public string Title { get; set; } = "";
            public string ImagePath { get; set; } = ""; 
            public string Description { get; set; } = "";
            
            // Nâng cấp bản v4.1+ giải quyết khoảng trống hiển thị
            public string Formula { get; set; } = "";
            public string MathAnalysis { get; set; } = "";
            public string DiscussionQuestion { get; set; } = "";
            public string InteractiveTask { get; set; } = "";
        }
    }
    ```
*   **Phương pháp phản biện:** 
    *   *Phản biện:* Việc thêm các thuộc tính này có làm crash các công cụ khác khi biên dịch không?
    *   *Phản biện đáp trả:* Không. Do các thuộc tính được gán giá trị mặc định `""` và sử dụng cơ chế Get/Set tiêu chuẩn của C#, các lớp kế thừa hoặc gọi đối tượng này ở các công cụ khác (như `BasicMathTool`, `ComplexNumberTool`...) vẫn biên dịch thành công và chạy bình thường mà không cần sửa đổi mã nguồn của chúng.

---

### Hạng mục 2: Tái cấu trúc Layout chống trống giao diện (PracticalAppViewer.xaml)
*   **Mô tả yêu cầu:** Tổ chức lại bố cục hiển thị cột trái và cột phải của khu vực xem trước để tận dụng triệt để không gian màn hình độ phân giải cao.
*   **Dữ liệu đầu vào:** Cấu trúc XAML.
*   **Dữ liệu đầu ra:** Giao diện co giãn tự thích ứng, không còn các khoảng trống đứng vô nghĩa.
*   **Phương pháp thực hiện:**
    1.  Tại [PracticalAppViewer.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Controls/PracticalAppViewer.xaml#L80):
        Thay thế toàn bộ cấu trúc Grid bên trong Grid Column 2 bằng layout cải tiến 2 cột song song như sau:
        ```xml
        <Grid Grid.Column="2" Margin="24">
            <Grid.ColumnDefinitions>
                <Grid.ColumnDefinition Width="1.2*"/>
                <Grid.ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- CỘT TRÁI PREVIEW: Ảnh & Thẻ công thức cốt lõi bên dưới -->
            <Grid Grid.Column="0" Margin="0,0,20,0">
                <Grid.RowDefinitions>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                
                <Border Grid.Row="0" CornerRadius="12" BorderThickness="1" BorderBrush="{DynamicResource BorderBrush}" Background="{DynamicResource SurfaceBackground}" ClipToBounds="True" Margin="0,0,0,12">
                    <Border.Effect>
                        <DropShadowEffect BlurRadius="12" ShadowDepth="2" Opacity="0.06"/>
                    </Border.Effect>
                    <Grid>
                        <Image x:Name="imgLargePreview" Stretch="Uniform" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        <TextBlock x:Name="txtLoadingPlaceholder" Text="⏳ Đang tải hình ảnh..." FontSize="14" Foreground="#94A3B8" HorizontalAlignment="Center" VerticalAlignment="Center" Visibility="Collapsed"/>
                    </Grid>
                </Border>

                <!-- Thẻ khái niệm toán học trực quan (Lấp đầy khoảng trống đứng dưới ảnh) -->
                <Border x:Name="panelMathFormula" Grid.Row="1" CornerRadius="12" BorderThickness="1" BorderBrush="#FFE082" Background="#FFFDE7" Padding="16,12" Visibility="Collapsed">
                    <StackPanel>
                        <TextBlock Text="🔑 Khái niệm sư phạm cốt lõi" FontSize="13" FontWeight="Bold" Foreground="#F57C00" Margin="0,0,0,4"/>
                        <TextBlock x:Name="txtMathFormula" Text="" FontSize="15" FontWeight="SemiBold" Foreground="#37474F" TextWrapping="Wrap" HorizontalAlignment="Center" TextAlignment="Center"/>
                    </StackPanel>
                </Border>
            </Grid>

            <!-- CỘT PHẢI PREVIEW: Nội dung đa tầng cuộn độc lập -->
            <Grid Grid.Column="1">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <TextBlock x:Name="txtDetailTitle" Grid.Row="0" FontSize="20" FontWeight="Bold" Foreground="{DynamicResource TextPrimary}" Margin="0,0,0,12" HorizontalAlignment="Left" TextWrapping="Wrap"/>

                <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                    <StackPanel Margin="0,0,4,0">
                        <TextBlock x:Name="txtDetailDesc" FontSize="14.5" FontWeight="Medium" Foreground="{DynamicResource TextSecondary}" TextWrapping="Wrap" LineHeight="22" Margin="0,0,0,16"/>

                        <!-- Khối Phân tích toán học (Xanh lam) -->
                        <Border x:Name="panelMathAnalysis" CornerRadius="10" Background="#E3F2FD" BorderBrush="#90CAF9" BorderThickness="1" Padding="14,12" Margin="0,0,0,12" Visibility="Collapsed">
                            <StackPanel>
                                <TextBlock Text="📊 Phân tích Toán học" FontSize="13.5" FontWeight="Bold" Foreground="#0D47A1" Margin="0,0,0,6"/>
                                <TextBlock x:Name="txtMathAnalysis" FontSize="13" Foreground="#1565C0" TextWrapping="Wrap" LineHeight="18"/>
                            </StackPanel>
                        </Border>

                        <!-- Khối Câu hỏi thảo luận (Cam nhạt) -->
                        <Border x:Name="panelDiscussionQuestion" CornerRadius="10" Background="#FFF3E0" BorderBrush="#FFB74D" BorderThickness="1" Padding="14,12" Margin="0,0,0,12" Visibility="Collapsed">
                            <StackPanel>
                                <TextBlock Text="💬 Câu hỏi gợi mở &amp; Thảo luận" FontSize="13.5" FontWeight="Bold" Foreground="#E65100" Margin="0,0,0,6"/>
                                <TextBlock x:Name="txtDiscussionQuestion" FontSize="13" Foreground="#D84315" TextWrapping="Wrap" LineHeight="18"/>
                            </StackPanel>
                        </Border>

                        <!-- Khối Nhiệm vụ thực hành (Xanh lá) -->
                        <Border x:Name="panelPracticeTask" CornerRadius="10" Background="#E8F5E9" BorderBrush="#A5D6A7" BorderThickness="1" Padding="14,12" Margin="0,0,0,12" Visibility="Collapsed">
                            <StackPanel>
                                <TextBlock Text="✍️ Nhiệm vụ thực hành học sinh" FontSize="13.5" FontWeight="Bold" Foreground="#2E7D32" Margin="0,0,0,6"/>
                                <TextBlock x:Name="txtPracticeTask" FontSize="13" Foreground="#1B5E20" TextWrapping="Wrap" LineHeight="18"/>
                            </StackPanel>
                        </Border>
                    </StackPanel>
                </ScrollViewer>
            </Grid>
        </Grid>
        ```
    2.  Tại [PracticalAppViewer.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Controls/PracticalAppViewer.xaml.cs#L29):
        Cập nhật hàm xử lý sự kiện chuyển đổi đề mục `LstPracticalItems_SelectionChanged` để gán dữ liệu động cho các bảng thông tin:
        ```csharp
        txtDetailTitle.Text = selectedItem.Title;
        txtDetailDesc.Text = selectedItem.Description;

        // Cập nhật Khái niệm sư phạm cốt lõi
        if (panelMathFormula != null && txtMathFormula != null)
        {
            var formula = GetPropertyValue(selectedItem, "Formula");
            if (!string.IsNullOrEmpty(formula))
            {
                txtMathFormula.Text = formula;
                panelMathFormula.Visibility = Visibility.Visible;
            }
            else
            {
                panelMathFormula.Visibility = Visibility.Collapsed;
            }
        }

        // Cập nhật Phân tích toán học
        if (panelMathAnalysis != null && txtMathAnalysis != null)
        {
            var analysis = GetPropertyValue(selectedItem, "MathAnalysis");
            if (!string.IsNullOrEmpty(analysis))
            {
                txtMathAnalysis.Text = analysis;
                panelMathAnalysis.Visibility = Visibility.Visible;
            }
            else
            {
                panelMathAnalysis.Visibility = Visibility.Collapsed;
            }
        }

        // Cập nhật Câu hỏi gợi mở
        if (panelDiscussionQuestion != null && txtDiscussionQuestion != null)
        {
            var question = GetPropertyValue(selectedItem, "DiscussionQuestion");
            if (!string.IsNullOrEmpty(question))
            {
                txtDiscussionQuestion.Text = question;
                panelDiscussionQuestion.Visibility = Visibility.Visible;
            }
            else
            {
                panelDiscussionQuestion.Visibility = Visibility.Collapsed;
            }
        }

        // Cập nhật Nhiệm vụ thực hành
        if (panelPracticeTask != null && txtPracticeTask != null)
        {
            var task = GetPropertyValue(selectedItem, "InteractiveTask");
            if (!string.IsNullOrEmpty(task))
            {
                txtPracticeTask.Text = task;
                panelPracticeTask.Visibility = Visibility.Visible;
            }
            else
            {
                panelPracticeTask.Visibility = Visibility.Collapsed;
            }
        }
        ```
        *Lưu ý:* Thêm hàm trợ giúp phản chiếu (reflection) an toàn để lấy thuộc tính động mà không gây lỗi biên dịch ở các bản build không đồng nhất:
        ```csharp
        private string GetPropertyValue(object obj, string propName)
        {
            if (obj == null) return "";
            var prop = obj.GetType().GetProperty(propName);
            return prop != null ? (prop.GetValue(obj) as string ?? "") : "";
        }
        ```

---

### Hạng mục 3: Làm phong phú dữ liệu Ứng dụng thực tế Phân số (FractionTool.xaml.cs)
*   **Mô tả yêu cầu:** Khai báo đầy đủ thông tin học thuật trực quan cho cả 8 đề mục ứng dụng thực tế phân số trong code-behind của FractionTool.
*   **Dữ liệu đầu vào:** Logic khởi tạo danh sách `List<PracticalAppItem>`.
*   **Dữ liệu đầu ra:** Danh sách các đề mục có đầy đủ thuộc tính `Formula`, `MathAnalysis`, `DiscussionQuestion`, `InteractiveTask` được chuẩn hóa tiếng Việt.
*   **Phương pháp thực hiện:**
    Tại tệp [FractionTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml.cs#L652):
    Thay đổi khởi tạo danh sách `items` thành phiên bản chi tiết đầy đủ sau:
    ```csharp
    var items = new List<PracticalAppItem>
    {
        new PracticalAppItem
        {
            Icon = "🍕",
            Title = isVN ? "Chia sẻ thức ăn" : "Culinary Recipe Scaling",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_1_{suffix}.png",
            Description = isVN 
                ? "Phân số biểu diễn việc chia một chiếc pizza hoặc bánh ngọt thành các phần bằng nhau." 
                : "Adjust cooking ingredient portions up or down by multiplying fraction ratios.",
            Formula = isVN ? "Phần thức ăn nhận được = Số phần ăn lấy đi / Tổng số phần bằng nhau" : "Portion = Parts Taken / Total Equal Parts",
            MathAnalysis = isVN 
                ? "Một chiếc bánh pizza được cắt thành 8 phần bằng nhau. Nếu em ăn 3 phần, em đã ăn 3/8 chiếc bánh. Phần bánh còn lại là 5/8 chiếc bánh.\nPhép cộng: 3/8 (phần đã ăn) + 5/8 (phần còn lại) = 8/8 = 1 chiếc bánh nguyên vẹn."
                : "A pizza is cut into 8 equal slices. Eating 3 slices means you consumed 3/8 of the pizza. The remaining fraction is 5/8.",
            DiscussionQuestion = isVN
                ? "Nếu cắt chiếc bánh làm 4 phần và ăn 2 phần, so với cắt làm 8 phần và ăn 4 phần thì lượng bánh ăn được có bằng nhau không? Hãy liên hệ với bài học phân số bằng nhau."
                : "Is 2/4 of a cake equal to 4/8 of the same cake? Explain using equivalent fractions.",
            InteractiveTask = isVN
                ? "Thực hành chia một chiếc bánh hình tròn giấy thành 6 phần bằng nhau. Hãy tô màu đỏ vào 2 phần, và màu xanh vào 3 phần. Viết các phân số tương ứng biểu diễn phần tô màu."
                : "Divide a paper circle into 6 equal slices. Color 2 slices red and 3 slices green. Write down the corresponding fractions."
        },
        new PracticalAppItem
        {
            Icon = "🍳",
            Title = isVN ? "Đo lường nguyên liệu nấu ăn" : "Culinary Portions",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_2_{suffix}.png",
            Description = isVN 
                ? "Các công thức nấu ăn thường dùng phân số để đong đếm nguyên liệu chính xác." 
                : "Recipes use fractions for measuring ingredients precisely.",
            Formula = isVN ? "Nguyên liệu cần dùng = Định lượng chuẩn × Tỷ lệ tăng/giảm công thức" : "Required Ingredient = Base Amount × Scale Ratio",
            MathAnalysis = isVN
                ? "Công thức chuẩn cần dùng 3/4 cốc bột mì. Nếu ta muốn làm một nửa công thức (tỷ lệ 1/2), lượng bột mì cần đong là:\n3/4 × 1/2 = 3/8 cốc bột mì."
                : "A recipe requires 3/4 cup of flour. Making a half batch means multiplying 3/4 by 1/2, resulting in 3/8 cup of flour.",
            DiscussionQuestion = isVN
                ? "Làm thế nào để đong được đúng 3/8 cốc bột mì nếu em chỉ có các loại cốc đong chia vạch 1/8 cốc?"
                : "How can you measure 3/8 cup of flour if you only have a 1/8 cup measuring tool?",
            InteractiveTask = isVN
                ? "Đọc một công thức làm bánh ngọt bất kỳ trên sách. Hãy tính lại định lượng của tất cả nguyên liệu nếu chúng ta muốn làm gấp đôi công thức (nhân với 2)."
                : "Choose a recipe and double all the fractional measurements. Write down the new recipe proportions."
        },
        new PracticalAppItem
        {
            Icon = "⏰",
            Title = isVN ? "Xem đồng hồ & Thời gian" : "Clock & Time Intervals",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_3_{suffix}.png",
            Description = isVN 
                ? "Mặt đồng hồ kim được chia thành các khoảng góc phần tư giúp chúng ta hình dung thời gian trôi qua trực quan." 
                : "Analog clocks are divided into quadrants to visualize time intervals.",
            Formula = isVN ? "Phân số thời gian = Số phút trôi qua / 60 phút" : "Time Fraction = Minutes Passed / 60 Minutes",
            MathAnalysis = isVN
                ? "Mặt đồng hồ hình tròn có 60 phút. Khoảng thời gian 15 phút tương đương phân số 15/60 = 1/4 giờ. Khoảng thời gian 30 phút tương đương phân số 30/60 = 1/2 giờ (nửa tiếng)."
                : "An hour has 60 minutes. A 15-minute interval represents 15/60 = 1/4 of an hour. A 30-minute interval represents 30/60 = 1/2 of an hour.",
            DiscussionQuestion = isVN
                ? "Tại sao người ta thường gọi 15 phút là 'một phần tư giờ' và 45 phút là 'ba phần tư giờ'?"
                : "Why do we say 'a quarter past' for 15 minutes and 'a quarter to' for 45 minutes?",
            InteractiveTask = isVN
                ? "Vẽ một mặt đồng hồ tròn. Hãy tô màu xanh lá cây vào góc đại diện cho khoảng thời gian 20 phút. Rút gọn phân số biểu diễn khoảng thời gian này."
                : "Draw a clock face. Color the area representing 20 minutes and simplify the resulting fraction."
        },
        new PracticalAppItem
        {
            Icon = "⛽",
            Title = isVN ? "Vạch nhiên liệu xe cộ" : "Fuel Gauge Levels",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_4_{suffix}.png",
            Description = isVN 
                ? "Kim chỉ nhiên liệu trên bảng điều khiển xe hiển thị lượng xăng còn lại dưới dạng phân số như 1/4, 1/2 hoặc 3/4 bình xăng." 
                : "Fuel gauges display remaining fuel level as fractional markers like 1/4, 1/2, or 3/4.",
            Formula = isVN ? "Tỷ lệ xăng còn lại = Lượng xăng thực tế trong bình / Thể tích tối đa của bình xăng" : "Remaining Fuel Ratio = Current Volume / Tank Capacity",
            MathAnalysis = isVN
                ? "Nếu bình xăng của xe máy có dung tích tối đa là 4.8 lít xăng. Khi kim xăng chỉ ở vạch 1/4 bình, lượng xăng còn lại trong bình là:\n4.8 × 1/4 = 1.2 lít xăng."
                : "If a motorcycle tank holds 4.8 liters, a 1/4 full marker indicates the remaining fuel volume is 4.8 × 1/4 = 1.2 liters.",
            DiscussionQuestion = isVN
                ? "Nếu xe đi hết 1.2 lít xăng cho quãng đường 50 km, bình xăng đầy (4.8 lít) sẽ giúp xe đi được quãng đường tối đa là bao nhiêu km?"
                : "If the vehicle travels 50 km on 1.2 liters (1/4 tank), how far can it go on a full tank (4.8 liters)?",
            InteractiveTask = isVN
                ? "Quan sát kim chỉ vạch xăng trên xe của gia đình và ghi nhận phân số chỉ xăng. Tính toán lượng xăng còn lại nếu biết dung tích tối đa của bình chứa."
                : "Check a family vehicle's fuel gauge. Calculate the remaining liters if you know the maximum tank capacity."
        },
        new PracticalAppItem
        {
            Icon = "🎵",
            Title = isVN ? "Nốt nhạc và Nhịp điệu" : "Musical Notes & Rhythm",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_5_{suffix}.png",
            Description = isVN 
                ? "Các nốt nhạc khác nhau biểu thị trường độ dài của âm thanh bằng các phân số." 
                : "Musical notes represent duration values as fraction ratios.",
            Formula = isVN ? "Trường độ nốt nhạc = Giá trị phân số của nốt × Độ dài nhịp phách cơ sở" : "Note Duration = Note Value × Beat Duration",
            MathAnalysis = isVN
                ? "Nốt tròn đại diện cho 1 nhịp nguyên. Nốt trắng bằng 1/2 nốt tròn. Nốt đen bằng 1/4 nốt tròn. Nốt móc đơn bằng 1/8 nốt tròn.\nPhép nhân: Một nhịp phách chứa 4 nốt móc đơn sẽ có độ dài bằng: 4 × 1/8 = 4/8 = 1/2 nhịp nguyên."
                : "A whole note has a value of 1. A half note is 1/2, a quarter note is 1/4, and an eighth note is 1/8.",
            DiscussionQuestion = isVN
                ? "Có bao nhiêu nốt móc đơn (1/8) cần kết hợp lại để có trường độ dài bằng đúng một nốt trắng (1/2)?"
                : "How many eighth notes (1/8) are needed to equal the duration of a half note (1/2)?",
            InteractiveTask = isVN
                ? "Gõ nhịp phách bằng tay theo chuỗi nốt sau: 1 nốt đen (1/4), 2 nốt móc đơn (1/8 + 1/8), và 1 nốt trắng (1/2). Tính tổng trường độ của cả chuỗi."
                : "Clap the rhythm for a quarter note, two eighth notes, and a half note. Calculate the sum of their durations."
        },
        new PracticalAppItem
        {
            Icon = "🗺",
            Title = isVN ? "️ Bản đồ & Tỷ lệ xích" : "Geography & Map Scale",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_6_{suffix}.png",
            Description = isVN 
                ? "Bản đồ địa lý sử dụng phân số tỷ lệ xích để biểu diễn khoảng cách thực tế được thu nhỏ lại trên bản vẽ." 
                : "Map scales express real-world distance reductions using fractional ratios.",
            Formula = isVN ? "Tỷ lệ xích (m) = Khoảng cách đo được trên bản đồ / Khoảng cách thực tế ngoài thực địa" : "Map Scale = Map Distance / Real Distance",
            MathAnalysis = isVN
                ? "Tỷ lệ xích 1/10.000 có nghĩa là 1 cm trên bản đồ đại diện cho 10.000 cm (tương đương 100 mét) ngoài thực tế. Nếu khoảng cách giữa trường học và thư viện trên bản đồ đo được là 5 cm, thì khoảng cách thực tế sẽ là:\n5 cm × 10.000 = 50.000 cm = 500 mét."
                : "With a scale of 1/10,000, 1 cm on the map represents 10,000 cm (100 m) in reality. A map distance of 5 cm equals a real distance of 500 meters.",
            DiscussionQuestion = isVN
                ? "Nếu bản đồ tăng độ chi tiết và đổi tỷ lệ xích thành 1/2.000 thì kích thước của trường học trên bản đồ sẽ to lên hay nhỏ đi? Giải thích tại sao mẫu số nhỏ hơn lại làm hình ảnh to hơn?"
                : "If the scale changes to 1/2,000, will the school's size on the map appear larger or smaller? Explain.",
            InteractiveTask = isVN
                ? "Sử dụng thước kẻ đo khoảng cách từ Town Center đến Green Park trên bản đồ minh họa. Áp dụng tỷ lệ xích 1/10.000 để tính khoảng cách thực tế."
                : "Measure the map distance from Town Center to Green Park. Calculate the real distance using the 1/10,000 scale."
        },
        new PracticalAppItem
        {
            Icon = "📈",
            Title = isVN ? "Phân chia cổ phần công ty" : "Corporate Equity Split",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_7_{suffix}.png",
            Description = isVN 
                ? "Biểu diễn tỷ lệ vốn góp của các cổ đông dưới dạng phân số để thực hiện chia cổ tức và biểu quyết." 
                : "Represent investor share ratios as fractions for dividend distribution and voting.",
            Formula = isVN ? "Tỷ lệ cổ phần = Số cổ phiếu sở hữu / Tổng số cổ phiếu lưu hành" : "Shareholding Ratio = Shares Owned / Total Shares",
            MathAnalysis = isVN
                ? "Công ty có tổng cộng 1.000 cổ phần. Cổ đông A sở hữu 300 cổ phần (tương đương phân số 3/10), cổ đông B sở hữu 500 cổ phần (tương đương phân số 1/2), cổ đông C sở hữu 200 cổ phần (tương đương 1/5).\nTổng cộng cổ phần: 3/10 + 1/2 + 1/5 = 3/10 + 5/10 + 2/10 = 10/10 = 1 (toàn bộ công ty)."
                : "A company has 1,000 total shares. Shareholder A owns 300 shares (3/10), B owns 500 (1/2), and C owns 200 (1/5). Summing these gives 3/10 + 5/10 + 2/10 = 1.",
            DiscussionQuestion = isVN
                ? "Để thông qua một quyết định quan trọng, công ty cần sự đồng ý của các cổ đông sở hữu hơn 1/2 tổng số cổ phần. Cổ đông A và C có thể bắt tay nhau để đạt được tỷ lệ này không?"
                : "A decision requires >1/2 approval. Can shareholders A (3/10) and C (1/5) approve it together by combining their shares?",
            InteractiveTask = isVN
                ? "Giả sử công ty chia mức cổ tức trị giá 200 triệu đồng. Hãy tính số tiền cổ tức mỗi cổ đông A, B, C nhận được dựa trên tỷ lệ phân số cổ phần tương ứng."
                : "Distribute a 200 million VND dividend among shareholders A (3/10), B (1/2), and C (1/5). Calculate each payout."
        },
        new PracticalAppItem
        {
            Icon = "🎶",
            Title = isVN ? "Nhịp phách âm nhạc" : "Musical Time Signatures",
            ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_8_{suffix}.png",
            Description = isVN 
                ? "Sử dụng phân số chỉ nhịp (như 3/4, 4/4) để phân chia trường độ của nốt nhạc trong mỗi ô nhịp." 
                : "Use time signatures (like 3/4, 4/4) to divide beat durations within measures.",
            Formula = isVN ? "Số lượng phách mỗi ô nhịp = Tử số của số chỉ nhịp nhịp; Độ dài phách cơ sở = 1 / Mẫu số" : "Beats per Measure = Numerator; Beat Value = 1 / Denominator",
            MathAnalysis = isVN
                ? "Số chỉ nhịp 3/4 có tử số là 3 và mẫu số là 4. Nghĩa là mỗi ô nhịp có 3 phách, và mỗi phách có giá trị bằng một nốt đen (1/4). Tổng độ dài các nốt nhạc trong một ô nhịp bắt buộc phải bằng: 3 × 1/4 = 3/4 phách tiêu chuẩn."
                : "A 3/4 time signature has a numerator of 3 and a denominator of 4, meaning 3 beats per measure, each valued at a quarter note (1/4).",
            DiscussionQuestion = isVN
                ? "Trong ô nhịp 3/4, em có thể sử dụng đồng thời 1 nốt trắng (1/2) và 2 nốt móc đơn (1/8 + 1/8) được không? Hãy tính tổng phân số của chúng."
                : "In a 3/4 measure, can you place a half note (1/2) and two eighth notes (1/8 + 1/8)? Verify by sum.",
            InteractiveTask = isVN
                ? "Hãy tính tổng trường độ của một ô nhịp chứa: 1 nốt đen (1/4), 1 nốt móc kép (1/16) và 1 nốt đen chấm dôi (3/8). Ô nhịp đó có đủ nhịp 3/4 không?"
                : "Sum the duration values: 1/4 note, 1/16 note, and 3/8 note. Check if they fit in a 3/4 measure."
        }
    };
    ```

---

### Hạng mục 4: Thay thế MessageBox bằng Inline Status/Error Banner
*   **Mô tả yêu cầu:** Loại bỏ hoàn toàn 14 hộp thoại `MessageBox.Show` gây cản trở và cướp tiêu điểm trên màn hình cảm ứng, đồng bộ hóa thông báo lỗi/thành công vào luồng giao diện trực quan của chương trình.
*   **Dữ liệu đầu vào:** Sự kiện click nút bấm hoặc lỗi phát sinh trong tính toán/xuất file.
*   **Dữ liệu đầu ra:** Cập nhật thông điệp lên Banner thông báo nội bộ, đổi màu sắc Banner (Đỏ cho lỗi, Xanh cho thành công, Cam cho cảnh báo) và tự động biến mất sau 3 giây đối với các thông báo thành công.
*   **Phương pháp thực hiện:**
    1.  Trong [FractionTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml):
        Thêm một Panel thông báo lỗi/trạng thái dùng chung ở trên đầu cột nhập liệu (Grid Column 0) hoặc Grid Column 1:
        ```xml
        <!-- Thêm Banner thông báo trạng thái/lỗi inline -->
        <Border x:Name="inlineNotification" Visibility="Collapsed" CornerRadius="8" Padding="12,10" Margin="0,0,0,16" BorderThickness="1">
            <StackPanel Orientation="Horizontal">
                <TextBlock x:Name="txtNotificationIcon" Text="⚠️" FontSize="16" Margin="0,0,8,0" VerticalAlignment="Center"/>
                <TextBlock x:Name="txtNotificationMessage" Text="" FontSize="13.5" FontWeight="SemiBold" TextWrapping="Wrap" VerticalAlignment="Center" Foreground="#333333"/>
            </StackPanel>
        </Border>
        ```
    2.  Tại [FractionTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml.cs):
        *   Tạo hàm trợ giúp để hiển thị thông báo động:
            ```csharp
            private void ShowNotification(string message, string type = "error")
            {
                if (inlineNotification == null || txtNotificationMessage == null || txtNotificationIcon == null) return;
                
                txtNotificationMessage.Text = message;
                if (type == "error")
                {
                    inlineNotification.Background = new SolidColorBrush(Color.FromRgb(255, 235, 235)); // Hồng nhạt
                    inlineNotification.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 83, 80));   // Đỏ
                    txtNotificationIcon.Text = "❌";
                }
                else if (type == "warning")
                {
                    inlineNotification.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)); // Cam nhạt
                    inlineNotification.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 167, 38));  // Cam
                    txtNotificationIcon.Text = "⚠️";
                }
                else
                {
                    inlineNotification.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Xanh nhạt
                    inlineNotification.BorderBrush = new SolidColorBrush(Color.FromRgb(102, 187, 106)); // Xanh lá
                    txtNotificationIcon.Text = "🎉";
                    
                    // Tự động ẩn sau 3 giây đối với thông báo thành công
                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    timer.Tick += (s, args) => { inlineNotification.Visibility = Visibility.Collapsed; timer.Stop(); };
                    timer.Start();
                }
                
                inlineNotification.Visibility = Visibility.Visible;
            }
            ```
        *   Thay thế tất cả các hàm `MessageBox.Show(...)` bằng hàm `ShowNotification(...)`.
            *Ví dụ dòng 808:*
            ```csharp
            // Trước:
            MessageBox.Show("Vui lòng nhập câu trả lời đầy đủ (cả tử số và mẫu số).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            
            // Sau:
            ShowNotification("Vui lòng nhập câu trả lời đầy đủ (cả tử số và mẫu số).", "warning");
            ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Khi người dùng cuộn ScrollViewer, Banner thông báo có bị trôi đi mất không?
    *   *Tối ưu:* Để giải quyết việc trôi thông tin, đặt điều khiển `inlineNotification` ở lớp cha ngoài cùng của cột trái nhập liệu, nằm trên ScrollViewer chính. Điều này giúp Banner luôn cố định ở đầu màn hình, đập ngay vào mắt người dùng bất kể họ đang ở vị trí cuộn nào, mang lại trải nghiệm tối ưu.

---

### Hạng mục 5: Bảo mật lưu trữ kỷ lục điểm số thi đua
*   **Mô tả yêu cầu:** Bảo mật tệp lưu trữ điểm số cao của học sinh, ngăn chặn hành vi sửa đổi thủ công để gian lận điểm rèn luyện thi đua.
*   **Dữ liệu đầu vào:** Điểm số cao `_quizHighScore`.
*   **Dữ liệu đầu ra:** Tệp `fraction_highscore.txt` chứa chuỗi mã hóa băm hoặc lưu trữ trực tiếp vào CSDL SQLite thông qua `DatabaseManager` để đồng bộ an toàn.
*   **Phương pháp thực hiện:**
    1.  Sử dụng cơ chế mã hóa đối xứng AES đơn giản hoặc lưu trữ băm kèm chuỗi muối (Salt) để tự đối chiếu tính toàn vẹn khi load tệp:
        ```csharp
        private void SaveHighScore()
        {
            try
            {
                System.IO.Directory.CreateDirectory(global::QASmartClass.Services.AppPaths.SettingsDir);
                string path = System.IO.Path.Combine(global::QASmartClass.Services.AppPaths.SettingsDir, "fraction_highscore.txt");
                string scoreStr = _quizHighScore.ToString();
                string salt = "QASmartClass_Salt_2026";
                string hash = GetSha256(scoreStr + salt);
                
                // Lưu điểm số và chuỗi băm để đối chiếu
                System.IO.File.WriteAllText(path, $"{scoreStr}|{hash}");
            }
            catch { }
        }

        private void LoadHighScore()
        {
            try
            {
                string path = System.IO.Path.Combine(global::QASmartClass.Services.AppPaths.SettingsDir, "fraction_highscore.txt");
                if (System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path);
                    var parts = content.Split('|');
                    if (parts.Length == 2)
                    {
                        string scoreStr = parts[0];
                        string hash = parts[1];
                        string salt = "QASmartClass_Salt_2026";
                        if (hash == GetSha256(scoreStr + salt))
                        {
                            if (int.TryParse(scoreStr, out int hs))
                            {
                                _quizHighScore = hs;
                                txtQuizHighScore.Text = $"Kỷ lục cao nhất: {_quizHighScore} 👑";
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static string GetSha256(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
        ```
*   **Phương pháp phản biện:**
    *   *Phản biện:* Học sinh có thể sửa cả điểm và tự tính mã hash SHA-256 mới để ghi đè không?
    *   *Phản biện đáp trả:* Về lý thuyết là có thể nếu học sinh biết được chuỗi muối `salt` bí mật. Tuy nhiên, chuỗi muối được nhúng cứng (hardcoded) trong code biên dịch DLL, việc giải ngược mã máy để tìm chuỗi muối nằm ngoài năng lực của 99.9% học sinh cấp Tiểu học và THCS. Đây là giải pháp an ninh gọn nhẹ, thực tiễn và không làm phức tạp hóa hệ thống.

---

### Hạng mục 6: Đồng bộ hóa Font Segoe UI và thích ứng cấp học v4.1
*   **Mô tả yêu cầu:** Đồng bộ phông chữ hệ thống để đảm bảo kết xuất tiếng Việt hoàn hảo và lọc phím số âm khi chạy trên cấu hình Tiểu học.
*   **Dữ liệu đầu vào:** Cấu hình cấp học từ hệ thống.
*   **Dữ liệu đầu ra:** Vô hiệu hóa phím âm `-` trên bàn phím ảo và loại bỏ preset chứa số âm đối với học sinh Tiểu học.
*   **Phương pháp thực hiện:**
    1.  Tại [FractionTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml):
        Thiết lập thuộc tính `FontFamily="Segoe UI"` ngay tại thẻ gốc `<controls:BaseToolControl ...>` ở dòng 1.
    2.  Tại [FractionTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Math/FractionTool.xaml.cs#L53):
        Kiểm tra cấu hình cấp học và cấu hình bàn phím ảo tương ứng:
        ```csharp
        bool isPrimary = global::QASmartClass.Services.SettingsManager.CurrentGradeLevel == GradeLevel.Primary;
        
        // Cấu hình bàn phím ảo không cho phép số âm nếu là cấp Tiểu học
        TouchNumPad.Attach(txtN1, step: 1, min: isPrimary ? 0 : -100);
        TouchNumPad.Attach(txtD1, step: 1, min: 1);
        TouchNumPad.Attach(txtN2, step: 1, min: isPrimary ? 0 : -100);
        TouchNumPad.Attach(txtD2, step: 1, min: 1);
        TouchNumPad.Attach(txtSN, step: 1, min: isPrimary ? 0 : -100);
        TouchNumPad.Attach(txtSD, step: 1, min: 1);
        ```

---

## ═══ PHẦN 5: KẾ HOẠCH KIỂM THỬ THẨM ĐỊNH (VERIFICATION PLAN) ═══

Để kiểm tra độ chính xác và tính tuân thủ sau khi coder thực hiện sửa đổi, bộ phận QA/QC cần thực hiện checksheet kiểm định từng bước sau:

### Checksheet Kiểm thử chức năng & Giao diện (Mức Coder & Tester)

- [ ] **Hạng mục 1 & 2: Kiểm tra Layout chống trống**
    - [ ] [ ] Biên dịch dự án thành công không phát sinh lỗi tham chiếu rỗng ở các công cụ khác.
    - [ ] [ ] Mở tab "Ứng dụng thực tế" -> Chọn đề mục "Bản đồ & Tỷ lệ xích".
    - [ ] [ ] Xác nhận ảnh bản đồ hiển thị ở cột trái và **Thẻ khái niệm sư phạm màu vàng** xuất hiện ngay phía dưới, lấp đầy khoảng trống đứng.
    - [ ] [ ] Xác nhận ở cột phải xuất hiện đầy đủ 3 Card thông tin màu: Phân tích Toán học (Xanh dương), Câu hỏi gợi mở (Cam), Nhiệm vụ thực hành (Xanh lá) với font chữ Segoe UI sắc nét, không bị xén chữ.
    - [ ] [ ] Cuộn ScrollViewer cột phải lên xuống độc lập để xác nhận thanh cuộn hoạt động bình thường, không làm phình to chiều cao cửa sổ.

- [ ] **Hạng mục 3: Kiểm tra Localization & Tính đầy đủ của 8 ứng dụng**
    - [ ] [ ] Chuyển đổi ngôn ngữ phần mềm sang tiếng Việt -> Nhấp lần lượt qua cả 8 đề mục ứng dụng thực tế.
    - [ ] [ ] Xác nhận 100% nội dung (Mô tả, Phân tích, Câu hỏi, Nhiệm vụ, Công thức) hiển thị hoàn toàn bằng tiếng Việt, không bị trộn lẫn tiếng Anh.
    - [ ] [ ] Chuyển đổi ngôn ngữ phần mềm sang tiếng Anh -> Xác nhận giao diện hiển thị đúng tiếng Anh tương ứng.

- [ ] **Hạng mục 4: Thử nghiệm lỗi & Kháng chặn luồng (No MessageBox)**
    - [ ] [ ] Tại Tab 1 (Operations) -> Xóa trống ô Tử số -> Bấm "Xem trên đồ thị Graph".
    - [ ] [ ] Xác nhận **không xuất hiện hộp thoại MessageBox**. Thay vào đó, Banner đỏ xuất hiện ở đầu Panel nhập liệu hiển thị nội dung lỗi tương ứng.
    - [ ] [ ] Tại Tab 3 (Quiz) -> Nhấn kiểm tra đáp án trống -> Xác nhận lỗi được in ra inline ở feedback panel một cách mềm mại.

- [ ] **Hạng mục 5: Kiểm định Bảo mật điểm cao**
    - [ ] [ ] Chơi game trắc nghiệm đạt 50 điểm để tạo kỷ lục điểm mới. Tắt phần mềm.
    - [ ] [ ] Tìm đến tệp `fraction_highscore.txt` trong thư mục AppData -> Xác nhận nội dung tệp lưu trữ dưới định dạng băm mã hóa bảo vệ (ví dụ: `50|3a4fbc...`).
    - [ ] [ ] Cố tình sửa điểm số `50` thành `9999` bằng Notepad rồi lưu lại -> Khởi động lại ứng dụng -> Xác nhận hệ thống phát hiện sửa đổi bất hợp pháp và reset điểm cao về 0 hoặc giữ nguyên điểm cũ chứ không load điểm gian lận.

- [ ] **Hạng mục 6: Kiểm thử Dynamic UI theo cấp học**
    - [ ] [ ] Cấu hình hệ thống ở chế độ "Primary" (Tiểu học) -> Xác nhận các phím âm `-` trên bàn phím ảo bị khóa cứng và các preset ví dụ chứa số âm không xuất hiện.
