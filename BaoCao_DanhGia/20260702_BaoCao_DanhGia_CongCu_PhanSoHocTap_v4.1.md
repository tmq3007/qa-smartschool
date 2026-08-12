# BÁO CÁO ĐÁNH GIÁ CHUYÊN GIA: PHÂN HỆ CÔNG CỤ PHÂN SỐ HỌC TẬP (QA SMARTCLASS v4.1)
**HỘI ĐỒNG THẨM ĐỊNH ĐA NGÀNH DỰ ÁN HỌC ĐƯỜNG QA SMART SCHOOL**
*Biên bản đánh giá chuyên sâu và nghiệm thu sư phạm - Ngày 02 tháng 07 năm 2026*

---

## ═══ PHẦN 1: TỔNG QUAN HẠNG MỤC ĐÁNH GIÁ & HIỆN TRẠNG ═══

Để hoàn thiện công cụ **Phân Số Học Tập** (FractionTool) dành cho học sinh Lớp 4-6 đáp ứng tiêu chuẩn khắt khe của bộ quy chuẩn sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.1**, Hội đồng chuyên gia gồm 17 thành viên đã tiến hành đánh giá chi tiết từ mã nguồn đến giao diện người dùng (UI/UX) thực tế.

### 1. Hiện trạng giao diện (Qua phân tích ảnh chụp màn hình và mã nguồn)
*   **Vấn đề khoảng trống (Layout Starvation):** Giao diện tab "Ứng dụng thực tế" đang bị trống cục bộ nghiêm trọng.
    *   **Cột trái Preview:** Hình ảnh bản đồ minh họa được đặt trong Border có tỷ lệ rộng `1.2*` co giãn theo chiều cao cửa sổ. Khi hiển thị hình ảnh tỷ lệ ngang (Landscape) như bản đồ, cơ chế `Stretch="Uniform"` tạo ra khoảng trống trắng khổng lồ ở phía trên và phía dưới hình ảnh bên trong Border.
    *   **Cột phải chi tiết:** Chỉ hiển thị tiêu đề và một đoạn mô tả cực ngắn (2 dòng) về tỷ lệ xích. Khoảng không gian trống bên dưới mô tả chiếm đến hơn 80% diện tích cột phải, gây cảm giác ứng dụng thô sơ, chưa hoàn thiện.
*   **Hạn chế tính sư phạm:** Nội dung ứng dụng thực tế quá sơ sài, thiếu các công thức toán học trực quan, thiếu câu hỏi khơi gợi tư duy và nhiệm vụ thực hành tại lớp cho học sinh.
*   **Font chữ & Bố cục:** Phông chữ hệ thống chưa được đồng bộ bắt buộc `Segoe UI` trên tất cả các điều khiển TextBlock của giao diện, dẫn đến nguy cơ lệch font chữ tiếng Việt trên một số thiết bị phòng máy cũ.
*   **Tương tác & Chỉ dẫn:** Lạm dụng hộp thoại cảnh báo hệ thống (`MessageBox.Show`) gây cướp tiêu điểm (Focus Lock), cản trở thao tác trên màn hình tương tác thông minh và không đồng bộ thẩm mỹ phần mềm.

---

## ═══ PHẦN 2: Ý KIẾN CHI TIẾT TỪ HỘI ĐỒNG 17 CHUYÊN GIA ═══

### 1. 📐 Trưởng bộ phận thiết kế dự án QA Smart School (Project Design Head)
> "Cấu trúc giao diện của FractionTool hiện tại chưa đạt sự cân bằng về mặt thị giác theo tiêu chuẩn v4.1. Tỷ lệ không gian trống (white space) phân bổ không hợp lý. Khoảng trống quá nhiều ở phần hiển thị ứng dụng thực tế là do thiết kế luồng dữ liệu một chiều quá đơn giản. Chúng ta cần bổ sung thêm các trường thông tin học thuật trực quan như 'Khái niệm sư phạm cốt lõi', 'Phân tích toán học', 'Câu hỏi thảo luận' và 'Nhiệm vụ thực hành' để lấp đầy khoảng trống một cách có ích, tăng chiều sâu tri thức cho công cụ."

