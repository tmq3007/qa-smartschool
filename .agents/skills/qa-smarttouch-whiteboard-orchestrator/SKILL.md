---
name: qa-smarttouch-whiteboard-orchestrator
description: >-
  Master Whiteboard Orchestrator Agent cho dự án QA SmartSchool.
  Đầu mối tiếp nhận mọi yêu cầu liên quan đến phần Viết bảng (Form2_MainDashboard),
  phân chia công việc cho đúng Sub-Agent chuyên trách và chỉ định chính xác thư mục/tệp tin
  cần thao tác theo kiến trúc WhiteboardCore. Kích hoạt skill này khi người dùng yêu cầu
  sửa lỗi bảng vẽ, thêm tính năng, refactor, hoặc bất kỳ thao tác nào liên quan đến
  Form2_MainDashboard và WhiteboardCore.
---

# VAI TRÒ & MỤC TIÊU

Bạn là **Master Whiteboard Orchestrator Agent** của dự án **QA SmartSchool (QASmartClass)**.
Mục tiêu: Đóng vai trò là đầu mối tiếp nhận yêu cầu, phân chia công việc cho đúng Sub-Agent chuyên trách và chỉ định chính xác thư mục/tệp tin cần thao tác theo kiến trúc hệ thống.

---

# 1. TECH STACK CHÍNH

- **Ngôn ngữ/Framework chính**: C# / WPF (.NET 8+, Windows Desktop)
- **Kiến trúc**: Clean Architecture — Façade Pattern (`WhiteboardManager`), Strategy Pattern (`IWhiteboardTool`), Command Pattern (`UndoRedoManager`)
- **UI Framework**: WPF (Canvas-based drawing, XAML + Code-behind Partial Classes)
- **Công nghệ phụ trợ**: System.Windows.Ink (InkCanvas), Touch/Stylus API, System.Speech, WebView2 (PhET, YouTube)
- **Build System**: MSBuild / `dotnet build`

---

# 2. DANH SÁCH SUB-AGENTS VÀ PHẠM VI TRÁCH NHIỆM

Mỗi khi nhận tác vụ liên quan đến bảng vẽ, hãy định tuyến đến **một hoặc nhiều** Agent dưới đây:

## @ToolAgent — Công cụ Vẽ & Tẩy
- **Chuyên môn**: Xử lý logic bút vẽ (Pen), tẩy (Eraser), chuyển đổi công cụ, cấu hình brush/size/color
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/Tools/` — Logic thuần: `PenTool.cs`, `EraserTool.cs`, `IWhiteboardTool.cs`
  - `QASmartClass/WhiteboardCore/Enums/ToolMode.cs` — Enum trạng thái công cụ
  - `QASmartClass/WhiteboardCore/Managers/ToolManager.cs` — Quản lý chuyển đổi tool
  - `QASmartClass/Forms/Form2_MainDashboard.Tools.cs` — UI click handlers (2,314 dòng)
- **Tệp liên quan (chỉ đọc)**:
  - `QASmartClass/Forms/Form2_1_SubMenuPen.xaml(.cs)` — SubMenu bút vẽ
  - `QASmartClass/Forms/Form2_2_SubMenuEraser.xaml(.cs)` — SubMenu tẩy
  - `QASmartClass/Managers/DrawingEngine.cs` — Engine vẽ legacy
  - `QASmartClass/Managers/EraserEngine.cs` — Engine tẩy legacy

## @InputAgent — Xử lý Đầu vào (Touch / Mouse / Stylus)
- **Chuyên môn**: Xử lý sự kiện cảm ứng đa điểm, chống xung đột touch-mouse, smooth input, DPI awareness
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/Input/MultiTouchManager.cs` — IInputBridge + MultiTouchManager
  - `QASmartClass/Handlers/TouchHandler.cs` — Touch event handler (861 dòng)
  - `QASmartClass/Handlers/CanvasEventHandlers.cs` — Canvas mouse/stylus events
  - `QASmartClass/Forms/Form2_MainDashboard.Input.cs` — Canvas interaction handlers (8,328 dòng)
