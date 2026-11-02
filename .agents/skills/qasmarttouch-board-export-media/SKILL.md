---
name: qasmarttouch-board-export-media
description: >-
  Chuyên gia quản lý Đa trang bảng (BoardManager 1-10 trang), Lưu và Mở tệp bài giảng (.qasc),
  Xuất bản PDF / PNG / JPEG & In ấn (CanvasExportService), Ghi hình bài giảng (ScreenRecorderService),
  Camera nổi (FloatingCameraWindow) và Nội dung nhúng WebView2 trong QASmartTouch. Kích hoạt skill này
  khi làm việc với lưu/mở file, chuyển trang bảng, xuất ảnh/PDF, quay video, camera, micro hoặc âm thanh.
---

# Chuyên Gia Đa Trang Bảng, Lưu/Xuất Bài Giảng & Phần Cứng Đa Phương Tiện

Skill này đảm bảo mọi tính năng lưu trữ bài giảng, xuất bản tài liệu và tương tác phần cứng ngoại vi (Camera, Microphone, Ghi hình, `WebView2`) của **`QASmartTouch`** hoạt động bền bỉ, an toàn trong môi trường lớp học thực tế.

---

## 1. Bản Đồ Tệp Tin Thực Thi Chính

* **Quản lý Đa trang bảng & Tệp `.qasc`:**
  * `QASmartClass/Managers/BoardManager.cs` — Lưu trạng thái từng trang (`BoardState`, tối đa 10 trang), chuyển trang (`BeforeBoardSaved`, `AfterBoardLoaded`), tuần tự hóa Lưu/Mở tệp `.qasc`.
  * `QASmartClass/Forms/Form2_MainDashboard.BoardManagement.cs` — Giao diện quản lý trang bảng, đổi màu nền / dòng kẻ / ô ly (`BackgroundLayer`), chế độ chia đôi bảng Multi-User.
* **Xuất PDF / PNG / JPEG & In ấn:**
  * `QASmartClass/Services/CanvasExportService.cs` — `CaptureCanvasToImageBytes`, xuất PDF (`QuestPDF`), PNG, JPEG, Print.
  * `QASmartClass/Forms/Form2_20_ExportLectureDialog.xaml(.cs)` & `Form2_21_ShareLectureDialog.xaml(.cs)`.
* **Ghi hình Bài giảng & Camera Nổi:**
  * `QASmartClass/Forms/PreRecordSetupDialog.xaml(.cs)`, `CountdownOverlay.xaml(.cs)`, `FloatingRecorderBar.xaml(.cs)`, `FloatingCameraWindow.xaml(.cs)`.
  * `QASmartClass/Services/ScreenRecorderService.cs`, `Services/Camera/CameraDiscoveryService.cs`, `Services/Camera/CameraPreviewService.cs`.
* **Đa phương tiện nhúng (`WebView2`, Video, Ảnh):**
  * `QASmartClass/Forms/Form2_MainDashboard.MediaControls.cs` & `Form2_MainDashboard.ImageCanvas.cs`.

---

## 2. Ba Quy Tắc Bất Biến Về Lưu Trữ & Xuất Bản (`Board & Export Invariants`)

1. **Bảo Toàn Tuyệt Đối `BackgroundLayer` và `MainInkPresenter` Khi Xuất PDF/Ảnh:**
   * Trong `CanvasExportService.CaptureCanvasToImageBytes()`, hàm tạm ẩn các điều khiển hệ thống (`_selectionBox`, `_contextToolbar`, HUD...) dựa trên `isSystemElementPredicate`.
   * **BẮT BUỘC giữ nguyên 2 ngoại lệ sống còn:**
     * Không bao giờ ẩn phần tử có `Tag == "BackgroundLayer"` (để giữ màu nền bảng xanh/trắng/đen và lưới ô ly).
     * Không bao giờ ẩn `System.Windows.Controls.InkPresenter` (`MainInkPresenter`) — dù `IsSystemElement()` trả về `true` cho nó để bảo vệ khỏi `ClearAll`, nó chứa toàn bộ nét chữ viết tay của giáo viên!
   * Luôn đặt khối khôi phục `Visibility` gốc của các phần tử đã tạm ẩn bên trong `finally { ... }` để nếu xuất file gặp lỗi ngoại lệ, giao diện bảng vẽ không bị mất thanh điều khiển.

