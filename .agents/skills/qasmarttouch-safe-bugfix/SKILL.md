---
name: qasmarttouch-safe-bugfix
description: >-
  Quy trình chẩn đoán gốc rễ, khoanh vùng bán kính ảnh hưởng và sửa lỗi an toàn không gây hồi quy
  (Zero-Regression Bugfix Protocol) dành riêng cho mã nguồn kế thừa QASmartTouch. Kích hoạt skill này
  mỗi khi người dùng báo lỗi (bug), phản ánh hành vi bất thường, hoặc phàn nàn "vừa sửa xong chỗ này
  lại hỏng chỗ khác".
---

# Quy Trình Sửa Lỗi Không Hồi Quy Cho Mã Nguồn Kế Thừa (`Zero-Regression Bugfix Protocol`)

Khi người dùng báo một lỗi trong `QASmartTouch`, tuyệt đối không đoán mò hoặc vá đè câu lệnh `if` ở ngọn. Hãy thực thi nghiêm ngặt quy trình 5 bước của **Principal Debugging Engineer** dưới đây:

---

## Bước 1: Tái Hiện Luồng Thực Thi Bằng Mã Nguồn (Trace Execution Path)

1. Mở đọc trực tiếp hàm khởi phát sự kiện (không suy đoán qua tên biến):
   * Lỗi từ nút bấm / SubMenu $\rightarrow$ Bắt đầu từ sự kiện Click trong `Form2_MainDashboard.Tools.cs`.
   * Lỗi khi chạm vẽ / tẩy / kéo trên bảng $\rightarrow$ Bắt đầu từ `Form2_MainDashboard.Input.cs` và `Handlers/TouchHandler.cs`, đồng thời đọc skill `qasmarttouch-input-touch-pen-eraser`.
   * Lỗi khi chọn / di chuyển / xoay / đổi màu / Undo-Redo $\rightarrow$ Bắt đầu từ `Form2_MainDashboard.Selection.cs` và `Form2_MainDashboard.Tools.cs` (`ExecuteUndoAction` / `ApplyModifyValue`), đồng thời đọc skill `qasmarttouch-selection-transform-undo`.
2. Lần theo toàn bộ chuỗi hàm từ lúc sự kiện đi vào tới lúc trạng thái bị sai lệch. Đối chiếu với [Sổ đen Bẫy lịch sử](../qasmarttouch-architecture/references/state-contracts-and-pitfalls.md) xem có rơi vào các bẫy quen thuộc (như `SelectTool` gọi `ResetAllToolStates`, `EnsurePassthroughFocusAndPenMode`, thiếu `RebuildQuadTree`, v.v.) không.

---

## Bước 2: Phân Biệt "Triệu Chứng" vs. "Nguyên Nhân Gốc" (Symptom vs. Root Cause)

Luôn tự đặt câu hỏi **"Tại sao trạng thái lại bị lệch ngay từ đầu?"**:
* ❌ **Vá triệu chứng (CẤM):** Thấy `NullReferenceException` hoặc hình không chọn được thì bọc `try { ... } catch { }` hoặc gọi bừa `RefreshSelectableObjects()` trong vòng lặp `MouseMove` gây giật lag toàn bảng.
* ✅ **Trị tận gốc (CHUẨN):** Phát hiện sau khi `Undo Remove` khôi phục phần tử vào `MainInteractiveBoard.Children`, code quên đăng ký phần tử đó vào `_selectionManager`, nên bổ sung đúng lời gọi `RegisterNewObjectWithSelectionManager(element)` ngay tại điểm khôi phục.

---

## Bước 3: Quét Bán Kính Ảnh Hưởng Trước Khi Chạm Vào Hàm Dùng Chung (Blast-Radius Scan)

Trước khi sửa bất kỳ dòng nào trong một hàm:
1. Kiểm tra xem có những nơi nào khác đang gọi hàm đó (`Callers`).
2. Kiểm tra xem biến trạng thái bạn định sửa (`_selectedTool`, `_objectSelectionMode`, `_isDrawing`, `ToolManager.CurrentMode`, `_touchHandler`...) đang được đọc ở những file Partial Class nào khác.
3. Nếu đây là lỗi Cấp độ 2 (`Architect Gate` — chạm vào Logic/State/Input/Selection/Đa file), trình bày gọn **Nguyên nhân gốc + Tác động 4 Cổng + Kế hoạch sửa tối thiểu** và chờ người dùng xác nhận trước khi sửa file.

---

## Bước 4: Phẫu Thuật Tối Thiểu & Kiểm Chứng Biên Dịch `QASmartTouch`

1. Sửa đúng những dòng cần thiết (Minimal Surgical Diff). Giữ nguyên định dạng, comment và các guard an toàn hiện có xung quanh.
2. Nếu có tách thêm lớp helper/service mới, kiểm tra tệp [`QASmartTouch.csproj`](file:///e:/qa-smartschool/QASmartTouch/QASmartTouch.csproj) đã bao gồm tệp đó chưa.
3. Chạy lệnh kiểm chứng bắt buộc:
   ```powershell
   dotnet build QASmartTouch/QASmartTouch.csproj
   ```
   Phải đạt **`0 Error(s)`** trước khi báo cáo hoàn tất.

---

## Bước 5: Xuất "Checklist Test Tay 30 Giây" & Gợi Ý Lưu Bẫy Mới (`/learn`)

Vì người dùng đang trực tiếp kiểm thử tay trên ứng dụng `QASmartTouch`, cuối câu trả lời sau khi sửa xong **BẮT BUỘC** trình bày hộp kiểm ngắn gọn theo mẫu:

```markdown
### 🧪 Checklist Test Tay Nhanh (30 giây)
- [ ] **Kiểm tra lỗi chính:** [1-2 thao tác cụ thể để xác nhận lỗi vừa báo đã hết hẳn]
- [ ] **Kiểm tra hồi quy công cụ kế cận:** [Ví dụ: Chuyển sang Bút vẽ -> Tẩy -> Chọn vùng xem có bị kẹt hay tự vẽ đè không]
- [ ] **Kiểm tra hồi quy Undo/Redo:** [Bấm Undo -> Redo thao tác vừa làm xem đối tượng và khung chọn có đồng bộ không]
```

* Nếu lỗi vừa sửa xuất phát từ một quy ước ngầm hiểm hóc chưa có trong `state-contracts-and-pitfalls.md`, hãy chủ động nhắc ngắn gọn: *"Mẹo: Nếu kiểm tra thấy đã chuẩn, bạn có thể gõ `/learn` để mình lưu bẫy này vào Sổ đen của dự án, tránh tái diễn về sau."*

