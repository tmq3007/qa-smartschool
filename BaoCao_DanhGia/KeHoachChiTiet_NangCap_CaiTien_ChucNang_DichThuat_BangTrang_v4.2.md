# KẾ HOẠCH CHI TIẾT NÂNG CẤP, CẢI TIẾN VÀ CHUẨN HÓA CHỨC NĂNG DỊCH THUẬT THÔNG MINH (SMART TRANSLATE) TRONG VÙNG CHỌN BẢNG TRẮNG
## DỰ ÁN QA SMARTCLASS V4.2 — HỆ THỐNG GIÁO DỤC THÔNG MINH QA SMART SCHOOL

- **Đơn vị chủ trì:** Ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập
- **Mã kế hoạch:** `KH-TRANSLATE-V4.2-20260722` (Phiên bản Bổ sung & Tối ưu hóa Chuyên sâu theo Phản biện 'N')
- **Mô-đun áp dụng:** `Form2_MainDashboard`, `Form2_24_BingTranslator`, `VocabularyService`, `TranslationCacheRepository` & Bảng tương tác **SMART TOUCH**
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** 🎯 ĐÃ HOÀN THIỆN CHUYÊN SÂU & CHUYỂN GIAO THỰC THI (READY FOR CODING)

---

## PHẦN I: QUYẾT ĐỊNH THÀNH LẬP HỘI ĐỒNG CHUYÊN GIA & PHÂN CÔNG THẨM ĐỊNH

Trưởng ban Thiết kế Dự án QA Smart School quyết định thành lập **Hội đồng Chuyên gia Độc lập gồm 10 Chuyên gia Đầu ngành** để chỉ đạo, tự phản biện và phê duyệt Kế hoạch nâng cấp chức năng Dịch thuật Thông minh trong Vùng chọn:

| STT | Họ và tên / Chuyên gia | Vai trò trong Hội đồng | Lĩnh vực Phụ trách Thẩm định & Giám sát |
| :---: | :--- | :--- | :--- |
| **1** | **Chuyên gia Trưởng ban** | Chủ tịch Hội đồng | Chỉ đạo định hướng tổng thể, duyệt Kế hoạch và phán quyết cuối cùng. |
| **2** | **KTS Kiến trúc Phần mềm** | Ủy viên Thẩm định Core | Kiểm soát kiến trúc .NET 9, luồng xử lý bất đồng bộ Async/Await & Guard Clauses. |
| **3** | **Chuyên gia Ngôn ngữ & NLP** | Ủy viên Kỹ thuật NLP | Tối ưu nhận diện ngôn ngữ tự động (Auto-Detect EN/VI), Thuật ngữ STEM & Từ điển Offline. |
| **4** | **Chuyên gia UI/UX Cảm ứng** | Ủy viên Trải nghiệm UX | Thiết kế giao diện Cửa sổ Dịch thuật và Thẻ chữ Song ngữ phát âm chuẩn màn 86-inch. |
| **5** | **Chuyên gia Phương pháp Giảng dạy** | Ủy viên Sư phạm Số | Quy chuẩn hóa phương pháp dạy học song ngữ EN - VI trên Bảng tương tác. |
| **6** | **Kỹ sư Trưởng QA/QC** | Ủy viên Đảm bảo Chất lượng | Xây dựng và trực tiếp nghiệm thu Bộ Checksheet Kiểm thử khắt khe 10 bước. |
| **7** | **Giáo viên Trưởng Môn Tiếng Anh** | Ủy viên Thực nghiệm | Kiểm thử thực tế khả năng dịch từ vựng, ngữ pháp và thuật ngữ môn học STEM. |
| **8** | **Chuyên gia CSDL & Bộ nhớ Tạm** | Ủy viên Tương tác Hạ tầng | Thiết kế cơ chế Lưu đệm Offline (SQLite Translation Cache Engine) khi mất mạng. |
| **9** | **Chuyên gia Clipboard & Dữ liệu** | Ủy viên An toàn Dữ liệu | Kiểm soát an toàn Clipboard, ngăn rò rỉ ngoại lệ C# vào bộ nhớ tạm người dùng. |
| **10** | **Chuyên gia Quy chuẩn Thương hiệu** | Ủy viên Tuân thủ Hệ thống | Đảm bảo tuân thủ `QC_4.2_LANGUAGE_BRANDING` & `QC_4.2_LAYOUT_GRID`. |

---

