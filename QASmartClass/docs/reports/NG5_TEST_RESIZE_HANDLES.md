# NG-5: Test Resize Handles - Hướng dẫn kiểm tra

## 📋 Mục tiêu
Kiểm tra chức năng resize (thay đổi kích thước) qua các điểm resize handles cho **tất cả 7 loại đối tượng**:
1. Polyline (vẽ tự do)
2. Rectangle (hình chữ nhật)
3. Ellipse (hình elip)
4. Line (đường thẳng)
5. TextBlock (văn bản)
6. Border (khung viền)
7. Image (hình ảnh)

## 🔧 Cải tiến đã thực hiện

### Trước khi sửa:
- ❌ Resize chỉ hoạt động với FrameworkElement (Rectangle, Ellipse, TextBlock, Border, Image)
- ❌ Polyline không resize được (cần scale Points collection)
- ❌ Line không resize được (cần update X1,Y1,X2,Y2)

### Sau khi sửa:
- ✅ **Line**: Cập nhật X2, Y2 dựa trên kích thước mới
- ✅ **Polyline**: Scale tất cả các điểm theo tỷ lệ (proportional scaling)
- ✅ **FrameworkElement**: Giữ nguyên logic Width/Height (Rectangle, Ellipse, TextBlock, Border, Image)
- ✅ **Canvas Position**: Cập nhật vị trí khi resize từ góc TopLeft, TopRight, BottomLeft

## 📝 Quy trình kiểm tra

### TEST 1: Polyline (Vẽ tự do) - PRIORITY HIGH
**Chuẩn bị:**
1. Click công cụ "Bút" (Pen tool)
2. Vẽ một hình dạng tự do phức tạp (ví dụ: chữ "S" hoặc vòng tròn không đều)

**Kiểm tra:**
1. Click chọn đối tượng → xuất hiện SelectionBox với 4 resize handles
2. Kéo góc **BottomRight** (góc phải dưới) → hình dạng phóng to đồng tỉ lệ
   - ✅ Pass: Tất cả các điểm di chuyển theo tỷ lệ, hình dạng tổng thể được giữ nguyên
   - ❌ Fail: Hình dạng bị méo, điểm không di chuyển đồng đều
3. Kéo góc **TopLeft** (góc trái trên) → hình dạng thu nhỏ và di chuyển
   - ✅ Pass: Hình dạng giữ tỷ lệ, vị trí thay đổi đúng
   - ❌ Fail: Hình dạng biến dạng hoặc không di chuyển
4. Kéo góc **TopRight** và **BottomLeft** → kiểm tra tương tự

**Debug Output cần có:**
```
🔧 Resize Polyline: [N] points scaled
```

---

### TEST 2: Line (Đường thẳng) - PRIORITY HIGH
**Chuẩn bị:**
1. Click công cụ "Đường thẳng" (Line tool)
2. Vẽ một đường thẳng từ trái sang phải

**Kiểm tra:**
1. Click chọn đường thẳng → xuất hiện SelectionBox
2. Kéo góc **BottomRight** → đường thẳng kéo dài
   - ✅ Pass: Điểm cuối (X2, Y2) di chuyển, điểm đầu (X1, Y1) giữ nguyên
   - ❌ Fail: Cả hai điểm di chuyển hoặc đường thẳng không thay đổi
3. Kéo góc **TopLeft** → điểm đầu di chuyển, đường thẳng di chuyển
   - ✅ Pass: Điểm đầu di chuyển đúng, Canvas position cập nhật
   - ❌ Fail: Đường thẳng không di chuyển hoặc bị lỗi
4. Kéo góc **TopRight** → chiều dài thay đổi, hướng xoay thay đổi
5. Kéo góc **BottomLeft** → chiều dài và hướng thay đổi

**Debug Output cần có:**
```
🔧 Resize Line: (X1,Y1) → (X2,Y2)
```

---

### TEST 3: Rectangle (Hình chữ nhật) - PRIORITY MEDIUM
**Chuẩn bị:**
1. Click công cụ "Hình chữ nhật"
2. Vẽ một hình chữ nhật 200x150 pixels

**Kiểm tra:**
1. Kéo góc **BottomRight** → Width và Height tăng
   - ✅ Pass: Hình chữ nhật phóng to, tỷ lệ thay đổi tự do
   - ❌ Fail: Kích thước không thay đổi