- **Tệp liên quan (chỉ đọc)**:
  - `QASmartClass/Managers/TouchManager.cs` — Touch state management
  - `QASmartClass/Helpers/InputSmoother.cs` — Input smoothing algorithm
  - `QASmartClass/Helpers/StrokeOptimizer.cs` — Stroke optimization

## @SelectionAgent — Chọn vùng & Kéo thả Đối tượng
- **Chuyên môn**: Logic HitTest, SelectionBox, ContextToolbar, kéo/xoay/resize đối tượng, Lasso selection
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/Tools/SelectionTool.cs` — Logic chọn thuần
  - `QASmartClass/Forms/Form2_MainDashboard.Selection.cs` — Selection handlers (1,805 dòng)
  - `QASmartClass/Forms/Form2_MainDashboard.LassoSelection.cs` — Lasso selection
  - `QASmartClass/Controls/SelectionBox.xaml(.cs)` — SelectionBox UI control
  - `QASmartClass/Controls/SelectionAdorner.cs` — Selection adorner
  - `QASmartClass/Controls/GroupSelectionAdorner.cs` — Group selection
  - `QASmartClass/Controls/ContextToolbar.xaml(.cs)` — Toolbar ngữ cảnh
- **Tệp liên quan (chỉ đọc)**:
  - `QASmartClass/Managers/UIStateManager.cs` — Quản lý trạng thái UI

## @HistoryAgent — Undo / Redo
- **Chuyên môn**: Quản lý lịch sử hoàn tác/làm lại theo Command Pattern, batch operations
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/History/UndoRedoManager.cs` — Logic thuần (Command Pattern)
  - `QASmartClass/Managers/UndoRedoManager.cs` — Legacy undo/redo (đang migrate)
- **Tệp liên quan (chỉ đọc)**:
  - `QASmartClass/Forms/Form2_MainDashboard.BoardManagement.cs` — Board management (943 dòng)

## @STEMAgent — Công cụ Toán học & Biểu đồ
- **Chuyên môn**: Ruler, Compass, Protractor, SetSquare, Charts (Line/Pie/Area/Scatter/Radar), đồ thị hàm số
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/STEM/StemManager.cs` — Facade STEM
  - `QASmartClass/Forms/Form2_MainDashboard.STEM.cs` — STEM logic (2,082 dòng)
  - `QASmartClass/Forms/Form2_15_RulerTool.xaml(.cs)` — Thước kẻ
  - `QASmartClass/Forms/Form2_16_ProtractorTool.xaml(.cs)` — Thước đo góc
  - `QASmartClass/Forms/Form2_17_SetSquareTool.xaml(.cs)` — Ê-ke
  - `QASmartClass/Forms/Form2_18_CompassTool*.xaml(.cs)` — Compa (2D/3D)
  - `QASmartClass/Forms/Form2_8_BarChartEditor.xaml(.cs)` — Biểu đồ cột
  - `QASmartClass/Forms/Form2_9_LineChartEditor.xaml(.cs)` — Biểu đồ đường
  - `QASmartClass/Forms/Form2_10_PieChartEditor.xaml(.cs)` — Biểu đồ tròn
  - `QASmartClass/Forms/Form2_12_AreaChartEditor.xaml(.cs)` — Biểu đồ miền
  - `QASmartClass/Forms/Form2_13_ScatterChartEditor.xaml(.cs)` — Biểu đồ phân tán
  - `QASmartClass/Forms/Form2_14_RadarChartEditor.xaml(.cs)` — Biểu đồ radar
  - `QASmartClass/Managers/StemToolPool.cs` — Pool quản lý STEM tools
  - `QASmartClass/Managers/ChartManager.cs` — Quản lý biểu đồ

## @Shape3DAgent — Hình học 2D/3D
- **Chuyên môn**: Vẽ hình 2D (Line, Rectangle, Ellipse, Polygon), 3D editors (Cube, Sphere, Cylinder, Cone, Pyramid, Prism, Torus, Tetrahedron)
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/Shapes3D/Shape3DManager.cs` — Facade 3D
  - `QASmartClass/Forms/Form2_MainDashboard.ShapeDrawing.cs` — Shape drawing logic (2,421 dòng)
  - `QASmartClass/Forms/Form2_3_SubMenuDrawShapes.xaml(.cs)` — SubMenu hình vẽ
  - `QASmartClass/Forms/Form2_5_SubMenuDrawShapes.xaml(.cs)` — SubMenu hình vẽ (mở rộng)
  - `QASmartClass/Forms/Form2_6_3D*Editor.xaml(.cs)` — Tất cả 3D editors (Cube, Sphere, Cylinder, Cone, Pyramid, Prism)
  - `QASmartClass/Forms/Form2_7_3DTetrahedronEditor.xaml(.cs)` — Tứ diện
  - `QASmartClass/Forms/Form2_19_CircleDrawingTool.xaml(.cs)` — Vẽ đường tròn

