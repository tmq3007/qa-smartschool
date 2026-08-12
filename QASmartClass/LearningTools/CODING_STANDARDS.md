# 📏 QA SmartClass — Coding Standards & Design System
> **Version:** 3.0 | **Cập nhật:** 2026-07-07
> **Áp dụng:** Toàn bộ LearningTools + StudentClient UI

---

## 🎯 Mục Tiêu

1. **Tránh lỗi font** — Tiếng Việt + emoji luôn hiển thị đúng
2. **Trình chiếu tối ưu** — Đọc được từ khoảng cách 3-6m trên màn hình 55"-86"
3. **Dễ sử dụng** — Touch-friendly, consistent UX cho GV + HS
4. **Nhận diện thương hiệu** — Màu sắc, spacing, layout thống nhất QA Smart Class
5. **Code maintainable** — Một chỗ sửa, toàn bộ cập nhật

---

## 🧱 KIẾN TRÚC MVVM (Từ v3.2+)

> **BẮT BUỘC:** Tất cả các module và giao diện mới từ v3.2 trở đi (bao gồm Admin, Reporting, và các màn hình mới) phải tuân thủ nghiêm ngặt mô hình MVVM (Model-View-ViewModel) sử dụng `CommunityToolkit.Mvvm`.

### Nguyên Tắc MVVM Tuyệt Đối
1. **View (XAML & .xaml.cs):** KHÔNG CHỨA logic nghiệp vụ. Chỉ dùng để binding và khởi tạo UI. Các sự kiện tương tác (`Click`, `TextChanged`) phải dùng `Command` hoặc `EventToCommandBehavior`.
2. **ViewModel (.cs):** Xử lý logic, dữ liệu. Kế thừa `ObservableObject`. Khai báo thuộc tính với `[ObservableProperty]` và hành động với `[RelayCommand]`.
3. **Model/Service (.cs):** Cung cấp dữ liệu, gọi DB hoặc mạng. View không được gọi trực tiếp Service, chỉ ViewModel mới gọi.
4. Tham khảo tài liệu chi tiết tại: `docs/MVVM_GUIDE.md`.

---

## 🔤 FONT — Quy Tắc Tuyệt Đối

### ❌ CẤM sử dụng
```
Consolas          — Không hỗ trợ tiếng Việt + emoji
Courier New       — Không hỗ trợ tiếng Việt
Cambria Math      — Hạn chế glyph tiếng Việt  
Bất kỳ monospace  — Trừ khi CHỈ hiển thị số ASCII thuần
```

### ✅ CHỈ sử dụng
```csharp
// Trong code-behind (.cs):
FontFamily = DS.FontPrimary;    // = new FontFamily("Segoe UI")

// Trong XAML:
FontFamily="Segoe UI"
```

### Quy tắc áp dụng
| Loại nội dung | Font | Lý do |
|:---|:---|:---|
| Mọi text tiếng Việt | `Segoe UI` | Hỗ trợ đầy đủ Unicode Vietnamese |
| Emoji (📊⚡🧬) | `Segoe UI` | Hỗ trợ color emoji trên Windows |
| Công thức toán | `Segoe UI` | Dùng Unicode math symbols thay vì font riêng |
| Số liệu, input | `Segoe UI` | Nhất quán, tránh mixing fonts |

---

## 📏 FONT SIZE — Tối Ưu Trình Chiếu

> **Nguyên tắc:** Học sinh ngồi cuối lớp (5-6m) vẫn đọc được nội dung chính

| Constant | Size | Dùng cho | Ví dụ |
|:---|:---:|:---|:---|
| `DS.FontTitle` | 24 | Tiêu đề tool (code-behind) | "📊 Thống Kê Cơ Bản" |
| `DS.FontFormula` | 28 | Công thức lớn | "ax² + bx + c = 0" |
| `DS.FontInput` | 20 | Ô nhập liệu | TextBox cho hệ số |
| `DS.FontResult` | 16 | Dòng kết quả | "x₁ = -2, x₂ = 5" |
| `DS.FontLabel` | 16 | Label | "Nhập hệ số a:" |
| `DS.FontPreset` | 15 | Nút preset, tab label | "VD: a=1, b=-3, c=2" |
| `DS.FontSubtitle` | 14 | Mô tả phụ | "Lớp 10 • Δ = b²-4ac" |
| `DS.FontNote` | 13 | Ghi chú, tooltip | "Nhấn để copy" |
| `DS.FontTag` | 12 | Badge/tag | "Lớp 10-12" |

