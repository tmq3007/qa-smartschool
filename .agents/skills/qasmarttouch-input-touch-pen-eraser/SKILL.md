---
name: qasmarttouch-input-touch-pen-eraser
description: >-
  Chuyên gia điều phối luồng đầu vào Cảm ứng đa điểm (Multi-Touch), Chuột (Mouse), Bút cảm ứng (Stylus),
  công cụ Bút vẽ (Pen), Tẩy (Eraser / NativeEraseSession), Cử chỉ 2 ngón (Pan / PinchZoom) và chuyển đổi
  công cụ trong QASmartTouch. BẮT BUỘC kích hoạt skill này mỗi khi sửa lỗi hoặc phát triển tính năng liên quan
  đến Form2_MainDashboard.Input.cs, TouchHandler.cs, ToolManager.cs, SubMenu Bút/Tẩy, hoặc lỗi kẹt/loạn sự kiện
  giữa các công cụ trên bảng vẽ.
---

# Chuyên Gia Luồng Đầu Vào: Cảm Ứng, Chuột, Bút Vẽ & Tẩy (`Input / Touch / Tool Sync`)

Skill này bảo vệ **Điểm nóng #1 của dự án** (`Form2_MainDashboard.Input.cs` ~10.815 dòng, `TouchHandler.cs`, `Form2_MainDashboard.Tools.cs`, và `WhiteboardCore/Managers/ToolManager.cs`), ngăn chặn triệt để tình trạng **"sửa công cụ A làm kẹt hoặc loạn sự kiện ở công cụ B"**.

---

## 1. Bản Đồ 3 Hệ Thống Trạng Thái Công Cụ Đang Cùng Tồn Tại

Khi một công cụ trên bảng viết hoạt động, có **3 tầng trạng thái** phải luôn khớp nhau 100%:

| Tầng Trạng Thái | Nơi Quản Lý | Biến / Enum Cốt Lõi |
| :--- | :--- | :--- |
| **1. `WhiteboardCore` FSM** | `_whiteboardManager.ToolManager` (`ToolManager.cs` & `OnWhiteboardToolChanged` trong `Form2_MainDashboard.xaml.cs`) | `ToolMode`: `None`, `Pen`, `Eraser`, `Select`, `Shape`, `Insert`, `Zoom`, `Pointer`, `Pan`, `Text`, `AI_Interaction`, `MultiUser`. |
| **2. Bộ Xử Lý Cảm Ứng** | `_touchHandler` (`Handlers/TouchHandler.cs`) | `TouchToolMode`: `None`, `Drawing`, `Eraser`, `Pointer`, `Selection` + hàm dọn phiên `ResetTouchState()`. |
| **3. Cờ UI & Tương Tác Trên `Form2_MainDashboard`** | `Form2_MainDashboard.*.cs` | `_selectedTool`, `_isDrawing`, `_eraserEnabled`, `_objectSelectionMode`, `_isLassoMode`, `_isMagicWandMode`, `_isShapeDrawingMode`, `_isTableDrawingMode`, `_isPointerModeActive`, `_isPanModeActive`, `_isMultiUserModeActive`. |

---

## 2. Giao Thức 5 Bước Bắt Buộc Khi Chuyển Đổi Hoặc Kích Hoạt Công Cụ

Mọi hàm bật/chuyển công cụ mới hoặc sửa đổi **BẮT BUỘC** tuân thủ đúng thứ tự 5 bước sau (sai thứ tự sẽ gây mất cờ hoặc rò rỉ sự kiện):

1. **Dọn sạch công cụ cũ & đóng SubMenu:**
   ```csharp
   DeactivateAllTools();
   CloseAllSubmenus();
   ```
2. **Cập nhật nút Toolbar đang chọn (`SelectTool`) TRƯỚC khi gán cờ mới:**
   * ⚠️ `SelectTool(button)` tự động gọi `ResetAllToolStates()` bên trong (xóa `_isLassoMode`, `_isRectangleSelecting`, `_draggedSelectionObject`, nhả `ReleaseMouseCapture()`, `ReleaseStylusCapture()`, `ReleaseAllTouchCaptures()`).
   * Do đó, **phải gọi `SelectTool(button)` xong xuôi rồi mới được gán các cờ riêng của công cụ mới!**
3. **Đồng bộ `ToolManager` (`WhiteboardCore` FSM):**
   * Gọi `_whiteboardManager?.ToolManager.SwitchTo(ToolMode.<TargetMode>);`
   * Lưu ý trong `OnWhiteboardToolChanged` (`Form2_MainDashboard.xaml.cs`): các mode ngoài `Pen`, `Eraser`, `Select`, `Pointer` mặc định đưa `_touchHandler` về `TouchToolMode.None`.
4. **Đồng bộ `_touchHandler` & Xóa phiên chạm dở dang:**
   * Gọi `_touchHandler?.SetToolMode(TouchToolMode.<TargetTouchMode>);`
   * Nếu chuyển chế độ tương tác cảm ứng hoặc hủy thao tác giữa chừng, gọi `_touchHandler?.ResetTouchState()` để xóa sạch `_activeTouchScreenPoints`, `_isTwoFingerGestureActive`, và `_suppressDrawingUntilAllReleased`.
5. **Khôi phục Focus về Bảng chính khi đóng SubMenu:**
   * Trong sự kiện `Closed` của mọi cửa sổ SubMenu (`Form2_1`, `Form2_2`, `Form2_4`, `Form2_5`, `Form2_6`...), luôn gọi `this.Activate(); this.Focus(); MainInteractiveBoard?.Focus();` và kiểm tra `ReferenceEquals(_activeSubMenu, currentMenu)` trước khi xóa `_activeSubMenu` (tránh xóa nhầm nếu người dùng vừa bấm sang nút Toolbar khác!).

