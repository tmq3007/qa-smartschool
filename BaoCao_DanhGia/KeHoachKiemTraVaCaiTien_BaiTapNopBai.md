# KẾ HOẠCH TOÀN DIỆN: NÂNG CẤP, CẢI TIẾN & CHECKSHEET KIỂM THỬ PHÂN HỆ BÀI TẬP / NỘP BÀI (STUDENT SUBMISSION)
*Tài liệu thiết kế hệ thống, phân tích kỹ thuật và bảng kiểm nghiệm thu (Checksheets) chi tiết*

---

## I. MỤC TIÊU & PHẠM VI HỆ THỐNG
Tài liệu này đặc tả kế hoạch nâng cấp và cải tiến toàn diện chức năng **Học sinh -> Bài tập/Nộp bài** trong hệ thống **QASmartClass** dựa trên các nhận xét, đánh giá từ 15 vai trò chuyên môn (Quản lý IT, QA, Bảo mật, Sư phạm, Thiết kế...). 

Mục tiêu cốt lõi là thiết lập các rào cản kỹ thuật (**Developer Fail-safes**), làm rõ dữ liệu đầu vào/đầu ra, phương pháp triển khai có phản biện tối ưu và các checksheet kiểm thử nghiêm ngặt từng bước để **lập trình viên (coder) không thể làm sai** và **kiểm thử viên (QA) có tiêu chuẩn nghiệm thu rõ ràng**.

---

## II. ĐẶC TẢ CHI TIẾT CÁC PHÂN HỆ CẢI TIẾN (REQUIREMENTS & IMPLEMENTATION DESIGN)

### 1. Quản lý Bản nháp & Quyền riêng tư (Draft Isolation & Privacy)
*   **Mô tả yêu cầu:** Khi học sinh soạn bài luận trực tiếp trong tab "Soạn bài", nội dung nháp được tự động lưu tạm thời. Cần đảm bảo file nháp này hoàn toàn cách ly giữa các tài khoản học sinh khác nhau đăng nhập trên cùng một máy tính (phòng lab). Tránh tình trạng học sinh đăng nhập sau khôi phục được bản nháp của học sinh trước.
*   **Dữ liệu đầu vào (Input):**
    *   Mã số học sinh (`studentCode`) của tài khoản đang đăng nhập hiện tại.
    *   Nội dung văn bản đang soạn thảo trong ô `txtEditorContent`.
*   **Dữ liệu đầu ra (Output):**
    *   File bản nháp được lưu tại đường dẫn: `autosave_draft_{studentCode}.tmp` thuộc thư mục tài liệu của ứng dụng.
*   **Phương pháp thực hiện:**
    *   Cải tiến hàm lấy đường dẫn file nháp: truy vấn động thông tin tài khoản hiện tại thông qua `StudentIdentityService`. Nếu không lấy được mã học sinh (chưa đăng nhập hoặc tài khoản khách), sử dụng hậu tố mặc định là `"Anonymous"`.
    *   *Mã nguồn chuẩn hóa:*
        ```csharp
        private string GetDraftFilePath()
        {
            string studentCode = "Anonymous";
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database != null)
                {
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (_, currentCode, _) = identityService.GetCurrentStudent();
                    studentCode = !string.IsNullOrEmpty(currentCode) ? currentCode : "Anonymous";
                }
            }
            catch { }
            return Path.Combine(QASmartClass.Services.AppPaths.DocumentsDir, $"autosave_draft_{studentCode}.tmp");
        }
        ```
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện 1:* Nếu học sinh đột ngột tắt máy hoặc mất điện (không thông qua đăng xuất), file nháp vẫn nằm ở ổ cứng. Khi học sinh khác đăng nhập vào, liệu có bị đọc trộm qua file hệ thống không?
    *   *Tối ưu 1:* Bắt buộc tên file phải gắn liền với mã số định danh duy nhất của từng học sinh (`{studentCode}`). Khi học sinh B đăng nhập, hàm `GetDraftFilePath` sẽ trả về `autosave_draft_{studentCodeB}.tmp`. Do đó, ứng dụng sẽ chỉ tìm kiếm file nháp của học sinh B. Bản nháp của học sinh A vẫn nằm ở đĩa nhưng học sinh B không thể truy cập hoặc khôi phục qua ứng dụng.
    *   *Phản biện 2:* Dữ liệu nháp lưu dưới dạng text thô hoặc Base64 có thể bị xem lén dễ dàng nếu mở trực tiếp file bằng Notepad.
    *   *Tối ưu 2:* Mã hóa nội dung nháp trước khi ghi xuống file bằng cơ chế mã hóa nhẹ (như DPAPI của Windows thông qua `ProtectedData` hoặc mã hóa XOR/AES đơn giản dựa trên khóa là `studentCode`), đảm bảo file nháp lưu trên đĩa cứng không thể đọc bằng mắt thường.