### 2. 🔌 Quản lý IT (IT Manager)
> "Về khía cạnh vận hành hệ thống, việc tải các tệp ảnh lớn như `app_fraction_6_VN.png` từ tài nguyên (Pack URI) cần được kiểm soát chặt chẽ để tránh tràn bộ nhớ RAM của các dòng máy trạm học sinh cấu hình yếu (phòng Lab Model A). Cơ chế nạp ảnh bất đồng bộ của `PracticalAppViewer` hoạt động tốt, tuy nhiên việc thiếu cơ chế giải phóng tài nguyên hình ảnh cũ khi chuyển đổi đề mục có thể gây rò rỉ bộ nhớ (Memory Leak) sau thời gian dài sử dụng."

### 3. 🔍 Chuyên gia kiểm thử (Testing Expert)
> "Tôi phát hiện một số điểm cần cải tiến trong phần kiểm thử chức năng:
> 1. Giao diện đang sử dụng hộp thoại `MessageBox.Show` để thông báo lỗi nhập liệu hoặc lỗi xuất PDF. Điều này gây chặn luồng kiểm thử tự động UI.
> 2. Trong trò chơi trắc nghiệm, logic kiểm tra đáp án phân số tối giản hoạt động đúng, nhưng phần phản hồi lỗi nhập mẫu số bằng 0 cần hiển thị trực quan thay vì bật popup gây gián đoạn kịch bản test tự động.
> 3. Cần bổ sung các ca kiểm thử biên cho trường hợp nhập số âm cực lớn hoặc mẫu số lớn để đảm bảo độ bền hệ thống."

### 4. 🎨 Chuyên gia thiết kế giao diện phần mềm (Software UI Design Expert)
> "Màu sắc chủ đạo (Orange 900 - `#E65100`) của FractionTool có tính sư phạm rất cao, kích thích sự chú ý và tư duy của học sinh tiểu học. Tuy nhiên, layout của tab ứng dụng thực tế là một điểm trừ lớn. 
> * **Giải pháp khắc phục:** Tại cột trái, chúng ta nên tách Border ảnh ra và đặt thêm một 'Thẻ khái niệm sư phạm cốt lõi' ở phía dưới để tận dụng khoảng trống đứng. 
> * Tại cột phải, thay vì một ScrollViewer chứa đoạn text mô tả đơn điệu, ta nên chia thành các khối thông tin (Cards) có viền bo tròn nhẹ, nền màu nhạt (ví dụ: vàng nhạt `#FFFDE7` cho Câu hỏi, xanh nhạt `#E1F5FE` cho Phân tích toán học) để tạo nhịp điệu thị giác và thu hút học sinh."

### 5. ⚙️ Chuyên gia phân tích và thiết kế hệ thống (Systems Analysis & Design Expert)
> "Lớp dữ liệu `PracticalAppItem` hiện tại quá nghèo nàn, chỉ có `Icon`, `Title`, `ImagePath`, `Description`. Đây chính là nguyên nhân kỹ thuật gốc rễ dẫn đến sự thiếu hụt thông tin hiển thị. Tôi đề xuất refactor lại model này, bổ sung thêm các thuộc tính: `Formula` (Công thức hiển thị dạng toán học), `MathAnalysis` (Phân tích toán học chi tiết), `DiscussionQuestion` (Câu hỏi thảo luận sư phạm), và `InteractiveTask` (Nhiệm vụ thực hành). `PracticalAppViewer` sẽ tự động ẩn/hiển thị các thẻ này nếu dữ liệu đầu vào có hoặc trống, đảm bảo tính tương thích ngược."

