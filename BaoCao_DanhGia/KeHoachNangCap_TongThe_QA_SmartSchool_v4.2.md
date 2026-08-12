# BẢN THIẾT KẾ & KẾ HOẠCH NÂNG CẤP TỔNG THỂ HỆ THỐNG - QA SMART SCHOOL (BẢN 4.2)

*Tài liệu kỹ thuật tối mật dành cho Kiến trúc sư Hệ thống, Lập trình viên (Coder) và Kiểm thử viên (QA/QC)*
*Được phê duyệt bởi Trưởng ban thiết kế dự án QA Smart School*

---

## 🎯 MỤC TIÊU & TẦM NHÌN HỆ THỐNG
Bản kế hoạch nâng cấp tổng thể **Bản 4.2** đồng bộ hóa toàn bộ 11 hạng mục cải tiến kỹ thuật cốt lõi, quy chuẩn thích ứng theo 3 cấp học tại Việt Nam, tối ưu cấu hình phần mềm theo 3 mô hình hạ tầng phòng máy từ khó khăn đến hiện đại, và giải quyết triệt để lỗi hiển thị font chữ học thuật (đặc biệt là chức năng vẽ vectơ và phiên âm tiếng Anh IPA). 

Tài liệu được biên soạn cực kỳ tỷ mỉ để làm cẩm nang hướng dẫn thực thi, đảm bảo **coder không thể lập trình sai** và **kiểm thử viên có bộ checksheet chi tiết từng bước** để nghiệm thu chất lượng sản phẩm.

---

## 📂 SƠ ĐỒ CẤU TRÚC THƯ MỤC ẢNH HƯỞNG
Lập trình viên cần định vị chính xác các file mã nguồn và tệp cấu hình trước khi chỉnh sửa:
*   **Cấu hình hệ thống:** `QASmartClass/StudentClient/Models/settings.json` (tệp cấu hình client).
*   **Giao diện & Styles (XAML):** [StudentSubmitPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentSubmitPage.xaml), `App.xaml` (tài nguyên dùng chung).
*   **Logic điều khiển (C#):** [StudentSubmitPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentSubmitPage.xaml.cs), [StudentFileTransfer.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Services/StudentFileTransfer.cs), `AppDbContext.cs` (kết nối SQLite).
*   **Phân hệ chuyên môn:** `QASmartClass/LearningTools/` (vẽ vectơ, công cụ phương trình bậc 2).

---

## ══ PHẦN 1: BỘ QUY CHUẨN THIẾT KẾ SƯ PHẠM & RÀNG BUỘC KỸ THUẬT (QA SMARTCLASS V4.2) ══

Mọi tính năng nâng cấp phải tuân thủ nghiêm ngặt **5 Ràng buộc thép** của môi trường giáo dục:
1.  **Ràng buộc phần cứng yếu**: Ứng dụng phải hoạt động mượt mà trên RAM 4GB, CPU đời cũ, ổ HDD cơ học. Không cài đặt các thư viện đồ họa nặng làm chậm thời gian tải ứng dụng.
2.  **Ràng buộc mạng LAN chập chờn**: Ứng dụng phải tự phát hiện mất mạng ngầm (sau 6 lần ping liên tiếp thất bại) và tự chuyển sang chế độ tự học ngoại tuyến (Offline Mode), xếp hàng gửi file ngầm qua thư mục `PendingSync` mà không gây crash hay gián đoạn học sinh.
3.  **Ràng buộc đóng băng ổ cứng (DeepFreeze)**: Các file cấu hình `settings.json`, SQLite database và dữ liệu hàng đợi phải được đặt tại các phân vùng ngoại lệ không đóng băng hoặc được sao lưu nhanh về máy chủ giáo viên.
4.  **Ràng buộc cách ly dữ liệu giữa các ca học**: Học sinh ca sau dùng chung máy không thể đọc trộm file tạm nháp `.tmp` hay xem lịch sử nhận file của học sinh ca trước. Bản nháp bắt buộc phải mã hóa bằng Windows DPAPI kết hợp khóa đối xứng AES-256 dựa trên mã học sinh `StudentCode`.
5.  **Ràng buộc tránh cướp tiêu điểm (Focus Protection)**: Không hiển thị hộp thoại pop-up chặn bàn phím (`ShowDialog()`) khi học sinh đang gõ bài viết. Tất cả thông báo nhận tài liệu, mất mạng phải được đưa về Toast Notification tự ẩn ở góc màn hình.

---

## ══ PHẦN 2: CHI TIẾT 11 HẠNG MỤC CẢI TIẾN KỸ THUẬT ══

### Hạng mục 1: Cấu hình cổng mạng động `FILE_PORT` qua tệp cấu hình bên ngoài
*   **Yêu cầu thiết kế**: Loại bỏ hằng số hardcode `FILE_PORT = 29879`. Cho phép cấu hình qua tệp `settings.json` đặt tại thư mục chạy chương trình. Fallback về cổng mặc định nếu file cấu hình bị lỗi hoặc giá trị sai dải.
*   **Dữ liệu đầu vào**: File `settings.json` chứa `{"FilePort": 29879}`.
*   **Dữ liệu đầu ra**: Biến kiểu số nguyên `int` đại diện cho cổng kết nối của TCP Socket.
*   **Phương pháp thực hiện**:
    1.  Tạo class `ClientConfiguration` ánh xạ các trường trong `settings.json`.
    2.  Dùng `File.Exists()` để kiểm tra, nếu chưa có thì tự động tạo file mẫu với giá trị mặc định.
    3.  Bọc lệnh phân tích cú pháp bằng khối `try-catch`. Sử dụng `System.Text.Json` để deserialize.
    4.  Kiểm tra điều kiện biên: `if (port < 1024 || port > 65535) port = 29879;`
*   **Phản biện & Tối ưu hóa**:
    *   *Phản biện*: Tại sao không lưu cấu hình trong Registry?
    *   *Tối ưu*: Máy tính trường học thường bị khóa quyền Administrator nên ứng dụng không có quyền ghi Registry. Đọc/ghi tệp `settings.json` cục bộ trong thư mục ứng dụng là giải pháp an toàn và tương thích 100% với môi trường đóng băng ổ cứng.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] File `settings.json` được tạo tự động khi khởi động app lần đầu tiên.
    - [ ] [ ] Thay đổi cổng thành `35000` trong file json và khởi chạy thành công kết nối TCP qua cổng này.
    - [ ] [ ] Điền giá trị `"hacked"` hoặc `999999` vào trường `FilePort`, app tự phục hồi về cổng `29879` mà không gây crash.

---

### Hạng mục 2: Tự động dọn dẹp các tệp ngoại tuyến quá hạn trong thư mục `PendingSync`
*   **Yêu cầu thiết kế**: Tự động xóa các file đã đồng bộ thành công lưu trên đĩa máy trạm sau 7 ngày, và xóa file chưa đồng bộ sau 14 ngày để bảo toàn dung lượng đĩa cứng phòng máy.
*   **Dữ liệu đầu vào**: Danh sách file trong thư mục `PendingSync/`. Trạng thái gửi tệp ghi nhận trong CSDL SQLite cục bộ.
*   **Dữ liệu đầu ra**: Xóa vật lý tệp tin và cập nhật log dọn dẹp.
*   **Phương pháp thực hiện**:
    1.  Tạo dịch vụ chạy ngầm `DirectoryCleanupService` kích hoạt khi ứng dụng khởi chạy.
    2.  Quét toàn bộ file có đuôi `.docx`, `.pdf`, `.png` trong `PendingSync`.
    3.  Đối chiếu trạng thái của tệp tin với bảng `FileTransfers` trong CSDL SQLite cục bộ:
        *   Nếu tệp tin có `Status = 'Sent'` và `DateTime.Now - File.GetLastWriteTime > 7 ngày`: Thực hiện `File.Delete(filePath)`.
        *   Nếu tệp tin chưa đồng bộ thành công nhưng `DateTime.Now - File.GetLastWriteTime > 14 ngày`: Thực hiện xóa bỏ bắt buộc.