2. Kéo góc **TopLeft** → Width, Height giảm, vị trí di chuyển
   - ✅ Pass: Canvas.Left và Canvas.Top cập nhật đúng
   - ❌ Fail: Hình chữ nhật không di chuyển
3. Kéo cạnh **Right** (resize handle bên phải) → chỉ Width thay đổi
4. Kéo cạnh **Top** (resize handle trên) → chỉ Height thay đổi, vị trí Y di chuyển

**Debug Output cần có:**
```
🔧 Resize Rectangle: [Width]x[Height]
```

---

### TEST 4: Ellipse (Hình elip) - PRIORITY MEDIUM
**Chuẩn bị:**
1. Click công cụ "Hình tròn/Elip"
2. Vẽ một hình tròn hoặc elip

**Kiểm tra:**
1. Kéo góc **BottomRight** → hình tròn/elip phóng to
   - ✅ Pass: Hình dạng oval/tròn duy trì, kích thước tăng
   - ❌ Fail: Hình bị méo không đúng
2. Kéo góc **TopLeft** → hình thu nhỏ và di chuyển
   - ✅ Pass: Vị trí Canvas cập nhật, hình dạng giữ nguyên
3. Kiểm tra resize theo cạnh (Top, Bottom, Left, Right)

**Debug Output cần có:**
```
🔧 Resize Ellipse: [Width]x[Height]
```

---

### TEST 5: TextBlock (Văn bản) - PRIORITY LOW
**Chuẩn bị:**
1. Click công cụ "Text" (Văn bản)
2. Thêm một đoạn văn bản dài (ví dụ: "Đây là văn bản kiểm tra resize")

**Kiểm tra:**
1. Kéo góc **BottomRight** → container văn bản mở rộng
   - ✅ Pass: Văn bản tự động reflow (xuống dòng) trong container mới
   - ❌ Fail: Văn bản không thay đổi hoặc bị cắt
2. Kéo góc **TopLeft** → container thu nhỏ
   - ✅ Pass: Văn bản reflow, vị trí Canvas cập nhật
3. Kéo cạnh **Bottom** → chiều cao tăng (văn bản có nhiều không gian)

**Debug Output cần có:**
```
🔧 Resize TextBlock: [Width]x[Height]
```

---

### TEST 6: Border (Khung viền) - PRIORITY LOW
**Chuẩn bị:**
1. Tạo một Border element (nếu có trong UI)
2. Hoặc tạo một hình có viền rõ ràng

**Kiểm tra:**
1. Kéo góc **BottomRight** → Border mở rộng
   - ✅ Pass: Độ dày viền giữ nguyên (không scale), kích thước tăng
   - ❌ Fail: Viền bị méo hoặc không thay đổi
2. Kéo góc **TopLeft** → Border thu nhỏ và di chuyển
3. Kiểm tra các góc khác

**Debug Output cần có:**
```
🔧 Resize Border: [Width]x[Height]
```

---

### TEST 7: Image (Hình ảnh) - PRIORITY LOW
**Chuẩn bị:**
1. Chèn một hình ảnh vào canvas (nếu có chức năng)

**Kiểm tra:**
1. Kéo góc **BottomRight** → hình ảnh phóng to
   - ✅ Pass: Hình ảnh scale đúng (kiểm tra aspect ratio nếu có)
   - ❌ Fail: Hình ảnh bị méo hoặc không scale
2. Kéo góc **TopLeft** → hình ảnh thu nhỏ và di chuyển
3. Kiểm tra xem có giữ tỷ lệ khung hình không (aspect ratio preservation)

**Debug Output cần có:**
```
🔧 Resize Image: [Width]x[Height]
```

---

## 🐛 Các lỗi có thể gặp

### Lỗi 1: Polyline không scale đều
**Triệu chứng:** Một số điểm di chuyển, một số không
**Nguyên nhân:** Logic scale không áp dụng cho tất cả Points
**Cách kiểm tra:** Vẽ hình dạng phức tạp và quan sát kỹ khi resize

### Lỗi 2: Line không di chuyển khi kéo TopLeft
**Triệu chứng:** Đường thẳng resize nhưng không di chuyển
**Nguyên nhân:** Canvas.SetLeft/SetTop không được cập nhật
**Cách kiểm tra:** Kéo góc TopLeft và xem vị trí có thay đổi không

