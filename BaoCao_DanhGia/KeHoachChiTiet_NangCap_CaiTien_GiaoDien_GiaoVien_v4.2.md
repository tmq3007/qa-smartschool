# Kế hoạch Nâng cấp & Cải tiến Phân hệ Giáo viên (Teacher Portal) - Lên bản QA SmartClass v4.2

Tài liệu này đặc tả chi tiết kế hoạch thiết kế, tái cấu trúc mã nguồn, khắc phục các lỗi logic, tối ưu hóa cơ sở dữ liệu và cải tiến giao diện người dùng của phân hệ Giáo viên (`TeacherHub`), tuân thủ nghiêm ngặt bộ tiêu chuẩn thiết kế sư phạm và các ràng buộc kỹ thuật của phiên bản **QA SmartClass v4.2**.

---

## ══ BỘ TIÊU CHUẨN SƯ PHẠM & RÀNG BUỘC KỸ THUẬT v4.2 ÁP DỤNG ══

1. **Ràng buộc An toàn Dữ liệu Học bạ:** Tuyệt đối không để xảy ra sai lệch điểm số hoặc gán nhầm hành vi khen thưởng/kỷ luật giữa các học sinh trong lớp.
2. **Ràng buộc Trải nghiệm Giáo viên:** Giáo viên lớn tuổi phải sử dụng được phần mềm dễ dàng thông qua các bảng chỉ dẫn từng bước (Step-by-Step UI Guidance) và các hộp thoại hỗ trợ.
3. **Ràng buộc Cấu hình Động (Master Configuration Mode):** Tích hợp các tham số cấu hình linh hoạt trong hệ thống để nhà trường và giáo viên tự cấu hình các phương thức xử lý (Phê duyệt, Đối sánh điểm, Môn học) phù hợp với thực tế vận hành.
4. **Ràng buộc IT & Quản trị Hệ thống:** 
   * Không rò rỉ kết nối cơ sở dữ liệu (SQLite Connection Leak).
   * Giải phóng tài nguyên ngay khi đóng hoặc chuyển trang (`Unloaded` event).
   * Tránh hiện tượng ghi đè dữ liệu rác vào bộ nhớ đệm (Change Tracker Leak).

---

## ══ CHI TIẾT 12 HẠNG MỤC CẢI TIẾN & KHẮC PHỤC ══

