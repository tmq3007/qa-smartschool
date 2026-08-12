# KẾ HOẠCH CHI TIẾT NÂNG CẤP, CẢI TIẾN VÀ CHUẨN HÓA CHỨC NĂNG NHẬN DIỆN CHỮ VIẾT TAY/IN (OCR) TRÊN BẢNG TRẮNG TƯƠNG TÁC
## DỰ ÁN QA SMARTCLASS V4.2 — HỆ THỐNG GIÁO DỤC THÔNG MINH QA SMART SCHOOL

- **Đơn vị chủ trì:** Ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập
- **Mã kế hoạch:** `KH-OCR-V4.2-20260722`
- **Mô-đun áp dụng:** `WindowsOCRService`, `Form2_MainDashboard` & Bảng trắng tương tác **SMART TOUCH**
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** 🎯 ĐÃ PHÊ DUYỆT & CHUYỂN GIAO THỰC THI (READY FOR CODING)

---

## PHẦN I: QUYẾT ĐỊNH THÀNH LẬP HỘI ĐỒNG CHUYÊN GIA & PHÂN CÔNG THẨM ĐỊNH

Trưởng ban Thiết kế Dự án QA Smart School quyết định thành lập **Hội đồng Chuyên gia Độc lập gồm 10 Chuyên gia Đầu ngành** để chỉ đạo, tự phản biện và phê duyệt Kế hoạch nâng cấp chức năng Nhận diện chữ OCR:

| STT | Họ và tên / Chuyên gia | Vai trò trong Hội đồng | Lĩnh vực Phụ trách Thẩm định & Giám sát |
| :---: | :--- | :--- | :--- |
| **1** | **Chuyên gia Trưởng ban** | Chủ tịch Hội đồng | Chỉ đạo định hướng tổng thể, duyệt Kế hoạch và phán quyết cuối cùng. |
| **2** | **KTS Kiến trúc Phần mềm** | Ủy viên Thẩm định Core | Kiểm soát kiến trúc .NET 9, luồng bộ nhớ WinRT `Windows.Media.Ocr` & Async Interop. |
| **3** | **Chuyên gia Thị giác Máy tính** | Ủy viên Kỹ thuật AI | Tối ưu hóa tiền xử lý ảnh (Crop, Binarization, Contrast enhancement) cho OCR. |
| **4** | **Chuyên gia UI/UX Cảm ứng** | Ủy viên Trải nghiệm UX | Triệt tiêu mâu thuẫn thông điệp giao diện, tối ưu Dialog & Toast cho màn 86-inch. |
| **5** | **Chuyên gia Phương pháp Giảng dạy** | Ủy viên Sư phạm Số | Quy chuẩn hóa định dạng Thẻ chữ in (Text Box / Font chữ Inter/Outfit trên bảng). |
| **6** | **Kỹ sư Trưởng QA/QC** | Ủy viên Đảm bảo Chất lượng | Xây dựng và trực tiếp nghiệm thu Bộ Checksheet Kiểm thử khắt khe từng bước. |
| **7** | **Giáo viên Trưởng Bộ môn** | Ủy viên Thực nghiệm | Thẩm định chất lượng nhận diện Chữ số, Tiếng Việt có dấu và Tiếng Anh thực tế. |
| **8** | **Chuyên gia SMART TOUCH** | Ủy viên Tương tác Phần cứng | Kiểm thử độ nhạy khoanh vùng chọn & phản hồi chạm nút trên màn tương tác 86". |
| **9** | **Chuyên gia Clipboard & Dữ liệu** | Ủy viên An toàn Dữ liệu | Kiểm soát an toàn Clipboard, chống rò rỉ chuỗi ngoại lệ C# vào dữ liệu người dùng. |
| **10** | **Chuyên gia Quy chuẩn Thương hiệu** | Ủy viên Tuân thủ Hệ thống | Đảm bảo tuân thủ `QC_4.2_LANGUAGE_BRANDING` & `QC_4.2_LAYOUT_GRID`. |

---