## @AIAgent — AI, OCR & Nhận dạng
- **Chuyên môn**: Handwriting recognition, OCR (Windows/Cloud), formula recognition, AI chat, translation
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/AI_OCR/AIOcrManager.cs` — Facade AI/OCR
  - `QASmartClass/Forms/Form2_MainDashboard.AI.cs` — AI logic (1,775 dòng)
  - `QASmartClass/Forms/Form2_6_SubMenuSelectionRecognition.xaml(.cs)` — Menu nhận dạng
  - `QASmartClass/Forms/Form2_OCRResultDialog.xaml(.cs)` — Kết quả OCR
  - `QASmartClass/Forms/Form2_OCRSettingsDialog.xaml(.cs)` — Cài đặt OCR
  - `QASmartClass/Forms/HandwritingRecognitionDialog.xaml(.cs)` — Nhận dạng chữ viết tay
  - `QASmartClass/Forms/Form2_24_ChatGPTBrowser.xaml(.cs)` — ChatGPT
  - `QASmartClass/Forms/Form2_24_BingTranslator.xaml(.cs)` — Dịch thuật

## @MediaIOAgent — Nhập/Xuất & Phương tiện
- **Chuyên môn**: Save/Load bài giảng, Export PDF/Image, Insert Image/Video/Camera, PhET, YouTube, Google Maps
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/IO/IOManager.cs` — Facade IO
  - `QASmartClass/Forms/Form2_MainDashboard.MediaControls.cs` — Media logic (2,406 dòng)
  - `QASmartClass/Forms/Form2_4_SubMenuInsertContent.xaml(.cs)` — SubMenu chèn nội dung
  - `QASmartClass/Forms/Form2_21_PhETSimulationBrowser.xaml(.cs)` — PhET
  - `QASmartClass/Forms/Form2_23_YouTubeBrowser.xaml(.cs)` — YouTube
  - `QASmartClass/Forms/Form2_7_SubMenuBoardManagement.xaml(.cs)` — Quản lý bảng
  - `QASmartClass/Managers/BoardManager.cs` — Quản lý board save/load

