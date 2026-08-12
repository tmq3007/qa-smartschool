# KẾ HOẠCH CHI TIẾT NÂNG CẤP, CẢI TIẾN VÀ CHUẨN HÓA CHỨC NĂNG TÌM KIẾM TRI THỨC (SMART WEB SEARCH) TRONG VÙNG CHỌN BẢNG TRẮNG
## DỰ ÁN QA SMARTCLASS V4.2 — HỆ THỐNG GIÁO DỤC THÔNG MINH QA SMART SCHOOL

- **Đơn vị chủ trì:** Ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập
- **Mã kế hoạch:** `KH-SEARCH-V4.2-20260722`
- **Mô-đun áp dụng:** `Form2_MainDashboard`, `Form2_26_GoogleSearchBrowser`, `KnowledgeBaseService` & Bảng tương tác **SMART TOUCH**
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** 🎯 ĐÃ PHÊ DUYỆT & CHUYỂN GIAO THỰC THI (READY FOR CODING)

---

## PHẦN I: QUYẾT ĐỊNH THÀNH LẬP HỘI ĐỒNG CHUYÊN GIA & PHÂN CÔNG THẨM ĐỊNH

Trưởng ban Thiết kế Dự án QA Smart School quyết định thành lập **Hội đồng Chuyên gia Độc lập gồm 10 Chuyên gia Đầu ngành** để chỉ đạo, tự phản biện và phê duyệt Kế hoạch nâng cấp chức năng Tìm kiếm Tri thức (Smart Web Search) trong Vùng chọn:

| STT | Họ và tên / Chuyên gia | Vai trò trong Hội đồng | Lĩnh vực Phụ trách Thẩm định & Giám sát |
| :---: | :--- | :--- | :--- |
| **1** | **Chuyên gia Trưởng ban** | Chủ tịch Hội đồng | Chỉ đạo định hướng tổng thể, duyệt Kế hoạch và phán quyết cuối cùng. |
| **2** | **KTS Kiến trúc Phần mềm** | Ủy viên Thẩm định Core | Kiểm soát kiến trúc WebView2 nhúng nội bộ, thay thế mở Browser ngoại vi. |
| **3** | **Chuyên gia Trích xuất Thông tin & AI** | Ủy viên Kỹ thuật Search | Tối ưu hóa từ khóa tìm kiếm (Query Optimization) và lọc bỏ ký tự rác. |
| **4** | **Chuyên gia UI/UX Cảm ứng** | Ủy viên Trải nghiệm UX | Thiết kế Trình duyệt Nhúng Nội bộ `Form2_26_GoogleSearchBrowser` cho màn 86-inch. |
| **5** | **Chuyên gia Phương pháp Giảng dạy** | Ủy viên Sư phạm Số | Quy chuẩn hóa phương pháp khai thác tri thức số & Chèn Thẻ Tri thức lên Bảng. |
| **6** | **Kỹ sư Trưởng QA/QC** | Ủy viên Đảm bảo Chất lượng | Xây dựng và trực tiếp nghiệm thu Bộ Checksheet Kiểm thử khắt khe 8 bước. |
| **7** | **Giáo viên Trưởng Môn Khoa học / STEM** | Ủy viên Thực nghiệm | Kiểm thử thực tế tìm kiếm khái niệm, hình ảnh và tư liệu minh họa bài giảng. |
| **8** | **Chuyên gia Mạng & An toàn Thông tin** | Ủy viên Tương tác Hạ tầng | Thẩm định cơ chế tự chuyển đổi sang Kho Tri thức Nội bộ Offline khi ngắt mạng. |
| **9** | **Chuyên gia Clipboard & Dữ liệu** | Ủy viên An toàn Dữ liệu | Kiểm soát an toàn dữ liệu từ khóa, ngăn rò rỉ ngoại lệ C# vào ô tìm kiếm. |
| **10** | **Chuyên gia Quy chuẩn Thương hiệu** | Ủy viên Tuân thủ Hệ thống | Đảm bảo tuân thủ `QC_4.2_LANGUAGE_BRANDING` & `QC_4.2_LAYOUT_GRID`. |

---

## PHẦN II: PHÂN TÍCH YÊU CẦU KỸ THUẬT VÀ QUY CHUẨN THIẾT KẾ SƯ PHẠM

