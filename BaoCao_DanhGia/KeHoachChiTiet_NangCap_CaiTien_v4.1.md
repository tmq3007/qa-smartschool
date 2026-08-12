# KẾ HOẠCH NÂNG CẤP VÀ CẢI TIẾN HỆ THỐNG - DỰ ÁN QA SMARTCLASS (BẢN 4.1)

*Tài liệu hướng dẫn kỹ thuật chi tiết dành cho nhà phát triển, lập trình viên và kiểm thử viên*
*Được biên soạn dưới góc nhìn của Chuyên gia Thiết kế Hệ thống, Cơ sở Dữ liệu và QA/QC*

---

## 🎯 MỤC TIÊU
Đồng bộ hóa các cải tiến kỹ thuật theo 11 khuyến nghị đánh giá chuyên môn từ bản 4.0, thiết lập một quy chuẩn phát triển chặt chẽ, tối ưu hóa hiệu năng, tăng cường bảo mật dữ liệu học sinh và trải nghiệm sư phạm. Tài liệu này được thiết kế chi tiết để lập trình viên có thể thực hiện chính xác và kiểm thử viên có checksheet đối chiếu rõ ràng.

---

## 📂 SƠ ĐỒ CẤU TRÚC THƯ MỤC ẢNH HƯỞNG
Trước khi bắt đầu, lập trình viên cần xác định rõ các file mã nguồn và cấu trúc thư mục sẽ chịu tác động trực tiếp:
*   **Cấu hình & Tiện ích:** `QASmartClass/StudentClient/Models/settings.json` hoặc cấu hình toàn cục.
*   **Giao diện người dùng (XAML):** [StudentSubmitPage.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentSubmitPage.xaml)
*   **Logic điều khiển (C#):** [StudentSubmitPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Views/StudentSubmitPage.xaml.cs)
*   **Dịch vụ mạng & Truyền tải:** [StudentFileTransfer.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/StudentClient/Services/StudentFileTransfer.cs)
*   **Cơ sở dữ liệu:** `AppDbContext.cs` & SQLite CSDL file.

---

## ═══ PHẦN 1: CHI TIẾT 11 HẠNG MỤC CẢI TIẾN ═══

### Hạng mục 1: Cấu hình động cổng truyền file (`FILE_PORT`) qua tệp cấu hình bên ngoài
*   **Yêu cầu thiết kế:** Loại bỏ hằng số hardcode `FILE_PORT = 29879`. Cho phép cấu hình cổng động thông qua tệp `settings.json` đặt tại thư mục cài đặt hoặc thư mục AppData.
*   **Dữ liệu đầu vào:**
    *   Tệp `settings.json` chứa cấu trúc: `{"FilePort": 29879}`.
*   **Dữ liệu đầu ra:** Cổng mạng (`int`) hợp lệ nằm trong dải `1024` đến `65535`.
*   **Phương pháp thực hiện:**
    1.  Tạo lớp model `ClientSettings` ánh xạ các thuộc tính cấu hình.
    2.  Khi khởi chạy ứng dụng, kiểm tra sự tồn tại của tệp `settings.json`. Nếu chưa tồn tại, tự động tạo tệp mặc định.
    3.  Đọc và phân tích cú pháp (parse) tệp tin bằng `System.Text.Json` hoặc `Newtonsoft.Json`.
    4.  Nếu tệp hỏng hoặc giá trị không hợp lệ (ví dụ: chứa chữ cái hoặc ngoài dải cổng), ghi nhật ký cảnh báo và tự động gán giá trị mặc định là `29879`.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao không dùng `App.config` truyền thống của .NET?
    *   *Tối ưu:* `App.config` khó ghi đè động và định dạng XML rườm rà. Định dạng JSON hiện đại hơn, dễ đọc và dễ tích hợp cập nhật cấu hình tự động từ server giáo viên xuống client học sinh trong tương lai.
    *   *Phòng ngừa lỗi:* Sử dụng khối `try-catch` bao bọc quá trình đọc file. Tránh việc ứng dụng crash ngay khi khởi động do tệp cấu hình bị hỏng.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Kiểm tra tự động tạo tệp `settings.json` khi khởi động ứng dụng lần đầu tiên.
    - [ ] [ ] Sửa cổng trong `settings.json` thành `30000` và kiểm tra socket có lắng nghe/kết nối qua cổng này không.
    - [ ] [ ] Điền chuỗi `"abc"` vào giá trị `FilePort`, kiểm tra hệ thống có tự động fallback về cổng `29879` mà không gây crash không.

---

### Hạng mục 2: Tự động dọn dẹp các tệp ngoại tuyến quá hạn trong thư mục `PendingSync`
*   **Yêu cầu thiết kế:** Ngăn chặn việc đầy đĩa cứng tại máy Lab trường học do tích tụ tệp tin ngoại tuyến của các học sinh thuộc ca học cũ đã hoàn thành đồng bộ.
*   **Dữ liệu đầu vào:** Thư mục đĩa `PendingSync/`. Thời gian sửa đổi cuối (`LastWriteTime`) của từng tệp tin.
*   **Dữ liệu đầu ra:** Xóa bỏ vật lý các tệp tin hết hạn. Cập nhật nhật ký sự kiện dọn dẹp đĩa cứng.
*   **Phương pháp thực hiện:**
    1.  Tạo dịch vụ chạy ngầm định kỳ (hoặc kích hoạt một lần khi khởi động ứng dụng).
    2.  Dùng `Directory.GetFiles` quét thư mục `PendingSync`.
    3.  Với mỗi tệp tin, đối chiếu với cơ sở dữ liệu SQLite cục bộ:
        *   Nếu trạng thái của tệp tin là đã gửi (`Status = Sent` hoặc có log đồng bộ thành công) và thời gian chỉnh sửa cuối vượt quá 7 ngày: Thực hiện `File.Delete()`.
        *   Nếu tệp tin chưa từng đồng bộ thành công (`⚠️ Chưa gửi`) và thời gian chỉnh sửa cuối vượt quá 14 ngày: Thực hiện xóa để bảo toàn không gian ổ cứng cho máy Lab.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Việc xóa file ngoại tuyến chưa đồng bộ sau 14 ngày có quá mạo hiểm?
    *   *Tối ưu:* Sau 14 ngày, buổi học đó đã trôi qua rất lâu và học sinh chắc chắn đã có điểm số hoặc được xử lý thủ công. Việc giữ lại vô thời hạn sẽ làm tăng nguy cơ rò rỉ dữ liệu hoặc tràn ổ cứng máy tính đời cũ. 14 ngày là ngưỡng an toàn tuyệt đối.
    *   *Phòng ngừa lỗi:* Trước khi xóa, kiểm tra xem tệp tin có đang bị khóa bởi tiến trình khác không (`IsFileLocked`). Nếu bị khóa, bỏ qua và thực hiện ở phiên làm việc sau.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Tạo tệp mock trong `PendingSync` có ngày tạo cách đây 8 ngày (đã đồng bộ) và 15 ngày (chưa đồng bộ).
    - [ ] [ ] Khởi chạy dịch vụ dọn dẹp và kiểm tra các tệp mock trên có bị xóa chính xác hay không.
    - [ ] [ ] Đảm bảo log sự kiện `CleanUpPendingSync` ghi nhận đúng số lượng tệp đã xóa thành công.

---

### Hạng mục 3: Thay thế các emoji Unicode thô sơ bằng biểu tượng đồ họa vectơ (SVG) sắc nét
*   **Yêu cầu thiết kế:** Nâng cao thẩm mỹ giao diện bằng cách loại bỏ các emoji văn bản thô (như 📂, ⚠️, ⏳) và thay thế bằng các SVG Path sắc nét, hỗ trợ co giãn trên màn hình độ phân giải cao (High DPI).
*   **Dữ liệu đầu vào:** Tài nguyên thiết kế (các chuỗi SVG Path).
*   **Dữ liệu đầu ra:** Các tab và trạng thái trên UI hiển thị icon sắc nét, đồng bộ màu sắc chủ đạo.
*   **Phương pháp thực hiện:**
    1.  Khai báo các đối tượng `Geometry` trong tệp tài nguyên chung `App.xaml` (hoặc một ResourceDictionary riêng biệt).
        *   Ví dụ: `<Geometry x:Key="IconFolder">M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z</Geometry>`
    2.  Tại `StudentSubmitPage.xaml`, sử dụng thẻ `<Path Data="{StaticResource IconFolder}" Fill="{DynamicResource PrimaryBrush}" Width="16" Height="16"/>` để hiển thị.
    3.  Thiết lập `Style Trigger` để đổi màu `Fill` của Path khi di chuột qua (`MouseOver`) hoặc khi TabItem được chọn (`IsSelected`).
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao không dùng thư viện icon Nuget (như FontAwesome)?
    *   *Tối ưu:* Thư viện bên ngoài làm phình dung lượng file build và phụ thuộc bên thứ ba. Nhúng trực tiếp mã Geometry vẽ trực tiếp bằng card đồ họa của máy tính (WPF hardware acceleration) giúp giao diện khởi chạy cực kỳ mượt mà.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Kiểm tra giao diện ở độ phân giải scale 150% và 200% xem icon có bị vỡ nét (pixelated) không.
    - [ ] [ ] Di chuột qua các tab, kiểm tra màu sắc icon có đổi hiệu ứng mượt mà (chuyển sang màu nhấn Accent Color) không.

---

### Hạng mục 4: Kích hoạt chế độ ghi nhật ký trước (WAL - Write-Ahead Logging) cho cơ sở dữ liệu SQLite
*   **Yêu cầu thiết kế:** Tối ưu hóa tốc độ truy xuất cơ sở dữ liệu cục bộ SQLite và ngăn chặn triệt để lỗi "Database is locked" khi chạy nhiều luồng ghi đồng thời (ghi log sự kiện và ghi lịch sử gửi file).
*   **Dữ liệu đầu vào:** Kết nối SQLite thông qua Entity Framework Core (hoặc Microsoft.Data.Sqlite).
*   **Dữ liệu đầu ra:** SQLite hoạt động ở chế độ WAL, sinh ra hai file tạm `.db-wal` và `.db-shm` cạnh file `.db` chính khi có kết nối mở.
*   **Phương pháp thực hiện:**
    1.  Trong hàm khởi tạo hoặc cấu hình của `AppDbContext`, sau khi thiết lập kết nối, thực thi lệnh PRAGMA:
        ```csharp
        using (var connection = Database.GetDbConnection())
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA journal_mode=WAL;";
                command.ExecuteNonQuery();
            }
        }
        ```
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Chế độ WAL có hoạt động ổn định trên phân quyền máy Lab trường học không?
    *   *Tối ưu:* WAL hoạt động tuyệt đối tốt trên ổ cứng cục bộ (Local Drive). Chỉ có rủi ro nếu file DB đặt trên ổ đĩa mạng chia sẻ (Network Share) - điều mà ứng dụng SmartClass không làm. Chế độ WAL cho phép đọc song song mà không bị block bởi luồng ghi, rất phù hợp với ứng dụng WPF đa luồng.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Chạy ứng dụng, vào thư mục chứa dữ liệu và kiểm tra sự xuất hiện của file `smartclass.db-wal` khi thực hiện ghi log.
    - [ ] [ ] Kiểm tra log đầu ra xem có bất kỳ lỗi `SqliteException (0x80004005): database is locked` nào phát sinh khi gửi file liên tục hay không.

---

### Hạng mục 5: Mã hóa nội dung tệp bản nháp viết tay (`.tmp`) bằng DPAPI phối hợp khóa đối xứng AES
*   **Yêu cầu thiết kế:** Bảo mật tuyệt đối quyền riêng tư bản nháp làm bài của học sinh. Học sinh ca sau dùng File Explorer không thể mở đọc trộm nội dung file tạm `.tmp` của học sinh ca trước trên đĩa.
*   **Dữ liệu đầu vào:** Chuỗi văn bản bài làm tự do của học sinh (Plaintext).
*   **Dữ liệu đầu ra:** File `.tmp` lưu nội dung đã mã hóa (Ciphertext).
*   **Phương pháp thực hiện:**
    1.  *Thiết kế thuật toán mã hóa:*
        *   Tạo khóa AES động từ tổ hợp: `Khóa nội bộ cứng (AppSalt) + Mã số học sinh đăng nhập (StudentCode)`.
        *   Mã hóa nội dung văn bản bằng AES-256.
        *   Bọc thêm một lớp bảo vệ bên ngoài bằng DPAPI của Windows (`System.Security.Cryptography.ProtectedData.Protect` với `DataProtectionScope.CurrentUser`).
    2.  *Khi Lưu (Write):* Mã hóa chuỗi văn bản -> Ghi mảng byte kết quả xuống file `autosave_draft_{studentCode}.tmp`.
    3.  *Khi Đọc (Read):* Giải mã file -> Nếu gặp lỗi giải mã (do file bị sửa đổi, rỗng hoặc tài khoản Windows khác truy cập), bỏ qua lỗi, xóa file lỗi và hiển thị vùng soạn thảo trống (tránh crash ứng dụng).
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao chỉ dùng DPAPI là chưa đủ?
    *   *Tối ưu:* Nếu phòng Lab dùng chung 1 tài khoản đăng nhập Windows duy nhất (ví dụ: user "Student") cho tất cả học sinh, thì DPAPI chỉ bảo mật ở cấp độ User Windows, học sinh đăng nhập ca sau vẫn giải mã được của ca trước. Do đó, việc kết hợp AES khóa động dựa trên `StudentCode` (mã định danh riêng của từng học sinh trong ứng dụng) sẽ đảm bảo cô lập dữ liệu 100%.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Đăng nhập Học sinh A, viết nội dung nháp -> Đăng xuất. Dùng phần mềm Notepad mở file `.tmp` kiểm tra xem nội dung đã bị mã hóa thành ký tự vô nghĩa chưa.
    - [ ] [ ] Đăng nhập Học sinh B trên cùng máy tính, kiểm tra xem giao diện soạn thảo có hiển thị trống (không tải nhầm bài của A) hay không.
    - [ ] [ ] Sửa đổi nội dung file `.tmp` bằng Notepad để làm lỗi file cấu trúc, khởi chạy app với tài khoản A xem app có tự động bỏ qua lỗi và chạy bình thường không.

---

### Hạng mục 6: Thanh tiến độ viết bài trực quan đổi màu sắc động theo mục tiêu từ
*   **Yêu cầu thiết kế:** Kích thích động lực viết của học sinh bằng cách đổi màu thanh tiến độ: <50% màu Đỏ tươi, 50-99% màu Cam, >=100% màu Xanh lá cây sáng.
*   **Dữ liệu đầu vào:** Số lượng từ hiện tại (`int`), Mục tiêu từ (`int`).
*   **Dữ liệu đầu ra:** Thuộc tính `Foreground` của ProgressBar thay đổi màu sắc tương ứng.
*   **Phương pháp thực hiện:**
    1.  Xây dựng một `IValueConverter` trong C# đặt tên là `WordCountToColorConverter`:
        *   Nhận giá trị phần trăm (`CurrentCount / TargetCount * 100`).
        *   Nếu phần trăm `< 50`: Trả về `SolidColorBrush` màu đỏ `#FF4D4D`.
        *   Nếu phần trăm từ `50` đến `99`: Trả về `SolidColorBrush` màu cam `#FFA500`.
        *   Nếu phần trăm `>= 100`: Trả về `SolidColorBrush` màu xanh lá `#2ECC71`.
    2.  Tại XAML, liên kết thuộc tính `Foreground` của ProgressBar với thuộc tính phần trăm và áp dụng converter này.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Có nên dùng hiệu ứng chuyển màu mượt mà (Gradient Color transition) thay vì đổi màu đột ngột?
    *   *Tối ưu:* Hiệu ứng đổi màu đột ngột ở các mốc ranh giới (50%, 100%) mang tính sư phạm cao hơn, giúp học sinh nhận thức rõ ràng cột mốc hoàn thành nhiệm vụ của mình.
    *   *Phòng ngừa lỗi:* Nếu `TargetCount` bằng 0, gán mặc định phần trăm là 100% và đổi màu xanh lá để tránh lỗi chia cho 0 (`DivideByZeroException`).
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Đặt mục tiêu từ là `100`. Gõ `10` từ -> Kiểm tra thanh tiến độ có màu Đỏ không.
    - [ ] [ ] Gõ `60` từ -> Kiểm tra thanh tiến độ có tự động chuyển màu Cam không.
    - [ ] [ ] Gõ `100` từ -> Kiểm tra thanh tiến độ có chuyển màu Xanh lá không.
    - [ ] [ ] Đặt mục tiêu từ là `0`, kiểm tra xem hệ thống có hoạt động bình thường, không crash không.

---

### Hạng mục 7: Đồng bộ nhật ký sự kiện (`EventLogs`) về máy chủ giáo viên định kỳ
*   **Yêu cầu thiết kế:** Gửi các log sự kiện cục bộ của học sinh về server giáo viên để ban giám hiệu giám sát hoạt động phòng máy và giải quyết tranh chấp điểm số.
*   **Dữ liệu đầu vào:** Các bản ghi trong bảng `EventLogs` ở SQLite cục bộ có cột `Synced = 0`.
*   **Dữ liệu đầu ra:** Gói tin mạng TCP chứa mảng JSON các bản ghi log gửi đi. Cập nhật trạng thái `Synced = 1` sau khi nhận phản hồi ACK.
*   **Phương pháp thực hiện:**
    1.  Tạo một tác vụ nền (`BackgroundWorker` hoặc `Task.Run` lặp vô tận sau mỗi 5 phút).
    2.  Truy vấn SQLite lấy tối đa 100 bản ghi chưa đồng bộ (`Synced = 0`).
    3.  Chuyển đổi danh sách bản ghi thành chuỗi JSON.
    4.  Gửi qua TCP Socket đang kết nối với máy chủ giáo viên với Header định danh gói tin: `COMMAND:SYNC_LOGS`.
    5.  Sau khi nhận được chuỗi phản hồi ACK thành công từ Server, cập nhật trạng thái `Synced = 1` cho các bản ghi đó trong SQLite bằng một Transaction.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao không gửi log ngay lập tức khi sự kiện xảy ra?
    *   *Tối ưu:* Gửi log tức thời (real-time) sẽ gây nghẽn băng thông mạng LAN phòng máy khi cả lớp học sinh (40-50 máy) thực hiện các thao tác cùng lúc. Cơ chế gửi gom lô (batching) định kỳ 5 phút giúp tối ưu tải cho máy chủ giáo viên và bảo toàn tài nguyên máy trạm.
    *   *An toàn dữ liệu:* Mỗi bản ghi log phải có một thuộc tính ID kiểu `Guid` (UUID). Nếu xảy ra rớt mạng khi đang nhận phản hồi ACK (dẫn đến client chưa cập nhật `Synced = 1` và sẽ gửi lại log ở chu kỳ sau), máy chủ giáo viên dựa vào `Guid` này để loại bỏ các bản ghi trùng lặp (Idempotency).
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Ngắt mạng LAN, tạo ra các log sự kiện (nộp bài, viết bài). Kiểm tra bảng `EventLogs` xem trạng thái có lưu `Synced = 0` không.
    - [ ] [ ] Bật lại mạng LAN, kiểm tra sau chu kỳ 5 phút các log đó có được gửi đi và cập nhật thành `Synced = 1` trong SQLite cục bộ không.
    - [ ] [ ] Gửi thử dữ liệu trùng lặp Guid lên server và kiểm tra server không ghi đè hoặc tạo bản ghi trùng lặp trong DB của giáo viên.

---

### Hạng mục 8: Cho phép thu gọn/mở rộng bảng hướng dẫn viết bài ở phía bên phải màn hình
*   **Yêu cầu thiết kế:** Cho phép học sinh ẩn bảng cẩm nang hướng dẫn bên phải để tăng tối đa diện tích khung soạn thảo văn bản khi làm bài thi/bài luận dài.
*   **Dữ liệu đầu vào:** Nhấp chọn nút "Thu gọn hướng dẫn" / "Hiện hướng dẫn".
*   **Dữ liệu đầu ra:** Bố cục màn hình thay đổi kích thước linh hoạt.
*   **Phương pháp thực hiện:**
    1.  Tại Grid chia bố cục màn hình:
        *   Cột 1 (Soạn bài): `Width="*"`.
        *   Cột 2 (Splitter ngăn cách): `Width="Auto"`.
        *   Cột 3 (Cẩm nang hướng dẫn): `Width="300"`.
    2.  Đặt bảng hướng dẫn và Splitter vào trong các thẻ XML được đặt tên rõ ràng.
    3.  Khi nhấp nút Toggle:
        *   Nếu đang hiển thị: Đặt `Visibility` của Cột 3 và GridSplitter thành `Collapsed`. Thay đổi icon nút Toggle thành `<` hoặc `«`.
        *   Nếu đang ẩn: Đặt `Visibility` của Cột 3 và GridSplitter thành `Visible`. Thay đổi icon nút Toggle thành `>` hoặc `»`.
    4.  Lưu trạng thái ẩn/hiện này vào tệp `settings.json` để giữ cấu hình cho phiên làm việc tiếp theo.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Tại sao phải ẩn cả GridSplitter?
    *   *Tối ưu:* Nếu không ẩn GridSplitter, học sinh có thể vô tình kéo chuột ở biên cột và làm phát sinh các lỗi vỡ layout hoặc tạo khoảng trống vô nghĩa bên phải màn hình.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Nhấp nút Thu gọn -> Kiểm tra vùng soạn thảo văn bản tự động giãn rộng ra chiếm trọn 100% chiều ngang màn hình.
    - [ ] [ ] Kéo giãn cửa sổ ứng dụng khi bảng hướng dẫn đang thu gọn -> Đảm bảo giao diện co giãn hoàn hảo không lỗi bố cục.
    - [ ] [ ] Tắt ứng dụng khi đang thu gọn, mở lại ứng dụng xem trạng thái thu gọn có được khôi phục không.

---

### Hạng mục 9: Tự động phân cấp thư mục lưu trữ file nhận từ giáo viên theo `ReceivedFiles/MonHoc_GiaoVien/`
*   **Yêu cầu thiết kế:** Sắp xếp tài liệu học tập ngăn nắp, tránh việc lưu phẳng tất cả các tệp nhận được vào một thư mục gốc duy nhất gây hỗn loạn và khó tìm kiếm cho học sinh.
*   **Dữ liệu đầu vào:** Thông tin file nhận từ Server giáo viên gửi sang: `FileName`, `SubjectName` (Tên môn), `TeacherName` (Tên giáo viên).
*   **Dữ liệu đầu ra:** Tệp tin được ghi thành công vào đường dẫn đĩa: `[Thư_Mục_Nhận_Mặc_Định]/[SubjectName]_[TeacherName]/[FileName]`.
*   **Phương pháp thực hiện:**
    1.  Xây dựng hàm xử lý chuỗi ký tự đặc biệt (Sanitize):
        *   Loại bỏ các ký tự cấm đặt tên thư mục trên hệ điều hành Windows bao gồm: `\ / : * ? " < > |`. Thay thế bằng dấu gạch dưới `_`.
        *   Loại bỏ các khoảng trắng thừa ở đầu và cuối chuỗi.
    2.  Tạo đường dẫn thư mục đích bằng: `Path.Combine(defaultPath, $"{subject}_{teacher}")`.
    3.  Gọi `Directory.CreateDirectory(targetFolder)` để tự động khởi tạo thư mục cha phân cấp nếu chưa tồn tại.
    4.  Ghi luồng dữ liệu file nhận được vào thư mục mới này.
*   **Phản biện & Tối ưu hóa:**
    *   *Phòng ngừa lỗi:* Nếu tên môn học hoặc giáo viên bị trống (null hoặc rỗng) do lỗi truyền dữ liệu từ Server, hệ thống sẽ tự động phân loại vào thư mục mặc định `ReceivedFiles/Chung/` thay vì crash tiến trình ghi file.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Gửi thử tệp tin với môn học có tên `"Tin Học: Lớp 10"` (chứa dấu hai chấm) và giáo viên `"Nguyễn Văn A/B"` (chứa dấu xuyệt).
    - [ ] [ ] Kiểm tra xem thư mục đích được tạo ra có tên là `"Tin Hoc_ Lop 10_Nguyen Van A_B"` (không chứa ký tự lỗi) không.
    - [ ] [ ] Đảm bảo file được ghi vào đúng thư mục phân cấp vừa tạo.

---

### Hạng mục 10: Tích hợp âm thanh hiệu ứng phản hồi nhẹ khi nộp bài thành công
*   **Yêu cầu thiết kế:** Mang lại cảm giác hứng thú và xác nhận rõ ràng về mặt âm thanh cho học sinh khi gửi file nộp bài thành công.
*   **Dữ liệu đầu vào:** Sự kiện gửi file thành công trả về từ socket hoặc hoàn thành đồng bộ ngoại tuyến.
*   **Dữ liệu đầu ra:** Âm thanh phản hồi ngắn phát ra loa hoặc tai nghe của học sinh.
*   **Phương pháp thực hiện:**
    1.  Nhúng một tệp âm thanh định dạng `.wav` chất lượng cao, thời lượng cực ngắn (~1 giây), âm thanh nhẹ nhàng (như tiếng chuông "ding" nhẹ) vào tài nguyên ứng dụng dưới dạng `Embedded Resource` hoặc tệp đính kèm thư mục Assets.
    2.  Sử dụng lớp `System.Media.SoundPlayer` để phát âm thanh:
        ```csharp
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QASmartClass.Assets.success.wav"))
        {
            if (stream != null)
            {
                using (var player = new SoundPlayer(stream))
                {
                    player.Play(); // Chạy bất đồng bộ mặc định
                }
            }
        }
        ```
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Âm thanh có gây ồn ào và làm mất tập trung trong phòng thi/phòng máy không?
    *   *Tối ưu:* Có! Do đó, bắt buộc phải có một tùy chọn checkbox trên giao diện cài đặt: `[x] Bật âm thanh thông báo` để học sinh hoặc giáo viên có thể tắt hoàn toàn nếu cần không gian yên tĩnh tuyệt đối.
    *   *Phòng ngừa lỗi:* Bọc lệnh phát âm thanh trong khối `try-catch` trống. Đảm bảo ứng dụng không crash nếu máy trạm của học sinh không gắn tai nghe/loa, hỏng driver âm thanh, hoặc tệp âm thanh bị lỗi.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Bấm nộp bài thành công -> Kiểm tra âm thanh phản hồi có phát ra loa/tai nghe không.
    - [ ] [ ] Tắt tùy chọn âm thanh trên giao diện cài đặt -> Kiểm tra nộp bài thành công không phát ra tiếng động.
    - [ ] [ ] Vô hiệu hóa thiết bị âm thanh của Windows (Disable Sound Card) -> Thực hiện nộp bài để đảm bảo phần mềm hoạt động bình thường, không phát sinh lỗi crash.

---

### Hạng mục 11: Biểu đồ mini phân tích xu hướng học tập và tần suất nộp bài của học sinh
*   **Yêu cầu thiết kế:** Cung cấp cho học sinh cái nhìn trực quan về quá trình luyện viết của mình trong tháng, thể hiện sự chăm chỉ thông qua số bài nộp và số lượng từ đã viết.
*   **Dữ liệu đầu vào:** Dữ liệu lịch sử truy vấn SQLite từ bảng `FileTransfers` và `EventLogs` lọc theo `StudentCode` hiện tại và thời gian `Timestamp` thuộc tháng hiện hành.
*   **Dữ liệu đầu ra:** Biểu đồ mini (Sparkline hoặc biểu đồ cột đơn giản) hiển thị trên giao diện.
*   **Phương pháp thực hiện:**
    1.  *Thiết kế biểu đồ tối giản bằng XAML:*
        *   Sử dụng một `ItemsControl` với hướng hiển thị ngang (`Horizontal StackPanel` hoặc `UniformGrid`).
        *   Mỗi phần tử đại diện cho một ngày trong tuần hoặc một đợt làm bài.
        *   Sử dụng các thẻ hình chữ nhật `<Rectangle>` có `Height` được liên kết (Binding) động với tỷ lệ số lượng từ của bài viết đó so với mốc mục tiêu.
    2.  *Query tối ưu:* Lập chỉ mục (Index) SQLite cho cột `StudentCode` và `Timestamp` trong bảng dữ liệu để đảm bảo câu lệnh truy vấn diễn ra tức thì (< 10ms) không gây đơ giao diện.
*   **Phản biện & Tối ưu hóa:**
    *   *Phản biện:* Có nên tích hợp thư viện biểu đồ lớn như LiveCharts hay OxyPlot?
    *   *Tối ưu:* Không nên. Máy tính phòng Lab trường học thường có cấu hình yếu và dùng Windows cũ, các thư viện vẽ biểu đồ nặng có thể gây chậm hiệu năng vẽ giao diện hoặc thiếu tương thích thư viện Directx. Tự vẽ bằng các thẻ Shape WPF cơ bản nhẹ hơn gấp 100 lần, ổn định tuyệt đối và dễ căn chỉnh style theo ý muốn.
*   **Bảng kiểm tra (Checksheet) cho Coder:**
    - [ ] [ ] Đăng nhập tài khoản mới chưa có bài làm -> Đảm bảo khu vực biểu đồ hiển thị thông báo thân thiện: `"Hãy bắt đầu bài viết đầu tiên để xem phân tích tiến độ!"` thay vì hiển thị biểu đồ trống rỗng, lỗi vỡ khung.
    - [ ] [ ] Làm 3 bài tập viết với các số lượng từ khác nhau -> Đảm bảo các cột biểu đồ vẽ đúng chiều cao tương quan trên màn hình.

---

## ═══ PHẦN 2: BẢNG KHỞI TẠO VÀ CẬP NHẬT CƠ SỞ DỮ LIỆU (SQLITE DB SPEC) ═══

Để hỗ trợ các tính năng đồng bộ nhật ký sự kiện, lưu trữ trạng thái đồng bộ và mã hóa, cấu trúc cơ sở dữ liệu SQLite cần được cập nhật như sau:

### 1. Script thiết lập chế độ WAL
Mỗi khi khởi động kết nối CSDL trong mã nguồn C#:
```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
```

### 2. Cập nhật Bảng `EventLogs`
Bổ sung cấu trúc để phục vụ đồng bộ nhật ký:
```sql
CREATE TABLE IF NOT EXISTS EventLogs (
    Id TEXT PRIMARY KEY,             -- Lưu GUID (UUID) dạng chuỗi để chống trùng lặp chéo
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

## ═══ PHẦN 3: BỘ KIỂM THỬ TÍCH HỢP TOÀN DIỆN (QA INTEGRATION TESTING) ═══

Lập trình viên và kiểm thử viên bắt buộc phải kiểm tra chéo (Double-Check) toàn bộ các tình huống sau trước khi tiến hành nghiệm thu mã nguồn.

### 📋 BẢNG KIỂM TRA TỔNG HỢP SAU NÂNG CẤP
| **8** | Kiểm thử phân tích tiến độ | Xem bảng biểu đồ tiến độ số từ trong 5 ngày làm bài viết liên tiếp. | Biểu đồ cột vẽ chính xác tỷ lệ số lượng từ theo đúng lịch sử lưu trong SQLite của học sinh hiện tại. | `[ ] Chưa test` |

---

## ═══ PHẦN 4: QUY CHUẨN FONT CHỮ HỌC THUẬT & KHOA HỌC (ACADEMIC FONTS SPECIFICATION) ═══

Để tránh tuyệt đối lỗi hiển thị ký tự (ô vuông lỗi font `☐` hoặc ký tự bị méo lệch hàng) khi chương trình biểu diễn các nội dung học thuật phức tạp (toán học, vật lý, hóa học, phiên âm IPA), lập trình viên bắt buộc phải chuẩn hóa và phân tách Font Family theo từng tình huống cụ thể thay vì áp dụng một font chữ cứng nhắc cho toàn bộ ứng dụng.

### 1. Phân loại Font Family theo phân hệ sử dụng
| Phân hệ / Loại văn bản | Font Family khuyến nghị | Vai trò & Đặc điểm kỹ thuật | Cách áp dụng trong XAML |
| :--- | :--- | :--- | :--- |
| **Giao diện & Văn bản chung** (General UI & Standard Text) | `Segoe UI`, `Roboto`, `Inter` | Hiển thị rõ nét tiếng Việt có dấu, hỗ trợ rendering High DPI tốt, nét chữ hiện đại. | `FontFamily="Segoe UI"` |
| **Công thức & Ký hiệu Toán/Lý/Hóa** (Mathematical & Scientific) | `Cambria Math`, `Times New Roman`, `Segoe UI Symbol`, `Cambria` | Chứa đầy đủ Glyph học thuật đặc biệt ($\Delta, \sum, \int, \sqrt{}, \pi, \alpha, \beta, \neq, \approx$, chỉ số trên/dưới) mà không bị lệch dòng. **Đặc biệt bắt buộc sử dụng `Times New Roman` cho chức năng vẽ vectơ để sửa lỗi hiển thị ký hiệu.** | `FontFamily="Cambria Math, Times New Roman, Segoe UI Symbol"` |
| **Phiên âm Quốc tế IPA** (English Phonetic Transcription) | `Segoe UI`, `Arial`, `Times New Roman` | Hỗ trợ trọn vẹn các Unicode Block `IPA Extensions` (U+0250 đến U+02AF) và `Spacing Modifier Letters` (U+02B0 đến U+02FF). | `FontFamily="Segoe UI, Times New Roman"` |
| **Mã nguồn & Dữ liệu máy tính** (Monospaced & Code View) | `Consolas`, `Cascadia Code`, `Courier New` | Các ký tự có độ rộng bằng nhau (Monospaced), dễ phát hiện lỗi cú pháp, cấu trúc JSON/XML thẳng hàng. | `FontFamily="Consolas, Cascadia Code"` |

### 2. Nguyên tắc triển khai cho Coder
*   **Không kế thừa thụ động**: Tuyệt đối không để các TextBlock công thức toán học hoặc phiên âm tiếng Anh tự động kế thừa `FontFamily` chung của Window hoặc UserControl. Phải khai báo `FontFamily` hiển thị cục bộ hoặc thông qua `Style` chuyên biệt.
*   **Chức năng vẽ Vectơ bắt buộc dùng Times New Roman**: Cấu hình cứng `FontFamily="Times New Roman"` cho tất cả các nhãn ký hiệu vectơ (như $\vec{a}$, $\overrightarrow{AB}$, tọa độ đỉnh) trên Canvas vẽ vectơ để xử lý dứt điểm lỗi hiển thị font.
*   **Thứ tự ưu tiên Font Fallback**: Trong XAML, luôn khai báo chuỗi danh sách Font cách nhau bằng dấu phẩy (ví dụ: `FontFamily="Cambria Math, Times New Roman, Segoe UI Symbol, Cambria"`). Nếu hệ điều hành Windows cũ thiếu font thứ nhất, nó sẽ tự động tìm font thứ hai để thay thế, tránh việc trả về font hệ thống lỗi.
*   **Không hardcode font lạ**: Không được sử dụng các font tải bên ngoài mà Windows không có sẵn (như các font chuyên toán tự chế), trừ khi font đó được nhúng thẳng dưới dạng tài nguyên ứng dụng (`/Assets/Fonts/#FontName`).

### 3. Checksheet kiểm thử Font (QA Checksheet)
- [ ] [ ] Kiểm tra hiển thị các ký tự toán học đặc biệt như $\Delta, \alpha, \beta, \gamma, \sum, \int, \neq, \approx, \sqrt{x}$ trong khung kết quả giải hệ phương trình và phương trình bậc 2 xem có bị ô vuông lỗi font `☐` hay không.
- [ ] [ ] Kiểm tra chức năng vẽ vectơ (Vector Drawing Canvas): Đảm bảo các ký hiệu vectơ ($\vec{v}$, $\overrightarrow{AB}$, mũi tên chỉ hướng) và tọa độ tương quan hiển thị đúng định dạng `Times New Roman`, không bị mất nét hoặc hiển thị ô vuông lỗi.
- [ ] [ ] Kiểm tra hiển thị phiên âm tiếng Anh như `/æ/`, `/θ/`, `/ʃ/`, `/dʒ/` có đúng vị trí, không bị biến dạng ký tự.
- [ ] [ ] Thay đổi kích thước cỡ chữ (FontSize) từ 12 lên 24, kiểm tra xem các ký hiệu chỉ số trên/dưới (Superscript/Subscript) có bị lệch dòng baseline so với ký tự chính hay không.
- [ ] [ ] Mở ứng dụng trên máy chạy Windows 10 và Windows 11 để đảm bảo tính đồng bộ hiển thị font chữ.