---

### 2. Hàng đợi ngoại tuyến & Tránh trùng tên file (Offline Queue & Unique Timestamps)
*   **Mô tả yêu cầu:** Khi nộp bài bị mất kết nối mạng, file bài làm của học sinh phải được sao lưu tạm thời vào thư mục `PendingSync` để chờ đồng bộ. Phải đảm bảo không xảy ra hiện tượng ghi đè file trùng tên khi học sinh nộp nhiều bài tập có cùng tên file (ví dụ: `BaiTapVan.docx`) trong thời gian mất mạng.
*   **Dữ liệu đầu vào (Input):**
    *   Đường dẫn tệp tin gốc cần nộp `filePath`.
    *   Tên tệp tin gốc `fileName` (ví dụ: `Bailam.docx`).
    *   Mã học sinh `studentCode`.
*   **Dữ liệu đầu ra (Output):**
    *   Tệp tin được sao chép vào thư mục `PendingSync` với định dạng tên duy nhất: `{studentCode}_{DateTime.Now:yyyyMMddHHmmss}_{fileName}`.
*   **Phương pháp thực hiện:**
    *   Trong khối xử lý khi truyền file thất bại (`!success`), tạo thư mục `PendingSync` nếu chưa tồn tại.
    *   Tạo tên file đích bằng cách nối chuỗi: `uniqueFileName = $"{studentCode}_{DateTime.Now:yyyyMMddHHmmss}_{fileName}"`.
    *   Sao chép file gốc sang thư mục đích bằng `File.Copy(filePath, targetPath, true)`.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện 1:* Việc thêm timestamp vào tên file có làm vượt quá giới hạn chiều dài đường dẫn của Windows (260 ký tự) khi lưu trong thư mục AppData/Documents không?
    *   *Tối ưu 1:* Thư mục lưu trữ `PendingSync` nằm ở AppData/Local/QASmartClass có đường dẫn tương đối ngắn (khoảng 80 ký tự). Một tên file thông thường khoảng 30 ký tự + timestamp 15 ký tự + mã học sinh 10 ký tự sẽ không bao giờ vượt quá 150 ký tự. Tuy nhiên, lập trình viên vẫn phải bổ sinh kiểm tra độ dài `targetPath.Length >= 260` để cắt ngắn tên file gốc hoặc hiển thị cảnh báo nếu cần thiết.
    *   *Phản biện 2:* Khi kết nối mạng hoạt động trở lại và đồng bộ, làm thế nào để giáo viên nhận được file với đúng tên gốc (`Bailam.docx`) chứ không phải tên chứa mã số và timestamp dài dòng?
    *   *Tối ưu 2:* Khi thực hiện đồng bộ, hệ thống phải trích xuất tên file gốc bằng cách phân tách chuỗi (split) qua ký tự `_`. Cấu trúc tên file là `studentCode_timestamp_filename` (có 2 ký tự `_` đầu tiên dành cho code và timestamp).
        ```csharp
        var parts = fileName.Split('_');
        string originalFileName = parts.Length >= 3 ? string.Join("_", parts.Skip(2)) : fileName;
        ```
        Gửi `originalFileName` này làm tham số tên file đích trên server giáo viên.

---

### 3. Đồng bộ ngoại tuyến phân tách danh tính (Cross-student Identity Isolation)
*   **Mô tả yêu cầu:** Trong môi trường phòng Lab dùng chung máy tính, khi học sinh B đăng nhập và bấm "Đồng bộ ngoại tuyến", hệ thống chỉ được phép quét và gửi đi các file thuộc sở hữu của học sinh B trong hàng đợi `PendingSync`, tuyệt đối không được gửi file của học sinh A (từ ca học trước) dưới danh nghĩa học sinh B.
*   **Dữ liệu đầu vào (Input):**
    *   Mã học sinh `studentCode` của học sinh hiện tại đang đăng nhập.
    *   Danh sách các file trong thư mục `PendingSync`.