### 6. 🗄️ Chuyên gia về cơ sở dữ liệu và thiết bị kết nối ngoại vi (DB & Peripheral Connection Expert)
> "Việc lưu kỷ lục điểm cao (`fraction_highscore.txt`) bằng cách ghi đè trực tiếp một tệp tin văn bản phẳng trong thư mục cài đặt là giải pháp thô sơ. Nếu học sinh tắt ứng dụng đột ngột hoặc mất nguồn điện khi đang ghi file, tệp tin có thể bị hỏng (Corrupted). Hơn nữa, việc đọc ghi liên tục vào bộ nhớ flash của các thiết bị Kiosk/Thin Client không qua bộ đệm (Caching) sẽ làm giảm tuổi thọ thiết bị. Đề xuất đưa bảng điểm cao vào cơ sở dữ liệu SQLite cục bộ `smartclass.db` thông qua `DatabaseManager`."

### 7. 🛡️ Chuyên gia về bảo mật và an ninh mạng (Cybersecurity & Privacy Expert)
> "Tệp lưu trữ điểm cao `fraction_highscore.txt` lưu dữ liệu dạng số nguyên không mã hóa. Học sinh có kiến thức IT trung bình dễ dàng tìm thấy đường dẫn trong AppData để chỉnh sửa điểm số lên vô hạn, phá hỏng tính thi đua lành mạnh. Cần mã hóa chuỗi điểm số bằng thuật toán băm SHA-256 kèm muối (Salt) hoặc mã hóa đối xứng AES-256 dựa trên định danh máy trạm để đảm bảo tính toàn vẹn dữ liệu học đường."

### 8. 🏫 Nhà giáo dục (Educator)
> "Phương pháp trực quan hóa phân số bằng hình ảnh bản đồ thực tế rất tuyệt vời, giúp học sinh thoát khỏi sự trừu tượng hóa khô khan. Tuy nhiên, tính sư phạm sẽ sâu sắc hơn rất nhiều nếu chúng ta giải thích rõ mối quan hệ toán học: 'Tử số 1 đại diện cho 1 đơn vị khoảng cách trên bản đồ (ví dụ 1cm), mẫu số 10.000 đại diện cho 10.000 đơn vị tương tự ngoài thực tế (10.000cm = 100m)'. Việc thiếu liên kết này làm giảm đi 50% giá trị giáo dục của hình ảnh minh họa."

### 9. 🎓 Nhà Quản lý hiệu trưởng nhà trường (School Principal/Manager)
> "Từ góc độ quản lý trường học, việc tích hợp bảng điểm cá nhân và hệ thống huy hiệu (Tập sự, Chiến binh, Hiệp sĩ, Trưởng lão) rất có ích trong việc thúc đẩy phong trào học tập tự chủ. Tuy nhiên, hệ thống cần hỗ trợ đồng bộ dữ liệu điểm số này về máy giáo viên và cổng thông tin học tập của nhà trường để phục vụ công tác đánh giá định kỳ, thay vì chỉ lưu cục bộ tại máy học sinh."

### 10. 👥 Trưởng bộ môn của trường (Head of Department)
> "Bộ môn Toán đánh giá FractionTool bám sát chương trình GDPT 2018 cho khối Tiểu học và THCS. Tuy nhiên, việc chuẩn hóa phân số có hỗ trợ số âm (ví dụ `-3/4`) là kiến thức thuộc lớp 6 (chương trình THCS). Do đó, khi áp dụng Dynamic UI cho học sinh lớp 4-5 (Tiểu học), hệ thống cần tự động lọc bỏ các preset số âm và vô hiệu hóa phím âm `-` trên bàn phím ảo để tránh gây quá tải nhận thức cho các em học sinh nhỏ tuổi."

### 11. 👩‍🏫 Giáo viên ưu tú với nhiều kinh nghiệm (Elite Experienced Teacher)
> "Khi đứng lớp giảng dạy bằng màn hình tương tác lớn, việc xuất hiện các thông báo dạng `MessageBox.Show` là cực kỳ phiền toái. Nó bắt buộc tôi phải di chuyển đến góc màn hình để bấm nút xác nhận, làm gián đoạn mạch bài giảng và làm phân tán sự tập trung của học sinh. Mọi phản hồi lỗi nên được đưa vào thanh trạng thái dưới chân hoặc banner màu đỏ tự ẩn sau 3 giây. Ngoài ra, cần có chỉ dẫn sử dụng từng bước rõ ràng cho các em tự học tại nhà."

