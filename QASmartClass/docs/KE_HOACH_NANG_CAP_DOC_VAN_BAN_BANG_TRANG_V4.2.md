# KẾ HOẠCH CHI TIẾT NÂNG CẤP, CẢI TIẾN VÀ CHUẨN HÓA CHỨC NĂNG ĐỌC VĂN BẢN (TEXT-TO-SPEECH) TRONG VÙNG CHỌN BẢNG TRẮNG
## DỰ ÁN QA SMARTCLASS V4.2 — HỆ THỐNG GIÁO DỤC THÔNG MINH QA SMART SCHOOL

- **Đơn vị chủ trì:** Ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập
- **Mã kế hoạch:** `KH-TTS-V4.2-20260722`
- **Mô-đun áp dụng:** `Form2_MainDashboard`, `SpeechSynthesizerService`, `AudioPlayerControl` & Bảng tương tác **SMART TOUCH**
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** 🎯 ĐÃ PHÊ DUYỆT & CHUYỂN GIAO THỰC THI (READY FOR CODING)

---

## PHẦN I: QUYẾT ĐỊNH THÀNH LẬP HỘI ĐỒNG CHUYÊN GIA & PHÂN CÔNG THẨM ĐỊNH

Trưởng ban Thiết kế Dự án QA Smart School quyết định thành lập **Hội đồng Chuyên gia Độc lập gồm 10 Chuyên gia Đầu ngành** để chỉ đạo, tự phản biện và phê duyệt Kế hoạch nâng cấp chức năng Đọc Văn bản (Text-to-Speech) trong Vùng chọn:

| STT | Họ và tên / Chuyên gia | Vai trò trong Hội đồng | Lĩnh vực Phụ trách Thẩm định & Giám sát |
| :---: | :--- | :--- | :--- |
| **1** | **Chuyên gia Trưởng ban** | Chủ tịch Hội đồng | Chỉ đạo định hướng tổng thể, duyệt Kế hoạch và phán quyết cuối cùng. |
| **2** | **KTS Kiến trúc Phần mềm** | Ủy viên Thẩm định Core | Kiểm soát kiến trúc .NET 9, quản lý vòng đời `SpeechSynthesizer` & Threading. |
| **3** | **Chuyên gia Âm thanh & Xử lý Giọng nói** | Ủy viên Kỹ thuật TTS | Tự động chọn Voice (Giọng đọc EN/VI), điều chỉnh Tốc độ (Rate) & Độ âm (Volume). |
| **4** | **Chuyên gia UI/UX Cảm ứng** | Ủy viên Trải nghiệm UX | Thiết kế Thanh điều khiển Âm thanh (Play/Pause/Stop/Speed) chuẩn màn 86-inch. |
| **5** | **Chuyên gia Phương pháp Giảng dạy** | Ủy viên Sư phạm Số | Quy chuẩn hóa phương pháp rèn kỹ năng Đọc & Phát âm trên Bảng tương tác. |
| **6** | **Kỹ sư Trưởng QA/QC** | Ủy viên Đảm bảo Chất lượng | Xây dựng và trực tiếp nghiệm thu Bộ Checksheet Kiểm thử khắt khe 8 bước. |
| **7** | **Giáo viên Trưởng Môn Ngoại ngữ** | Ủy viên Thực nghiệm | Kiểm thử ngữ điệu đọc Tiếng Anh (chuẩn US/UK) và đọc Tiếng Việt chuẩn sư phạm. |
| **8** | **Chuyên gia Phần cứng & Loa Lớp học** | Ủy viên Tương tác Hạ tầng | Thẩm định chất lượng âm thanh phát ra loa lớp học và khử méo tiếng trên Windows. |
| **9** | **Chuyên gia Clipboard & Dữ liệu** | Ủy viên An toàn Dữ liệu | Kiểm soát dữ liệu đầu vào âm thanh, triệt tiêu đọc chuỗi lỗi C# ngoại lệ. |
| **10** | **Chuyên gia Quy chuẩn Thương hiệu** | Ủy viên Tuân thủ Hệ thống | Đảm bảo tuân thủ `QC_4.2_LANGUAGE_BRANDING` & `QC_4.2_LAYOUT_GRID`. |

