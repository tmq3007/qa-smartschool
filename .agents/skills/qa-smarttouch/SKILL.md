---
name: qa-smartclass-classroom
description: >-
  Skill chuyên biệt cho CanvasPage — bảng vẽ tương tác (Smart Touch whiteboard)
  của QA SmartClass, tại Classroom/Views/CanvasPage.xaml(.cs).
  Kích hoạt khi người dùng hỏi về: sửa lỗi bảng vẽ, thêm công cụ vẽ mới,
  xử lý Undo/Redo, đồng bộ nét vẽ lên màn hình học sinh, chế độ Focus/Spotlight,
  zoom/pan canvas, lưu bảng thành slide bài giảng, hoặc bất kỳ tính năng nào
  liên quan đến CanvasPage (F20).
---

# CanvasPage — Bảng vẽ Smart Touch (F20)

Đây là "runbook" chuyên sâu cho `CanvasPage`. Đọc kỹ trước khi chỉnh sửa bất kỳ dòng code nào.

---

## 1. Vị trí file

| File | Đường dẫn |
|---|---|
| XAML | `Classroom/Views/CanvasPage.xaml` |
| Code-behind | `Classroom/Views/CanvasPage.xaml.cs` (~2007 dòng, 69 KB) |
| Backup (tham chiếu cũ) | `CanvasPage.xaml.bak`, `CanvasPage.xaml.cs.bak`, `CanvasPage.xaml.cs.bak2` |

---

## 2. Cấu trúc XAML — Canvas Layers (từ dưới lên)

```
Grid (canvasArea) — ClipToBounds, xử lý MouseDown/Move/Up/Wheel
└── Grid (zoomContainer) — RenderTransform: ScaleTransform + TranslateTransform
    ├── Canvas (bgPatternCanvas)      — Nền: grid/dots/lines, IsHitTestVisible=False
    ├── AdornerDecorator
    │   └── InkCanvas (drawCanvas)    — Bút vẽ tay / Tẩy mực (InkCanvas WPF)
    ├── Canvas (shapeCanvas)          — Hình vẽ: Rectangle, Ellipse, Line, Arrow, TextBlock
    └── ScrollViewer (lessonScroller)
        └── StackPanel (lessonOverlay)— Overlay bài giảng (ảnh + text từ DB)
├── Canvas (focusOverlay)             — Chế độ Focus (ô tối phủ màn hình)
├── Canvas (spotlightOverlay)         — Đèn chiếu Spotlight
├── Border (flashOverlay)             — Hiệu ứng chớp khi chụp ảnh
├── Canvas (shapePreviewCanvas)       — Preview hình đang vẽ (nét đứt)
├── StackPanel (blockNav)             — Điều hướng block bài giảng (⬆/⬇)
├── Border (zoomBadge)               — Hiển thị mức zoom hiện tại
├── Border (reactionCounterPanel)    — Đếm phản hồi 👍/😕 từ học sinh
├── Border (toolHint)                 — Gợi ý công cụ (auto-fade)
└── Canvas (floatingReactionCanvas)  — Emoji bay lên khi HS phản hồi
```

---

## 3. Danh sách công cụ vẽ (`_currentTool`)

| Tag | Tên | InkCanvas Mode | Ghi chú |
|---|---|---|---|
| `"Pen"` | ✏️ Bút | `Ink` | Default. Width/Height theo `strokeSlider` |
| `"Highlight"` | 🖌️ Highlight | `Ink` | IsHighlighter=true, màu vàng ARGB(128,255,255,0), W=20 H=10 |
| `"Eraser"` | 🧹 Tẩy | `EraseByStroke` | Tẩy mực trên `drawCanvas` + **ENGINE B** tẩy shape trên `shapeCanvas` |
| `"Select"` | ⬚ Chọn | `Select` | Chọn và di chuyển nét |
| `"Shape"` | ▭ HCN | `None` | Kéo chuột → Rectangle trên `shapeCanvas` |
| `"Ellipse"` | ⬭ Elip | `None` | Kéo chuột → Ellipse trên `shapeCanvas` |
| `"Line"` | ╱ Thẳng | `None` | Kéo chuột → Line trên `shapeCanvas` |
| `"Arrow"` | → Mũi tên | `None` | Kéo chuột → `GeometryGroup` (line + 2 arrowheads) |
| `"Text"` | T Chữ | `None` | Click → TextBox inline → Enter commit → TextBlock trên `shapeCanvas` |

