# Kế hoạch Chi tiết Nâng cấp & Cải tiến Giao diện Nhật ký An ninh và Dashboard Báo cáo Khách tham quan
**Phân hệ Nhật ký Bảo vệ & Quản lý Khách ra vào — Bộ tiêu chuẩn QA SmartClass v4.1**  
*Ngày cập nhật kế hoạch: 30 tháng 06 năm 2026*  

---

## ═══ MỤC TIÊU ═══
Khắc phục hoàn toàn các lỗi logic kỹ thuật, lỗi thiết kế cơ sở dữ liệu phi cấu trúc, lỗi UI/UX mỏi mắt do TextBox nhấp nháy ngược, đồng thời xây dựng một phân hệ báo cáo/dashboard chuyên dụng dành cho Khách tham quan để kết nối đồng bộ với toàn hệ thống. Kế hoạch này áp dụng nghiêm ngặt bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật QA SmartClass v4.1, kèm theo bộ tự phản biện tối ưu hóa và checksheet kiểm thử mở rộng ở từng bước để lập trình viên thực hiện chính xác 100%, không thể làm sai.

---

## ═══ CÁC TÙY CHỌN CẤU HÌNH HỆ THỐNG (MASTER CONFIGURATIONS) ═══

Để đáp ứng điều kiện thực tế của từng trường học, hệ thống sẽ đọc thiết lập động trong bảng `SystemSettings` để cấu hình luồng nghiệp vụ quản lý khách ra vào và an ninh trường học:

| ID Cấu hình (Key) | Mô tả cấu hình | Giá trị mặc định | Giá trị tùy chọn |
| :--- | :--- | :--- | :--- |
| `Security_Visitor_AuthLevel` | Mức độ xác thực thông tin khách khi Check-In | `"High"` | `"Simple"` (Chỉ nhập tay Họ tên & SĐT)<br>`"High"` (Bắt buộc quét CCCD gắn chip và cấp thẻ khách) |
| `Security_Visitor_Encryption` | Trạng thái mã hóa dữ liệu cá nhân nhạy cảm trong DB | `"True"` | `"True"` (Mã hóa AES-256 các trường CCCD & Địa chỉ)<br>`"False"` (Lưu văn bản thô phục vụ kết xuất nhanh) |
| `Security_Visitor_Notification` | Hình thức thông báo cho giáo viên/cán bộ khi khách đến gặp | `"InApp"` | `"Disabled"` (Không thông báo)<br>`"InApp"` (Thông báo đẩy trên app TeacherHub/StaffDashboard)<br>`"All"` (Thông báo đẩy + Gửi email/SMS tự động đến SĐT giáo viên) |
| `Security_Visitor_BlinkingStyle` | Hiệu ứng viền của ô quét QR CCCD khi chờ nhập liệu | `"Pulse"` | `"Pulse"` (Nhấp nháy xung nhịp dịu mắt khi được focus)<br>`"Static"` (Không nhấp nháy, giữ viền sáng phẳng) |
| `Security_Visitor_AutoReleaseMode` | Chế độ tự động giải phóng (Check-Out) khách quên báo khi về | `"DailyReset"` | `"Manual"` (Chỉ cho phép giải phóng thủ công bởi bảo vệ)<br>`"DailyReset"` (Tự động Check-Out tất cả khách vào lúc 22:00 hàng ngày)<br>`"Timeout"` (Tự động Check-Out sau 8 tiếng kể từ lúc Check-In) |
| `Security_Visitor_PreRegistration` | Cho phép đăng ký lịch hẹn trước cho phụ huynh/đối tác | `"Enabled"` | `"Disabled"` (Không cho phép đăng ký trước, mọi khách phải quét tại cổng)<br>`"Enabled"` (Giáo viên tạo lịch hẹn trước, sinh mã QR gửi khách quét nhanh) |
| `Security_Visitor_PrintBadge` | Tự động in thẻ khách bằng máy in nhiệt/nhãn dán mini tại cổng | `"Disabled"` | `"Disabled"` (Không in thẻ, chỉ quản lý trên phần mềm)<br>`"Enabled"` (Tự động in nhãn dán chứa thông tin và mã QR Check-Out khi khách đến) |

---

## ═══ CHI TIẾT KẾ HOẠCH NÂNG CẤP & CẢI TIẾN ═══