### 12. 👦 Học sinh (Student)
> "Em rất thích phần tô màu phân số vì nó giống như trò chơi vẽ tranh. Nhưng tab Ứng dụng thực tế nhìn hơi chán vì chỉ có một bức ảnh bản đồ và mấy dòng chữ ngắn, không có câu hỏi đố vui hay thử thách nào để tụi em bấm chọn. Em mong có thêm các câu đố nhỏ liên quan đến bản đồ để nhận thêm điểm thưởng và thăng cấp huy hiệu nhanh hơn."

### 13. 🧹 Nhân viên nhà trường (School Staff)
> "Vấn đề in ấn và xuất bản phiếu bài tập là nhu cầu thực tế rất lớn của giáo viên. Tính năng 'Xuất lời giải PDF' sử dụng thư viện QuestPDF là một điểm cộng lớn. Tuy nhiên, giao diện xuất file cần cho phép giáo viên tùy chọn ẩn/hiện lời giải chi tiết (chỉ xuất đề bài để làm bài kiểm tra) và tự động điền thông tin trường lớp, tên học sinh vào tiêu đề trang PDF."

### 14. 🎮 Một gamer giỏi (Pro Gamer)
> "Cơ chế gamification (trò chơi hóa) của FractionTool có khung sườn tốt với streak nhân đôi điểm thưởng khi đạt chuỗi 5 câu đúng liên tiếp. Tuy nhiên, giao diện thiếu đi hiệu ứng kích thích cảm xúc (Juiciness): không có hiệu ứng pháo hoa khi lên cấp huy hiệu, âm thanh chúc mừng còn đơn điệu và phần tô màu phân số chưa có bộ đếm thời gian (Time Attack Mode) để tăng tính thử thách cho người chơi."

### 15. 🏢 Cán bộ quản lý của phòng giáo dục (District Education Manager)
> "Chúng tôi quan tâm đến tính bình đẳng trong tiếp cận giáo dục. Việc FractionTool tích hợp bàn phím ảo cảm ứng `TouchNumPad` ngay trên phần mềm giúp các trường học chưa có điều kiện trang bị bàn phím vật lý vẫn vận hành tốt. Bố cục phần mềm cần tối ưu hóa không gian trống để hiển thị tốt trên các dòng máy chiếu độ phân giải thấp (1024x768) phổ biến ở các huyện ngoại thành."

### 16. 🏫 Chuyên viên của sở giáo dục (Provincial Department Specialist)
> "Phần mềm cần tuân thủ nghiêm ngặt Thông tư quy định về thiết bị dạy học tối thiểu của Bộ Giáo dục. Việc sử dụng ngôn ngữ tiếng Việt 100% trong phiên bản Việt hóa là bắt buộc. Tôi phát hiện ở tab 2, khi chọn tiếng Việt, một số đề mục tiếng Anh vẫn xuất hiện ở phần mô tả nội dung nếu dữ liệu cấu hình bị thiếu. Cần có cơ chế kiểm duyệt dữ liệu ngôn ngữ (Localization fallback) chặt chẽ, không được để lộ chữ tiếng Anh khi người dùng đang chọn chế độ Tiếng Việt."

### 17. 🔬 Nhà khoa học giáo dục (Educational Scientist)
> "Sự chuyển dịch từ mô hình biểu diễn liên tục (Fraction Bar trực quan ở cột trái) sang mô hình ứng dụng thực tế (bản đồ tỷ lệ xích) là một bước chuyển nhận thức quan trọng từ tư duy cụ thể sang tư duy trừu tượng. Việc để trống quá nhiều không gian hiển thị làm mất đi cơ hội củng cố nhận thức. Việc lấp đầy khoảng trống bằng các câu hỏi thảo luận mang tính gợi mở sẽ kích hoạt vùng tư duy phản biện (Critical Thinking) và giúp học sinh neo giữ kiến thức sâu sắc hơn."

