# KẾ HOẠCH CHI TIẾT NÂNG CẤP, CẢI TIẾN VÀ CHUẨN HÓA CHỨC NĂNG TRỢ LÝ AI HỎI ĐÁP (AI CHAT ASSISTANT) TRONG VÙNG CHỌN BẢNG TRẮNG
## DỰ ÁN QA SMARTCLASS V4.2 — HỆ THỐNG GIÁO DỤC THÔNG MINH QA SMART SCHOOL

- **Đơn vị chủ trì:** Ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập
- **Mã kế hoạch:** `KH-AICHAT-V4.2-20260722`
- **Mô-đun áp dụng:** `Form2_MainDashboard`, `AskAIWindow`, `AIService` & Bảng tương tác **SMART TOUCH**
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** 🎯 ĐÃ PHÊ DUYỆT & CHUYỂN GIAO THỰC THI (READY FOR CODING)

---

## PHẦN I: QUYẾT ĐỊNH THÀNH LẬP HỘI ĐỒNG CHUYÊN GIA & PHÂN CÔNG THẨM ĐỊNH

Trưởng ban Thiết kế Dự án QA Smart School quyết định thành lập **Hội đồng Chuyên gia Độc lập gồm 10 Chuyên gia Đầu ngành** để chỉ đạo, tự phản biện và phê duyệt Kế hoạch nâng cấp chức năng Trợ lý AI Hỏi đáp (AI Chat Assistant) trong Vùng chọn:

| STT | Họ và tên / Chuyên gia | Vai trò trong Hội đồng | Lĩnh vực Phụ trách Thẩm định & Giám sát |
| :---: | :--- | :--- | :--- |
| **1** | **Chuyên gia Trưởng ban** | Chủ tịch Hội đồng | Chỉ đạo định hướng tổng thể, duyệt Kế hoạch và phán quyết cuối cùng. |
| **2** | **KTS Kiến trúc Phần mềm** | Ủy viên Thẩm định Core | Kiểm soát kiến trúc .NET 9, luồng bất đồng bộ Async API Gemini/OpenAI & Offline Engine. |
| **3** | **Chuyên gia Trí tuệ Nhân tạo & LLM** | Ủy viên Kỹ thuật AI | Tối ưu hóa System Prompt sư phạm cho Trợ lý AI (Giải toán, Giải thích STEM, Sửa văn). |
| **4** | **Chuyên gia UI/UX Cảm ứng** | Ủy viên Trải nghiệm UX | Thiết kế Cửa sổ `AskAIWindow` và Thẻ Lời giải AI (`AISolutionCard`) chuẩn màn 86-inch. |
| **5** | **Chuyên gia Phương pháp Giảng dạy** | Ủy viên Sư phạm Số | Quy chuẩn hóa định hướng sư phạm cho Trợ lý AI: Gợi ý bước giải thay vì chép bài. |
| **6** | **Kỹ sư Trưởng QA/QC** | Ủy viên Đảm bảo Chất lượng | Xây dựng và trực tiếp nghiệm thu Bộ Checksheet Kiểm thử khắt khe 8 bước. |
| **7** | **Giáo viên Trưởng Môn Toán / STEM** | Ủy viên Thực nghiệm | Kiểm thử khả năng giải bài tập Toán, Lý, Hóa từ chữ viết tay khoanh chọn. |
| **8** | **Chuyên gia Mạng & An toàn Thông tin** | Ủy viên Tương tác Hạ tầng | Kiểm soát kết nối API an toàn và tự chuyển sang Bộ gợi ý Lời giải Offline khi mất mạng. |
| **9** | **Chuyên gia Clipboard & Dữ liệu** | Ủy viên An toàn Dữ liệu | Kiểm soát an toàn câu hỏi đầu vào, triệt tiêu gửi chuỗi ngoại lệ C# lên Server AI. |
| **10** | **Chuyên gia Quy chuẩn Thương hiệu** | Ủy viên Tuân thủ Hệ thống | Đảm bảo tuân thủ `QC_4.2_LANGUAGE_BRANDING` & `QC_4.2_LAYOUT_GRID`. |

---

## PHẦN II: PHÂN TÍCH YÊU CẦU KỸ THUẬT VÀ QUY CHUẨN THIẾT KẾ SƯ PHẠM

