# BÁO CÁO THẨM ĐỊNH & ĐÁNH GIÁ CHI TIẾT PHÂN HỆ QUẢN LÝ PHỤ TRÁCH ĐOÀN ĐỘI
**Áp dụng Bộ quy chuẩn Thiết kế Sư phạm và Ràng buộc Kỹ thuật QA SmartClass v4.1**  
*Ngày báo cáo: 30 tháng 06 năm 2026*  
*Mã tài liệu: BC-TD-YUM-4.1*  

---

## ═══ THÀNH PHẦN HỘI ĐỒNG THẨM ĐỊNH (17 CHUYÊN GIA) ═══

Hội đồng Chuyên gia Dự án **QA Smart School** gồm 17 thành viên đại diện cho các góc nhìn từ Kỹ thuật hệ thống, An ninh mạng, Thiết kế giao diện (UI/UX), Giáo dục học, Nhà quản lý và Người dùng cuối (Giáo viên, Học sinh) đã tiến hành rà soát, đánh giá thực tế phân hệ **Quản lý Phụ trách Đoàn Đội (Youth Union & Pioneers Management Module)**.

---

## ═══ PHẦN I: TỔNG HỢP CÁC LỖI LOGIC HỆ THỐNG & ĐIỂM YẾU GIAO DIỆN CẦN KHẮC PHỤC ═══

Qua quá trình chạy thử nghiệm, kiểm thử mã nguồn và rà soát giao diện XAML/C#, Hội đồng đã phát hiện các lỗi logic nghiêm trọng và các điểm chưa tối ưu sau đây:

### 1. Lỗi logic bất đồng bộ mã hóa chuỗi (Accent Mismatch Bug) - Nghiêm trọng
*   **Mô tả lỗi:** 
    *   Tệp cấu hình hệ thống [YouthConstants.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/YouthConstants.cs) định nghĩa các loại thành viên và chức vụ bằng chuỗi **không dấu**:
        *   `YouthMemberTypes.DoanVien = "Doan vien"`, `YouthMemberTypes.DoiVien = "Doi vien"`
        *   `YouthPositions.ThanhVien = "Thanh vien"`, `YouthPositions.BiThu = "Bi thu"`, v.v.
    *   Tuy nhiên, biểu mẫu nhập liệu trong [MemberListView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/MemberListView.xaml.cs) lại lưu trực tiếp chuỗi **có dấu** từ UI vào Database:
        *   `cbType.Items.Add("Đoàn viên"); cbType.Items.Add("Đội viên");`
        *   `cbPos.Items.Add("Thành viên"); cbPos.Items.Add("Bí thư"); cbPos.Items.Add("Phó Bí thư");`
    *   **Hậu quả:** 
        1.  **Lỗi bộ lọc:** Trong `MemberListView.xaml.cs`, bộ lọc theo loại sử dụng Tag không dấu `"Doan vien"`, so sánh `m.MemberType == typeTag` sẽ luôn trả về **0 kết quả** đối với các bản ghi được tạo mới từ giao diện (do lưu là `"Đoàn viên"`).
        2.  **Lỗi theo dõi Đoàn phí:** Trong `FeeTrackerView.xaml.cs`, hệ thống tải danh sách cần đóng phí bằng lệnh `allMembers.Where(m => m.Status == "Active" && m.MemberType == "Doan vien")`. Tất cả các đoàn viên thêm từ giao diện (lưu là `"Đoàn viên"`) sẽ bị **bỏ sót hoàn toàn khỏi danh sách đóng phí**.
        3.  **Lỗi đồng bộ kết nạp mới:** Lệnh `SyncApprovedRecruitmentsAsync` trong `YouthUnionService.cs` đồng bộ học sinh kết nạp mới bằng chuỗi có dấu `"Đoàn viên"`, trong khi `RecruitmentView.xaml.cs` lại lưu mặc định là `"Doan vien"`. Sự bất nhất này làm sai lệch dữ liệu thống kê trên Dashboard.

