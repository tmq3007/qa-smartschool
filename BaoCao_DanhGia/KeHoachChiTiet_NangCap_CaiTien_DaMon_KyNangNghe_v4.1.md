# KẾ HOẠCH NÂNG CẤP VÀ CẢI TIẾN CHI TIẾT CÔNG CỤ ĐA MÔN & KỸ NĂNG NGHỀ (BẢN 4.1)

*Tài liệu phân tích hệ thống, phương án mã nguồn tối ưu và checksheet kiểm thử QA/QC dành cho phân hệ Đa môn (Multi-disciplinary) và Kỹ năng nghề / Quản lý (Workplace/Vocational)*
*Được lập bởi Hội đồng chuyên gia: Trưởng bộ phận thiết kế QA Smart School, Quản lý IT, Chuyên gia kiểm thử, Chuyên gia thiết kế UI/UX, Chuyên gia phân tích hệ thống & CSDL, Chuyên gia thiết bị ngoại vi, An ninh mạng, các Nhà giáo dục và đại diện Học sinh/Giáo viên ưu tú.*

---

## 🎯 MỤC TIÊU & PHẠM VI
Đồng bộ hóa giao diện và logic kỹ thuật của các **Công cụ học tập Đa môn & Công cụ chung** (như Sơ đồ tư duy, Bảng công thức, Dòng lịch sử, Bảng tuần hoàn, Luyện trí tuệ...) và các **Công cụ Kỹ năng nghề/Quản lý** (như Eisenhower, SWOT, PDCA, 5S, Pareto...) theo bộ quy chuẩn thiết kế sư phạm và kỹ thuật **QA SmartClass v4.1**.

**Các yêu cầu trọng tâm:**
1.  **Typography**: Loại bỏ triệt để phông chữ cứng `Segoe UI` để chuyển sang phông chữ thương hiệu động `Inter` (cho nội dung) và `Outfit` (cho tiêu đề) nhằm hiển thị tiếng Việt sắc nét trên mọi độ phân giải.
2.  **Layout & Responsive**: Khắc phục hiện tượng tràn viền, đè chèn nút bấm trên màn hình nhỏ và xử lý triệt để việc mất góc/cắt hình khi chuyển nội dung sang Bảng trắng SmartScreen.
3.  **Sư phạm & Chỉ dẫn**: Bổ sung chỉ dẫn sử dụng từng bước (Guided Steps) trực quan và sửa các lỗi logic tính toán hoặc nhập liệu số thập phân trên màn hình cảm ứng.
4.  **Ngôn ngữ**: Đảm bảo 100% tiếng Việt chuẩn hóa học đường, sư phạm cao.

---

## 📂 CẤU TRÚC THƯ MỤC & CÁC TỆP TIN ẢNH HƯỞNG
Lập trình viên cần kiểm tra và chỉnh sửa chính xác trên các tệp tin sau:
1.  **Công cụ Đa môn & Công cụ chung (Multi-disciplinary/General Tools):**
    *   [BrainstormTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/BrainstormTool.xaml) & [BrainstormTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/BrainstormTool.xaml.cs) (Động não nhanh)
    *   [FormulasTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/FormulasTool.xaml.cs) (Bảng công thức đa môn)
    *   [HistoryTimelineTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/HistoryTimelineTool.xaml) (Trục lịch sử Việt Nam & Thế giới)
    *   [MathSymbolsTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MathSymbolsTool.xaml) & [MathSymbolsTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MathSymbolsTool.xaml.cs) (Ký hiệu toán học)
    *   [MindmapTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MindmapTool.xaml) & [MindmapTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MindmapTool.xaml.cs) (Sơ đồ tư duy học tập)
    *   [PeriodicTableTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/PeriodicTableTool.xaml.cs) (Bảng tuần hoàn các nguyên tố hóa học)
    *   [PhysicsSandboxTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/PhysicsSandboxTool.xaml) & [PhysicsSandboxTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/PhysicsSandboxTool.xaml.cs) (Mô phỏng quang học/cơ học)
    *   [PlanetsTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/PlanetsTool.xaml) & [PlanetsTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/PlanetsTool.xaml.cs) (Hệ mặt trời 3D)
2.  **Công cụ Tư duy & Luyện não (Thinking Tools):**
    *   [IqQuizTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/IqQuizTool.xaml.cs) (Luyện trí thông minh IQ)
    *   [MentalMathTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/MentalMathTool.xaml) & [MentalMathTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/MentalMathTool.xaml.cs) (Tính nhẩm nhanh)
3.  **Công cụ Kỹ năng nghề & Quản lý dự án (Workplace/Vocational Tools):**
    *   [EisenhowerTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Workplace/EisenhowerTool.xaml) (Ma trận thời gian quản lý việc học)
