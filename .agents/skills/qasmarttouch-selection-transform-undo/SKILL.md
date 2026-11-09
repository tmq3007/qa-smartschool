---
name: qasmarttouch-selection-transform-undo
description: >-
  Chuyên gia quản lý phân hệ Chọn vùng (Rectangle, Lasso, Đũa phép Magic Wand), Khung bao SelectionBox,
  Thanh công cụ ngữ cảnh ContextToolbar (Đổi màu, Độ dày, Copy, Xoay 90°, Lật ngang/dọc, Khóa, Xóa),
  chỉ mục không gian QuadTree và hệ thống Hoàn tác Undo/Redo trong QASmartTouch. BẮT BUỘC kích hoạt skill này
  khi thêm đối tượng mới lên bảng hoặc sửa bất kỳ lỗi nào liên quan đến chọn đối tượng, biến đổi hình học,
  đổi thuộc tính, hoặc Undo/Redo.
---

# Chuyên Gia Chọn Vùng, Biến Đổi Hình Học & Undo/Redo (`Selection, Transform & History Parity`)

Skill này bảo vệ **Điểm nóng #2 của dự án** (`Form2_MainDashboard.Selection.cs`, `Form2_MainDashboard.LassoSelection.cs`, `Form2_MainDashboard.Tools.cs`, `Form2_MainDashboard.xaml.cs`, `Services/SelectionManager.cs`, `Services/TransformService.cs`, `Controls/SelectionBox.xaml.cs`, `Controls/ContextToolbar.xaml.cs`).

---

## 1. Hợp Đồng Đồng Bộ 6 Nhóm Phần Tử (`6-Type Element Parity`)

Một `SelectableObject` trên bảng vẽ có thể đại diện cho **6 cấu trúc dữ liệu hoàn toàn khác nhau**. Mọi thao tác trong phân hệ Chọn vùng và Undo/Redo **BẮT BUỘC** phải xử lý đủ cả 6 nhóm (không được chỉ viết `if (obj.Element is Shape)` rồi bỏ rơi các nhóm còn lại):

| Nhóm Phần Tử | Cách Nhận Diện Trong Code | Đặc Thù Kỹ Thuật Phải Lưu Ý |
| :--- | :--- | :--- |
| **1. Nét Mực Tự Do (`Native Stroke`)** | `obj.Stroke != null` (`obj.Element == null`, nằm trong `MainInkPresenter.Strokes`) | • Màu: `obj.Stroke.DrawingAttributes.Color`<br>• Độ dày: `DrawingAttributes.Width` & `Height`<br>• Biến đổi: biến đổi ma trận trên `StylusPoints` qua `ElementTransformState`<br>• Undo thuộc tính: `RecordModifyStrokeAction(...)` |
| **2. Hình Học 2D & Đường Gấp Khúc** | `obj.Element is Shape` (`Line`, `Rectangle`, `Ellipse`, `Polygon`, `Path`, `Polyline`) | • Màu viền: `shape.Stroke`; Màu tô (nếu có, vd đầu mũi tên): `shape.Fill`<br>• Độ dày: `shape.StrokeThickness`<br>• Undo thuộc tính: `RecordModifyAction(...)` |
| **3. Khối Hình Học 3D** | `obj.Element is Canvas shapeCanvas` có `Tag?.ToString() == "3DShape"` | • Chứa nhiều `Shape`/`Path` con trong `shapeCanvas.Children` (cạnh nhìn thấy nét liền + cạnh khuất nét đứt `StrokeDashArray`).<br>• Khi Đổi màu, Đổi độ dày, Đũa phép (`ExtractPrimaryColor`), Copy hoặc `ApplyModifyValue`: **phải duyệt `shapeCanvas.Children.OfType<Shape>()`** và **giữ nguyên `StrokeDashArray`** của nét đứt! |
| **4. Khung Văn Bản / Thẻ OCR** | `obj.Element is Border border` (chứa `TextBlock` hoặc `TextBox` bên trong) | • Viền khung: `border.BorderBrush`, `border.BorderThickness`.<br>• Nội dung & màu chữ: truy xuất phần tử chữ con bên trong (`TextBlock`/`TextBox`). |
| **5. Bảng Biểu & Biểu Đồ** | `obj.Element is Grid gridContainer` (`TableEditorControl` hoặc Chart container) | • Chọn, di chuyển, thu phóng, nhân bản và xóa nguyên khối container `Grid`. |
| **6. Ảnh, Đa Phương Tiện & STEM** | `obj.Element is Image` hoặc container Video / `WebView2` / Học cụ | • Khi Xóa (`OnToolbarDeleteClicked`, `ClearAll`, `Redo ClearAll`, `Undo Add`): bắt buộc gọi `CleanupWebView2Media(element)` để tắt âm thanh/tiến trình trình duyệt ngầm. |