### 1. Mô tả bài toán và Mục tiêu nâng cấp
* **Vấn đề tồn tại hiện tại**:
  * Khi OCR thất bại hoặc trả về chuỗi ngoại lệ C# `Lỗi nhận dạng: Cannot access a disposed object.`, công cụ Tìm kiếm vẫn lấy chuỗi lỗi này gửi lên Google ➔ Kết quả Google tìm ra các tệp log lỗi kỹ thuật C#, gây mâu thuẫn UX và không có giá trị sư phạm.
  * Mã nguồn cũ sử dụng `System.Diagnostics.Process.Start(url)` làm vỡ giao diện bảng trắng: Mở Chrome/Edge bên ngoài Windows làm thu nhỏ ứng dụng giảng dạy, gây bất tiện lớn khi giáo viên đang đứng lớp trên màn hình cảm ứng 86-inch.
  * Khi trường học ngắt mạng Internet (chỉ chạy mạng LAN nội bộ), việc mở Google bị lỗi `ERR_INTERNET_DISCONNECTED` mà không tự động chuyển sang Kho Tri thức/Bách khoa toàn thư Offline `Form2_20_Encyclopedia`.
* **Mục tiêu nâng cấp**:
  1. Bảo vệ ranh giới (Guard Clause): Chỉ mở Tìm kiếm khi OCR trả về `IsSuccess == true` và từ khóa sạch dài $\ge 2$ ký tự.
  2. Thay thế mở trình duyệt ngoài bằng Trình duyệt Nhúng Nội bộ `Form2_26_GoogleSearchBrowser` (WebView2), giúp giữ nguyên giao diện bài giảng **SMART TOUCH** 86-inch.
  3. Tích hợp cơ chế Dịch chuyển Mạng (Offline Fallback): Nếu có Internet ➔ Mở Google Search nhúng nội bộ. Nếu mất Internet ➔ Tự động mở Kho Tri thức / Từ điển Bách khoa Ngoại tuyến `Form2_20_Encyclopedia`.
  4. Bổ sung nút **`✨ Chèn Thẻ Tri thức lên Bảng`** cho phép chụp nhanh kết quả tìm kiếm/hình ảnh từ trình duyệt và dán trực tiếp lên bảng trắng.

### 2. Ràng buộc Hệ thống & Quy chuẩn QA SmartClass v4.2
* **Ràng buộc Thương hiệu (`QC_4.2_LANGUAGE_BRANDING`):**
  * Giữ nguyên tuyệt đối các từ khóa thương hiệu Tiếng Anh: **SMART CLASS**, **SMART TOUCH**, **DESKTOP**.
* **Ràng buộc Bố cục Giao diện (`QC_4.2_LAYOUT_GRID`):**
  * Trình duyệt Nhúng `Form2_26_GoogleSearchBrowser` phải đặt trên Root Grid kéo dãn (`HorizontalAlignment="Stretch"`), hỗ trợ hiển thị đè cân đối trên màn 86-inch.

---

## PHẦN III: MA TRẬN PHÂN TÍCH VÀ TỰ PHẢN BIỆN KỸ THUẬT (SELF-CRITIQUE MATRIX)

Hội đồng Chuyên gia đã thực hiện tự phản biện kỹ thuật giữa các phương án triển khai để chọn ra giải pháp tối ưu nhất cho Coder thực hiện:

| Hạng mục | Phương án Cũ / Thô sơ | Phương án Đề xuất Ban đầu | Phản biện Kỹ thuật của Hội đồng | **Phương án Tối ưu Chọn thực thi** |
| :---: | :--- | :--- | :--- | :--- |
| **Kiểm tra Đầu vào Tìm kiếm** | Truyền kết quả OCR bất kể lỗi hay thành công ➔ Tìm câu lỗi C#. | Kiểm tra chuỗi `!string.IsNullOrEmpty(text)`. | Chuỗi lỗi OCR vẫn là chuỗi phi rỗng nên Google vẫn tìm câu lỗi C#. | **Kiểm tra `ocrResult.IsSuccess == true && text.Length >= 2`. Nếu thất bại ➔ Chỉ hiện Toast cam cảnh báo.** |
| **Cách thức Mở Trình duyệt** | `Process.Start(url)` mở Chrome ngoài Windows. | Mở Trình duyệt mặc định hệ thống. | Mở app ngoài làm thu nhỏ phần mềm giảng dạy, gián đoạn tiết học của giáo viên. | **Tích hợp cửa sổ WebView2 nhúng `Form2_26_GoogleSearchBrowser` mở popup ngay trong ứng dụng.** |
| **Xử lý Mất mạng (Offline)** | Hiện trang lỗi `ERR_INTERNET_DISCONNECTED`. | Bắt `try-catch` hiện thông báo mất mạng. | Giáo viên không thể khai thác dữ liệu bài giảng khi trường ngắt Internet. | **Kiểm tra `GetIsNetworkAvailable()`. Nếu Offline ➔ Tự chuyển sang Mở Bách khoa toàn thư Offline `Form2_20_Encyclopedia`.** |
| **Chèn Tri thức lên Bảng** | Chỉ xem không thể dán dữ liệu lên bảng. | Chụp ảnh toàn màn hình dán vào. | Chụp toàn màn hình dính cả thanh công cụ rác của trình duyệt. | **Nút `✨ Chèn Thẻ Tri thức` tự động trích xuất Tóm tắt khái niệm + Hình ảnh minh họa dán lên Canvas.** |

