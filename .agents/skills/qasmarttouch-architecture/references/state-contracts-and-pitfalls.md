# Sổ Đen Hợp Đồng Trạng Thái & Bẫy Lịch Sử (`State Contracts & Known Pitfalls`)

Đây là danh sách các **quy ước ngầm và bẫy kỹ thuật thực tế** trong mã nguồn `QASmartTouch`. Luôn đọc kỹ trước khi sửa các luồng liên quan và cập nhật bổ sung vào tệp này (qua lệnh `/learn`) mỗi khi phát hiện thêm bẫy mới.

---

## 💣 BẪY 1: `SelectTool(Button)` Tự Động Gọi `ResetAllToolStates()` Xóa Sạch Cờ!
* **Vị trí:** `Form2_MainDashboard.xaml.cs` (`SelectTool` $\rightarrow$ `ResetAllToolStates`) và `Form2_MainDashboard.Tools.cs`.
* **Cơ chế ngầm:** Bên trong hàm `SelectTool(button)` có gọi `ResetAllToolStates()`, hàm này sẽ reset `_isLassoMode = false`, `_isRectangleSelecting = false`, `_isDraggingSelection = false`, `_draggedSelectionObject = null`, đồng thời detach `_selectionBox` và nhả Mouse/Touch Capture.
* **Quy tắc bắt buộc:** Khi kích hoạt bất kỳ chế độ công cụ nào (ví dụ: Chọn chữ nhật, Lasso, Đũa phép, Vẽ hình...), **BẮT BUỘC gọi `DeactivateAllTools()` và `SelectTool(btn)` TRƯỚC**, sau đó mới gán các biến cờ chế độ riêng (`_objectSelectionMode = true`, `_isLassoMode = true`, `_isMagicWandMode = true`...) và gọi `_whiteboardManager.ToolManager.SwitchTo(...)`. Nếu gán cờ trước khi gọi `SelectTool()`, cờ sẽ bị xóa trắng ngay lập tức!

---

## 💣 BẪY 2: `EnsurePassthroughFocusAndPenMode` Tự Động Nhảy Về Bút Vẽ Khi Chạm Bảng!
* **Vị trí:** `Form2_MainDashboard.Input.cs` (đầu tệp, hàm `EnsurePassthroughFocusAndPenMode` và `IsDragHandleOrEmbeddedWidget`).
* **Cơ chế ngầm:** Để hỗ trợ giáo viên đang mở cửa sổ phụ (Trình duyệt, Máy tính...) có thể chạm thẳng vào bảng viết ngay lập tức, hàm này sẽ tự động kích hoạt lại chế độ Bút vẽ trừ khi phát hiện đang ở một công cụ hợp lệ khác.
* **Quy tắc bắt buộc:**
  1. Mọi công cụ tương tác trên bảng phải chuyển `ToolManager.CurrentMode` sang giá trị khác `ToolMode.None` và khác `ToolMode.Pen` (hoặc có guard tường minh trong `EnsurePassthroughFocusAndPenMode`).
  2. Mọi tay nắm kéo thả, góc thu phóng, thanh điều khiển nhúng trên Canvas phải có `Tag` hoặc `Name` được nhận diện trong `IsDragHandleOrEmbeddedWidget(originalSource)`, nếu không khi chạm tay kéo widget sẽ bị phát sinh nét mực vẽ đè lên bảng.
  3. Khi duyệt cây giao diện từ `originalSource` đi lên trong WPF, luôn kiểm tra `element is Visual || element is Visual3D` trước khi gọi `VisualTreeHelper.GetParent(element)` (với `Run`/`Paragraph`/`FlowDocument` là `ContentElement`, phải dùng `LogicalTreeHelper.GetParent` để không nổ `InvalidOperationException`).

---

## 💣 BẪY 3: Nghịch Lý Kép Của `MainInkPresenter` & `BackgroundLayer` Trong `IsSystemElement()`
* **Vị trí:** `Form2_MainDashboard.Tools.cs` (`IsSystemElement`), `Managers/BoardManager.cs`, và `Services/CanvasExportService.cs`.
* **Cơ chế ngầm:**
  * `MainInkPresenter` trả về `true` trong `IsSystemElement()` để khi bấm **Xóa toàn bộ bảng (`ClearAll`)**, vòng lặp xóa `MainInteractiveBoard.Children` **không xóa mất chính control `InkPresenter`** (chỉ được xóa dữ liệu nét mực qua `MainInkPresenter.Strokes.Clear()`).
  * Ngược lại, trong `CanvasExportService.CaptureCanvasToImageBytes()`, hàm cũng truyền `IsSystemElement` vào để tạm ẩn các khung điều khiển UI (`_selectionBox`, `_contextToolbar`...) khi chụp ảnh xuất PDF/PNG.