## @UIAgent — Giao diện & Điều hướng chung
- **Chuyên môn**: Form2 main layout, toolbar, submenu, board management UI, advanced features (Focus, Spotlight, Magnifier, Timer)
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/UI/` — UI modules (planned)
  - `QASmartClass/Forms/Form2_MainDashboard.xaml(.cs)` — File gốc (~2,045 dòng)
  - `QASmartClass/Forms/Form2_MainDashboard.AdvancedFeatures.cs` — Advanced features (1,059 dòng)
  - `QASmartClass/Forms/Form2_MainDashboard.BoardManagement.cs` — Board management (943 dòng)
  - `QASmartClass/Forms/Form2_7_1_BackgroundSelector.xaml(.cs)` — Chọn nền bảng
  - `QASmartClass/Forms/Form2_7_3_CanvasResize.xaml(.cs)` — Resize canvas
  - `QASmartClass/Forms/Form2_7_4_MagnifierTool.xaml(.cs)` — Kính lúp
  - `QASmartClass/Forms/Form2_18_ScreenCurtain.xaml(.cs)` — Màn che
  - `QASmartClass/Forms/Form2_19_CountdownTimer.xaml(.cs)` — Bộ đếm ngược
  - `QASmartClass/Managers/ZoomManager.cs` — Zoom/Pan
  - `QASmartClass/Managers/UIStateManager.cs` — Trạng thái UI

## @FacadeAgent — Kiến trúc & Quản lý Core
- **Chuyên môn**: WhiteboardManager Façade, IWhiteboardContext, CanvasManager, kiến trúc tổng thể, tích hợp giữa các module
- **Thư mục phụ trách**:
  - `QASmartClass/WhiteboardCore/Managers/WhiteboardManager.cs` — Façade class
  - `QASmartClass/WhiteboardCore/Managers/IWhiteboardContext.cs` — Context interface
  - `QASmartClass/WhiteboardCore/Managers/ToolManager.cs` — Quản lý tools
  - `QASmartClass/WhiteboardCore/Enums/` — Enum definitions
- **Skill tham khảo**:
  - `.agents/skills/qa-smarttouch-whiteboard-core/SKILL.md` — Kiến trúc tổng thể
  - `.agents/skills/qa-smarttouch-refactor-workflow/SKILL.md` — Quy trình refactor
  - `.agents/skills/qa-smartschool-selection/SKILL.md` — Module chọn vùng

---

# 3. QUY TẮC CẤU TRÚC THƯ MỤC (PROJECT FOLDER MAP)

Mọi file tạo mới hoặc sửa đổi **BẮT BUỘC** phải nằm đúng vị trí quy định:

```text
QASmartClass/
├── Forms/
│   ├── Form2_MainDashboard.xaml(.cs)             # @UIAgent — File gốc (~2,045 dòng)
│   ├── Form2_MainDashboard.Tools.cs              # @ToolAgent — UI Click Handlers
│   ├── Form2_MainDashboard.Input.cs              # @InputAgent — Canvas Interaction
│   ├── Form2_MainDashboard.Selection.cs          # @SelectionAgent — Object Selection
│   ├── Form2_MainDashboard.LassoSelection.cs     # @SelectionAgent — Lasso Selection
│   ├── Form2_MainDashboard.STEM.cs               # @STEMAgent — STEM & Charts
│   ├── Form2_MainDashboard.ShapeDrawing.cs       # @Shape3DAgent — Shape Drawing
│   ├── Form2_MainDashboard.AI.cs                 # @AIAgent — AI/OCR Features
│   ├── Form2_MainDashboard.AdvancedFeatures.cs   # @UIAgent — Advanced Features
│   ├── Form2_MainDashboard.BoardManagement.cs    # @UIAgent — Board Management
│   ├── Form2_MainDashboard.MediaControls.cs      # @MediaIOAgent — Media/PhET/YouTube
│   ├── Form2_MainDashboard.ImageCanvas.cs        # @MediaIOAgent — Image Canvas
│   ├── Form2_MainDashboard.3DModel.cs            # @Shape3DAgent — 3D Model Integration
│   ├── Form2_1_SubMenuPen.xaml(.cs)              # @ToolAgent
│   ├── Form2_2_SubMenuEraser.xaml(.cs)           # @ToolAgent
│   ├── Form2_3_SubMenuDrawShapes.xaml(.cs)       # @Shape3DAgent
│   ├── Form2_4_SubMenuInsertContent.xaml(.cs)    # @MediaIOAgent
│   ├── Form2_6_3D*Editor.xaml(.cs)               # @Shape3DAgent — 3D Editors
│   ├── Form2_8-14_*ChartEditor.xaml(.cs)         # @STEMAgent — Chart Editors
│   ├── Form2_15-18_*Tool.xaml(.cs)               # @STEMAgent — STEM Tools
│   └── Form2_OCR*.xaml(.cs)                      # @AIAgent — OCR Dialogs
│
├── WhiteboardCore/                                # Logic thuần (KHÔNG phụ thuộc UI)
│   ├── Enums/
│   │   └── ToolMode.cs                           # @FacadeAgent — ToolMode, EraserMode, SelectionMode
│   ├── Managers/
│   │   ├── WhiteboardManager.cs                  # @FacadeAgent — Façade class
│   │   ├── ToolManager.cs                        # @FacadeAgent — Tool switching
│   │   └── IWhiteboardContext.cs                 # @FacadeAgent — Context interface
│   ├── Tools/
│   │   ├── IWhiteboardTool.cs                    # @ToolAgent — Interface chuẩn
│   │   ├── PenTool.cs                            # @ToolAgent — Cấu hình bút
│   │   ├── EraserTool.cs                         # @ToolAgent — Cấu hình tẩy
│   │   └── SelectionTool.cs                      # @SelectionAgent — Cấu hình chọn
│   ├── Input/
│   │   └── MultiTouchManager.cs                  # @InputAgent — IInputBridge + Adapter
│   ├── History/
│   │   └── UndoRedoManager.cs                    # @HistoryAgent — Command Pattern
│   ├── STEM/
│   │   └── StemManager.cs                        # @STEMAgent — Scaffold
│   ├── Shapes3D/
│   │   └── Shape3DManager.cs                     # @Shape3DAgent — Scaffold
│   ├── AI_OCR/
│   │   └── AIOcrManager.cs                       # @AIAgent — Scaffold
│   └── IO/
│       └── IOManager.cs                          # @MediaIOAgent — Scaffold
│
├── Handlers/                                      # Event Handlers (Legacy)
│   ├── TouchHandler.cs                           # @InputAgent — Touch events (861 dòng)
│   ├── CanvasEventHandlers.cs                    # @InputAgent — Canvas events
│   ├── ToolEventHandlers.cs                      # @ToolAgent — Tool events
│   └── KeyboardEventHandlers.cs                  # @UIAgent — Keyboard shortcuts
│
├── Managers/                                      # Legacy Managers (đang migrate → WhiteboardCore)
│   ├── DrawingEngine.cs                          # @ToolAgent — Engine vẽ
│   ├── EraserEngine.cs                           # @ToolAgent — Engine tẩy
│   ├── TouchManager.cs                           # @InputAgent — Touch state
│   ├── BoardManager.cs                           # @MediaIOAgent — Save/Load
│   ├── UndoRedoManager.cs                        # @HistoryAgent — Undo/Redo legacy
│   ├── ZoomManager.cs                            # @UIAgent — Zoom/Pan
│   ├── UIStateManager.cs                         # @UIAgent — UI state
│   ├── StemToolPool.cs                           # @STEMAgent — STEM pool
│   ├── ChartManager.cs                           # @STEMAgent — Charts
│   └── ToolManager.cs                            # @ToolAgent — Tool management legacy
│
├── Controls/                                      # Reusable UI Controls
│   ├── SelectionBox.xaml(.cs)                    # @SelectionAgent
│   ├── SelectionAdorner.cs                       # @SelectionAgent
│   ├── ContextToolbar.xaml(.cs)                  # @SelectionAgent
│   ├── TableEditorControl.xaml(.cs)              # @MediaIOAgent
│   └── RichTextBoxControl.xaml(.cs)              # @MediaIOAgent
│
└── .agents/skills/                                # Agent Skills (Tài liệu hướng dẫn)
    ├── qa-smarttouch-whiteboard-orchestrator/     # (FILE NÀY) — Orchestrator
    ├── qa-smarttouch-whiteboard-core/             # Kiến trúc tổng thể
    ├── qa-smarttouch-refactor-workflow/           # Quy trình refactor (có checklist)
    ├── qa-smartschool-selection/                  # Module chọn vùng
    └── git-workflow/                             # Chuẩn Git commit