---

## ═══ PHẦN 3: BẢNG TỔNG HỢP TIÊU CHÍ TUÂN THỦ (COMPLIANCE MATRIX v4.1) ═══

| ID Quy chuẩn | Tiêu chí đánh giá | Hiện trạng đo đạc thực tế | Mức độ đáp ứng | Trạng thái |
| :--- | :--- | :--- | :--- | :---: |
| **QC_01_FONT** | Font chữ tiếng Việt học thuật | Sử dụng font mặc định hệ thống ở một số TextBlock; chip nút bấm sử dụng Segoe UI. Chưa đồng bộ triệt để. | 80% | **CẢNH BÁO** |
| **QC_02_LAYOUT**| Phân bổ không gian hiển thị | Tab Ứng dụng thực tế bị trống >60% diện tích màn hình. Image co giãn Stretch Uniform tạo khoảng trống đứng lớn. | 40% | **KHÔNG ĐẠT** |
| **QC_03_COLOR** | Màu sắc sư phạm | Sử dụng tông màu cam ấm (Orange 900) kết hợp vàng nhạt mang tính sư phạm cao, dễ nhìn, không chói mắt. | 100% | **ĐẠT** |
| **QC_04_LOGIC** | Logic chức năng & Toán học | Thuật toán tối giản, quy đồng, so sánh và đổi hỗn số hoạt động chính xác 100% về mặt toán học. | 100% | **ĐẠT** |
| **QC_05_IMAGE** | Chất lượng hình ảnh minh họa | Ảnh bản đồ và nốt nhạc hiển thị sắc nét, không bị vỡ hình. Tuy nhiên chưa có chú thích trực quan đè lên ảnh. | 90% | **ĐẠT** |
| **QC_06_STEP**  | Chỉ dẫn sử dụng từng bước | Đã có panel Hướng dẫn nhanh lý thuyết nhưng thiếu chỉ dẫn động (Interactive Walkthrough) cho từng chức năng. | 70% | **CẢNH BÁO** |
| **QC_07_LANG**  | Ngôn ngữ hiển thị | Tiếng Việt chuẩn xác. Tuy nhiên cần bảo đảm Localization hoạt động đồng bộ, không lẫn lộn từ tiếng Anh. | 95% | **ĐẠT** |

---

## ═══ PHẦN 4: ĐỀ XUẤT CẢI TIẾN & KHẮC PHỤC KỸ THUẬT CHI TIẾT ═══

Để giải quyết triệt để lỗi giao diện trống nhiều và hoàn thiện phần mềm theo đúng tiêu chuẩn v4.1, Hội đồng đề xuất phương án cải tiến kỹ thuật đồng bộ trên 3 tệp tin lõi:

### 1. Nâng cấp cấu trúc dữ liệu (`PracticalAppItem.cs`)
Bổ sung các thuộc tính học thuật phong phú để cung cấp nội dung hiển thị:
```csharp
namespace QASmartClass.LearningTools.Models
{
    public class PracticalAppItem
    {
        public string Icon { get; set; } = "🌍";
        public string Title { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string Description { get; set; } = "";
        
        // Các thuộc tính nâng cấp v4.1+ để giải quyết khoảng trống hiển thị
        public string Formula { get; set; } = "";             // Công thức toán học (ví dụ: "Tỷ lệ = Khoảng cách bản đồ / Khoảng cách thực tế")
        public string MathAnalysis { get; set; } = "";        // Phân tích toán học sâu
        public string DiscussionQuestion { get; set; } = "";  // Câu hỏi gợi mở tư duy sư phạm
        public string InteractiveTask { get; set; } = "";     // Nhiệm vụ thực hành cho học sinh
    }
}
```