### 2. Lỗi khóa luồng giao diện khi quét thẻ RFID (UI Dispatcher Thread Blockage) - Nghiêm trọng
*   **Mô tả lỗi:** Trong [AttendanceView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/YouthUnion/Views/AttendanceView.xaml.cs) (phần xử lý sự kiện quét thẻ `ProcessCardScanAsync`), khi phát hiện thẻ không hợp lệ hoặc học sinh không có tên trong danh sách buổi sinh hoạt, chương trình gọi trực tiếp lệnh `MessageBox.Show()`.
*   **Hậu quả:** Lệnh hiển thị hộp thoại cảnh báo này chạy trên luồng chính (UI Dispatcher) và ở chế độ Modal (khóa màn hình). Trong thực tế sinh hoạt tập thể tại trường, hàng trăm học sinh sẽ quét thẻ liên tục. Một thẻ lỗi sẽ làm **treo toàn bộ luồng đọc thẻ của cổng COM**, gây ùn tắc giao thông tại cổng và có khả năng làm tràn bộ đệm Serial Port dẫn đến crash ứng dụng.

### 3. Lỗi lạm dụng tiếng Anh và tiếng Việt không dấu trên giao diện (Localization & Usability Violations)
*   **Mô tả lỗi:** Bộ tiêu chuẩn QA SmartClass v4.1 yêu cầu Việt hóa 100% giao diện và hiển thị tiếng Việt chuẩn mực sư phạm. Tuy nhiên:
    *   **Trên Dashboard:**
        *   Trạng thái hiển thị hoạt động gần đây sử dụng trực tiếp chuỗi DB tiếng Anh: `"Completed"`, `"Approved"`, `"Draft"`.
        *   Nhãn hiển thị các thẻ thống kê sử dụng tiếng Việt không dấu cẩu thả: `"Dang sinh hoat: {active}"`, `"Da hoan thanh: {completedActs}"`, `"Da dong: {feeStats.Paid}/{total}"`.
    *   **Trên trang quản lý Đoàn phí (FeeTrackerView):**
        *   Cột trạng thái hiển thị: `"Da dong"` và `"Chua dong"` không dấu.
        *   Hộp chọn bộ lọc trạng thái chứa các Tag tiếng Anh: `"Paid"`, `"Unpaid"`.
    *   **Trên các biểu mẫu nghiệp vụ khác:** Các combobox chứa các tùy chọn trạng thái tiếng Anh thô như `"Draft"`, `"Approved"`, `"Completed"`, `"Pending"`, `"Rejected"`, `"Income"`, `"Expense"` hiển thị trực tiếp cho giáo viên/học sinh.

### 4. Thiếu hụt trầm trọng chỉ dẫn sử dụng từng bước (UX Guide Shortage)
*   **Mô tả lỗi:** Ngoại trừ trang Điểm danh (`AttendanceView.xaml`) có một khung hướng dẫn RFID tạm ổn, tất cả 10 trang chức năng còn lại đều **không có khung hướng dẫn sử dụng từng bước (Step-by-step guides)**.
*   **Hậu quả:** Giáo viên phụ trách Đoàn Đội mới nhận nhiệm vụ, hoặc các học sinh trong Ban chấp hành Chi đoàn sẽ gặp khó khăn khi thao tác các tính năng phức tạp như: "Nhập dữ liệu Excel", "Quy trình kết nạp Đoàn viên mới", "Thiết lập biểu quyết ẩn danh", hay "Cân đối dự toán ngân sách". Điều này vi phạm nghiêm trọng mục tiêu giảm thiểu sai sót vận hành của v4.1.

---

## ═══ PHẦN II: ĐÁNH GIÁ CHI TIẾT TỪ HỘI ĐỒNG 17 CHUYÊN GIA ═══

### 1. Trưởng bộ phận thiết kế dự án QA Smart School
> *"Về mặt kiến trúc giao diện, việc bố trí thanh menu điều hướng dạng Expander và cửa sổ hiển thị dữ liệu (FeatureFrame) ở bên phải là hợp lý. Tuy nhiên, việc thiếu các chỉ báo trạng thái trực quan (như sơ đồ quy trình, các thanh tiến độ trực quan cho Đoàn phí) làm giao diện trông giống như một phần mềm kế toán cũ kỹ, thiếu tinh thần năng động của Đoàn - Đội."*

