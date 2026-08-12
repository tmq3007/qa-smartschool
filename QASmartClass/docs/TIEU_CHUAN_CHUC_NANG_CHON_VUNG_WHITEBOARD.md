# TIÊU CHUẨN KỸ THUẬT & THIẾT KẾ SƯ PHẠM
## CHỨC NĂNG CHỌN VÙNG VÀ THAO TÁC ĐA ĐỐI TƯỢNG TRÊN BẢNG TRẮNG TƯƠNG TÁC (SMART TOUCH)

- **Mã tính năng / Mô-đun:** `NG-1` / `Form2_MainDashboard` & `SelectionManager`
- **Tên chức năng:** Chọn vùng khoanh chữ nhật và thao tác biến đổi nhóm đối tượng (Rectangle Selection & Multi-Object Group Transform)
- **Hệ thống áp dụng:** QA SmartClass v4.2 & Bảng trắng tương tác **SMART TOUCH** (Màn hình cảm ứng 86-inch)
- **Ngày ban hành:** 22/07/2026
- **Trạng thái:** ✅ ĐÃ TIÊU CHUẨN HÓA & NGHIỆM THU (VERIFIED)

---

## I. MỤC TIÊU VÀ PHẠM VI ÁP DỤNG

### 1. Mục tiêu
Cung cấp chuẩn mực thiết kế sư phạm và kiến trúc phần mềm cho tính năng **Chọn vùng (Rectangle Drag Selection)** trên Bảng trắng tương tác **SMART TOUCH**, cho phép Giáo viên và Học sinh:
1. Khoanh vùng và chọn đồng thời nhiều đối tượng thuộc các chủng loại khác nhau (nét vẽ tự do, hình 2D, khối 3D, hộp văn bản, ghi chú).
2. Di chuyển, xoay, thay đổi kích thước (Resize/Rotate) và đổi thuộc tính đồng bộ cho toàn bộ nhóm đối tượng được chọn.
3. Tối ưu trải nghiệm tương tác trên màn hình cảm ứng cỡ lớn 86-inch và thao tác phím tắt / chuột trên máy tính.

### 2. Phạm vi đối tượng hỗ trợ chọn vùng
Tất cả các đối tượng vẽ và tương tác trên Canvas của Bảng trắng đều phải hỗ trợ chọn vùng đồng nhất:
* **Nét vẽ tự do (Freehand Strokes):** Bút thường (Pen), Bút dạ quang (Highlighter).
* **Hình học phẳng 2D:** Hình chữ nhật, Hình vuông, Hình tròn/Elip, Tam giác, Đa giác, Đường thẳng, Mũi tên.
* **Khối hình học 3D & Mô hình tương tác:** Lập phương, Tứ diện, Nón, Trụ, Cầu, Mô hình STEM/Vật lý/Hóa học.
* **Văn bản & Ghi chú:** Hộp văn bản (Text Box), Thẻ ghi chú (Sticky Notes), Công thức toán học (Math Formulas).

---

## II. QUY CHUẨN THIẾT KẾ GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG (UI/UX STANDARDS)

### 1. Khung xem trước khi khoanh vùng chọn (Selection Preview Rectangle)
Trong quá trình kéo giữ chuột hoặc vuốt cảm ứng để chọn vùng, hệ thống hiển thị hình chữ nhật phản hồi thị giác với thông số:
* **Đường viền (Stroke):** Màu xanh lam chủ đạo `RGB(52, 152, 219)` (Branding Blue).
* **Độ dày viền (StrokeThickness):** `2px` (hiển thị sắc nét trên màn hình cảm ứng 86-inch ở khoảng cách xa).
* **Kiểu nét (StrokeDashArray):** Nét đứt `[5, 3]` (5px nét, 3px khoảng trống) để phân biệt rõ với nét vẽ hình chữ nhật thường.
* **Màu nền phủ (Fill):** `FromArgb(30, 52, 152, 219)` (~12% Alpha semi-transparent), giúp xem rõ các đối tượng nằm phía bên dưới vùng chọn.
* **Thứ tự lớp hiển thị (ZIndex):** Thiết lập `ZIndex = 9999` (luôn đè lên trên tất cả các layer đối tượng trên Canvas).