*   **Phản biện & Tối ưu hóa**:
    *   *Phản biện*: Việc xóa file chưa đồng bộ sau 14 ngày có làm mất bài học sinh không?
    *   *Tối ưu*: Sau 14 ngày, buổi học đã kết thúc và điểm số đã được nhập thủ công. Lưu trữ lâu hơn chỉ làm tăng nguy cơ rò rỉ dữ liệu chéo ca học và đầy ổ cứng.
    *   *Phòng ngừa lỗi*: Bọc lệnh xóa trong hàm kiểm tra tệp có bị khóa bởi tiến trình khác không (`IsFileLocked`).
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Tạo file giả lập có ngày tạo cách đây 10 ngày trong `PendingSync` (đã đồng bộ), kiểm tra file có bị xóa khi chạy app.
    - [ ] [ ] Tạo file giả lập có ngày tạo cách đây 15 ngày (chưa đồng bộ), kiểm tra file có bị xóa bắt buộc.
    - [ ] [ ] Bọc lệnh xóa trong `try-catch` để đảm bảo file đang bị tiến trình khác mở không gây crash app.

---

### Hạng mục 3: Thay thế emoji Unicode thô sơ bằng Vector Path SVG sắc nét
*   **Yêu cầu thiết kế**: Nâng cao thẩm mỹ giao diện bằng cách thay các emoji như 📂, ⚠️, ⏳ bằng các icon vẽ bằng vector hình học SVG có hiệu ứng đổi màu khi hover hoặc chọn tab.
*   **Dữ liệu đầu vào**: Khai báo chuỗi hình học SVG Geometry.
*   **Dữ liệu đầu ra**: Giao diện hiển thị sắc nét trên các màn hình High DPI (2K, 4K).
*   **Phương pháp thực hiện**:
    1.  Khai báo tài nguyên tĩnh trong `App.xaml`:
        ```xml
        <Geometry x:Key="IconWarning">M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z</Geometry>
        ```
    2.  Tại giao diện `StudentSubmitPage.xaml`, nhúng thông qua thẻ Path:
        ```xml
        <Path Data="{StaticResource IconWarning}" Fill="{DynamicResource WarningBrush}" Width="16" Height="16"/>
        ```
    3.  Sử dụng XAML Triggers để đổi màu `Fill` của Path khi di chuột qua (`IsMouseOver = True`).
*   **Phản biện & Tối ưu hóa**:
    *   *Tối ưu*: Tránh nhúng các thư viện font bên ngoài như FontAwesome hay Material Design Icons Nuget. Việc này giúp giảm dung lượng tệp thực thi `.dll` của Client đi khoảng 5-10MB và tăng tốc độ vẽ giao diện phần cứng WPF lên tối đa.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Mở ứng dụng trên màn hình có scale DPI 125% và 150%, kiểm tra các biểu tượng trạng thái có bị nhòe nét không.
    - [ ] [ ] Di chuột qua các nút bấm có chứa icon, kiểm tra màu sắc có đổi mượt mà.

---

### Hạng mục 4: Kích hoạt chế độ ghi trước WAL (Write-Ahead Logging) cho SQLite
*   **Yêu cầu thiết kế**: Tối ưu tốc độ ghi đĩa và tránh lỗi xung đột khóa cơ sở dữ liệu SQLite cục bộ khi nhiều luồng chạy ngầm ghi log đồng thời.
*   **Dữ liệu đầu vào**: DbContext cấu hình kết nối SQLite.
*   **Dữ liệu đầu ra**: Xuất hiện tệp `-wal` và `-shm` kế bên tệp `.db` chính khi có luồng ghi dữ liệu.
*   **Phương pháp thực hiện**:
    1.  Tại phương thức khởi tạo của `AppDbContext` hoặc phương thức cấu hình kết nối, chạy lệnh SQL PRAGMA trực tiếp:
        ```csharp
        using (var conn = Database.GetDbConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
                cmd.ExecuteNonQuery();
            }
        }
        ```
*   **Phản biện & Tối ưu hóa**:
    *   *Tối ưu*: Chế độ WAL cho phép một luồng ghi hoạt động đồng thời với nhiều luồng đọc mà không bị block. Việc kết hợp với `PRAGMA synchronous = NORMAL` giúp giảm tần suất gọi lệnh ghi xuống đĩa cứng vật lý, kéo dài tuổi thọ của các ổ đĩa HDD/SSD cũ trong phòng máy tính nhà trường.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Chạy ứng dụng và ghi log liên tục, kiểm tra tệp `smartclass.db-wal` có tự động sinh ra trong thư mục dữ liệu không.
    - [ ] [ ] Chạy kiểm thử đa luồng (ghi log đồng thời từ 5 luồng chạy ngầm khác nhau), đảm bảo không phát sinh lỗi khóa cơ sở dữ liệu `SQLiteException: database is locked`.

---

### Hạng mục 5: Mã hóa tệp nháp tự động `.tmp` bằng DPAPI kết hợp khóa đối xứng AES-256
*   **Yêu cầu thiết kế**: Đảm bảo an toàn quyền riêng tư tuyệt đối cho bản thảo học sinh làm bài tự luận. Học sinh ca sau trên cùng một máy tính Windows không thể mở đọc trộm file tạm của ca học trước.
*   **Dữ liệu đầu vào**: Văn bản thô của học sinh soạn thảo (Plaintext).
*   **Dữ liệu đầu ra**: Mảng byte nhị phân đã mã hóa (Ciphertext) lưu trong file nháp `autosave_draft_{studentCode}.tmp`.
*   **Phương pháp thực hiện**:
    1.  *Tạo khóa mã hóa AES*: Khóa AES-256 được tạo bằng cách băm chuỗi kết hợp: `Muối_Mã_Hóa_Hệ_Thống (AppSalt) + Mã_Số_Học_Sinh (StudentCode)`.
    2.  *Mã hóa dữ liệu*: Sử dụng thuật toán AES-256 mã hóa văn bản thô.
    3.  *Mã hóa DPAPI*: Sử dụng lớp `ProtectedData.Protect` của Windows bọc tiếp một lớp ngoài mảng byte đã mã hóa AES theo phạm vi người dùng đăng nhập Windows hiện tại (`DataProtectionScope.CurrentUser`).
    4.  *Đọc và Giải mã*: Khi mở app, giải mã ngược lại. Nếu phát sinh lỗi giải mã (do tệp bị can thiệp đổi nội dung hoặc tài khoản khác cố tình mở), chương trình phải bỏ qua lỗi, xóa tệp hỏng và hiển thị màn hình trắng, không được crash app.