### Hạng mục 1: Khắc phục lỗi rò rỉ kết nối CSDL tại Class MI Dashboard
* **Yêu cầu kỹ thuật:** Loại bỏ việc truyền `AppDbContext` qua constructor của [ClassMIDashboardView](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/ClassMIDashboardView.xaml.cs). Chuyển sang khởi tạo cục bộ trong sự kiện `Loaded` và gọi `Dispose()` giải phóng kết nối trong sự kiện `Unloaded`.
* **Dữ liệu đầu vào:** Sự kiện hệ thống `Loaded` và `Unloaded` của UserControl.
* **Dữ liệu đầu ra:** Trạng thái kết nối SQLite được đóng hoàn toàn sau khi đóng trang, giải phóng file lock.
* **Phương pháp thực hiện:**
  1. Loại bỏ tham số `AppDbContext db` khỏi constructor của [ClassMIDashboardView](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/ClassMIDashboardView.xaml.cs). Thay thế bằng constructor không tham số:
     ```csharp
     public ClassMIDashboardView()
     {
         InitializeComponent();
         Loaded += Page_Loaded;
         Unloaded += Page_Unloaded;
     }
     ```
  2. Định nghĩa biến toàn cục `private AppDbContext? _db;`.
  3. Trong `Page_Loaded`, khởi tạo `_db = new AppDbContext();` và gọi `LoadRosters();`.
  4. Trong `Page_Unloaded`, gọi:
     ```csharp
     _db?.Dispose();
     _db = null;
     ```
  5. Sửa phương thức gọi trong [TeacherHubWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/TeacherHubWindow.xaml.cs#L171-L175):
     ```csharp
     private void BtnNavClassMIDashboard_Click(object sender, RoutedEventArgs e)
     {
         ShowView<ClassMIDashboardView>();
     }
     ```

---

### Hạng mục 2: Khắc phục lỗi trùng tên học sinh và mồ côi bản ghi kỷ luật
* **Yêu cầu kỹ thuật:** Thay thế TextBox nhập tay họ tên học sinh `txtName` bằng một ComboBox chứa danh sách học sinh thực tế của lớp được chọn.
* **Dữ liệu đầu vào:** Danh sách học sinh của lớp đang hoạt động (`rosterId`) từ bảng `Students`.
* **Dữ liệu đầu ra:** Thu nhận chính xác `student.Id` khi giáo viên nhấn lưu, loại bỏ hoàn toàn việc gõ tay tự do.
* **Phương pháp thực hiện:**
  1. Trong [HomeroomDiaryPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/HomeroomDiaryPage.xaml.cs#L168-L170), tại hàm tạo cửa sổ `BtnAddDiscipline_Click`:
     * Thay thế `var txtName = new TextBox {...};` bằng `var cboStudents = new ComboBox { SelectedValuePath = "Id", DisplayMemberPath = "DisplayInfo" };`.
     * Định nghĩa class hiển thị phụ trợ hoặc nạp dữ liệu:
       ```csharp
       var studentList = _db.ClassRosterStudents
           .Where(rs => rs.RosterId == rosterId)
           .Join(_db.Students, rs => rs.StudentId, s => s.Id, (rs, s) => new { s.Id, DisplayInfo = $"{s.FullName} ({s.StudentCode})" })
           .ToList();
       cboStudents.ItemsSource = studentList;
       if (studentList.Any()) cboStudents.SelectedIndex = 0;
       ```
  2. Tại sự kiện click lưu, lấy ID trực tiếp:
     ```csharp
     if (cboStudents.SelectedValue == null) return;
     int selectedStudentId = (int)cboStudents.SelectedValue;
     var student = _db.Students.Find(selectedStudentId);
     ```

---

### Hạng mục 3: Bổ sung mức độ vi phạm và hình thức xử lý kỷ luật
* **Yêu cầu kỹ thuật:** Thêm điều khiển nhập liệu trên UI sổ chủ nhiệm để chọn `ViolationType` và `DisciplineLevel`. Cập nhật phương thức lưu trong `DisciplineService`.
* **Dữ liệu đầu vào:** Lựa chọn mức độ (Nhẹ/Trung bình/Nặng) và hình thức xử lý (Nhắc nhở, Phê bình trước lớp, Khiển trách trước hội đồng).
* **Dữ liệu đầu ra:** Lưu đầy đủ vào cột `ViolationType` và `DisciplineLevel` của bảng `DisciplineRecords`.
* **Phương pháp thực hiện:**
  1. Tại dialog thêm kỷ luật trong [HomeroomDiaryPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/HomeroomDiaryPage.xaml.cs), bổ sung:
     * ComboBox mức độ vi phạm: `cboViolationType` (Các giá trị: "Nhẹ", "Trung bình", "Nặng").
     * ComboBox hình thức kỷ luật: `cboDisciplineLevel` (Các giá trị: "Nhắc nhở", "Phê bình trước lớp", "Kỷ luật trước hội đồng").
     * Ẩn/hiện động các combobox này: Chỉ hiển thị khi `cboType.SelectedValue` là `"Discipline"` hoặc `"Warning"`. Nếu là `"Commendation"`, đổi nhãn thành "Cấp khen thưởng" (Lớp, Trường, Quận, Tỉnh).
  2. Thay đổi chữ ký phương thức [DisciplineService.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Services/DisciplineService.cs#L204):
     ```csharp
     public bool CreateHomeroomRecord(int studentId, string studentName, string className, string type, string reason, string reportedBy, string violationType, string disciplineLevel)
     ```
     Và gán trực tiếp vào thuộc tính của thực thể `DisciplineRecord` trước khi `SaveChanges()`.

---

### Hạng mục 4: Khắc phục lỗi rò rỉ Change Tracker khi Import Excel lỗi
* **Yêu cầu kỹ thuật:** Gọi lệnh dọn dẹp Change Tracker của EF Core trong khối xử lý ngoại lệ và Rollback để tránh tích tụ thực thể lỗi.
* **Dữ liệu đầu vào:** Trạng thái lỗi trong quá trình phân tích tệp Excel.
* **Dữ liệu đầu ra:** Bộ nhớ đệm Change Tracker sạch sẽ hoàn toàn.
* **Phương pháp thực hiện:**
  1. Trong phương thức `BtnImportExcel_Click` của [QuestionBankView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/QuestionBankView.xaml.cs#L380-L401):
     * Tại vị trí `transaction.Rollback();`, bổ sung ngay lệnh dọn dẹp bộ nhớ đệm:
       ```csharp
       _db.ChangeTracker.Clear();
       ```
     * Thực hiện tương tự trong khối `catch (Exception ex)` để đảm bảo khi xảy ra lỗi bất kỳ, Change Tracker vẫn được làm sạch trước khi đóng transaction.

---

### Hạng mục 5: Khắc phục lỗi dán điểm Excel trùng tên & Tích hợp cấu hình song song (Master Setup)
* **Yêu cầu kỹ thuật:** Hỗ trợ cấu hình song song hai phương thức xử lý trùng tên học sinh khi dán điểm từ Excel. Cho phép chọn phương thức qua Cài đặt hệ thống.
* **Cài đặt Hệ thống bổ sung:** Tham số cấu hình `GradingMatchDuplicateOption` (Kiểu string, giá trị: `"SkipAndWarn"` hoặc `"ShowSelectorDialog"`).
* **Dữ liệu đầu vào:** Dữ liệu clipboard dạng dòng `Mã_HS hoặc Họ_Tên <TAB> Điểm`.
* **Dữ liệu đầu ra:** Điểm số được điền chính xác, hành vi phù hợp với cấu hình thiết lập.
* **Phương pháp thực hiện:**
  1. Nếu cấu hình là `"SkipAndWarn"`: Hệ thống bỏ qua các dòng trùng tên, không dán điểm và đưa vào danh sách cảnh báo `duplicateNameWarnings` hiển thị cho giáo viên ở cuối phiên dán.
  2. Nếu cấu hình là `"ShowSelectorDialog"`: Khi gặp một dòng trùng tên trong danh sách dán:
     * Tạm dừng tiến trình dán điểm và hiển thị một cửa sổ hội thoại nhỏ `DuplicateStudentSelectorWindow` gồm:
       * Danh sách các học sinh trùng tên tìm thấy (Hiển thị đầy đủ Họ tên, Mã học sinh, Ảnh thẻ, Tên lớp).
       * Giáo viên nhấp chuột chọn đúng học sinh cần nhập điểm.
       * Điểm số của dòng đó sẽ được gán chính xác cho học sinh được chọn.
       * Cung cấp nút "Bỏ qua dòng này" để bỏ qua.

---

### Hạng mục 6: Khắc phục lỗi hardcode ClassId = 1 trong Kế hoạch Phụ đạo
* **Yêu cầu kỹ thuật:** Bổ sung ComboBox chọn lớp học `CbRoster` vào giao diện kế hoạch phụ đạo để thay thế hoàn toàn tham số hardcode.
* **Dữ liệu đầu vào:** Lớp học được chọn bởi giáo viên trên UI.
* **Dữ liệu đầu ra:** Danh sách cảnh báo học sinh yếu kém hiển thị chính xác theo lớp được chọn.
* **Phương pháp thực hiện:**
  1. Thêm ComboBox chọn lớp vào [RemedialPlanView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/RemedialPlanView.xaml):
     ```xml
     <StackPanel Orientation="Horizontal" Margin="0,0,0,15">
         <TextBlock Text="Chọn lớp dạy:" VerticalAlignment="Center" Margin="0,0,10,0" FontWeight="Bold"/>
         <ComboBox x:Name="CbRoster" Width="150" Height="30" SelectionChanged="CbRoster_SelectionChanged"/>
     </StackPanel>
     ```
  2. Trong [RemedialPlanView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/RemedialPlanView.xaml.cs):
     * Khởi tạo danh sách lớp dạy của giáo viên hiện tại trong `Page_Loaded` (tương tự như Dashboard).
     * Tại sự kiện `CbRoster_SelectionChanged`:
       ```csharp
       if (CbRoster.SelectedValue is int rosterId)
       {
           var warnings = _remedialService.GetEarlyWarningStudents(rosterId); // Truyền động RosterId thay vì số 1
           LvWarnings.ItemsSource = warnings;
       }
       ```

---

### Hạng mục 7: Chọn môn học phụ đạo động từ CSDL & Hỗ trợ nhập tự do (Khác)
* **Yêu cầu kỹ thuật:** Thay thế TextBox nhập môn học. Nạp danh mục môn học động từ bảng `SubjectMaster` trong CSDL. Hỗ trợ tùy chọn `"Môn học khác"` để hiển thị TextBox nhập tay khi cần thiết.
* **Dữ liệu đầu vào:** Bảng dữ liệu môn học `SubjectMaster`. Lựa chọn của giáo viên trên giao diện.
* **Dữ liệu đầu ra:** Tên môn học chuẩn chỉnh hoặc môn học tự do được lưu trữ đồng bộ.
* **Phương pháp thực hiện:**
  1. Thay thế TextBox môn học bằng ComboBox `cboSubject` kết hợp một TextBox ẩn `txtCustomSubject` đặt ngay bên dưới.
  2. Nạp dữ liệu môn học động:
     ```csharp
     var subjects = _db.SubjectMasters.Select(s => s.SubjectName).ToList();
     subjects.Add("Khác..."); // Tùy chọn nhập tự do
     cboSubject.ItemsSource = subjects;
     cboSubject.SelectedIndex = 0;
     ```
  3. Bắt sự kiện `SelectionChanged` của `cboSubject`:
     * Nếu chọn `"Khác..."`: Hiển thị `txtCustomSubject` (đặt thuộc tính `Visibility = Visibility.Visible`).
     * Ngược lại: Ẩn `txtCustomSubject` (`Visibility = Visibility.Collapsed`).
  4. Khi lưu, nếu chọn "Khác..." thì lấy giá trị từ `txtCustomSubject.Text`, ngược lại lấy từ `cboSubject.SelectedItem.ToString()`.

---

### Hạng mục 8: Lọc đề tài SKKN theo mã số duy nhất TeacherCode của giáo viên đăng nhập
* **Yêu cầu kỹ thuật:** Sửa phương thức truy vấn SKKN từ lọc theo tên đầy đủ (FullName) sang lọc theo mã định danh duy nhất (TeacherCode) để bảo mật tài liệu.
* **Dữ liệu đầu vào:** `TeacherCode` lấy từ `StaffSession.CurrentUser.TeacherCode`.
* **Dữ liệu đầu ra:** Danh sách các đề tài SKKN được lọc chính xác theo tác giả duy nhất.
* **Phương pháp thực hiện:**
  1. Thêm phương thức mới trong `SkknService.cs`:
     ```csharp
     public List<Skkn> GetSkknsByAuthorCode(string teacherCode)
     {
         return _db.Skkns.Where(s => s.CreatedBy == teacherCode).ToList();
     }
     ```
  2. Trong [SkknView.xaml.cs:L47](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/SkknView.xaml.cs#L47), sửa đổi:
     ```csharp
     string teacherCode = StaffSession.CurrentUser?.TeacherCode ?? "GV001";
     var list = _skknService.GetSkknsByAuthorCode(teacherCode);
     ```

---

### Hạng mục 9: Đặt mốc thời gian hết hạn Bảng tin mặc định vào cuối ngày (23:59:59)
* **Yêu cầu kỹ thuật:** Cấu hình thời điểm hết hạn tự động của thông báo bảng tin rơi vào giây cuối cùng của ngày được chọn thay vì lúc nửa đêm bắt đầu ngày.
* **Dữ liệu đầu vào:** Giá trị ngày được chọn trên `DatePicker` hết hạn (`DpExpiresAt.SelectedDate`).
* **Dữ liệu đầu ra:** Đối tượng `DateTime` có giờ là `23:59:59`.
* **Phương pháp thực hiện:**
  1. Trong [BulletinBoardPage.xaml.cs:L100-105](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/BulletinBoardPage.xaml.cs#L100-L105):
     * Khi lấy giá trị ngày hết hạn của bản tin:
       ```csharp
       if (DpExpiresAt.SelectedDate.HasValue)
       {
           var eDate = DpExpiresAt.SelectedDate.Value;
           bulletin.ExpiresAt = eDate.Date.AddDays(1).AddSeconds(-1); // Đặt mốc 23:59:59 của ngày được chọn
       }
       ```

---

### Hạng mục 10: Bổ sung chỉ dẫn sử dụng từng bước (Step-by-step UI Guidance) trên các trang phức tạp
* **Yêu cầu kỹ thuật:** Nhúng một panel thông tin hướng dẫn nghiệp vụ thu gọn (collapsible) ở đầu các màn hình chức năng phức tạp: Chấm điểm, Ngân hàng đề, Sổ chủ nhiệm, Lập kế hoạch phụ đạo.
* **Dữ liệu đầu vào:** Thiết kế giao diện Grid và ToggleButton XAML.
* **Dữ liệu đầu ra:** Panel chỉ dẫn xuất hiện rõ ràng khi giáo viên click nút "Hướng dẫn nghiệp vụ".
* **Phương pháp thực hiện:**
  1. Khai báo một vùng hiển thị hướng dẫn sư phạm trong file XAML (Ví dụ trong `TeacherGradingView.xaml`):
     ```xml
     <Expander Header="📖 HƯỚNG DẪN CHẤM ĐIỂM NHANH (3 BƯỚC)" Background="#EFF6FF" BorderBrush="#BFDBFE" BorderThickness="1" Margin="0,0,0,15" Padding="10">
         <TextBlock TextWrapping="Wrap" FontSize="13" LineHeight="18" Foreground="#1E3A8A">
             Bước 1: Chọn Lớp học và Môn học ở bộ lọc phía trên để tải bảng điểm.<LineBreak/>
             Bước 2: Copy bảng điểm từ Excel (Cột 1: Mã học sinh hoặc Họ tên, Cột 2: Điểm số).<LineBreak/>
             Bước 3: Click chọn cột điểm đích trên bảng này (Ví dụ: cột Miệng, 15 Phút) và nhấn "Dán điểm Excel" -> Nhấn "Lưu Bảng Điểm".
         </TextBlock>
     </Expander>
     ```

---

### Hạng mục 11: Lưu trữ toàn bộ mã nguồn dưới định dạng chuẩn UTF-8 with BOM
* **Yêu cầu kỹ thuật:** Chuyển đổi mã hóa các file source code C# (.cs) của `TeacherHub` bị lỗi font chữ chú thích sang mã hóa chuẩn UTF-8 with BOM để hiển thị dấu tiếng Việt hoàn hảo trên mọi môi trường phát triển.
* **Dữ liệu đầu vào:** Các file source code hiện tại.
* **Dữ liệu đầu ra:** Các tệp tin được ghi đè bằng mã hóa UTF-8 với dấu BOM (Byte Order Mark).
* **Phương pháp thực hiện:** Chạy một script Python tự động quét thư mục `TeacherHub` để đọc và mã hóa lại tất cả file `.cs` thành UTF-8 with BOM.

---

### Hạng mục 12: Cấu hình phông chữ học thuật Cambria Math/Times New Roman cho ký hiệu toán học
* **Yêu cầu kỹ thuật:** Khai báo phông chữ fallback cụ thể cho các TextBlock hiển thị nhận xét hoặc công thức trên bảng điểm.
* **Dữ liệu đầu vào:** Khai báo thuộc tính `FontFamily` trong XAML.
* **Dữ liệu đầu ra:** Ký hiệu công thức hiển thị sắc nét, không bị ô vuông.
* **Phương pháp thực hiện:**
  1. Cấu hình phông chữ trong [TeacherGradingView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/TeacherGradingView.xaml) cho cột hiển thị nhận xét và các ký hiệu liên quan:
     `FontFamily="Cambria Math, Times New Roman, Segoe UI Symbol, Segoe UI"`

---

### Hạng mục 13 (Mới): Tích hợp phê duyệt Kế hoạch Phụ đạo theo cấu hình trường (Master Settings)
* **Yêu cầu kỹ thuật:** Cho phép cấu hình quy trình phê duyệt kế hoạch phụ đạo tùy thuộc vào quy chế từng trường học.
* **Cài đặt Hệ thống bổ sung:** Tham số cấu hình `RemedialPlanRequiresApproval` (Kiểu boolean, giá trị `true` hoặc `false`).
* **Phương pháp thực hiện:**
  1. Khi giáo viên tạo kế hoạch phụ đạo:
     * Nếu `RemedialPlanRequiresApproval == true`: Trạng thái ban đầu của kế hoạch là `"Pending"`. Kế hoạch sẽ xuất hiện trong hàng đợi phê duyệt tại màn hình quản lý của Hiệu trưởng / Tổ trưởng chuyên môn. Giáo viên chưa thể tạo các buổi học thực tế cho đến khi kế hoạch chuyển trạng thái `"Approved"`.
     * Nếu `RemedialPlanRequiresApproval == false`: Trạng thái ban đầu là `"Approved"`. Giáo viên bắt đầu thực hiện phụ đạo ngay lập tức.
  2. Bổ sung liên kết duyệt trên bảng điều khiển trung tâm của Ban Giám hiệu.

---

## ══ BẢNG KIỂM TRA NGHIỆM THU CHI TIẾT (QA CHECKSHEET) ══

### Bước 1: Khắc phục rò rỉ kết nối DbContext tại Class MI Dashboard
- [ ] [ ] Kiểm tra xem constructor của `ClassMIDashboardView` đã chuyển về không tham số chưa.
- [ ] [ ] Xác nhận sự kiện `Page_Unloaded` có chứa lệnh `_db?.Dispose()` và `_db = null;`.
- [ ] [ ] Kiểm tra click chuyển đổi liên tục giữa "MI Dashboard Lớp" và "Dashboard" xem có lỗi `ObjectDisposedException` nào xảy ra không.
- [ ] [ ] **(Mới - Test Case 1.1)**: Mở đồng thời hai tab Class MI Dashboard trên hai màn hình khác nhau (Multi-Window), kiểm tra việc truy vấn dữ liệu đồng thời xem có ném lỗi xung đột database lock.

### Bước 2: Khắc phục trùng tên & mồ côi kỷ luật
- [ ] [ ] Xác nhận TextBox `txtName` đã được thay thế hoàn toàn bằng ComboBox học sinh trong dialog.
- [ ] [ ] Kiểm tra xem ComboBox có hiển thị đầy đủ thông tin: `Họ tên (Mã học sinh)` không.
- [ ] [ ] Kiểm tra trong cơ sở dữ liệu sau khi lưu xem cột `StudentId` của bản ghi kỷ luật có khớp chính xác ID của học sinh được chọn (không bằng 0).
- [ ] [ ] **(Mới - Test Case 2.1)**: Kiểm thử với lớp học có 0 học sinh (lớp mới lập). Xác nhận ComboBox hiển thị trống và vô hiệu hóa nút "Lưu lại" (không cho phép tạo bản ghi rỗng).

### Bước 3: Bổ sung ViolationType và DisciplineLevel
- [ ] [ ] Xác nhận có thêm 2 ComboBox chọn mức độ và hình thức xử lý khi chọn loại "Kỷ luật".
- [ ] [ ] Xác nhận các cột `ViolationType` và `DisciplineLevel` được ghi đầy đủ thông tin vào DB.
- [ ] [ ] **(Mới - Test Case 3.1)**: Kiểm thử thay đổi qua lại Loại sự kiện (Khen thưởng -> Kỷ luật -> Nhắc nhở) trên UI, kiểm tra xem các ComboBox hình thức xử lý tương ứng có ẩn/hiện và thay đổi danh mục (Commendation Level vs Discipline Level) chính xác không.

### Bước 4: Khắc phục rò rỉ Change Tracker
- [ ] [ ] Thử thực hiện import một file Excel câu hỏi bị lỗi để giao dịch bị rollback.
- [ ] [ ] Tạo thêm 1 câu hỏi đơn lẻ bằng tay trên giao diện và nhấn Lưu.
- [ ] [ ] Truy cập cơ sở dữ liệu và đảm bảo câu hỏi lỗi trước đó không hề bị lưu vào DB.

### Bước 5: Khắc phục lỗi dán điểm Excel trùng tên (Master Setup)
- [ ] [ ] **(Mới - Test Case 5.1)**: Đặt cấu hình `GradingMatchDuplicateOption = "SkipAndWarn"`. Thực hiện paste bảng điểm chứa 2 học sinh trùng tên "Nguyễn Văn An". Đảm bảo hệ thống bỏ qua 2 dòng này và cuối phiên hiển thị hộp thoại liệt kê rõ 2 học sinh bị bỏ qua để nhập tay.
- [ ] [ ] **(Mới - Test Case 5.2)**: Đặt cấu hình `GradingMatchDuplicateOption = "ShowSelectorDialog"`. Thực hiện paste bảng điểm có học sinh trùng tên "Nguyễn Văn An". Xác nhận cửa sổ `DuplicateStudentSelectorWindow` hiện lên hiển thị ảnh thẻ, mã HS của 2 học sinh đó. Chọn học sinh thứ 2 -> Xác nhận điểm số được điền đúng dòng học sinh thứ 2 trên Grid.

### Bước 6: Khắc phục lỗi hardcode ClassId = 1
- [ ] [ ] Chọn lớp 11A1 trên màn hình Kế hoạch phụ đạo.
- [ ] [ ] Xác nhận danh sách cảnh báo học sinh yếu hiển thị đúng học sinh của lớp 11A1 thay vì lớp 10A1.
- [ ] [ ] **(Mới - Test Case 6.1)**: Chọn lớp mà giáo viên không dạy trong danh sách (nếu có phân quyền). Đảm bảo hệ thống báo cảnh báo: "Bạn không có quyền quản lý lớp học này" và ẩn danh sách.

### Bước 7: Môn học phụ đạo động & Nhập tùy chọn "Khác"
- [ ] [ ] Mở màn hình tạo Kế hoạch phụ đạo.
- [ ] [ ] Xác nhận trường môn học là một ComboBox danh sách nạp từ CSDL, không cho gõ tay tự do ở trạng thái mặc định.
- [ ] [ ] **(Mới - Test Case 7.1)**: Chọn mục "Khác..." trong ComboBox môn học. Xác nhận TextBox nhập tự do hiển thị lên. Gõ "Môn Kỹ Năng Sống" -> Nhấn Lưu -> Kiểm tra CSDL xem kế hoạch đã được ghi nhận đúng môn "Môn Kỹ Năng Sống".

### Bước 8: Bảo mật SKKN theo TeacherCode
- [ ] [ ] Đăng nhập bằng tài khoản Giáo viên A, xem danh sách SKKN.
- [ ] [ ] Đăng nhập bằng tài khoản Giáo viên B trùng tên với giáo viên A.
- [ ] [ ] Xác nhận giáo viên B không xem được các sáng kiến kinh nghiệm do giáo viên A đã nộp.

### Bước 9: Mốc thời gian hết hạn Bảng tin
- [ ] [ ] Lên lịch ẩn một thông báo bảng tin vào ngày mai.
- [ ] [ ] Xác nhận trong CSDL bản ghi có trường `ExpiresAt` lưu thời gian là `23:59:59` của ngày mai.

### Bước 10: Chỉ dẫn sử dụng Step-by-Step
- [ ] [ ] Mở các màn hình chức năng của Phân hệ Giáo viên.
- [ ] [ ] Xác nhận có sự xuất hiện của các panel chỉ dẫn từng bước hướng dẫn nghiệp vụ rõ ràng.

### Bước 11: Lưu trữ UTF-8 with BOM
- [ ] [ ] Mở các tệp code C# trong Visual Studio hoặc trình soạn thảo.
- [ ] [ ] Xác nhận toàn bộ dấu tiếng Việt trong phần comment hiển thị rõ nét, không bị lỗi font hệ thống.

### Bước 12: Phông chữ công thức học thuật
- [ ] [ ] Viết nhận xét có chứa ký hiệu toán học đặc biệt ($\Delta, \neq$).
- [ ] [ ] Xác nhận hiển thị rõ ràng trên màn hình, không bị ô vuông lỗi.

### Bước 13: Cấu hình Phê duyệt Kế hoạch Phụ đạo (Master Setup)
- [ ] [ ] **(Mới - Test Case 13.1)**: Cấu hình `RemedialPlanRequiresApproval = false`. Tạo kế hoạch phụ đạo cho học sinh A. Đảm bảo trạng thái ghi nhận là "Approved" và giáo viên có thể thêm buổi học (Session) ngay lập tức.
- [ ] [ ] **(Mới - Test Case 13.2)**: Cấu hình `RemedialPlanRequiresApproval = true`. Tạo kế hoạch phụ đạo cho học sinh B. Xác nhận kế hoạch lưu ở trạng thái "Pending", nút "Ghi chép buổi học" bị vô hiệu hóa kèm tooltip: "Kế hoạch đang chờ Ban Giám hiệu phê duyệt".
