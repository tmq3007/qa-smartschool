# KẾ HOẠCH NÂNG CẤP & CẢI TIẾN HỆ THỐNG HOÀN TÁC (UNDO/REDO) — DỰ ÁN QA SMARTCLASS v4.2
**Tác giả: Trưởng ban Thiết kế dự án QA Smart School**

---

> [!NOTE]
> Tài liệu này đóng vai trò là kiến trúc thiết kế, quy trình kiểm thử và chỉ dẫn mã nguồn chi tiết nhằm cải tiến toàn diện chức năng **Hoàn tác (Undo/Redo)** của công cụ Bảng trắng tương tác Giáo viên và Bảng trắng Học sinh theo đúng bộ quy chuẩn sư phạm và kỹ thuật **QA SmartClass v4.2**. 

---

## ═══ PHẦN 1: PHƯƠNG PHÁP KHẮC PHỤC & PHẢN BIỆN THIẾT KẾ ═══

### 1. Giải quyết lỗi font chữ tiếng Việt và mã hóa XAML
*   **Vấn đề:** Các chuỗi ký tự Unicode tiếng Việt trong XAML bị hiển thị thành ký tự rác (Mojibake) do tệp tin được lưu ở bảng mã ANSI/Windows-1252 khi lập trình viên sử dụng một số trình soạn thảo trên Windows.
*   **Phương pháp thực hiện & Phản biện:**
    *   *Sai lầm phổ biến:* Chỉ sửa chữ rác trực tiếp trong code-behind hoặc XAML mà không đổi mã hóa tệp tin $\rightarrow$ khi biên dịch (MSBuild) trên máy chủ CI/CD hoặc máy tính khác sẽ tiếp tục bị vỡ chữ.
    *   *Giải pháp tối ưu:* Quy chuẩn hóa mã hóa file bắt buộc: Đầu tiên phải lưu lại (Save As) tệp tin dưới định dạng mã hóa **UTF-8 with BOM (Codepage 65001)** thông qua menu `File -> Advanced Save Options` của Visual Studio. Sau đó mới tiến hành viết lại các chuỗi Unicode tiếng Việt chính xác. Điều này đảm bảo tính nhất quán đa nền tảng cho MSBuild.

### 2. Triển khai hoàn tác cho Modify (Di chuyển/Xoay/Co giãn hình vẽ)
*   **Vấn đề:** Giáo viên không thể Undo các hành động kéo thả, thay đổi kích thước hoặc xoay hình vẽ do nhánh `ActionType.Modify` đang bỏ trống.
*   **Phương pháp thực hiện & Phản biện:**
    *   *Sai lầm phổ biến:* Chỉ lưu tọa độ mới của đối tượng $\rightarrow$ khi gọi Undo sẽ không biết tọa độ cũ của đối tượng để phục hồi.
    *   *Giải pháp tối ưu:* Thiết kế cấu trúc lưu trữ trạng thái biến đổi đối tượng `TransformationState` chứa:
        *   `Point Position` (vị trí Canvas.Left, Canvas.Top)
        *   `Size Size` (chiều rộng Width, chiều cao Height)
        *   `double RotationAngle` (góc xoay hiện tại)
    *   *Quy trình hoạt động:*
        1.  Khi người dùng bắt đầu thao tác kéo thả hoặc xoay đối tượng (bắt đầu nhấn chuột `MouseDown`), chụp lại trạng thái hiện tại và lưu vào biến tạm `_activeOldState`.
        2.  Khi hoàn thành thao tác (thả chuột `MouseUp`), chụp lại trạng thái mới `newState`.
        3.  Đóng gói cả 2 trạng thái vào một hành động `UndoRedoAction` có `OldValue = _activeOldState` và `NewValue = newState`. Đẩy hành động này vào stack.
        4.  Khi Undo: Đọc dữ liệu từ `OldValue` và gán ngược lại thuộc tính giao diện của đối tượng. Khi Redo: Đọc từ `NewValue` và gán lại.

