---
name: qasmarttouch-architecture
description: >-
  Bản đồ kiến trúc trung tâm và điều phối phân hệ cho phần mềm bảng viết tương tác QA Smart Touch
  (QASmartTouch.csproj / Form2_MainDashboard). Kích hoạt skill này khi thiết kế tính năng mới,
  đánh giá tác động toàn hệ thống, tra cứu vai trò của 13 tệp Partial Classes, kiến trúc bảng vẽ lai
  (MainInkPresenter + MainInteractiveBoard), vòng đời 8 bước của đối tượng, hoặc tra cứu các bẫy lịch sử
  trong mã nguồn kế thừa.
---

# Kiến trúc Trung tâm QA Smart Touch (`Form2_MainDashboard` & `WhiteboardCore`)

Skill này cung cấp bức tranh toàn cảnh về kiến trúc thực thi của **`QASmartTouch`** (`QASmartTouch/QASmartTouch.csproj`), giúp định vị chính xác tệp tin cần can thiệp và ngăn chặn lỗi dây chuyền giữa các phân hệ.

---

## 1. Kiến trúc Bảng Vẽ Lai 2 Lớp (`Hybrid Dual-Layer Canvas`)

Bảng vẽ trung tâm là `MainInteractiveBoard` (`System.Windows.Controls.Canvas` trong `QASmartClass/Forms/Form2_MainDashboard.xaml`), vận hành đồng thời **2 lớp nội dung** được phân tầng bởi `ZIndexConstants`:

1. **Tầng Nền (`BackgroundLayer`):**
   - Màu nền bảng và các phần tử lưới ô ly / dòng kẻ có `Tag = "BackgroundLayer"`.
2. **Tầng Mực Viết Tay Độ Trễ Thấp (`MainInkPresenter` — `System.Windows.Controls.InkPresenter`):**
   - Nằm bên trong `MainInteractiveBoard.Children`, tự đồng bộ kích thước theo `MainInteractiveBoard_SizeChanged`.
   - Quản lý toàn bộ nét bút tự do và dạ quang dưới dạng `System.Windows.Ink.Stroke` (`MainInkPresenter.Strokes`).
   - Hỗ trợ tẩy nét nguyên chiếc (`Stroke`) và tẩy cắt xén hình học theo điểm (`Point`) qua `NativeEraseSession`.
3. **Tầng Đối Tượng Đồ Họa & Học Cụ (`UIElement` trực tiếp trong `MainInteractiveBoard.Children`):**
   - Hình 2D (`Shape`, `Polyline`), Khối 3D (`Canvas` có `Tag = "3DShape"` chứa các `Path` nét liền & nét đứt), Khung chữ/Thẻ OCR (`Border` chứa `TextBlock`/`TextBox`), Bảng & Biểu đồ (`Grid`), Ảnh (`Image`), Khung nhúng đa phương tiện (`WebView2`/Video), và Học cụ Toán học STEM.
4. **Tầng Giao Diện Điều Khiển Hệ Thống (`ZIndexConstants.SystemUIBase` trở lên):**
   - `_selectionBox`, `_contextToolbar`, `_thicknessPicker`, `_colorPicker`, `_moreMenu`, `_floatingTouchKeyboard`, `_smartStatusBadge`, `panelWelcomeState`.

---

## 2. Bản đồ Điều phối 13 Partial Classes & Tầng Dịch Vụ

> **Nguyên tắc vàng:** Toàn bộ 13 tệp `Form2_MainDashboard.*.cs` biên dịch thành **chung một class C# (~31.000+ dòng)**. Tuyệt đối không đặt nhầm logic sang tệp khác vai trò và ưu tiên tách logic thuần túy mới (>50 dòng) sang `WhiteboardCore/` hoặc `Services/`.