## PHẦN II: TỔNG HỢP CÁC ĐIỂM BỔ SUNG NÂNG CẤP CHUYÊN SÂU (ADVANCED UPGRADES)

Dựa trên lượt rà soát và phản biện **N**, Hội đồng Chuyên gia bổ sung **4 tính năng cao cấp** cho công cụ Dịch thuật:

### 1. Cơ chế Bộ đệm lưu trữ Dịch thuật Offline (SQLite Translation Cache):
* Khi có kết quả dịch thuật Online thành công từ Bing/Google API, hệ thống tự động lưu bản ghi `(SourceText, TargetText, LanguagePair)` vào cơ sở dữ liệu `smartclass.db`.
* Khi mất mạng Internet về sau, nếu giáo viên khoanh chọn lại câu/từ đã từng dịch, hệ thống sẽ ưu tiên nạp ngay từ bộ đệm SQLite Cache mà không sợ bị thiếu dữ liệu từ điển.

### 2. Tra cứu Thuật ngữ Chuyên ngành STEM (Math/Physics/Chemistry Terms):
* Tích hợp kho từ vựng thuật ngữ Toán - Lý - Hóa (vd: `"Triangle"` ➔ `"Hình tam giác"`, `"Bisector"` ➔ `"Đường phân giác"`, `"Velocity"` ➔ `"Vận tốc"`, `"Acceleration"` ➔ `"Gia tốc"`).

### 3. Tích hợp Phát âm Nhanh Song ngữ (Pronunciation Sound Player):
* Bổ sung nút bấm **🔊 Phát âm chuẩn** trên Hộp thoại Dịch thuật và Thẻ chữ Song ngữ (Sử dụng `SpeechSynthesizer` giọng đọc phát âm tiếng Anh/tiếng Việt chất lượng cao).

### 4. Tương thích Cảm ứng Màn hình lớn **SMART TOUCH** (86-inch):
* Các nút bấm trong cửa sổ Dịch thuật được nâng kích thước chạm tối thiểu `48x48px`, khoảng cách nút `12px`, phông chữ lớn `20pt` giúp giáo viên dễ chạm chọn bằng ngón tay hoặc bút cảm ứng.

---

## PHẦN III: MA TRẬN PHÂN TÍCH VÀ TỰ PHẢN BIỆN KỸ THUẬT NÂNG CAO

| Hạng mục | Phương án Thô sơ | Phản biện Kỹ thuật của Hội đồng | **Phương án Tối ưu Chọn Thực thi** |
| :---: | :--- | :--- | :--- |
| **Kiểm tra Đầu vào Dịch thuật** | Truyền kết quả OCR bất kể lỗi hay thành công ➔ Dịch câu lỗi. | Chuỗi lỗi OCR vẫn là chuỗi phi rỗng nên UI vẫn mở cửa sổ dịch lỗi. | **Kiểm tra `ocrResult.IsSuccess == true && text.Length >= 2`. Nếu thất bại ➔ Chỉ hiện Toast cam cảnh báo.** |
| **Xử lý Mất mạng (Offline)** | Đợi API Timeout 30s ➔ Ném lỗi crash. | Bắt exception `try-catch` và hiển thị thông báo lỗi mạng. | **Kiểm tra `GetIsNetworkAvailable()`. Nếu Offline ➔ Ưu tiên tra SQLite Translation Cache, nếu không có ➔ Tra Từ điển Nội bộ `VocabularyData.json`.** |
| **Lưu đệm Dịch thuật** | Không lưu lại ➔ Mất mạng là mất bản dịch. | Lưu ra tệp text tạm ➔ Dễ bị xóa hoặc mất đồng bộ. | **Lưu bộ đệm vào bảng `TranslationCache` trong `smartclass.db` kèm chỉ mục tìm kiếm nhanh.** |
| **Phát âm Từ vựng** | Chỉ dịch chữ không phát âm. | Phát âm qua Google TTS Online ➔ Sẽ hỏng khi ngắt mạng. | **Tích hợp `SpeechSynthesizer` offline ngoại tuyến của Windows, tự động chọn Voice EN hoặc VI tương ứng.** |
| **Hiển thị Kết quả trên Bảng** | Chèn TextBlock đơn giản. | Học ngoại ngữ cần hiển thị dạng Song ngữ (từ gốc + nghĩa) có nút phát âm. | **Chèn `BilingualCardControl` (Ví dụ: `Square / Hình vuông`), có nút 🔊 Phát âm và viền bo tròn 8px.** |

---

