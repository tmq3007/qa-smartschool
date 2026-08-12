# Kế hoạch Chi tiết Nâng cấp Phân hệ Tư vấn Giáo dục & Hỗ trợ Tâm lý (SEL)
**Phân hệ Tư vấn Học đường & Hỗ trợ Tâm lý Học sinh (SEL) - Bộ quy chuẩn QA SmartClass v4.1**
*Ngày lập: 11 tháng 07 năm 2026*

---

## ═══ THÀNH PHẦN THÔNG QUA (BAN THIẾT KẾ & HỘI ĐỒNG IT) ═══

Kế hoạch nâng cấp và sửa đổi này được xây dựng bởi Trưởng ban thiết kế dự án QA Smart School, nhà phân tích thiết kế hệ thống, chuyên gia cơ sở dữ liệu và chuyên gia kiểm thử nhằm đảm bảo toàn bộ mã nguồn của phân hệ Tư vấn Giáo dục được tái cấu trúc đồng bộ, đạt chuẩn thiết kế sư phạm cao cấp và không phát sinh lỗi khi lập trình.

---

## ═══ PHẦN I: MÔ TẢ YÊU CẦU & THÔNG SỐ ĐẦU VÀO / ĐẦU RA KẾT QUẢ ═══

### 1. Đồng bộ hóa trạng thái Ca Tư vấn (Status)
*   **Yêu cầu:** Loại bỏ hoàn toàn các chuỗi trạng thái bị lỗi font (`Ch? x? lư`, `Đã lên l?ch`, `Ch? x? l`) và đồng bộ hóa các chuỗi trạng thái trên toàn hệ thống.
*   **Dữ liệu đầu vào:** Hằng số tĩnh định nghĩa trong `StatusConstants.cs`.
*   **Dữ liệu đầu ra:** Các chuỗi trạng thái quy chuẩn tiếng Việt có dấu:
    *   `Pending`: `"Chờ xử lý"`
    *   `Scheduled`: `"Đã lên lịch"`
    *   `InProgress`: `"Đang theo dõi"`
    *   `Completed`: `"Đã hoàn thành"`
*   **Biện pháp kiểm tra:** Tất cả các truy vấn đếm số lượng ca tư vấn, tạo mới ca tư vấn, và cập nhật ca tư vấn phải đối chiếu trực tiếp với các hằng số này.

### 2. Sổ theo dõi Tâm lý Học đường SEL (`SchoolCounselingView`)
*   **Yêu cầu 1: Nhận diện học sinh chính xác trong ComboBox**
    *   **Đầu vào:** Danh sách học sinh active từ DB.
    *   **Đầu ra:** ComboBox hiển thị chuỗi định dạng: `"[Mã học sinh] - [Họ tên] ([Lớp])"`.
    *   **Mục tiêu:** Tránh nhầm lẫn khi ghi nhận hồ sơ của học sinh trùng tên.
*   **Yêu cầu 2: Slider nhập điểm Mood trực quan**
    *   **Đầu vào:** Giá trị kéo của Slider từ 1 đến 10.
    *   **Đầu ra:** Điểm mood lưu xuống DB và hiển thị Emoji tâm trạng tương ứng bên cạnh:
        *   Điểm 1-3: icon 😞 (Cần chú ý khẩn cấp).
        *   Điểm 4-6: icon 😐 (Bình thường / Có áp lực nhẹ).
        *   Điểm 7-10: icon 🙂 (Tốt / Vui vẻ).
*   **Yêu cầu 3: Phát hiện từ khóa nhạy cảm không phân biệt chữ hoa/thường**
    *   **Đầu vào:** Chuỗi văn bản nhập trong TextBox Ghi chú.
    *   **Đầu ra:** Danh sách từ khóa được phát hiện chính xác dù người dùng nhập chữ viết hoa hay viết thường (ví dụ: "Trầm cảm" hay "bắt nạt").
*   **Yêu cầu 4: Đánh giá rủi ro cực đoan (Critical Risk)**
    *   **Đầu vào:** Ghi chú chứa từ khóa tự hại ("tự tử", "muốn chết", "hủy hoại bản thân").
    *   **Đầu ra:** Gán tự động mức rủi ro là `"Critical"`, hiển thị banner đỏ nhấp nháy cảnh báo cực lớn trên giao diện chi tiết học sinh và tự động gửi thông báo khẩn tới Ban Giám Hiệu.
*   **Yêu cầu 5: Ẩn nút "Gửi Cảnh báo" sau khi gửi thành công**
    *   **Đầu vào:** Thuộc tính `IsNotified` của bản ghi.
    *   **Đầu ra:** Ẩn nút "Gửi Cảnh báo" nếu `IsNotified == true` để tránh giáo viên spam gửi nhiều thông báo cảnh báo giống hệt nhau tới phụ huynh.

