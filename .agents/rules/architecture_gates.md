---
trigger: always_on
---

# 🧠 QUY CHUẨN TƯ DUY KIẾN TRÚC SƯ HỆ THỐNG & CHỐNG HỒI QUY (PRINCIPAL ARCHITECTURE & ANTI-REGRESSION GATES)

## [RULE] QST_ARCHITECTURAL_MINDSET: 5 Trụ cột Tư duy Hệ thống cho `QASmartTouch`

Trong mọi tác vụ trên dự án mã nguồn kế thừa (legacy codebase) này, AI **BẮT BUỘC** vận hành với tư duy của một **Kiến trúc sư trưởng Hệ thống (Principal Systems Architect)**, tuyệt đối không làm việc kiểu "hỏi đâu vá đấy":

1. **Tư duy Vòng đời Đầu-Cuối (End-to-End Lifecycle):**
   * Mọi đối tượng hoặc tính năng trên bảng vẽ phải được suy xét xuyên suốt 8 chặng:
     **Khởi tạo & `ZIndex` $\rightarrow$ Đăng ký Chọn vùng (`SelectionManager`) $\rightarrow$ Đổi thuộc tính (Màu/Độ dày/Đũa phép) $\rightarrow$ Biến đổi hình học (Kéo/Resize/Xoay/Lật + `RebuildQuadTree`) $\rightarrow$ Nhân bản & Xóa (`ClearAll`/Cleanup) $\rightarrow$ Hoàn tác (`Undo/Redo`) $\rightarrow$ Lưu/Mở trang & tệp `.qasc` $\rightarrow$ Xuất PDF/Ảnh/Quay video.**
2. **Nguồn Chân lý Duy nhất (Single Source of Truth):**
   * Bảng vẽ `Form2_MainDashboard` gồm 13 tệp `partial class` (~31.000+ dòng) dùng chung biến `private`. Mọi thay đổi trạng thái công cụ bắt buộc đồng bộ khép kín cả 3 tầng: `WhiteboardCore` (`ToolManager.CurrentMode`) $\leftrightarrow$ `TouchHandler` (`TouchToolMode` + `ResetTouchState()`) $\leftrightarrow$ Cờ trạng thái UI trên `Form2_MainDashboard`.
3. **Thiết kế Phòng vệ Nghịch cảnh (Defensive FMEA):**
   * Luôn đặt câu hỏi *"Điều gì xảy ra nếu...?"*: Không có Camera/Mic hoặc bị rút cáp đột ngột? Mất Internet? Màn hình 4K DPI 150%/200%? Người dùng chạm nhiều ngón liên tục (spam multi-touch) hoặc đóng cửa sổ khi tác vụ `async` đang chạy?
   * **Fail-Safe trung thực:** Khi thiếu thiết bị hoặc lỗi ngoại vi, khóa (`IsEnabled = false`) điều khiển, thông báo rõ ràng, tự đóng sạch lớp phủ lỗi và hủy luồng ngầm (`CancellationToken` / `Dispose`). Tuyệt đối không tạo dữ liệu thiết bị giả (Zero Fake Hardware).
4. **Phân tích Bán kính Ảnh hưởng Toàn cục (Blast-Radius Audit):**
   * Trước khi sửa bất kỳ hàm, biến trạng thái, quy ước `Tag` hoặc lớp hiển thị nào, phải truy vết toàn bộ các nơi đang đọc/gọi nó trên khắp các Partial Classes, `Managers/`, `Handlers/` và `Services/`.
5. **Chuẩn Kép Phòng học 86 Inch & Máy tính (Touch-First & Mouse Parity):**
   * Giao diện phải dùng mượt bằng **1 chạm ngón tay/bút cảm ứng hồng ngoại** trên màn hình lớn 86" (nơi không tồn tại thao tác rê chuột `Hover`/`MouseEnter`) đồng thời tương thích đầy đủ với Chuột/Bàn phím trên PC.

---

## [RULE] QST_LEGACY_ANTI_REGRESSION_LAWS: 5 Luật Thép Chống "Sửa Chỗ Này Hỏng Chỗ Khác"

1. **Cấm Vá Ngọn (No Band-Aid Fixes):**
   * Cấm bọc `try { ... } catch { }` rỗng làm nuốt lỗi và để lại trạng thái dở dang.
   * Cấm dùng `Task.Delay` / `Dispatcher.BeginInvoke` mò mẫm để che giấu lỗi xung đột thứ tự sự kiện khi chưa tìm ra nguyên nhân gốc (ví dụ: quên chú ý `SelectTool()` tự động gọi `ResetAllToolStates()` xóa sạch cờ vừa gán trước đó).