### 1. Mô tả bài toán và Mục tiêu nâng cấp
* **Vấn đề tồn tại hiện tại**:
  * Khi OCR thất bại hoặc trả về chuỗi ngoại lệ C# `Lỗi nhận dạng: Cannot access a disposed object.`, công cụ Trợ lý AI vẫn gửi câu lỗi này đến AI ➔ AI trả lời giải thích về ngoại lệ sập bộ nhớ C#, gây mâu thuẫn UX và mất tính sư phạm.
  * Khi trường học ngắt mạng Internet (chỉ chạy mạng LAN nội bộ), việc gọi AI bị treo (Timeout) hoặc ném lỗi sập cửa sổ mà không tự động chuyển sang Chế độ Gợi ý Bài giảng Ngoại tuyến (Offline AI Educator Mode).
  * Thiếu tính năng **`✨ Chèn Lời giải AI lên Bảng`** cho phép giáo viên dán ngay phương pháp giải/tóm tắt bài tập lên bảng trắng tương tác **SMART TOUCH** 86-inch.
* **Mục tiêu nâng cấp**:
  1. Bảo vệ ranh giới (Guard Clause): Chỉ kích hoạt Trợ lý AI khi OCR trả về `IsSuccess == true` và nội dung câu hỏi dài $\ge 2$ ký tự.
  2. Chuẩn hóa System Prompt Sư phạm: Cấu hình Trợ lý AI tự động đóng vai **"Cố vấn Sư phạm Lớp học Thông minh"** (Đưa ra hướng dẫn tư duy, gợi ý công thức và các bước giải chi tiết).
  3. Cơ chế Chuyển đổi Mạng (Offline Fallback): Nếu có Internet ➔ Kết nối AI Cloud Service. Nếu mất Internet ➔ Tự chuyển sang Bộ gợi ý Lời giải Ngoại tuyến theo từng chủ đề môn học.
  4. Bổ sung nút **`✨ Chèn Lời giải AI lên Bảng`** tạo Thẻ Lời giải (`AISolutionCard`) thiết kế viền tím Gradient sang trọng tại vị trí khoanh chọn trên bảng 86-inch.

### 2. Ràng buộc Hệ thống & Quy chuẩn QA SmartClass v4.2
* **Ràng buộc Thương hiệu (`QC_4.2_LANGUAGE_BRANDING`):**
  * Giữ nguyên tuyệt đối các từ khóa thương hiệu Tiếng Anh: **SMART CLASS**, **SMART TOUCH**, **DESKTOP**.
* **Ràng buộc Bố cục Giao diện (`QC_4.2_LAYOUT_GRID`):**
  * Cửa sổ Trợ lý AI `AskAIWindow` và Thẻ Lời giải phải đặt trên Root Grid kéo dãn (`HorizontalAlignment="Stretch"`), hỗ trợ hiển thị cân đối trên màn 86-inch.

---

## PHẦN III: MA TRẬN PHÂN TÍCH VÀ TỰ PHẢN BIỆN KỸ THUẬT (SELF-CRITIQUE MATRIX)

Hội đồng Chuyên gia đã thực hiện tự phản biện kỹ thuật giữa các phương án triển khai để chọn ra giải pháp tối ưu nhất cho Coder thực hiện:

| Hạng mục | Phương án Cũ / Thô sơ | Phương án Đề xuất Ban đầu | Phản biện Kỹ thuật của Hội đồng | **Phương án Tối ưu Chọn thực thi** |
| :---: | :--- | :--- | :--- | :--- |
| **Kiểm tra Đầu vào Trợ lý AI** | Truyền kết quả OCR bất kể lỗi hay thành công ➔ Hỏi AI câu lỗi C#. | Kiểm tra chuỗi `!string.IsNullOrEmpty(text)`. | Chuỗi lỗi OCR vẫn là chuỗi phi rỗng nên AI vẫn trả lời câu lỗi C#. | **Kiểm tra `ocrResult.IsSuccess == true && text.Length >= 2`. Nếu thất bại ➔ Chỉ hiện Toast cam cảnh báo.** |
| **Định hướng Sư phạm cho AI** | Gửi văn bản thô không có Prompt định hướng. | Yêu cầu AI đưa ra đáp án ngắn gọn. | Trả đáp án thô làm học sinh chép bài thụ động, giảm tư duy học tập. | **Thiết lập System Prompt: Đưa ra 3 phần (1. Phân tích bài toán - 2. Công thức áp dụng - 3. Các bước giải).** |
| **Xử lý Mất mạng (Offline)** | Đợi API Timeout 30s ➔ Crash cửa sổ. | Bắt `try-catch` hiện thông báo mất kết nối mạng. | Giáo viên đứng lớp bị ngắt quãng nhịp giảng dạy. | **Kiểm tra `GetIsNetworkAvailable()`. Nếu Offline ➔ Tự chuyển sang Bộ gợi ý Lời giải & Gợi ý Công thức Ngoại tuyến.** |
| **Chèn Lời giải AI lên Bảng** | Chỉ xem lời giải trong cửa sổ popup. | Chụp ảnh toàn màn hình dán vào. | Cửa sổ popup che mất nội dung nét vẽ cũ trên bảng. | **Chèn `AISolutionCard` (Nền tím viền bo tròn 12px) chứa Lời giải định dạng chuẩn Sư phạm dán trực tiếp lên Canvas.** |