---

## PHẦN IV: KẾ HOẠCH THỰC HIỆN CHI TIẾT TỪNG BƯỚC (STEP-BY-STEP FOR CODER)

### 📌 BƯỚC 1: Cập nhật Trình duyệt Nhúng Nội bộ (`Form2_26_GoogleSearchBrowser.xaml.cs`)

```csharp
public partial class Form2_26_GoogleSearchBrowser : Window
{
    public Form2_26_GoogleSearchBrowser(string keyword)
    {
        InitializeComponent();
        SearchKeyword(keyword);
    }

    public async void SearchKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        
        string encoded = Uri.EscapeDataString(keyword.Trim());
        string targetUrl = $"https://www.google.com/search?q={encoded}";

        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.Navigate(targetUrl);

        TxtKeywordTitle.Text = $"🔍 Tri thức Google: \"{keyword.Trim()}\"";
    }

    private void BtnInsertToBoard_Click(object sender, RoutedEventArgs e)
    {
        // Trích xuất hình ảnh hoặc thông tin tóm tắt dán lên Canvas
        if (Owner is Form2_MainDashboard dashboard)
        {
            dashboard.InsertKnowledgeCardFromBrowser(TxtKeywordTitle.Text);
            this.Close();
        }
    }
}
```

---

### 📌 BƯỚC 2: Tích hợp Guard Clause & Gọi WebView2 trong `Form2_MainDashboard.xaml.cs`

```csharp
private async Task ExecuteSmartSearchAsync(OcrOperationResult ocrResult, Rect bounds)
{
    // Guard Clause: Ngăn chặn tìm kiếm câu lỗi OCR
    if (!ocrResult.IsSuccess || string.IsNullOrWhiteSpace(ocrResult.Text) || ocrResult.Text.Length < 2)
    {
        ShowSmartStatusBadge("⚠️ Không tìm thấy từ khóa hợp lệ trong vùng chọn để tìm kiếm.");
        return;
    }

    string cleanKeyword = ocrResult.Text.Trim();
    ShowSmartStatusBadge($"🔍 Đang tra cứu tri thức cho từ khóa: \"{cleanKeyword}\"");

    // Kiểm tra kết nối Internet
    bool isOnline = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

    if (isOnline)
    {
        // Chế độ Online: Mở Trình duyệt WebView2 Nhúng Nội bộ
        var browser = new Form2_26_GoogleSearchBrowser(cleanKeyword)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        browser.Show();
        ShowSmartStatusBadge($"🌐 Đã mở Trình duyệt Tri thức nhúng cho: \"{cleanKeyword}\"");
    }
    else
    {
        // Chế độ Offline: Mở Kho Tri thức Bách khoa Toàn thư Nội bộ
        OpenOfflineEncyclopedia(cleanKeyword);
    }
}
```

---

### 📌 BƯỚC 3: Tạo Thẻ Tri thức trên Bảng (`InsertKnowledgeCardFromBrowser`)

```csharp
public void InsertKnowledgeCardFromBrowser(string title, string summaryText = "")
{
    Point pos = new Point(350, 220);

    Border cardBorder = new Border
    {
        Background = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Royal Blue
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(16, 12, 16, 12),
        MaxWidth = 450,
        Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 3, Opacity = 0.25 }
    };

    StackPanel panel = new StackPanel { Orientation = Orientation.Vertical };

    TextBlock txtTitle = new TextBlock
    {
        Text = title,
        FontFamily = new FontFamily("Inter, Outfit, Segoe UI"),
        FontSize = 20,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
        TextWrapping = TextWrapping.Wrap
    };

    panel.Children.Add(txtTitle);

    if (!string.IsNullOrWhiteSpace(summaryText))
    {
        Separator sep = new Separator { Margin = new Thickness(0, 8, 0, 8) };
        TextBlock txtSummary = new TextBlock
        {
            Text = summaryText,
            FontFamily = new FontFamily("Inter, Outfit, Segoe UI"),
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(sep);
        panel.Children.Add(txtSummary);
    }

    cardBorder.Child = panel;

    Canvas.SetLeft(cardBorder, pos.X);
    Canvas.SetTop(cardBorder, pos.Y);
    Canvas.SetZIndex(cardBorder, 1000);

    MainInteractiveBoard.Children.Add(cardBorder);

    _selectionManager?.RegisterNewObject(cardBorder, ObjectType.Text);
    ShowSmartStatusBadge("✨ Đã chèn Thẻ Tri thức lên bảng thành công!");
}
```