## PHẦN II: PHÂN TÍCH YÊU CẦU KỸ THUẬT VÀ QUY CHUẨN THIẾT KẾ SƯ PHẠM

### 1. Mô tả bài toán và Mục tiêu nâng cấp
* **Vấn đề tồn tại hiện tại**:
  * Khi thực hiện OCR khoanh vùng chữ viết tay/in, hệ thống bị sập luồng bộ nhớ `ObjectDisposedException` trong `WindowsOCRService.cs` (do đóng `BinaryWriter` làm hủy luồng `InMemoryRandomAccessStream`).
  * Giao diện `Form2_MainDashboard.xaml.cs` nhận chuỗi ngoại lệ C# `Lỗi nhận dạng (OCR Error): Cannot access a disposed object.`, nhưng lại hiển thị hộp thoại báo **`☑ Nhận diện chữ (OCR) thành công:`**, tự động copy chuỗi lỗi vào Clipboard và hỏi chèn chuỗi lỗi lên bảng.
* **Mục tiêu nâng cấp**:
  1. Khắc phục triệt me 100% lỗi `ObjectDisposedException` trong luồng xử lý `WindowsOCRService.cs`.
  2. Triệt tiêu hoàn toàn mâu thuẫn giao diện: Phân định rành rọt giữa **Nhận diện Thành công** (trả về chữ chuẩn) và **Nhận diện Thất bại** (hiển thị thông báo hướng dẫn ngắn gọn, KHÔNG copy lỗi vào Clipboard, KHÔNG đề xuất chèn rác lên bảng).
  3. Tiền xử lý ảnh (Crop vùng khoanh chọn, tăng độ tương phản, scaling) giúp tăng tỷ lệ nhận diện chữ viết tay/in đạt $\ge 95\%$.
  4. Tự động chuyển đổi kết quả OCR thành **Thẻ chữ in chuẩn sư phạm** (Sử dụng Font chữ hệ thống `Inter` hoặc `Outfit`, kích thước chữ thích hợp trên màn hình cảm ứng **SMART TOUCH** 86-inch).

### 2. Ràng buộc Hệ thống & Quy chuẩn QA SmartClass v4.2
* **Ràng buộc Thương hiệu (`QC_4.2_LANGUAGE_BRANDING`):**
  * Giữ nguyên tuyệt đối các từ khóa thương hiệu Tiếng Anh: **SMART CLASS**, **SMART TOUCH**, **DESKTOP**.
* **Ràng buộc Bố cục Giao diện (`QC_4.2_LAYOUT_GRID`):**
  * Hộp thoại Dialog và Toast thông báo phải đặt trên Root Grid kéo dãn (`HorizontalAlignment="Stretch"`), hỗ trợ hiển thị cân đối trên màn hình 86-inch.

---

## PHẦN III: MA TRẬN PHÂN TÍCH VÀ TỰ PHẢN BIỆN KỸ THUẬT (SELF-CRITIQUE MATRIX)

Hội đồng Chuyên gia đã thực hiện tự phản biện kỹ thuật giữa các phương án triển khai để chọn ra giải pháp tối ưu nhất cho Coder thực hiện:

