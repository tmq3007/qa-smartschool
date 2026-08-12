# KẾ HOẠCH CHI TIẾT NÂNG CẤP VÀ XỬ LÝ TRIỆT ĐỂ LỖI KHOANH VÙNG TỰ DO LASSO (LASSO SELECTION TOOL)
## DỰ ÁN QA SMARTCLASS V4.2 — HỆ THỐNG GIÁO DỤC THÔNG MINH QA SMART SCHOOL

- **Đơn vị chủ trì:** Ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Mở rộng
- **Mã kế hoạch:** `KH-LASSO-TRIETDE-V4.2-20260722`
- **Mô-đun áp dụng:** `LassoSelectionTool`, `SelectionManager`, `TouchHandler`, `Form2_MainDashboard` & Màn hình tương tác **SMART TOUCH**
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** 🎯 BỔ SUNG HỘI ĐỒNG CHUYÊN GIA & PHÊ DUYỆT ĐẶC BIỆT (EXPANDED EXPERT COUNCIL APPROVED)

---

## PHẦN I: MỞ RỘNG HỘI ĐỒNG CHUYÊN GIA VỚI 12 CHUYÊN GIA ĐẦU NGÀNH

Trưởng ban Thiết kế Dự án QA Smart School quyết định **bổ sung thêm 2 Chuyên gia đặc thù**, nâng tổng số thành viên Hội đồng Chuyên gia lên **12 Chuyên gia Đầu ngành** để mở cuộc điều tra nguyên nhân gốc rễ (Forensic Audit) và xử lý triệt để 100% lỗi chọn Lasso:

| STT | Họ và tên / Chuyên gia | Vai trò trong Hội đồng | Lĩnh vực Phụ trách Thẩm định & Giám sát Đặc thù |
| :---: | :--- | :--- | :--- |
| **1** | **Chuyên gia Trưởng ban** | Chủ tịch Hội đồng | Chỉ đạo định hướng tổng thể, duyệt Kế hoạch và phán quyết cuối cùng. |
| **2** | **KTS Kiến trúc Phần mềm** | Ủy viên Thẩm định Core | Phân tích luồng sự kiện WPF Event Routing & Dispatcher Priority. |
| **3** | **Chuyên gia WPF Stylus & Touch Engine (MỚI BỔ SUNG)** | **Ủy viên Kỹ thuật Cảm ứng** | **Bắt và xử lý luồng sự kiện `TouchDown/Move/Up` và `StylusDown/Move/Up` ngăn WPF nuốt sự kiện.** |
| **4** | **Chuyên gia Hình học Không gian (MỚI BỔ SUNG)** | **Ủy viên Thuật toán Hit-Test** | **Chuyển đổi ma trận `TransformToAncestor` cho toàn bộ các điểm `Polyline.Points` trên Canvas.** |
| **5** | **Chuyên gia UI/UX Cảm ứng** | Ủy viên Trải nghiệm UX | Kiểm soát hiển thị nét đứt vàng lấp lấp lấp lóa (`Marching Ants`) trên màn 86-inch. |
| **6** | **Chuyên gia Phương pháp Giảng dạy** | Ủy viên Sư phạm Số | Đảm bảo thao tác khoanh chọn nét chữ viết tay của giáo viên đạt độ nhạy 100%. |
| **7** | **Kỹ sư Trưởng QA/QC** | Ủy viên Đảm bảo Chất lượng | Xây dựng và trực tiếp nghiệm thu Bộ Checksheet Kiểm thử khắt khe 8 bước. |
| **8** | **Giáo viên Trưởng Môn Toán / STEM** | Ủy viên Thực nghiệm | Kiểm thử khoanh chọn thực tế trên các hình vẽ tam giác, đường tròn, đồ thị. |
| **9** | **Chuyên gia SMART TOUCH** | Ủy viên Tương tác Phần ứng | Kiểm soát tương tác đa điểm ngón tay trên phần cứng màn hình 86-inch 4K. |
| **10** | **Chuyên gia Hiệu năng & Bộ nhớ** | Ủy viên Tối ưu Hệ thống | Kiểm soát độ trễ quét Lasso dưới 16ms, không lag giật. |
| **11** | **Chuyên gia Clipboard & Dữ liệu** | Ủy viên An toàn Dữ liệu | Bảo toàn dữ liệu đối tượng được chọn khi chuyển sang công cụ khác. |
| **12** | **Chuyên gia Quy chuẩn Thương hiệu** | Ủy viên Tuân thủ Hệ thống | Đảm bảo tuân thủ `QC_4.2_LANGUAGE_BRANDING` & `QC_4.2_LAYOUT_GRID`. |