4.  **Thành phần dùng chung & Kiểm soát đầu vào (Shared Core Controls):**
    *   [TeachingActionHelper.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Helpers/TeachingActionHelper.cs) (Hỗ trợ nạp bảng trắng & tương tác học đường)
    *   [TouchNumPad.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Controls/TouchNumPad.cs) (Bàn phím số cảm ứng tích hợp)

---

## ═══ CHI TIẾT 6 HẠNG MỤC CẢI TIẾN & ĐÁNH GIÁ CHUYÊN SÂU ═══

### Hạng mục 1: Sửa lỗi chụp mất chữ/cắt góc khi đưa thẻ công thức lên Bảng trắng (Loi_57)
*   **Hiện tượng lỗi**: Khi giáo viên nhấp nút 🖊️ trên thanh công cụ hover của thẻ công thức trong `FormulasTool` để gửi lên bảng trắng SmartScreen, hình ảnh hiển thị bị cắt lề dưới hoặc mất thông tin phần đuôi nếu thẻ có kích thước dài.
*   **Nguyên nhân kỹ thuật**: 
    1.  Hàm `RenderVisualUnclipped` trong [TeachingActionHelper.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Helpers/TeachingActionHelper.cs#L1140) giới hạn chiều cao tối đa ở mức cứng `height = width * 4.5`. Khi thẻ công thức chứa nhiều văn bản hoặc phân tích thực tiễn chi tiết, tỷ lệ này bị vượt quá gây mất phần đuôi.
    2.  Hơn thế nữa, khi gọi `element.Arrange()` lên phần tử đang liên kết trực tiếp trong Visual Tree, bộ lọc layout của container cha (ví dụ `WrapPanel` hoặc `Grid` có `ClipToBounds=True`) vẫn áp đặt vùng cắt lên phần tử trong quá trình dựng `VisualBrush`, dẫn tới ảnh chụp bị cắt góc.
*   **Giải pháp tối ưu**:
    *   **Tách biệt phần tử khi render (Isolate & Render)**: Tạm thời ngắt liên kết (Disconnect) giữa thẻ công thức và container cha của nó trước khi thực hiện đo đạc (`Measure`) và sắp đặt (`Arrange`) độc lập. Sau khi xuất ảnh `RenderTargetBitmap` sắc nét thành công, khôi phục lại vị trí của thẻ trong Visual Tree. Việc này giúp phần tử được render 100% trọn vẹn mà không bị ảnh hưởng bởi bất kỳ thuộc tính clip nào từ cha.
    *   **Nâng giới hạn chiều cao**: Tăng hệ số cắt chiều cao từ `4.5` lên `10.0` lần chiều rộng để đáp ứng trọn vẹn các thẻ công thức phức tạp nhất.

*Đoạn mã đề xuất nâng cấp trong [TeachingActionHelper.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Helpers/TeachingActionHelper.cs#L1115):*
```csharp
public static RenderTargetBitmap? RenderVisualUnclipped(FrameworkElement element, double scale = 2.0)
{
    var svStates = new System.Collections.Generic.List<ScrollBarState>();
    var sbStates = new System.Collections.Generic.List<ScrollBarVisibilityState>();
    
    // 1. Lưu giữ và tạm thời ngắt liên kết cha để tránh clipping
    DependencyObject? parent = element.Parent ?? VisualTreeHelper.GetParent(element);
    Panel? parentPanel = parent as Panel;
    ContentControl? parentContent = parent as ContentControl;
    Decorator? parentDecorator = parent as Decorator;
    
    int childIndex = -1;
    object? oldContent = null;
    UIElement? oldChild = null;
    
    try
    {
        if (parentPanel != null)
        {
            childIndex = parentPanel.Children.IndexOf(element);
            if (childIndex >= 0) parentPanel.Children.RemoveAt(childIndex);
        }
        else if (parentContent != null)
        {
            oldContent = parentContent.Content;
            parentContent.Content = null;
        }
        else if (parentDecorator != null)
        {
            oldChild = parentDecorator.Child;
            parentDecorator.Child = null;
        }

        double width = element.ActualWidth;
        if (double.IsNaN(width) || width < 1) width = element.Width;
        if (double.IsNaN(width) || width < 10) width = 350;

        CollectAndHideScrollBars(element, svStates);
        CollectAndHideIndividualScrollBars(element, sbStates);

        // 2. Đo đạc kích thước thực trong trạng thái độc lập
        element.Measure(new Size(width, double.PositiveInfinity));
        double height = element.DesiredSize.Height;

        if (double.IsNaN(height) || height < 10) height = element.ActualHeight;
        if (double.IsNaN(height) || height < 10) height = element.Height;
        if (double.IsNaN(height) || height < 10) height = 200;

        // Tăng giới hạn chiều cao lên 10.0 lần chiều rộng
        if (height > width * 10.0)
        {
            height = width * 10.0;
        }

        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            double radiusX = 0, radiusY = 0;
            if (element is Border border)
            {
                radiusX = border.CornerRadius.TopLeft;
                radiusY = border.CornerRadius.TopLeft;
            }

            var rect = new Rect(0, 0, width, height);
            dc.DrawRoundedRectangle(Brushes.White, null, rect, radiusX, radiusY);

            var brush = new VisualBrush(element)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, width, height)
            };
            dc.DrawRectangle(brush, null, rect);
        }

        var finalRtb = new RenderTargetBitmap(
            (int)(width * scale), (int)(height * scale),
            96 * scale, 96 * scale, PixelFormats.Pbgra32);
        finalRtb.Render(visual);

        return finalRtb;
    }
    catch (Exception ex)
    {
        Log.Warning("RenderVisualUnclipped error: {Err}", ex.Message);
        return null;
    }
    finally
    {
        // 3. Khôi phục lại phần tử về vị trí cũ trong Visual Tree ngay lập tức
        if (parentPanel != null && childIndex >= 0)
        {
            if (!parentPanel.Children.Contains(element))
                parentPanel.Children.Insert(childIndex, element);
        }
        else if (parentContent != null)
        {
            parentContent.Content = oldContent;
        }
        else if (parentDecorator != null)
        {
            parentDecorator.Child = oldChild;
        }

        element.InvalidateMeasure();
        element.InvalidateArrange();
        if (parent is UIElement parentElement)
        {
            parentElement.InvalidateMeasure();
            parentElement.InvalidateArrange();
        }
        element.UpdateLayout();

        foreach (var state in svStates)
        {
            state.ScrollViewer.HorizontalScrollBarVisibility = state.HorizontalVisibility;
            state.ScrollViewer.VerticalScrollBarVisibility = state.VerticalVisibility;
        }
        foreach (var state in sbStates)
        {
            state.Element.Visibility = state.Visibility;
        }
    }
}
```

---

### Hạng mục 2: Tái cấu trúc thanh công cụ Sơ đồ tư duy tránh đè lấn nút bấm (Loi_58)
*   **Góc nhìn UI/UX**: Trong [MindmapTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MindmapTool.xaml#L22), toàn bộ thanh công cụ được đặt chung trong một thẻ `<WrapPanel>`. Khi màn hình thu nhỏ (đặc biệt trên tablet học sinh hoặc màn hình tương tác tỷ lệ 4:3), nút "📚 Văn học" và các template mẫu bị đẩy xuống dòng và chèn ép, đè lấp lên các nút tiện ích thao tác hệ thống ở phía sau như "🔀 Đổi cấu trúc", "🎨 Đổi màu", tạo nên giao diện lộn xộn, mất thẩm mỹ sư phạm.
*   **Giải pháp nâng cấp**: Thay thế `WrapPanel` đơn lẻ bằng một `Grid` phân chia rõ ràng làm 2 cột:
    *   **Cột 0 (Trái)**: Chứa các công cụ vẽ sơ đồ và template học tập (các nút tác động nội dung).
    *   **Cột 1 (Phải)**: Chứa các công cụ chung của hệ thống (lưu, mở, xuất ảnh, xóa tất cả) được căn phải gọn gàng.
    Nếu chiều rộng co hẹp, các nút bấm trong từng cột sẽ tự xuống dòng một cách độc lập trong phạm vi khu vực của mình mà không bao giờ chèn lấn lẫn nhau.

*Đoạn mã XAML đề xuất tái cấu trúc trong [MindmapTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Multi/MindmapTool.xaml#L21-L85):*
```xml
<!-- === TOOLBAR RESTRUCTURED === -->
<Border Grid.Row="0" Background="#F8F9FA" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Left: Edit Tools & Templates -->
        <WrapPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center" Padding="12,8,0,8">
            <TextBlock Text="🧠 Sơ đồ tư duy" FontSize="16" FontWeight="Bold" Foreground="#333" VerticalAlignment="Center" Margin="0,0,16,6"/>
            <Button x:Name="btnAddCenter" Content="🎯 Chủ đề trung tâm" Padding="10,6" Margin="0,0,8,6" FontSize="12" FontWeight="SemiBold" Cursor="Hand" Click="AddCenter_Click" ToolTip="Tạo node gốc ở giữa"/>
            <Button x:Name="btnAddChild" Content="➕ Thêm nhánh" Padding="10,6" Margin="0,0,8,6" FontSize="12" Cursor="Hand" Click="AddChild_Click" ToolTip="Thêm nhánh con cho node đang chọn" IsEnabled="False"/>
            <Button x:Name="btnDeleteNode" Content="❌ Xóa nhánh" Padding="10,6" Margin="0,0,8,6" FontSize="12" Cursor="Hand" Click="DeleteNode_Click" ToolTip="Xóa nhánh đang chọn" IsEnabled="False"/>

            <Border Height="24" Width="1" Background="#D0D0D0" Margin="4,0,8,6" VerticalAlignment="Center"/>
            <ToggleButton x:Name="btnEmojiToggle" Content="😊 Biểu tượng ▾" Padding="8,5" Margin="0,0,8,6" FontSize="12" Cursor="Hand" ToolTip="Mở bảng chọn biểu tượng"/>
            <!-- Popup popEmoji giữ nguyên -->
            <Popup x:Name="popEmoji" IsOpen="{Binding IsChecked, ElementName=btnEmojiToggle}" StaysOpen="False" PlacementTarget="{Binding ElementName=btnEmojiToggle}" Placement="Bottom" Margin="0,4,0,0">
                <!-- Nội dung Popup giữ nguyên -->
            </Popup>

            <Border Height="24" Width="1" Background="#D0D0D0" Margin="8,0,8,6" VerticalAlignment="Center"/>
            <TextBlock Text="Template mẫu:" FontSize="12" FontWeight="SemiBold" Foreground="#757575" VerticalAlignment="Center" Margin="0,0,6,6"/>
            <Button Content="📚 Văn học" Padding="8,5" Margin="0,0,4,6" FontSize="11" Cursor="Hand" Click="Template_Literature" ToolTip="Phân tích tác phẩm văn học"/>
            <Button Content="🔬 STEM" Padding="8,5" Margin="0,0,4,6" FontSize="11" Cursor="Hand" Click="Template_STEM" ToolTip="Tóm tắt bài học STEM"/>
            <Button Content="📋 Dự án" Padding="8,5" Margin="0,0,4,6" FontSize="11" Cursor="Hand" Click="Template_Project" ToolTip="Lập kế hoạch dự án"/>
            <Button Content="🏛️ Lịch sử" Padding="8,5" Margin="0,0,4,6" FontSize="11" Cursor="Hand" Click="Template_History" ToolTip="Tóm tắt sự kiện lịch sử"/>
            <Button Content="📐 Toán học" Padding="8,5" Margin="0,0,4,6" FontSize="11" Cursor="Hand" Click="Template_Math" ToolTip="Phân loại tư duy Toán học"/>
            <Button Content="🗓️ Kế hoạch" Padding="8,5" Margin="0,0,8,6" FontSize="11" Cursor="Hand" Click="Template_Weekly" ToolTip="Lập kế hoạch học tập tuần"/>

            <Border Height="24" Width="1" Background="#D0D0D0" Margin="4,0,8,6" VerticalAlignment="Center"/>
            <Button Content="📂 Thư viện 20 mẫu" Padding="10,5" FontSize="11" FontWeight="SemiBold" Cursor="Hand" Click="OpenTemplateGallery_Click" ToolTip="Mở thư viện 20 mẫu sơ đồ tư duy" Foreground="#1976D2" Margin="0,0,12,6"/>
        </WrapPanel>

        <!-- Right: Actions/Canvas Commands -->
        <WrapPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center" HorizontalAlignment="Right" Padding="0,8,12,8">
            <Button x:Name="btnToggleLayout" Content="🔀 Đổi cấu trúc" Padding="10,6" Margin="0,0,6,6" FontSize="12" Cursor="Hand" Click="ToggleLayout_Click" ToolTip="Chuyển đổi kiểu sắp xếp"/>
            <Button x:Name="btnToggleStyle" Content="🖌️ Đổi nét vẽ" Padding="10,6" Margin="0,0,6,6" FontSize="12" Cursor="Hand" Click="ToggleStyle_Click" ToolTip="Chuyển đổi kiểu nét nối"/>
            <Button x:Name="btnChangeColor" Content="🎨 Đổi màu" Padding="10,6" Margin="0,0,6,6" FontSize="12" Cursor="Hand" Click="ChangeColor_Click" ToolTip="Đổi màu nhánh"/>
            <Button x:Name="btnSave" Content="💾 Lưu" Padding="10,6" Margin="0,0,6,6" FontSize="12" Cursor="Hand" Click="Save_Click" ToolTip="Lưu sơ đồ vào database"/>
            <Button x:Name="btnLoad" Content="📂 Mở" Padding="10,6" Margin="0,0,16,6" FontSize="12" Cursor="Hand" Click="Load_Click" ToolTip="Mở sơ đồ đã lưu"/>
            <Button x:Name="btnExport" Content="📸 Xuất ảnh" Padding="10,6" Margin="0,0,6,6" FontSize="12" Cursor="Hand" Click="Export_Click" ToolTip="Xuất sơ đồ thành ảnh"/>
            <Button x:Name="btnClearAll" Content="🗑️ Xóa tất cả" Padding="10,6" Margin="0,0,0,6" FontSize="12" Cursor="Hand" Click="ClearAll_Click" ToolTip="Xóa toàn bộ sơ đồ"/>
        </WrapPanel>
    </Grid>
</Border>
```

---

### Hạng mục 3: Tích hợp Bảng trắng cho công cụ Tính nhẩm nhanh (Loi_59 & Loi_60)
*   **Góc nhìn Nhà giáo dục & Chuyên gia sư phạm**: Trong giờ học toán tính nhẩm hoặc tư duy IQ, học sinh và giáo viên rất cần một bảng nháp phụ để thực hiện tính toán trung gian trước khi chọn đáp án. Hiện tại, `MentalMathTool` hoàn toàn thiếu khả năng tương tác để giáo viên chụp câu hỏi gửi lên bảng vẽ lớn SmartScreen.
*   **Giải pháp nâng cấp**: 
    1.  Tại tệp [MentalMathTool.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/MentalMathTool.xaml#L178): Gán tên cho StackPanel chứa vùng câu hỏi hoạt động: `<StackPanel x:Name="spQuestionArea" Grid.Row="1" ...>`.
    2.  Tại tệp [MentalMathTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/MentalMathTool.xaml.cs#L20): Cho lớp kế thừa interface `IWhiteboardCaptureProvider` để định nghĩa vùng cần chụp xuất lên bảng vẽ chung.

*Đoạn mã đề xuất trong [MentalMathTool.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Views/Thinking/MentalMathTool.xaml.cs):*
```csharp
// Thay đổi định nghĩa class:
public partial class MentalMathTool : BaseToolControl, IWhiteboardCaptureProvider
{
    // Bổ sung phương thức GetWhiteboardBitmapAsync của IWhiteboardCaptureProvider:
    public System.Threading.Tasks.Task<System.Windows.Media.Imaging.BitmapSource?> GetWhiteboardBitmapAsync()
    {
        // Chụp độc lập vùng câu hỏi phép tính (spQuestionArea)
        var rtb = TeachingActionHelper.RenderVisualUnclipped(spQuestionArea, 2.0);
        return System.Threading.Tasks.Task.FromResult<System.Windows.Media.Imaging.BitmapSource?>(rtb);
    }
    
    // (Giữ nguyên các mã nguồn khác của MentalMathTool)
}
```

---

### Hạng mục 4: Khắc phục lỗi nhập dấu phẩy thập phân không nhất quán trên bàn phím cảm ứng (Loi_61)
*   **Nguyên nhân kỹ thuật**:
    1.  Tại [TouchNumPad.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Controls/TouchNumPad.cs#L555), khi giáo viên nhấp nút tăng/giảm (`▲`/`▼`), giá trị được định dạng cứng bằng `CultureInfo.InvariantCulture`, cho ra dấu phân cách thập phân là dấu chấm `.` (ví dụ: `1.5`).
    2.  Tuy nhiên, khi người dùng nhấp nút dấu chấm `.` trực tiếp trên bàn phím ảo, hàm `AppendText` lại tự động chuyển đổi ký tự dựa trên cài đặt quốc gia hệ điều hành (`CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator`). Nếu hệ điều hành chạy ngôn ngữ Tiếng Việt, dấu phân cách chuyển thành dấu phẩy `,` (ví dụ: `1,`).
    3.  Sự xung đột này khiến TextBox vừa chứa dấu chấm vừa chứa dấu phẩy, dẫn tới hàm `double.TryParse` báo lỗi sai định dạng số, khóa chết nút điều hướng tăng giảm và làm lỗi logic tính toán của các công cụ Khoa học (Density, Lens, PhScale, WaveSpeed).
*   **Giải pháp tối ưu**:
    Đồng bộ hóa toàn bộ quy trình sinh chuỗi hiển thị và tính toán trong `TouchNumPad.cs` dựa trên `CurrentCulture` để khớp với bàn phím của Windows, đồng thời cho phép `ParsingHelper.TryParseDouble` tự động chuẩn hóa dấu phẩy thành dấu chấm trước khi tính toán.

*Mã nguồn đề xuất nâng cấp trong [TouchNumPad.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/LearningTools/Controls/TouchNumPad.cs):*
```csharp
// 1. Trong hàm AppendText (dòng 489):
private static void AppendText(string text)
{
    if (_currentTarget == null) return;

    if (text == ".")
    {
        if (!_allowDecimal) return;
        // Kiểm tra chứa cả dấu chấm và dấu phẩy để tránh nhập đúp phân tách thập phân
        if (_currentTarget.Text.Contains(".") || _currentTarget.Text.Contains(",")) return;
        text = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
    }

    var current = _currentTarget.Text ?? "";
    _currentTarget.Text = current + text;
    _currentTarget.CaretIndex = _currentTarget.Text.Length;
    _currentTarget.Focus();
}

// 2. Trong hàm IncrementValue (dòng 535):
private static void IncrementValue(double delta)
{
    if (_currentTarget == null) return;

    double current = 0;
    if (!string.IsNullOrWhiteSpace(_currentTarget.Text))
    {
        // Thay thế dấu phẩy thành dấu chấm để double.TryParse chuẩn Invariant luôn đọc được
        if (!double.TryParse(_currentTarget.Text.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out current))
            return;
    }

    current += delta;
    current = Math.Max(_min, Math.Min(_max, current));

    if (Math.Abs(_step - Math.Floor(_step)) < 0.0001)
        _currentTarget.Text = ((int)Math.Round(current)).ToString();
    else
        // Sử dụng CurrentCulture thay vì InvariantCulture để đầu ra đồng bộ dấu thập phân (dấu phẩy ở VN, dấu chấm ở US)
        _currentTarget.Text = current.ToString("G10",
            System.Globalization.CultureInfo.CurrentCulture);

    _currentTarget.CaretIndex = _currentTarget.Text.Length;
    _currentTarget.Focus();
}
```

---

### Hạng mục 5: Loại bỏ triệt để phông chữ Segoe UI cứng ở 8 công cụ Đa môn (Brand Alignment)
*   **Hiện tượng lỗi**: Các công cụ `BrainstormTool`, `FormulasTool`, `HistoryTimelineTool`, `MathSymbolsTool`, `MindmapTool`, `PeriodicTableTool`, `PhysicsSandboxTool` và `PlanetsTool` chứa các thuộc tính cài đặt cứng `FontFamily="Segoe UI"` trong giao diện XAML hoặc mã code-behind C#. Việc này đè lên các tài nguyên phông chữ hệ thống `InterFont` và `OutfitFont` đã được cài đặt đồng bộ toàn cục tại `InterOutfitFonts.xaml`, gây vỡ thiết kế thương hiệu cao cấp của bản 4.1.
*   **Giải pháp khắc phục**:
    1.  **Trong XAML**: Loại bỏ thuộc tính `FontFamily="Segoe UI"` để tự động kế thừa font hệ thống, hoặc liên kết động qua resource: `FontFamily="{DynamicResource InterFont}"` (văn bản thường) hoặc `FontFamily="{DynamicResource OutfitFont}"` (tiêu đề/tab).
    2.  **Trong C#**: Xóa bỏ các khai báo dạng `FontFamily = new FontFamily("Segoe UI")`. Thay bằng việc sử dụng phông chữ kế thừa từ control cha hoặc gọi tài nguyên động:
        `FontFamily = (FontFamily)Application.Current.FindResource("InterFont")`.

*Danh sách các tệp tin cần loại bỏ Segoe UI:*
*   **BrainstormTool.xaml (L11)**: Loại bỏ `<Setter Property="FontFamily" Value="Segoe UI"/>`
*   **BrainstormTool.xaml.cs (L410, L430)**: Đổi thành `FontFamily = (FontFamily)Application.Current.FindResource("InterFont")`
*   **FormulasTool.xaml.cs (L391, L945, L1005, L1236)**: Loại bỏ `FontFamily = new FontFamily("Segoe UI")` để TextBox/TextBlock tự động nhận font Inter từ hệ thống.
*   **HistoryTimelineTool.xaml (L12, L37)**: Loại bỏ `<Setter Property="FontFamily" Value="Segoe UI"/>`
*   **MathSymbolsTool.xaml (L14)**: Loại bỏ `<Setter Property="FontFamily" Value="Segoe UI"/>`
*   **MathSymbolsTool.xaml.cs (L379, L438)**: Loại bỏ `FontFamily = new FontFamily("Segoe UI")`
*   **MindmapTool.xaml.cs (L302, L1352, L1619, L1695)**: Xóa gán `Segoe UI` trong code-behind.
*   **PeriodicTableTool.xaml.cs (L46, L126, L175, L181, L201)**: Loại bỏ gán font cứng Segoe UI khi sinh động các ô nguyên tố.
*   **PhysicsSandboxTool.xaml (L35)** & **PhysicsSandboxTool.xaml.cs (L367)**: Thay thế Segoe UI bằng cách kế thừa font hệ thống.
*   **PlanetsTool.xaml.cs (L685)**: Loại bỏ `FontFamily = new FontFamily("Segoe UI")` khi dựng nhãn thông tin hành tinh.

---

### Hạng mục 6: Đánh giá bố cục, màu sắc và lỗ hổng sư phạm ở các công cụ Kỹ năng nghề
*   **Giao diện & Khoảng trống**:
    *   **NoiseMonitorTool**: Thiếu `ScrollViewer` bọc ngoài bảng điều khiển thiết lập mức âm lượng nhạy của mic. Trên các màn hình máy tính bảng học sinh có độ phân giải đứng, panel cấu hình bị tràn mép dưới và che khuất nút "Lưu cấu hình". Cần bổ sung `ScrollViewer` bao ngoài `StackPanel` cấu hình.
    *   **PlanetsTool**: Popup chi tiết thông tin các hành tinh (Planets Detail Overlay) có chiều cao cố định và chứa nhiều văn bản giới thiệu khoa học vũ trụ bằng tiếng Việt. Khi học sinh mở trên màn hình nhỏ, phần mô tả bị cắt lửng không xem được hết. Cần bọc vùng mô tả hành tinh vào một thẻ `<ScrollViewer VerticalScrollBarVisibility="Auto">`.
    *   **SwotTool & PdcaTool & FiveSTool**: Các màu sắc chủ đạo của các ô phân tích (Strengths, Weaknesses... hoặc Plan, Do, Check, Act) đang dùng màu thuần gốc chói mắt (pure green, red). Cần điều chỉnh sang dải màu Pastel dịu mắt, tăng độ tương phản của chữ (High-contrast) để học sinh cận thị ngồi xa màn hình tương tác vẫn đọc rõ.
*   **Logic sư phạm & Quy chuẩn v4.1**:
    *   Các công cụ kỹ năng nghề (Eisenhower, SWOT, PDCA, 5S) cần được tích hợp **Banner chỉ dẫn từng bước (Guided Steps Banner)** ở phía trên để học sinh biết cách thao tác độc lập (ví dụ: Bước 1: Nhập công việc -> Bước 2: Kéo thả vào ô ma trận -> Bước 3: Đặt hạn chót -> Bước 4: Tích chọn hoàn thành để nổ pháo hoa).

---

## ═══ CHỈ DẪN SƯ PHẠM VÀ HƯỚNG DẪN SỬ DỤNG TỪNG BƯỚC ═══

Để giảm thiểu sai sót và tăng tính trực quan trong giảng dạy, giao diện các công cụ cần tuân thủ bảng hướng dẫn các bước thực hiện như sau:

### 1. Ma Trận Eisenhower (EisenhowerTool)
*   **Bước 1**: Nhập tên nhiệm vụ học tập vào ô nhập liệu nhanh.
*   **Bước 2**: Nhấp chọn nút **Thêm** và phân loại vào một trong 4 ô (Ô 1: Làm ngay, Ô 2: Lên lịch, Ô 3: Ủy thác, Ô 4: Loại bỏ).
*   **Bước 3**: Click chọn biểu tượng lịch để thiết lập ngày hạn chót (Deadline).
*   **Bước 4**: Học sinh có thể kéo thả nhiệm vụ qua lại giữa các ô để điều chỉnh phân loại khi mức độ khẩn cấp thay đổi.
*   **Bước 5**: Tích chọn hoàn thành nhiệm vụ. Khi hoàn thành toàn bộ ô khẩn cấp (Ô 1), hệ thống hiển thị hiệu ứng confetti chúc mừng.

### 2. Sơ đồ tư duy học tập (MindmapTool)
*   **Bước 1**: Nhấp nút **Chủ đề trung tâm** để khởi tạo nút gốc cho bài học.
*   **Bước 2**: Chọn node hiện tại và nhấp **Thêm nhánh** (hoặc nhấn phím `Insert`/`Tab` trên bàn phím) để mở rộng các ý con.
*   **Bước 3**: Di chuột vào node, nhấp đúp để soạn thảo nội dung tiếng Việt. Nhấp chọn **Biểu tượng** để gắn các emoji tương tác sinh động.
*   **Bước 4**: Nhấp các nút chức năng bên phải: **Đổi cấu trúc** (ngang/dọc/tỏa tròn), **Đổi nét vẽ**, **Đổi màu** để cá nhân hóa sơ đồ.
*   **Bước 5**: Nhấp **Lưu** hoặc **Xuất ảnh** để lưu lại thành quả học tập hoặc nộp bài.

---

## 🏁 BẢNG KIỂM THỬ TÍCH HỢP HỒI QUY (QA INTEGRATION TESTING)

QA/QC kiểm thử viên bắt buộc phải kiểm tra tất cả các ca kiểm thử (Test Cases) sau để đảm bảo hệ thống nâng cấp đạt chuẩn 100%:

| Bước | Tên bài kiểm thử (Test Case) | Dữ liệu đầu vào (Input) | Luồng thực hiện (Steps) | Kết quả mong đợi (Expected Output) | Trạng thái |
|:---:|---|---|---|---|:---:|
| **1** | Kiểm tra xuất thẻ công thức lên Bảng trắng | Thẻ công thức dài (môn Vật Lý hoặc Hóa Học). | 1. Di chuột vào thẻ công thức.<br>2. Nhấp nút 🖊️ (Bảng trắng) trên thanh công cụ hover. | - SmartScreen mở ra tự động.<br>- Thẻ công thức hiển thị đầy đủ thông tin từ đầu đến cuối, không bị cắt góc dưới, không bị mất văn bản mô tả thực tiễn. | `[ ] Chưa test` |
| **2** | Kiểm tra co giãn màn hình Sơ đồ tư duy | Thu hẹp chiều rộng cửa sổ ứng dụng xuống dưới 900px. | 1. Xem thanh công cụ phía trên sơ đồ tư duy.<br>2. Kiểm tra vị trí nút "📚 Văn học" và các template. | - Các template tự động xuống dòng đẹp mắt.<br>- Các nút tính năng như "🔀 Đổi cấu trúc", "🎨 Đổi màu" căn phải gọn gàng, không bị chồng đè hay che khuất nút template mẫu. | `[ ] Chưa test` |
| **3** | Kiểm tra Bảng trắng Tính nhẩm nhanh | Mở công cụ "Tính Nhẩm Nhanh". | 1. Nhấp nút Bảng trắng (🖊️) trên thanh công cụ chính của Hub.<br>2. Kiểm tra ảnh chụp đưa lên SmartScreen. | - Hệ thống tự động chụp riêng vùng câu hỏi phép tính (spQuestionArea).<br>- Ảnh đưa lên bảng vẽ gọn gàng, sắc nét, không bị dính bàn phím ảo hay bảng kỷ lục. | `[ ] Chưa test` |
| **4** | Kiểm tra nhập số thập phân qua phím ảo | Nhập giá trị `5.5` vào ô "Khối lượng" (DensityTool) hoặc "Hằng số H+" (PhScaleTool) bằng TouchNumPad. | 1. Focus TextBox nhận bàn phím số ảo.<br>2. Nhấp số `5`, nhấp nút `.`, nhấp số `5`.<br>3. Nhấp nút `▲` tăng trị số.<br>4. Nhấp nút `▼` giảm trị số. | - Trên OS Việt Nam: TextBox hiển thị `5,5` (dấu phẩy).<br>- Nhấp `▲` tăng lên `6,5` (không lỗi).<br>- Nhấp `▼` giảm về `5,5` (không lỗi).<br>- Hệ thống tính toán ra kết quả chính xác, không báo lỗi định dạng số. | `[ ] Chưa test` |
| **5** | Kiểm tra phông chữ đồng bộ | Mở lần lượt các công cụ: Brainstorm, Formulas, HistoryTimeline, MathSymbols, Mindmap. | 1. Xem kiểu chữ của nhãn, nút bấm và tiêu đề.<br>2. So sánh với font chữ `Inter` và `Outfit` chuẩn. | - Không còn bất kỳ control nào hiển thị bằng font Segoe UI.<br>- Toàn bộ hiển thị đồng bộ phông chữ thương hiệu Inter và Outfit sắc nét. | `[ ] Chưa test` |
| **6** | Kiểm tra cuộn tràn màn hình Planets & Noise | Mở PlanetsTool và nhấp chọn xem chi tiết 1 hành tinh; mở NoiseMonitorTool cấu hình mic trên màn hình đứng. | 1. Đọc văn bản mô tả hành tinh dài trong popup.<br>2. Thử thao tác kéo cuộn thông tin.<br>3. Kiểm tra nút bấm cấu hình mic ở đáy panel. | - Có thanh cuộn ScrollBar tự động xuất hiện.<br>- Học sinh cuộn xem hết 100% nội dung mô tả hành tinh và bấm được nút cấu hình mic dễ dàng, không bị tràn ẩn. | `[ ] Chưa test` |

---

*Tài liệu đã được kiểm duyệt bởi Hội đồng kỹ thuật dự án QA Smart School. Yêu cầu bộ phận phát triển và QA phối hợp triển khai nâng cấp chính xác theo kế hoạch.*