| Miền Nghiệp vụ | Tệp tin Thực thi Chính (`QASmartClass/...`) | Trách nhiệm & Các Hàm Neo Cốt lõi |
| :--- | :--- | :--- |
| **Khởi tạo & Điều phối chung** | `Forms/Form2_MainDashboard.xaml(.cs)` (~3.166 dòng) | Khởi tạo cửa sổ, `ZIndex` (`AllocateStrokeZIndex`, `AllocateImageZIndex`), `ResetAllToolStates`, `SelectTool`, `OnWhiteboardToolChanged`, `RefreshSelectableObjects`, `RegisterNewObjectWithSelectionManager`. |
| **Toolbar, Undo/Redo & OCR** | `Forms/Form2_MainDashboard.Tools.cs` (~2.983 dòng) | Sự kiện nút Toolbar (`btn1_Pen_Click` ... `btn12_MoreExtended_Click`), mở SubMenu (`Form2_1`, `Form2_2`, `Form2_4`, `Form2_5`, `Form2_6`), `DeactivateAllTools`, `IsSystemElement`, hệ thống **Undo/Redo** (`_undoStack`, `ExecuteUndoAction`, `ExecuteRedoAction`, `ApplyModifyValue`), Đũa phép (`PerformMagicWandSelection`). |
| **Đầu vào Chuột/Cảm ứng/Bút/Tẩy** | `Forms/Form2_MainDashboard.Input.cs` (~10.815 dòng)<br>`Handlers/TouchHandler.cs`<br>`WhiteboardCore/Managers/ToolManager.cs` | **Điểm nóng #1:** Vẽ nét, tẩy nét, `EnsurePassthroughFocusAndPenMode`, `IsDragHandleOrEmbeddedWidget`, cảm ứng đa điểm, cử chỉ 2 ngón Pan/PinchZoom. *(Khi đụng vùng này, bắt buộc đọc thêm skill `qasmarttouch-input-touch-pen-eraser`)*. |
| **Chọn vùng & Biến đổi** | `Forms/Form2_MainDashboard.Selection.cs` (~2.320 dòng)<br>`Forms/Form2_MainDashboard.LassoSelection.cs`<br>`Services/SelectionManager.cs` & `TransformService.cs`<br>`Controls/SelectionBox.xaml(.cs)` & `ContextToolbar.xaml(.cs)` | **Điểm nóng #2:** Chọn đơn/nhóm/Lasso, QuadTree HitTest, kéo thả, Xoay 90°, Lật ngang/dọc, Resize, Đổi màu (`OnColorSelected`), Độ dày (`OnThicknessChanged`), Copy, Xóa. *(Khi đụng vùng này, bắt buộc đọc thêm skill `qasmarttouch-selection-transform-undo`)*. |
| **Hình học 2D & Khối 3D** | `Forms/Form2_MainDashboard.ShapeDrawing.cs` (~2.148 dòng)<br>`Forms/Form2_MainDashboard.3DModel.cs`<br>`Forms/Form2_5_SubMenuDrawShapes.xaml(.cs)` | Vẽ hình 2D, dựng khối 3D (`Finalize3DShapeDrawing`, `Create3DShapeContainer` tạo `Canvas` với `Tag="3DShape"`), nhập mô hình 3D. |
| **Học cụ STEM & Biểu đồ** | `Forms/Form2_MainDashboard.STEM.cs` (~2.030 dòng)<br>`Forms/Form2_4_SubMenuInsertContent.xaml(.cs)`<br>`Forms/Form2_8_*` $\rightarrow$ `Form2_18_*` | Thước thẳng, Thước đo độ, Ê-ke, Compa (vẽ nét đẩy qua `RecordToolDrawAction`), và 7 trình biên tập Biểu đồ Toán học. |
| **Ảnh, Video & WebView2** | `Forms/Form2_MainDashboard.MediaControls.cs` (~3.981 dòng)<br>`Forms/Form2_MainDashboard.ImageCanvas.cs` | Chèn/cắt ảnh, Video nội bộ, YouTube, PhET, Google Maps, Bảng dữ liệu, dọn tài nguyên `CleanupWebView2Media`. |
| **Đa trang bảng, Lưu/Xuất & Ghi hình** | `Forms/Form2_MainDashboard.BoardManagement.cs` (~1.728 dòng)<br>`Managers/BoardManager.cs`<br>`Services/CanvasExportService.cs` & `ScreenRecorderService.cs` | Chuyển trang bảng (1–10), chế độ Chia đôi bảng (Multi-User), Lưu/Mở `.qasc`, Xuất PDF/PNG/JPEG, In ấn, Quay video bài giảng & Camera nổi. *(Khi đụng vùng này, đọc thêm skill `qasmarttouch-board-export-media`)*. |
| **AI & Tiện ích mở rộng** | `Forms/Form2_MainDashboard.AI.cs`<br>`Forms/Form2_MainDashboard.AdvancedFeatures.cs` | Nhận diện chữ viết tay thông minh, trợ lý AI, tiện ích bảng nâng cao. |

---

## 3. Tài Liệu Tham Chiếu Chuyên Sâu (Đọc Theo Nhu Cầu)

* **Khi thêm hoặc sửa bất kỳ loại đối tượng hiển thị nào trên bảng:** Đọc [Ma trận 8 Bước Vòng đời Đối tượng](./references/object-lifecycle-matrix.md) để kiểm tra không bỏ sót mắt xích nào.
* **Trước khi sửa các hàm chuyển trạng thái, `Tag`, `ZIndex` hoặc xử lý lỗi lạ trong code cũ:** Đọc [Sổ đen Hợp đồng Trạng thái & Bẫy Lịch sử](./references/state-contracts-and-pitfalls.md).

---

## 4. Khuôn Mẫu Trình Bày Kế Hoạch (Dành Cho Tác Vụ Cấp Độ 2 — `Architect Gate`)

Khi xử lý yêu cầu thuộc Cấp độ 2 (đụng Logic / State / Input / Selection / Đa file), trình bày gọn và sắc nét theo 4 mục sau trước khi sửa code:

1. **🔍 Nguyên nhân gốc / Luồng kiến trúc:** Chỉ rõ hàm, dòng code và luồng sự kiện/trạng thái thực tế gây ra vấn đề.
2. **🛡️ Kiểm tra Tác động 4 Cổng:**
   - *Cổng 1 (Input/Touch/Tool Sync):* Ảnh hưởng gì tới Bút, Tẩy, Cảm ứng 1–2 ngón, `EnsurePassthroughFocusAndPenMode`?
   - *Cổng 2 (Selection/QuadTree/Undo):* Đồng bộ trên các loại phần tử, `RebuildQuadTree` và `Undo/Redo` ra sao?
   - *Cổng 3 (Save/Export/Media):* Tác động tới Lưu `.qasc`, Xuất PDF/Ảnh hoặc dọn tài nguyên ngầm?
   - *Cổng 4 (86" Touch & `QASmartTouch.csproj`):* Đảm bảo 1 chạm ăn ngay và kiểm tra Link file trong `QASmartTouch.csproj`.
3. **🛠️ Phương án can thiệp phẫu thuật (Surgical Plan):** Bảng danh sách file + hàm sẽ sửa (giữ thay đổi tối thiểu, không đụng vùng ổn định).
4. **⏸️ Xác nhận:** Chờ người dùng duyệt trước khi ghi file.

