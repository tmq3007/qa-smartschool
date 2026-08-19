---
name: qa-smarttouch-whiteboard-core
description: >-
  Kế hoạch tổng thể và Hướng dẫn cấu trúc (Master Plan & Architecture Guide) 
  cho việc Tái cấu trúc và Nâng cấp Lõi Bảng Vẽ QA SmartSchool bằng thuật toán 
  từ TouchBoard. Sử dụng skill này định hướng cho mọi thay đổi liên quan đến bảng vẽ.
---

# Kế hoạch Tổng thể & Kiến trúc Lõi Bảng Vẽ (Whiteboard Core)

Tài liệu này là bản lề định hướng cho việc nâng cấp toàn diện chức năng "Viết bảng, Tẩy, Chọn vùng" của QA SmartSchool (`Form2_MainDashboard`). Quá trình được chia làm 3 giai đoạn nhằm cách ly rủi ro và đảm bảo an toàn cho hệ thống.

## 1. Thiết kế Kiến trúc Thư mục mới (WhiteboardCore)

Toàn bộ mã nguồn liên quan đến tương tác bảng vẽ phải được di chuyển ra khỏi `Form2_MainDashboard.xaml.cs` và đặt vào thư mục `QASmartClass/WhiteboardCore/` theo chuẩn cấu trúc sau:

```text
QASmartClass/
└── WhiteboardCore/                   # Thư mục gốc chứa lõi bảng vẽ
    ├── Enums/
    │   └── ToolMode.cs               # Enum Trạng thái (Pen, Eraser, Select, Shape, Insert...)
    ├── Managers/
    │   ├── WhiteboardManager.cs      # Façade class: Cầu nối duy nhất giao tiếp với Form2
    │   ├── ToolManager.cs            # Quản lý việc chuyển đổi giữa các công cụ
    │   └── CanvasManager.cs          # Xử lý cấu hình UI (Zoom, Pan, Clear All, Background)
    ├── Input/
    │   └── MultiTouchManager.cs      # Độc quyền xử lý Touch, Stylus, Mouse, chống xung đột
    ├── Tools/
    │   ├── IWhiteboardTool.cs        # Interface bắt buộc cho mọi công cụ bảng
    │   ├── PenTool.cs                # Quản lý sự kiện vẽ
    │   ├── EraserTool.cs             # Quản lý sự kiện tẩy
    │   └── SelectionTool.cs          # Quản lý vùng chọn, kéo thả
    ├── History/
    │   └── UndoRedoManager.cs        # Cô lập lịch sử hoàn tác (Command Pattern)
    ├── UI/                           # Quản lý giao diện, Submenus, Toolbars (Partial classes)
    ├── STEM/                         # Quản lý các công cụ Toán học (Ruler, Compass, Protractor)
    ├── Shapes3D/                     # Quản lý khởi tạo và tương tác Khối 3D
    ├── IO/                           # Quản lý Save/Load bài giảng (.qasc), Export PDF/Image
    └── AI_OCR/                       # Tích hợp AI (Handwriting, Object Recognition, OCR)
```

## 2. Giai đoạn 1: Tái cấu trúc & Cách ly (Refactor & Isolate)

**Mục tiêu:** Đưa code về đúng thư mục, giải phóng `Form2` mà không làm thay đổi luồng xử lý thực tế.

- **Chuyển giao Event (Event Routing):** Dời toàn bộ logic `MouseDown`, `TouchDown`, `StylusDown` từ `Form2` sang `MultiTouchManager.cs`. Biến nó thành "người gác cổng" để chặn các xung đột cảm ứng. Cấm tuyệt đối việc gọi `CaptureMouse()` bừa bãi.
- **Đóng gói Logic (Encapsulation):** 
  - Di chuyển thuật toán `MoveSelectableObject` và HitTest thủ công hiện tại sang `SelectionTool.cs`. 
  - Mang thuật toán xóa sang `EraserTool.cs`.
- **Dọn dẹp (Clean up):** Xóa bỏ mã logic thô ở `Form2_MainDashboard.xaml.cs`. File `Form2` giờ chỉ còn nhiệm vụ UI Binding (click nút menu -> gọi `_whiteboardManager.ToolManager.SwitchTo(...)`).

## 3. Giai đoạn 2: Thay thế lõi bằng chuẩn TouchBoard (Engine Upgrade)

**Mục tiêu:** Khắc phục triệt để lỗi Tẩy trượt và Chọn vùng bị "Teleport" bằng cách sử dụng sức mạnh Native của WPF `InkCanvas`.

- **Khôi phục Native InkCanvas:** Đảm bảo lớp vẽ tự do (Strokes) tận dụng được tính năng `InkCanvas.Select()` và `EraseByStroke` gốc của hệ thống thay vì phải tự viết thuật toán tính toán va chạm (HitTest).
- **Can thiệp lai (Hybrid Selection):** 
  - Đối với nét chữ/vẽ tự do (`System.Windows.Ink.Stroke`): Giao cho Native WPF xử lý.
  - Đối với các widget STEM (Compa, Thước, Hình học, Ảnh): Vẫn dùng custom `SelectionBox` (khung viền xanh) từ `SelectionTool.cs` đã refactor ở Giai đoạn 1.

## 4. Giai đoạn 3: Tương thích dữ liệu (Data Migration)

**Mục tiêu:** Chuyển đổi dữ liệu và đảm bảo khả năng tương thích ngược của File bài giảng.

- Các nét vẽ cũ đang được lưu dưới dạng `Polyline` hoặc `Path` phải được Convert (chuyển đổi) sang chuẩn `System.Windows.Ink.Stroke` khi nạp (Load) file bài giảng cũ.
- Thiết lập cơ chế lưu (`Save`) mới ưu tiên định dạng `.isf` cho Strokes (để lưu giữ áp lực bút) hoặc Serialize mảng tọa độ điểm (StylusPoints) ra JSON.

## 5. Quy định Bắt buộc khi Code (Architecture Rules)

1. **Luật 1 (Input Separation):** Mọi sự kiện đầu vào **BẮT BUỘC** phải đi qua `MultiTouchManager`. Tuyệt đối cấm viết `CaptureMouse` rải rác ở code-behind.
2. **Luật 2 (Tool Delegation):** Logic xử lý tọa độ phải nằm gọn trong các class implement `IWhiteboardTool` (`PenTool`, `EraserTool`...). Các class này không được quyền can thiệp vào các Manager khác (như `CanvasManager`) ngoại trừ việc lấy thông tin hiển thị.
3. **Luật 3 (Regression Testing):** Bất cứ khi nào cập nhật code tại `WhiteboardCore`, lập trình viên (hoặc AI) bắt buộc phải kiểm thử lại 3 tính năng: Undo/Redo nét vẽ, Tẩy nét chạm nhiều ngón tay, và thao tác Zoom/Pan bảng vẽ.