| Hạng mục | Phương án Cũ / Thô sơ | Phương án Đề xuất Ban đầu | Phản biện Kỹ thuật của Hội đồng | **Phương án Tối ưu Chọn thực thi** |
| :---: | :--- | :--- | :--- | :--- |
| **Xử lý Luồng bộ nhớ Stream trong OCR** | Sử dụng `BinaryWriter` bọc luồng WinRT ➔ Bị lỗi `ObjectDisposedException`. | Ghi byte array trực tiếp qua `randomAccessStream.AsStreamForWrite().Write()`. | Nếu không flush và seek luồng đúng cách, `BitmapDecoder` của WinRT vẫn bị treo không đọc được ảnh. | **Ghi trực tiếp qua `DataWriter` chuyên dụng của WinRT `Windows.Storage.Streams` và Flush/Detach luồng an toàn.** |
| **Xử lý Kết quả Trả về khi gặp Lỗi** | Trả về chuỗi `Lỗi nhận dạng (OCR Error): ...` làm kết quả OCR. | Trả về chuỗi rỗng `string.Empty` hoặc `null`. | Nếu chỉ trả về chuỗi rỗng, UI không phân biệt được giữa việc "Không tìm thấy chữ trong hình" và "Lỗi phần cứng/engine". | **Sử dụng cấu trúc `OcrResultHolder` chứa `bool IsSuccess`, `string RecognizedText`, `string ErrorMessage`.** |
| **Giao diện Phản hồi Người dùng (UI Response)** | Hiển thị Hộp thoại thành công bất kể kết quả nào ➔ Bị lỗi mâu thuẫn UX. | Ẩn hoàn toàn Hộp thoại khi có lỗi, chỉ hiện Toast. | Giáo viên cần biết lý do nếu OCR không ra chữ (vd: vùng chọn quá nhỏ hoặc hình ảnh quá mờ). | **Chỉ mở Hộp thoại chèn chữ khi `IsSuccess == true` và có nội dung chữ hợp lệ. Khi thất bại ➔ Hiện Toast màu cam cảnh báo.** |
| **Xử lý Ảnh Vùng chọn (Image Cropping)** | Lấy toàn bộ Canvas làm ảnh đầu vào ➔ Chậm và dính rác. | Crop theo Bounding Box của vùng chọn. | Nếu chỉ crop theo Bounding Box sít sao, các nét chữ viền ngoài bị mất làm giảm độ chính xác OCR. | **Crop theo Bounding Box + Thêm Padding 15px xung quanh + Tăng tương phản trắng/đen (Contrast threshold).** |
| **Định dạng Thẻ chữ chèn lên Bảng** | Chèn TextBlock cơ bản không có khung. | Chèn Text Box chuẩn. | Cần đảm bảo chuẩn sư phạm: Dễ đọc từ khoảng cách 3-5m trong lớp học, có viền bo tròn nhẹ và bóng mờ. | **Tạo `EditableTextBox` với Font `Inter/Outfit`, Size 24pt, Padding 10px, Nền trắng viền xanh bo tròn Radius 6px.** |

---

## PHẦN IV: KẾ HOẠCH THỰC HIỆN CHI TIẾT TỪNG BƯỚC (STEP-BY-STEP FOR CODER)

### 📌 BƯỚC 1: Tối ưu hóa Luồng Bộ nhớ & Engine OCR trong `WindowsOCRService.cs`

#### A. Mô tả Yêu cầu:
Viết lại phương thức `RecognizeTextAsync` trong `QASmartClass\Services\WindowsOCRService.cs` sử dụng `DataWriter` của WinRT để chuyển đổi `BitmapSource` sang `SoftwareBitmap` an toàn, triệt tiêu 100% lỗi `ObjectDisposedException`.

#### B. Dữ liệu Đầu vào (Input):
* `BitmapSource bitmapSource`: Hình ảnh chụp vùng khoanh chọn từ Canvas.

#### C. Dữ liệu Đầu ra (Output):
* Khai báo lớp kết quả `OcrOperationResult`:
  ```csharp
  public class OcrOperationResult
  {
      public bool IsSuccess { get; set; }
      public string Text { get; set; } = string.Empty;
      public string ErrorMessage { get; set; } = string.Empty;
  }
  ```