### 3. Triển khai hoàn tác cho thao tác Clear All trên Desktop Overlay
*   **Vấn đề:** Giáo viên bị mất sạch nét vẽ trên màn hình nền desktop khi lỡ tay bấm nhầm nút Clear All mà không thể khôi phục.
*   **Phương pháp thực hiện & Phản biện:**
    *   *Sai lầm phổ biến:* Dùng lệnh `DrawingCanvas.Children.Clear()` $\rightarrow$ hủy toàn bộ đối tượng làm rỗng tham chiếu $\rightarrow$ GC (Garbage Collector) sẽ dọn dẹp bộ nhớ và không thể khôi phục lại các nét vẽ.
    *   *Giải pháp tối ưu:* Trước khi thực hiện lệnh xóa canvas, đóng gói danh sách tất cả các đối tượng đồ họa (nét vẽ Polyline) hiện tại vào một hành động Batch kiểu `ActionType.ClearAll`. Khi giáo viên gọi Undo, duyệt danh sách này để add lại từng nét vẽ vào `Children` của Canvas và danh sách quản lý nét vẽ `_strokes`.

### 4. Thiết kế chức năng Undo/Redo cho Bảng trắng Học sinh
*   **Vấn đề:** Phân hệ bảng trắng của học sinh hoàn toàn thiếu chức năng Undo/Redo, gây ức chế sư phạm khi học sinh vẽ sai.
*   **Phương pháp thực hiện & Phản biện:**
    *   *Sai lầm phổ biến:* Sao chép nguyên mẫu hệ thống Command Pattern phức tạp của giáo viên sang phân hệ học sinh $\rightarrow$ gây thừa mã nguồn, dễ phát sinh lỗi đồng bộ và tăng kích thước tệp ứng dụng của Client.
    *   *Giải pháp tối ưu:* Vì học sinh chỉ thao tác với các nét cọ vẽ tự do (`Stroke`) trên `InkCanvas`, phương án an toàn và tối ưu nhất là sử dụng cơ chế **Memento Pattern** (Chụp ảnh trạng thái). Sử dụng 2 stack `_undoStack` và `_redoStack` lưu trữ trạng thái bản sao của `StrokeCollection`. Mỗi khi học sinh vẽ xong hoặc xóa nét vẽ, ta nhân bản danh sách nét vẽ hiện tại (`Strokes.Clone()`) và đẩy vào stack. Khi bấm Undo, ta phục hồi lại bản sao trước đó. Cách làm này cực kỳ đơn giản, hoạt động trơn tru và coder không thể viết sai được.

---

## ═══ PHẦN 2: THIẾT KẾ CẤU TRÚC DỮ LIỆU & KIẾN TRÚC LỚP ═══

### 1. Cấu trúc lưu trữ trạng thái biến đổi (WPF / C#)
Lập trình viên tạo lớp trợ giúp lưu trữ trạng thái sau tại namespace `QASmartTouch.Models`:
```csharp
public class TransformationState
{
    public Point Position { get; set; }
    public Size Size { get; set; }
    public double RotationAngle { get; set; }
}
```

### 2. Sơ đồ trạng thái stack hoàn tác (Bảng trắng Học sinh)
```
[Nét vẽ 1] ---> Vẽ tiếp nét 2 ---> [Nét vẽ 1 + 2] (Top of Stack)
  (Undo)                             (Redo)
  Đẩy [Nét vẽ 1 + 2] sang Redo Stack; Khôi phục Canvas về trạng thái [Nét vẽ 1]
```

---

## ═══ PHẦN 3: KẾ HOẠCH TRIỂN KHAI CHI TIẾT (STEP-BY-STEP WORKFLOW) ═══

### 📋 Bước 1: Chuẩn hóa mã hóa ký tự Unicode và Font chữ (XAML)
*   **Mô tả:** Đưa các tệp tin XAML về chuẩn UTF-8 with BOM và thay thế chuỗi hiển thị bị hỏng bằng tiếng Việt chuẩn Unicode.
*   **Dữ liệu đầu vào (Input):** Tệp tin XAML hiện tại chứa chuỗi ký tự bị lỗi hiển thị.
*   **Phương pháp thực hiện:**
    1.  Mở tệp tin bằng Visual Studio, chọn `File` -> `Save As...`.
    2.  Bấm vào mũi tên cạnh nút `Save`, chọn `Save with Encoding...`.
    3.  Tại mục `Encoding`, chọn `Unicode (UTF-8 with signature) - Codepage 65001`. Nhấn `OK`.
    4.  Cập nhật lại các chuỗi ký tự hiển thị trực tiếp trong file theo đúng Unicode tiếng Việt.
