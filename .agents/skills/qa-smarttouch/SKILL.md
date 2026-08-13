---
name: qa-smartclass-classroom
description: >-
  Skill chuyên biệt cho Form2_MainDashboard — bảng vẽ tương tác (Smart Touch whiteboard)
  của QA SmartClass, tại Forms/Form2_MainDashboard.xaml(.cs).
  Kích hoạt khi người dùng hỏi về: sửa lỗi bảng vẽ, thêm công cụ vẽ mới,
  xử lý Undo/Redo, đồng bộ nét vẽ lên màn hình học sinh, chế độ Focus/Spotlight,
  zoom/pan canvas, lưu bảng thành slide bài giảng, hoặc bất kỳ tính năng nào
  liên quan đến Form2_MainDashboard (F20).
---

# Form2_MainDashboard — Bảng vẽ Smart Touch (F20)

Đây là "runbook" chuyên sâu cho `Form2_MainDashboard`. Đọc kỹ trước khi chỉnh sửa bất kỳ dòng code nào.

> **🚨 RÀNG BUỘC QUAN TRỌNG:** KHÔNG ĐƯỢC tự động sửa đổi code hay file cấu hình. Bạn PHẢI giải thích rõ phương án, hiển thị code dự kiến thay đổi và yêu cầu người dùng REVIEW, xác nhận trước khi thực hiện bất kỳ hành động ghi file hay sửa đổi nào.

---

## 1. Vị trí file

| File | Đường dẫn |
|---|---|
| XAML | `Forms/Form2_MainDashboard.xaml` |
| Code-behind | `Forms/Form2_MainDashboard.xaml.cs` (~25000 dòng, 1.1 MB) |

---

## 2. Cấu trúc XAML — Canvas Layers (từ dưới lên)

```text
Grid (MainGrid)
├── Border (HeaderBorder) — Chứa Logo và tên App
└── Grid (Content Panel)
    ├── ScrollViewer (MainScrollViewer)
    │   └── Canvas (MainInteractiveBoard) — Bảng vẽ chính, xử lý MouseDown/Move/Up/Wheel. Nền mặc định #3D6D64.
    ├── Border (panelWelcomeState) — Hiển thị hướng dẫn khi chưa chọn công cụ
    └── Grid (Toolbar Overlay)
        └── StackPanel (panelTools) — Thanh công cụ nằm ngang ở dưới cùng
            ├── btn1_Pen (Bút)
            ├── btn2_Eraser (Tẩy)
            ├── btn3_Undo (Hoàn tác)
            ├── btn4_Redo (Làm lại)
            ├── btn5_Shapes (Hình học)
            ├── btn6_Inserts (Chèn)
            ├── btn7_Zoom (Phóng to)
            ├── btn8_Select (Chọn vùng)
            ├── btn9_BoardManagement (Công cụ bảng)
            └── btn10_WindowMode (Chế độ Desktop)
```

---

## 3. Hệ thống công cụ vẽ (`SelectTool`)

Khi chọn một công cụ (ví dụ: click `btn1_Pen_Click`), hệ thống gọi `SelectTool(Button toolButton)`.
Các bước xử lý trong `SelectTool`:
1. Hủy kích hoạt tất cả công cụ cũ (`DeactivateAllTools()`).
2. Tắt các chế độ (Drawing, Eraser, ObjectSelection...).
3. Reset màu nền của tất cả các nút công cụ trong `panelTools` về `#F1F2F6`.
4. Dựa vào Name của Button (vd: `btn1_Pen`), kích hoạt logic tương ứng (bật `_drawingEnabled`, v.v.).
5. Đổi màu nền của Button đang chọn sang `#2E86DE`.

> ⚠️ **Lưu ý UI:** Nếu gán cứng `toolButton.Background = activeBrush`, tính năng Hover trong Template (XAML) sẽ bị ghi đè. Cần cân nhắc cơ chế `ClearValue(BackgroundProperty)` hoặc sử dụng Trigger để UI không bị đơ/mờ, đồng thời phải thêm điều kiện chặn spam click nếu tool đó đã được chọn.

---

## 4. Hệ thống Undo/Redo

Sử dụng Command Pattern với `_undoStack` và `_redoStack`.
Các lệnh tương tự CanvasPage cũ (`AddStrokeCommand`, `ClearCanvasCommand`...).

---

## 5. Cảnh báo bảo mật tương tác (Passthrough Guard)

`Form2_MainDashboard` chứa nhiều tính năng nâng cao: cửa sổ nhúng (Google Search, YouTube), kéo rê (DragHandle), v.v.
Do đó, khi xử lý `MainInteractiveBoard_MouseDown`:
- Kiểm tra `IsDragHandleOrEmbeddedWidget`: Bỏ qua nếu click vào UI con.
- Nếu đang ở chế độ Pointer (`_isPointerModeActive`) hoặc Pan (`_isPanModeActive`): KHÔNG tự động bật bút vẽ.
- Method `EnsurePassthroughFocusAndPenMode`: Auto-Pen mode nếu thỏa mãn các điều kiện không vướng menu/widget.

---

## 6. Lưu ý màn hình đa thiết bị (SmartTouch 86")

- Khác với Laptop, màn hình SmartTouch dùng cảm ứng hồng ngoại.
- Hỗ trợ đa điểm.
- Luôn kiểm tra kỹ các sự kiện `TouchDown` so với `MouseDown` để tránh spam double click hoặc lỗi capture chuột.

---

## 7. Quy tắc lập trình

- Tuyệt đối bảo toàn Code hiện tại. `Form2_MainDashboard.xaml.cs` là một file rất lớn, không được dùng Replace All mù quáng.
- Xác định Line range chính xác khi Replace.
- Trình bày Code Block đề xuất và chờ User gật đầu trước khi thực thi.