*   **Phản biện & Tối ưu hóa**:
    *   *Phản biện*: Tại sao phải kết hợp cả hai lớp AES và DPAPI?
    *   *Tối ưu*: Nếu phòng Lab chỉ dùng chung 1 tài khoản đăng nhập Windows (ví dụ tài khoản "Student") cho tất cả học sinh, thì DPAPI đơn lẻ sẽ không thể bảo vệ bài viết chéo giữa các ca học. Do đó, việc kết hợp AES khóa động dựa trên mã định danh cá nhân `StudentCode` sẽ đảm bảo cách ly dữ liệu 100%.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Đăng nhập Học sinh A, viết nội dung nháp -> Đăng xuất. Dùng Notepad mở tệp `.tmp` trên đĩa cứng và xác nhận dữ liệu đã được mã hóa nhị phân vô nghĩa.
    - [ ] [ ] Đăng nhập Học sinh B trên cùng tài khoản Windows đó, kiểm tra xem giao diện soạn thảo có hiển thị trống (không tải nhầm bài của học sinh A).
    - [ ] [ ] Dùng Notepad sửa đổi cấu trúc file `.tmp` để làm hỏng file, mở lại ứng dụng xem app có tự động bỏ qua lỗi và mở giao diện soạn bài bình thường không.

---

### Hạng mục 6: Thanh tiến độ soạn bài đổi màu sắc động theo mục tiêu từ
*   **Yêu cầu thiết kế**: Thanh tiến độ hiển thị phần trăm hoàn thành số từ viết so với mục tiêu. Đổi màu trực quan: <50% màu đỏ tươi, 50-99% màu cam, >=100% màu xanh lá cây sáng.
*   **Dữ liệu đầu vào**: Số từ hiện tại (`int`), Mục tiêu từ (`int`).
*   **Dữ liệu đầu ra**: Thuộc tính `Foreground` của ProgressBar được cập nhật màu sắc Hex tương ứng.
*   **Phương pháp thực hiện**:
    1.  Viết một `IValueConverter` tên `WordCountToColorConverter` kế thừa từ `IValueConverter`:
        *   Tỷ lệ phần trăm = `(CurrentCount / TargetCount) * 100`.
        *   Nếu `TargetCount == 0`, gán Tỷ lệ = 100%.
        *   Tỷ lệ `< 50`: Trả về `SolidColorBrush` màu Đỏ `#FF4C4C`.
        *   Tỷ lệ từ `50` đến `99`: Trả về `SolidColorBrush` màu Cam `#FFA500`.
        *   Tỷ lệ `>= 100`: Trả về `SolidColorBrush` màu Xanh lá `#2ECC71`.
    2.  Ràng buộc converter này vào thuộc tính `Foreground` của ProgressBar trong file XAML.
*   **Phản biện & Tối ưu hóa**:
    *   *Tối ưu*: Viết Converter trong code-behind bằng C# giúp quản lý mã màu tập trung và xử lý lỗi chia cho 0 một cách an toàn nhất, tránh viết các Triggers XAML phức tạp làm giảm hiệu năng render của ứng dụng.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Đặt mục tiêu từ là `100`. Gõ `10` từ -> Kiểm tra thanh tiến độ hiển thị màu Đỏ.
    - [ ] [ ] Gõ `60` từ -> Kiểm tra thanh tiến độ hiển thị màu Cam.
    - [ ] [ ] Gõ `100` từ -> Kiểm tra thanh tiến độ chuyển sang màu Xanh lá.
    - [ ] [ ] Đặt mục tiêu từ là `0`, gõ bài viết và đảm bảo không xảy ra crash hệ thống do lỗi chia cho 0.

---

### Hạng mục 7: Đồng bộ nhật ký sự kiện (`EventLogs`) về máy chủ giáo viên định kỳ
*   **Yêu cầu thiết kế**: Gửi log hoạt động cục bộ chưa đồng bộ về server giáo viên định kỳ 5 phút một lần để ban giám hiệu giám sát chất lượng và giải quyết khiếu nại.
*   **Dữ liệu đầu vào**: Các dòng log sự kiện trong bảng `EventLogs` của SQLite cục bộ có trạng thái `Synced = 0`.
*   **Dữ liệu đầu ra**: Các gói dữ liệu JSON gửi qua TCP, cập nhật trạng thái `Synced = 1` trong SQLite cục bộ sau khi nhận ACK thành công.
*   **Phương pháp thực hiện**:
    1.  Tạo dịch vụ chạy nền `SyncLogService` lặp lại chu kỳ mỗi 5 phút bằng `Task.Delay`.
    2.  Truy vấn SQLite lấy danh sách tối đa 100 bản ghi chưa đồng bộ (`Synced = 0`).
    3.  Chuyển đổi danh sách bản ghi thành mảng JSON.
    4.  Mỗi dòng log bắt buộc phải có khóa chính là một chuỗi GUID (`Id TEXT PRIMARY KEY`) được sinh ra tự động ngay khi sự kiện xảy ra ở máy trạm.
    5.  Gửi mảng JSON qua TCP Socket. Server giáo viên sau khi ghi nhận thành công sẽ phản hồi chuỗi `SYNC_ACK`. Client nhận được `SYNC_ACK` thực hiện lệnh update `Synced = 1` cho các bản ghi tương ứng trong SQLite.
*   **Phản biện & Tối ưu hóa**:
    *   *Phản biện*: Tại sao phải gửi gom lô (batching) 5 phút/lần thay vì gửi tức thời?
    *   *Tối ưu*: Nếu gửi log tức thời, khi cả lớp học sinh thao tác cùng lúc sẽ tạo ra hàng ngàn kết nối TCP đồng thời làm nghẽn băng thông mạng LAN của nhà trường. Gửi gom lô 5 phút giúp tối ưu hóa lưu lượng truyền tải và giảm tải xử lý cho máy chủ giáo viên.
    *   *An toàn dữ liệu*: Khóa chính GUID giúp máy chủ giáo viên lọc bỏ trùng lặp dữ liệu (Idempotency) nếu xảy ra sự cố rớt mạng LAN giữa chừng khi client đã gửi dữ liệu thành công nhưng chưa kịp nhận ACK phản hồi từ server.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Rút dây mạng LAN, tạo ra các hành động (nhấn viết bài, nộp bài), kiểm tra SQLite xem log được ghi nhận với `Synced = 0`.
    - [ ] [ ] Cắm lại dây mạng LAN, kiểm tra sau chu kỳ 5 phút các log đó được gửi đi thành công và cập nhật trạng thái `Synced = 1` trong CSDL cục bộ.
    - [ ] [ ] Gửi thử bản tin log chứa GUID trùng lặp lên server giáo viên, kiểm tra xem server có tự động bỏ qua (không ghi nhận trùng lặp) hay không.

---

