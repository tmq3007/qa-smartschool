---
trigger: always_on
---

# 🎯 QUY CHUẨN KIỂM SOÁT ĐỘ CHÍNH XÁC GIAO DIỆN (UI RUNTIME VERIFICATION)

## [RULE] QC_4.2_UI_RUNTIME_ACCURACY: Nguyên tắc Xác thực Giao diện Thực tế

Mọi phân tích, đánh giá, kế hoạch hoặc đề xuất liên quan đến giao diện người dùng của **QA Smart Touch (Whiteboard - Form2)** bắt buộc phải tuân thủ nghiêm ngặt 4 nguyên tắc sau:

### 1. Truy vết từ Mã nguồn Thực thi (Runtime Code-Behind Trace)
* Tuyệt đối không được phỏng đoán giao diện dựa trên tên tệp tin hoặc các tài liệu dự án cũ (`docs/`, `TECH_DEBT.md`,...).
* Mỗi khi đề cập đến một nút bấm hoặc menu trên Toolbar chính, bắt buộc phải:
  1. Kiểm tra sự kiện click tương ứng trong `Form2_MainDashboard.Tools.cs` (hoặc code-behind đang active) để xác định chính xác class Window/Control nào đang được gọi ra màn hình (ví dụ: `btn8_Select` gọi `Form2_6` chứ không phải `Form2_9`).
  2. Mở chính xác tệp XAML mà code C# đang khởi tạo (`new Form2_X...`).

### 2. Bộ lọc Cờ ẩn Triệt để (Absolute Visibility Filter)
* Bất kỳ phần tử UI nào (Button, StackPanel, Grid, RadioButton...) có thuộc tính `Visibility="Collapsed"` hoặc `Visibility="Hidden"`:
  * Phải được xem là **HOÀN TOÀN KHÔNG TỒN TẠI** đối với mắt người dùng trên màn hình.
  * Nếu một container cha bị `Visibility="Collapsed"`, toàn bộ các nút con bên trong đều bị coi là không hiển thị.
  * Tuyệt đối không được liệt kê, mô tả hoặc đưa các thành phần đã ẩn này vào kế hoạch tối ưu giao diện như thể chúng đang hiển thị.

### 3. Phân biệt Rõ ràng: Active Code vs Orphan Code (Mã mồ côi)
* Trong dự án có nhiều tệp tin mang tên tương tự nhau do lịch sử phát triển (ví dụ: `Form2_6` vs `Form2_9`, `Form2_3` vs `Form2_5`):
  * Chỉ phân tích tệp tin đang được liên kết thực sự với giao diện chính.
  * Khi phát hiện tệp tin mồ côi (không còn được gọi bởi bất kỳ luồng UI nào), phải thông báo rõ ràng cho người dùng biết đó là code mồ côi/thử nghiệm cũ.

### 4. Cam kết Thực chứng (Zero Hallucination Guarantee)
* Mọi nhận định về số lượng nút, loại bút, loại tẩy, chức năng hiện có trên bảng viết đều phải được trích dẫn kèm số dòng và tên tệp tin XAML cụ thể để người dùng có thể kiểm chứng ngay lập tức.