### 2. Quản lý IT
> *"Dịch vụ `YouthUnionService` cần tối ưu hóa các câu truy vấn. Việc giải phóng kết nối SQLite thông qua cơ chế Dispose của View cần được giám sát chặt chẽ. Hệ thống cần tích hợp bộ đệm (Queue) cho sự kiện đọc thẻ RFID thay vì xử lý tuần tự trực tiếp trên UI Dispatcher để tránh nghẽn cổ chai."*

### 3. Chuyên gia kiểm thử (QA/Testing)
> *"Lỗi bất đồng bộ chuỗi có dấu và không dấu giữa UI và DB là một lỗi kiểm thử cơ bản nhưng để lại hậu quả nghiêm trọng. Nó làm tê liệt chức năng lọc danh sách thành viên và bỏ sót học sinh trong tính năng đóng đoàn phí. Ngoài ra, việc nhập Excel thiếu bước kiểm tra định dạng dữ liệu (Data Validation) trước khi ghi xuống DB có thể gây hỏng cấu trúc dữ liệu."*

### 4. Chuyên gia thiết kế giao diện phần mềm (UI/UX)
> *"Font chữ Segoe UI được áp dụng tốt, khoảng cách lề (Padding/Margin) nhìn chung thoáng đạt. Tuy nhiên, việc để hiển thị tiếng Việt không dấu (như 'Da dong', 'Dang sinh hoat') là không chấp nhận được ở một sản phẩm giáo dục cao cấp. Màu sắc các trạng thái cần có độ tương phản cao hơn để hỗ trợ người dùng có thị lực kém."*

### 5. Chuyên gia phân tích và thiết kế hệ thống
> *"Mô hình dữ liệu `YouthMember` và `YouthActivity` được liên kết chặt chẽ. Tuy nhiên, luồng nghiệp vụ giữa 'Yêu cầu kết nạp' -> 'Phê duyệt' -> 'Đồng bộ danh sách' đang có sự chồng chéo trong việc định nghĩa các hằng số. Cần chuẩn hóa toàn bộ các trường trạng thái sang bảng tra cứu (Lookup table) hoặc Enum có định nghĩa rõ ràng."*

### 6. Chuyên gia về cơ sở dữ liệu và thiết bị kết nối ngoại vi
> *"Hệ thống tương thích tốt với đầu đọc RFID 13.56MHz qua cổng COM ảo. Tuy nhiên, việc xử lý chuỗi RFID thô nhận từ Serial Port cần có cơ chế loại bỏ ký tự nhiễu (Trim, Regex match) kỹ càng hơn. Cổng COM cần được tự động dò tìm (Auto-detect) thông minh thay vì chỉ lấy phần tử đầu tiên trong danh sách một cách may rủi."*

### 7. Chuyên gia về bảo mật và an ninh mạng
> *"Tính năng bỏ phiếu ẩn danh (`CastVoteAsync`) được thiết kế tốt: Tách biệt `VoterId` bằng cách gán về 0 trong bảng `YouthVotes` và ghi nhận trạng thái đã bỏ phiếu ở bảng `YouthVoterRegistry`. Tuy nhiên, tệp xuất báo cáo PDF/Excel lưu trong thư mục Documents của người dùng chưa được mã hóa, có nguy cơ lộ thông tin cá nhân của học sinh."*

### 8. Nhà giáo dục
> *"Phân hệ Đoàn Đội đóng vai trò thúc đẩy phong trào thi đua và rèn luyện đạo đức. Việc chấm điểm thi đua cần liên kết chặt chẽ với xếp loại hạnh kiểm của nhà trường. Các hoạt động phong trào phải có hình ảnh minh họa thực tiễn, sinh động để khơi dậy tinh thần nhiệt huyết của học sinh."*

### 9. Nhà quản lý (Hiệu trưởng nhà trường)
> *"Tôi cần các báo cáo thống kê trực quan hơn trên Dashboard. Thay vì chỉ hiển thị các con số khô khan, Dashboard nên có biểu đồ cột thể hiện tiến độ đóng đoàn phí của các lớp và biểu đồ đường thể hiện điểm thi đua tuần/tháng để Ban giám hiệu có cái nhìn toàn cảnh tức thời."*