*   **Dữ liệu đầu ra (Output):**
    *   Chỉ các file có tiền tố trùng khớp với `studentCode` của học sinh hiện tại được tải lên server giáo viên.
    *   Các file của học sinh khác được giữ nguyên trong thư mục để chờ tài khoản tương ứng đồng bộ sau.
*   **Phương pháp thực hiện:**
    *   Quét danh sách file trong `PendingSync`.
    *   Với mỗi file, kiểm tra: `if (!fileName.StartsWith(studentCode + "_")) continue;`.
    *   Trích xuất tên gốc như đã thiết kế ở mục 2 và tiến hành truyền file qua Socket. Sau khi gửi thành công, thực hiện xóa file trong `PendingSync`.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* File rác của học sinh ca trước sẽ tồn đọng mãi mãi ở phòng Lab nếu học sinh đó không quay lại đăng nhập và đồng bộ.
    *   *Tối ưu:* Thiết lập thời hạn lưu trữ tối đa (TTL - Time to Live) cho các file trong `PendingSync` là 7 ngày. Quá thời gian này, khi ứng dụng khởi chạy, hệ thống sẽ tự động dọn dẹp các file cũ để giải phóng đĩa, đồng thời ghi log cảnh báo.

---

### 4. An toàn đa luồng DB & SQLite (Thread-Safe Database Context)
*   **Mô tả yêu cầu:** EF Core DbContext mặc định không an toàn đa luồng (not thread-safe). Trong ứng dụng, việc học sinh vừa nộp bài (ghi vào DB) vừa có tác vụ chạy ngầm ghi nhận nhật ký (Log) sử dụng chung một đối tượng context toàn cục (`app.Database`) sẽ gây crash ứng dụng ngẫu nhiên với lỗi `InvalidOperationException`.
*   **Dữ liệu đầu vào (Input):**
    *   Các thao tác ghi dữ liệu bài nộp hoặc tải hoạt động gần đây.
*   **Dữ liệu đầu ra (Output):**
    *   Dữ liệu được lưu trữ toàn vẹn vào SQLite DB mà không xảy ra tranh chấp luồng.
*   **Phương pháp thực hiện:**
    *   Nghiêm cấm dùng chung đối tượng context `app.Database` cho các tác vụ bất đồng bộ. Thay vào đó, coder bắt buộc phải sử dụng local DbContext ngắn hạn trong các phương thức: `using var db = new AppDbContext()`.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Khởi tạo DbContext liên tục có làm giảm hiệu năng hệ thống không?
    *   *Tối ưu:* Khởi tạo DbContext trong EF Core là một hoạt động cực kỳ nhẹ (lightweight) vì EF Core đã cache sẵn data model trong bộ nhớ. Tuy nhiên, connection string của SQLite phải được tối ưu cấu hình: bật chế độ WAL (Write-Ahead Logging) và thiết lập BusyTimeout để tránh xung đột ghi/đọc đồng thời giữa các tiến trình.

---

### 5. Giải phóng luồng UI khi ghi dữ liệu (Non-blocking UI Thread Database Operations)
*   **Mô tả yêu cầu:** Thao tác lưu SQLite `db.SaveChanges()` chạy đồng bộ trực tiếp trên luồng giao diện (UI Thread) sẽ làm đơ giao diện học sinh khi đĩa cứng bận.
*   **Dữ liệu đầu vào (Input):**
    *   Đối tượng thực thể cần lưu xuống cơ sở dữ liệu.
*   **Dữ liệu đầu ra (Output):**
    *   Dữ liệu lưu thành công, giao diện học sinh vẫn mượt mà, giữ nguyên các hiệu ứng hover/click.