### Nhiệm vụ 1: Tái cấu trúc cơ sở dữ liệu bảng `SecurityLogs`
*   **Mô tả yêu cầu:** Thay thế việc lưu thông tin CCCD ghép chuỗi thô bằng cấu trúc cơ sở dữ liệu chuẩn hóa, tách biệt rõ ràng các thuộc tính cá nhân của khách.
*   **Dữ liệu đầu vào:** Thông tin thẻ CCCD từ đầu quét QR (Số định danh, Họ tên, Giới tính, Địa chỉ, ngày cấp).
*   **Dữ liệu đầu ra:** Bản ghi `SecurityLog` trong SQLite lưu trữ cấu trúc:
    *   `CccdNumber` (string): Số định danh cá nhân công dân (Mã hóa).
    *   `VisitorName` (string): Họ và tên khách tham quan.
    *   `Gender` (string): Nam/Nữ.
    *   `Address` (string): Địa chỉ thường trú (Mã hóa).
    *   `HostTeacherId` (string): Mã giáo viên/nhân viên tiếp đón.
    *   `VisitPurpose` (string): Mục đích chuyến thăm (Công tác, Gặp phụ huynh, v.v.).
    *   `BadgeNumber` (string): Số thẻ khách đeo do bảo vệ cấp phát.
    *   `IsCheckedOut` (bool): Đã ra cổng hay chưa.
*   **Phương pháp thực hiện:**
    1.  Tạo tệp migration mới bổ sung các trường dữ liệu trên vào bảng `SecurityLogs` trong SQLite.
    2.  Cập nhật lớp mô hình dữ liệu `SecurityLog` trong [Models\SecurityLog.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Models/SecurityLog.cs).
    3.  **Viết mã bóc tách dữ liệu cũ (Up migration code):** Viết logic bóc tách tự động các dòng `Description` cũ có dạng `"Khách: {name} - CCCD: {cccd}..."` bằng Regular Expression để phân bổ ngược lại vào các cột mới, bảo toàn dữ liệu lịch sử của trường học.
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc thêm nhiều cột mới và chạy Regex bóc tách chuỗi cũ khi chạy Migration có thể làm chậm quá trình khởi động ứng dụng lần đầu sau khi cập nhật nếu CSDL có hàng chục ngàn dòng.
    *   *Giải pháp tối ưu:* Thực hiện chạy Migration bất đồng bộ (Asynchronous Migration) hoặc bọc toàn bộ khối di trú trong một Transaction đơn lẻ của SQLite. Nếu bóc tách chuỗi cũ bị lỗi định dạng, đặt giá trị mặc định cho cột mới và lưu chuỗi gốc vào cột `Description` để tránh mất mát thông tin.

---

### Nhiệm vụ 2: Khắc phục lỗi nhấp nháy viền cam TextBox khi mất focus (`IsFocused = False`)
*   **Mô tả yêu cầu:** Loại bỏ hiệu ứng nhấp nháy màu cam đậm chói mắt khi TextBox quét CCCD không được chọn, đồng thời cấu hình hiệu ứng Pulse dịu mắt khi sẵn sàng làm việc theo quy chuẩn thẩm mỹ sư phạm v4.1.
*   **Dữ liệu đầu vào:** Trạng thái tiêu điểm `IsFocused` của TextBox `TxtQrInput`.
*   **Dữ liệu đầu ra:** TextBox có viền phẳng màu xám sáng mặc định khi mất focus, và nhấp nháy xung nhịp (Pulse) màu xanh dương nhẹ dịu mắt (`#3B82F6`) khi có focus.
*   **Phương pháp thực hiện:**
    1.  Mở tệp [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml).
    2.  Sửa đổi thẻ `Style` có khóa `BlinkingTextBoxStyle`:
        *   Thay thế `<Trigger Property="IsFocused" Value="False">` bằng `<Trigger Property="IsFocused" Value="True">`.
        *   Đổi đích màu của ColorAnimation từ màu cam đậm `#EA580C` sang màu xanh dương thương hiệu `#3B82F6`.
        *   Tăng `Duration` từ `0:0:0.8` lên `0:0:1.5` để nhịp co giãn mềm mại hơn, không gây mỏi mắt.
    3.  Đồng thời tích hợp cấu hình `Security_Visitor_BlinkingStyle`: Nếu cấu hình là `"Static"`, vô hiệu hóa hoàn toàn Storyboard nhấp nháy, giữ viền xanh dương phẳng khi có focus.
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Khi bảo vệ mở màn hình nhưng quên bấm chuột chọn ô quét QR, thiết bị quét (chế độ giả lập bàn phím) sẽ gõ ký tự ra ngoài màn hình. Nếu không có cảnh báo nhấp nháy khi mất focus, bảo vệ sẽ không nhận biết được là con trỏ đang nằm ngoài ô.
    *   *Giải pháp tối ưu:* Thay vì nhấp nháy TextBox khi mất focus, ta thêm một nhãn cảnh báo nhỏ màu đỏ tĩnh ở ngay phía trên: `⚠️ Vui lòng nhấp vào đây trước khi quét thẻ!` và nhãn này chỉ hiển thị khi `TxtQrInput.IsFocused == false`. Cách này vừa thu hút sự chú ý vừa không gây mỏi mắt như hiệu ứng nhấp nháy động liên tục.

