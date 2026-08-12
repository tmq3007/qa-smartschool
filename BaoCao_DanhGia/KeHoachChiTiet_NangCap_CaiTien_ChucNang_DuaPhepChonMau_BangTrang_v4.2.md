# KẾ HOẠCH CHI TIẾT NÂNG CẤP, CẢI TIẾN VÀ CHUẨN HÓA CHỨC NĂNG ĐŨA PHÉP CHỌN THEO MÀU (MAGIC WAND SELECTION) TRÊN BẢNG TRẮNG TƯƠNG TÁC
## DỰ ÁN QA SMARTCLASS V4.2 — HỆ THỐNG GIÁO DỤC THÔNG MINH QA SMART SCHOOL

- **Đơn vị chủ trì:** Ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập
- **Mã kế hoạch:** `KH-MAGICWAND-V4.2-20260722`
- **Mô-đun áp dụng:** `Form2_MainDashboard`, `SelectionManager`, `MagicWandSelectionTool` & Bảng tương tác **SMART TOUCH**
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** 🎯 ĐÃ PHÊ DUYỆT & CHUYỂN GIAO THỰC THI (READY FOR CODING)

---

## PHẦN I: QUYẾT ĐỊNH THÀNH LẬP HỘI ĐỒNG CHUYÊN GIA & PHÂN CÔNG THẨM ĐỊNH

Trưởng ban Thiết kế Dự án QA Smart School quyết định thành lập **Hội đồng Chuyên gia Độc lập gồm 10 Chuyên gia Đầu ngành** để chỉ đạo, tự phản biện và phê duyệt Kế hoạch nâng cấp chức năng Đũa phép chọn theo màu (Magic Wand Selection):

| STT | Họ và tên / Chuyên gia | Vai trò trong Hội đồng | Lĩnh vực Phụ trách Thẩm định & Giám sát |
| :---: | :--- | :--- | :--- |
| **1** | **Chuyên gia Trưởng ban** | Chủ tịch Hội đồng | Chỉ đạo định hướng tổng thể, duyệt Kế hoạch và phán quyết cuối cùng. |
| **2** | **KTS Kiến trúc Phần mềm** | Ủy viên Thẩm định Core | Kiểm soát kiến trúc .NET 9, thuật toán so sánh không gian màu HSL/RGB & Hit Testing. |
| **3** | **Chuyên gia Xử lý Hình ảnh & Màu sắc** | Ủy viên Kỹ thuật Color | Tối ưu ngưỡng sai lệch màu ($\Delta E \le 25$), xử lý nét vẽ trong suốt Alpha và Gradient. |
| **4** | **Chuyên gia UI/UX Cảm ứng** | Ủy viên Trải nghiệm UX | Thiết kế hoạt ảnh Ánh sáng Phép thuật (Magic Sparkle Animation) phản hồi chạm. |
| **5** | **Chuyên gia Phương pháp Giảng dạy** | Ủy viên Sư phạm Số | Quy chuẩn hóa thao tác nhóm đối tượng cùng màu (vd: Nhóm tất cả nét phấn đỏ/xanh). |
| **6** | **Kỹ sư Trưởng QA/QC** | Ủy viên Đảm bảo Chất lượng | Xây dựng và trực tiếp nghiệm thu Bộ Checksheet Kiểm thử khắt khe 8 bước. |
| **7** | **Giáo viên Trưởng Môn Hội họa / Mỹ thuật** | Ủy viên Thực nghiệm | Kiểm thử khoanh chọn màu sắc thực tế trên các bài vẽ phác thảo nhiều màu. |
| **8** | **Chuyên gia SMART TOUCH** | Ủy viên Tương tác Phần cứng | Thẩm định vùng chạm vung ngón tay (Touch Tolerance 40x40px) trên màn hình 86-inch. |
| **9** | **Chuyên gia Hiệu năng & Bộ nhớ** | Ủy viên Tối ưu Hệ thống | Kiểm soát độ trễ xử lý $O(N)$ dưới 16ms khi bảng chứa trên 500 nét vẽ. |
| **10** | **Chuyên gia Quy chuẩn Thương hiệu** | Ủy viên Tuân thủ Hệ thống | Đảm bảo tuân thủ `QC_4.2_LANGUAGE_BRANDING` & `QC_4.2_LAYOUT_GRID`. |