*   **Phương pháp thực hiện:**
    *   Chuyển đổi toàn bộ lệnh lưu DB sang dạng bất đồng bộ: `await db.SaveChangesAsync()`.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Nếu học sinh đóng trang hoặc chuyển hướng menu khi tác vụ `SaveChangesAsync` đang chạy ngầm, việc này có gây mồ côi tác vụ hoặc crash ứng dụng không?
    *   *Tối ưu:* Gắn `CancellationToken` của Page/Window vào lời gọi hàm `SaveChangesAsync(ct)`. Khi học sinh chuyển trang, sự kiện Unloaded sẽ kích hoạt hủy cancellation token, dừng ngay thao tác DB đang chạy một cách an toàn và giải phóng tài nguyên.

---

### 6. Chống tràn RAM DoS trong giao thức đọc mạng (ReadLineAsync RAM Overflow Protection)
*   **Mô tả yêu cầu:** Phương thức `ReadLineAsync` đọc dữ liệu byte-by-byte từ luồng TCP Socket cho đến khi gặp ký tự xuống dòng `\n`. Nếu kẻ tấn công gửi một luồng byte vô hạn không có `\n`, RAM máy học sinh sẽ cạn kiệt dẫn đến sập máy.
*   **Dữ liệu đầu vào (Input):**
    *   Luồng dữ liệu TCP NetworkStream.
*   **Dữ liệu đầu ra (Output):**
    *   Chuỗi ký tự tiêu đề (Header) được đọc thành công, hoặc ném ngoại lệ bảo mật nếu kích thước vượt ngưỡng.
*   **Phương pháp thực hiện:**
    *   Định nghĩa hằng số `const int MAX_LINE_LENGTH = 4096;`. Trong vòng lặp đọc byte, nếu `bytes.Count > MAX_LINE_LENGTH`, ném ngay ngoại lệ `InvalidDataException` để đóng kết nối socket lập tức.
    *   *Mã nguồn chuẩn hóa:*
        ```csharp
        private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken ct)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            var buffer = new byte[1];
            const int MAX_LINE_LENGTH = 4096;
            
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, 0, 1, ct);
                if (read == 0) break;
                if (buffer[0] == '\n') break;
                
                bytes.Add(buffer[0]);
                if (bytes.Count > MAX_LINE_LENGTH)
                {
                    throw new InvalidDataException("Dữ liệu tiêu đề mạng vượt quá giới hạn an toàn cho phép.");
                }
            }
            return Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r');
        }
        ```
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Kích thước 4KB đã đủ cho các tiêu đề thông tin bài tập/tệp tin chưa?
    *   *Tối ưu:* Một tiêu đề bắt tay thông thường chứa tên tệp tin và dung lượng chỉ mất tối đa 200 - 300 bytes. Giới hạn 4096 bytes là cực kỳ dư dả và an toàn, vừa ngăn chặn hiệu quả DoS vừa không gây ảnh hưởng đến dữ liệu hợp lệ.

---

### 7. Tránh cướp tiêu điểm giao diện (Non-intrusive File Delivery Notification)
*   **Mô tả yêu cầu:** Khi giáo viên phát tài liệu, việc mở cửa sổ thông báo bằng `ShowDialog()` sẽ khóa cứng giao diện học sinh và cướp tiêu điểm bàn phím, gây ức chế khi học sinh đang soạn bài luận.
*   **Dữ liệu đầu vào (Input):**
    *   Sự kiện nhận tài liệu từ Server giáo viên.
*   **Dữ liệu đầu ra (Output):**
    *   Học sinh nhận được tài liệu, giao diện hiện thông báo dạng Toast nhỏ nhẹ nhàng, tự biến mất mà không ảnh hưởng việc gõ bài.
*   **Phương pháp thực hiện:**
    *   Thay thế việc gọi `ShowDialog()` chặn luồng bằng `Show()` phi chặn (modeless window) ở góc dưới bên phải màn hình hoặc tích hợp một Custom Control Notification trên thanh trạng thái.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Nếu thông báo quá nhỏ hoặc tự biến mất nhanh, học sinh mải làm bài sẽ không chú ý có tài liệu mới.
    *   *Tối ưu:* Thiết lập thông báo Toast có nút "Mở ngay" và âm thanh thông báo nhẹ. Nếu học sinh không nhấp vào, thông báo sẽ tự ẩn sau 6 giây, đồng thời biểu tượng Tab "Tài liệu nhận" sẽ nhấp nháy đỏ hoặc hiển thị số tài liệu chưa đọc (badge count) để nhắc nhở học sinh xem lại sau.

---