---

### Nhiệm vụ 3: Xây dựng Dashboard Báo cáo Khách tham quan chuyên biệt
*   **Mô tả yêu cầu:** Thiết kế một tab báo cáo hoặc màn hình con (dashboard) riêng biệt tích hợp vào trang Báo cáo Tổng hợp để hiển thị số liệu khách tham quan.
*   **Dữ liệu đầu vào:** Bảng dữ liệu `SecurityLogs` truy vấn theo khoảng thời gian được chọn (`SelectedPeriod`).
*   **Dữ liệu đầu ra:** Các chỉ số trực quan bao gồm:
    *   Tổng số khách ghé thăm trong kỳ.
    *   Số lượng khách đang có mặt trong trường (Chưa Check-Out).
    *   Biểu đồ hình tròn phân bổ mục đích chuyến thăm (Visit Purpose).
    *   Thời gian lưu trú trung bình của khách (tính từ thời điểm Check-In đến Check-Out trong cùng một ngày).
*   **Phương pháp thực hiện:**
    1.  Trong [ReportsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/ReportsView.xaml), bổ sung một `TabControl` lớn chia làm 2 Tab: `1. Thống kê Hoạt động & Canteen` (giao diện cũ) và `2. Báo cáo Khách tham quan & An ninh` (giao diện mới).
    2.  Trong Tab thứ 2, thiết kế bố cục gồm:
        *   3 thẻ số liệu (Metrics Cards) ở trên cùng: `Lượt khách ghé thăm`, `Khách đang trong trường`, `Thời gian lưu trú TB (Phút)`.
        *   Biểu đồ tròn phân bổ mục đích (sử dụng các thẻ hình dạng lồng nhau hoặc thanh tiến trình xếp chồng).
        *   Một DataGrid phụ hiển thị danh sách khách hiện đang có mặt trong trường kèm theo tên người tiếp đón.
    3.  Cập nhật [ReportsViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/ReportsViewModel.cs) để viết các truy vấn LINQ:
        *   Đếm số lượng khách: `db.SecurityLogs.Count(l => l.EventType == "CheckIn" && l.Timestamp >= startDate && l.Timestamp <= endDate)`
        *   Đếm khách chưa ra: `db.SecurityLogs.Count(l => l.EventType == "CheckIn" && !l.IsCheckedOut && l.Timestamp >= startDate)`
        *   Tính thời gian lưu trú TB: Tính hiệu số `Timestamp` giữa dòng Check-In và Check-Out của cùng một khách trong ngày.
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tính toán thời gian lưu trú TB bằng cách quét so khớp tên khách có thể bị sai nếu trùng tên.
    *   *Giải pháp tối ưu:* So khớp dựa trên cột `CccdNumber` đã bóc tách. Nếu bản ghi không có thông tin Check-Out đối xứng, bỏ qua bản ghi đó khỏi phép tính trung bình để tránh làm sai lệch số liệu thời gian lưu trú.

---