### 3. Thiết kế Popup nhập liệu bằng XAML chuyên nghiệp
*   **Yêu cầu:** Loại bỏ 100% các Window tạo bằng code-behind trong tệp [CounselingProfileView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Counseling/Views/CounselingProfileView.xaml.cs). Thay thế bằng hai tệp XAML mới là `CounselingProfileDialog.xaml` và `CounselingSessionDialog.xaml`.
*   **Đầu vào:** Các TextBox nhập thông tin.
*   **Đầu ra:** Validation kiểm tra nếu để trống các trường bắt buộc (Tên học sinh, Lớp, Nội dung trao đổi) thì hiển thị cảnh báo lỗi và không cho phép đóng hộp thoại để lưu dữ liệu rỗng.
*   **Thiết kế mỹ thuật:** Hộp thoại có góc bo tròn, drop shadow nhẹ, font Segoe UI và sử dụng bảng màu Teal/Indigo chuyên nghiệp của quy chuẩn v4.1.

### 4. Khắc phục lỗi kẹt điều phối & Cải tiến Dashboard
*   **Yêu cầu 1: Sửa lỗi kẹt trang chi tiết**
    *   **Đầu vào:** Sự kiện click nút "Hủy bỏ" hoặc lưu thành công ở nút "Lưu Phiên" tại màn hình chi tiết phiên tư vấn.
    *   **Đầu ra:** Gọi lệnh `NavigationService.GoBack()` để tự động quay lại màn hình danh sách chính, giúp người dùng không bị kẹt lại.
*   **Yêu cầu 2: Thống kê và vẽ biểu đồ phân phối Dashboard**
    *   **Đầu vào:** Tập dữ liệu `IssueDistribution` (Phân phối loại vấn đề) và thống kê số ca tư vấn từ dịch vụ.
    *   **Đầu ra:** Biển diễn đồ họa biểu đồ hình tròn (Pie Chart) trực quan bằng Canvas/Grid biểu diễn tỷ lệ các loại vấn đề học sinh đang gặp phải (Học tập, Gia đình, Tâm lý, Quan hệ bạn bè...) để Ban giám hiệu tiện theo dõi. Bổ sung thống kê ca "Đang theo dõi" lên Dashboard.

---

## ═══ PHẦN II: PHƯƠNG PHÁP THỰC HIỆN & PHẢN BIỆN CHI TIẾT ═══

### 1. Phản biện Thiết kế Trạng thái Ca tư vấn:
*   *Ý kiến trái chiều:* Nên lưu trạng thái bằng mã số ID trạng thái trong cơ sở dữ liệu để dễ chuẩn hóa quốc tế.
*   *Phản biện phản bác:* Vì dự án QA SmartClass v4.1 yêu cầu thiết kế tối giản phần cứng SQLite và vận hành độc lập (Solo Mode), việc tạo thêm bảng liên kết trạng thái sẽ làm phình to số lượng bảng trong CSDL và làm chậm các câu truy vấn JOIN. Việc lưu trữ trực tiếp chuỗi tiếng Việt quy chuẩn `"Chờ xử lý"`, `"Đã lên lịch"`, `"Đang theo dõi"`, `"Đã hoàn thành"` thông qua lớp hằng số `StatusConstants` là giải pháp tối ưu nhất, vừa dễ đọc trực tiếp từ CSDL vừa tiết kiệm tài nguyên hệ thống.