### 2. Khung bao nhóm đối tượng (Multi-Object Bounding Box)
Sau khi thả chuột/thả tay cảm ứng, các đối tượng nằm trong vùng chọn sẽ được bao quanh bởi một khung Bounding Box chuẩn:
* **8 Nốt điều chỉnh kích thước (Resize Handles):** Vị trí tại 4 góc và 4 trung điểm cạnh. Kích thước nốt tối thiểu `10x10px` để dễ dàng chạm/kéo bằng ngón tay trên **SMART TOUCH**.
* **1 Nốt xoay (Rotate Handle):** Đặt ở phía trên trung điểm cạnh trên (cách viền `-20px`), kết nối bằng một đường dóng đứng.
* **Thanh công cụ ngữ cảnh nhóm (Group Context Toolbar):** Hiển thị ngay trên đầu Bounding Box, chứa các nút chức năng:
  * 🧩 **Gom nhóm (Group / Ungroup):** Liên kết các đối tượng được chọn thành 1 khối duy nhất.
  * 🎨 **Đổi màu nhóm (Group Color):** Thay đổi màu nét / màu nền hàng loạt.
  * 🔒 **Khóa đối tượng (Lock):** Cố định nhóm đối tượng tránh di chuyển vô ý.
  * 🗑️ **Xóa nhóm (Delete):** Xóa tất cả các đối tượng đang được chọn.

---

## III. QUY CHUẨN THUẬT TOÁN & PHÂN ĐỊNH CHẾ ĐỘ (TECHNICAL SPECIFICATIONS)

### 1. Thuật toán va chạm (Hit Testing Algorithm)
* **Phương thức kiểm tra:** Sử dụng `Rect.IntersectsWith(obj.Bounds)` để xác định đối tượng. Chỉ cần một phần của đối tượng chạm vào khung chữ nhật chọn vùng là đối tượng đó được đưa vào danh sách chọn (phù hợp với tiêu chuẩn UX Photoshop / PowerPoint).
* **Bỏ qua đối tượng bị khóa:** Hệ thống tự động kiểm tra thuộc tính `obj.IsLocked`. Nếu `IsLocked == true`, đối tượng đó sẽ bị bỏ qua và không được chọn vào nhóm.

### 2. Tính toán hướng kéo vùng chọn (Drag Direction Independence)
Thuật toán phải xử lý chính xác cho cả 4 hướng kéo chọn của người dùng (Trái ➔ Phải, Phải ➔ Trái, Trên ➔ Dưới, Dưới ➔ Trên):
```csharp
double left = Math.Min(startPoint.X, currentPoint.X);
double top = Math.Min(startPoint.Y, currentPoint.Y);
double width = Math.Abs(currentPoint.X - startPoint.X);
double height = Math.Abs(currentPoint.Y - startPoint.Y);
Rect selectionRect = new Rect(left, top, width, height);
```

### 3. Phân định chế độ tương tác (Mode Disambiguation)
Để tránh xung đột giữa hành vi Vẽ tự do và Chọn vùng trên màn hình cảm ứng:
* **Khi đang ở Chế độ Chọn (Select Tool Mode):** Thao tác kéo giữ trên vùng trống Canvas ➔ Kích hoạt Chọn vùng chữ nhật.
* **Khi đang ở Chế độ Vẽ (Pen / Shape Tool Mode):** Thao tác kéo giữ ➔ Thực hiện vẽ nét hoặc vẽ hình, không kích hoạt Chọn vùng.
* **Chuyển đổi linh hoạt (Smart Touch Gesture):** Hỗ trợ cử chỉ 2 ngón tay chạm giữ đồng thời trên vùng trống để mở vùng chọn nhanh kể cả khi đang ở chế độ vẽ.

---

## IV. BỘ PHÍM TẮT VÀ CỬ CHỈ THAO TÁC CHUẨN

