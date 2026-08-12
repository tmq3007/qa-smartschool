# KẾ HOẠCH NÂNG CẤP CHI TIẾT: CẢI TIẾN TRẢI NGHIỆM TƯƠNG TÁC (MICRO UX) PHÂN HỆ BẢO VỆ
**Đơn vị lập:** Trưởng ban Thiết kế dự án QA Smart School
**Tiêu chuẩn áp dụng:** Bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật QA SmartClass v4.2
**Trạng thái:** Chờ Duyệt (Pending Approval)

---

## ═══ MỤC TIÊU NÂNG CẤP ═══
Tối ưu hóa các tương tác nhỏ nhất (Micro UX) của Nhân viên Bảo vệ để tăng tốc độ xử lý sự vụ trực cổng, giảm bớt thao tác dùng chuột thủ công, cung cấp phản hồi âm thanh (audio feedback) giúp giảm thiểu sai sót khi nhập liệu.

---

## ═══ BẢN ĐỒ TỔNG THỂ CÁC BƯỚC THỰC HIỆN ═══

| Bước | Nội dung cải tiến | File ảnh hưởng | Trách nhiệm kiểm tra |
| :--- | :--- | :--- | :--- |
| **B1** | Tự động Focus nâng cao thông qua Dispatcher | `SecurityKioskView.xaml.cs` | UI/UX & QA Tester |
| **B2** | Tích hợp hệ thống phím tắt chuyển sự kiện F1-F4 | `SecurityKioskView.xaml`, `xaml.cs` | UI/UX & Coder |
| **B3** | Tích hợp âm thanh phản hồi nghiệp vụ (Sound Cue) | `SecurityKioskViewModel.cs` | Sound Quality & QA |

---

## ═══ PHẦN I: THIẾT KẾ KỸ THUẬT CHI TIẾT ═══

### Bước 1: Tự động Focus nâng cao thông qua Dispatcher
*   **Yêu cầu:** Đảm bảo ô nhập QR CCCD `TxtQrInput` luôn được kích hoạt con trỏ nhập liệu ngay khi bảo vệ mở màn hình nhật ký bảo vệ hoặc chuyển từ màn hình khác sang.
*   **Phương pháp thực hiện:**
    *   Mở tệp [SecurityKioskView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml.cs).
    *   Sửa đổi khối xử lý sự kiện `Loaded` bằng cách đưa lệnh `Focus` vào hàng đợi Dispatcher với độ ưu tiên `Input`:
        ```csharp
        Loaded += async (s, e) => 
        { 
            Dispatcher.BeginInvoke(new Action(() => TxtQrInput.Focus()), System.Windows.Threading.DispatcherPriority.Input);
            if (DataContext is ViewModels.SecurityKioskViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
        ```
*   **Phản biện hệ thống:** Chạy trực tiếp `TxtQrInput.Focus()` đồng bộ đôi khi bị mất tác dụng do WPF chưa hoàn tất kết xuất đồ họa (render) tại thời điểm kích hoạt sự kiện `Loaded`. Sử dụng Dispatcher giúp trì hoãn tác vụ focus cho đến khi UI sẵn sàng nhận dữ liệu đầu vào.

---

### Bước 2: Tích hợp phím tắt nhanh chuyển sự kiện F1 - F4
*   **Yêu cầu:** Cho phép bấm phím nóng để thay đổi loại sự kiện trực cổng:
    *   `F1`: Khách vào cổng (`CheckIn`)
    *   `F2`: Khách ra cổng (`CheckOut`)
    *   `F3`: Bàn giao chìa khóa (`KeyExchange`)
    *   `F4`: Sự cố an ninh (`Incident`)