### Hạng mục 8: Cho phép thu gọn/mở rộng bảng hướng dẫn viết bài ở phía bên phải màn hình
*   **Yêu cầu thiết kế**: Bổ sung nút Toggle thu gọn cẩm nang hướng dẫn bên phải màn hình để tăng không gian hiển thị cho khung soạn thảo văn bản khi học sinh làm bài thi tự luận dài.
*   **Dữ liệu đầu vào**: Sự kiện bấm chọn nút Toggle.
*   **Dữ liệu đầu ra**: Bảng hướng dẫn và thanh GridSplitter thay đổi thuộc tính `Visibility` giữa `Visible` và `Collapsed`.
*   **Phương pháp thực hiện**:
    1.  Bố trí cột trong Grid XAML:
        *   Cột 1 (Soạn bài): `Width="*"`.
        *   Cột 2 (GridSplitter): `Width="Auto"`.
        *   Cột 3 (Cẩm nang hướng dẫn): `Width="300"`.
    2.  Khi bấm nút thu gọn: Đặt thuộc tính `Visibility` của Cột 3 và GridSplitter thành `Collapsed`. Thay đổi nhãn nút Toggle thành `«` hoặc `<`.
    3.  Khi bấm nút mở rộng: Đặt thuộc tính `Visibility` của Cột 3 và GridSplitter thành `Visible`. Thay đổi nhãn nút Toggle thành `»` hoặc `>`.
    4.  Lưu trạng thái ẩn/hiện hiện tại vào tệp `settings.json` để duy trì giao diện người dùng yêu thích trong những lần khởi chạy sau.
*   **Phản biện & Tối ưu hóa**:
    *   *Tối ưu*: Việc thu ẩn cả thanh GridSplitter là bắt buộc. Nếu chỉ ẩn bảng hướng dẫn mà giữ lại GridSplitter, học sinh vẫn có thể vô tình rê chuột và kéo thanh ngăn cách sang phải, làm hỏng bố cục giao diện hiển thị.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Nhấn nút Thu gọn -> Kiểm tra vùng soạn bài tự động giãn rộng ra chiếm 100% diện tích màn hình.
    - [ ] [ ] Kéo giãn kích thước cửa sổ khi đang thu gọn -> Kiểm tra giao diện co giãn bình thường không vỡ bố cục.
    - [ ] [ ] Tắt ứng dụng khi đang thu gọn, mở lại ứng dụng xem trạng thái thu gọn có được giữ nguyên.

---

### Hạng mục 9: Tự động phân cấp thư mục nhận file từ giáo viên theo `ReceivedFiles/MonHoc_GiaoVien/`
*   **Yêu cầu thiết kế**: Tài liệu giáo viên gửi xuống máy học sinh phải được phân loại ngăn nắp theo môn học và tên giáo viên thay vì lưu phẳng tất cả vào một thư mục chung.
*   **Dữ liệu đầu vào**: Metadata của tệp tin từ server gửi sang: `FileName`, `SubjectName` (Tên môn học), `TeacherName` (Tên giáo viên).
*   **Dữ liệu đầu ra**: Tệp tin được lưu chính xác trên đĩa cứng tại đường dẫn phân cấp tương ứng.
*   **Phương pháp thực hiện**:
    1.  Xây dựng hàm xử lý chuỗi (Sanitize): Loại bỏ tất cả các ký tự cấm đặt tên thư mục trên hệ điều hành Windows bao gồm: `\ / : * ? " < > |`. Thay thế chúng bằng dấu gạch dưới `_`.
    2.  Tạo đường dẫn thư mục đích: `string targetDir = Path.Combine(defaultPath, $"{subjectName}_{teacherName}");`.
    3.  Gọi `Directory.CreateDirectory(targetDir)` để tự động tạo thư mục cha phân cấp nếu chưa tồn tại trên ổ đĩa.
    4.  Ghi luồng dữ liệu byte nhận được vào đường dẫn tệp `Path.Combine(targetDir, fileName)`.
*   **Phản biện & Tối ưu hóa**:
    *   *Phòng ngừa lỗi*: Nếu giá trị `SubjectName` hoặc `TeacherName` truyền sang bị trống (null) do lỗi mạng, hệ thống phải tự động chuyển hướng lưu tệp vào thư mục mặc định `ReceivedFiles/Chung/` thay vì gây crash luồng nhận file của học sinh.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Gửi thử tệp với môn học có tên đặc biệt `"Ngữ Văn: Lớp 10"` (chứa dấu hai chấm) và giáo viên `"Nguyễn Văn A/B"` (chứa dấu xuyệt).
    - [ ] [ ] Kiểm tra thư mục đích được tạo thành công có tên là `"Ngu Van_ Lop 10_Nguyen Van A_B"` (các ký tự đặc biệt đã được thay thế bằng dấu gạch dưới).
    - [ ] [ ] Đảm bảo file được ghi nhận thành công và đọc được bình thường bên trong thư mục vừa tạo.

---

### Hạng mục 10: Tích hợp âm thanh hiệu ứng phản hồi nhẹ khi nộp bài thành công
*   **Yêu cầu thiết kế**: Phát ra một âm thanh ngắn, nhẹ nhàng khi tệp bài làm được gửi thành công lên máy giáo viên để tăng tính xác nhận và cảm giác an tâm cho học sinh.
*   **Dữ liệu đầu vào**: Sự kiện truyền tệp tin thành công (`UploadSuccess`).
*   **Dữ liệu đầu ra**: Âm thanh phát ra loa/tai nghe thiết bị.
*   **Phương pháp thực hiện**:
    1.  Nhúng một tệp âm thanh `.wav` ngắn chất lượng cao (~1 giây, tiếng chuông ding nhẹ) vào tài nguyên ứng dụng dưới dạng `Embedded Resource` để tránh lỗi mất tệp khi cài đặt.
    2.  Sử dụng lớp `System.Media.SoundPlayer` để phát âm thanh bất đồng bộ để tránh chặn luồng giao diện chính:
        ```csharp
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QASmartClass.Assets.success.wav"))
        {
            if (stream != null)
            {
                using (var player = new SoundPlayer(stream))
                {
                    player.Play(); // Chạy bất đồng bộ
                }
            }
        }
        ```
*   **Phản biện & Tối ưu hóa**:
    *   *Phản biện*: Âm thanh phát ra có thể gây ồn trong phòng thi hoặc phòng học cần yên tĩnh.
    *   *Tối ưu*: Bắt buộc thiết kế một checkbox cấu hình `[x] Bật âm thanh thông báo` trên giao diện cài đặt (`StudentSettingsPage`). Cho phép học sinh hoặc giáo viên tắt hoàn toàn âm báo.
    *   *Phòng ngừa lỗi*: Bọc toàn bộ khối lệnh phát âm thanh trong khối `try-catch` trống để tránh crash ứng dụng khi thiết bị không hỗ trợ card âm thanh hoặc driver âm thanh bị lỗi.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Bấm nộp bài thành công -> Xác nhận có âm thanh phản hồi phát ra.
    - [ ] [ ] Tắt checkbox âm thanh cài đặt -> Nộp bài thành công không phát ra âm báo.
    - [ ] [ ] Tắt card âm thanh trong Device Manager -> Thực hiện nộp bài để đảm bảo ứng dụng không phát sinh crash.

---