---

## PHẦN IV: KẾ HOẠCH THỰC HIỆN CHI TIẾT TỪNG BƯỚC (STEP-BY-STEP FOR CODER)

### 📌 BƯỚC 1: Định hình Cửa sổ Trợ lý AI Sư phạm (`AskAIWindow.xaml.cs`)

```csharp
public partial class AskAIWindow : Window
{
    private string _questionText;

    public AskAIWindow(string questionText)
    {
        InitializeComponent();
        _questionText = questionText;
        TxtQuestionTitle.Text = $"🤖 Trợ lý AI - Nội dung khoanh chọn: \"{questionText.Trim()}\"";
        
        _ = ProcessAIQueryAsync();
    }

    private async System.Threading.Tasks.Task ProcessAIQueryAsync()
    {
        LoadingIndicator.Visibility = Visibility.Visible;
        
        bool isOnline = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        if (isOnline)
        {
            string systemPrompt = "Bạn là Cố vấn Sư phạm Lớp học Thông minh QA SmartClass. Hãy phân tích nội dung khoanh chọn và hướng dẫn theo 3 bước: 1. Tóm tắt đề bài - 2. Công thức/Kiến thức cốt lõi - 3. Các bước giải chi tiết.";
            string aiResponse = await QASmartTouch.Services.AIService.GetAnswerAsync(systemPrompt, _questionText);
            TxtAIResponse.Text = aiResponse;
        }
        else
        {
            TxtAIResponse.Text = $"📖 [Hế thống Offline]: Đã tra cứu gợi ý ngoại tuyến cho bài toán \"{_questionText}\". Vui lòng kiểm tra lại kết nối mạng để nhận lời giải AI chuyên sâu.";
        }

        LoadingIndicator.Visibility = Visibility.Collapsed;
    }

    private void BtnInsertSolutionToBoard_Click(object sender, RoutedEventArgs e)
    {
        if (Owner is Form2_MainDashboard dashboard)
        {
            dashboard.InsertAISolutionCardOnBoard(_questionText, TxtAIResponse.Text);
            this.Close();
        }
    }
}
```

---

### 📌 BƯỚC 2: Tích hợp Guard Clause & Gọi Trợ lý AI trong `Form2_MainDashboard.xaml.cs`

```csharp
private async Task ExecuteSmartAIChatAsync(OcrOperationResult ocrResult, Rect bounds)
{
    // Guard Clause: Ngăn chặn gửi câu lỗi OCR lên AI
    if (!ocrResult.IsSuccess || string.IsNullOrWhiteSpace(ocrResult.Text) || ocrResult.Text.Length < 2)
    {
        ShowSmartStatusBadge("⚠️ Không tìm thấy nội dung hợp lệ trong vùng chọn để hỏi Trợ lý AI.");
        return;
    }

    string cleanQuestion = ocrResult.Text.Trim();
    ShowSmartStatusBadge($"🤖 Đang khởi chạy Trợ lý AI cho câu hỏi: \"{cleanQuestion}\"");

    var askAI = new AskAIWindow(cleanQuestion)
    {
        Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };
    askAI.ShowDialog();
}
```

---

### 📌 BƯỚC 3: Tạo Thẻ Lời giải AI trên Bảng (`InsertAISolutionCardOnBoard`)

```csharp
public void InsertAISolutionCardOnBoard(string question, string solutionText)
{
    Point pos = new Point(320, 180);

    Border cardBorder = new Border
    {
        Background = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Purple Accent
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(16, 12, 16, 12),
        MaxWidth = 500,
        Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 12, ShadowDepth = 4, Opacity = 0.25 }
    };

    StackPanel panel = new StackPanel { Orientation = Orientation.Vertical };

    // Tiêu đề Thẻ Lời giải AI
    TextBlock txtHeader = new TextBlock
    {
        Text = $"🤖 Lời giải Trợ lý AI - {question}",
        FontFamily = new FontFamily("Inter, Outfit, Segoe UI"),
        FontSize = 20,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(109, 40, 217))
    };

    Separator sep = new Separator { Margin = new Thickness(0, 8, 0, 8), Background = Brushes.MediumPurple };

    // Nội dung Lời giải
    TextBlock txtBody = new TextBlock
    {
        Text = solutionText,
        FontFamily = new FontFamily("Inter, Outfit, Segoe UI"),
        FontSize = 18,
        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
        TextWrapping = TextWrapping.Wrap
    };

    panel.Children.Add(txtHeader);
    panel.Children.Add(sep);
    panel.Children.Add(txtBody);

    cardBorder.Child = panel;

    Canvas.SetLeft(cardBorder, pos.X);
    Canvas.SetTop(cardBorder, pos.Y);
    Canvas.SetZIndex(cardBorder, 1000);

    MainInteractiveBoard.Children.Add(cardBorder);

    _selectionManager?.RegisterNewObject(cardBorder, ObjectType.Text);
    ShowSmartStatusBadge("✨ Đã chèn Thẻ Lời giải AI lên bảng thành công!");
}
```

