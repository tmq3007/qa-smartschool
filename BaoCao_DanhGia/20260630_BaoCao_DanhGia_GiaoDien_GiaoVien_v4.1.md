# Báo cáo Thẩm định & Đánh giá Giao diện Giáo viên (Teacher Hub)
**Phân hệ Giáo viên — Bộ tiêu chuẩn QA SmartClass v4.1**  
*Ngày lập báo cáo: 30 tháng 06 năm 2026*  

---

## ═══ THÀNH PHẦN HỘI ĐỒNG THẨM ĐỊNH (17 CHUYÊN GIA) ═══

Hội đồng Chuyên gia Dự án **QA Smart School** gồm 17 thành viên đại diện cho tất cả các bên liên quan đã tiến hành đánh giá chi tiết về mặt chức năng sư phạm, logic hệ thống và giao diện người dùng của **Giao diện Giáo viên (Teacher Hub / Teacher Portal)**:
1. **Trưởng bộ phận thiết kế dự án QA Smart School**
2. **Quản lý IT**
3. **Chuyên gia kiểm thử (QA/Testing Expert)**
4. **Chuyên gia thiết kế giao diện phần mềm (UI/UX)**
5. **Chuyên gia phân tích và thiết kế hệ thống**
6. **Chuyên gia về cơ sở dữ liệu và thiết bị kết nối ngoại vi**
7. **Chuyên gia về bảo mật và an ninh mạng**
8. **Nhà giáo dục**
9. **Nhà quản lý / Hiệu trưởng nhà trường**
10. **Trưởng bộ môn của trường**
11. **Giáo viên ưu tú với nhiều kinh nghiệm**
12. **Học sinh**
13. **Nhân viên nhà trường**
14. **Gamer giỏi (Chuyên gia Gamification)**
15. **Cán bộ quản lý của phòng giáo dục**
16. **Chuyên viên của sở giáo dục**
17. **Nhà khoa học giáo dục**

---

## ═══ PHẦN I: CÁC LỖI KỸ THUẬT, LOGIC HỆ THỐNG & VIỆT HÓA PHÁT HIỆN ═══

Qua quá trình rà soát mã nguồn và kiểm thử giao diện trực quan của phân hệ Giáo viên (`TeacherHub`), Hội đồng đã phát hiện các lỗi nghiêm trọng sau đây cần được sửa đổi ngay lập tức:

