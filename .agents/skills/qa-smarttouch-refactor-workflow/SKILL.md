---
name: qa-smarttouch-refactor-workflow
description: >-
  Quy trình (Workflow) từng bước để bóc tách 25.000 dòng code của Form2_MainDashboard 
  ra thành các phân hệ (Partial classes & Modules) dễ quản lý.
---

# Quy trình Tái cấu trúc (Refactor Workflow) Toàn diện

Mục tiêu: Di dời toàn bộ **code logic bảng vẽ** (vẽ, tẩy, chọn vùng, undo/redo, touch,
STEM tools, AI...) từ file khổng lồ `Forms/Form2_MainDashboard.xaml.cs` vào thư mục
`QASmartClass/WhiteboardCore/` để giảm tải áp lực cho Form2 và dễ quản lý hơn.

## Nguyên tắc An toàn (Safety First)
> [!IMPORTANT]
> - Áp dụng **Strangler Fig Pattern**: Pha 1 tách Partial Class → Pha 2 rút logic sang WhiteboardCore.
> - Sau mỗi cụm di dời, **bắt buộc chạy `dotnet build`** để đảm bảo không lỗi syntax.
> - **KHÔNG** tự ý xóa code logic bên trong `Form2_MainDashboard.xaml.cs` trừ khi được xác nhận.
> - Sau khi di dời, thay block đã xóa bằng comment tham chiếu `// ── MOVED TO <file>.cs ──`.

---

## Cấu trúc thư mục đích (WhiteboardCore)

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

---

## Thiết kế Interfaces trước khi Code

### IWhiteboardContext — Tránh phụ thuộc ngược vào Form2
Khi chuyển code sang `WhiteboardCore/`, các Manager/Tool cần thao tác với Canvas và
Undo Stack nhưng **không được import/reference ngược lại Form2**. Giải pháp là định nghĩa
một Context Interface:

```csharp
// WhiteboardCore/Managers/IWhiteboardContext.cs
public interface IWhiteboardContext
{
    System.Windows.Controls.Canvas MainCanvas { get; }
    IUndoRedoManager History { get; }
    void InvalidateVisual();
}
```

`Form2_MainDashboard` sẽ implement interface này và truyền vào `WhiteboardManager`:
```csharp
// Trong Form2_MainDashboard.xaml.cs
_whiteboardManager = new WhiteboardManager(this as IWhiteboardContext);
```

---

## Lộ trình 3 Giai đoạn (Optimized Roadmap)

### 🔷 Giai đoạn 1: Hạ tầng & Chuẩn hóa
Xây dựng nền móng trước khi di dời bất kỳ dòng code nào.

**1.1 — Khởi tạo thư mục & Base Interfaces**
- Tạo cây thư mục `WhiteboardCore/` đầy đủ.
- Tạo: `ToolMode.cs`, `IWhiteboardTool.cs`, `IWhiteboardContext.cs`.
- Tạo: `WhiteboardManager.cs`, `ToolManager.cs`, `MultiTouchManager.cs` (boilerplate rỗng).
- Tạo: `PenTool.cs`, `EraserTool.cs`, `SelectionTool.cs` (boilerplate rỗng).

**1.2 — Dựng UndoRedoManager (Ưu tiên số 1)**
> Phải làm **trước** khi di dời PenTool/EraserTool vì tất cả đều phụ thuộc `RecordAction()`.
- Di dời toàn bộ class `UndoRedoAction` + `RecordAction`, `ExecuteUndoAction`, `ExecuteRedoAction` vào `WhiteboardCore/History/UndoRedoManager.cs`.
- Cập nhật Form2 gọi qua: `_whiteboardManager.History.RecordAdd(element)`.

---

### 🔷 Giai đoạn 2: Tách File bằng Partial Class (Giảm tải Form2 ngay lập tức)
Tách `Form2_MainDashboard.xaml.cs` thành nhiều file partial **trong cùng namespace/class**
để giảm ngay số dòng mà không phát sinh lỗi binding hay truy cập UI elements.

| File Partial | Chứa gì | #region trong Form2 gốc |
|---|---|---|
| `Form2_MainDashboard.Input.cs` | Touch, Mouse, Stylus event handlers | `#region Canvas Interaction Handlers` |
| `Form2_MainDashboard.Tools.cs` | Pen, Eraser, Undo/Redo click handlers | `#region Tool Click Handlers` |
| `Form2_MainDashboard.Selection.cs` | Lasso, MagicWand, SelectionBox handlers | `#region Object Selection Event Handlers` |
| `Form2_MainDashboard.STEM.cs` | Ruler, Compass, Chart draw methods | `#region Line/Pie/Area/Scatter/Radar Chart` |
| `Form2_MainDashboard.Shapes3D.cs` | 3D shape drag & drop | `#region 3D Shape Drag & Drop` |
| `Form2_MainDashboard.AI.cs` | OCR, Handwriting, Text operations | `#region Module 2.3/2.5, Sprint 4` |
| `Form2_MainDashboard.IO.cs` | ImportImage, ImportVideo, Screenshot | `ImportImage()`, `ImportVideo()` |
| `Form2_MainDashboard.MediaControls.cs` | PhET, Image, YouTube, GoogleMaps | `#region PhET/Image/YouTube/GoogleMaps` |