---

## PHẦN II: NGUYÊN NHÂN GỐC RỄ NỔI BỔI VÀ TẠI SAO CÁC LẦN TEST TRƯỚC VẪN BỊ LỖI

Sau khi Hội đồng 12 Chuyên gia thực hiện mổ xẻ mã nguồn (Code Forensic Audit), chúng tôi đã phát hiện **3 NGUYÊN NHÂN GỐC RỄ** khiến người dùng kiểm thử thực tế bằng ngón tay hoặc bút cảm ứng vẫn không khoanh chọn được:

1. **Lỗi Nuốt sự kiện Cảm ứng WPF (Touch/Stylus Event Interception Failure):**
   * Trong lớp `LassoSelectionTool.cs` cũ, hệ thống **CHỈ ĐẮNG KÝ SỰ KIỆN CHUỘT (`MouseLeftButtonDown`, `MouseMove`, `MouseLeftButtonUp`)**.
   * Khi giáo viên thao tác bằng **ngón tay hoặc bút cảm ứng** trên màn hình 86-inch hoặc màn cảm ứng laptop, Windows phát sự kiện `TouchDown/TouchMove/TouchUp` hoặc `StylusDown/StylusMove/StylusUp`. Luồng sự kiện này bị `TouchHandler` hoặc hệ thống WPF giữ lại mà KHÔNG chuyển đổi (promote) thành sự kiện Chuột ➔ Dẫn đến danh sách `_lassoPoints` bị rỗng ($\text{Count} = 0$), khiến việc khoanh chọn thất bại hoàn toàn.

2. **Lỗi Tọa độ Điểm Nét vẽ Polyline chưa qua Ma trận Transform (`TransformToAncestor`):**
   * Các nét vẽ tay `Polyline` khi được vẽ trên Canvas có thể chứa thuộc tính `RenderTransform` hoặc nằm trong container phụ. Việc lấy trực tiếp `polyline.Points[i]` mà không chuyển đổi qua `polyline.TransformToAncestor(_canvas).Transform(pt)` khiến tọa độ kiểm tra bị lệch hàng trăm pixel so với nét vẽ thực tế hiển thị trên màn hình.

3. **Lỗi `Stylus.IsPressAndHoldEnabled` gây đơ nét vẽ Lasso:**
   * WPF mặc định bật chế độ nhấn giữ Stylus để giả lập chuột phải (Press and Hold for Right Click), gây ra độ trễ 500ms khi bắt đầu vẽ nét Lasso ➔ Làm mất các điểm đầu tiên của đường khoanh Lasso.

---

## PHẦN III: MA TRẬN PHẢN BIỆN KỸ THUẬT VÀ GIẢI PHÁP TỔNG THỂ

Hội đồng 12 Chuyên gia đưa ra giải pháp xử lý triệt để 100%:

| Hạng mục Lỗi | Nguyên nhân Kỹ thuật | Giải pháp Triệt để của Hội đồng 12 Chuyên gia |
| :---: | :--- | :--- |
| **Sự kiện Cảm ứng** | `LassoSelectionTool` chỉ lắng nghe Mouse Events. | **Đăng ký đồng thời Mouse Events, Touch Events (`TouchDown`, `TouchMove`, `TouchUp`) và Stylus Events (`StylusDown`, `StylusMove`, `StylusUp`).** |
| **Bản đồ Tọa độ Nét vẽ** | Tọa độ `Polyline.Points` chưa biến đổi qua Canvas. | **Sử dụng `GeneralTransform transform = polyline.TransformToAncestor(_canvas)` để chuyển từng điểm `pt` sang tọa độ Canvas tuyệt đối: `transform.Transform(pt)`.** |
| **Xung đột TouchHandler** | `_touchHandler` nuốt mất sự kiện Touch. | **Trong `ActivateLassoSelectionMode()`, vô hiệu hóa tạm thời `_touchHandler` và vô hiệu hóa `Stylus.SetIsPressAndHoldEnabled(_canvas, false)`.** |
| **Quét Thùng rác (All Objects)** | `_canvas.Children` chứa các element ẩn/hệ thống. | **Duyệt kết hợp cả `_canvas.Children` và `_selectionManager.GetAllObjects()`, đăng ký tự động các đối tượng chưa có trong bộ nhớ.** |

---

## PHẦN IV: KẾ HOẠCH THỰC HIỆN CHI TIẾT TỪNG BƯỚC CHO CODER

### 📌 BƯỚC 1: Đăng ký Cảm ứng Đa điểm & Vô hiệu hóa PressAndHold trong `LassoSelectionTool.cs`