> **Lưu ý XAML Header:** Tiêu đề trong gradient header XAML dùng `FontSize="20"` (không phải 24) vì header có chiều cao hạn chế. Giá trị DS.FontTitle (24) dùng cho tiêu đề trong code-behind UI.ToolHeader().

---

## 🎨 MÀU SẮC — Brand System

### Thương hiệu QA Smart Class
```csharp
DS.BrandPrimary    // #1565C0 — Blue chính
DS.BrandSecondary  // #0D47A1 — Blue đậm  
DS.BrandAccent     // #E65100 — Orange nhấn
```

### Nhóm Tool (Category)
| Nhóm | Foreground | Background | Dùng cho |
|:---|:---|:---|:---|
| Toán | `DS.CatMath` #1565C0 | `DS.CatMathBg` #E3F2FD | Header, stripe |
| Khoa học | `DS.CatScience` #2E7D32 | `DS.CatScienceBg` #E8F5E9 | Header, stripe |
| Ngôn ngữ | `DS.CatLanguage` #6A1B9A | `DS.CatLanguageBg` #F3E5F5 | Header, stripe |
| Đa môn | `DS.CatMulti` #E65100 | `DS.CatMultiBg` #FFF3E0 | Header, stripe |
| Tư duy | `DS.CatThinking` #AD1457 | `DS.CatThinkingBg` #FCE4EC | Header, stripe |
| Kỹ năng | `DS.CatWorkplace` #00796B | `DS.CatWorkplaceBg` #E0F2F1 | Header, stripe |

### Kết quả (Result Colors)
```csharp
DS.ResultPrimary   // #1565C0 — Kết quả chính, công thức
DS.ResultSuccess   // #2E7D32 — Đúng, tốt, hoàn thành
DS.ResultWarning   // #E65100 — Cảnh báo, chú ý
DS.ResultDanger    // #C62828 — Lỗi, sai, nguy hiểm
DS.ResultInfo      // #757575 — Ghi chú, phụ
DS.ResultSpecial   // #7B1FA2 — Đặc biệt, nổi bật
DS.ResultDark      // #283593 — Nhấn mạnh cuối
```

---

## 📐 SPACING & LAYOUT

### Khoảng cách chuẩn
```csharp
DS.PadCard        = 20   // Padding trong card
DS.PadChip        = 12   // Padding trong chip/badge  
DS.MarginSection  = 16   // Margin giữa sections
DS.MarginResult   = 4    // Margin giữa result rows
DS.RadiusCard     = 12   // Bo góc card
DS.RadiusChip     = 8    // Bo góc chip
DS.RadiusHeader   = 10   // Bo góc header
```

### Touch Target (màn hình cảm ứng)
```csharp
DS.TouchMinHeight = 44   // Chiều cao tối thiểu nút (px)
DS.TouchMinWidth  = 44   // Chiều rộng tối thiểu nút
DS.TouchGap       = 8    // Gap giữa 2 nút (tránh bấm nhầm)
DS.ContentMaxWidth = 800 // Max width nội dung
```

---

## 🏗️ UI FACTORY — Cách Tạo Components

### ❌ KHÔNG làm thế này (hardcode)
```csharp
var border = new Border {
    Background = new SolidColorBrush(Color.FromArgb(20, 21, 101, 192)),
    CornerRadius = new CornerRadius(8),
    Padding = new Thickness(14, 8, 14, 8),
    Margin = new Thickness(0, 0, 0, 4)
};
border.Child = new TextBlock {
    Text = "Kết quả: x = 5",
    FontSize = 13,
    FontFamily = new FontFamily("Consolas, Segoe UI"),  // ❌ CONSOLAS!
    Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192))
};
resultPanel.Children.Add(border);
```

### ✅ LÀM thế này (dùng UI Factory)
```csharp
UI.ResultRow("Kết quả: x = 5", DS.ResultPrimary, resultPanel);
```

### Các method chuẩn
```csharp
// Dòng kết quả click-to-copy
UI.ResultRow(text, color, panel);

// Nút preset/ví dụ nhanh
UI.PresetButton("VD: a=1", color, () => { txtA.Text = "1"; }, presetPanel);

// Nút Desmos
UI.DesmosButton("y = ax² + bx + c", DS.CatMath, OpenDesmos_Click);

// Card section
UI.SectionCard(content, borderColor);

// Header gradient
UI.ToolHeader("📊", "Thống Kê", "Lớp 10 • Mean, Median", startColor, endColor, textColor);

// Input field
var txt = UI.InputField("0", DS.FontInput);

// Format số
UI.Fmt(3.14159);  // → "3.14159"
UI.Fmt(5.0);      // → "5"
```