### Hạng mục 11: Biểu đồ mini phân tích xu hướng học tập của học sinh
*   **Yêu cầu thiết kế**: Hiển thị biểu đồ cột tối giản (Sparkline) biểu diễn số lượng từ viết trung bình và số bài tập đã nộp của học sinh trong tháng hiện tại.
*   **Dữ liệu đầu vào**: Truy vấn CSDL SQLite từ bảng `FileTransfers` và `EventLogs` lọc theo `StudentCode` hiện tại và thời gian trong tháng.
*   **Dữ liệu đầu ra**: Giao diện hiển thị các cột hình chữ nhật có chiều cao tương quan trên UI.
*   **Phương pháp thực hiện**:
    1.  *Thiết kế đồ họa tối giản*: Sử dụng thẻ XAML `ItemsControl` kết hợp cấu trúc `UniformGrid` hướng ngang.
    2.  *Vẽ cột biểu đồ*: Mỗi phần tử trong ItemsControl liên kết (Binding) dữ liệu chiều cao `Height` của thẻ `<Rectangle>` tỉ lệ thuận với số từ của bài viết đó so với mục tiêu.
    3.  *Tối ưu truy vấn*: Thiết lập chỉ mục (Index) SQLite cho hai cột `StudentCode` và `Timestamp` trong bảng dữ liệu để đảm bảo tốc độ truy vấn tức thì (<10ms).
*   **Phản biện & Tối ưu hóa**:
    *   *Tối ưu*: Việc tự vẽ biểu đồ bằng các Shape WPF cơ bản giúp ứng dụng nhẹ hơn gấp nhiều lần so với cài các thư viện vẽ đồ thị nặng ký của bên thứ ba, loại bỏ nguy cơ chậm trễ giao diện và không tương thích đồ họa trên các máy tính phòng Lab chạy Windows cũ.
*   **Bảng kiểm tra (Checksheet) cho Coder**:
    - [ ] [ ] Đăng nhập tài khoản mới chưa làm bài tập nào -> Đảm bảo biểu đồ hiển thị nhãn thân thiện: `"Hãy bắt đầu bài viết đầu tiên để xem phân tích tiến độ!"` (không bị vỡ layout).
    - [ ] [ ] Nộp 3 bài tập viết có số từ khác nhau -> Đảm bảo các cột biểu đồ vẽ đúng tỷ lệ chiều cao trực quan trên màn hình.

---

## ══ PHẦN 3: ĐẶC TẢ CẤU HÌNH THÍCH ỨNG THEO CẤP HỌC ══

Để đáp ứng quy chuẩn sư phạm của từng cấp học tại Việt Nam, lập trình viên cấu hình các tham số chức năng và giao diện cụ thể theo bảng quy chuẩn dưới đây:

### 1. Phân cấp chức năng giao diện
*   **Cấp Tiểu học (Primary)**:
    *   *Mục tiêu*: Đơn giản hóa tối đa, tránh làm học sinh phân tâm.
    *   *Cấu hình*:
        *   Ẩn hoàn toàn tính năng viết bài luận tự do dài ở `StudentSubmitPage`.
        *   Khóa chức năng đếm từ (`WordCounter`).
        *   Kích hoạt module trò chơi học thuật phát triển trí tuệ `GameHubPage`.
        *   Bật hiển thị hình ảnh minh họa lớn trên giao diện làm bài trắc nghiệm `StudentQuizPage`.
*   **Cấp THCS (Junior High)**:
    *   *Mục tiêu*: Rèn luyện tư duy tự luận ngắn và thi đua tập thể.
    *   *Cấu hình*:
        *   Mở phân hệ Soạn bài viết tự luận (giới hạn bài viết ngắn 100 - 200 từ).
        *   Kích hoạt các công cụ bổ trợ học tập: Công cụ Số nguyên tố, Giải hệ phương trình bậc nhất 2 ẩn.
        *   Mở phân hệ chấm điểm nề nếp thi đua Đoàn/Đội (`YouthUnion`) dành cho đội trực tuần.
*   **Cấp THPT (Senior High)**:
    *   *Mục tiêu*: Tự luận chuyên sâu, ôn luyện thi cử và định hướng nghề nghiệp.
    *   *Cấu hình*:
        *   Mở toàn bộ tính năng soạn bài viết tự luận dài (không giới hạn từ).
        *   Bật công cụ toán học nâng cao (Giải phương trình bậc hai có số phức, đồ thị Parabol đỉnh).
        *   Kích hoạt bài trắc nghiệm tính cách định hướng nghề nghiệp `CareerTestPage`.
*   **Mô hình Trường Liên cấp (Multi-level / K-12)**:
    *   *Mục tiêu*: Chuyển đổi giao diện động (Dynamic UI Switch) theo ca học và tài khoản đăng nhập của từng học sinh để dùng chung phòng máy Lab hoặc hạ tầng mạng.
    *   *Cấu hình động*:
        *   Hệ thống không đọc tham số tĩnh `GradeLevel` trong `settings.json`. Thay vào đó, sau khi đăng nhập (`StudentLoginWindow.xaml.cs`), hệ thống lấy thuộc tính lớp của học sinh đăng nhập để cấu hình giao diện `StudentShell` thời gian thực.
        *   Tự động kích hoạt các tab học tập, công cụ bổ trợ học thuật, và kiểm duyệt nề nếp thi đua cờ đỏ tương ứng với khối lớp của tài khoản đăng nhập.
        *   Bật cơ chế dọn dẹp hàng đợi và file tạm `.tmp` ngay khi sự kiện Đăng xuất (Logout) diễn ra để cô lập dữ liệu hoàn toàn giữa các cấp học.

### 2. Coder Checksheet cho Phân cấp Học sinh
- [ ] [ ] Cấu hình tham số `GradeLevel` trong `settings.json` thành `Primary`. Khởi chạy ứng dụng và đảm bảo không xuất hiện tab Soạn bài tự luận dài hay chức năng đếm từ.
- [ ] [ ] Cấu hình `GradeLevel` thành `THPT`. Đảm bảo các công cụ số phức ở phương trình bậc 2 và trắc nghiệm nghề nghiệp hoạt động đầy đủ.
- [ ] [ ] Kiểm tra tính năng trường Liên cấp: Đăng nhập bằng tài khoản học sinh Lớp 3 -> Xác nhận giao diện hiển thị dạng thu gọn (Tiểu học). Đăng xuất, đăng nhập ngay bằng tài khoản học sinh Lớp 11 -> Đảm bảo giao diện tự động chuyển đổi sang dạng đầy đủ tính năng (THPT), và tệp nháp tạm của học sinh lớp 3 cũ đã bị xóa sạch khỏi đĩa cứng để bảo mật.

---

## ══ PHẦN 4: ĐẶC TẢ CẤU HÌNH THÍCH ỨNG THEO ĐIỀU KIỆN HẠ TẦNG PHÒNG MÁY ══

Tùy vào điều kiện cơ sở vật chất phòng máy tính của từng trường học, kỹ thuật viên cài đặt điều chỉnh cấu hình tham số kỹ thuật trong tệp `settings.json` máy học sinh theo 3 mô hình tiêu chuẩn sau:

### 1. Bảng tham số cấu hình trong `settings.json`
```json
{
  "GradeLevel": "THPT",                      // Cấp học: Primary, THCS, THPT
  "InfrastructureModel": "ModelA",           // Mô hình hạ tầng: ModelA, ModelB, ModelC
  "FilePort": 29879,                         // Cổng kết nối TCP File Transfer
  "EnableWALMode": true,                     // Bật chế độ ghi trước WAL cho SQLite
  "SyncIntervalMinutes": 15,                 // Chu kỳ đồng bộ log: 1 (ModelC), 5 (ModelB), 15 (ModelA)
  "EnableAudioEffects": false,               // Bật/tắt âm thanh hiệu ứng (ModelA tắt để nhẹ máy)
  "EnableDynamicSVG": false,                 // Bật/tắt hiệu ứng SVG động (ModelA tắt để giảm tải RAM)
  "AutoCleanPendingSyncDays": 7,             // Số ngày lưu trữ tối đa tệp đã đồng bộ trong PendingSync
  "EnableOfflineFallback": true              // Bật chế độ tự học ngoại tuyến khi mất ping liên tiếp
}
```