### 1. Lỗi đúc kiểu (Casting Bug) gây treo (Crash) khi trình chiếu trên SmartTouch
*   **Vị trí phát hiện:** Tệp [LessonPlanPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/LessonPlanPage.xaml.cs#L249)
*   **Mô tả:** Trong phương thức `BtnPresentToSmartTouch_Click`, mã nguồn viết: `var app = (QASmartTouch.App)Application.Current;`. Tuy nhiên, trong toàn bộ giải pháp phần mềm, lớp ứng dụng hiện hành được khởi chạy và quản lý dưới namespace `QASmartClass` chứ không phải `QASmartTouch`.
*   **Hậu quả:** Khi giáo viên click vào nút **"▶ Trình chiếu"** trên giao diện soạn giáo án để đồng bộ nội dung lên bảng tương tác, ứng dụng sẽ ngay lập tức ném ra ngoại lệ `InvalidCastException` tại thời điểm chạy (runtime) và bị sập hoàn toàn (Crash), làm gián đoạn bài giảng và gây hoang mang trong lớp học.

### 2. Lỗi mồ côi bản ghi kỷ luật (Orphan Discipline Records) do thiếu liên kết StudentId
*   **Vị trí phát hiện:** Tệp [HomeroomDiaryPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/HomeroomDiaryPage.xaml.cs#L205)
*   **Mô tả:** Khi giáo viên thêm mới một sự kiện Khen thưởng / Kỷ luật thông qua giao diện Pop-up của Nhật ký chủ nhiệm, hệ thống cho phép giáo viên nhập tên học sinh dưới dạng văn bản tự do (`studentName = txtName.Text.Trim()`) và lưu trực tiếp bản ghi vào cơ sở dữ liệu mà **không thực hiện tra cứu để gán giá trị `StudentId`**.
*   **Hậu quả:** 
    *   Trường `StudentId` trong bảng `DisciplineRecords` bị để mặc định là `0`. Do đó, các bản ghi này hoàn toàn bị mồ côi (orphaned), không liên kết với hồ sơ học sinh.
    *   Phân hệ **Cổng Phụ huynh (Parent Portal)** và **Cổng Học sinh** vốn truy vấn dữ liệu kỷ luật bằng mã `StudentId` (thông qua `DisciplineService.GetByStudent(studentId)`) sẽ không thể tìm thấy bất kỳ bản ghi nào, khiến phụ huynh không nhận được cảnh báo hoặc khen thưởng của con em mình.
    *   Đồng thời, các trường quan trọng khác như `ViolationType` (Mức độ vi phạm) và `DisciplineLevel` (Hình thức kỷ luật) bị bỏ trống hoàn toàn, làm hỏng các báo cáo thống kê chuyên môn của nhà trường.

### 3. Lỗi bất đồng bộ định dạng dữ liệu điểm danh gây sai lệch cảnh báo học tập sớm (Early Warning)
*   **Vị trí phát hiện:** Tệp [EarlyWarningService.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Services/EarlyWarningService.cs#L35)
*   **Mô tả:** Trong cơ sở dữ liệu `smartclass.db`, trạng thái điểm danh của học sinh được lưu trữ không nhất quán (lúc là chữ thường `"absent"`, `"present"`, lúc viết hoa đầu `"Absent"`, `"Present"`, lúc lại thuần Việt `"Vắng"`, `"Có mặt"`). Tuy nhiên, tại dịch vụ phân tích học sinh có nguy cơ học tập kém (`EarlyWarningService`), kịch bản chỉ lọc điều kiện vắng bằng chuỗi viết thường hoặc tiếng Việt:
    ```csharp
    (a.Status == "absent" || a.Status == "Vắng" || a.Status == "vắng")
    ```
*   **Hậu quả:** Bất kỳ buổi nghỉ học nào được hệ thống tự động ghi nhận dưới dạng chuỗi viết hoa `"Absent"` sẽ bị bỏ qua hoàn toàn khỏi bộ đếm ngày vắng của học sinh. Hệ thống sẽ cảnh báo sai lệch (Ví dụ: Học sinh thực tế vắng 6 buổi trong tháng, đạt ngưỡng cảnh báo nguy hiểm, nhưng trên giao diện giáo viên chỉ hiển thị vắng 2 buổi), làm vô hiệu hóa công cụ can thiệp sư phạm sớm.

### 4. Lỗi thiếu ReferenceId trong liên kết ký duyệt Sổ liên lạc điện tử của phụ huynh
*   **Vị trí phát hiện:** Tệp [ParentApprovalView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/ParentApprovalView.xaml.cs#L109)
*   **Mô tả:** Khi giáo viên gửi một yêu cầu chữ ký số từ phụ huynh để xác nhận Sổ liên lạc cuối kỳ, đối tượng `ParentApproval` được tạo mới và lưu vào cơ sở dữ liệu chỉ chứa `StudentId` và `DocumentType = "Sổ Liên Lạc"`, trong khi trường chỉ định khóa tham chiếu dữ liệu gốc `ReferenceId` hoàn toàn bị bỏ sót (bằng `0`).
*   **Hậu quả:** Phụ huynh khi đăng nhập vào ứng dụng di động sẽ nhận được yêu cầu ký duyệt sổ liên lạc, nhưng hệ thống không thể biết yêu cầu này tương ứng với bảng điểm học kỳ nào, năm học nào để hiển thị nội dung học bạ chi tiết trước khi ký. Giao diện ký duyệt sẽ trống trơn hoặc liên kết sai dữ liệu học bạ.

### 5. Lỗi thuật toán tỷ trọng làm hỏng biểu đồ Radar Đa trí tuệ (Class MI Radar Chart)
*   **Vị trí phát hiện:** Tệp [ClassMIDashboardView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/ClassMIDashboardView.xaml.cs#L183)
*   **Mô tả:** Radar vẽ biểu đồ 8 khía cạnh thông minh dựa trên công thức tính tỷ lệ phần trăm đóng góp:
    ```csharp
    double percent = totalRaw == 0 ? 0.125 : (scores[i] / totalRaw);
    double r = percent * maxRadius * 3;
    ```
*   **Hậu quả:** Đây là một lỗi toán học nghiêm trọng trong biểu diễn đồ thị trực quan:
    *   Nếu một lớp học có học lực cực kỳ xuất sắc và đồng đều (đạt điểm tối đa 100 XP ở cả 8 loại trí thông minh), tổng điểm `totalRaw` sẽ là 800 XP. Khi đó, tỷ trọng của mỗi cột chỉ là `100 / 800 = 0.125`. Bán kính vẽ tương ứng là `r = 0.125 * 90 * 3 = 33.75` (chỉ chiếm 37% kích thước vòng tròn tối đa).
    *   Hậu quả là biểu đồ radar của một lớp học sinh xuất sắc toàn diện trông vẫn bị bóp nghẹt, méo mó và nhỏ bé ở trung tâm giống hệt như một lớp yếu kém, gây hiểu lầm tai hại cho giáo viên chủ nhiệm và ban giám hiệu về năng lực thực tế của học sinh. Biểu đồ radar chuẩn phải dựa trên thang đo tuyệt đối của từng kỹ năng (ví dụ: `scores[i] / 100 * maxRadius`).

### 6. Lỗi lệch ngày và mất khả năng kiểm soát thời gian trong Lên lịch Bảng tin
*   **Vị trí phát hiện:** Tệp [BulletinBoardPage.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/BulletinBoardPage.xaml.cs#L215-L218)
*   **Mô tả:** Chức năng lên lịch đăng tin (`scheduledAt`) và ngày hết hạn bản tin (`expiresAt`) sử dụng trực tiếp đối tượng `SelectedDate` từ điều khiển `<DatePicker>`. Điều khiển này trong WPF mặc định chỉ cho chọn ngày và tự động đặt giờ/phút/giây về `00:00:00` (nửa đêm).
*   **Hậu quả:** 
    *   Giáo viên không có cách nào đặt giờ xuất bản bản tin cụ thể (Ví dụ: Muốn tin tức tự động đăng lúc 08:00 sáng khi bắt đầu giờ sinh hoạt lớp).
    *   Đặc biệt, đối với ngày hết hạn, nếu giáo viên cấu hình bản tin hết hạn vào ngày 01/07/2026, bản tin sẽ bị ẩn ngay lập tức vào lúc `00:00:00` của ngày 01/07 (tức là vừa bước sang ngày mới bản tin đã biến mất, thay vì hiển thị hết ngày 01/07 đến `23:59:59`), gây mất thông tin truyền thông trong trường học.

### 7. Lỗi hỏng mã hóa ký tự (Font Encoding Error) trong chú thích tệp code C#
*   **Vị trí phát hiện:** Tệp [TeacherDashboardView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/TeacherDashboardView.xaml.cs#L40) và nhiều vị trí khác.
*   **Mô tả:** Tệp code C# chứa các bình luận tiếng Việt có dấu bị lỗi hiển thị font hệ thống thành các ký tự lạ hoặc dấu hỏi chấm (ví dụ: `// --- 1. i?m TB l?p`, `// --- 2. Phn b? di?m`, `// --- 3. Chuyn c?n hm nay`).
*   **Hậu quả:** Phản ánh quy trình quản lý mã nguồn chưa chuẩn hóa mã hóa UTF-8 với BOM. Tuy chỉ nằm trong phần bình luận (comment) không gây lỗi biên dịch, việc này gây khó khăn lớn cho đội ngũ kỹ thuật IT khi bảo trì, đọc hiểu mã nguồn và vi phạm tiêu chuẩn tài liệu kỹ thuật sạch sẽ v4.1.

### 8. Lỗi sử dụng ngôn từ thiếu tính sư phạm (Anti-pedagogical Labels) trên giao diện đa trí tuệ
*   **Vị trí phát hiện:** Tệp [ClassMIDashboardView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/ClassMIDashboardView.xaml#L133) và tệp code-behind tương ứng [ClassMIDashboardView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/TeacherHub/Views/ClassMIDashboardView.xaml.cs#L111).
*   **Mô tả:** Nhãn tiêu đề vùng cảnh báo hiển thị chuỗi ký tự: `"🚨 Vùng Rủi Ro (Học Sinh Lười Tương Tác)"` và các dòng trạng thái cảnh báo như `"Lười tương tác (Báo động Đỏ)"`, `"Chưa từng chơi game (Báo động Đen)"`.
*   **Hậu quả:** Vi phạm nghiêm trọng bộ quy chuẩn thiết kế sư phạm QA SmartClass v4.1. Phần mềm giáo dục tuyệt đối không được gán nhãn tiêu cực mang tính quy chụp như "Lười tương tác" hoặc dùng từ ngữ mang cảm giác đe dọa như "Báo động Đen" lên học sinh. Thiết kế sư phạm yêu cầu sử dụng ngôn từ mang tính xây dựng, khuyến khích sự tiến bộ của người học.

---

## ═══ PHẦN II: Ý KIẾN & ĐÁNH GIÁ CHI TIẾT TỪ HỘI ĐỒNG CHUYÊN GIA ═══

### 1. 💼 Trưởng bộ phận thiết kế dự án QA Smart School (Project Design Lead)
> "Sự cố sập ứng dụng (InvalidCastException) khi nhấn nút Trình chiếu là một lỗi tích hợp hệ thống nghiêm trọng. Namespace `QASmartTouch` thuộc về một dự án bảng tương tác khác trong hệ sinh thái và không được phép ép kiểu bừa bãi trong ứng dụng chính `QASmartClass`. Điều này thể hiện sự thiếu sót lớn trong khâu duyệt kiến trúc tích hợp."

### 2. 💻 Quản lý IT (IT Manager)
> "Hệ thống cơ sở dữ liệu lưu trữ cột `Status` của bảng `AttendanceRecords` quá hỗn loạn. Việc để tồn tại song song cả tiếng Anh lẫn tiếng Việt, cả chữ hoa chữ thường như 'present', 'Present', 'Có mặt' là kịch bản tồi tệ nhất cho việc đồng bộ hóa dữ liệu. Tôi đề xuất chuẩn hóa trường này sang dạng mã số Enum hoặc TinyInt (ví dụ: 1 = Có mặt, 2 = Vắng, 3 = Đi muộn)."

### 3. 🔍 Chuyên gia kiểm thử phần mềm (QA Tester)
> "Tất cả 8 lỗi phát hiện ở Phần I đều là những lỗi kiểm thử nghiêm trọng. Việc tạo bản ghi kỷ luật mà không gán `StudentId` khiến dữ liệu bị mồ côi là lỗi luồng nghiệp vụ cơ bản (Integration Flow Blocked). Đội ngũ QA cần bổ sung ngay các test case liên kết dữ liệu chéo giữa các phân hệ trước khi phát hành phiên bản mới."

### 4. 🎨 Chuyên gia thiết kế giao diện (UI/UX Designer)
> "Layout của trang Soạn giáo án đã áp dụng tốt cơ cấu Grid 2 cột giúp tận dụng tốt chiều ngang màn hình. Tuy nhiên, việc thiếu thanh cuộn ở các bảng thống kê và biểu đồ cột cố định pixel không co giãn tốt khi giáo viên phóng to ứng dụng trên máy chiếu. Nhãn cảnh báo 'Học sinh lười tương tác' có màu đỏ quá chói (#E11D48) kết hợp icon còi báo động tạo ra áp lực thị giác không cần thiết."

### 5. ⚙️ Chuyên gia phân tích và thiết kế hệ thống (System Analyst)
> "Dữ liệu liên kết giữa Giáo án (`LessonPlan`) và Bài giảng (`Lesson`) ở trang soạn giáo án hiện tại rất lỏng lẻo. Khi tạo bài giảng từ giáo án, hệ thống lưu trường `TeacherName` bằng giá trị `_currentTeacherId.ToString()` (Ví dụ: 'GV001'). Trong khi đó, bảng `Lessons` ở kịch bản Classroom lại mong muốn lưu tên đầy đủ (FullName) của giáo viên. Sự lệch pha định danh này sẽ làm sai lệch chức năng lọc bài giảng của giáo viên."

### 6. 🗄️ Chuyên gia về cơ sở dữ liệu và thiết bị kết nối ngoại vi (DB & Connectivity Specialist)
> "Việc thiết lập mặc định `Status = "Draft"` cho `DisciplineRecord` trong database khiến các hồ sơ kỷ luật do giáo viên tạo ra ở HomeroomDiary bị giam giữ vô thời hạn dưới dạng nháp. Giáo viên không thể đẩy hồ sơ này lên trạng thái 'Pending' để Hiệu trưởng xét duyệt thông qua `DisciplineService`. Cần bổ sung trường trạng thái tùy chọn khi tạo."

### 7. 🛡️ Chuyên gia về bảo mật và an ninh mạng (Security Expert)
> "Khi tạo yêu cầu ký duyệt sổ liên lạc gửi phụ huynh, việc không gán `ReferenceId` không chỉ gây lỗi hiển thị mà còn là một lỗ hổng logic bảo mật. Kẻ xấu có thể giả mạo yêu cầu ký duyệt bằng cách gửi một ID trống để ép buộc phụ huynh xác thực một văn bản rác ngoài hệ thống. Mọi yêu cầu chữ ký số phải được ký chéo (Cross-linked) với ID của học bạ cụ thể."

### 8. 🏫 Nhà giáo dục (Educator)
> "Mô hình radar đa trí tuệ là một công cụ sư phạm tuyệt vời để đánh giá sự phát triển toàn diện của trẻ em. Nhưng thuật toán vẽ radar hiện tại đang làm sai lệch bản chất của thuyết đa trí tuệ của Howard Gardner. Trí thông minh là năng lực tuyệt đối của cá nhân trên từng khía cạnh, không phải là chiếc bánh chia phần trăm để so sánh tỷ trọng. Việc tính toán theo tỷ trọng vô tình phạt những học sinh giỏi đều tất cả các môn."

### 9. 🎓 Nhà quản lý / Hiệu trưởng nhà trường (School Principal)
> "Tôi cần một quy trình phê duyệt kỷ luật rõ ràng. Hiện tại, tính năng Khen thưởng / Kỷ luật ở Sổ chủ nhiệm hoạt động độc lập và tự lưu thẳng vào database mà không đi qua các bộ lọc kiểm soát hành chính của Ban giám hiệu là sai quy định quản lý học đường."

### 10. 👥 Trưởng bộ môn của trường (Head of Department)
> "Tại phân hệ Kiểm định đề thi (`DeptHeadReviewView`), việc ẩn cột Hành động của giáo viên thường bằng cách gán `ColActions.Visibility = Visibility.Collapsed` ở code-behind là giải pháp nhanh nhưng dễ lỗi. Giao diện nên có sự phân quyền từ tầng dữ liệu (Data Binding) dựa trên vai trò của phiên đăng nhập để đảm bảo an toàn."

### 11. 👩‍🏫 Giáo viên ưu tú với nhiều kinh nghiệm (Elite Teacher)
> "Giáo viên chúng tôi rất bận rộn khi đứng lớp. Tính năng lên lịch bảng tin rất hữu ích nhưng việc bắt buộc phải nhập ngày hết hạn bản tin hoặc chỉ cho chọn ngày mà không cho chọn giờ đăng làm chúng tôi gặp nhiều bất tiện. Chúng tôi thường soạn thông báo vào tối hôm trước và muốn tin tự động hiện lúc 07h00 sáng hôm sau."

### 12. 👦 Học sinh (Student)
> "Chúng em cảm thấy rất buồn khi bị gắn mác là 'Lười tương tác' trên bảng phân tích của thầy cô. Nhiều bạn do điều kiện gia đình không có máy tính riêng để chơi game tích điểm XP ở nhà, chứ không phải các bạn lười biếng. Xin hãy đổi tên cảnh báo thành một cái tên tích cực hơn."

### 13. 🧹 Nhân viên nhà trường - Thiết bị & Hậu cần (Staff Support)
> "Trong chức năng xuất giáo án ra file PDF (`BtnExportPdf_Click`), file được lưu mặc định vào thư mục `MyDocuments` với tên file tự sinh. Giáo viên lớn tuổi thường không biết tìm file đã xuất ở đâu. Hệ thống nên mở trực tiếp thư mục chứa file sau khi xuất thành công để thầy cô dễ nhìn thấy."

### 14. 🎮 Gamer giỏi (Pro Gamer / Gamification Specialist)
> "Điểm XP đa trí tuệ trong hệ thống được tích lũy từ các hoạt động học tập. Việc tính toán tỷ trọng trên Radar khiến cho việc thăng cấp (Level Up) của học sinh bị hiển thị sai lệch. Một gamer thực thụ cần nhìn thấy thanh tiến trình (Progress Bar) cụ thể của từng loại năng lực thay vì một hình đa giác radar rúm ró ở giữa màn hình."

### 15. 🏢 Cán bộ quản lý của phòng giáo dục (District Admin)
> "Việc nhập điểm từ Excel/CSV cần có cơ chế kiểm tra định dạng cực kỳ nghiêm ngặt. Lỗi paste điểm từ Excel vào bảng điểm (`BtnPasteExcel_Click`) chỉ so khớp tên không dấu (`cleanDbName == cleanPIdentifier`) rất dễ dẫn đến tình trạng râu ông nọ cắm cằm bà kia đối với các học sinh trùng tên trong cùng một lớp (Ví dụ: Nguyễn An và Nguyễn Anh)."

### 16. 🏫 Chuyên viên của sở giáo dục (Provincial Specialist)
> "Sổ chủ nhiệm điện tử phải phản ánh chính xác các tiêu chuẩn của Bộ Giáo dục. Việc ghi nhận số học sinh vắng mặt dựa trên đếm chuỗi cứng 'Absent' bị sót dữ liệu sẽ dẫn đến việc tổng hợp báo cáo chuyên cần gửi lên Sở bị sai lệch tỷ lệ phần trăm chuyên cần của toàn trường."

### 17. 🔬 Nhà khoa học giáo dục (Educational Scientist)
> "Ngôn từ trong lớp học có sức mạnh định hình tâm lý học đường. Nhãn 'Báo động Đen' gợi liên tưởng đến những hình phạt nặng nề, tạo tâm lý sợ hãi, phản tác dụng giáo dục. Sư phạm hiện đại hướng tới kỷ luật tích cực (Positive Discipline), do đó các cảnh báo cần được viết dưới dạng đề xuất hỗ trợ (Ví dụ: 'Cần hỗ trợ tương tác xã hội' hoặc 'Cần khích lệ tham gia nhóm')."

---

## ═══ PHẦN III: BẢNG ĐỐI CHIẾU TRƯỚC VS SAU CẢI TIẾN ═══

| Phân hệ / Tiêu chí | Trạng thái Hiện tại (Lỗi v4.1) | Giải pháp Đề xuất (Sau cải tiến) | Lợi ích Sư phạm & Kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Trình chiếu Giáo án** | Ép kiểu nhầm sang `QASmartTouch.App` gây sập phần mềm khi click trình chiếu. | Thay đổi thành `(QASmartClass.App)Application.Current` và gán thuộc tính an toàn. | Khắc phục hoàn toàn lỗi sập ứng dụng, đảm bảo bài dạy diễn ra trôi chảy. |
| **Ghi nhận Kỷ luật** | Không gán `StudentId` (bằng 0), làm mồ côi bản ghi, Cổng phụ huynh không thể hiển thị. | Thực hiện truy vấn kiểm tra tên học sinh trong lớp để tự động ánh xạ và gán `StudentId` chuẩn. | Phụ huynh lập tức nhận được thông báo nề nếp của con trên điện thoại. |
| **Thống kê Vắng học** | Chỉ đếm trạng thái viết thường `"absent"` hoặc tiếng Việt, bỏ sót chữ viết hoa `"Absent"`. | Chuyển đổi trạng thái về chữ thường trước khi so sánh: `a.Status.ToLower() == "absent"`. | Đảm bảo cảnh báo học sinh vắng học chính xác 100%, không bị sót. |
| **Ký duyệt Sổ liên lạc** | Bỏ trống `ReferenceId` (bằng 0), phụ huynh không có nội dung học bạ cụ thể để ký duyệt. | Gán `ReferenceId = currentContactBook.Id` khi tạo yêu cầu phê duyệt sổ liên lạc. | Phụ huynh xem được toàn bộ điểm số, nhận xét chi tiết trước khi đặt bút ký điện tử. |
| **Radar Đa Trí Tuệ** | Tính toán kích thước đồ thị theo tỷ trọng (tỷ lệ phần trăm chia phần), làm co rúm biểu đồ lớp giỏi. | Thay đổi công thức vẽ radar dựa trên điểm tuyệt đối: `scores[i] / Max_XP * maxRadius`. | Phản ánh chính xác điểm mạnh thực tế của tập thể lớp trên từng khía cạnh năng lực. |
| **Lên lịch Bản tin** | Thiết lập ngày xuất bản/hết hạn qua `DatePicker` bị cố định giờ lúc `00:00:00` nửa đêm. | Bổ sung thêm điều khiển chọn Giờ/Phút (`TimePicker`) kế bên điều khiển chọn ngày. | Giáo viên chủ động giờ đăng tin và tin hết hạn vào đúng cuối ngày (`23:59:59`). |
| **Ngôn ngữ Sư phạm** | Sử dụng từ tiêu cực `"Lười tương tác"`, `"Vùng rủi ro"`, `"Báo động Đen"`. | Thay bằng ngôn từ sư phạm tích cực: `"Cần hỗ trợ tương tác"`, `"Chưa có dữ liệu hoạt động"`. | Phù hợp chuẩn tâm lý học đường, xây dựng môi trường giáo dục thân thiện. |
| **Mã hóa Tệp tin** | Các chú thích tiếng Việt trong file code C# bị lỗi font hiển thị (hỏng encoding). | Lưu lại tất cả tệp code C# dưới định dạng mã hóa chuẩn **UTF-8 with BOM**. | Đảm bảo mã nguồn hiển thị đúng trên mọi máy tính phát triển, thuận tiện bảo trì. |

---

## ═══ PHẦN IV: CHECKSHEET KIỂM THỬ KHẮC PHỤC (VERIFICATION CHECKSHEET) ═══

*   [ ] **Kiểm tra Trình chiếu giáo án:** Nhấp vào nút "Trình chiếu" trong màn hình soạn giáo án, xác nhận hệ thống chuyển đổi màn hình sang chế độ bảng tương tác SmartTouch mượt mà, không xảy ra crash.
*   [ ] **Kiểm tra Ánh xạ ID Kỷ luật:** Tạo mới một bản ghi kỷ luật tại trang Homeroom Diary, mở cơ sở dữ liệu xác nhận trường `StudentId` đã được điền đúng mã ID học sinh thực tế thay vì số 0.
*   [ ] **Kiểm tra Đồng bộ trạng thái vắng:** Nhập thử một bản ghi điểm danh vắng với trạng thái viết hoa `"Absent"`, kiểm tra màn hình Early Warning xem số buổi vắng của học sinh có tăng lên chính xác hay không.
*   [ ] **Kiểm tra Tham chiếu ký duyệt:** Gửi yêu cầu ký duyệt sổ liên lạc, xác nhận bản ghi `ParentApprovals` trong database đã lưu đúng `ReferenceId` trỏ tới bảng điểm tương ứng.
*   [ ] **Kiểm tra Hiển thị Radar:** Nhập điểm 100 XP cho tất cả khía cạnh đa trí tuệ của học sinh lớp 10A1, kiểm tra biểu đồ radar xem có nở rộng tối đa đạt sát viền canvas hay không.
*   [ ] **Kiểm tra Thời gian bản tin:** Đặt lịch ẩn một thông báo vào ngày hôm sau, kiểm tra thông báo đó vẫn xuất hiện vào ban ngày và chỉ ẩn đi khi hết ngày.
*   [ ] **Kiểm tra Việt hóa & Ngôn từ:** Rà soát toàn bộ giao diện Class MI Dashboard và các thông báo lỗi để đảm bảo không còn xuất hiện các cụm từ tiêu cực "lười tương tác", "báo động đen".
*   [ ] **Kiểm tra Đọc file PDF:** Thử xuất giáo án ra file PDF, xác nhận sau khi xuất thành công, hệ thống tự động mở thư mục chứa tệp tin hoặc mở trực tiếp tệp PDF lên để kiểm tra.

---

## ═══ KẾT LUẬN CỦA HỘI ĐỒNG ═══

Hội đồng Chuyên gia đánh giá giao diện Giáo viên (`TeacherHub`) của dự án QA SmartSchool **CHƯA ĐẠT tiêu chuẩn thiết kế sư phạm và kịch bản kỹ thuật của phiên bản QA SmartClass v4.1**. Các lỗi logic về biểu đồ đa trí tuệ, mồ côi bản ghi kỷ luật và đặc biệt là lỗi casting gây sập ứng dụng khi trình chiếu cần phải được ưu tiên sửa chữa ngay lập tức trong chu kỳ phát triển tiếp theo.

**Khuyến nghị:** Nhóm phát triển phần mềm cần tiến hành tái cấu trúc lại luồng ghi nhận khen thưởng/kỷ luật của học sinh để đi qua `DisciplineService` nhằm đảm bảo tính toàn vẹn dữ liệu, đồng thời chuyển đổi thuật toán vẽ radar sang dạng điểm số tuyệt đối.

*Hội đồng Chuyên gia thống nhất ký duyệt báo cáo thẩm định.*