*   **Phương pháp thực hiện:**
    *   **Trong XAML** [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml): Đăng ký sự kiện nhấn phím toàn cục tại gốc UserControl:
        ```xml
        <UserControl ... PreviewKeyDown="UserControl_PreviewKeyDown">
        ```
    *   **Trong Code-behind** [SecurityKioskView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml.cs): Bổ sung phương thức xử lý sự kiện:
        ```csharp
        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is ViewModels.SecurityKioskViewModel vm)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.F1:
                        vm.SelectedEventType = "CheckIn";
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.F2:
                        vm.SelectedEventType = "CheckOut";
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.F3:
                        vm.SelectedEventType = "KeyExchange";
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.F4:
                        vm.SelectedEventType = "Incident";
                        e.Handled = true;
                        break;
                }
            }
        }
        ```
*   **Phản biện hệ thống:** Sự kiện `PreviewKeyDown` đánh chặn phím từ ngoài vào trong (Tunneling event), đảm bảo phím nóng F1-F4 được kích hoạt ngay cả khi con trỏ đang nằm sâu trong các TextBox. Thiết lập `e.Handled = true` ngăn chặn phím nóng làm kích hoạt các chức năng trợ giúp mặc định của Windows.

---

### Bước 3: Tích hợp âm thanh phản hồi nghiệp vụ (Sound Cue)
*   **Yêu cầu:** Phát âm thanh cảnh báo ngắn (Bíp) để xác nhận thao tác thành công mà bảo vệ không cần dán mắt vào màn hình. Phát âm thanh cảnh báo lỗi khi lưu thất bại.
*   **Phương pháp thực hiện:**
    *   Sử dụng thư viện hệ thống Windows `System.Media.SystemSounds`.
    *   **Khi quét QR thành công** tại phương thức `OnQrInputBufferChanged` của [SecurityKioskViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/SecurityKioskViewModel.cs):
        ```csharp
        System.Media.SystemSounds.Asterisk.Play();
        ```
    *   **Khi ghi nhận sự kiện thành công** tại phương thức `SaveAsync`:
        ```csharp
        System.Media.SystemSounds.Asterisk.Play();
        ```
    *   **Khi lưu thất bại** (Lỗi bỏ trống tên/mô tả hoặc exception):
        ```csharp
        System.Media.SystemSounds.Hand.Play();
        ```
*   **Phản biện hệ thống:** Sử dụng tiếng chuông hệ thống có sẵn của Windows (`SystemSounds`) giúp phân hệ có âm thanh báo hiệu ngay tức thì mà không cần cài đặt thêm tài nguyên tập tin `.wav` hay `.mp3` cồng kềnh vào mã nguồn dự án.

---

## ═══ PHẦN II: CHECKSHEET KIỂM TRA CHO KIỂM THỬ (QA CHECKSHEET) ═══

- [ ] **Check-Focus:** Khi mở ứng dụng lần đầu và kích hoạt tab Nhật ký Bảo vệ, xác nhận con trỏ nhập liệu tự nhấp nháy tại ô nhập QR CCCD.
- [ ] **Check-Switch-Focus:** Chuyển qua tab "Giám sát cổng trường C1" rồi quay lại tab "Nhật ký Bảo vệ", xác nhận con trỏ vẫn tự động Focus thành công.
- [ ] **Check-F1:** Bấm phím `F1`, ComboBox đổi sang "Khách vào cổng".
- [ ] **Check-F2:** Bấm phím `F2`, ComboBox đổi sang "Khách ra cổng".
- [ ] **Check-F3:** Bấm phím `F3`, ComboBox đổi sang "Bàn giao chìa khóa".
- [ ] **Check-F4:** Bấm phím `F4`, ComboBox đổi sang "Sự cố an ninh".
- [ ] **Check-Sound-Success:** Quét mã QR CCCD hợp lệ, máy tính phát âm thanh "Asterisk" (tiếng bíp nhẹ).
- [ ] **Check-Sound-Save:** Nhấn nút ghi nhận sự kiện thành công, máy tính phát âm thanh báo thành công.
- [ ] **Check-Sound-Error:** Bấm nút lưu khi chưa điền thông tin (lỗi validation), máy tính phát âm thanh "Hand" (tiếng chuông lỗi của Windows).

---

**TRƯỞNG BAN THIẾT KẾ DỰ ÁN QA SMART SCHOOL**
*(Đã ký và đóng dấu phê duyệt)*