2. **Đồng Bộ Khi Chuyển Trang Bảng (`SwitchBoard`) & Nạp File `.qasc`:**
   * Trước khi chuyển trang hoặc lưu file (`BeforeBoardSaved`): phải chốt mọi thao tác đang dở (bỏ chọn `_selectionManager?.DeselectAll()`, đóng TextBox đang gõ dở nếu có).
   * Sau khi nạp trang mới hoặc mở file `.qasc` (`AfterBoardLoaded`): phải đảm bảo `MainInkPresenter` tồn tại đúng kích thước trong `MainInteractiveBoard`, khôi phục `BackgroundLayer`, và gọi `RefreshSelectableObjects()` + `_selectionManager?.RebuildQuadTree()` để mọi phần tử trên trang vừa mở đều chọn/tương tác được ngay.

3. **Dọn Sạch Tiến Trình `WebView2` & Media (`CleanupWebView2Media`):**
   * Các khung YouTube, PhET, Google Maps hoặc Video nội bộ sử dụng tài nguyên không tự giải phóng khi chỉ gọi `Canvas.Children.Remove(...)`.
   * Khi xóa đối tượng, xóa trang bảng, `ClearAll`, hoặc đóng ứng dụng, phải gọi `CleanupWebView2Media` để dừng hẳn âm thanh/video phát ngầm và giải phóng bộ nhớ `WebView2`.

---

## 3. Ba Quy Tắc Phòng Vệ Phần Cứng & Lớp Phủ Quay Video (`Hardware & Overlay Fail-Safe`)

1. **Zero Fake Hardware (Cấm Tạo Thiết Bị Giả):**
   * Khi máy tính/màn hình tương tác không cắm Camera hoặc Microphone (hoặc thiết bị đang bị ứng dụng khác chiếm khóa độc quyền), giao diện thiết lập (`PreRecordSetupDialog`, Camera Settings...) phải hiển thị trung thực *"Không tìm thấy thiết bị"* và khóa (`IsEnabled = false`) công tắc bật thiết bị đó. Tuyệt đối không sinh mục thiết bị giả/mock làm người dùng tưởng đang ghi được tiếng/hình.

2. **Vệ Sinh Lớp Phủ Khi Quay Màn Hình (`Overlay Hygiene`):**
   * Các thanh điều khiển hệ thống phục vụ quay phim (như `FloatingRecorderBar`) phải gắn cờ Win32 `SetWindowDisplayAffinity(..., WDA_EXCLUDEFROMCAPTURE)` để không bị thu dính vào video bài giảng của giáo viên.
   * Ngược lại, cửa sổ nội dung như `FloatingCameraWindow` (hình giáo viên) nếu gặp lỗi kết nối/rút cáp Camera đột ngột thì phải **tự đóng cửa sổ ngay lập tức** và thông báo gọn nhẹ, tuyệt đối không để khung đen/khung báo lỗi nằm chết trên bảng viết bị quay vào video bài giảng.

3. **Thu Hồi Luồng Ngầm Xác Định (`Deterministic Async Teardown`):**
   * Mọi luồng đọc frame Camera (`OpenCvSharp4`), thu âm (`NAudio`), hoặc ghi hình (`ScreenRecorderLib`) phải có `CancellationToken`, chờ `await StopAsync()` hoàn tất ghi header file MP4 rồi mới `Dispose()`, tránh làm hỏng (corrupt) file video vừa quay hoặc treo tiến trình `QASmartTouch.exe` ngầm sau khi thoát.