## PHẦN IV: KẾ HOẠCH THỰC HIỆN CHI TIẾT TỪNG BƯỚC (STEP-BY-STEP FOR CODER)

### 📌 BƯỚC 1: Xây dựng Bộ Dịch thuật Ngoại tuyến Kép (`VocabularyService` & `SQLite Cache`)

```csharp
public class VocabularyService
{
    private static Dictionary<string, string>? _offlineDict;

    public static string LookupOffline(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string key = text.Trim().ToLowerInvariant();

        // 1. Kiểm tra SQLite Cache trước
        string cachedResult = GetCachedTranslation(key);
        if (!string.IsNullOrEmpty(cachedResult))
        {
            return cachedResult;
        }

        // 2. Tra từ điển nội bộ VocabularyData.json
        EnsureDictionaryLoaded();
        if (_offlineDict != null && _offlineDict.TryGetValue(key, out string? meaning))
        {
            return meaning;
        }

        return $"[Từ điển Offline]: Không tìm thấy từ \"{text}\" trong CSDL nội bộ.";
    }

    public static void SaveToCache(string sourceText, string translatedText, string langPair)
    {
        try
        {
            // Lưu vào SQLite smartclass.db bảng TranslationCache
            AppDbContext.SaveTranslationCache(sourceText.Trim().ToLowerInvariant(), translatedText.Trim(), langPair);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Failed to save translation cache: {ex.Message}");
        }
    }

    private static string GetCachedTranslation(string key)
    {
        try
        {
            return AppDbContext.GetTranslationFromCache(key);
        }
        catch { return string.Empty; }
    }

    private static void EnsureDictionaryLoaded()
    {
        if (_offlineDict != null) return;
        try
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data", "Language", "VocabularyData.json");
            if (File.Exists(dbPath))
            {
                string json = File.ReadAllText(dbPath);
                var rawDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                _offlineDict = new Dictionary<string, string>(rawDict ?? new(), StringComparer.OrdinalIgnoreCase);
            }
        }
        catch { _offlineDict = new Dictionary<string, string>(); }
    }
}
```

---

### 📌 BƯỚC 2: Thêm Guard Clause, Auto-Detect Language & Phát âm trong `Form2_MainDashboard.xaml.cs`

```csharp
private async Task ExecuteSmartTranslationAsync(OcrOperationResult ocrResult, Rect bounds)
{
    // Guard Clause: Ngăn chặn lan truyền lỗi OCR
    if (!ocrResult.IsSuccess || string.IsNullOrWhiteSpace(ocrResult.Text) || ocrResult.Text.Length < 2)
    {
        ShowSmartStatusBadge("⚠️ Không tìm thấy văn bản hợp lệ trong vùng chọn để dịch thuật.");
        return;
    }

    string sourceText = ocrResult.Text.Trim();
    bool isVietnamese = System.Text.RegularExpressions.Regex.IsMatch(
        sourceText, @"[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    string targetLang = isVietnamese ? "en" : "vi";
    bool isOnline = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

    if (isOnline)
    {
        var bingTranslator = new Form2_24_BingTranslator(sourceText) { Owner = this };
        bingTranslator.Show();
        ShowSmartStatusBadge($"🌐 Đã mở Dịch thuật Online cho: \"{sourceText}\"");
    }
    else
    {
        string offlineResult = QASmartTouch.Services.VocabularyService.LookupOffline(sourceText);
        ShowOfflineTranslationDialog(sourceText, offlineResult, bounds);
    }
}
```

---

### 📌 BƯỚC 3: Tạo Thẻ Chữ Song ngữ Tích hợp Phát âm 🔊 (`InsertBilingualCardOnBoard`)