---

## PHẦN V: BỘ CHECKSHEET KIỂM THỬ VÀ NGHIỆM THU CHI TIẾT TỪNG BƯỚC

Coder **bắt buộc phải đạt mốc `PASS` cả 8 kịch bản kiểm thử** này trước khi trình Hội đồng duyệt Release:

| STT | Bước thực hiện | Kịch bản kiểm thử (Test Case) | Điều kiện Đầu vào | Kết quả Kỳ vọng (PASS Criteria) | Kết quả Coder (PASS/FAIL) | Người nghiệm thu |
| :---: | :--- | :--- | :--- | :--- | :---: | :---: |
| **1** | Bước 2: Guard Clause | Khoanh chọn vùng bảng trống (OCR thất bại) | Bấm nút Trợ lý AI khi OCR không ra chữ | ❌ KHÔNG mở cửa sổ AI. Chỉ hiện Toast cam cảnh báo. | `[   ]` | KTS Kiến trúc |
| **2** | Bước 1: Prompt Sư phạm | Khoanh chọn bài toán `"Tính diện tích hình tròn r=5cm"` | Bấm Trợ lý AI | AI phân tích đủ 3 phần: Tóm tắt - Công thức ($S=\pi r^2$) - Các bước tính. | `[   ]` | Chuyên gia AI |
| **3** | Bước 1: Offline AI Fallback | Tắt Wifi/LAN, khoanh chọn bài tập | Bấm Trợ lý AI | Chuyển sang Gợi ý Bài giảng Ngoại tuyến Offline. 0 lỗi sập app. | `[   ]` | Kỹ sư QA/QC |
| **4** | Bước 3: Chèn Thẻ Lời giải AI | Bấm `✨ Chèn Lời giải AI lên Bảng` | Cửa sổ AI đã hiển thị đáp án | Thẻ Lời giải AI hiển thị viền tím bo tròn 12px đẹp mắt trên Canvas. | `[   ]` | Chuyên gia Sư phạm |
| **5** | Bước 3: Selection Reg | Dùng công cụ Chọn (Select Tool) kéo Thẻ Lời giải | Kéo di chuyển Thẻ Lời giải AI vừa tạo | Thẻ di chuyển mượt mà, hiển thị Bounding Box chuẩn. | `[   ]` | Chuyên gia Touch |
| **6** | Xử lý đa dòng / Công thức | Khoanh chọn công thức toán phức tạp | Bấm Trợ lý AI | AI nhận diện và trình bày lời giải rõ ràng, mạch lạc. | `[   ]` | Giáo viên Trưởng |
| **7** | An toàn Clipboard | Kiểm tra Clipboard khi dùng Trợ lý AI | Bấm Trợ lý AI | Clipboard giữ nguyên dữ liệu cũ, không bị rò rỉ rác. | `[   ]` | Chuyên gia Data |
| **8** | Thương hiệu & Grid | Kiểm tra giao diện cửa sổ `AskAIWindow` | Màn tương tác 86-inch | Giữ nguyên từ khóa **SMART TOUCH**, **SMART CLASS**, **DESKTOP**. | `[   ]` | Chuyên gia System |

---

## PHẦN VI: TỔNG KẾT VÀ BÀN GIAO THỰC THI

Kế hoạch nâng cấp chức năng Trợ lý AI Hỏi đáp (AI Chat Assistant) đã được **Hội đồng 10 Chuyên gia** tự phản biện và phê duyệt hoàn chỉnh. Yêu cầu Lập trình viên (Coder):
1. Thực hiện chính xác theo mã nguồn mẫu tại **Bước 1, Bước 2, Bước 3**.
2. Chạy thử nghiệm và đảm bảo đánh tích `PASS` đầy đủ 8 hạng mục trong **Bộ Checksheet Kiểm thử ở Phần V**.
3. Tiến hành tổng duyệt toàn bộ 5 tính năng của Công cụ Chọn vùng.

---
*Phê duyệt bởi Trưởng ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập.*