| Thao tác | Phím / Cử chỉ | Hành vi hệ thống |
| :--- | :--- | :--- |
| **Kéo chọn vùng tiêu chuẩn** | Click/Touch + Kéo trên vùng trống | Chọn tất cả đối tượng chạm vào khung hình chữ nhật (xóa vùng chọn cũ). |
| **Chọn bổ sung (Add)** | `Ctrl` + Kéo chọn vùng | Giữ nguyên các đối tượng đang chọn, bổ sung thêm các đối tượng trong vùng mới. |
| **Loại trừ vùng chọn (Subtract)** | `Shift` + Kéo chọn vùng | Bỏ chọn các đối tượng nằm trong khung chọn mới. |
| **Chọn/Bỏ chọn lẻ** | `Ctrl` + Click/Touch vào đối tượng | Đảo trạng thái chọn của duy nhất đối tượng được click. |
| **Hủy toàn bộ chọn** | Click/Touch vào vị trí trống bất kỳ | Giải phóng khung Bounding Box, đưa trạng thái về không chọn (`Deselect All`). |
| **Gom nhóm đối tượng** | `Ctrl + G` | Nhóm các đối tượng đang chọn thành một `GroupObject`. |
| **Rã nhóm đối tượng** | `Ctrl + Shift + G` | Tách nhóm đối tượng thành các thành phần đơn lẻ. |

---

## V. BẢNG CHECKSHEET KIỂM THỬ TIÊU CHUẨN (TESTING CHECKSHEET)

| STT | Kịch bản kiểm thử (Test Case) | Dữ liệu / Thao tác đầu vào | Kết quả kỳ vọng (Pass Criteria) | Trạng thái |
| :---: | :--- | :--- | :--- | :---: |
| 1 | Khoanh vùng chọn nét vẽ & hình 2D | Vẽ nét bút + Hình chữ nhật + Thẻ ghi chú. Kéo chọn cả 3. | Khung nét đứt xuất hiện. Chọn thành công cả 3 đối tượng cùng lúc. | ✅ PASS |
| 2 | Kéo chọn ngược hướng | Kéo từ góc Phải-Dưới lên góc Trái-Trên. | Khung nét đứt hiển thị đúng kích thước, chọn đúng các đối tượng. | ✅ PASS |
| 3 | Kiểm tra đối tượng bị khóa (`IsLocked`) | Khóa 1 hình tròn. Kéo khoanh vùng chứa hình tròn đó và các nét vẽ. | Nét vẽ được chọn, hình tròn bị khóa không bị đưa vào nhóm chọn. | ✅ PASS |
| 4 | Thao tác trên màn hình **SMART TOUCH** | Dùng ngón tay / Bút cảm ứng vuốt khoanh vùng trên màn 86". | Khung đứt nét bám theo đầu ngón tay mượt mà, không bị lag/giật. | ✅ PASS |
| 5 | Biến đổi nhóm (Group Resize/Rotate) | Kéo 8 nốt góc / nốt xoay trên Bounding Box nhóm. | Tất cả đối tượng trong nhóm đồng loạt phóng to, thu nhỏ, xoay đồng bộ. | ✅ PASS |
| 6 | Thao tác chọn bổ sung (`Ctrl + Drag`) | Chọn sẵn 2 hình. Giữ `Ctrl` và kéo chọn thêm 1 hình khác. | Cả 3 hình đều nằm trong nhóm chọn mới. | ✅ PASS |
| 7 | Hủy chọn khi click vùng trống | Click chuột/chạm tay vào vùng Canvas trống. | Khung Bounding Box biến mất, giải phóng nhóm chọn. | ✅ PASS |
| 8 | Chuyển chế độ giữa **DESKTOP** & **SMART CLASS** | Chuyển đổi giữa chế độ Windows Desktop và màn hình quản lý lớp học. | Trạng thái chọn và cấu hình vùng chọn lưu trữ ổn định, không gây crash. | ✅ PASS |

---

## VI. RÀNG BUỘC THƯƠNG HIỆU VÀ QUY CHUẨN HỆ THỐNG QA SMARTCLASS V4.2

1. **Ràng buộc giữ nguyên từ khóa thương hiệu Tiếng Anh (`QC_4.2_LANGUAGE_BRANDING`):**
   * Giữ nguyên tuyệt đối các nhãn từ khóa: **SMART CLASS**, **SMART TOUCH**, **DESKTOP** trong toàn bộ giao diện và văn bản tài liệu liên quan.
2. **Ràng buộc Bố cục Giao diện (`QC_4.2_LAYOUT_GRID`):**
   * Khung điều khiển và các bảng tương tác phải thiết lập `HorizontalAlignment="Stretch"` trên Root Grid, không cố định MaxWidth ngoài cùng để tránh hiện tượng giật lệch giao diện khi thay đổi độ phân giải màn hình cảm ứng 86-inch.

---
*Tài liệu được phê duyệt bởi Hội đồng Chuyên gia và Kỹ sư Thiết kế Dự án QA SmartClass v4.2.*