> **Quan trọng:** Khi tool là `Shape/Ellipse/Line/Arrow/Text`, **`drawCanvas.IsHitTestVisible = false`** để chuột đi xuyên qua InkCanvas đến `canvasArea` xử lý shape drawing.

---

## 4. Hệ thống Undo/Redo (Command Pattern)

### Interface & Commands (namespace `QASmartClass.Services.Canvas`)
| Command | Tác dụng |
|---|---|
| `AddStrokeCommand` | Thêm 1 nét mực vào `drawCanvas` |
| `EraseStrokesCommand` | Xóa 1 hoặc nhiều nét mực |
| `AddShapeCommand` | Thêm shape (Rectangle/Ellipse/Line/Path) vào `shapeCanvas` |
| `AddTextCommand` | Thêm TextBlock vào `shapeCanvas` |
| `DeleteElementsCommand` | Xóa shape/text khỏi `shapeCanvas` (Eraser ENGINE B) |
| `ClearCanvasCommand` | Xóa toàn bộ `drawCanvas.Strokes` + `shapeCanvas.Children` |

### Flow
```
Người dùng vẽ/xóa
    → PushCommand(cmd)  →  _undoStack
Ctrl+Z → Undo_Click()  →  cmd.Unexecute()  →  _redoStack
Ctrl+Y → Redo_Click()  →  cmd.Execute()    →  _undoStack
```

### Giới hạn Undo
- Default: **100 bước**
- Cấu hình: `%LOCALAPPDATA%\QASmartClass\Settings\board_settings.json` → `"MaxUndoSteps": N`
- Range cho phép: 10–500

### Cờ quan trọng: `_isUndoRedo`
```csharp
_isUndoRedo = true;
// thực hiện Undo/Redo
_isUndoRedo = false;
```
`StrokeCollected` và `StrokeErased` kiểm tra `_isUndoRedo` để **không PushCommand** khi đang Undo/Redo.

---

## 5. ENGINE B — Eraser cho Shapes

Khi tool = `"Eraser"`, tẩy mực hoạt động tự động qua `InkCanvas`. Nhưng để tẩy **shape** trên `shapeCanvas`, ENGINE B hoạt động độc lập qua `PreviewMouse*` và `PreviewStylus*` events.

```
Constants:
  SHAPE_ERASER_RADIUS = 15.0 px  (bán kính hit detection)
  ERASER_MOVE_THRESHOLD = 5.0 px (tối thiểu di chuyển mới trigger)

Flow:
  PreviewMouseDown/StylusDown → EraseShapesAtPoint(pos) + clear _erasedElementsInCurrentStroke
  PreviewMouseMove/StylusMove → EraseShapesAtPoint(pos) nếu đủ threshold
  PreviewMouseUp/StylusUp     → PushCommand(DeleteElementsCommand) cho toàn bộ đã xóa trong stroke
```

### `IsCloseToShape()` — Hit detection theo loại shape
| Shape | Thuật toán |
|---|---|
| `Line` | `DistanceToSegment()` ≤ 15px |
| `Path` (Arrow) | Kiểm tra từng `LineGeometry` trong `GeometryGroup` |
| `Rectangle` | Khoảng cách đến 4 cạnh (không phải bên trong) |
| `Ellipse` | Khoảng cách đến đường biên elip (không phải bên trong) |
| Khác (TextBlock) | `Rect.IntersectsWith(bounds)` |

---

## 6. Zoom & Pan