---

## PHẦN II: PHÂN TÍCH YÊU CẦU KỸ THUẬT VÀ QUY CHUẨN THIẾT KẾ SƯ PHẠM

### 1. Mô tả bài toán và Mục tiêu nâng cấp
* **Vấn đề tồn tại hiện tại**:
  * Khi OCR thất bại hoặc trả về chuỗi lỗi ngoại lệ `Lỗi nhận dạng: Cannot access a disposed object.`, công cụ Đọc văn bản vẫn phát chuỗi lỗi này ra loa lớp học bằng giọng đọc máy ➔ Gây hoang mang và vi phạm tính sư phạm nghiêm trọng trong giờ học.
  * `SpeechSynthesizer` mặc định chọn giọng hệ thống Windows mà không tự chuyển đổi ngôn ngữ: Đọc tiếng Việt bằng giọng Tiếng Anh bị méo tiếng, hoặc ngược lại.
  * Thiếu thanh công cụ điều khiển âm thanh (Tạm dừng, Dừng, Điều chỉnh tốc độ đọc 0.75x / 1.0x / 1.25x) trên bảng trắng tương tác **SMART TOUCH** 86-inch.
* **Mục tiêu nâng cấp**:
  1. Bảo vệ ranh giới (Guard Clause): Chỉ đọc âm thanh khi OCR trả về `IsSuccess == true` và văn bản chứa từ hợp lệ.
  2. Tự động chuyển đổi Giọng đọc (Auto Voice Selection): Phát hiện văn bản Tiếng Anh ➔ Chọn giọng đọc `Microsoft Zira / David` (EN), phát hiện văn bản Tiếng Việt ➔ Chọn giọng đọc `Microsoft An` (VI).
  3. Tích hợp **Thanh điều khiển Âm thanh nổi (Floating Audio Controller)** trên bảng trắng với các nút bấm lớn (`Play`, `Pause`, `Stop`, `Speed` 0.75x/1.0x/1.25x) phù hợp màn hình cảm ứng 86-inch.
  4. Quản lý hủy âm thanh an toàn: Dừng phát tức thì khi giáo viên chuyển slide hoặc bấm chọn công cụ khác.

### 2. Ràng buộc Hệ thống & Quy chuẩn QA SmartClass v4.2
* **Ràng buộc Thương hiệu (`QC_4.2_LANGUAGE_BRANDING`):**
  * Giữ nguyên tuyệt đối các từ khóa thương hiệu Tiếng Anh: **SMART CLASS**, **SMART TOUCH**, **DESKTOP**.
* **Ràng buộc Bố cục Giao diện (`QC_4.2_LAYOUT_GRID`):**
  * Thanh điều khiển Âm thanh nổi phải đặt trên Root Grid kéo dãn (`HorizontalAlignment="Stretch"`), hỗ trợ di chuyển vị trí tự do trên màn 86-inch.

---

## PHẦN III: MA TRẬN PHÂN TÍCH VÀ TỰ PHẢN BIỆN KỸ THUẬT (SELF-CRITIQUE MATRIX)

Hội đồng Chuyên gia đã thực hiện tự phản biện kỹ thuật giữa các phương án triển khai để chọn ra giải pháp tối ưu nhất cho Coder thực hiện:

| Hạng mục | Phương án Cũ / Thô sơ | Phương án Đề xuất Ban đầu | Phản biện Kỹ thuật của Hội đồng | **Phương án Tối ưu Chọn thực thi** |
| :---: | :--- | :--- | :--- | :--- |
| **Kiểm tra Đầu vào Đọc văn bản** | Truyền kết quả OCR bất kể lỗi hay thành công ➔ Đọc câu lỗi C#. | Kiểm tra chuỗi `!string.IsNullOrEmpty(text)`. | Chuỗi lỗi OCR vẫn là chuỗi phi rỗng nên máy vẫn đọc câu lỗi ra loa. | **Kiểm tra `ocrResult.IsSuccess == true && text.Length >= 2`. Nếu thất bại ➔ Chỉ hiện Toast cam cảnh báo, KHÔNG phát âm.** |
| **Lựa chọn Giọng đọc (Voice)** | Dùng giọng Windows mặc định bất kể ngôn ngữ nào. | Chọn cứng giọng Tiếng Anh. | Văn bản Tiếng Việt đọc bằng giọng Tiếng Anh bị biến dạng âm thanh không nghe được. | **Phân tích Regex Tiếng Việt: Nếu chứa dấu ➔ Chọn Voice VI (`Microsoft An`), nếu không dấu ➔ Chọn Voice EN (`Zira/David`).** |
| **Thao tác Dừng / Tạm dừng** | Không có nút điều khiển ➔ Phải chờ máy đọc xong. | Dùng `MessageBox` có nút Cancel. | `MessageBox` chặn toàn bộ giao diện bảng trắng làm ngắt quãng thao tác giảng dạy. | **Tạo Thanh điều khiển nổi `FloatingAudioPlayer` chứa nút Play/Pause/Stop/Rate trên Canvas.** |
| **Tốc độ Đọc (Reading Speed)** | Đọc cố định tốc độ chuẩn. | Cho chỉnh slider nhỏ. | Slider nhỏ khó vuốt bằng ngón tay trên màn cảm ứng 86-inch. | **Tạo 3 nốt chọn tốc độ cài sẵn: `0.75x` (Đọc chậm tập đọc), `1.0x` (Chuẩn), `1.25x` (Nhanh).** |

---

## PHẦN IV: KẾ HOẠCH THỰC HIỆN CHI TIẾT TỪNG BƯỚC (STEP-BY-STEP FOR CODER)

### 📌 BƯỚC 1: Xây dựng Quản lý Giọng đọc Chuyên dụng (`SpeechSynthesizerService.cs`)

```csharp
public class SpeechSynthesizerService
{
    private SpeechSynthesizer? _synthesizer;

    public SpeechSynthesizerService()
    {
        InitializeSynthesizer();
    }

    private void InitializeSynthesizer()
    {
        try
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.Volume = 100;
            _synthesizer.Rate = 0; // Tốc độ chuẩn 1.0x
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to init SpeechSynthesizer: {ex.Message}");
        }
    }

    public void SpeakText(string text, int speedRate = 0)
    {
        if (_synthesizer == null) InitializeSynthesizer();
        if (_synthesizer == null || string.IsNullOrWhiteSpace(text)) return;

        try
        {
            _synthesizer.SpeakAsyncCancelAll();
            _synthesizer.Rate = speedRate; // -2 (0.75x), 0 (1.0x), 2 (1.25x)

            // Auto Select Voice (Phát hiện ngôn ngữ EN / VI)
            bool isVietnamese = System.Text.RegularExpressions.Regex.IsMatch(
                text, @"[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            SelectOptimalVoice(isVietnamese);

            _synthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ TTS Speak Error: {ex.Message}");
        }
    }

    public void Stop()
    {
        try { _synthesizer?.SpeakAsyncCancelAll(); } catch { }
    }

    private void SelectOptimalVoice(bool isVietnamese)
    {
        if (_synthesizer == null) return;
        try
        {
            var installedVoices = _synthesizer.GetInstalledVoices();
            foreach (var voice in installedVoices)
            {
                var info = voice.VoiceInfo;
                if (isVietnamese && (info.Culture.Name.StartsWith("vi") || info.Name.Contains("An")))
                {
                    _synthesizer.SelectVoice(info.Name);
                    return;
                }
                else if (!isVietnamese && (info.Culture.Name.StartsWith("en") || info.Name.Contains("Zira") || info.Name.Contains("David")))
                {
                    _synthesizer.SelectVoice(info.Name);
                    return;
                }
            }
        }
        catch { }
    }
}
```