*   **Kết quả đầu ra (Output):** Tệp tin XAML được biên dịch chính xác mà không gặp lỗi phông chữ trên mọi máy tính.

---

### 📋 Bước 2: Bổ dung Logic hoàn tác Modify (CanvasPage / Form2_MainDashboard)
*   **Mô tả:** Triển khai chụp và phục hồi trạng thái di chuyển/xoay/co giãn hình vẽ trên bảng vẽ tương tác chính.
*   **Dữ liệu đầu vào (Input):** Các hành động chuột kéo thả, xoay đối tượng hình học.
*   **Phương pháp thực hiện:**
    1.  Tại sự kiện bắt đầu tương tác chuột (`OnMouseDown` của đối tượng vẽ): Chụp trạng thái ban đầu của hình vẽ và lưu vào biến thành viên `_tempOldState`.
    2.  Tại sự kiện kết thúc tương tác chuột (`OnMouseUp` của đối tượng vẽ): Chụp trạng thái kết thúc `newState`.
    3.  Tạo hành động `UndoRedoAction` với kiểu `ActionType.Modify`, gán `OldValue = _tempOldState` và `NewValue = newState`. Đẩy vào stack hoàn tác chính.
    4.  Cập nhật logic `ExecuteUndoAction` để khôi phục `OldValue` khi Undo và `ExecuteRedoAction` để khôi phục `NewValue` khi Redo.
*   **Kết quả đầu ra (Output):** Giáo viên có thể hoàn tác chính xác các bước di chuyển, thay đổi kích thước và góc xoay của hình ảnh, biểu đồ toán học.

---

### 📋 Bước 3: Hoàn thiện logic hoàn tác Clear All trên Desktop Overlay
*   **Mô tả:** Đóng gói toàn bộ nét vẽ trên màn hình phụ nổi cảm ứng để hỗ trợ hoàn tác sau khi dọn bảng.
*   **Dữ liệu đầu vào (Input):** Danh sách các nét vẽ cảm ứng `_strokes` và `DrawingCanvas.Children`.
*   **Phương pháp thực hiện:**
    1.  Trong hàm `ClearAll()` của `AnnotationOverlay.xaml.cs`, trước khi dọn dẹp canvas, sao chép toàn bộ các đối tượng nét vẽ Polyline hiện hành vào một danh sách tạm thời `List<UIElement> tempStrokes`.
    2.  Đóng gói danh sách này vào một hành động kiểu `ActionType.ClearAll` và đẩy vào `_undoRedoManager`.
    3.  Cập nhật hàm `ExecuteUndoAction` để nếu kiểu là `ClearAll`, duyệt qua danh sách và add lại các nét vẽ vào Canvas.
*   **Kết quả đầu ra (Output):** Khi giáo viên vô tình bấm nút Clear All trên toolbar nổi cảm ứng, bấm Undo sẽ khôi phục lại toàn bộ nét viết vẽ trên màn hình.

---

### 📋 Bước 4: Tích hợp Undo/Redo cho Bảng trắng Học sinh (StudentLocalWhiteboardPage)
*   **Mô tả:** Triển khai 2 stack lưu trữ lịch sử nét vẽ và tạo các nút bấm điều khiển Undo/Redo trên giao diện học sinh.
*   **Dữ liệu đầu vào (Input):** Sự kiện vẽ nét (`StrokeCollected`) và tẩy nét (`StrokeErased`).
*   **Phương pháp thực hiện:**
    1.  Khai báo `_undoStack` và `_redoStack` kiểu `Stack<StrokeCollection>` trong code-behind của bảng vẽ học sinh.
    2.  Mỗi khi nhận được sự kiện thay đổi nét vẽ, nhân bản danh sách nét vẽ hiện tại của `InkCanvas` và đưa vào stack hoàn tác.
    3.  Tạo hai nút bấm `btnUndo` và `btnRedo` trên toolbar của học sinh, liên kết sự kiện click để pop stack và đè lại thuộc tính `Strokes` của `localInkCanvas`.
*   **Kết quả đầu ra (Output):** Học sinh có khả năng hoàn tác/làm lại từng nét vẽ cọ dễ dàng, giảm thao tác tẩy xóa thủ công.

---

## ═══ PHẦN 4: CHECKSHEET KIỂM THỬ CHẤT LƯỢNG (QA CHECKSHEET) ═══