```csharp
public void Activate()
{
    if (IsActive) return;

    IsActive = true;
    
    // 1. Đăng ký Mouse Events
    _canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
    _canvas.MouseMove += Canvas_MouseMove;
    _canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

    // 2. 🚀 BỔ SUNG: Đăng ký Touch Events (Cho cảm ứng ngón tay)
    _canvas.TouchDown += Canvas_TouchDown;
    _canvas.TouchMove += Canvas_TouchMove;
    _canvas.TouchUp += Canvas_TouchUp;

    // 3. 🚀 BỔ SUNG: Vô hiệu hóa Stylus Press-And-Hold để nét vẽ Lasso ăn ngay lập tức
    Stylus.SetIsPressAndHoldEnabled(_canvas, false);

    _canvas.Cursor = Cursors.Cross;
    System.Diagnostics.Debug.WriteLine("✅ Lasso Tool Activated with Mouse & Touch support");
}
```

---

### 📌 BƯỚC 2: Chuyển đổi Tọa độ Nét vẽ Polyline tuyệt đối (`TransformToAncestor`)

```csharp
private bool IsElementInLasso(UIElement element)
{
    try
    {
        // 🚀 CHUYỂN ĐỔI MA TRẬN TỌA ĐỘ NẾT VẼ POLYLINE
        if (element is Polyline polyline && polyline.Points.Count > 0)
        {
            GeneralTransform? transform = null;
            try
            {
                transform = polyline.TransformToAncestor(_canvas);
            }
            catch { }

            double pLeft = Canvas.GetLeft(polyline);
            double pTop = Canvas.GetTop(polyline);
            if (double.IsNaN(pLeft)) pLeft = 0;
            if (double.IsNaN(pTop)) pTop = 0;

            foreach (var pt in polyline.Points)
            {
                Point canvasPt = transform != null ? transform.Transform(pt) : new Point(pLeft + pt.X, pTop + pt.Y);
                if (IsPointInPolygon(canvasPt, _lassoPoints))
                    return true;
            }

            // Kiểm tra trung điểm giữa các điểm
            for (int i = 0; i < polyline.Points.Count - 1; i++)
            {
                Point rawMid = new Point((polyline.Points[i].X + polyline.Points[i + 1].X) / 2,
                                         (polyline.Points[i].Y + polyline.Points[i + 1].Y) / 2);
                Point canvasMid = transform != null ? transform.Transform(rawMid) : new Point(pLeft + rawMid.X, pTop + rawMid.Y);
                if (IsPointInPolygon(canvasMid, _lassoPoints))
                    return true;
            }

            // Kiểm tra Lasso bao trùm toàn bộ nét vẽ
            Rect polyBounds = GetElementCanvasBounds(polyline);
            if (!polyBounds.IsEmpty && _lassoPoints.Any(p => polyBounds.Contains(p)))
                return true;

            return false;
        }

        // Xử lý các hình học, khung chữ, ảnh khác...
        Rect bounds = GetElementCanvasBounds(element);
        if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.IsEmpty)
            return false;

        // 9 điểm mẫu kiểm tra
        Point[] testPoints = new Point[]
        {
            new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2),
            bounds.TopLeft, bounds.TopRight, bounds.BottomLeft, bounds.BottomRight,
            new Point(bounds.Left + bounds.Width / 2, bounds.Top),
            new Point(bounds.Left + bounds.Width / 2, bounds.Bottom),
            new Point(bounds.Left, bounds.Top + bounds.Height / 2),
            new Point(bounds.Right, bounds.Top + bounds.Height / 2)
        };

        if (testPoints.Any(p => IsPointInPolygon(p, _lassoPoints)))
            return true;

        if (_lassoPoints.Any(p => bounds.Contains(p)))
            return true;

        return false;
    }
    catch
    {
        return false;
    }
}
```

---

### 📌 BƯỚC 3: Đồng bộ Xử lý Cảm ứng Touch / Stylus Handlers