### Nhiệm vụ 4: Sửa lỗi ProgressBar hiển thị sự cố có Maximum tĩnh là `50`
*   **Mô tả yêu cầu:** Tính toán giá trị cực đại ProgressBar động để hiển thị phần trăm phân bổ sự cố chính xác theo chuẩn trực quan hóa dữ liệu.
*   **Dữ liệu đầu vào:** Tổng số lượng sự cố trong khoảng thời gian được chọn.
*   **Dữ liệu đầu ra:** Giá trị `Maximum` của ProgressBar tự động cập nhật khớp với tổng số sự cố, tránh lỗi tràn vạch hoặc hiển thị sai tỷ lệ.
*   **Phương pháp thực hiện:**
    1.  Trong [ReportsViewModel.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/ViewModels/ReportsViewModel.cs), thêm một thuộc tính mới:
        ```csharp
        [ObservableProperty]
        private int _totalIncidentsCount = 1; // mặc định là 1 để tránh chia cho 0
        ```
    2.  Trong hàm `LoadStatsAsync()`, gán:
        ```csharp
        TotalIncidentsCount = incidentLogs.Count > 0 ? incidentLogs.Count : 1;
        ```
    3.  Trong [ReportsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/ReportsView.xaml#L152), sửa thuộc tính của ProgressBar:
        ```xml
        Maximum="{Binding DataContext.TotalIncidentsCount, RelativeSource={RelativeSource AncestorType=ItemsControl}}"
        ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Nếu tổng số sự cố là 0, đặt giá trị mặc định là 1 trong ViewModel sẽ khiến thanh ProgressBar hiển thị 0% nhưng có trục tối đa là 1. Điều này hoàn toàn chính xác về mặt hiển thị và an toàn về mặt lập trình.
    *   *Giải pháp tối ưu:* Sử dụng thuộc tính `RelativeSource` chuẩn để trỏ tới `DataContext` của View chính vì ProgressBar nằm trong một `ItemsControl.ItemTemplate` có DataContext cục bộ là phần tử con.

---

### Nhiệm vụ 5: Tách biệt và liên kết luồng đăng ký Check-In và Check-Out của khách
*   **Mô tả yêu cầu:** Cải tiến quy trình đón tiếp để bảo vệ không phải nhập lại từ đầu thông tin của khách khi họ ra về, đồng thời bắt buộc chọn mục đích và người tiếp đón.
*   **Dữ liệu đầu vào:** Danh sách khách đang trong trường; Biểu mẫu chọn tiếp tiếp đón.
*   **Dữ liệu đầu ra:** Bản ghi Check-Out tự động cập nhật trạng thái `IsCheckedOut = true` của bản ghi Check-In tương ứng, khép kín phiên làm việc.
*   **Phương pháp thực hiện:**
    1.  Bổ sung vào giao diện đón tiếp [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml) các trường:
        *   Hộp chọn Mục đích (`VisitPurpose`): `Liên hệ công tác`, `Gặp phụ huynh`, `Bảo trì thiết bị`, `Khác`.
        *   Hộp chọn Cán bộ tiếp đón (`HostTeacherId`): Tải động danh sách giáo viên từ bảng `TeacherProfiles` hỗ trợ tìm kiếm nhanh theo tên.
        *   Hộp nhập Số thẻ khách cấp (`BadgeNumber`).
    2.  Khi bảo vệ chọn loại sự kiện là `"Khách ra cổng" (CheckOut)`:
        *   Thay vì để bảo vệ gõ tên tự do, hiển thị một `ComboBox` chứa danh sách các khách đang ở trong trường (lọc từ DB những bản ghi có `EventType == "CheckIn" && !IsCheckedOut`).
        *   Khi bảo vệ chọn một vị khách từ danh sách này, hệ thống tự động điền Số CCCD, Họ tên của khách vào các ô nhập liệu, bảo vệ chỉ việc bấm nút `🛡 Ghi Nhận Sự Kiện` để hoàn tất Check-Out.
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Nếu khách làm mất thẻ hoặc quên không báo khi về, làm thế nào để giải phóng danh sách khách trong trường?
    *   *Giải pháp tối ưu:* Cung cấp một tính năng "Giải phóng thủ công" dành cho Admin hoặc Bảo vệ ca trưởng, cho phép nhấp chuột phải vào danh sách khách đang trong trường và chọn "Đánh dấu đã rời trường thủ công" kèm theo lý do để đóng phiên.

---

### Nhiệm vụ 6: Sửa lỗi cuộn co giãn và hỗ trợ xuống dòng cho dữ liệu DataGrid lịch sử
*   **Mô tả yêu cầu:** Đảm bảo toàn bộ bảng dữ liệu không bị cắt chữ và có thanh cuộn mượt mà ở mọi độ phân giải màn hình.
*   **Dữ liệu đầu vào:** Danh sách nhật ký sự kiện có trường địa chỉ và chi tiết dài.
*   **Dữ liệu đầu ra:** Giao diện có thanh cuộn đứng tự động xuất hiện, các ô dữ liệu tự động xuống hàng khi văn bản vượt quá độ rộng cột.
*   **Phương pháp thực hiện:**
    1.  Trong [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml):
        *   Bọc Grid chính của cột bên phải (dòng 188) trong một `Grid` cha có thiết lập hàng động, đảm bảo DataGrid được cấp phát chiều cao hợp lý.
        *   Không dùng ScrollViewer bọc ngoài DataGrid vì sẽ làm hỏng tính năng ảo hóa dòng (Row Virtualization). Thay vào đó, thiết lập thuộc tính `VerticalScrollBarVisibility="Auto"` trực tiếp trên điều khiển `<DataGrid>`.
        *   Cập nhật định nghĩa cột Chi tiết trong DataGrid:
            ```xml
            <DataGridTextColumn Header="Chi tiết" Binding="{Binding Description}" Width="*">
                <DataGridTextColumn.ElementStyle>
                    <Style TargetType="TextBlock">
                        <Setter Property="TextWrapping" Value="Wrap"/>
                    </Style>
                </DataGridTextColumn.ElementStyle>
            </DataGridTextColumn>
            ```
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc cho phép xuống dòng tự động (`TextWrapping="Wrap"`) sẽ làm độ cao của các dòng trong DataGrid không đều nhau, gây mất thẩm mỹ khi cuộn danh sách.
    *   *Giải pháp tối ưu:* Thiết lập thuộc tính `MaxHeight="80"` cho TextBlock trong cột Chi tiết để giới hạn chiều cao tối đa của một dòng (hiển thị tối đa 3-4 dòng văn bản), nếu dài hơn sẽ hiển thị dấu ba chấm `...` và hiển thị đầy đủ địa chỉ trong Tooltip khi di chuột qua ô đó.

---

### Nhiệm vụ 7: Thiết kế khung Hướng dẫn sử dụng từng bước (UX Guide) chuẩn sư phạm v4.1
*   **Mô tả yêu cầu:** Bổ sung khung hướng dẫn nhanh giúp nhân viên bảo vệ dễ dàng tiếp cận quy trình đón tiếp và quét mã QR CCCD.
*   **Dữ liệu đầu vào:** Nội dung hướng dẫn nghiệp vụ đón tiếp khách tham quan.
*   **Dữ liệu đầu ra:** Khung thông tin chỉ dẫn hiển thị trực quan ở vùng trống của biểu mẫu đăng ký.
*   **Phương pháp thực hiện:**
    1.  Trong [SecurityKioskView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/SecurityKioskView.xaml), thiết kế một khối `<Border>` hướng dẫn đặt ngay dưới nút Ghi Nhận Sự Kiện:
        *   Sử dụng màu nền xanh lam nhạt dịu mát `#EFF6FF`, viền xanh nhạt `#BFDBFE` và chữ màu xanh dương đậm `#1E40AF`.
        *   Nội dung chỉ dẫn:
            *   `💡 Quy trình đăng ký khách tham quan:`
            *   `Bước 1: Click chuột chọn ô quét QR CCCD phía trên.`
            *   `Bước 2: Đưa mã QR trên thẻ CCCD của khách vào trước camera đầu đọc.`
            *   `Bước 3: Chọn Mục đích viếng thăm & nhập tên Cán bộ tiếp đón.`
            *   `Bước 4: Cấp thẻ khách tương ứng và bấm nút [Ghi Nhận Sự Kiện].`

---

### Nhiệm vụ 8: Mã hóa thông tin cá nhân của khách trong cơ sở dữ liệu (AES-256 & DPAPI)
*   **Mô tả yêu cầu:** Đảm bảo các thông tin nhạy cảm như Số CCCD và Địa chỉ được mã hóa trước khi ghi vào SQLite và giải mã khi tải lên giao diện.
*   **Dữ liệu đầu vào:** Cấu hình `Security_Visitor_Encryption` từ hệ thống; Số CCCD và Địa chỉ của khách.
*   **Dữ liệu đầu ra:** Chuỗi dữ liệu mã hóa lưu trong cơ sở dữ liệu; Chuỗi giải mã hiển thị trên giao diện.
*   **Phương pháp thực hiện:**
    1.  Xây dựng lớp bảo mật [Security\EncryptionHelper.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Security/EncryptionHelper.cs) sử dụng thuật toán mã hóa đối xứng AES-256. Khóa mã hóa (Key) và vectơ khởi tạo (IV) được bảo vệ bằng cơ chế `ProtectedData` của Windows (DPAPI) để đảm bảo an toàn tuyệt đối.
    2.  Trong ViewModel, trước khi lưu bản ghi vào cơ sở dữ liệu:
        *   Nếu cấu hình `Security_Visitor_Encryption` là `"True"`: gọi hàm mã hóa `EncryptionHelper.Encrypt(CccdNumber)` và `EncryptionHelper.Encrypt(Address)`.
    3.  Khi tải danh sách lịch sử lên giao diện, thực hiện giải mã ngược lại trước khi gán dữ liệu vào thuộc tính hiển thị.
*   **Phương pháp phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc mã hóa toàn bộ dữ liệu CCCD sẽ khiến chức năng tìm kiếm lịch sử theo số CCCD của bảo vệ bị vô hiệu hóa vì không thể thực hiện truy vấn so khớp chuỗi thô.
    *   *Giải pháp tối ưu:* Tạo thêm cột `CccdHash` trong bảng `SecurityLogs` chứa mã băm một chiều SHA-256 của số CCCD (không thể dịch ngược nhưng là duy nhất cho mỗi số CCCD). Khi bảo vệ nhập số CCCD vào ô tìm kiếm, hệ thống sẽ thực hiện băm SHA-256 số đó và thực hiện truy vấn so khớp chính xác trên cột `CccdHash`. Điều này giúp vừa bảo mật tuyệt đối vừa giữ nguyên hiệu năng tìm kiếm nhanh của cơ sở dữ liệu.

---

## ═══ BỘ CHECKSHEET KIỂM THỬ MỞ RỘNG (EXPANDED TEST CASES) ═══

Lập trình viên và kiểm thử viên bắt buộc phải chạy qua toàn bộ danh sách kiểm tra này trước khi bàn giao sản phẩm nâng cấp:

### 1. Kiểm thử bóc tách dữ liệu CCCD & Luồng nghiệp vụ Check-In
*   `[ ]` **TC_VIS_01 (Tự động điền CCCD):** Quét mã QR CCCD gắn chip hợp lệ. Kết quả: Số định danh tự động điền vào ô Số CCCD, Họ tên điền vào ô Họ tên, Giới tính điền vào ô Giới tính, Địa chỉ điền vào ô Địa chỉ. Các thông tin này không được hiển thị gộp chung trong ô Mô tả sự kiện.
*   `[ ]` **TC_VIS_02 (Ràng buộc trường bắt buộc):** Để trống ô Họ tên hoặc Người tiếp đón rồi bấm Lưu. Kết quả: Hệ thống hiển thị hộp thoại thông báo lỗi tiếng Việt chuẩn: "Vui lòng nhập họ tên khách và cán bộ tiếp đón." và không cho phép lưu bản ghi.
*   `[ ]` **TC_VIS_03 (Quét QR CCCD sai định dạng):** Quét một mã QR không đúng định dạng CCCD gắn chip của Việt Nam. Kết quả: Hệ thống bọc lỗi an toàn, hiển thị thông báo: "⚠️ Mã QR không đúng định dạng thẻ CCCD gắn chip. Vui lòng kiểm tra lại hoặc nhập tay." Không được xảy ra hiện tượng treo hay tắt ứng dụng đột ngột.

### 2. Kiểm thử luồng Check-Out đối xứng
*   `[ ]` **TC_VIS_OUT_01 (Danh sách khách đang trong trường):** Bấm chọn loại sự kiện "Khách ra cổng". Kết quả: ComboBox danh sách khách xuất hiện và chỉ hiển thị những vị khách đã Check-In hôm nay nhưng chưa Check-Out.
*   `[ ]` **TC_VIS_OUT_02 (Tự động điền Check-Out):** Chọn một vị khách cụ thể từ danh sách khách đang trong trường. Kết quả: Hệ thống tự điền Số CCCD, Họ tên, Số thẻ khách của người đó vào biểu mẫu.
*   `[ ]` **TC_VIS_OUT_03 (Đóng phiên viếng thăm):** Bấm Lưu Check-Out. Kết quả: Bản ghi Check-In tương ứng trong cơ sở dữ liệu được cập nhật thuộc tính `IsCheckedOut = true`. Vị khách đó biến mất khỏi ComboBox danh sách khách đang trong trường.

### 3. Kiểm thử thẩm mỹ & Hiệu ứng giao diện (Nhiệm vụ 2 & 6)
*   `[ ]` **TC_UI_01 (TextBox mất focus):** Mở giao diện Nhật ký bảo vệ, click chuột ra ngoài ô quét QR. Kết quả: TextBox quét QR không còn nhấp nháy màu cam đậm. Viền TextBox giữ màu xám mặc định.
*   `[ ]` **TC_UI_02 (TextBox nhận focus):** Click chuột vào ô quét QR. Kết quả: Viền TextBox chuyển sang màu xanh dương và thực hiện hiệu ứng Pulse co giãn độ sáng mềm mại (chu kỳ 1.5 giây).
*   `[ ]` **TC_UI_03 (Xu xuống dòng cột Chi tiết):** Tạo một bản ghi khách có địa chỉ thường trú dài (120 ký tự). Xem trên DataGrid lịch sử ở độ phân giải 1024x768. Kết quả: Ô địa chỉ tự động xuống hàng hiển thị đầy đủ, không bị cắt chữ ở biên cột.
*   `[ ]` **TC_UI_04 (Thanh cuộn đứng):** Thu nhỏ cửa sổ ứng dụng về độ cao tối thiểu (600px). Kết quả: Xuất hiện thanh cuộn đứng ở bảng lịch sử cho phép cuộn xem đầy đủ danh sách, không có phần tử nào bị tràn ra ngoài màn hình.

### 4. Kiểm thử Dashboard Báo cáo Khách tham quan (Nhiệm vụ 3 & 4)
*   `[ ]` **TC_REP_01 (Thẻ chỉ số hoạt động):** Truy cập trang Báo cáo Tổng hợp, chọn tab "Báo cáo Khách tham quan". Kết quả: Thẻ chỉ số hiển thị chính xác tổng số lượt khách trong ngày và số khách hiện còn đang ở trong trường khớp 100% với bảng nhật ký bảo vệ.
*   `[ ]` **TC_REP_02 (Biểu đồ phân bổ mục đích):** Ghi nhận 3 khách có mục đích "Liên hệ công tác" và 1 khách có mục đích "Gặp phụ huynh". Kiểm tra biểu đồ mục đích. Kết quả: Biểu đồ cập nhật chính xác tỷ lệ phân hóa.
*   `[ ]` **TC_REP_03 (Động hóa ProgressBar):** Ghi nhận tổng số sự cố an ninh trong ngày là 8 vụ. Kiểm tra biểu đồ sự cố. Kết quả: Vạch tiến trình ProgressBar hiển thị đúng tỷ lệ phần trăm tương đối trên thang cực đại là 8, không bị cứng nhắc ở giá trị 50.

### 5. Kiểm thử Bảo mật & Mã hóa dữ liệu (Nhiệm vụ 8)
*   `[ ]` **TC_SEC_01 (Mã hóa cơ sở dữ liệu):** Lưu thông tin khách mới khi cấu hình `Security_Visitor_Encryption` = `"True"`. Sử dụng DB Browser mở trực tiếp tệp SQLite `smartclass.db`. Kết quả: Nội dung trong cột `CccdNumber` và `Address` hiển thị dưới dạng chuỗi nhị phân mã hóa không thể đọc thô. Cột `VisitorName` hiển thị dạng văn bản thô để phục vụ tìm nhanh.
*   `[ ]` **TC_SEC_02 (Giải mã hiển thị):** Kiểm tra danh sách lịch sử khách trên ứng dụng. Kết quả: Số CCCD và Địa chỉ hiển thị dưới dạng văn bản giải mã tiếng Việt chuẩn có dấu, không bị vỡ font chữ.
*   `[ ]` **TC_SEC_03 (Băm SHA-256 tìm kiếm):** Nhập số CCCD của khách vào ô tìm kiếm. Kết quả: Hệ thống tìm kiếm thành công bản ghi tương ứng thông qua chỉ mục băm `CccdHash` trong thời gian dưới 100ms.

### 6. [MỚI] Kiểm thử Di trú & Đồng bộ dữ liệu cũ (Nhiệm vụ 1)
*   `[ ]` **TC_MIG_01 (Di trú chuỗi cũ chuẩn):** SQLite chứa bản ghi cũ có `Description` = `"Khách: Nguyễn Văn An - CCCD: 079090012345 - Giới tính: Nam - Địa chỉ: 123 Nguyễn Trãi, Quận 5"`. Chạy nâng cấp hệ thống. Kết quả: Dữ liệu di trú thành công, cột `VisitorName` = `"Nguyễn Văn An"`, `CccdNumber` = `"079090012345"`, `Gender` = `"Nam"`, `Address` = `"123 Nguyễn Trãi, Quận 5"`.
*   `[ ]` **TC_MIG_02 (Di trú chuỗi cũ lỗi cấu trúc):** Bản ghi cũ có `Description` = `"Khách vào liên hệ sửa điện thoại bàn"` (không khớp định dạng bóc tách CCCD). Chạy nâng cấp. Kết quả: Di trú thành công, hệ thống đặt `VisitorName` = `"Khách vãng lai"`, `CccdNumber` = `null`, và giữ nguyên mô tả thô tại cột `Description`. Ứng dụng không bị crash.

### 7. [MỚI] Kiểm thử Đăng ký trước lịch hẹn (Pre-registration)
*   `[ ]` **TC_PRE_01 (Cấu hình Disabled):** Đặt `Security_Visitor_PreRegistration` = `"Disabled"`. Bảo vệ quét mã QR lịch hẹn do giáo viên gửi phụ huynh. Kết quả: Hệ thống báo lỗi: "Chức năng đăng ký trước đang bị khóa. Vui lòng làm thủ tục quét CCCD tại cổng."
*   `[ ]` **TC_PRE_02 (Quét lịch hẹn hợp lệ):** Cấu hình `Enabled`. Bảo vệ quét QR lịch hẹn hợp lệ trên điện thoại của phụ huynh. Kết quả: Điền tự động chính xác: Họ tên, Cán bộ tiếp đón, Mục đích (ví dụ: "Gặp GVCN lớp 10A1"). Trạng thái lịch hẹn trong DB chuyển sang "Đã đến".
*   `[ ]` **TC_PRE_03 (Quét lịch hẹn hết hạn):** Lịch hẹn được đăng ký vào ngày hôm qua. Khách quét mã vào hôm nay. Kết quả: Hệ thống hiển thị cảnh báo: "⚠️ Mã QR lịch hẹn này đã hết hạn (chỉ có giá trị trong ngày đăng ký). Vui lòng quét thẻ CCCD để đăng ký mới."

### 8. [MỚI] Kiểm thử Tự động giải phóng phiên (Auto-Release)
*   `[ ]` **TC_REL_01 (Giải phóng thủ công):** Danh sách khách trong trường chứa phụ huynh Nguyễn Văn A. Bảo vệ nhấp chuột phải, chọn "Check-Out thủ công" và nhập lý do "Khách quên quét thẻ ra về". Kết quả: Trạng thái cập nhật thành công `IsCheckedOut = true`, số thẻ khách được giải phóng về kho để cấp cho người sau.
*   `[ ]` **TC_REL_02 (Tự động giải phóng DailyReset):** Đặt `Security_Visitor_AutoReleaseMode` = `"DailyReset"`. Tạo khách Check-In lúc 15:00. Đổi giờ hệ thống qua 22:01. Kết quả: Luồng nền (Background Job) tự động cập nhật bản ghi thành `IsCheckedOut = true` với lý do "Tự động checkout hệ thống lúc 22:00".
*   `[ ]` **TC_REL_03 (Tự động giải phóng Timeout):** Đặt `Security_Visitor_AutoReleaseMode` = `"Timeout"`. Tạo khách Check-In lúc 08:00. Đổi giờ hệ thống qua 16:01 (quá 8 tiếng). Kết quả: Bản ghi tự động được đóng phiên, giải phóng thẻ khách tương ứng.

### 9. [MỚI] Kiểm thử In ấn thẻ khách (Print Badge)
*   `[ ]` **TC_PRN_01 (In thẻ khi đăng ký):** Đặt `Security_Visitor_PrintBadge` = `"Enabled"`. Thực hiện Check-In thành công cho khách. Kết quả: Gửi lệnh in thành công đến máy in nhãn, in ra thẻ chứa Logo trường, Tên khách, Số thẻ cấp, Giờ vào, Người gặp và Mã QR Check-Out.
*   `[ ]` **TC_PRN_02 (Xử lý lỗi máy in):** Rút cáp USB máy in nhiệt. Thực hiện Check-In. Kết quả: Hệ thống ghi nhận khách thành công vào SQLite, hiển thị thông báo nhẹ: "Không kết nối được máy in thẻ khách, vui lòng cấp thẻ nhựa thay thế" mà không làm treo hay gián đoạn luồng Check-In của bảo vệ.

### 10. [MỚI] Kiểm thử Thông báo Đón tiếp (Notifications)
*   `[ ]` **TC_NOT_01 (Thông báo InApp):** Đặt `Security_Visitor_Notification` = `"InApp"`. Check-In khách đến gặp "Cô Lan". Kết quả: Trình duyệt của giáo viên hoặc tài khoản `TeacherHub` của Cô Lan nhận ngay một thông báo đẩy góc màn hình: "🔔 Khách [Tên khách] đang chờ cô ở phòng bảo vệ cổng C1."
*   `[ ]` **TC_NOT_02 (Thông báo All):** Đặt `Security_Visitor_Notification` = `"All"`. Check-In khách. Kết quả: Hệ thống ghi nhận thành công và tự động gọi API gửi Email/SMS đến số điện thoại của giáo viên tiếp đón đăng ký trên hệ thống.

---

## ═══ KẾ HOẠCH XÁC MINH & KIỂM THỬ TÍCH HỢP (VERIFICATION PLAN) ═══

### Kiểm thử tự động (Automated Testing)
*   Bổ sung bộ unit test tích hợp kiểm tra quy trình mã hóa và giải mã dữ liệu khách ra vào:
    `dotnet test QASmartClass.Tests --filter FullyQualifiedName~VisitorEncryptionTests`
*   Kiểm tra tính toàn vẹn của SQLite khi chạy nâng cấp Schema di trú dữ liệu:
    `dotnet test QASmartClass.Tests --filter FullyQualifiedName~DatabaseMigrationTests`
*   Kiểm tra logic giải phóng phiên tự động và đăng ký lịch hẹn trước:
    `dotnet test QASmartClass.Tests --filter FullyQualifiedName~VisitorWorkflowTests`

### Kiểm thử thực tế (Manual UAT)
*   Nhà trường bàn giao sản phẩm nâng cấp cho tổ bảo vệ chạy thử nghiệm thực tế với đầu đọc QR CCCD USB trong 3 ngày làm việc để đánh giá độ mỏi mắt và tính ổn định trước khi phát hành chính thức.