---

## 2. Bảy Điểm Chạm Phải Khớp Nhau (`7-Point Synchronization Checklist`)

Mỗi khi sửa lỗi hoặc thêm tính năng liên quan tới đối tượng trên bảng, hãy kiểm tra lần lượt **7 điểm chạm** sau:

### Điểm 1: Đăng Ký Tức Thời & Quét Toàn Bảng
* Khi một phần tử mới được tạo ra trên bảng, phải gọi ngay `RegisterNewObjectWithSelectionManager(element)` (hoặc `RegisterNewStrokeWithSelectionManager(stroke)`).
* Đồng thời kiểm tra hàm `RefreshSelectableObjects()` trong `Form2_MainDashboard.xaml.cs` đã nhận diện đúng loại phần tử đó chưa (vì khi người dùng bấm sang công cụ Chọn vùng, `ActivateRectangleSelectionMode` / `Lasso` / `MagicWand` sẽ gọi `RefreshSelectableObjects()` quét lại từ đầu!).

### Điểm 2: Bộ Ba Đồng Bộ Không Gian Sau Mọi Biến Đổi (`Bounds + SelectionBox + QuadTree`)
* `SelectionManager.HitTest` dùng cây không gian **QuadTree**. Nếu phần tử đã đổi vị trí/kích thước/xoay/độ dày trên màn hình nhưng không cập nhật QuadTree, người dùng chạm vào hình ở chỗ mới sẽ không chọn được!
* Sau **mọi** thao tác Kéo thả, Resize, Xoay 90°, Lật, Đổi độ dày, **VÀ sau khi khôi phục trong `ApplyModifyValue` (Undo/Redo)**, luôn đảm bảo chuỗi 3 lệnh:
  ```csharp
  obj.UpdateBounds();
  _selectionBox?.UpdatePosition();
  _selectionManager?.RebuildQuadTree();
  ```

### Điểm 3: Đổi Màu & Chọn Cùng Màu Bằng Đũa Phép (`Color & Magic Wand`)
* Phải đồng bộ logic ở cả **3 hàm**:
  1. `OnColorSelected` (`Form2_MainDashboard.Selection.cs`)
  2. `ExtractPrimaryColor` (`Form2_MainDashboard.Tools.cs` — dùng cho Đũa phép `PerformMagicWandSelection`)
  3. `ApplyModifyValue` nhánh `if (value is Color color)` (`Form2_MainDashboard.Tools.cs` — dùng cho Undo/Redo).

### Điểm 4: Đổi Độ Dày Nét (`Thickness`)
* Đồng bộ ở cả `OnThicknessChanged` (`Form2_MainDashboard.Selection.cs`) và nhánh `if (value is double dVal)` trong `ApplyModifyValue` (`Form2_MainDashboard.Tools.cs`), kèm cập nhật `obj.UpdateBounds()` và `_selectionBox?.UpdatePosition()`.

### Điểm 5: Di Chuyển, Resize, Xoay 90° & Lật Đối Xứng (`Geometric Transforms`)
* Mọi phép biến đổi qua `SelectionBox` hoặc nút trên `ContextToolbar` (`OnToolbarRotate90Clicked`, `OnToolbarFlipHorizontalClicked`, `OnToolbarFlipVerticalClicked`) phải chụp `ElementTransformState` **trước** (`initialStates`) và **sau** (`finalStates`) cho từng đối tượng (kể cả khi đang chọn nhóm `ObjectType.Group` nhiều phần tử hỗn hợp cả `Stroke` lẫn `UIElement`), rồi đẩy vào `RecordTransformAction(initialStates, finalStates)`.