### 10. Trưởng bộ môn của trường
> *"Việc phê duyệt dự toán ngân sách hoạt động Đoàn cần nhanh chóng. Hệ thống cần tự động đưa ra cảnh báo nếu số chi vượt quá quỹ hiện có trước khi giáo viên bấm nút gửi đề xuất, giúp tiết kiệm thời gian chỉnh sửa kế hoạch nhiều lần."*

### 11. Giáo viên ưu tú với nhiều kinh nghiệm
> *"Giao diện điểm danh bằng RFID giúp chúng tôi tiết kiệm 10-15 phút mỗi buổi sinh hoạt. Nhưng nếu thẻ lỗi mà hiện hộp thoại bắt bấm 'OK' thì rất phiền phức khi đang đứng quản lý lớp. Hãy chuyển cảnh báo lỗi thành âm thanh bíp dài hoặc đổi màu đỏ trên màn hình để lớp học không bị gián đoạn."*

### 12. Học sinh (Đoàn viên/Đội viên)
> *"Chúng em muốn tự xem được điểm thi đua và lịch sử hoạt động của mình trên giao diện cá nhân. Các huy chương và danh hiệu thi đua nên được thiết kế đẹp mắt, mang tính trò chơi hóa (gamification) để tạo động lực phấn đấu."*

### 13. Nhân viên nhà trường
> *"Chức năng nhập danh sách từ tệp Excel rất tiện lợi. Tuy nhiên, nếu tệp Excel có cột trống hoặc viết sai tên lớp, hệ thống nên chỉ ra cụ thể dòng nào bị lỗi để chúng tôi sửa, thay vì báo lỗi chung chung hoặc bỏ qua âm thầm."*

### 14. Gamer giỏi (Chuyên gia Gamification & UX tương tác)
> *"Bảng xếp hạng Top 5 Đoàn viên xuất sắc trên Dashboard là một điểm cộng lớn. Tuy nhiên, hiệu ứng tương tác còn nghèo nàn. Rê chuột vào các thẻ thành tích cần có hiệu ứng nổi nhẹ (Scale/Shadow), biểu tượng huy chương vàng/bạc/đồng nên có màu sắc ánh kim nổi bật thay vì icon emoji mặc định của Windows."*

### 15. Cán bộ quản lý phòng giáo dục
> *"Hệ thống dữ liệu của phân hệ này phải đảm bảo tính chuẩn hóa cao để có thể tích hợp và báo cáo số liệu lên cơ sở dữ liệu dùng chung của Phòng Giáo dục theo định kỳ."*

### 16. Chuyên viên sở giáo dục
> *"Mẫu biểu và quy trình kết nạp Đoàn viên mới trong hệ thống cần cập nhật chính xác theo Điều lệ Đoàn TNCS Hồ Chí Minh sửa đổi mới nhất để đảm bảo tính pháp lý sư phạm."*

### 17. Nhà khoa học giáo dục
> *"Việc đánh giá thi đua không nên chỉ dựa trên các điểm phạt hoặc điểm trừ. Hệ thống cần khuyến khích việc tốt qua tính năng ghi nhận gương người tốt việc tốt (được tích hợp trực tiếp vào điểm thi đua) để định hướng nhân cách tích cực cho học sinh."*

---

## ═══ PHẦN III: KẾ HOẠCH NÂNG CẤP & CẢI TIẾN CHI TIẾT (v4.1 COMPLIANCE) ═══

Để đáp ứng hoàn hảo bộ quy chuẩn QA SmartClass v4.1, Hội đồng đề xuất các hành động sửa lỗi và tối ưu hóa kỹ thuật lập tức như sau:

### 1. Chuẩn hóa và Đồng bộ hóa cơ sở dữ liệu
*   **Hành động:** Thay đổi toàn bộ việc lưu trữ dữ liệu loại thành viên và chức vụ về dạng chuỗi không dấu chuẩn (hoặc tích hợp bộ chuyển đổi Converter hai chiều hoàn chỉnh).
*   **Giải pháp code:** Cập nhật các biểu mẫu lưu trữ dữ liệu để ánh xạ chuẩn:
    *   `UI: "Đoàn viên" <--> DB: "Doan vien"`
    *   `UI: "Đội viên" <--> DB: "Doi vien"`
    *   `UI: "Thành viên" <--> DB: "Thanh vien"`
    *   `UI: "Bí thư" <--> DB: "Bi thu"`
    *   `UI: "Phó Bí thư" <--> DB: "Pho BT"`
    *   `UI: "Ủy viên BCH" <--> DB: "UV BCH"`