#### D. Mã nguồn Chuẩn hóa (Coder bắt buộc làm đúng theo mẫu này):
```csharp
public async Task<OcrOperationResult> RecognizeTextAsync(BitmapSource bitmapSource)
{
    if (bitmapSource == null)
    {
        return new OcrOperationResult { IsSuccess = false, ErrorMessage = "Hình ảnh đầu vào không hợp lệ." };
    }

    if (_ocrEngine == null)
    {
        InitializeEngine();
        if (_ocrEngine == null)
        {
            return new OcrOperationResult { IsSuccess = false, ErrorMessage = "Không thể khởi tạo công cụ OCR ngoại tuyến của Windows." };
        }
    }

    try
    {
        byte[] imageBytes;
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        using (var ms = new MemoryStream())
        {
            encoder.Save(ms);
            imageBytes = ms.ToArray();
        }

        using (var randomAccessStream = new InMemoryRandomAccessStream())
        {
            using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(imageBytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }

            randomAccessStream.Seek(0);

            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessStream);
            using (SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync())
            {
                OcrResult result = await _ocrEngine.RecognizeAsync(softwareBitmap);
                
                if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                {
                    string cleanedText = result.Text.Trim();
                    System.Diagnostics.Debug.WriteLine($"✅ OCR thành công: '{cleanedText}'");
                    return new OcrOperationResult { IsSuccess = true, Text = cleanedText };
                }
                
                return new OcrOperationResult { IsSuccess = false, ErrorMessage = "Không tìm thấy chữ hoặc số trong vùng khoanh chọn." };
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"❌ Windows OCR Exception: {ex.Message}");
        return new OcrOperationResult { IsSuccess = false, ErrorMessage = $"Lỗi xử lý OCR: {ex.Message}" };
    }
}
```

---

### 📌 BƯỚC 2: Tiền xử lý Cắt hình ảnh Vùng chọn (Image Cropping with Padding)

#### A. Mô tả Yêu cầu:
Trong `Form2_MainDashboard.xaml.cs`, viết hàm `CaptureSelectedAreaBitmap(Rect selectionBounds)` để chụp đúng khu vực được khoanh chọn kèm Padding 15px, loại bỏ thành phần UI rác trước khi gửi vào OCR.

#### B. Dữ liệu Đầu vào (Input):
* `Rect selectionBounds`: Tọa độ vùng chữ nhật hoặc Bounding Box của vùng khoanh chọn.

#### C. Dữ liệu Đầu ra (Output):
* `BitmapSource`: Ảnh đã crop chuẩn nét.

#### D. Mã nguồn Chuẩn hóa cho Coder:
```csharp
private BitmapSource? CaptureSelectedAreaBitmap(Rect bounds)
{
    if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
        return null;

    // Thêm Padding 15px xung quanh để không làm đứt nét chữ sát viền
    double padding = 15;
    double left = Math.Max(0, bounds.Left - padding);
    double top = Math.Max(0, bounds.Top - padding);
    double width = Math.Min(MainInteractiveBoard.ActualWidth - left, bounds.Width + padding * 2);
    double height = Math.Min(MainInteractiveBoard.ActualHeight - top, bounds.Height + padding * 2);

    int pixelW = (int)Math.Ceiling(width);
    int pixelH = (int)Math.Ceiling(height);

    if (pixelW <= 0 || pixelH <= 0) return null;

    RenderTargetBitmap rtb = new RenderTargetBitmap(
        pixelW, pixelH, 96, 96, PixelFormats.Pbgra32);

    // Chụp chính xác phần Canvas tương ứng
    DrawingVisual dv = new DrawingVisual();
    using (DrawingContext dc = dv.RenderOpen())
    {
        VisualBrush vb = new VisualBrush(MainInteractiveBoard)
        {
            Viewbox = new Rect(left / MainInteractiveBoard.ActualWidth, 
                               top / MainInteractiveBoard.ActualHeight, 
                               width / MainInteractiveBoard.ActualWidth, 
                               height / MainInteractiveBoard.ActualHeight),
            ViewboxUnits = BrushMappingMode.RelativeToBoundingBox
        };
        dc.DrawRectangle(vb, null, new Rect(0, 0, pixelW, pixelH));
    }

    rtb.Render(dv);
    return rtb;
}
```

---

### 📌 BƯỚC 3: Xử lý Giao diện Thông báo & Triệt tiêu Mâu thuẫn UX

#### A. Mô tả Yêu cầu:
Cập nhật hàm xử lý OCR trong `Form2_MainDashboard.xaml.cs`:
1. Nếu `result.IsSuccess == true`:
   - Copy `result.Text` vào Clipboard.
   - Hiển thị Toast thông báo xanh: `☑ Nhận diện chữ OCR thành công: "<text>"`.
   - Hiển thị Hộp thoại hỏi chèn thẻ chữ in với đáp án rõ ràng.