### Zoom
| Cách | Shortcut / Event |
|---|---|
| Ctrl + Scroll | `CanvasArea_MouseWheel` |
| Nút 🔍+ / 🔍− | `ZoomIn_Click` / `ZoomOut_Click` (bước 0.15) |
| Nút 1:1 | `ZoomReset_Click` (về 1.0, reset pan) |
| Ctrl++ / Ctrl+- | `_windowKeyDownHandler` (bước 0.15) |

- Range: **0.5x → 3.0x**
- Animation: `CubicEase.EaseOut`, 200ms
- Transform: `zoomTransform (ScaleTransform)` + `panTransform (TranslateTransform)`

### Pan (Kéo canvas)
- Kích hoạt: giữ **Space** (toggle `_panMode`) hoặc nút ✋ hoặc Middle Click
- Cũng kích hoạt nếu zoom > 1.0 + Alt giữ
- Giới hạn pan: `Math.Max(3000, canvasWidth * (zoom-1)/2)` theo mỗi chiều

---

## 7. Đồng bộ lên màn hình học sinh (`CMD|WHITEBOARD_*`)

### Khi vẽ nét mới (StrokeCollected)
```csharp
string cmd = $"CMD|WHITEBOARD_DRAW|{color}|{strokeWidth}|{points}|{canvasW}|{canvasH}";
// points: "x1.0,y1.0;x2.0,y2.0;..."
app.NetworkService.SendCommandAsync(cmd);
```

### Khi xóa nét (Undo)
```csharp
// Gọi sau Undo/Redo để sync toàn bộ trạng thái
SyncStrokesToStudents();
```

### Khi xóa toàn bộ bảng (ClearCanvas)
```csharp
app.NetworkService.SendCommandAsync("CMD|WHITEBOARD_CLEAR|ALL");
```

> **Điều kiện:** Chỉ gửi khi `app.NetworkService.IsBroadcasting == true`. Không gửi khi chưa bắt đầu phát.

---

## 8. Nhận phản hồi học sinh (Reaction)

CanvasPage subscribe `NetworkService.MessageReceived` khi `Loaded`, unsubscribe khi `Unloaded`.

| Tin nhắn HS gửi | Hành động |
|---|---|
| `CHAT|...|...|👍 Đã hiểu` | `_reactionUnderstandCount++`, cập nhật progress bar, trigger emoji 👍 bay |
| `CHAT|...|...|😕 Chưa hiểu` | `_reactionConfusedCount++`, trigger emoji 😕 bay |

### Thanh tỷ lệ hiểu bài
- `prgUnderstandRate` (ProgressBar, 0–100)
- `txtUnderstandPercent` (TextBlock %)
- Công thức: `understand / (understand + confused) * 100`
- Hiển thị trên toolbar, chỉ visible khi `_lessonId > 0`

---

## 9. Nền bảng (`_bgMode`)

| `_bgMode` | Tên | Render |
|---|---|---|
| 0 | ⊟ Trơn | Không vẽ gì, nền `#0F3460` |
| 1 | ⊞ Ô vuông | Grid dọc + ngang, spacing 40px, ARGB(30,255,255,255) |
| 2 | ⁙ Chấm | Lưới chấm, spacing 30px, dot 3px, ARGB(40,255,255,255) |
| 3 | ☰ Kẻ ngang | Chỉ đường ngang, spacing 40px, ARGB(25,255,255,255) |

Vùng render: **2000×1400px** (hardcoded). Cycle qua nút `⊞/⁙/☰/⊟`.

---

## 10. Overlay bài giảng (Lesson Overlay)

Khi `CanvasPage` được khởi tạo với `lessonId > 0`:
- Load `LessonContents` từ DB (EF Core) theo `SortOrder`
- Render từng content block (`Text`, `Image`, `Header`) vào `lessonOverlay` (StackPanel)
- Navigation: ⬆️ `PrevBlock_Click` / ⬇️ `NextBlock_Click` — scroll `lessonScroller`
- `blockNav` visible khi có lesson, `reactionCounterPanel` visible

---

## 11. Lưu bảng

