---
name: qa-smartschool-selection
description: >-
  Skill chuyên biệt quản lý và tham chiếu Module Chọn Vùng (Selection Module)
  trong file Form2_MainDashboard.xaml.cs. Sử dụng skill này để tra cứu
  nhanh các dòng code xử lý vùng chọn, bắt sự kiện chuột/cảm ứng và
  di chuyển/thao tác với đối tượng, tránh việc phải đọc lại toàn bộ file.
---

# Form2_MainDashboard — Module Chọn Vùng (Selection)

Tài liệu này lưu trữ các điểm mấu chốt, biến trạng thái và vị trí code liên quan đến tính năng **Chọn vùng, kéo rê đối tượng** trong bản vẽ SmartTouch.

## 1. Các biến và đối tượng cốt lõi

- **`_objectSelectionMode`**: Cờ (boolean) xác định trạng thái hiện tại có đang bật công cụ chọn vùng hay không.
- **`_selectionManager`**: Quản lý danh sách các đối tượng có thể được chọn (HitTest, Copy/Paste...).
- **`_selectionBox`**: UI hiển thị viền bao quanh đối tượng được chọn (hỗ trợ resize, rotate).
- **`_draggedSelectionObject`**: Lưu đối tượng đang được người dùng bấm giữ và chuẩn bị kéo thả (Drag).

## 2. Vị trí Code (Line Reference) trong `Form2_MainDashboard.xaml.cs`

File `Form2_MainDashboard.xaml.cs` có kích thước rất lớn (~25000 dòng). Hãy tham chiếu các vị trí sau để can thiệp đúng chỗ:

### 2.1. Sự kiện MouseDown chung (`MainInteractiveBoard_MouseDown`)
- **Dòng ~4450 - 4500**: Logic kiểm tra va chạm (Hit) với các UI nổi (DragHandle, widget nhúng). Ngăn chặn vẽ nét đè lên widget.
- **Dòng ~4630 - 4652**: Đoạn code ủy quyền xử lý từ `MouseDown` chung sang chế độ Chọn vùng. 
  - *Lưu ý quan trọng*: Có xử lý `if (_selectionBox != null && _selectionBox.IsTransforming)` để tránh xung đột chuột khi đang kéo dãn (resize/rotate) vùng chọn.
  - Tại đây sẽ gọi hàm `MainBoard_SelectionMouseDown(sender, e)`.

### 2.2. Xử lý Logic Chọn cụ thể (`MainBoard_SelectionMouseDown`)
- **Dòng ~17290 - 17362**: Logic chính khi người dùng bấm chuột ở chế độ chọn:
  - `_selectionManager.HitTest(clickPoint)`: Kiểm tra xem có bấm trúng đối tượng nào không.
  - `isClickOnSelectedObject`: Kiểm tra xem đối tượng vừa bấm có đang nằm trong danh sách đã chọn hay không.
  - Phân loại xử lý: Kéo di chuyển (`_draggedSelectionObject`), chọn mới (`_pendingHitObject`), hoặc chuẩn bị quét khối hình chữ nhật (`_rectangleSelectionStartPoint`).

### 2.3. Logic di chuyển đối tượng (`MoveSelectableObject`)
- **Dòng ~17368+**: Hàm di chuyển một đối tượng (`SelectableObject obj, Vector dragVector`). Hỗ trợ di chuyển `Polyline`, `Line`, `Path` (TranslateTransform), `FrameworkElement` (TextBlock, Image, ...).

## 3. Chú ý khi xử lý sự kiện Chuột & Cảm ứng (Mouse/Touch Handling)

Khi cần sửa các lỗi "Bấm ra ngoài vùng chọn lại biến thành kéo đối tượng" (thường gặp trên màn hình cảm ứng hồng ngoại):

1. **Ưu tiên sự kiện chuột `MouseDown`**: Các logic chọn hiện tại đang được viết rất kỹ ở `MainInteractiveBoard_MouseDown` và `MainBoard_SelectionMouseDown`. 
2. **Can thiệp `ManipulationStarting`**: Thay vì viết lại hệ thống Touch phức tạp, nếu màn hình nhận nhầm Touch thành Drag, hãy hủy sự kiện manipulation nếu chỉ có 1 ngón chạm.
   - Hàm can thiệp khuyên dùng: `MainInteractiveBoard_ManipulationStarting`
   - Gọi `e.Cancel()` khi `TouchesCount == 1`.
3. **Quản lý Mouse Capture**: Cẩn thận với `MainInteractiveBoard.CaptureMouse();`. Chỉ Capture khi thực sự cần thiết (chuẩn bị vẽ hoặc kéo). Nếu quên Release sẽ gây lỗi kẹt trạng thái chuột (`_isMoving` bị kẹt `true`).

### 3.1. QC_4.2_TOUCH_TELEPORT_FIX (đã áp dụng)

Fix lỗi "Teleport" — vùng chọn bay đến vị trí ngón tay khi chạm ra ngoài trên cảm ứng:

- **Canvas XAML** (`Form2_MainDashboard.xaml`): Đã thêm `Stylus.IsFlicksEnabled="False"`, `Stylus.IsPressAndHoldEnabled="False"`, `Stylus.IsTapFeedbackEnabled="False"`, `Stylus.IsTouchFeedbackEnabled="False"` để chặn Windows nuốt `MouseUp`.
- **Safety guard** (`MainBoard_SelectionMouseDown`, dòng ~17294): Đầu hàm kiểm tra `_draggedSelectionObject != null` → reset state trước khi xử lý click mới.
- **CaptureMouse kép** (`MainInteractiveBoard_MouseDown`, dòng ~4649): Không CaptureMouse nếu `SelectionBox` đang attach đối tượng đó (tránh xung đột capture giữa `MainInteractiveBoard` và `SelectionBorder`).
- **MouseLeave cleanup** (`MainInteractiveBoard_MouseLeave`, dòng ~5374): Cleanup `_draggedSelectionObject` khi ngón tay rời canvas.

> **💡 Mẹo:** Bất cứ khi nào cần sửa logic Select/Drag/Drop, hãy dùng công cụ tìm kiếm với các từ khóa `MainBoard_SelectionMouseDown`, `_draggedSelectionObject`, `_isRectangleSelecting`, `TOUCH_TELEPORT_FIX` để tìm đúng ngữ cảnh.