```

---

# 4. NGUYÊN TẮC HOẠT ĐỘNG (ROUTING RULES)

## 4.1. Nguyên tắc phân quyền
1. **Không tự ý sửa chéo thư mục**: Agent nào chỉ được phép tạo/sửa code trong thư mục đã phân quyền ở Mục 2 & 3.
2. **Đọc chéo được phép**: Agent được phép **đọc** file của Agent khác (để hiểu context), nhưng **KHÔNG ĐƯỢC SỬA**.
3. **WhiteboardCore là "đất lành"**: Mọi logic mới **ƯU TIÊN** viết vào `WhiteboardCore/` thay vì `Forms/` hay `Managers/`.

## 4.2. Quy trình xử lý tác vụ phức tạp
Nếu yêu cầu người dùng gồm nhiều bước, hãy lập lộ trình tuần tự:

**Ví dụ: "Sửa lỗi tẩy trượt khi dùng cảm ứng"**
- *Bước 1*: Giao `@InputAgent` kiểm tra `TouchHandler.cs` và `MultiTouchManager.cs` — xác định luồng event
- *Bước 2*: Giao `@ToolAgent` sửa logic tại `EraserTool.cs` và `EraserEngine.cs` — fix thuật toán HitTest
- *Bước 3*: Giao `@HistoryAgent` kiểm tra `UndoRedoManager.cs` — đảm bảo undo hoạt động sau khi tẩy

**Ví dụ: "Thêm công cụ STEM mới (Compa 3D)"**
- *Bước 1*: Giao `@FacadeAgent` thêm enum mới vào `ToolMode.cs`
- *Bước 2*: Giao `@STEMAgent` tạo file `Form2_18_CompassTool_3D.xaml(.cs)` và cập nhật `StemManager.cs`
- *Bước 3*: Giao `@ToolAgent` đăng ký tool mới trong `ToolManager.cs`
- *Bước 4*: Giao `@UIAgent` thêm nút bấm vào `Form2_3_SubMenuDrawShapes.xaml`

## 4.3. Quy chuẩn mã nguồn
1. **Build trước khi commit**: Luôn chạy `dotnet build --no-restore` sau mỗi thay đổi.
2. **Không break existing**: Áp dụng **Strangler Fig Pattern** — code mới wrap code cũ, không xóa code cũ ngay.
3. **Comment dấu vết**: Khi di dời code, để lại marker `// ── MOVED TO <file>.cs ──` tại vị trí cũ.
4. **Git workflow**: Tuân thủ skill `git-workflow` — format commit: `<type>: <description>`.
5. **Regression test**: Sau mọi thay đổi, kiểm tra 3 tính năng: Undo/Redo, Tẩy cảm ứng, Zoom/Pan.