| Chức năng | Button | Hành động |
|---|---|---|
| 💾 Lưu | `SaveCanvas_Click` | Dialog chọn `.isf` (Ink) hoặc `.png` |
| 💾 Lưu Slide | `SaveCanvasAsSlide_Click` | Render → PNG → lưu vào `AppPaths.PicturesDir` → thêm `LessonContent` vào DB |
| 📂 Mở | `LoadCanvas_Click` | Load `.isf` vào `drawCanvas.Strokes`, clear Undo/Redo |
| 📷 Chụp | `Screenshot_Click` | PNG → `Pictures\QASmartClass\BangVe_*.png` + flash animation |
| 🗑 Xóa | `ClearCanvas_Click` | Confirm dialog → `ClearCanvasCommand` + sync network |

### PNG Export (`SaveAsPng`)
```csharp
// Render toàn bộ canvasArea (bao gồm shapeCanvas, bgPattern, inkCanvas)
var rtb = new RenderTargetBitmap((int)canvasArea.ActualWidth, (int)canvasArea.ActualHeight, 96, 96, PixelFormats.Pbgra32);
rtb.Render(canvasArea);
// Encode → PngBitmapEncoder → FileStream
```

---

## 12. Focus Mode & Spotlight

| Mode | Kích hoạt | Hành động |
|---|---|---|
| 🎯 Focus | `FocusMode_Click` | Toggle `_focusMode`. Overlay tối phủ toàn canvas, ô sáng di chuyển theo chuột |
| 💡 Spotlight | `Spotlight_Click` | Toggle `_spotlightMode`. Vùng sáng tròn theo chuột, xung quanh tối |

Tắt bằng cách click lại nút, hoặc click vào overlay.

---

## 13. Keyboard Shortcuts

| Phím | Hành động |
|---|---|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Space` (giữ) | Bật Pan Mode tạm thời |
| `Ctrl++` / `Ctrl+=` | Zoom In |
| `Ctrl+-` | Zoom Out |

> Key handler được attach vào `Window.KeyDown/KeyUp` (không phải Page), và **detach khi Unloaded** để tránh memory leak.

---

## 14. Màu palette

```csharp
// 8 màu cố định trong toolbar (Ellipse Tag)
#FFFFFF, #FF5252, #FF9800, #FFD740, #69F0AE, #40C4FF, #7C4DFF, #FF80AB