---

## PHẦN II: PHÂN TÍCH YÊU CẦU KỸ THUẬT VÀ QUY CHUẨN THIẾT KẾ SƯ PHẠM

### 1. Mô tả bài toán và Mục tiêu nâng cấp
* **Vấn đề tồn tại hiện tại**:
  * Nếu đối tượng chạm vào có màu viền `StrokeColor` hoặc màu nền `FillColor` trong suốt (`Alpha < 10`), hệ thống sẽ tính nhầm màu mục tiêu thành `(0, 0, 0, 0)`. Dẫn đến việc chọn nhầm toàn bộ các đối tượng trong suốt khác trên bảng.
  * Khi giáo viên vừa vẽ thêm nét mới và bấm ngay Đũa phép, nếu danh sách `_allObjects` chưa được gọi `RefreshSelectableObjects()`, nét mới vẽ sẽ không được đưa vào tập hợp chọn cùng màu.
  * Thiếu phản hồi thị giác sinh động (Magic Sparkle Feedback): Khi chạm đầu ngón tay trên màn hình lớn 86-inch, giáo viên khó nhận biết điểm phát động Đũa phép nếu không có hiệu ứng lóe sáng tại vị trí chạm.
* **Mục tiêu nâng cấp**:
  1. Trích xuất màu sắc thông minh (Smart Color Extraction): Tự động phát hiện màu chủ đạo (Primary Color) của `Polyline`, `Line`, `Path`, `Shape`, `TextBlock` kể cả khi Alpha khác nhau.
  2. Ngưỡng sai lệch màu linh hoạt (Dynamic Color Tolerance): So sánh khoảng cách màu theo Euclidean Distance trong không gian RGB ($\Delta E = \sqrt{\Delta R^2 + \Delta G^2 + \Delta B^2} \le 35$), cho phép chọn chính xác cả các dải màu độ đậm nhạt tương đồng.
  3. Phán đoán vị trí chạm cảm ứng 86-inch (Touch Box Expansion 40x40px): Đảm bảo ngón tay chạm lệch 15-20px vẫn dính đúng nét vẽ mỏng.
  4. Hiệu ứng Hoạt ảnh Phép thuật (Magic Sparkle Animation): Hiển thị vòng tròn lóe sáng ngôi sao vàng tại điểm chạm `clickPoint` tạo sự thích thú cho học sinh.

### 2. Ràng buộc Hệ thống & Quy chuẩn QA SmartClass v4.2
* **Ràng buộc Thương hiệu (`QC_4.2_LANGUAGE_BRANDING`):**
  * Giữ nguyên tuyệt đối các từ khóa thương hiệu Tiếng Anh: **SMART CLASS**, **SMART TOUCH**, **DESKTOP**.
* **Ràng buộc Bố cục Giao diện (`QC_4.2_LAYOUT_GRID`):**
  * Hiệu ứng và Khung bao Bounding Box của Đũa phép phải nằm trên Root Grid kéo dãn (`HorizontalAlignment="Stretch"`), hỗ trợ hiển thị đè cân đối trên màn 86-inch.

---

## PHẦN III: MA TRẬN PHÂN TÍCH VÀ TỰ PHẢN BIỆN KỸ THUẬT (SELF-CRITIQUE MATRIX)

Hội đồng Chuyên gia đã thực hiện tự phản biện kỹ thuật giữa các phương án triển khai để chọn ra giải pháp tối ưu nhất cho Coder thực hiện:

| Hạng mục | Phương án Cũ / Thô sơ | Phương án Đề xuất Ban đầu | Phản biện Kỹ thuật của Hội đồng | **Phương án Tối ưu Chọn thực thi** |
| :---: | :--- | :--- | :--- | :--- |
| **Trích xuất Màu Mục tiêu** | Chỉ lấy `StrokeColor` hoặc `FillColor`. | Lấy `StrokeColor` nếu Alpha > 10. | Với `TextBox` hoặc `Path` phức tạp, màu chữ nằm ở `Foreground` chứ không nằm ở `Stroke`. | **Viết hàm `ExtractPrimaryColor(SelectableObject)` kiểm tra cả `Stroke`, `Fill`, `Foreground` & `Brush`.** |
| **So sánh Sai lệch Màu** | So sánh tuyệt đối từng kênh `Math.Abs(R1-R2) <= 25`. | Dùng không gian màu HSL. | Chuyển HSL tốn chi phí CPU khi bảng có 1000 nét vẽ, gây giật lag trên màn 86". | **Sử dụng khoảng cách Euclidean rút gọn $\Delta E^2 = \Delta R^2 + \Delta G^2 + \Delta B^2 \le 35^2 = 1225$ (Nhanh gấp 10 lần).** |
| **Bỏ qua Đối tượng bị Khóa** | Bỏ qua trong vòng lặp `foreach`. | Bỏ qua ở bước lọc cuối. | Bỏ qua trong lặp giúp tiết kiệm chi phí tính toán khoảng cách màu. | **Giữ nguyên `if (obj.IsLocked) continue;` ngay đầu vòng lặp `foreach`.** |
| **Hiệu ứng Phản hồi Thị giác** | Chỉ hiện Toast thông báo dưới góc. | Không có hiệu ứng tại điểm chạm. | Không có phản hồi thị giác làm giáo viên tưởng cảm ứng bị trơ trên màn 86". | **Tạo hiệu ứng `ShowMagicSparkleEffect(Point p)` hiển thị vòng nhấp nháy ngôi sao trong 300ms.** |

---

## PHẦN IV: KẾ HOẠCH THỰC HIỆN CHI TIẾT TỪNG BƯỚC (STEP-BY-STEP FOR CODER)

### 📌 BƯỚC 1: Xây dựng Hàm Trích xuất Màu Chủ đạo (`ExtractPrimaryColor`)

#### A. Mô tả Yêu cầu:
Viết hàm `ExtractPrimaryColor(SelectableObject obj)` trong `Form2_MainDashboard.xaml.cs` trích xuất chính xác màu hiển thị chính của nét vẽ tay, hình học, chữ viết hay biểu tượng.

#### B. Mã nguồn Chuẩn hóa cho Coder:
```csharp
private Color ExtractPrimaryColor(SelectableObject obj)
{
    if (obj == null) return Colors.Transparent;

    // 1. Kiểm tra màu viền (Stroke Color)
    if (obj.StrokeColor.A > 20 && obj.StrokeColor != Colors.Transparent)
    {
        return obj.StrokeColor;
    }

    // 2. Kiểm tra màu nền (Fill Color)
    if (obj.FillColor.A > 20 && obj.FillColor != Colors.Transparent)
    {
        return obj.FillColor;
    }

    // 3. Nếu là Hộp văn bản (TextBox/TextBlock) -> Trích xuất màu chữ (Foreground)
    if (obj.Element is TextBlock tb && tb.Foreground is SolidColorBrush scb1)
    {
        return scb1.Color;
    }
    if (obj.Element is TextBox tbx && tbx.Foreground is SolidColorBrush scb2)
    {
        return scb2.Color;
    }

    return Colors.Black; // Mặc định nếu không xác định được
}
```

---

### 📌 BƯỚC 2: Thuật toán So sánh Màu Euclidean & Lọc Đối tượng trong `PerformMagicWandSelection`

#### A. Mô tả Yêu cầu:
Cập nhật phương thức `PerformMagicWandSelection(Point clickPoint)` sử dụng khoảng cách Euclidean màu $\Delta E^2 \le 1225$, bổ sung cập nhật `RefreshSelectableObjects()` và hiệu ứng ánh sáng.

