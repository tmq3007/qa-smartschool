---
trigger: always_on
---

# 🎯 QUY CHUẨN XÁC THỰC GIAO DIỆN THỰC THI (UI RUNTIME VERIFICATION)

## [RULE] QST_UI_RUNTIME_ACCURACY: 4 Nguyên tắc Kiểm chứng Giao diện `QASmartTouch`

Mọi phân tích, đánh giá, kế hoạch hoặc chỉnh sửa liên quan đến giao diện người dùng của **QA Smart Touch (`Form2_MainDashboard` và các hộp thoại/công cụ con)** bắt buộc tuân thủ nghiêm ngặt 4 nguyên tắc sau:

### 1. Truy vết trực tiếp từ Sự kiện Code-Behind Thực thi (Runtime Event Trace)
* Tuyệt đối không phỏng đoán cửa sổ giao diện chỉ dựa vào tên tệp tin tương tự nhau trong thư mục `Forms/`.
* Mỗi khi làm việc với một nút bấm hoặc SubMenu trên Toolbar chính, bắt buộc phải:
  1. Kiểm tra hàm sự kiện click tương ứng trong `Form2_MainDashboard.Tools.cs` (hoặc Partial Class đang active) để xác định chính xác class `Window`/`UserControl` nào đang được khởi tạo (`new Form2_X...`).
     *(Ví dụ thực tế: `btn5_Shapes_Click` gọi `Form2_5_SubMenuDrawShapes`, `btn6_Inserts_Click` gọi `Form2_4_SubMenuInsertContent`, `btn8_Select_Click` gọi `Form2_6_SubMenuSelectionRecognition`).*
  2. Mở đọc trực tiếp đúng tệp `.xaml` và `.xaml.cs` đang được khởi tạo đó.

### 2. Bộ lọc Phần tử Ẩn Triệt để (Absolute Visibility Filter)
* Bất kỳ phần tử UI nào (`Button`, `StackPanel`, `Grid`, `Border`, `RadioButton`...) đang có `Visibility="Collapsed"` hoặc `Visibility="Hidden"` trong XAML (và không được bật động bởi code-behind):
  * Phải được coi là **KHÔNG HIỂN THỊ** đối với người dùng thực tế.
  * Nếu một container cha bị `Visibility="Collapsed"`, toàn bộ phần tử con bên trong cũng đang bị ẩn.
  * Không bao giờ mô tả hoặc sửa nhầm vào phần tử đã bị ẩn như thể nó đang hiện trên màn hình.

### 3. Phân biệt Rõ Active Code vs. Orphan Code (Mã Mồ Côi Lịch sử)
* Do lịch sử phát triển qua nhiều phiên bản, thư mục `Forms/` tồn tại một số tệp thử nghiệm cũ không còn được gọi từ bất kỳ luồng UI nào hoặc chưa được link vào `QASmartTouch/QASmartTouch.csproj`.
* Luôn kiểm tra cả **luồng khởi tạo C#** lẫn **danh sách `<Page>`/`<Compile>` trong `QASmartTouch/QASmartTouch.csproj`**. Khi phát hiện tệp mồ côi, phải thông báo rõ cho người dùng để tránh mất thời gian sửa nhầm file không chạy.

### 4. Cam kết Thực chứng (Zero Hallucination Citations)
* Mọi nhận định về cấu trúc giao diện, nút bấm, SubMenu, loại bút, loại tẩy hay học cụ hiện có đều phải trích dẫn kèm đường dẫn tệp và vị trí dòng/hàm cụ thể để người dùng kiểm chứng được ngay.