2. Nếu `result.IsSuccess == false`:
   - KHÔNG copy vào Clipboard.
   - KHÔNG hiển thị Hộp thoại hỏi chèn rác lên bảng.
   - Hiển thị Toast thông báo màu cam/đỏ: `⚠️ Nhận diện OCR không thành công: <ErrorMessage>`.

#### B. Mã nguồn Chuẩn hóa cho Coder:
```csharp
private async Task ProcessOCRSelectionAsync(Rect bounds)
{
    var croppedBitmap = CaptureSelectedAreaBitmap(bounds);
    if (croppedBitmap == null)
    {
        ShowSmartStatusBadge("⚠️ Vùng chọn không hợp lệ để nhận diện OCR.");
        return;
    }

    ShowSmartStatusBadge("🔍 Đang xử lý nhận diện chữ (OCR)...");

    var ocrService = new QASmartTouch.Services.WindowsOCRService();
    var result = await ocrService.RecognizeTextAsync(croppedBitmap);

    if (result.IsSuccess)
    {
        // 1. Chỉ copy dữ liệu chuẩn vào Clipboard
        Clipboard.SetText(result.Text);
        ShowSmartStatusBadge($"☑ Nhận diện chữ OCR thành công: \"{result.Text}\" (Đã chép vào bộ nhớ tạm)");

        // 2. Hỏi giáo viên có muốn chèn Thẻ chữ in lên bảng không
        var dlgResult = MessageBox.Show(
            $"☑ Nhận diện chữ (OCR) thành công:\n\n\"{result.Text}\"\n\n(Đã sao chép vào bộ nhớ tạm)\n\nBạn có muốn CHÈN THẺ CHỮ IN NÀY LÊN BẢNG không?",
            "Nhận diện chữ OCR - SMART TOUCH",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (dlgResult == MessageBoxResult.Yes)
        {
            InsertPrintedTextCardOnBoard(result.Text, bounds);
        }
    }
    else
    {
        // ❌ Thất bại: Tuyệt đối KHÔNG copy rác vào Clipboard, KHÔNG hỏi chèn thẻ chữ
        ShowSmartStatusBadge($"⚠️ Nhận diện OCR không thành công: {result.ErrorMessage}");
        System.Diagnostics.Debug.WriteLine($"⚠️ OCR Failed: {result.ErrorMessage}");
    }
}
```

---

### 📌 BƯỚC 4: Tạo Thẻ Chữ In Chuẩn Sư Phạm (`InsertPrintedTextCardOnBoard`)

#### A. Mô tả Yêu cầu:
Tạo hàm chèn Thẻ chữ in đẹp mắt lên bảng tại đúng vị trí vùng chọn, tuân thủ bộ quy chuẩn sư phạm cho màn hình 86-inch.

#### B. Mã nguồn Chuẩn hóa cho Coder:
```csharp
private void InsertPrintedTextCardOnBoard(string text, Rect targetBounds)
{
    TextBox card = new TextBox
    {
        Text = text,
        FontFamily = new FontFamily("Inter, Outfit, Segoe UI"),
        FontSize = 24,
        FontWeight = FontWeights.Medium,
        Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Dark Slate
        Background = Brushes.White,
        BorderBrush = new SolidColorBrush(Color.FromRgb(52, 152, 219)), // Branding Blue
        BorderThickness = new Thickness(2),
        Padding = new Thickness(12, 8, 12, 8),
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinWidth = 120,
        MinHeight = 50
    };

    // Đặt vị trí tại vùng chọn
    Canvas.SetLeft(card, targetBounds.Left);
    Canvas.SetTop(card, targetBounds.Top);
    Canvas.SetZIndex(card, 500);

    MainInteractiveBoard.Children.Add(card);

    // Đăng ký với SelectionManager để giáo viên di chuyển/phóng to sau này
    _selectionManager?.RegisterNewObject(card, ObjectType.Text);
    ShowSmartStatusBadge("✨ Đã chèn Thẻ chữ in lên bảng thành công!");
}
```

---