---

## 📋 CHECKLIST — Khi Tạo Tool Mới

- [ ] **Font**: Chỉ dùng `DS.FontPrimary` / `"Segoe UI"` — KHÔNG Consolas
- [ ] **Font size**: Dùng `DS.FontTitle/FontResult/...` — KHÔNG hardcode số
- [ ] **Màu**: Dùng `DS.ResultPrimary/CatMath/...` — KHÔNG hardcode hex
- [ ] **Result rows**: Dùng `UI.ResultRow()` — có click-to-copy tự động
- [ ] **Presets**: Dùng `UI.PresetButton()` — có hover effect
- [ ] **Desmos**: Dùng `UI.DesmosButton()` — layout chuẩn
- [ ] **Touch**: Mọi nút ≥ 44px height, gap ≥ 8px
- [ ] **MaxWidth**: Content ≤ 800px, có `ScrollViewer`
- [ ] **Namespace**: Kế thừa `BaseToolControl`
- [ ] **Registry**: Thêm vào `ToolRegistry.cs` + `LearningToolsHub.cs`
- [ ] **Sidebar**: Dùng `SidebarListBoxItemStyle` — KHÔNG copy-paste inline
- [ ] **Sidebar Width**: Cố định 250px
- [ ] **Naming**: viewPractical, practicalAppViewer — KHÔNG viewApp, contentApp  
- [ ] **Default tab**: Sidebar index 0 (Hướng dẫn) — KHÔNG index 1
- [ ] **Guide layout**: 2 cột GV/HS — KHÔNG 1 cột
- [ ] **PracticalApp**: ≥ 6 items, dùng `SetItemsSource()`
- [ ] **Dispose**: Override `Dispose()` nếu có timer/media

---

## 📛 NAMING CONVENTION — Quy Tắc Đặt Tên

### View Container Names (trong XAML)
| Tên ĐÚNG | Tên SAI | Lý do |
|:---|:---|:---|
| `viewPractical` | `viewApp`, `borderApp` | Thống nhất 70+ tools |
| `practicalAppViewer` | `contentApp` | Mapping rõ ràng với control class |
| `viewGuide` | — | Tab Hướng dẫn |
| `viewPlay` / `viewCalculator` | — | Tab chính theo chức năng |
| `sideMenu` | `sidebar`, `lstMenu` | Thống nhất |

### Sidebar Default Selection
- **BẮT BUỘC:** Item đầu tiên (index 0 = Hướng dẫn) phải có `IsSelected="True"`
- **KHÔNG** mặc định vào tab Calculator/Play

### Sidebar Shared Style
- **BẮT BUỘC** từ v3.0: Dùng `Style="{StaticResource SidebarListBoxItemStyle}"` thay vì copy-paste inline style
- Set màu riêng bằng local `DynamicResource`:
  ```xml
  <Grid.Resources>
      <SolidColorBrush x:Key="SidebarHoverBrush" Color="#C5CAE9"/>
      <SolidColorBrush x:Key="SidebarSelectedBrush" Color="{StaticResource BrandPrimaryColor}"/>
  </Grid.Resources>
  ```

### Guide Layout
- **BẮT BUỘC:** Layout 2 cột Giáo viên / Học sinh
- **KHÔNG** dùng layout 1 cột

---

## 📁 FILES THAM CHIẾU

| File | Vai trò |
|:---|:---|
| `LearningTools/DS.cs` | Design constants — fonts, colors, spacing |
| `LearningTools/UI.cs` | UI Factory — tạo components chuẩn |
| `LearningTools/Controls/BaseToolControl.cs` | Base class cho tool |
| `LearningTools/Models/ToolRegistry.cs` | Registry tool definitions |
| `LearningTools/Views/LearningToolsHub.xaml.cs` | Factory + Hub |
| `CODING_STANDARDS.md` | File này |
| `LearningTools/Controls/PracticalAppViewer.xaml` | Control ứng dụng thực tế (shared) |
| `LearningTools/Models/PracticalAppItem.cs` | Model data ứng dụng thực tế |
| `LearningTools/Themes/LearningToolsStyles.xaml` | Shared sidebar style (v3.0+) |

---

> **⚠️ QUY TẮC VÀNG:** Nếu bạn đang tạo `new FontFamily("...")` hoặc `new SolidColorBrush(Color.FromRgb(...))` trong code,  
> hãy dừng lại và kiểm tra xem `DS.cs` hoặc `UI.cs` đã có sẵn chưa.  
> Nếu chưa có, thêm vào `DS.cs` trước, rồi sử dụng từ đó.