### Điểm 6: Hệ Thống Undo / Redo Duy Nhất Trong `Form2_MainDashboard.Tools.cs`
* ⚠️ **Lưu ý quan trọng:** Mọi thao tác trên `Form2_MainDashboard` sử dụng trực tiếp `_undoStack` và `_redoStack` trong `Form2_MainDashboard.Tools.cs` (`RecordAction`, `ExecuteUndoAction`, `ExecuteRedoAction`, `ApplyModifyValue`). **Không** gọi vào các lớp Undo cũ đã `[Obsolete]`.
* Kiểm tra đối xứng tuyệt đối khi Undo/Redo:
  * **Khi phần tử bị gỡ khỏi bảng** (`Undo Add`, `Redo Remove`, `Redo ClearAll`): Phải xóa khỏi `MainInteractiveBoard.Children` / `MainInkPresenter.Strokes`, gọi `_selectionManager?.RemoveObjectByElement(...)` / `RemoveObjectByStroke(...)`, và **nếu đối tượng đó đang được chọn thì phải gọi `_selectionBox?.Detach()` + `_contextToolbar?.Hide()`** để không để lại "khung chọn ma" lơ lửng trên màn hình!
  * **Khi phần tử được đưa trở lại bảng** (`Undo Remove`, `Undo ClearAll`, `Redo Add`): Phải thêm lại vào Canvas/`InkPresenter` **VÀ gọi `RegisterNewObjectWithSelectionManager` / `RegisterNewStrokeWithSelectionManager`** để người dùng có thể chọn lại được ngay!

### Điểm 7: Chống Nhảy Đối Tượng ("Teleport Bug") & Kẹp Biên Menu Trên Màn Hình Cảm Ứng
* Trong `MainBoard_SelectionMouseDown`, luôn dọn sạch trạng thái kéo cũ (`_draggedSelectionObject`) ở đầu hàm và không gọi `MainInteractiveBoard.CaptureMouse()` đè lên khi `_selectionBox` đang `IsTransforming`.
* Mọi thanh nổi (`_contextToolbar`, `_colorPicker`, `_thicknessPicker`, `_moreMenu`) phải tính toán tọa độ qua `CalculateOptimalToolbarPosition` và kẹp biên nằm gọn trong `MainInteractiveBoard.ActualWidth` × `ActualHeight`, tự động lật lên trên nếu đối tượng nằm sát đáy màn hình gần Toolbar chính.

---

## 3. Kịch Bản Test Tay Hồi Quy Bắt Buộc Sau Khi Sửa Vùng Selection / Undo

Sau khi sửa code phân hệ này, hướng dẫn người dùng test nhanh 4 bước:
1. **Chọn hỗn hợp (Mixed Group):** Khoanh vùng chọn cùng lúc **1 nét bút viết tay + 1 hình 2D + 1 khối 3D** $\rightarrow$ Kéo di chuyển $\rightarrow$ Đổi màu $\rightarrow$ Xoay 90°.
2. **Kiểm tra HitTest sau biến đổi:** Bấm ra ngoài bỏ chọn $\rightarrow$ Chạm chọn lại từng hình ở **vị trí mới** xem QuadTree có bắt dính chính xác không.
3. **Chuỗi Undo / Redo khép kín:** Xóa nhóm đối tượng đó $\rightarrow$ Bấm **Undo** (hình hiện lại, chạm chọn lại phải ăn ngay) $\rightarrow$ Bấm **Redo** (hình biến mất, không còn khung `SelectionBox` treo sót lại).
4. **Đũa phép / Lasso:** Thử chọn nhanh bằng Lasso hoặc Đũa phép rồi chuyển về Bút vẽ xem có mượt không.