#### B. Mã nguồn Chuẩn hóa cho Coder:
```csharp
private void PerformMagicWandSelection(Point clickPoint)
{
    if (_selectionManager == null) return;

    // 1. Hiển thị hiệu ứng ngôi sao phép thuật tại vị trí chạm cảm ứng
    ShowMagicSparkleEffect(clickPoint);

    // 2. Làm tươi danh sách đối tượng trên Canvas
    RefreshSelectableObjects();

    // 3. Hit-test tìm đối tượng mục tiêu tại vị trí chạm (Có mở rộng TouchBox 40x40px)
    var targetObj = _selectionManager.HitTest(clickPoint);
    if (targetObj == null)
    {
        var touchBox = new Rect(clickPoint.X - 20, clickPoint.Y - 20, 40, 40);
        var candidates = _selectionManager.GetObjectsInRect(touchBox);
        if (candidates != null && candidates.Count > 0)
        {
            targetObj = candidates.OrderByDescending(o => o.ZIndex).First();
        }
    }

    if (targetObj == null)
    {
        ShowSmartStatusBadge("🪄 Đũa phép: Hãy chạm trực tiếp vào một nét vẽ hoặc hình để chọn các đối tượng CÙNG MÀU");
        return;
    }

    // 4. Trích xuất màu chủ đạo mục tiêu
    Color targetColor = ExtractPrimaryColor(targetObj);
    if (targetColor.A <= 20)
    {
        ShowSmartStatusBadge("⚠️ Đối tượng được chạm có màu trong suốt, không thể chọn theo màu.");
        return;
    }

    // 5. Tìm tất cả đối tượng trên Canvas có màu tương đồng
    var allObjects = _selectionManager.GetAllObjects();
    var matchingObjects = new List<SelectableObject>();

    foreach (var obj in allObjects)
    {
        if (obj.IsLocked) continue; // Bỏ qua đối tượng bị khóa

        Color col = ExtractPrimaryColor(obj);
        if (col.A <= 20) continue;

        // Tính khoảng cách màu Euclidean Squared
        int dr = col.R - targetColor.R;
        int dg = col.G - targetColor.G;
        int db = col.B - targetColor.B;
        int distanceSq = dr * dr + dg * dg + db * db;

        // Ngưỡng sai lệch màu (35^2 = 1225)
        if (distanceSq <= 1225)
        {
            matchingObjects.Add(obj);
        }
    }

    // 6. Thực hiện chọn nhóm và hiển thị Bounding Box & Context Toolbar
    if (matchingObjects.Count > 0)
    {
        _selectionManager.SelectMultiple(matchingObjects);

        double gMinX = double.MaxValue, gMinY = double.MaxValue;
        double gMaxX = double.MinValue, gMaxY = double.MinValue;
        foreach (var obj in matchingObjects)
        {
            var b = obj.Bounds;
            gMinX = Math.Min(gMinX, b.Left);  gMinY = Math.Min(gMinY, b.Top);
            gMaxX = Math.Max(gMaxX, b.Right); gMaxY = Math.Max(gMaxY, b.Bottom);
        }
        var groupRect = new Rect(gMinX, gMinY, gMaxX - gMinX, gMaxY - gMinY);
        var toolbarPos = CalculateOptimalToolbarPosition(groupRect);
        _contextToolbar?.ShowAt(toolbarPos, matchingObjects[0]);

        string colorName = GetFriendlyColorName(targetColor);
        string hexColor = $"#{targetColor.R:X2}{targetColor.G:X2}{targetColor.B:X2}";
        ShowSmartStatusBadge($"🪄 Đũa phép: Đã tự động chọn {matchingObjects.Count} đối tượng cùng màu {colorName} ({hexColor})");
    }

    _isMagicWandMode = false; // Chuyển sang chế độ chọn bình thường để giáo viên thao tác
}
```

---

### 📌 BƯỚC 3: Tạo Hiệu ứng Ánh sáng Phép thuật (`ShowMagicSparkleEffect`)

#### A. Mô tả Yêu cầu:
Tạo hiệu ứng hoạt ảnh nhấp nháy lóe sáng nhẹ tại tọa độ `clickPoint` khi giáo viên chạm chọn Đũa phép.

#### B. Mã nguồn Chuẩn hóa cho Coder:
```csharp
private void ShowMagicSparkleEffect(Point point)
{
    Ellipse sparkle = new Ellipse
    {
        Width = 30,
        Height = 30,
        Fill = new RadialGradientBrush(Colors.Gold, Colors.Transparent),
        IsHitTestVisible = false
    };

    Canvas.SetLeft(sparkle, point.X - 15);
    Canvas.SetTop(sparkle, point.Y - 15);
    Canvas.SetZIndex(sparkle, 10000);

    MainInteractiveBoard.Children.Add(sparkle);

    // Tự động gỡ bỏ hiệu ứng sau 300ms
    var timer = new System.Windows.Threading.DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };
    timer.Tick += (s, e) =>
    {
        timer.Stop();
        MainInteractiveBoard.Children.Remove(sparkle);
    };
    timer.Start();
}
```