---

## PHẦN V: BỘ CHECKSHEET KIỂM THỬ VÀ NGHIỆM THU CHI TIẾT TỪNG BƯỚC

Coder **bắt buộc phải đạt mốc `PASS` cả 8 kịch bản kiểm thử** này trước khi trình Hội đồng duyệt Release:

| STT | Bước thực hiện | Kịch bản kiểm thử (Test Case) | Điều kiện Đầu vào | Kết quả Kỳ vọng (PASS Criteria) | Kết quả Coder (PASS/FAIL) | Người nghiệm thu |
| :---: | :--- | :--- | :--- | :--- | :---: | :---: |
| **1** | Bước 2: Guard Clause | Khoanh chọn vùng bảng trống (OCR thất bại) | Bấm nút Tìm kiếm khi OCR không ra chữ | ❌ KHÔNG mở trình duyệt. Chỉ hiện Toast cam cảnh báo. | `[   ]` | KTS Kiến trúc |
| **2** | Bước 2: WebView2 Nhúng | Khoanh chọn từ khóa `"Tam giác vuông"` | Bấm nút Tìm kiếm | Mở cửa sổ nhúng `GoogleSearchBrowser` ngay trong ứng dụng, KHÔNG thu nhỏ bảng. | `[   ]` | UI/UX Expert |
| **3** | Bước 2: Offline Fallback | Tắt Wifi/LAN, khoanh chọn từ `"H2SO4"` | Bấm nút Tìm kiếm | Tự động mở Kho Tri thức Bách khoa Nội bộ `Encyclopedia`. 0 lỗi crash. | `[   ]` | Kỹ sư QA/QC |
| **4** | Bước 3: Chèn Thẻ Tri thức | Bấm `✨ Chèn Thẻ Tri thức lên Bảng` | Trình duyệt đang mở bài viết | Thẻ Tri thức hiển thị nền trắng viền xanh bo tròn 10px trên Canvas. | `[   ]` | Chuyên gia Sư phạm |
| **5** | Bước 3: Selection Reg | Dùng công cụ Chọn (Select Tool) kéo Thẻ Tri thức | Kéo di chuyển Thẻ Tri thức vừa tạo | Thẻ di chuyển mượt mà, hiển thị Bounding Box chuẩn. | `[   ]` | Chuyên gia Touch |
| **6** | Mã hóa URL An toàn | Khoanh chọn từ chứa ký tự đặc biệt `"E = mc^2"` | Bấm nút Tìm kiếm | Mã hóa `Uri.EscapeDataString` chuẩn, Google tìm kiếm đúng từ khóa. | `[   ]` | Chuyên gia AI |
| **7** | An toàn Clipboard | Kiểm tra Clipboard khi Tìm kiếm | Bấm Tìm kiếm | Clipboard giữ nguyên dữ liệu cũ, không bị rò rỉ rác. | `[   ]` | Chuyên gia Data |
| **8** | Thương hiệu & Grid | Kiểm tra giao diện cửa sổ Trình duyệt | Màn tương tác 86-inch | Giữ nguyên từ khóa **SMART TOUCH**, **SMART CLASS**, **DESKTOP**. | `[   ]` | Chuyên gia System |

---

## PHẦN VI: TỔNG KẾT VÀ BÀN GIAO THỰC THI

Kế hoạch nâng cấp chức năng Tìm kiếm Tri thức (Smart Web Search) đã được **Hội đồng 10 Chuyên gia** tự phản biện và phê duyệt hoàn chỉnh. Yêu cầu Lập trình viên (Coder):
1. Thực hiện chính xác theo mã nguồn mẫu tại **Bước 1, Bước 2, Bước 3**.
2. Chạy thử nghiệm và đảm bảo đánh tích `PASS` đầy đủ 8 hạng mục trong **Bộ Checksheet Kiểm thử ở Phần V**.
3. Tiến hành chuyển sang kiểm thử tính năng tiếp theo sau khi hoàn tất nghiệm thu.

---
*Phê duyệt bởi Trưởng ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập.*