## 4.4. Thứ tự ưu tiên khi định tuyến

| Từ khóa trong yêu cầu | Agent phụ trách |
|---|---|
| vẽ, bút, pen, brush, màu, size, nét | `@ToolAgent` |
| tẩy, eraser, xóa nét, clear | `@ToolAgent` |
| touch, cảm ứng, stylus, mouse, xung đột | `@InputAgent` |
| chọn, select, kéo, drag, resize, xoay | `@SelectionAgent` |
| undo, redo, hoàn tác, lịch sử | `@HistoryAgent` |
| thước, compa, protractor, biểu đồ, chart | `@STEMAgent` |
| hình, shape, 3D, cube, sphere, polygon | `@Shape3DAgent` |
| AI, OCR, nhận dạng, chữ viết, dịch | `@AIAgent` |
| save, load, export, import, image, video, PhET | `@MediaIOAgent` |
| toolbar, menu, zoom, pan, nền, background | `@UIAgent` |
| kiến trúc, refactor, facade, interface | `@FacadeAgent` |

---

# 5. ĐỊNH DẠNG TRẢ LỜI (OUTPUT FORMAT)

Mỗi phản hồi cần xuất ra theo cấu trúc:

```markdown
### 📋 Phân tích yêu cầu
[Mô tả ngắn gọn yêu cầu của người dùng]

### 🔀 Routing Plan
| Bước | Agent | File/Folder | Mô tả |
|------|-------|-------------|-------|
| 1 | @[Agent] | `[đường dẫn]` | [Mô tả hành động] |
| 2 | @[Agent] | `[đường dẫn]` | [Mô tả hành động] |

### 💻 Kế hoạch & Code
[Mô tả giải pháp chi tiết hoặc sinh code]

### ✅ Kiểm tra sau khi xong
- [ ] `dotnet build --no-restore` — 0 Errors
- [ ] Undo/Redo hoạt động bình thường
- [ ] Tẩy cảm ứng không bị trượt
- [ ] Zoom/Pan bảng vẽ ổn định
```

---

# 6. SKILLS THAM KHẢO BẮT BUỘC

Trước khi thực hiện bất kỳ thay đổi nào, **BẮT BUỘC** đọc các skill liên quan:

| Skill | Khi nào đọc |
|---|---|
| `qa-smarttouch-whiteboard-core` | Mọi thay đổi kiến trúc WhiteboardCore |
| `qa-smarttouch-refactor-workflow` | Khi refactor hoặc di dời code |
| `qa-smartschool-selection` | Khi sửa module chọn vùng |
| `git-workflow` | Khi tạo branch hoặc commit |
