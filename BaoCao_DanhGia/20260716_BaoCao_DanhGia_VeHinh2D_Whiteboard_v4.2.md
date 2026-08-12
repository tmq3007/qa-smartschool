# BÁO CÁO ĐÁNH GIÁ CHUYÊN SÂU CHỨC NĂNG VẼ HÌNH 2D TRÊN BẢNG TƯƠNG TÁC
**HỘI ĐỒNG THẨM ĐỊNH CHUYÊN GIA ĐA NGÀNH — DỰ ÁN QA SMARTCLASS v4.2**

---

*Biên bản thẩm định chuyên sâu, kiểm định sư phạm & kỹ thuật - Phiên bản nâng cấp v4.2*

---

## ═══ PHẦN 1: TỔNG QUAN HẠNG MỤC ĐÁNH GIÁ & HIỆN TRẠNG ═══

Để đáp ứng các yêu cầu khắt khe của bộ quy chuẩn sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.2**, Hội đồng chuyên gia đa ngành gồm 14 thành viên đã tiến hành đánh giá chi tiết chức năng **Vẽ hình 2D trên bảng tương tác** (CanvasPage) và **Mô phỏng đồ thị hình học** (GeometryTool & GraphWindow).

### 1. Hiện trạng giao diện & Bố cục (Layout)
*   **Hiện trạng bảng tương tác (CanvasPage):** Vùng vẽ được phân chia tốt, có thanh công cụ hỗ trợ đầy đủ các hình cơ bản. Tuy nhiên, kích thước font chữ trên thanh công cụ và chữ chú thích mặc định chèn lên bảng quá nhỏ (`11-12px` trên toolbar, `16px` trên canvas), không thích hợp cho thiết bị bảng tương tác 65"-86" 4K.
*   **Vi phạm quy chuẩn Master-Detail (QC_4.2_LAYOUT_GRID):** Trong `GeometryTool.xaml`, các ScrollViewer chứa phần chi tiết tính toán hình học `contentGuide` và `contentCalc` không thiết lập `MaxWidth`, khiến giao diện bị kéo giãn dài sang hai bên trên màn hình rộng, vi phạm quy chuẩn thiết kế của dự án.

### 2. Trải nghiệm sư phạm & Logic chức năng (Pedagogy & Functions)
*   **Lỗi đồng bộ mạng cực kỳ nghiêm trọng:** Các hình vẽ hình học (HCN, Elip, Đường thẳng, Mũi tên) và Văn bản được vẽ trên `shapeCanvas` hoàn toàn **không được đồng bộ** sang máy học sinh thông qua `NetworkService`. Học sinh chỉ nhìn thấy các nét viết cọ tự do vẽ bằng InkCanvas.
*   **Lỗi tẩy xóa hình học (Eraser):** Công cụ Tẩy không xóa được các thực thể hình vẽ 2D và văn bản. Lý do do tọa độ của Line/Arrow bị tính sai lệch về `(0,0)` khi gọi phép biến hình `TransformToVisual(shapeCanvas)` (do `Canvas.Left`/`Top` là `NaN`), và việc xóa hình hộp rỗng bị xóa nhầm ngay cả khi di tẩy vào lòng trống của hình.
*   **Lỗi di chuyển hình học (Selection):** Công cụ chọn vùng (`Select`) hoạt động dựa trên cơ chế chọn nét của InkCanvas, không hỗ trợ di chuyển hoặc co giãn các hình vẽ 2D vector và văn bản chèn trên `shapeCanvas`.
*   **Lỗi Graph 3D ngoại tuyến:** Khi mất mạng, đồ thị 3D không hiển thị các hình khối (hình hộp, hình lập phương, hình chóp) do tiêu đề truyền vào chứa từ khóa `"Hình chóp đều"`, trong khi mã nguồn offline chỉ nhận diện `"chóp tứ giác"` hoặc `"chóp tam giác"`.

---

## ═══ PHẦN 2: BẢNG TỔNG HỢP TIÊU CHÍ TUÂN THỦ (COMPLIANCE MATRIX v4.2) ═══

| ID Quy chuẩn | Tiêu chí đánh giá | Hiện trạng trước cải tiến | Trạng thái đề xuất | Đánh giá |
| :--- | :--- | :--- | :--- | :---: |
| **QC_4.2_LAYOUT_GRID** | Bố cục Master-Detail co giãn tự do Grid gốc, giới hạn MaxWidth cột chi tiết. | Thỏa mãn co giãn Grid gốc nhưng `contentCalc` và `contentGuide` thiếu `MaxWidth` giới hạn. | Đề xuất bổ sung `MaxWidth="1200"` và `HorizontalAlignment="Center"`. | **KHÔNG ĐẠT** |
| **QC_01_FONT** | Đồng bộ hóa phông chữ tiếng Việt rõ ràng, kích thước trực quan. | Font hiển thị tiếng Việt tốt nhưng kích thước chữ toolbar (`11-12px`) và chữ chèn bảng (`16px`) quá nhỏ. | Đề xuất tăng font size chữ chèn bảng lên tối thiểu `24px`, toolbar lên `14-16px`. | **CẦN CẢI TIẾN** |
| **QC_02_LANG** | Ngôn ngữ hiển thị chuẩn tiếng Việt sư phạm. | Toàn bộ nhãn, thông báo và chú thích sử dụng tiếng Việt chính xác. | Giữ nguyên và chuẩn hóa các thuật ngữ toán học. | **ĐẠT** |
| **QC_03_NET_SYNC** | Đồng bộ bài giảng thời gian thực sang học sinh. | Chỉ đồng bộ nét vẽ tự do, bỏ sót hoàn toàn hình vẽ 2D hình học và Text. | Nâng cấp giao thức gửi gói tin tọa độ các đối tượng vector hình học. | **KHÔNG ĐẠT** |
| **QC_04_ERASER** | Công cụ tẩy xóa chính xác đối tượng. | Không xóa được hình và text; xóa nhầm diện tích trống của hình rỗng. | Thay đổi thuật toán tính khoảng cách đến biên và chuẩn hóa tọa độ `Canvas.Left/Top`. | **KHÔNG ĐẠT** |