## PHẦN V: BỘ CHECKSHEET KIỂM THỬ VÀ NGHIỆM THU CHI TIẾT TỪNG BƯỚC

Coder phải vượt qua toàn bộ **8 Hạng mục Kiểm thử** dưới đây mới được coi là hoàn thành nhiệm vụ:

| STT | Bước thực hiện | Kịch bản kiểm thử (Test Case) | Điều kiện Đầu vào | Kết quả Kỳ vọng (PASS Criteria) | Kết quả Coder (PASS/FAIL) | Người nghiệm thu |
| :---: | :--- | :--- | :--- | :--- | :---: | :---: |
| **1** | Bước 1: Core OCR Stream | Kiểm tra luồng `InMemoryRandomAccessStream` không bị disposed | Chạy OCR 10 lần liên tiếp với nét chữ viết tay số `12` | Không ném ngoại lệ `ObjectDisposedException`. 0 lỗi sập luồng. | `[   ]` | KTS Kiến trúc |
| **2** | Bước 1: Return Object | Kiểm tra cấu trúc trả về `OcrOperationResult` | Bật OCR ở vùng không có chữ | `IsSuccess = false`, `ErrorMessage` rõ ràng, không bị crash. | `[   ]` | Kỹ sư QA/QC |
| **3** | Bước 2: Crop Image | Chụp ảnh vùng chọn có Padding 15px | Khoanh chọn sát viền chữ nét vẽ | Ảnh crop giữ nguyên các viền nét chữ, không bị cụt viền. | `[   ]` | Chuyên gia AI |
| **4** | Bước 3: Clipboard Safety | Kiểm tra dữ liệu bộ nhớ tạm khi OCR thất bại | Khoanh chọn vùng bảng trống không có chữ | Clipboard GIỮ NGUYÊN dữ liệu cũ, KHÔNG bị ghi đè chuỗi lỗi C#. | `[   ]` | Chuyên gia Data |
| **5** | Bước 3: Triệt tiêu Mâu thuẫn UX | Kiểm tra Hộp thoại & Toast khi OCR thất bại | Khoanh chọn vùng bảng trống không có chữ | ❌ KHÔNG hiện Hộp thoại "Thành công". Chỉ hiện Toast cam thông báo. | `[   ]` | Chuyên gia UI/UX |
| **6** | Bước 3: UX Thành công | Kiểm tra Hộp thoại khi OCR nhận diện ra chữ số | Khoanh chọn chữ viết tay số `2` | Hiển thị Hộp thoại thành công với nội dung `"2"`. Đã copy `"2"` vào Clipboard. | `[   ]` | Giáo viên Trưởng |
| **7** | Bước 4: Chèn Thẻ chữ | Bấm `Yes` trên Hộp thoại nhận diện thành công | Nội dung OCR = `"2"` | Thẻ chữ in hiển thị chữ `"2"` trắng viền xanh, Font Inter/Outfit Size 24. | `[   ]` | Chuyên gia Sư phạm |
| **8** | Bước 4: Đăng ký Selection | Thao tác chọn/kéo thẻ chữ in vừa tạo | Dùng công cụ Chọn (Select Tool) kéo thẻ chữ vừa tạo | Thẻ chữ di chuyển mượt mà, hiển thị Bounding Box chọn chuẩn. | `[   ]` | Chuyên gia Touch |

---

## PHẦN VI: TỔNG KẾT VÀ BÀN GIAO THỰC THI

Kế hoạch nâng cấp đã được **Hội đồng 10 Chuyên gia** tự phản biện và phê duyệt hoàn chỉnh. Yêu cầu Lập trình viên (Coder):
1. Thực hiện chính xác theo mã nguồn mẫu tại **Bước 1, Bước 2, Bước 3, Bước 4**.
2. Chạy thử nghiệm và đảm bảo đánh tích `PASS` đầy đủ 8 hạng mục trong **Bộ Checksheet Kiểm thử ở Phần V**.
3. Tiến hành Build Release nghiệm thu chính thức cho hệ thống **QA SmartClass v4.2**.

---
*Phê duyệt bởi Trưởng ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập.*