### 2. Mô hình Kỹ thuật chi tiết
*   **Mô hình A (Hạ tầng yếu - Phòng máy khó khăn)**:
    *   *Đặc điểm*: Máy yếu, mạng LAN chập chờn rớt gói liên tục.
    *   *Cấu hình*: `EnableWALMode: true`, `SyncIntervalMinutes: 15` (hoặc chỉ gửi thủ công khi kết thúc ca học để tránh nghẽn mạng), `EnableAudioEffects: false`, `EnableDynamicSVG: false`.
    *   *Hành vi phần mềm*: Tận dụng tối đa bộ đệm `PendingSync` lưu file đĩa cục bộ, xếp hàng nộp file ngầm tuần tự từ từ lên máy giáo viên thay vì nộp đồng loạt.
*   **Mô hình B (Hạ tầng đạt chuẩn - Phòng máy trung bình)**:
    *   *Đặc điểm*: Máy trạm trung bình khá, mạng LAN chạy cáp ổn định.
    *   *Cấu hình*: `EnableWALMode: true`, `SyncIntervalMinutes: 5`, `EnableAudioEffects: true`, `EnableDynamicSVG: true`, `AutoCleanPendingSyncDays: 14`.
*   **Mô hình C (Hạ tầng hiện đại - Trường chất lượng cao/Quốc tế)**:
    *   *Đặc điểm*: Thiết bị cá nhân 1-1, mạng Wi-Fi phủ rộng tốc độ cao, server mạnh.
    *   *Cấu hình*: `SyncIntervalMinutes: 1` (Đồng bộ thời gian thực), `EnableAudioEffects: true`, `EnableDynamicSVG: true`, `AutoCleanPendingSyncDays: 30`. Cho phép bảng vẽ phác thảo học sinh đồng bộ trực tiếp lên Smart Board của giáo viên.

---

## ══ PHẦN 5: QUY CHUẨN FONT CHỮ HỌC THUẬT & SỬA LỖI VẼ VECTƠ ══

Để tránh tuyệt đối lỗi hiển thị ký tự (ô vuông lỗi font `☐` hoặc ký tự bị méo lệch hàng) khi chương trình biểu diễn các nội dung học thuật phức tạp, lập trình viên bắt buộc phải cấu hình và phân tách Font Family theo bảng quy chuẩn sau:

### 1. Phân loại Font Family theo phân hệ sử dụng
| Phân hệ / Loại văn bản | Font Family khuyến nghị | Vai trò & Đặc điểm kỹ thuật | Cách áp dụng trong XAML |
| :--- | :--- | :--- | :--- |
| **Giao diện & Văn bản chung** | `Segoe UI`, `Roboto`, `Inter` | Hiển thị rõ nét tiếng Việt có dấu, hỗ trợ rendering High DPI tốt, nét chữ hiện đại. | `FontFamily="Segoe UI"` |
| **Công thức & Ký hiệu Toán/Lý/Hóa** | `Cambria Math`, `Times New Roman`, `Segoe UI Symbol`, `Cambria` | Chứa đầy đủ Glyph học thuật đặc biệt ($\Delta, \sum, \int, \sqrt{}, \pi, \alpha, \beta, \neq, \approx$, chỉ số trên/dưới) mà không bị lệch dòng. **Đặc biệt bắt buộc sử dụng `Times New Roman` cho chức năng vẽ vectơ để sửa lỗi hiển thị ký hiệu.** | `FontFamily="Cambria Math, Times New Roman, Segoe UI Symbol"` |
| **Phiên âm Quốc tế IPA** | `Segoe UI`, `Arial`, `Times New Roman` | Hỗ trợ trọn vẹn các Unicode Block `IPA Extensions` (U+0250 đến U+02AF) và `Spacing Modifier Letters` (U+02B0 đến U+02FF). | `FontFamily="Segoe UI, Times New Roman"` |
| **Mã nguồn & Dữ liệu máy tính** | `Consolas`, `Cascadia Code`, `Courier New` | Các ký tự có độ rộng bằng nhau (Monospaced), dễ phát hiện lỗi cú pháp, cấu trúc JSON/XML thẳng hàng. | `FontFamily="Consolas, Cascadia Code"` |

### 2. Nguyên tắc sửa lỗi font cho chức năng Vẽ Vectơ
*   **Triệu chứng lỗi**: Chức năng vẽ vectơ hiển thị sai ký hiệu mũi tên hướng hoặc các ký tự tọa độ toán học liên quan (hiển thị thành ô vuông lỗi font `☐` trên các máy Windows cũ phòng Lab).
*   **Giải pháp xử lý triệt để**: Cấu hình cứng thuộc tính `FontFamily="Times New Roman"` cho tất cả các nhãn (Labels) hiển thị ký hiệu vectơ (như $\vec{a}$, $\overrightarrow{AB}$, tọa độ đỉnh, chỉ số phương hướng) trên Canvas vẽ đồ thị.
*   **Ví dụ áp dụng trong XAML**:
    ```xml
    <!-- TextBlock hiển thị ký hiệu vectơ trên Canvas vẽ -->
    <TextBlock Text="AB&#x20D7;" FontFamily="Times New Roman" FontSize="14" FontWeight="Bold"/>
    ```
*   **Thứ tự ưu tiên Font Fallback**: Trong XAML, luôn khai báo chuỗi danh sách Font cách nhau bằng dấu phẩy: `FontFamily="Cambria Math, Times New Roman, Segoe UI Symbol, Cambria"`. Nếu máy tính cũ thiếu font thứ nhất, nó sẽ tự động dùng font tiếp theo để thay thế, tránh việc trả về font hệ thống lỗi.

### 3. Checksheet kiểm thử Font (QA Checksheet)
- [ ] [ ] Kiểm tra hiển thị các ký tự toán học đặc biệt như $\Delta, \alpha, \beta, \gamma, \sum, \int, \neq, \approx, \sqrt{x}$ trong khung kết quả giải hệ phương trình và phương trình bậc 2 xem có bị ô vuông lỗi font `☐` hay không.
- [ ] [ ] Kiểm tra chức năng vẽ vectơ (Vector Drawing Canvas): Đảm bảo các ký hiệu vectơ ($\vec{v}$, $\overrightarrow{AB}$, mũi tên chỉ hướng) và tọa độ tương quan hiển thị đúng định dạng `Times New Roman`, không bị mất nét hoặc hiển thị ô vuông lỗi.
- [ ] [ ] Kiểm tra hiển thị phiên âm tiếng Anh như `/æ/`, `/θ/`, `/ʃ/`, `/dʒ/` có đúng vị trí, không bị biến dạng ký tự.
- [ ] [ ] Thay đổi kích thước cỡ chữ (FontSize) từ 12 lên 24, kiểm tra xem các ký hiệu chỉ số trên/dưới (Superscript/Subscript) có bị lệch dòng baseline so với ký tự chính hay không.
- [ ] [ ] Mở ứng dụng trên máy chạy Windows 10 và Windows 11 để đảm bảo tính đồng bộ hiển thị font chữ.