### 2. Phản biện Thiết kế Dialog Nhập liệu:
*   *Ý kiến trái chiều:* Dựng popup bằng C# code-behind giúp viết nhanh gọn, không cần tạo thêm tệp tin XAML và dễ dàng truyền biến trực tiếp.
*   *Phản biện phản bác:* Cách làm này vi phạm nghiêm trọng quy chuẩn phân tách giao diện (XAML) và logic xử lý (C#) của WPF. Nó làm mã nguồn phình to khó đọc, không thể thiết kế Responsive và không áp dụng được các Style tài nguyên dùng chung của dự án (ví dụ: các Style nút bấm, ô nhập liệu chuẩn v4.1). Tạo tệp XAML riêng biệt giúp coder quản lý giao diện trực quan và dễ dàng bảo trì hoặc nâng cấp giao diện sau này.

### 3. Phản biện Phương án Vẽ biểu đồ Dashboard:
*   *Ý kiến trái chiều:* Nên cài đặt thư viện LiveCharts để vẽ biểu đồ tròn chuyên nghiệp.
*   *Phản biện phản bác:* Cài đặt thư viện bên thứ ba làm tăng dung lượng file cài đặt và tiềm ẩn nguy cơ crash ứng dụng do không tương thích môi trường máy trạm của trường học. Việc tự vẽ biểu đồ hình tròn hoặc biểu đồ dải màu phân phối dạng thanh ngang (Bar Chart) bằng Canvas/Grid trực tiếp trong WPF chỉ tốn vài chục dòng code nhưng đảm bảo tốc độ phản hồi tức thì (<10ms), mượt mà tuyệt đối trên màn hình cảm ứng tương tác Smart Touch.

---

## ═══ PHẦN III: CHECKSHEET KIỂM TRA NÂNG CẤP (TỪNG BƯỚC CHI TIẾT) ═══

### Bước 1: Đồng bộ hóa cấu trúc cơ sở dữ liệu & Service
- [ ] Thêm các hằng số trạng thái vào `StatusConstants.cs` trong lớp `CounselingStatus`.
- [ ] Sửa thuộc tính `Status` trong `AppDbContext.cs` mặc định sang hằng số tĩnh mới.
- [ ] Bổ sung trường `InProgressRequests` vào thực thể `CounselingDashboardStats` của `CounselingService.cs`.
- [ ] Cập nhật hàm thống kê `GetDashboardStatsAsync` của Service sử dụng toàn bộ hằng số chuẩn và kiểm tra số liệu trùng khớp 100%.

### Bước 2: Nâng cấp Giao diện Sổ theo dõi Tâm lý SEL (`SchoolCounselingView`)
- [ ] Cập nhật ComboBox hiển thị đầy đủ thông tin học sinh: `Mã học sinh - Tên học sinh (Lớp)`.
- [ ] Thay thế TextBox MoodScore bằng Slider WPF có giới hạn từ 1-10.
- [ ] Tích hợp TextBlock hiển thị Emoji tâm trạng động thay đổi theo giá trị của Slider.
- [ ] Sửa hàm phát hiện từ khóa sang `ToLowerInvariant()` để không phân biệt hoa/thường.
- [ ] Bổ sung điều kiện rủi ro cực đoan `"Critical"` khi phát hiện các từ khóa nhạy cảm nặng và gửi cảnh báo mật.
- [ ] Điều chỉnh Visibility của nút "Gửi Cảnh báo" chỉ hiển thị khi `record.IsNotified == false`.
- [ ] Chuyển cột ghi chú của DataGrid sang `Width="*"` để co giãn tự động theo chiều rộng màn hình.

### Bước 3: Thay thế Hộp thoại động bằng XAML Dialog
- [ ] Thiết kế tệp XAML `CounselingProfileDialog.xaml` có góc bo tròn, drop shadow và bảng màu Indigo/Teal cao cấp.
- [ ] Viết validation trong `CounselingProfileDialog.xaml.cs` ngăn chặn lưu hồ sơ rỗng.
- [ ] Thiết kế tệp XAML `CounselingSessionDialog.xaml` hỗ trợ DatePicker chọn ngày hẹn tiếp theo lớn hơn hoặc bằng ngày hiện tại.
- [ ] Viết validation trong `CounselingSessionDialog.xaml.cs` ngăn chặn lưu phiên tư vấn rỗng.
- [ ] Tích hợp gọi hai Dialog XAML mới vào `CounselingProfileView.xaml.cs`.
- [ ] Sửa thuộc tính hiển thị tại `LoadSessions()` loại bỏ hoàn toàn tiền tố `??` kỳ lạ.
- [ ] Thêm thanh tìm kiếm và bộ lọc lớp ở trang XAML chính và cài đặt logic tìm kiếm thời gian thực.
- [ ] Điền thông tin thông báo lỗi chi tiết ra MessageBox ở các khối `catch` thay vì bỏ trống.

### Bước 4: Hoàn thiện Dashboard & Điều phối ca tư vấn (CounselingHub)
- [ ] Thêm thống kê số ca "Đang theo dõi" dạng Card trên giao diện Dashboard.
- [ ] Vẽ biểu đồ tròn/dải phân phối vấn đề (`IssueDistribution`) trực quan bằng Canvas/Grid chuẩn màu sắc thương hiệu.
- [ ] Thêm nhãn chỉ dẫn sử dụng chi tiết (hướng dẫn nhấp đúp chuột để xem chi tiết) dưới tiêu đề bảng yêu cầu tư vấn.
- [ ] Cập nhật sự kiện nút Hủy và Lưu tại trang chi tiết phiên tư vấn tự động quay lui màn hình trước (`NavigationService.GoBack()`).
- [ ] Đồng bộ hóa 5 danh mục phân loại vấn đề tâm lý trên tất cả các màn hình nhập liệu.
- [ ] Cải thiện độ tương phản của TextBox `TxtClassName` ở chế độ ReadOnly (bỏ nền xám tối `#E0E0E0`).
- [ ] Build thành công toàn bộ dự án với 0 lỗi biên dịch, chạy thử nghiệm hoạt động mượt mà.