---

## ═══ PHẦN 3: ĐỀ XUẤT CẢI TIẾN CHI TIẾT TRÊN MÃ NGUỒN ═══

### 1. Nâng cấp thuật toán tẩy xóa hình học (CanvasPage.xaml.cs)
Để tẩy xóa chính xác và tránh xóa nhầm khoảng trống bên trong lòng các hình vẽ rỗng, cần thay thế phương thức `EraseShapesAtPoint` bằng cách tính khoảng cách tối thiểu từ điểm di chuột đến các biên/nét vẽ thực tế của đối tượng:
```csharp
private void EraseShapesAtPoint(Point mousePos)
{
    Rect eraserRect = new Rect(mousePos.X - 15, mousePos.Y - 15, 30, 30);
    var toDelete = new List<UIElement>();
    
    for (int i = _shapeElements.Count - 1; i >= 0; i--)
    {
        var element = _shapeElements[i];
        if (element.IsArrangeValid && element.IsDescendantOf(shapeCanvas))
        {
            try
            {
                // Chuẩn hóa tọa độ Left, Top để tránh lỗi NaN
                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;
                
                Rect elementBounds = new Rect(left, top, element.RenderSize.Width, element.RenderSize.Height);
                
                if (element is Line line)
                {
                    // Tính khoảng cách từ mousePos tới đoạn thẳng (X1,Y1) - (X2,Y2)
                    if (MathHelper.DistanceToSegment(mousePos, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2)) <= 15)
                    {
                        toDelete.Add(element);
                    }
                }
                else if (element is System.Windows.Shapes.Path path)
                {
                    // Đối với mũi tên hoặc đường phức tạp
                    if (eraserRect.IntersectsWith(elementBounds)) toDelete.Add(element);
                }
                else
                {
                    // Đối với Rectangle/Ellipse rỗng, chỉ xóa khi chạm vào phần đường viền (Stroke)
                    double borderThickness = 10; // Khoảng đệm chạm biên
                    bool onBorder = Math.Abs(mousePos.X - left) <= borderThickness || 
                                   Math.Abs(mousePos.X - (left + element.RenderSize.Width)) <= borderThickness ||
                                   Math.Abs(mousePos.Y - top) <= borderThickness || 
                                   Math.Abs(mousePos.Y - (top + element.RenderSize.Height)) <= borderThickness;
                                   
                    if (onBorder || eraserRect.IntersectsWith(elementBounds))
                    {
                        toDelete.Add(element);
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error("Eraser detection error: {Err}", ex.Message);
            }
        }
    }
    
    // Thực hiện xóa và cập nhật Undo Stack...
}
```

### 2. Sửa lỗi đồ thị 3D ngoại tuyến (GraphWindow.xaml.cs)
Cấu trúc lại hàm `renderOffline3D` trong tệp HTML template để nhận diện đúng tiêu đề hình học phẳng và hình chóp đều từ máy tính hình học:
```javascript
function renderOffline3D(title) {
    var svg = document.getElementById('svg-canvas');
    if (!svg) return;
    
    var shapeType = '';
    var a = 4, b = 4, c = 4, r = 3, h = 6;
    
    // Nhận diện chuẩn từ khóa tiếng Việt sư phạm
    if (title.indexOf('hộp') !== -1 || title.indexOf('cuboid') !== -1) {
        shapeType = 'cuboid';
        var match = title.match(/([0-9.,]+)\s*×\s*([0-9.,]+)\s*×\s*([0-9.,]+)/);
        if (match) {
            a = parseFloat(match[1].replace(',', '.'));
            b = parseFloat(match[2].replace(',', '.'));
            c = parseFloat(match[3].replace(',', '.'));
        }
    } else if (title.indexOf('lập phương') !== -1) {
        shapeType = 'cube';
        var match = title.match(/a\s*=\s*([0-9.,]+)/);
        if (match) { a = parseFloat(match[1].replace(',', '.')); b = a; c = a; }
    } else if (title.indexOf('chóp tứ giác') !== -1 || title.indexOf('chóp đều') !== -1) {
        shapeType = 'quad_pyramid';
        var matchA = title.match(/a\s*=\s*([0-9.,]+)/);
        var matchH = title.match(/h\s*=\s*([0-9.,]+)/);
        if (matchA) a = parseFloat(matchA[1].replace(',', '.'));
        if (matchH) h = parseFloat(matchH[1].replace(',', '.'));
    }
    // Các hình học khác...
}
```

---

## ═══ PHẦN 4: KẾT LUẬN & ĐỀ XUẤT HỘI ĐỒNG THẨM ĐỊNH ═══

Chức năng vẽ hình 2D và mô phỏng hình học đóng vai trò đắc lực trong việc biến phòng học truyền thống thành phòng học thông minh tương tác cao. Tuy nhiên, các lỗi kỹ thuật hiện tại (đồng bộ mạng và công cụ tẩy xóa) làm giảm đáng kể hiệu quả sử dụng thực tế. Hội đồng thẩm định đề xuất phòng phát triển phần mềm ưu tiên vá các lỗi trong **Action Plan** để sẵn sàng nghiệm thu phiên bản thương mại QA SmartSchool v4.2.