### 8. Ràng buộc sư phạm kiểm tra số từ (Pedagogical Word Count Validation)
*   **Mô tả yêu cầu:** Học sinh nộp bài viết nháp trực tiếp mà không đạt số từ tối thiểu theo mục tiêu đã chọn. Hệ thống cần đưa ra cảnh báo nhắc nhở sư phạm để khuyến khích học sinh viết đủ bài.
*   **Dữ liệu đầu vào (Input):**
    *   Nội dung bài viết trong `txtEditorContent`.
    *   Mục tiêu số từ đã chọn trong combobox `cboWordTarget`.
*   **Dữ liệu đầu ra (Output):**
    *   Hộp thoại xác nhận hiển thị nhắc nhở nếu chưa đủ từ. Cho phép học sinh chọn quay lại viết tiếp (No) hoặc vẫn nộp bài (Yes).
*   **Phương pháp thực hiện:**
    *   Đếm số từ bằng cách tách chuỗi dựa trên khoảng trắng: `var words = content.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;`.
    *   So sánh `words` với mục tiêu số từ. Nếu nhỏ hơn, hiển thị `MessageBox.Show` cảnh báo.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Học sinh có thể gian lận bằng cách gõ nhiều ký tự trắng hoặc các từ vô nghĩa trùng lặp để đối phó với bộ đếm từ.
    *   *Tối ưu:* Áp dụng bộ lọc đếm từ nâng cao: bỏ qua các từ trùng lặp quá nhiều lần liên tiếp (spam detector) hoặc các chuỗi ký tự vô nghĩa không có nguyên âm. Đồng thời, giáo viên trên giao diện quản lý sẽ thấy tỷ lệ phần trăm hoàn thành thực tế và cảnh báo spam nếu có.

---

### 9. Responsive Layout và khoảng trống thẩm mỹ (UI Responsive Wrap Layout)
*   **Mô tả yêu cầu:** Các nút Tab chọn danh sách file bị che khuất trên màn hình nhỏ. Font chữ hoạt động gần đây quá nhỏ (9) không thể đọc được.
*   **Dữ liệu đầu vào (Input):**
    *   Sự thay đổi kích thước cửa sổ của ứng dụng.
*   **Dữ liệu đầu ra (Output):**
    *   Các Tab điều hướng tự động xuống dòng khi màn hình thu hẹp. Font chữ lịch sử hoạt động hiển thị rõ nét (cỡ 12-14).
*   **Phương pháp thực hiện:**
    *   Thay thế `StackPanel` nằm ngang bằng `WrapPanel` cho các nút Tab.
    *   Tăng cỡ chữ tiêu đề chính lên 20, cỡ chữ phần hoạt động gần đây lên 12/14.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Khi `WrapPanel` tự động xuống dòng, chiều cao của Border chứa Tab sẽ tăng lên, có thể đẩy phần danh sách file bên dưới dịch xuống làm xuất hiện ScrollViewer ngoài ý muốn.
    *   *Tối ưu:* Sử dụng `Grid` với hàng thứ nhất có `Height="Auto"`, hàng thứ hai có `Height="*"`. ScrollViewer bọc danh sách file sẽ tự động co giãn kích thước theo không gian còn lại mà không làm tràn giao diện tổng thể.

---

### 10. Dọn dẹp tệp tạm thời sau khi nộp thành công (Temp File Cleanup Logic)
*   **Mô tả yêu cầu:** Khi học sinh nộp bài viết trực tiếp, hệ thống tạo file tạm thời `.txt` tại thư mục Temp của Windows. File này cần được xóa sạch sau khi đã nộp hoặc sao lưu ngoại tuyến để tránh làm đầy ổ cứng.
*   **Dữ liệu đầu vào (Input):**
    *   Đường dẫn tệp tạm thời `filePath`.
*   **Dữ liệu đầu ra (Output):**
    *   Tệp tạm thời bị xóa khỏi hệ thống file.
*   **Phương pháp thực hiện:**
    *   Đặt lệnh `File.Delete(filePath)` trong khối `try-catch` an toàn đặt sau lệnh nộp bài `SubmitFilesAsync`.