### 2. Tái cấu trúc giao diện hiển thị (`PracticalAppViewer.xaml`)
Thay đổi bố cục Side-by-Side để lấp đầy khoảng trống đứng bằng các khối thông tin trực quan:
*   **Cột trái (Ảnh + Khái niệm cốt lõi):** Đặt ảnh lớn và Thẻ khái niệm sư phạm trong một Grid có 2 hàng (`*` và `Auto`) để lấp đầy không gian trống bên dưới ảnh.
*   **Cột phải (Chi tiết nhiều tầng):** Bọc mô tả, phân tích toán học, câu hỏi thảo luận và nhiệm vụ thực hành vào các Border dạng Card có màu sắc và icon trực quan.

```xml
<!-- Đề xuất cải tiến cấu trúc Preview của PracticalAppViewer.xaml -->
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
        
        <!-- Khung ảnh lớn -->
        <Border Grid.Row="0" CornerRadius="12" BorderThickness="1" BorderBrush="{DynamicResource BorderBrush}" Background="{DynamicResource SurfaceBackground}" ClipToBounds="True" Margin="0,0,0,12">
            <Grid>
                <Image x:Name="imgLargePreview" Stretch="Uniform" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                <TextBlock x:Name="txtLoadingPlaceholder" Text="⏳ Đang tải hình ảnh..." FontSize="14" Foreground="#94A3B8" HorizontalAlignment="Center" VerticalAlignment="Center" Visibility="Collapsed"/>
            </Grid>
        </Border>

        <!-- Thẻ khái niệm toán học trực quan (Lấp đầy khoảng trống bên dưới ảnh) -->
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

        <!-- Tiêu đề chi tiết -->
        <TextBlock x:Name="txtDetailTitle" Grid.Row="0" FontSize="20" FontWeight="Bold" Foreground="{DynamicResource TextPrimary}" Margin="0,0,0,12" HorizontalAlignment="Left" TextWrapping="Wrap"/>

        <!-- Khu vực nội dung cuộn -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="0,0,4,0">
                <!-- Mô tả chung -->
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

### 3. Cập nhật dữ liệu ứng dụng thực tế phân số học tập (`FractionTool.xaml.cs`)
Bổ sung tri thức chi tiết cho các mục ứng dụng, đặc biệt là **Bản đồ & Tỷ lệ xích**:
```csharp
new PracticalAppItem
{
    Icon = "🗺",
    Title = isVN ? "Bản đồ & Tỷ lệ xích" : "Geography & Scale Mapping",
    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_6_{suffix}.png",
    Description = isVN 
        ? "Bản đồ địa lý sử dụng phân số tỷ lệ xích để biểu diễn khoảng cách thực tế được thu nhỏ lại trên bản vẽ." 
        : "Read blueprints and scale down physical dimensions using fraction scales.",
    Formula = "Tỷ lệ xích (m) = Khoảng cách trên bản đồ / Khoảng cách thực địa ngoài thực tế",
    MathAnalysis = "Với tỷ lệ xích 1/10.000, 1 cm trên bản đồ tương đương với 10.000 cm ngoài thực địa (tức là 100 mét). Nếu khoảng cách giữa trường học và thư viện trên bản đồ là 5 cm, thì khoảng cách thực tế sẽ là:\n5 cm × 10.000 = 50.000 cm = 500 mét.",
    DiscussionQuestion = "Nếu bản đồ tăng độ chi tiết và đổi tỷ lệ xích thành 1/2.000 thì kích thước của trường học trên bản đồ sẽ to lên hay nhỏ đi? Giải thích tại sao mẫu số nhỏ hơn lại làm hình ảnh to hơn?",
    InteractiveTask = "Sử dụng thước kẻ đo khoảng cách từ Town Center đến Green Park trên bản đồ. Tính toán khoảng cách thực tế dựa trên tỷ lệ 1/10.000 và điền kết quả vào phiếu bài tập."
}
```

---
> [!IMPORTANT]
> **Kết luận thẩm định:** Sau khi thực thi các cải tiến về layout của `PracticalAppViewer` và làm phong phú dữ liệu của `FractionTool`, công cụ **Phân Số Học Tập** sẽ hoàn toàn khắc phục được lỗi trống không gian hiển thị, nâng cao giá trị giáo dục trực quan và đáp ứng 100% quy chuẩn thiết kế sư phạm của QA SmartClass v4.1.