// 10 màu xoay vòng khi click màu tùy chỉnh
private static readonly string[] _extraColors = {
    "#E91E63", "#9C27B0", "#3F51B5", "#009688", "#795548",
    "#607D8B", "#F44336", "#00BCD4", "#CDDC39", "#FF5722"
};
```

---

## 15. Lỗi thường gặp & Cách xử lý

| Lỗi | Nguyên nhân | Cách xử lý |
|---|---|---|
| Tẩy không xóa được shape | `IsCloseToShape()` không nhận diện loại shape mới | Thêm `else if` vào `IsCloseToShape()` cho type mới |
| Nét vẽ không sync lên HS | `IsBroadcasting == false` hoặc `NetworkService == null` | Kiểm tra guard `if (app?.NetworkService?.IsBroadcasting == true)` |
| Undo sau khi Clear không khôi phục shape | `ClearCanvasCommand` phải clear cả `shapeCanvas` và `_shapeElements` | Kiểm tra `ClearCanvasCommand.Unexecute()` có restore `_shapeElements` |
| Memory leak keyboard handler | Không detach key handler khi navigate sang trang khác | Đảm bảo `Unloaded` event gọi `window.KeyDown -= _windowKeyDownHandler` |
| TextBox nhập chữ bị bắt phím Undo/Redo | Key handler check `Keyboard.FocusedElement is TextBox` | Đã có guard — không xử lý Ctrl+Z/Y khi focus đang ở TextBox |
| Emoji phản hồi không hiện | `floatingReactionCanvas.ActualWidth` = 0 khi page chưa render | Có fallback: `if (w <= 0) w = 800` |

---

## 16. Pattern thêm công cụ vẽ mới

1. **XAML:** Thêm `<Border x:Name="toolXxx" Tag="Xxx" MouseLeftButtonDown="SelectTool">` vào toolbar
2. **Code-behind `SelectTool()`:** Thêm `border.Name = "toolXxx"` vào mảng reset style + thêm `case "Xxx":` vào switch
3. **`CanvasArea_MouseDown()`:** Nếu tool cần shape drawing, thêm vào `switch (_currentTool)` → `flag2 = true`
4. **`StartShapeDraw()` / `UpdateShapePreview()` / `FinishShapeDraw()`:** Thêm `case "Xxx":` render shape tương ứng
5. **Undo:** Dùng `AddShapeCommand` (đã có) hoặc tạo Command mới nếu cần logic đặc biệt
6. **`IsCloseToShape()`:** Thêm hit detection cho shape type mới nếu cần tẩy được

---

## 17. File tham chiếu nhanh

> Dùng đường dẫn tương đối từ root dự án (`QASmartClass/`), không dùng đường dẫn tuyệt đối vì mỗi máy có vị trí clone khác nhau.

| File | Đường dẫn tương đối từ `QASmartClass/` |
|---|---|
| XAML chính | `Classroom/Views/CanvasPage.xaml` |
| Code-behind | `Classroom/Views/CanvasPage.xaml.cs` |
| Canvas Commands | `Services/Canvas/` *(IUndoableCommand + các Command class)* |
| Web app học sinh | `StudentWebApp/index.html` + `StudentWebApp/app.html` |
| WebSocket bridge | `Classroom/Services/WebSocketBridgeService.cs` |
| Cấu hình Undo | `%LOCALAPPDATA%\QASmartClass\Settings\board_settings.json` |
| Ảnh xuất slide | `%LOCALAPPDATA%\QASmartClass\Pictures\Lesson_*_Board_*.png` |
| Tài liệu cấu hình | `docs/CONFIGURATION_REFERENCE.md` |
| Hướng dẫn triển khai | `docs/DEPLOYMENT_GUIDE.md` |

---

## 18. Cấu hình thiết bị đặc thù

> **Mô hình triển khai thực tế:**
> - **Laptop giáo viên (Windows)** → chạy `QASmartClass.exe` (WPF), là nguồn phát lệnh
> - **Màn hình QA SmartTouch (Android)** → là thiết bị hiển thị + cảm ứng, chạy Android OS tích hợp
> - Hai thiết bị kết nối qua **HDMI/OPS** (vật lý) hoặc **không dây** (screen mirroring)

---

### 💻 Laptop giáo viên (Windows — chạy WPF CanvasPage)

Đây là máy chạy `QASmartClass.exe`. CanvasPage (WPF) hoạt động **trên laptop**, hình ảnh được chiếu lên bảng SmartTouch qua HDMI hoặc module OPS.

| Thông số | Yêu cầu |
|---|---|
| **OS** | Windows 10 v1903+ / Windows 11 |
| **Runtime** | .NET 8.0 Desktop Runtime |
| **RAM** | 8 GB (tối thiểu 4 GB) |
| **Input** | Mouse / Touchpad (laptop), hoặc touch qua USB khi dùng OPS |
| **Kết nối board** | HDMI ra bảng SmartTouch, USB Touch để nhận input cảm ứng từ bảng |

**Lưu ý Deep Freeze:** Nhiều trường dùng phần mềm đóng băng đĩa trên laptop. DB và Settings **phải** ở ổ D: hoặc theo cấu hình `workstation.json` để không bị mất khi reset.

---

### 📺 Màn hình QA SmartTouch (Android — bảng tương tác tích hợp)

Bảng SmartTouch là **thiết bị hiển thị thông minh** chạy Android natively. Không phải máy học sinh — đây là **bảng của giáo viên**.

#### Thông số phần cứng tiêu chuẩn

| Thông số | Giá trị |
|---|---|
| **Kích thước** | 55" / 65" / 75" / **86"** / 98" |
| **Tấm nền** | D-LED 4K Ultra HD (3840 × 2160 px) |
| **Độ sáng** | 350 – 400 cd/m² |
| **Góc nhìn** | 178°(H) / 178°(V) |
| **Màu sắc** | 1.07 tỷ màu (10-bit) |
| **Cảm ứng** | Hồng ngoại đa điểm, độ trễ **4ms – 6ms** |
| **OS tích hợp** | Android 11.0 (hoặc tùy dòng Pro/AI) |
| **RAM / ROM** | 1.5–2 GB RAM, 8 GB+ Storage |
| **Âm thanh** | Loa kép 20W–30W, âm thanh vòm |
| **Cổng** | HDMI, USB 2.0/3.0, Touch USB, VGA, LAN/Wi-Fi, Bluetooth |
| **Mở rộng** | **Khe cắm OPS PC** (Windows tùy chọn), Screen Mirroring không dây |

#### Hai chế độ kết nối với CanvasPage

**Chế độ 1 — OPS Module (Windows trên board, phổ biến nhất):**
```
Cắm OPS PC module (Windows) vào khe OPS của bảng
    → Bảng chạy Windows qua OPS như một máy tính nhúng
    → QASmartClass.exe chạy trực tiếp trên bảng
    → Input cảm ứng hồng ngoại → USB Touch → WPF Stylus/Touch events
    → Không cần laptop riêng