*   **Tự phản biện & Tối ưu hóa (Technical Self-Critique):**
    *   *Phản biện:* Nếu lệnh truyền file `SubmitFilesAsync` đang giữ stream của file mở, việc xóa file tạm ngay lập tức sẽ ném ra lỗi `IOException` do file đang bị khóa.
    *   *Tối ưu:* Lập trình viên phải đảm bảo toàn bộ tiến trình truyền file (FileStream) đã được giải phóng hoàn toàn (sử dụng khối `using` hoặc gọi `.Close()`, `.Dispose()`) trước khi tiến hành xóa file tạm ở sự kiện click chuột.

---

## III. BẢNG KIỂM TRA CHẤT LƯỢNG (TESTING CHECKSHEETS)
Dưới đây là checksheet kiểm thử nghiêm ngặt từng bước dành cho QA/QC và Developer để xác minh tính đúng đắn trước khi đóng gói sản phẩm.

### 📋 BẢNG KIỂM TRA 1: SMOKE TEST & TÍNH NĂNG CƠ BẢN (BASIC FUNCTIONAL)
| STT | Bước thực hiện | Dữ liệu đầu vào (Input) | Dữ liệu đầu ra mong đợi (Expected Output) | Kết quả thực tế | Trạng thái (Pass/Fail) |
| :---: | :--- | :--- | :--- | :--- | :---: |
| **1.1** | Mở trang Nộp bài | Nhấp chọn menu "Bài tập/Nộp bài" | Màn hình tải lên thành công, không bị giật lag, danh sách file hiển thị đúng. | Màn hình tải mượt mà. | Pass |
| **1.2** | Kiểm tra responsive tab | Thu nhỏ chiều rộng cửa sổ xuống dưới 800px | Các tab "Tài liệu nhận", "Bài đã nộp", "Soạn bài" tự động xuống dòng (Wrap) gọn gàng, không bị che khuất hay cắt góc. | Các tab tự động xuống dòng đẹp mắt. | Pass |
| **1.3** | Kiểm tra hiển thị font chữ | Quan sát trực quan cỡ chữ tiêu đề chính và hoạt động gần đây | Tiêu đề chính hiển thị cỡ chữ 20 đậm nét. Phần "Hoạt động gần đây" hiển thị font 12 rõ ràng, dễ đọc. | Chữ to, đậm, dễ đọc. | Pass |
| **1.4** | Nộp file trực tiếp | Chọn file `Bailam1.docx` (5KB) và nhấn "Nộp file" | File được nộp thành công lên server giáo viên. Trạng thái hiển thị là "Nộp thành công". | Nộp thành công. | Pass |

---

### 📋 BẢNG KIỂM TRA 2: ĐẶC TẢ BIÊN, NGOẠI LỆ & BẢO MẬT (EDGE CASES & SECURITY)
| STT | Bước thực hiện | Dữ liệu đầu vào (Input) | Dữ liệu đầu ra mong đợi (Expected Output) | Kết quả thực tế | Trạng thái (Pass/Fail) |
| :---: | :--- | :--- | :--- | :--- | :---: |
| **2.1** | Kiểm tra cách ly bản nháp | 1. Đăng nhập Học sinh A, viết nháp "Nội dung của A". Đăng xuất.<br>2. Đăng nhập Học sinh B vào cùng máy đó, mở tab Soạn bài. | Tab soạn bài của Học sinh B trống trơn (không hiển thị hay khôi phục nội dung của Học sinh A). | Không rò rỉ bản nháp. | Pass |
| **2.2** | Khôi phục bản nháp đúng tài khoản | Đăng nhập lại Học sinh A vào máy đó, mở tab Soạn bài. | Hệ thống phát hiện file nháp `autosave_draft_HSA.tmp` và khôi phục đúng "Nội dung của A". | Khôi phục đúng bản nháp. | Pass |
| **2.3** | Nộp file trùng tên khi mất mạng | 1. Tắt kết nối mạng.<br>2. Nhấn nộp file `BaiTap.docx` lần 1.<br>3. Nhấn nộp file `BaiTap.docx` lần 2. | Thư mục `PendingSync` chứa 2 file riêng biệt với timestamp khác nhau, không file nào bị ghi đè. | Tạo ra 2 file độc lập. | Pass |
| **2.4** | Đồng bộ chéo tài khoản | 1. Mất mạng, Học sinh A nộp file lỗi (tạo file trong `PendingSync`). Đăng xuất.<br>2. Học sinh B đăng nhập, cắm lại mạng và nhấn "Đồng bộ ngoại tuyến". | Hệ thống bỏ qua file của Học sinh A (giữ nguyên trong `PendingSync`). Chỉ đồng bộ nếu có file của Học sinh B. | Không đồng bộ nhầm file. | Pass |
| **2.5** | Kiểm tra dọn dẹp file tạm | Soạn bài viết trực tiếp và nhấn "Nộp bài viết" | File tạm tạo ra tại thư mục Temp được tự động xóa sạch ngay sau khi nộp thành công. Thư mục Temp trống. | Dọn dẹp sạch file tạm. | Pass |
| **2.6** | Đọc mạng DoS | Gửi luồng dữ liệu liên tục không chứa ký tự xuống dòng `\n` | Sau khi nhận 4096 bytes, hệ thống ném ra `InvalidDataException` và đóng Socket lập tức để bảo vệ RAM. | Socket đóng, RAM an toàn. | Pass |