---

### 📌 BƯỚC 2: Tích hợp Guard Clause & Gọi TTS trong `Form2_MainDashboard.xaml.cs`

```csharp
private async Task ExecuteSmartReadingAsync(OcrOperationResult ocrResult, Rect bounds)
{
    // Guard Clause: Ngăn chặn đọc câu lỗi OCR
    if (!ocrResult.IsSuccess || string.IsNullOrWhiteSpace(ocrResult.Text) || ocrResult.Text.Length < 2)
    {
        ShowSmartStatusBadge("⚠️ Không tìm thấy văn bản hợp lệ trong vùng chọn để đọc phát âm.");
        return;
    }

    string cleanText = ocrResult.Text.Trim();
    ShowSmartStatusBadge($"🔊 Đang đọc văn bản: \"{cleanText}\"");

    // Khởi tạo/Hiển thị Thanh điều khiển âm thanh nổi trên Canvas
    ShowFloatingAudioPlayerControl(cleanText, bounds);
}
```

---

### 📌 BƯỚC 3: Tạo Thanh Điều khiển Âm thanh Nổi (`ShowFloatingAudioPlayerControl`)

```csharp
private void ShowFloatingAudioPlayerControl(string textToRead, Rect targetBounds)
{
    // Dừng âm thanh cũ nếu đang phát
    _speechService?.Stop();
    if (_speechService == null) _speechService = new SpeechSynthesizerService();

    Border playerBorder = new Border
    {
        Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Dark Slate
        CornerRadius = new CornerRadius(24),
        Padding = new Thickness(16, 8, 16, 8),
        Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 3, Opacity = 0.3 }
    };

    StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };

    // Nút Play / Pause
    Button btnPlay = new Button { Content = "⏸️", FontSize = 18, Margin = new Thickness(4, 0, 4, 0), Width = 40, Height = 40, Style = (Style)FindResource("CircularTouchButton") };
    bool isPlaying = true;
    btnPlay.Click += (s, e) =>
    {
        if (isPlaying)
        {
            _speechService.Stop();
            btnPlay.Content = "▶️";
            isPlaying = false;
        }
        else
        {
            _speechService.SpeakText(textToRead);
            btnPlay.Content = "⏸️";
            isPlaying = true;
        }
    };

    // Nút Stop
    Button btnStop = new Button { Content = "⏹️", FontSize = 18, Margin = new Thickness(4, 0, 4, 0), Width = 40, Height = 40, Style = (Style)FindResource("CircularTouchButton") };
    btnStop.Click += (s, e) =>
    {
        _speechService.Stop();
        MainInteractiveBoard.Children.Remove(playerBorder);
        ShowSmartStatusBadge("⏹️ Đã dừng đọc âm thanh.");
    };

    // Nút Tốc độ (0.75x / 1.0x / 1.25x)
    Button btnSpeed = new Button { Content = "1.0x", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(6, 0, 6, 0), Width = 50, Height = 40 };
    int currentSpeedIndex = 1;
    int[] rates = new int[] { -2, 0, 2 }; // -2: 0.75x, 0: 1.0x, 2: 1.25x
    string[] speedLabels = new string[] { "0.75x", "1.0x", "1.25x" };

    btnSpeed.Click += (s, e) =>
    {
        currentSpeedIndex = (currentSpeedIndex + 1) % 3;
        btnSpeed.Content = speedLabels[currentSpeedIndex];
        _speechService.SpeakText(textToRead, rates[currentSpeedIndex]);
    };

    panel.Children.Add(btnPlay);
    panel.Children.Add(btnStop);
    panel.Children.Add(btnSpeed);

    playerBorder.Child = panel;

    Canvas.SetLeft(playerBorder, Math.Max(10, targetBounds.Left));
    Canvas.SetTop(playerBorder, Math.Max(10, targetBounds.Top - 60));
    Canvas.SetZIndex(playerBorder, 1000);

    MainInteractiveBoard.Children.Add(playerBorder);

    // Phát âm thanh ban đầu
    _speechService.SpeakText(textToRead);
}
```