```

**Chế độ 2 — Laptop + HDMI (phổ biến):**
```
Laptop GV (Windows, WPF) ──HDMI──► Bảng SmartTouch (hiển thị)
                           ──USB Touch──► Laptop (nhận input cảm ứng)
    → CanvasPage vẽ trên laptop, chiếu lên bảng 4K
    → Cảm ứng trên bảng truyền qua USB → Windows nhận như mouse/stylus
```

**Chế độ 3 — Android Native + Screen Mirroring:**
```
Android OS của bảng chạy app riêng
    → Laptop/PC mirror màn hình lên bảng qua Wi-Fi/Miracast
    → CanvasPage vẫn chạy trên Windows (laptop/OPS)
    → Bảng Android chỉ hiển thị, input touch qua USB hoặc không dây
```

#### Lưu ý khi code cho SmartTouch board

- **Touch input:** WPF nhận cảm ứng hồng ngoại qua USB như `StylusPoint`. Giữ nguyên các `PreviewStylus*` handler trong ENGINE B — đây là input chính khi dùng bảng.
- **Độ trễ 4–6ms:** Cảm ứng bảng rất nhạy. Không cần debounce thêm; `ERASER_MOVE_THRESHOLD = 5px` là phù hợp.
- **4K DPI:** WPF render ở 96 DPI logic. Trên bảng 4K 86", Windows thường set scale 150–200%. Canvas 2000×1400px vẫn đủ vì WPF scale theo DPI tự động.
- **Hardware acceleration:** Bật `CacheMode="BitmapCache"` cho các canvas layer tĩnh (`bgPatternCanvas`, `shapeCanvas`) để giảm tải GPU khi hiển thị 4K.
- **RAM giới hạn (OPS):** OPS module thường có RAM 4–8GB. Không cache quá nhiều `Page` hoặc `Bitmap` — `_pageCache` trong ClassroomShell đã giới hạn tốt.

---

### So sánh hai thiết bị

| Tiêu chí | Laptop GV (Windows/WPF) | SmartTouch Board (Android + OPS) |
|---|---|---|
| **Thiết bị** | Laptop / PC | Bảng 55–98 inch |
| **OS** | Windows 10/11 | Android 11 tích hợp + OPS Windows tùy chọn |
| **Chạy CanvasPage** | ✅ Trực tiếp | ✅ Qua OPS module (Windows nhúng) |
| **Input** | Mouse / Trackpad | Hồng ngoại đa điểm 4–6ms |
| **Kết nối** | Laptop → HDMI → Bảng | OPS: all-in-one trên bảng |
| **Độ phân giải hiển thị** | 1080p / 2K (laptop screen) | 4K Ultra HD (3840×2160) |
| **Stylus WPF** | Qua chuột / trackpad | ✅ Cảm ứng hồng ngoại → USB Touch → WPF Stylus |