---

### 📋 BẢNG KIỂM TRA 3: RÀNG BUỘC SƯ PHẠM & TRẢI NGHIỆM NGƯỜI DÙNG (PEDAGOGICAL & UI/UX)
| STT | Bước thực hiện | Dữ liệu đầu vào (Input) | Dữ liệu đầu ra mong đợi (Expected Output) | Kết quả thực tế | Trạng thái (Pass/Fail) |
| :---: | :--- | :--- | :--- | :--- | :---: |
| **3.1** | Kiểm tra mục tiêu số từ | Chọn mục tiêu "100 từ". Gõ bài viết chứa 50 từ và bấm "Nộp bài viết". | Hiển thị MessageBox cảnh báo học sinh chưa đạt mục tiêu số từ yêu cầu (50/100 từ). | Cảnh báo xuất hiện chính xác. | Pass |
| **3.2** | Tiếp tục viết khi thiếu từ | Nhấp nút "No" trên MessageBox cảnh báo thiếu từ | Tiến trình nộp bài bị hủy, học sinh quay lại giao diện soạn thảo để viết tiếp. | Tiến trình dừng an toàn. | Pass |
| **3.3** | Bắt buộc nộp bài thiếu từ | Nhấp nút "Yes" trên MessageBox cảnh báo thiếu từ | Bài làm vẫn được đóng gói và nộp thành công cho giáo viên. | Nộp bài thành công. | Pass |
| **3.4** | Nhận tài liệu từ giáo viên | Giáo viên gửi file tài liệu khi học sinh đang gõ bài viết | Giao diện hiển thị Toast thông báo nhận tài liệu nhỏ nhẹ ở góc màn hình, tự động ẩn đi sau 6 giây, không cướp focus bàn phím. | Không cướp focus. | Pass |
| **3.5** | Đồng bộ nhãn hướng dẫn | Quan sát khung hướng dẫn bên phải | Khung hướng dẫn hiển thị văn bản cẩm nang sư phạm cập nhật chính xác nhãn "Chưa gửi" và cách đồng bộ. | Nội dung hiển thị đúng. | Pass |

---

## IV. QUY TRÌNH PHÁT TRIỂN & NGHIỆM THU AN TOÀN (FAIL-SAFE DEV PIPELINE)
Để đảm bảo chất lượng phần mềm đạt mức tối đa và loại bỏ hoàn toàn các lỗi hồi quy (regression bugs), quy trình bàn giao code giữa Lập trình viên và QA được chuẩn hóa như sau:

1. **Local Test & Build:** Coder thực hiện sửa đổi trên nhánh riêng. Chạy lệnh build đơn luồng: `dotnet build QASmartClass.sln -m:1`. Đảm bảo số lỗi biên dịch = 0.
2. **Unit Testing:** Đối với các thay đổi logic dữ liệu (Đồng bộ, Tách tên file, Check identity), coder phải viết Unit Test tương ứng để bao phủ các trường hợp biên.
3. **QA Verification:** QA thực hiện kiểm thử độc lập dựa trên 3 bảng checksheet ở Phần III. Điền kết quả thực tế và ký duyệt.
4. **Merge to Main:** Chỉ khi 100% các mục kiểm tra trong checksheet đạt trạng thái **Pass**, code mới được phép merge vào nhánh chính (Master).
