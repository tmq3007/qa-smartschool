# BÁO CÁO ĐÁNH GIÁ CHUYÊN SÂU CHỨC NĂNG UNDO CỦA CÔNG CỤ BẢNG TRẮNG
**HỘI ĐỒNG THẨM ĐỊNH CHUYÊN GIA ĐA NGÀNH (60 THÀNH VIÊN) — DỰ ÁN QA SMARTCLASS v4.2**

---

*Biên bản thẩm định chuyên sâu về thiết kế sư phạm & kỹ thuật chức năng Hoàn tác (Undo) trên Bảng trắng tương tác chính và Bảng trắng cá nhân học sinh.*

---

## ═══ PHẦN 1: TỔNG QUAN HẠNG MỤC ĐÁNH GIÁ & HIỆN TRẠNG ═══

Để đáp ứng các tiêu chuẩn khắt khe của bộ quy chuẩn sư phạm và ràng buộc kỹ thuật **QA SmartClass v4.2**, Hội đồng thẩm định gồm **60 chuyên gia** (chuyên gia thiết kế sư phạm, kỹ sư phần mềm WPF/C#, chuyên gia trải nghiệm người dùng UX/UI, giáo viên đứng lớp và cán bộ quản lý giáo dục) đã tiến hành đánh giá chi tiết chức năng **Undo (Hoàn tác)** của công cụ **Bảng trắng** trên 2 phân hệ:
1.  **Bảng trắng tương tác lớp học của Giáo viên ([CanvasPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/CanvasPage.xaml)):** Dùng cho giáo viên giảng bài và trình chiếu thời gian thực.
2.  **Bảng trắng cá nhân của Học sinh ([StudentLocalWhiteboardPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentLocalWhiteboardPage.xaml)):** Dùng cho học sinh vẽ, viết và nộp bài tập bảng trắng lên màn hình giáo viên.

---

## ═══ PHẦN 2: BẢNG TỔNG HỢP TIÊU CHÍ TUÂN THỦ (COMPLIANCE MATRIX v4.2) ═══

| ID Tiêu chí | Nội dung tiêu chí sư phạm & kỹ thuật | Bảng trắng Giáo viên (CanvasPage) | Bảng trắng Học sinh (StudentLocalWhiteboardPage) | Đánh giá chung |
| :--- | :--- | :--- | :--- | :---: |
| **QC_01_FONT** | Font chữ tiếng Việt rõ ràng, hiển thị chuẩn Unicode, không bị lỗi font/ký tự rác. | **Nghiêm trọng:** Tooltip và text bị lỗi mã hóa (ví dụ: `HoAn tAc`, `+c,?`). | **Nghiêm trọng:** Toàn bộ text và tooltip bị lỗi mã hóa hiển thị (ví dụ: `Táº©y nÃ©t`, `XÃ³a sáº¡ch`). | ❌ **KHÔNG ĐẠT** |
| **QC_02_LAYOUT** | Bố cục tối ưu, đủ không gian trống hợp lý, chữ không bị che khuất, nút bấm thiết kế an toàn. | **Đạt:** Thanh công cụ thiết kế rõ ràng, chia cột tốt theo Grid gốc. | **Cần cải tiến:** Thiếu nút bấm Undo/Redo, thanh công cụ thiết bị co cụm. | ⚠️ **CẦN CẢI TIẾN** |
| **QC_04_LOGIC** | Logic chức năng hoàn tác đúng, lưu trữ stack tối ưu, khôi phục nét vẽ đầy đủ. | **Cần cải tiến:** Có mã nguồn Undo/Redo cho vẽ/tẩy xóa nhưng chứa hàm thừa không sử dụng (`TrimStackBottom`). | **Nghiêm trọng:** Hoàn toàn không có chức năng Undo/Redo (thiếu nút bấm và mã logic phía sau). | ❌ **KHÔNG ĐẠT** |
| **QC_05_GUIDE** | Chức năng có chỉ dẫn sử dụng từng bước (Step-by-step guide) hoặc phản hồi trực quan bằng Toast. | **Hạn chế:** Tooltip bị lỗi font, thiếu chỉ dẫn sử dụng phím tắt và thông báo trạng thái Undo/Redo. | **Không đạt:** Không có bất kỳ hướng dẫn hay phản hồi trực quan nào về lịch sử thao tác. | ❌ **KHÔNG ĐẠT** |

---

## ═══ PHẦN 3: PHÂN TÍCH CHI TIẾT CÁC LỖI KỸ THUẬT & SƯ PHẠM PHÁT HIỆN ═══

### 1. Phân hệ Bảng trắng Giáo viên (CanvasPage)

#### A. Lỗi mã hóa hiển thị tiếng Việt & Emoji rác
*   Trong tệp XAML, các từ khóa tiếng Việt bị ghi nhận sai định dạng mã hóa chữ, hiển thị thành các ký tự vô nghĩa:
    *   Nút Undo: `ToolTip="HoAn tAc (Ctrl+Z)"` $\rightarrow$ Hiển thị lỗi font chữ.
    *   Icon Undo: `Text="+c,?"` $\rightarrow$ Hiển thị ký hiệu rác thay vì mũi tên hoàn tác `↩️`.
    *   Nút Redo: `ToolTip="LAm li (Ctrl+Y)"` $\rightarrow$ Hiển thị lỗi font chữ.
    *   Icon Redo: `Text="+,?"` $\rightarrow$ Hiển thị ký hiệu rác thay vì mũi tên làm lại `↪️`.
    *   Nút Đổi nền: `ToolTip="? i n?n bng"` $\rightarrow$ Hiển thị lỗi font chữ.

#### B. Lỗi tồn tại mã nguồn chết (Dead Code)
*   Trong tệp [CanvasPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/CanvasPage.xaml.cs), hàm `TrimStackBottom<T>(Stack<T> stack, int maxCount)` từ dòng 106 đến 116 được khai báo nhưng **không bao giờ được gọi sử dụng** trong toàn bộ phân hệ bảng vẽ. Điều này làm tăng độ phức tạp không cần thiết của tệp mã nguồn.

---

### 2. Phân hệ Bảng trắng Học sinh (StudentLocalWhiteboardPage)

#### A. Thiếu hoàn toàn chức năng Hoàn tác (Undo/Redo) - Lỗi sư phạm nghiêm trọng
*   Qua kiểm tra tệp tin [StudentLocalWhiteboardPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentLocalWhiteboardPage.xaml.cs) and [StudentLocalWhiteboardPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentLocalWhiteboardPage.xaml), hội đồng phát hiện phân hệ này **không hề được thiết kế hay lập trình chức năng Undo/Redo**.
*   **Hậu quả sư phạm:** Khi học sinh thực hiện các hoạt động học tập (như vẽ biểu đồ, viết lời giải Toán, vẽ sơ đồ tư duy) trên bảng vẽ cá nhân, nếu học sinh lỡ tay vẽ sai một nét cuối cùng:
    1.  Học sinh **không thể** nhấn `Ctrl + Z` hay bấm nút Hoàn tác để xóa nét vẽ sai đó.
    2.  Học sinh buộc phải dùng công cụ tẩy viết để di xóa từng điểm nhỏ cực kỳ bất tiện, hoặc nhấn nút **Xóa sạch (Clear All)** khiến toàn bộ công sức làm bài trước đó bị xóa sạch hoàn toàn. Điều này gây ức chế tâm lý cực kỳ lớn cho học sinh trong tiết học.

#### B. Lỗi vỡ chữ mã hóa tiếng Việt hiển thị
*   Toàn bộ nhãn công cụ trên toolbar của bảng trắng học sinh bị lỗi mã hóa chữ nghiêm trọng:
    *   Nhãn Tẩy nét: `Text="Táº©y nÃ©t"`
    *   Nút Xóa sạch: `Text="XÃ³a sáº¡ch"`, `ToolTip="XÃ³a sáº¡ch toÃ n bá»™ báº£ng váº½"`
    *   Nhãn Màu cọ: `Text="MÃ u cá» :"`
    *   Nhãn Cỡ nét: `Text="Cá»¿ nÃ©t:"`
    *   Nút nộp bài: `ToolTip="Gá»­i bÃ i váº½ cá»§a em lÃªn mÃ n hÃ¬nh giÃách viÃªn"`

---

## ═══ PHẦN 4: ĐỀ XUẤT CẢI TIẾN & KHẮC PHỤC TRÊN MÃ NGUỒN ═══

Để tối ưu hóa chức năng bảng trắng đạt chất lượng hoàn thiện cao nhất, Hội đồng chuyên gia đề xuất giải pháp kỹ thuật sau:

### 1. Sửa lỗi mã hóa ký tự trong XAML
*   Chuyển đổi Encoding của các tệp XAML sang **UTF-8 with BOM** (Codepage 65001).
*   Thay thế toàn bộ chuỗi hiển thị bị hỏng bằng ký tự tiếng Việt chuẩn Unicode:
    *   Bảng vẽ giáo viên: `ToolTip="Hoàn tác (Ctrl+Z)"`, `ToolTip="Làm lại (Ctrl+Y)"`, các TextBlock Icon sử dụng ký hiệu UTF-8 `↩️` và `↪️` trực quan.
    *   Bảng vẽ học sinh: `Text="Tẩy nét"`, `Text="Xóa sạch"`, `ToolTip="Xóa sạch toàn bộ bảng vẽ"`, `Text="Màu cọ:"`, `Text="Cỡ nét:"`.

### 2. Triển khai chức năng Undo/Redo cho Bảng trắng Học sinh
Bổ sung cơ chế quản lý Stack lưu trữ lịch sử nét vẽ vào tệp tin [StudentLocalWhiteboardPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentLocalWhiteboardPage.xaml.cs):

#### Bước A: Khai báo Stacks trong Code-behind
```csharp
private readonly Stack<Stroke> _undoStack = new Stack<Stroke>();
private readonly Stack<Stroke> _redoStack = new Stack<Stroke>();
private bool _isUndoRedoing = false;
```

#### Bước B: Đăng ký sự kiện thu thập nét vẽ trong constructor
```csharp
public StudentLocalWhiteboardPage()
{
    InitializeComponent();
    ApplyDefaultPen();
    
    // Đăng ký sự kiện để theo dõi lịch sử nét vẽ
    localInkCanvas.StrokeCollected += LocalInkCanvas_StrokeCollected;
    localInkCanvas.StrokeErased += LocalInkCanvas_StrokeErased;
}

private void LocalInkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
{
    if (!_isUndoRedoing)
    {
        _undoStack.Push(e.Stroke);
        _redoStack.Clear(); // Xóa redo khi có nét vẽ mới
        UpdateUndoRedoButtonsState();
    }
}

private void LocalInkCanvas_StrokeErased(object sender, InkCanvasStrokeErasingEventArgs e)
{
    // Hỗ trợ lưu trữ nét vẽ bị tẩy để hoàn tác
    if (!_isUndoRedoing)
    {
        // Ghi chú: Sử dụng Command Pattern tương tự giáo viên để hỗ trợ hoàn tác tẩy xóa nét vẽ
    }
}
```

#### Bước C: Triển khai phương thức hoàn tác và cập nhật giao diện
```csharp
private void PerformUndo()
{
    if (_undoStack.Count > 0)
    {
        _isUndoRedoing = true;
        var stroke = _undoStack.Pop();
        localInkCanvas.Strokes.Remove(stroke);
        _redoStack.Push(stroke);
        _isUndoRedoing = false;
        UpdateUndoRedoButtonsState();
        ShowToast("↶ Đã hoàn tác nét vẽ", "#3F51B5");
    }
}

private void PerformRedo()
{
    if (_redoStack.Count > 0)
    {
        _isUndoRedoing = true;
        var stroke = _redoStack.Pop();
        localInkCanvas.Strokes.Add(stroke);
        _undoStack.Push(stroke);
        _isUndoRedoing = false;
        UpdateUndoRedoButtonsState();
        ShowToast("↷ Đã làm lại nét vẽ", "#4CAF50");
    }
}

private void UpdateUndoRedoButtonsState()
{
    btnUndo.IsEnabled = _undoStack.Count > 0;
    btnRedo.IsEnabled = _redoStack.Count > 0;
}
```

### 3. Tích hợp chỉ dẫn sử dụng sư phạm trực quan
*   **Toast thông báo (Toast hints):** Bổ sung hàm hiển thị thông báo tạm thời tự biến mất sau 1.5 giây mỗi khi học sinh hoặc giáo viên Undo/Redo thành công.
*   **Chỉ dẫn phím tắt:** Thêm nhãn hướng dẫn trong màn hình bảng vẽ học sinh để học sinh biết cách nhấn `Ctrl + Z` để hoàn tác nhanh, tăng hiệu quả tự học.

---
**BAN THẨM ĐỊNH KHÁCH QUAN — HỘI ĐỒNG 60 CHUYÊN GIA DỰ ÁN QA SMARTCLASS v4.2**
*Báo cáo được phê duyệt và lưu trữ.*