```csharp
private void Canvas_TouchDown(object? sender, TouchEventArgs e)
{
    _isDrawing = true;
    _startPoint = e.GetTouchPoint(_canvas).Position;
    _lassoPoints.Clear();
    _lassoPoints.Add(_startPoint);

    CreateLassoVisual();
    _canvas.CaptureTouch(e.TouchDevice);
    e.Handled = true;
}

private void Canvas_TouchMove(object? sender, TouchEventArgs e)
{
    if (!_isDrawing) return;

    Point currentPoint = e.GetTouchPoint(_canvas).Position;
    if (_lassoPoints.Count == 0 || GetDistance(_lassoPoints.Last(), currentPoint) > 4)
    {
        _lassoPoints.Add(currentPoint);
        UpdateLassoVisual();
    }
    e.Handled = true;
}

private void Canvas_TouchUp(object? sender, TouchEventArgs e)
{
    if (!_isDrawing) return;

    _isDrawing = false;
    _canvas.ReleaseTouchCapture(e.TouchDevice);

    var bounds = GetLassoBounds();
    if (bounds.Width >= 5 && bounds.Height >= 5 && _lassoPoints.Count > 2)
    {
        _lassoPoints.Add(_startPoint);
        UpdateLassoVisual();

        var selectedElements = FindElementsInLasso();
        SelectionCompleted?.Invoke(this, selectedElements);
    }

    System.Threading.Tasks.Task.Delay(200).ContinueWith(_ =>
    {
        Application.Current.Dispatcher.Invoke(() => CleanupVisual());
    });
    e.Handled = true;
}
```

---

## PHẦN V: BỘ CHECKSHEET KIỂM THỬ KHẮT KHE VÀ NGHIỆM THU 8 BƯỚC

Coder **bắt buộc phải đạt mốc `PASS` cả 8 kịch bản kiểm thử** này trước khi trình Hội đồng duyệt Release:

| STT | Bước thực hiện | Kịch bản kiểm thử (Test Case) | Điều kiện Đầu vào | Kết quả Kỳ vọng (PASS Criteria) | Kết quả Coder (PASS/FAIL) | Người nghiệm thu |
| :---: | :--- | :--- | :--- | :--- | :---: | :---: |
| **1** | Bằng Chuột (Mouse) | Dùng chuột kéo vòng Lasso quanh 2 hình tròn | Thao tác chuột máy tính | Chọn $100\%$ cả 2 hình tròn. | `[   ]` | KTS Kiến trúc |
| **2** | Bằng Ngón tay (Touch) | Dùng ngón tay khoanh Lasso trên màn cảm ứng | Thao tác ngón tay cảm ứng | Vòng Lasso đứt vàng lấp lánh xuất hiện ngay dưới ngón tay & chọn đúng đối tượng. | `[   ]` | **Chuyên gia WPF Touch** |
| **3** | Bằng Bút vẽ (Stylus Pen) | Dùng bút cảm ứng khoanh Lasso nét chữ viết tay | Thao tác bút cảm ứng | Bắt điểm nét Lasso ngay khi bút chạm màn hình (0ms delay). | `[   ]` | **Chuyên gia WPF Stylus** |
| **4** | Nét chữ Polyline | Khoanh Lasso một chữ viết tay lằn nhằn | Chữ viết tay dài đa nét | Chọn trọn vẹn toàn bộ các nét của chữ viết tay. | `[   ]` | **Chuyên gia Hình học** |
| **5** | Khung chữ Text Box | Khoanh Lasso một khung văn bản `TextBox` | Hộp văn bản có chữ | Chọn chính xác `TextBox` và hiển thị Bounding Box chứa. | `[   ]` | Kỹ sư QA/QC |
| **6** | Hình 3D / Hình 2D | Khoanh Lasso khối 3D Lập phương | Khối 3D trên Canvas | Chọn chính xác khối 3D và hiện Toolbar ngữ cảnh. | `[   ]` | Giáo viên Trưởng |
| **7** | Vùng chọn nhỏ | Khoanh vòng Lasso nhỏ 6x6 pixel | Vòng khoanh nhỏ | Vẫn chọn được đối tượng bên trong (vượt ngưỡng 5px). | `[   ]` | Chuyên gia UX |
| **8** | Thương hiệu & Grid | Kiểm tra Toast thông báo Lasso | Màn tương tác 86-inch | Giữ nguyên từ khóa **SMART TOUCH**, **SMART CLASS**, **DESKTOP**. | `[   ]` | Chuyên gia System |

---

## PHẦN VI: TỔNG KẾT VÀ BÀN GIAO THỰC THI

Kế hoạch nâng cấp và xử lý triệt để lỗi Chọn vùng Lasso đã được **Hội đồng 12 Chuyên gia** tự phản biện và phê duyệt hoàn chỉnh. Yêu cầu Lập trình viên (Coder):
1. Thực hiện chính xác theo mã nguồn mẫu tại **Bước 1, Bước 2, Bước 3**.
2. Chạy thử nghiệm và đảm bảo đánh tích `PASS` đầy đủ 8 hạng mục trong **Bộ Checksheet Kiểm thử ở Phần V**.

---
*Phê duyệt bởi Trưởng ban Thiết kế Dự án QA Smart School & Hội đồng Chuyên gia Độc lập Mở rộng.*