---

## ══ PHẦN 6: CƠ SỞ DỮ LIỆU SQLITE & SCRIPTS CẬP NHẬT ══

Để hỗ trợ các tính năng đồng bộ nhật ký sự kiện, lưu trữ trạng thái đồng bộ và mã hóa, cấu trúc cơ sở dữ liệu SQLite cục bộ được cập nhật thông qua các tập lệnh SQL sau:

### 1. Script thiết lập chế độ WAL
Mỗi khi khởi động kết nối CSDL trong mã nguồn C#:
```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
```

### 2. Cập nhật Bảng `EventLogs`
Bổ sung cấu trúc để phục vụ đồng bộ nhật ký sự kiện định kỳ:
```sql
CREATE TABLE IF NOT EXISTS EventLogs (
    Id TEXT PRIMARY KEY,             -- Lưu GUID (UUID) dạng chuỗi để chống trùng lặp chéo khi rớt mạng
    StudentCode TEXT NOT NULL,       -- Mã học sinh tạo sự kiện
    Actor TEXT NOT NULL,             -- Tên Actor (mã học sinh hoặc hệ thống)
    EventName TEXT NOT NULL,         -- Tên sự kiện (ví dụ: WriteDraft, SubmitFile, NetworkDrop)
    Description TEXT,                -- Chi tiết sự kiện
    Timestamp TEXT NOT NULL,         -- ISO-8601 String (yyyy-MM-dd HH:mm:ss)
    Synced INTEGER DEFAULT 0         -- Trạng thái đồng bộ về Server: 0 = Chưa gửi, 1 = Đã gửi thành công
);

-- Khởi tạo các Index để tăng tốc độ truy vấn phân tích
CREATE INDEX IF NOT EXISTS IX_EventLogs_StudentCode ON EventLogs(StudentCode);
CREATE INDEX IF NOT EXISTS IX_EventLogs_Synced ON EventLogs(Synced);
```

---

## ══ PHẦN 7: BẢNG ĐIỀU KHIỂN TRUNG TÂM & CƠ CHẾ PHÊ DUYỆT CẤU HÌNH (CENTRAL CONTROL PANEL) ══

Để nâng cao tính tự chủ của nhà trường và bảo vệ an toàn kỹ thuật cho hệ thống, hệ thống thiết lập một **Bảng điều khiển cấu hình trung tâm (Central Control Panel)** tích hợp trực tiếp trên phân hệ BQL (`LeadershipDashboardWindow` / `SystemSettingsView`). Bảng này phân tách quyền quản trị, kiểm soát nhập liệu và thiết lập cơ chế phê duyệt trước khi áp dụng cấu hình xuống các máy trạm.

### 1. Phân quyền & Tách biệt vai trò cấu hình (Separation of Roles)
Hệ thống phân chia các tham số cấu hình thành hai lớp độc lập:
*   **Lớp cấu hình Nghiệp vụ Sư phạm (School-level Settings)**:
    *   *Người quyết định*: Ban Giám hiệu, Giáo vụ nhà trường.
    *   *Các tham số*: Quy tắc tính điểm thi đua, thời hạn nộp bài tập, mục tiêu từ viết luận mặc định, kích hoạt/vô hiệu hóa các module theo cấp học (`GradeLevel`), bật/tắt trò chơi học thuật (`GameHubPage`).
*   **Lớp cấu hình Kỹ thuật Hệ thống (System-level Settings)**:
    *   *Người quyết định*: Quản trị hệ thống (System Admin), Kỹ thuật viên IT của trường.
    *   *Các tham số*: Địa chỉ IP Server kết nối, Cổng truyền tải tệp (`FilePort`), chu kỳ đồng bộ dữ liệu (`SyncIntervalMinutes`), khóa mã hóa đĩa cứng (Encryption Salt), kích hoạt chế độ SQLite WAL.

### 2. Cơ chế ràng buộc & Kiểm định dữ liệu nhập (Data Validation Constraints)
Mọi thay đổi trên bảng điều khiển trung tâm phải đi qua bộ kiểm kiểm duyệt dữ liệu `ConfigurationValidator` trước khi ghi nhận:
*   *Cổng mạng (Port)*: Phải là số nguyên nằm trong dải `1024 - 65535`. Không được trùng với các cổng dịch vụ hệ thống đã biết (như 80, 443, 1433, 3306).
*   *Địa chỉ IP*: Định dạng chuỗi IPv4/IPv6 hợp lệ hoặc tên miền DNS hợp lệ.
*   *Thời gian hạn nộp*: Định dạng ngày giờ chuẩn `dd/MM/yyyy HH:mm`, bắt buộc phải là thời điểm trong tương lai so với thời gian hiện hành.

### 3. Quy trình phê duyệt & Hàng đợi trực tuyến (Approval Workflow)
Mọi cấu hình sau khi sửa đổi không được ghi đè trực tiếp lên file hoạt động `settings.json` của hệ thống ngay lập tức:
1.  **Tạo phiếu yêu cầu**: Khi người dùng thay đổi tham số, hệ thống tạo một yêu cầu đổi cấu hình dạng phiếu ghi (`ConfigurationChangeRequest`).
2.  **Đưa vào hàng đợi**: Phiếu được lưu vào bảng dữ liệu ở trạng thái chờ duyệt `Pending`. Giáo viên bộ môn hoặc kỹ thuật viên phòng máy sẽ thấy trạng thái phiếu đang chờ.
3.  **Phê duyệt chuyên quyền**:
    *   Đối với cấu hình sư phạm: Chỉ tài khoản Hiệu trưởng/Ban Giám hiệu mới có quyền Duyệt (`Approve`) phiếu yêu cầu trong `ApprovalQueueView`.
    *   Đối với cấu hình kỹ thuật: Chỉ tài khoản Admin hệ thống (IT Admin) mới có quyền Duyệt phiếu.
4.  **Hủy/Từ chối**: Người có thẩm quyền có thể bấm từ chối (`Reject`) phiếu yêu cầu kèm lý do viết tay, phiếu sẽ bị hủy bỏ và giữ nguyên cấu hình cũ.

### 4. Thời điểm áp dụng cấu hình (Effectiveness Schedule)
Khi phê duyệt phiếu yêu cầu thay đổi cấu hình, người duyệt có quyền chỉ định thời điểm áp dụng (`EffectiveTimeType`):
*   **Áp dụng lập tức (Immediate)**: Hệ thống ghi đè file cấu hình và phát một bản tin socket TCP (`CMD:UPDATE_SETTINGS`) đến toàn bộ các máy trạm đang hoạt động để áp dụng ngay lập tức mà không cần khởi động lại.
*   **Sau khi khởi động lại (AfterReboot)**: Cấu hình mới được ghi nhận nhưng chỉ có hiệu lực từ phiên làm việc tiếp theo của ứng dụng.
*   **Theo ca học tiếp theo (NextShift)**: Hệ thống lên lịch tự động áp dụng cấu hình mới từ mốc thời gian cố định ca học (ví dụ: ca sáng áp dụng từ 06:00, ca chiều áp dụng từ 12:00 trưa).
*   **Thời gian định sẵn (Scheduled)**: Chỉ định chính xác ngày giờ cấu hình bắt đầu có hiệu lực (ví dụ: kích hoạt chế độ thi cử vào đúng 08:00 sáng ngày thi).