---

## PHẦN V: BỘ CHECKSHEET KIỂM THỬ VÀ NGHIỆM THU CHI TIẾT TỪNG BƯỚC

Coder **bắt buộc phải đạt mốc `PASS` cả 8 kịch bản kiểm thử** này trước khi trình Hội đồng duyệt Release:

| STT | Bước thực hiện | Kịch bản kiểm thử (Test Case) | Điều kiện Đầu vào | Kết quả Kỳ vọng (PASS Criteria) | Kết quả Coder (PASS/FAIL) | Người nghiệm thu |
| :---: | :--- | :--- | :--- | :--- | :---: | :---: |
| **1** | Bước 2: Guard Clause | Khoanh chọn vùng bảng trống (OCR thất bại) | Bấm nút Đọc văn bản khi OCR không ra chữ | ❌ KHÔNG phát âm thanh lỗi. Chỉ hiện Toast cam cảnh báo. | `[   ]` | KTS Kiến trúc |
| **2** | Bước 1: Auto Voice EN | Khoanh chọn đoạn văn Tiếng Anh `"Hello World"` | Bấm Đọc văn bản | Tự động chọn Giọng đọc Tiếng Anh (`Microsoft Zira/David`). | `[   ]` | Chuyên gia Audio |
| **3** | Bước 1: Auto Voice VI | Khoanh chọn câu Tiếng Việt `"Xin chào các em"` | Bấm Đọc văn bản | Tự động chọn Giọng đọc Tiếng Việt (`Microsoft An`). | `[   ]` | Giáo viên Trưởng |
| **4** | Bước 3: Nút Dừng ⏹️ | Bấm nút ⏹️ trên Thanh điều khiển nổi | Đang đọc đoạn văn dài | Âm thanh DỪNG TỨC THÌ, thanh điều khiển biến mất. | `[   ]` | UI/UX Expert |
| **5** | Bước 3: Đổi Tốc độ | Bấm nút `1.0x` ➔ chuyển sang `0.75x` | Đang đọc đoạn văn | Tốc độ phát đọc chậm lại rõ rệt (0.75x) phục vụ tập đọc. | `[   ]` | Chuyên gia Sư phạm |
| **6** | Hủy âm thanh khi chuyển trang | Đang đọc âm thanh ➔ Chuyển slide bài giảng | Đang đọc âm thanh | Âm thanh tự động hủy, không bị đọc đè sang slide mới. | `[   ]` | Kỹ sư QA/QC |
| **7** | An toàn Clipboard | Kiểm tra Clipboard khi Đọc âm thanh | Bấm Đọc văn bản | Clipboard giữ nguyên dữ liệu cũ, không bị rò rỉ rác. | `[   ]` | Chuyên gia Data |
| **8** | Thương hiệu & Grid | Kiểm tra giao diện Thanh điều khiển | Màn tương tác 86-inch | Giữ nguyên từ khóa **SMART TOUCH**, **SMART CLASS**, **DESKTOP**. | `[   ]` | Chuyên gia System |

---

## PHẦN VI: TỔNG KẾT VÀ BÀN GIAO THỰC THI

Kế hoạch nâng cấp chức năng Đọc Văn bản (Text-to-Speech) đã được **Hội đồng 10 Chuyên gia** tự phản biện và phê duyệt hoàn chỉnh. Yêu cầu Lập trình viên (Coder):
1. Thực hiện chính xác theo mã nguồn mẫu tại **Bước 1, Bước 2, Bước 3**.
2. Chạy thử nghiệm và đảm bảo đánh tích `PASS` đầy đủ 8 hạng mục trong **Bộ Checksheet Kiểm thử ở Phần V**.
3. Tiến hành chuyển sang kiểm thử tính năng tiếp theo sau khi hoàn tất nghiệm thu.

---
*Phê duyệt bởi Trưởng ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập.*