* **Quy tắc bắt buộc:** Trong mọi hàm chụp màn hình / xuất ảnh / xuất PDF / in ấn, **TUYỆT ĐỐI PHẢI BỎ QUA (KHÔNG ĐƯỢC ẨN)** `InkPresenter` và phần tử có `Tag == "BackgroundLayer"`.

---

## 💣 BẪY 4: Quy Ước Định Danh Qua Chuỗi `Tag` Trên `MainInteractiveBoard`
Nhiều phân hệ trong dự án nhận diện loại đối tượng thông qua thuộc tính `FrameworkElement.Tag`. Tuyệt đối không tự ý đổi hoặc ghi đè `Tag` của các phần tử sau:
* `Tag = "BackgroundLayer"`: Lớp nền màu / dòng kẻ / ô ly của bảng.
* `Tag = "3DShape"`: `Canvas` container chứa các `Path` cạnh nhìn thấy (nét liền) và cạnh khuất (nét đứt `StrokeDashArray`) của khối hình học 3D.
* `Tag = "DragHandle"` / `"ResizeHandle"` / `"SelectionBox"`: Tay nắm điều khiển tương tác (được `IsDragHandleOrEmbeddedWidget` bảo vệ khỏi nét vẽ).
* `Tag = "InteractiveYouTubeVideo"` / `"InteractiveGoogleMaps"` / `"InteractiveLocalVideo"` / `"LocalVideoContainer"`: Các khung đa phương tiện nhúng cần gọi `CleanupWebView2Media` khi xóa.

---

## 💣 BẪY 5: Lệch Chỉ Mục Không Gian `QuadTree` Khi Biến Đổi Hoặc Undo/Redo
* **Vị trí:** `Services/SelectionManager.cs`, `Form2_MainDashboard.Selection.cs`, `Form2_MainDashboard.Tools.cs` (`ApplyModifyValue`).
* **Cơ chế ngầm:** `SelectionManager.HitTest(point)` không quét trực tiếp Visual Tree của WPF mà tra cứu trên cây không gian `QuadTree` dựa vào `SelectableObject.Bounds` đã cache.
* **Quy tắc bắt buộc:** Bất cứ khi nào tọa độ, kích thước, góc xoay, phép lật hoặc độ dày nét của `UIElement` / `Stroke` thay đổi (bao gồm cả lúc kéo thả, lúc bấm nút trên `ContextToolbar`, **và lúc chạy `Undo`/`Redo` trong `ApplyModifyValue`**), bắt buộc phải gọi:
  1. `obj.UpdateBounds()`
  2. `_selectionBox?.UpdatePosition()` (nếu đối tượng đang được chọn)
  3. `_selectionManager?.RebuildQuadTree()`

---

## 💣 BẪY 6: Thêm Tệp Trong `QASmartClass/Services/` Nhưng Quên Khai Báo Trong `QASmartTouch.csproj`
* **Vị trí:** `QASmartTouch/QASmartTouch.csproj` (dòng 110–207).
* **Cơ chế ngầm:** Trong `QASmartTouch.csproj`, thư mục gốc `Services/*.cs` và các hộp thoại `Forms/*.xaml` (không bắt đầu bằng `Form2_`) được liệt kê **từng file một** chứ không dùng wildcard `**/*` (để loại bỏ các service mạng/SQLite của `QASmartClass`).
* **Quy tắc bắt buộc:** Nếu thêm file mới vào `QASmartClass/Services/TenServiceMoi.cs` hoặc hộp thoại tiện ích mới, phải thêm dòng `<Compile Include="..\QASmartClass\Services\TenServiceMoi.cs" Link="Services\TenServiceMoi.cs" />` vào `QASmartTouch/QASmartTouch.csproj` và chạy `dotnet build QASmartTouch/QASmartTouch.csproj` kiểm chứng ngay.