2. **Cấm Đẻ Thêm Cờ `bool` Mồ Côi:**
   * Không tự ý thêm biến `private bool _is...` mới vào `Form2_MainDashboard` nếu trạng thái đó có thể biểu diễn qua `ToolMode`, `TouchToolMode`, hoặc `SelectionMode`. Nếu bắt buộc phải thêm biến trạng thái phiên làm việc, **phải** đăng ký reset nó trong `DeactivateAllTools()` / `ResetAllToolStates()` và kiểm tra tương thích với `EnsurePassthroughFocusAndPenMode()`.
3. **Bảo Toàn Hợp Đồng Ngữ Nghĩa Hàm Dùng Chung:**
   * Không gộp chung hai mục đích trái ngược vào một hàm (ví dụ: `MainInkPresenter` là phần tử hệ thống trong `IsSystemElement()` để không bị xóa mất control khi `ClearAll`, nhưng là nội dung bài giảng bắt buộc phải hiển thị khi chụp ảnh/xuất PDF trong `CanvasExportService`).
4. **Ngừng Làm Phình To "God Files":**
   * Khi viết thuật toán mới, xử lý hình học, biến đổi dữ liệu hoặc dịch vụ mới (>50 dòng), phải ưu tiên tách thành class chuyên biệt sạch sẽ trong `WhiteboardCore/` hoặc `Services/`, sau đó gọi gọn từ Partial Class tương ứng; không nhồi thêm logic phức tạp trực tiếp vào `Form2_MainDashboard.Input.cs` (10.815 dòng).
5. **Phẫu Thuật Tối Thiểu (Minimal Surgical Diff):**
   * Chỉ can thiệp chính xác vào vùng hàm gây lỗi hoặc cần mở rộng. Tuyệt đối không tự ý format lại toàn bộ file hàng nghìn dòng, không đổi tên hàm/biến ổn định ngoài phạm vi yêu cầu, không xóa các đoạn guard bảo vệ cảm ứng đã có (`Stylus.IsFlicksEnabled="False"`, `IsDragHandleOrEmbeddedWidget`, v.v.).

---

## [RULE] QST_4_ENGINEERING_GATES: 4 Cổng Kiểm Soát Kỹ Thuật Bắt Buộc Trước Khi Sửa Code (Cấp độ 2)

Với mọi tác vụ thuộc **Cấp độ 2 (`Architect Gate`)**, AI phải kiểm tra đủ 4 cổng sau:

* **🚪 CỔNG 1 — Đồng bộ Luồng Đầu vào & Công cụ (`Input / Touch / Tool Sync`):**
  Chuyển đổi công cụ có đi qua `DeactivateAllTools()` $\rightarrow$ `SelectTool()` $\rightarrow$ rồi mới gán cờ chế độ riêng không? Đã đồng bộ `ToolManager.SwitchTo()`, `_touchHandler.SetToolMode()` + `ResetTouchState()`, nhả `MouseCapture`/`TouchCapture`, và chặn `EnsurePassthroughFocusAndPenMode` cướp quyền về Bút vẽ chưa?
* **🚪 CỔNG 2 — Đồng bộ Chọn vùng, QuadTree & Undo/Redo (`Selection & History Parity`):**
  Thay đổi này có hoạt động đồng nhất trên cả 6 loại phần tử (`Stroke` trên `MainInkPresenter`, `Shape`/`Polyline` 2D, `Canvas` có `Tag="3DShape"`, `Border` chứa Text, `Grid` Bảng/Biểu đồ, `Image`/Media/STEM) không? Sau khi biến đổi hoặc `Undo`/`Redo` (`ExecuteUndoAction`, `ExecuteRedoAction`, `ApplyModifyValue`), đã gọi `UpdateBounds()`, `_selectionBox?.UpdatePosition()`, `_selectionManager?.RebuildQuadTree()` và đăng ký/hủy đăng ký phần tử khỏi `_selectionManager` chưa?
* **🚪 CỔNG 3 — Toàn vẹn Lưu trữ, Xuất bản & Phần cứng (`Persistence, Export & Hardware Hygiene`):**
  Đối tượng có giữ nguyên vẹn khi chuyển trang bảng / Lưu-Mở `.qasc` (`BoardManager`) và Xuất PDF/PNG/JPEG (`CanvasExportService`) không? Nếu liên quan Camera/Ghi hình/WebView2, đã gắn `WDA_EXCLUDEFROMCAPTURE` cho thanh điều khiển, tự đóng cửa sổ lỗi phần cứng và dọn sạch `CleanupWebView2Media` khi xóa chưa?
* **🚪 CỔNG 4 — Tương thích Cảm ứng 86" & Liên kết Dự án (`Touch-First & Project Link`):**
  Mọi nút bấm/handle đều thao tác ăn ngay từ 1 chạm (không giấu sau `MouseEnter`/Hover, có `TouchActivationHelper` cho cửa sổ nổi, kẹp biên màn hình không tràn mép)? Nếu có tạo file mới, đã kiểm tra liên kết trong `QASmartTouch/QASmartTouch.csproj` chưa?