Dành cho bộ phận kiểm thử phần mềm (Tester/QA) để xác nhận chất lượng trước khi phát hành phiên bản v4.2 thương mại:

### 🎯 Phân hệ 1: Kiểm định phông chữ và Giao diện (Font & Spacing)
- [ ] **TC-1.1:** Đảm bảo tất cả các tooltip trên [FloatingToolbarWindow.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/FloatingToolbarWindow.xaml) và [CanvasPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Classroom/Views/CanvasPage.xaml) hiển thị chuẩn tiếng Việt Unicode (Không có ký tự rác như `HoAn tAc`, `Lm l?i`).
- [ ] **TC-1.2:** Icon Undo/Redo trên thanh công cụ hiển thị rõ ràng ký hiệu mũi tên `↩️` và `↪️`, không bị vỡ hoặc chèn ép giao diện.
- [ ] **TC-1.3:** Trên Bảng vẽ học sinh, toàn bộ nhãn hiển thị như "Tẩy nét", "Xóa sạch", "Màu cọ", "Cỡ nét" hiển thị chuẩn tiếng Việt không bị lỗi font.
- [ ] **TC-1.4:** Giữa nhóm nút Undo/Redo và nút Xóa sạch có khoảng cách đệm (hoặc thanh phân chia dọc) tối thiểu `12px` để tránh bấm nhầm bằng ngón tay trên màn hình cảm ứng tương tác.

### 🎯 Phân hệ 2: Kiểm định Logic chức năng Hoàn tác (Undo/Redo Logic)
- [ ] **TC-2.1:** Vẽ 1 nét cọ cọ tự do $\rightarrow$ Nhấn nút **Undo** (hoặc `Ctrl + Z`) $\rightarrow$ Nét vẽ biến mất. Nhấn **Redo** (hoặc `Ctrl + Y`) $\rightarrow$ Nét vẽ xuất hiện lại chính xác.
- [ ] **TC-2.2:** Di chuyển một hình vẽ 2D từ vị trí A sang B $\rightarrow$ Nhấn **Undo** $\rightarrow$ Hình vẽ tự động quay lại vị trí A. Nhấn **Redo** $\rightarrow$ Hình vẽ di chuyển đến vị trí B.
- [ ] **TC-2.3:** Xoay một đối tượng hình học góc 90 độ $\rightarrow$ Nhấn **Undo** $\rightarrow$ Đối tượng xoay ngược lại góc ban đầu.
- [ ] **TC-2.4:** Vẽ nhiều nét chú thích trên Desktop Overlay $\rightarrow$ Nhấn **Xóa tất cả (Clear All)** $\rightarrow$ Bảng trống $\rightarrow$ Nhấn **Undo** $\rightarrow$ Toàn bộ các nét vẽ trước đó xuất hiện lại đầy đủ thông tin, không bị mất nét hay vỡ hình.
- [ ] **TC-2.5:** Đảm bảo khi stack đạt tối đa số bước giới hạn (`MaxUndoSteps`), hệ thống thực hiện cắt tỉa phần dưới cùng của stack (Oldest actions) một cách an toàn mà không làm treo hay crash ứng dụng.

### 🎯 Phân hệ 3: Kiểm định trên Thiết bị Học sinh (Student Client)
- [ ] **TC-3.1:** Bảng vẽ học sinh hiển thị đầy đủ hai nút lệnh **Hoàn tác** và **Làm lại** trên thanh công cụ.
- [ ] **TC-3.2:** Học sinh vẽ các nét viết tự do $\rightarrow$ Nhấn nút **Hoàn tác** $\rightarrow$ Nét vẽ bị hủy bỏ. Nhấn **Làm lại** $\rightarrow$ Nét vẽ xuất hiện lại.
- [ ] **TC-3.3:** Học sinh vẽ bài $\rightarrow$ Gọi sự kiện **Xóa sạch** $\rightarrow$ Bảng trống $\rightarrow$ Nhấn **Hoàn tác** $\rightarrow$ Các nét vẽ quay lại đầy đủ.
- [ ] **TC-3.4:** Gửi bài vẽ lên máy giáo viên $\rightarrow$ Trình trạng nộp bài thành công và không xảy ra rò rỉ bộ nhớ (Memory Leak) do vẽ/tẩy nhiều lần trên Client.