---

## 3. Bốn Chốt Chặn Chống Xung Đột Sự Kiện Trên Màn Hình Cảm Ứng 86"

### Chốt chặn A — Chống `EnsurePassthroughFocusAndPenMode` Cướp Quyền Về Bút Vẽ
* Tại đầu luồng chạm trong `Form2_MainDashboard.Input.cs`, `EnsurePassthroughFocusAndPenMode(e.OriginalSource)` được gọi để tự bật bút khi chạm từ ngoài cửa sổ phụ vào bảng.
* **Kiểm tra bắt buộc:**
  1. Nếu thêm chế độ vẽ/kéo mới trên bảng (như vẽ bảng, vẽ hình, chọn vùng, kéo học cụ), đảm bảo `ToolManager.CurrentMode` **khác `ToolMode.None` và khác `ToolMode.Pen`**, hoặc có nhánh `if (_isYourMode) return;` ở đầu hàm này.
  2. Nếu thêm tay nắm kéo/xoay/thu phóng hoặc thanh điều khiển widget nhúng trên `MainInteractiveBoard`, phải đặt `Tag` (`"DragHandle"`, `"ResizeHandle"`, v.v.) khớp với hàm `IsDragHandleOrEmbeddedWidget(object originalSource)` để cú chạm vào tay nắm không bị biến thành nét vẽ mực!

### Chốt chặn B — Chống Lặp Sự Kiện Kép (`Touch / Stylus` $\rightarrow$ Promoted `Mouse`)
* Trong WPF trên Windows 10/11, một ngón tay chạm vào màn hình phát sinh cả chuỗi `TouchDown` / `StylusDown` **LẪN** sự kiện `MouseDown` giả lập (Promoted Mouse Event).
* **Kiểm tra bắt buộc:**
  * Không xử lý cùng một nét vẽ hoặc cùng một cú xóa hai lần ở cả `_touchHandler` lẫn `MainInteractiveBoard_MouseDown`. Luôn kiểm tra cách nhánh hiện tại đang lọc `e.StylusDevice != null` hoặc ủy quyền qua `_touchHandler`.
  * Tuyệt đối giữ nguyên thuộc tính `Stylus.IsFlicksEnabled="False"` và `Stylus.IsPressAndHoldEnabled="False"` trên `MainInteractiveBoard` và các control kéo thả để Windows không giữ trễ cú chạm (chờ menu chuột phải) hoặc nuốt mất sự kiện nhấc tay (`MouseUp`/`TouchUp`).

### Chốt chặn C — Xử Lý Cử Chỉ 2 Ngón (`PanOnly` vs `PinchZoom`) Không Để Lại "Rác Mực"
* Khi người dùng đặt 2 ngón tay lên bảng để kéo/thu phóng (`TouchHandler.cs`), ngón đầu tiên chạm xuống trước vài mili-giây có thể đã tạo nét vẽ nháp (`_preliminaryStroke`).
* Mọi chỉnh sửa trong `TouchHandler.cs` phải bảo toàn cơ chế: ngay khi phát hiện ngón thứ 2 (`_isTwoFingerGestureActive = true`), phải hủy sạch nét nháp của ngón thứ 1, bật `_suppressDrawingUntilAllReleased = true` cho tới khi **tất cả các ngón tay nhấc hết khỏi màn hình**, tuyệt đối không để ngón còn sót lại vẽ vệt mực dài ngang bảng.

### Chốt chặn D — Đóng Phiên Tẩy `NativeEraseSession` Trọn Vẹn
* Công cụ Tẩy trên `MainInkPresenter` dùng `NativeEraseSession` (tẩy cắt nét `Point` hoặc xóa nguyên nét `Stroke`).
* Phải đảm bảo phiên tẩy luôn được kết thúc và ghi vào `_undoStack` một lần duy nhất khi nhấc tay/chuột (`FinalizeNativeEraseSession` / `OnEndTouchEraseSession`), kể cả khi con trỏ trượt ra khỏi biên Canvas hoặc bị ngắt bởi cử chỉ đa điểm.

---

## 4. Kịch Bản Test Tay Hồi Quy Bắt Buộc Sau Khi Sửa Vùng Input / Tool

Mỗi khi sửa bất kỳ đoạn code nào thuộc phân hệ này, hãy hướng dẫn người dùng test nhanh chuỗi liên hoàn 4 bước sau:
1. **Bút $\rightarrow$ Tẩy $\rightarrow$ Bút:** Vẽ 3 nét $\rightarrow$ Chuyển Tẩy (xóa điểm & xóa nét) $\rightarrow$ Chuyển lại Bút vẽ tiếp xem nét có ăn ngay từ điểm chạm đầu tiên không.
2. **Bút $\rightarrow$ Chọn vùng $\rightarrow$ Kéo thả:** Chuyển sang Chọn vùng, kéo di chuyển nét vừa vẽ xem có bị vẽ đè thêm nét mực mới (lỗi Auto-Pen hijack) không.
3. **SubMenu Toggle:** Bấm mở SubMenu Bút $\rightarrow$ không đóng mà bấm thẳng sang nút Tẩy hoặc Hình học xem SubMenu cũ có đóng gọn và màu nút Toolbar có sáng đúng 1 nút duy nhất không.
4. **Undo / Redo sau Vẽ & Tẩy:** Bấm Undo 2 lần $\rightarrow$ Redo 2 lần xem nét bị tẩy cắt và nét vẽ mới có khôi phục chuẩn xác không.