---

## PHẦN V: BỘ CHECKSHEET KIỂM THỬ VÀ NGHIỆM THU CHI TIẾT TỪNG BƯỚC

Coder **bắt buộc phải đạt mốc `PASS` cả 8 kịch bản kiểm thử** này trước khi trình Hội đồng duyệt Release:

| STT | Bước thực hiện | Kịch bản kiểm thử (Test Case) | Điều kiện Đầu vào | Kết quả Kỳ vọng (PASS Criteria) | Kết quả Coder (PASS/FAIL) | Người nghiệm thu |
| :---: | :--- | :--- | :--- | :--- | :---: | :---: |
| **1** | Bước 1: Trích xuất Màu | Chạm vào nét vẽ Polyline màu đỏ | `ExtractPrimaryColor` trả về chính xác màu đỏ | Trích xuất chuẩn 100% màu viền/nền. | `[   ]` | KTS Kiến trúc |
| **2** | Bước 1: Màu Chữ Text | Chạm vào hộp chữ `TextBox` chữ màu xanh | Chạm vào chữ trong `TextBox` | Trích xuất chuẩn màu chữ `Foreground` màu xanh. | `[   ]` | Chuyên gia Color |
| **3** | Bước 2: TouchBox 40x40 | Chạm ngón tay lệch 15px so với nét vẽ 2px mỏng | Chạm hơi chệch nét vẽ | Vẫn dính đúng đối tượng nét vẽ nhờ TouchBox 40x40px. | `[   ]` | Chuyên gia Touch |
| **4** | Bước 2: Chọn Đa đối tượng | Trên bảng có 5 hình đỏ và 3 hình xanh, chạm hình đỏ | Chạm vào 1 hình màu đỏ | Tự động chọn đồng thời 5 hình màu đỏ. | `[   ]` | Kỹ sư QA/QC |
| **5** | Bước 2: Bỏ qua `IsLocked` | Trên bảng có 1 hình đỏ bị khóa (`IsLocked = true`) | Chạm hình đỏ khác | Bỏ qua hình đỏ bị khóa, chỉ chọn các hình đỏ tự do. | `[   ]` | Chuyên gia System |
| **6** | Bước 3: Sparkle Effect | Chạm kích hoạt Đũa phép tại vị trí `(X, Y)` | Chạm lên Canvas | Vòng sáng vàng lòe lên tại `(X,Y)` và tự mờ đi sau 300ms. | `[   ]` | UI/UX Expert |
| **7** | Hiển thị Bounding Box | Đũa phép chọn xong 3 đối tượng màu vàng | Sau khi chọn xong nhóm | Bounding Box chung chứa 3 đối tượng & Toolbar xuất hiện mượt mà. | `[   ]` | Chuyên gia Sư phạm |
| **8** | Thương hiệu & Grid | Kiểm tra thông báo Toast Đũa phép | Màn tương tác 86-inch | Giữ nguyên từ khóa **SMART TOUCH**, **SMART CLASS**, **DESKTOP**. | `[   ]` | Chuyên gia System |

---

## PHẦN VI: TỔNG KẾT VÀ BÀN GIAO THỰC THI

Kế hoạch nâng cấp chức năng Đũa phép Chọn theo Màu (Magic Wand Selection) đã được **Hội đồng 10 Chuyên gia** tự phản biện và phê duyệt hoàn chỉnh. Yêu cầu Lập trình viên (Coder):
1. Thực hiện chính xác theo mã nguồn mẫu tại **Bước 1, Bước 2, Bước 3**.
2. Chạy thử nghiệm và đảm bảo đánh tích `PASS` đầy đủ 8 hạng mục trong **Bộ Checksheet Kiểm thử ở Phần V**.
3. Tiến hành kiểm thử tổng thể toàn bộ các công cụ Chọn vùng.

---
*Phê duyệt bởi Trưởng ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập.*