### Lỗi 3: FrameworkElement bị nhảy vị trí
**Triệu chứng:** Rectangle/Ellipse nhảy vị trí lạ khi resize
**Nguyên nhân:** Canvas position bị set sai
**Cách kiểm tra:** Resize từ góc TopLeft và quan sát vị trí

### Lỗi 4: Resize quá nhỏ (< 10x10 pixels)
**Triệu chứng:** Đối tượng biến mất khi resize quá nhỏ
**Nguyên nhân:** Không có minimum size validation
**Cách kiểm tra:** Kéo resize về góc và xem có bị âm không

---

## ✅ Tiêu chí Pass/Fail

### Pass (OK) khi:
- ✅ Tất cả 7 loại đối tượng có thể resize từ 4 góc (TopLeft, TopRight, BottomLeft, BottomRight)
- ✅ Polyline: Hình dạng được giữ nguyên khi scale (proportional)
- ✅ Line: Điểm cuối (X2, Y2) di chuyển đúng khi resize
- ✅ FrameworkElement: Width/Height thay đổi đúng
- ✅ Vị trí Canvas được cập nhật khi resize từ TopLeft, TopRight, BottomLeft
- ✅ Debug output hiển thị log resize đúng loại đối tượng
- ✅ Không có crash hoặc exception

### Fail (NG) khi:
- ❌ Bất kỳ loại đối tượng nào không resize được
- ❌ Polyline bị méo, điểm không scale đều
- ❌ Line không di chuyển khi resize từ góc TopLeft
- ❌ FrameworkElement bị nhảy vị trí ngẫu nhiên
- ❌ Resize tạo ra kích thước âm (Width < 0, Height < 0)
- ❌ Ứng dụng crash khi resize

---

## 📊 Kết quả kiểm tra

### Bảng checklist:

| Loại đối tượng | BottomRight | TopLeft | TopRight | BottomLeft | Kết quả |
|----------------|-------------|---------|----------|------------|---------|
| Polyline       | ⬜          | ⬜      | ⬜       | ⬜         | ⬜      |
| Line           | ⬜          | ⬜      | ⬜       | ⬜         | ⬜      |
| Rectangle      | ⬜          | ⬜      | ⬜       | ⬜         | ⬜      |
| Ellipse        | ⬜          | ⬜      | ⬜       | ⬜         | ⬜      |
| TextBlock      | ⬜          | ⬜      | ⬜       | ⬜         | ⬜      |
| Border         | ⬜          | ⬜      | ⬜       | ⬜         | ⬜      |
| Image          | ⬜          | ⬜      | ⬜       | ⬜         | ⬜      |

**Ghi chú:**
- ✅ = Pass (hoạt động đúng)
- ❌ = Fail (có lỗi)
- ⚠️ = Partial (hoạt động nhưng có vấn đề nhỏ)
- ⬜ = Chưa test

---

## 🚀 Tiếp theo

Sau khi test xong NG-5, chất lượng tính năng "Chọn vùng" sẽ đạt **100%** (5/5 NG issues đã fix):

| Issue | Tên | Trạng thái |
|-------|-----|------------|
| NG-1  | Rectangle Drag Selection | ✅ OK |
| NG-2  | Copy all object types | ✅ OK |
| NG-3  | Keyboard shortcuts | ✅ OK |
| NG-4  | Test Move all types | ✅ OK |
| NG-5  | Test Resize handles | 🔄 Testing |

**Thời gian dự kiến test:** 20-30 phút
**Thời gian dự kiến tài liệu:** 10 phút

---

## 📌 Ghi chú kỹ thuật

### Code changes:
- **File**: `Services/TransformService.cs`
- **Method 1**: `Resize()` (lines 52-130) - Enhanced với type-specific logic
- **Method 2**: `ResizeFromHandle()` (lines 132-195) - Thêm Canvas position update

### Debug commands:
- Mở Debug Console trong Visual Studio
- Filter: `🔧 Resize` để xem log resize
- Kiểm tra pattern: `🔧 Resize [Type]: [Details]`

### Rollback (nếu cần):
```bash
git diff Services/TransformService.cs
git checkout Services/TransformService.cs
```