```csharp
private void InsertBilingualCardOnBoard(string originalText, string translatedText, Rect targetBounds)
{
    Border cardBorder = new Border
    {
        Background = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald Green
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(14, 10, 14, 10),
        Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black, Direction = 270, ShadowDepth = 3, Opacity = 0.2, BlurRadius = 8
        }
    };

    StackPanel panel = new StackPanel { Orientation = Orientation.Vertical };

    // Dòng 1: Nguyên bản + Nút Phát âm
    StackPanel row1 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
    TextBlock txtOriginal = new TextBlock
    {
        Text = originalText,
        FontFamily = new FontFamily("Inter, Outfit, Segoe UI"),
        FontSize = 22,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
    };
    Button btnSpeak = new Button
    {
        Content = "🔊",
        FontSize = 18,
        Margin = new Thickness(10, 0, 0, 0),
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Cursor = Cursors.Hand,
        ToolTip = "Phát âm từ vựng"
    };
    btnSpeak.Click += (s, e) => SpeakText(originalText);

    row1.Children.Add(txtOriginal);
    row1.Children.Add(btnSpeak);

    // Đường phân cách
    Separator sep = new Separator { Margin = new Thickness(0, 6, 0, 6), Background = Brushes.LightGray };

    // Dòng 2: Bản dịch
    TextBlock txtTranslated = new TextBlock
    {
        Text = translatedText,
        FontFamily = new FontFamily("Inter, Outfit, Segoe UI"),
        FontSize = 20,
        FontWeight = FontWeights.Medium,
        Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
        HorizontalAlignment = HorizontalAlignment.Center
    };

    panel.Children.Add(row1);
    panel.Children.Add(sep);
    panel.Children.Add(txtTranslated);

    cardBorder.Child = panel;

    Canvas.SetLeft(cardBorder, targetBounds.Left);
    Canvas.SetTop(cardBorder, targetBounds.Top);
    Canvas.SetZIndex(cardBorder, 500);

    MainInteractiveBoard.Children.Add(cardBorder);

    _selectionManager?.RegisterNewObject(cardBorder, ObjectType.Text);
    ShowSmartStatusBadge("✨ Đã chèn Thẻ chữ Song ngữ có phát âm lên bảng thành công!");
}
```

---

## PHẦN V: BỘ CHECKSHEET KIỂM THỬ KHẮT KHE MỞ RỘNG (10 HẠNG MỤC)

| STT | Kịch bản kiểm thử (Test Case) | Điều kiện Đầu vào | Kết quả Kỳ vọng (PASS Criteria) | Người nghiệm thu |
| :---: | :--- | :--- | :--- | :---: |
| **1** | Chống lan truyền lỗi OCR | Khoanh chọn vùng bảng trống (OCR thất bại) | ❌ KHÔNG mở cửa sổ Dịch thuật. Chỉ hiện Toast cam thông báo. | KTS Kiến trúc |
| **2** | Ngắt kết nối Mạng (Offline Mode) | Tắt Wifi/LAN, khoanh chọn từ `"Triangle"` | Tra cứu từ điển nội bộ trả về `"Hình tam giác"`. 0 lỗi crash. | Kỹ sư QA/QC |
| **3** | Nạp Bộ đệm SQLite Cache | Dịch online từ `"Hypotenuse"`, sau đó tắt mạng và dịch lại | Hệ thống nạp tức thì từ `TranslationCache` trong SQLite (`Cạnh huyền`). | CSDL Expert |
| **4** | Nhận diện Ngôn ngữ EN | Khoanh chọn từ Tiếng Anh `"Square"` | Tự động chọn ngôn ngữ đích là Tiếng Việt (`vi`). | Chuyên gia NLP |
| **5** | Nhận diện Ngôn ngữ VI | Khoanh chọn từ Tiếng Việt `"Hình tròn"` | Tự động chọn ngôn ngữ đích là Tiếng Anh (`en`). | Giáo viên Trưởng |
| **6** | Phát âm Nhanh 🔊 | Bấm nút 🔊 trên Thẻ chữ Song ngữ | Giọng đọc `SpeechSynthesizer` phát âm chuẩn từ `"Triangle"`. | Chuyên gia Sư phạm |
| **7** | Chèn Thẻ chữ Song ngữ | Bấm "Chèn Thẻ chữ Song ngữ lên Bảng" | Thẻ chữ Song ngữ hiển thị chuẩn 2 dòng có nút 🔊 viền xanh bo tròn. | UI/UX Expert |
| **8** | Chọn & Di chuyển Thẻ Song ngữ | Dùng công cụ Chọn (Select Tool) kéo Thẻ Song ngữ | Thẻ chữ di chuyển mượt mà, hiển thị Bounding Box chuẩn. | Chuyên gia Touch |
| **9** | An toàn Clipboard | Kiểm tra Clipboard khi Dịch thuật Offline | Clipboard không bị rò rỉ chuỗi ngoại lệ C#. | Chuyên gia Data |
| **10** | Thương hiệu & Grid Layout | Kiểm tra các nhãn cửa sổ Dịch thuật trên màn 86" | Giữ nguyên từ khóa **SMART TOUCH**, **SMART CLASS**, **DESKTOP**. | Chuyên gia System |

---
*Phê duyệt bởi Trưởng ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập.*