### 5. Coder Checksheet cho Bảng điều khiển Trung tâm
- [ ] [ ] Kiểm tra phân quyền: Đăng nhập tài khoản giáo vụ, cố gắng thay đổi cổng `FilePort` xem hệ thống có chặn và báo lỗi thiếu quyền kỹ thuật.
- [ ] [ ] Kiểm tra quy trình duyệt: Tạo yêu cầu đổi mục tiêu số từ lên `150` từ. Đăng xuất. Đăng nhập học sinh và đảm bảo mục tiêu từ vẫn giữ mốc cũ. Đăng nhập Hiệu trưởng, nhấn Duyệt yêu cầu -> Đăng nhập lại học sinh kiểm tra mốc từ đã được nâng lên 150.
- [ ] [ ] Kiểm tra thời điểm áp dụng: Sửa cấu hình với loại `AfterReboot`. Kiểm tra file `settings.json` đã được ghi đè nhưng tiến trình đang chạy của ứng dụng vẫn giữ nguyên cấu hình cũ cho đến khi tắt đi bật lại.

---

## ══ PHẦN 8: KỊCH BẢN KIỂM THỬ TÍCH HỢP TOÀN DIỆN (QA INTEGRATION TESTING) ══

Lập trình viên và kiểm thử viên bắt buộc phải kiểm tra chéo (Double-Check) toàn bộ các tình huống sau trước khi tiến hành nghiệm thu mã nguồn.

### 📋 BẢNG KIỂ TỰ TÍCH HỢP TỔNG HỢP SAU NÂNG CẤP
| STT | Tình huống kiểm thử | Mô tả chi tiết các bước thực hiện | Kết quả kỳ vọng (Đầu ra chuẩn) | Trạng thái |
| :--- | :--- | :--- | :--- | :--- |
| **1** | Kiểm thử đổi cổng file port | Sửa `FilePort` trong `settings.json` thành `55555`. Khởi chạy Server và Client. Thực hiện truyền tải tệp tin. | File được truyền nhận bình thường qua cổng 55555. File log ghi nhận kết nối qua cổng mới. | `[ ] Chưa test` |
| **2** | Kiểm thử dọn dẹp thư mục | Đặt 1 tệp tin mock 10 ngày tuổi trong `PendingSync`. Đánh dấu tệp đó đã đồng bộ trong DB. Kích hoạt dọn dẹp. | Tệp mock bị xóa khỏi đĩa. Thư mục `PendingSync` giảm dung lượng. | `[ ] Chưa test` |
| **3** | Kiểm thử hiển thị đồ họa SVG | Mở trang soạn thảo bài tập. Thay đổi thu nhỏ phóng to kích thước màn hình liên tục. | Các icon trên tab hiển thị sắc nét, không bị nhấp nháy hoặc biến dạng layout. | `[ ] Chưa test` |
| **4** | Kiểm thử bảo mật bản nháp | Đăng nhập tài khoản Học sinh A -> Viết nháp -> Tắt app. Đăng nhập tài khoản Học sinh B -> Mở khung viết. | Khung viết của B hoàn toàn trống. File nháp của A trên đĩa cứng ở dạng mã hóa nhị phân không đọc được. | `[ ] Chưa test` |
| **5** | Kiểm thử chia đôi / Thu gọn màn hình | Nhấp chọn nút thu gọn cẩm nang hướng dẫn bên phải màn hình. | Bảng hướng dẫn biến mất, thanh GridSplitter bị ẩn, khung soạn thảo viết bài mở rộng tối đa. | `[ ] Chưa test` |
| **6** | Kiểm thử lưu file nhận phân cấp | Nhận tệp `DeBai.pdf` từ giáo viên `NguyenVanA` cho môn `NguVan`. | File được lưu tại thư mục `ReceivedFiles/NguVan_NguyenVanA/DeBai.pdf`. Tên thư mục không bị lỗi ký tự. | `[ ] Chưa test` |
| **7** | Kiểm thử âm thanh nộp bài | Nhấn nộp bài khi có kết nối mạng ổn định. Thiết bị đã bật âm thanh. | Phát ra âm báo thành công ngắn nhẹ. Tắt checkbox âm thanh thì không phát tiếng. Không lỗi khi tắt Sound Card. | `[ ] Chưa test` |
| **8** | Kiểm thử phân tích tiến độ | Xem bảng biểu đồ tiến độ số từ trong 5 ngày làm bài viết liên tiếp. | Biểu đồ cột vẽ chính xác tỷ lệ số lượng từ theo đúng lịch sử lưu trong SQLite của học sinh hiện tại. | `[ ] Chưa test` |
| **9** | Kiểm thử phân cấp cấp học | Đặt `GradeLevel` thành `Primary`. Mở app. | Tab soạn bài tự luận dài bị ẩn hoàn toàn, không có module đếm từ. Chỉ mở làm trắc nghiệm và Game. | `[ ] Chưa test` |
| **10**| Kiểm thử hạ tầng yếu (ModelA) | Đặt `InfrastructureModel` thành `ModelA` trong settings.json. | Các hiệu ứng SVG động và âm thanh bị vô hiệu hóa; chu kỳ đồng bộ log dãn ra đúng 15 phút. Chế độ WAL hoạt động. | `[ ] Chưa test` |
| **11**| Kiểm thử font chữ vẽ vectơ | Vẽ các vectơ có ký hiệu mũi tên và tọa độ trong Canvas học thuật. | Các ký hiệu vectơ $\vec{a}$, $\overrightarrow{AB}$ hiển thị chuẩn xác font `Times New Roman`, không bị ô vuông lỗi font `☐`. | `[ ] Chưa test` |
| **12**| Kiểm thử mô hình liên cấp | Đăng nhập tài khoản học sinh Lớp 3 -> Đăng xuất -> Đăng nhập tài khoản học sinh Lớp 11. | Giao diện tự động chuyển đổi từ Tiểu học (thu gọn) sang THPT (đầy đủ). Dữ liệu nháp của học sinh ca trước được xóa sạch, đảm bảo cô lập 100%. | `[ ] Chưa test` |
| **13**| Kiểm thử phân quyền cấu hình | Đăng nhập tài khoản Sư phạm (Giáo vụ) -> Thay đổi IP Server kỹ thuật. | Giao diện hiển thị cảnh báo: `"Bạn không có quyền chỉnh sửa cấu hình kỹ thuật hệ thống."`, khóa không cho lưu phiếu. | `[ ] Chưa test` |
| **14**| Kiểm thử quy trình phê duyệt | Sửa cổng FilePort trong đề xuất -> Lưu đề xuất. Đăng nhập Hiệu trưởng -> Nhấp Duyệt. | Phiếu chuyển trạng thái từ `Pending` sang `Approved`. Chỉ khi đã duyệt, cấu hình mới được ghi đè. | `[ ] Chưa test` |
| **15**| Kiểm thử thời điểm áp dụng | Cấu hình đổi mục tiêu viết bài áp dụng loại `NextShift` lúc 10:00 sáng. | Học sinh ca sáng đang học vẫn dùng cấu hình cũ. Đúng 12:00 trưa (mốc ca chiều), cấu hình mới tự động được kích hoạt. | `[ ] Chưa test` |