### 2. Thiết kế Lớp phủ Hướng dẫn sử dụng từng bước (Step-by-step UX Guide)
*   **Hành động:** Bổ sung vào tất cả các tệp `.xaml` một thẻ `Border` hướng dẫn trực quan dạng Expandable/Collapsible ở đầu trang (sử dụng màu xanh mint ấm áp `#ECFDF5` hoặc xanh dương nhẹ `#EFF6FF` tùy thuộc tính năng).
*   **Mẫu cấu trúc XAML gợi ý:**
```xml
<Expander Header="💡 Hướng dẫn sử dụng nhanh hệ thống" Background="#F0F9FF" BorderBrush="#BAE6FD" Margin="0,0,0,15" IsExpanded="True">
    <StackPanel Padding="15">
        <TextBlock Text="Các bước thực hiện nghiệp vụ:" FontWeight="Bold" Foreground="#0369A1" Margin="0,0,0,5"/>
        <TextBlock Text="1. Tìm kiếm và chọn lọc đối tượng từ thanh công cụ." Margin="0,2"/>
        <TextBlock Text="2. Nhập thông tin/điểm số chính xác theo quy chế thi đua." Margin="0,2"/>
        <TextBlock Text="3. Nhấn 'Lưu' để đồng bộ dữ liệu lên hệ thống và gửi thông báo cho phụ huynh." Margin="0,2"/>
    </StackPanel>
</Expander>
```

### 3. Tối ưu hóa luồng RFID & Khắc phục treo UI
*   **Hành động:** Thay thế toàn bộ `MessageBox.Show()` trong luồng `ProcessCardScanAsync` bằng thông báo trạng thái trên giao diện (Status Label) màu đỏ nổi bật, kết hợp phát tiếng bíp cảnh báo lỗi có tần số khác biệt để người dùng nhận biết ngay mà không làm nghẽn luồng.
*   **Giải pháp code:**
```csharp
if (matchedMember == null)
{
    // Thay vì MessageBox.Show làm treo luồng:
    TxtRfidStatus.Text = $"Cảnh báo: Thẻ {cardUid} không hợp lệ!";
    TxtRfidStatus.Foreground = Brushes.Red;
    System.Media.SystemSounds.Hand.Play(); // Âm thanh lỗi
    return;
}
```

### 4. Việt hóa triệt để và Tối ưu hóa mỹ thuật
*   **Hành động:** 
    *   Tạo các Converter chuyên dụng (`StatusConverter`, `FeeConverter`, `PlanStatusConverter`) để chuyển toàn bộ dữ liệu trạng thái tiếng Anh từ Database sang tiếng Việt có dấu đầy đủ trước khi hiển thị lên DataGrid hoặc Badge.
    *   Cải tiến các giá trị hiển thị cứng trong code-behind của Dashboard và FeeTracker.
    *   Tích hợp thư viện LiveCharts hoặc WpfAnalyzers để vẽ biểu đồ trực quan hóa dữ liệu Đoàn phí và điểm thi đua.

---

## ═══ KẾT LUẬN ═══

Hội đồng Chuyên gia đề nghị đội ngũ phát triển phần mềm QA Smart School lập tức thực hiện các nâng cấp và sửa lỗi nêu trên. Việc hoàn thiện các chi tiết nhỏ này sẽ nâng tầm phân hệ **Quản lý Phụ trách Đoàn Đội** đạt chuẩn sư phạm quốc gia v4.1, mang lại trải nghiệm sử dụng tin cậy, mượt mà và an toàn cho nhà trường.

**Chữ ký xác nhận của Chủ tịch Hội đồng Thẩm định**  
*Trưởng bộ phận thiết kế dự án QA Smart School*