> ✅ Sau giai đoạn này: File `Form2_MainDashboard.xaml.cs` giảm từ 25.000 xuống ~3.000 dòng.

---

### 🔷 Giai đoạn 3: Rút ruột Logic sang WhiteboardCore
Từ các Partial Class, chuyển **Non-UI logic** (Business Logic thuần) sang `WhiteboardCore/`.
Event Handlers UI vẫn ở lại Partial Class, chỉ gọi sang Core thông qua `_whiteboardManager`.

**Ví dụ:**
```csharp
// Form2_MainDashboard.Tools.cs (UI Handler — ở lại)
private void btn1_Pen_Click(object sender, RoutedEventArgs e)
{
    _whiteboardManager.ToolManager.SwitchTo(ToolMode.Pen);
}

// WhiteboardCore/Tools/PenTool.cs (Business Logic — di chuyển vào)
public void Activate()
{
    _context.MainCanvas.DefaultDrawingAttributes.Color = _currentColor;
}
```

---

## Tiến độ Thực thi (Progress Checklist)

### Giai đoạn 1 — Hạ tầng ✅
- [x] **1.1** Khởi tạo Hạ tầng Thư mục (Scaffolding). ✅ Đã tạo đủ cây thư mục `WhiteboardCore/`.
- [x] **1.2a** Tạo Base Interfaces (`IWhiteboardTool`, `IWhiteboardContext`). ✅
- [x] **1.2b** Tạo Base Classes lõi (`ToolManager`, `MultiTouchManager`, `WhiteboardManager`). ✅
- [x] **1.3** Dựng `UndoRedoManager.cs` độc lập (`IUndoRedoManager` + implementation). ✅

### Giai đoạn 2 — Partial Class (Giảm tải Form2) ✅ **25.133 → 2.035 dòng**
- [x] **2.1** `Form2_MainDashboard.Tools.cs` — 2.314 lines (Tool Click Handlers).
- [x] **2.2** `Form2_MainDashboard.Input.cs` — 8.328 lines (Canvas Interaction Handlers).
- [x] **2.3** `Form2_MainDashboard.Selection.cs` — 1.805 lines (Object Selection).
- [x] **2.4** `Form2_MainDashboard.STEM.cs` — 2.082 lines (Charts + 3D Drag).
- [x] **2.5** `Form2_MainDashboard.ShapeDrawing.cs` — 2.421 lines (Shape Drawing + Path Helpers).
- [x] **2.6** `Form2_MainDashboard.AI.cs` — 1.759 lines (Text Ops + OCR + AI).
- [x] **2.7** `Form2_MainDashboard.AdvancedFeatures.cs` — 1.059 lines (Sprint 5).
- [x] **2.8** `Form2_MainDashboard.BoardManagement.cs` — 933 lines (Background, Pan, Pointer, MultiUser).
- [x] **2.9** `Form2_MainDashboard.MediaControls.cs` — 2.406 lines (PhET, Image, YouTube, Maps).

### Giai đoạn 3 — Rút Logic sang WhiteboardCore
- [x] **3.1** Chuyển Pen/Eraser logic → `WhiteboardCore/Tools/`. ✅ Tạo ToolMode enum, IWhiteboardTool, PenTool, EraserTool, ToolManager, WhiteboardManager Façade. Tích hợp vào Form2 constructor.
- [x] **3.2** Chuyển MultiTouch input → `WhiteboardCore/Input/`. ✅ Tạo IInputBridge interface + MultiTouchManager adapter. Gắn vào WhiteboardManager.InputManager.
- [x] **3.3** Chuyển Selection logic → `WhiteboardCore/Tools/SelectionTool.cs`. ✅ Tạo SelectionTool (Rectangle/Lasso/MagicWand modes). Đăng ký vào ToolManager.
- [x] **3.4** Chuyển STEM & Chart → `WhiteboardCore/STEM/`. ✅ Tạo IStemTool interface + StemManager scaffold.
- [x] **3.5** Chuyển 3D Shapes → `WhiteboardCore/Shapes3D/`. ✅ Tạo Shape3DManager scaffold.
- [x] **3.6** Chuyển AI/OCR → `WhiteboardCore/AI_OCR/`. ✅ Tạo AIOcrManager scaffold.
- [x] **3.7** Chuyển IO/Media → `WhiteboardCore/IO/`. ✅ Tạo IOManager scaffold.

> **Trạng thái GĐ3:** Kiến trúc WhiteboardCore hoàn chỉnh. Tools (Pen/Eraser/Selection) có logic cấu hình thuần.
> Các module STEM/3D/AI/IO ở dạng scaffold — sẵn sàng cho migration logic chi tiết khi cần.
> Build status: ✅ 0 Errors.



