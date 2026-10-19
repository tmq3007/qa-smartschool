# BÁO CÁO CÔNG VIỆC PHÂN HỆ SMART CLASS – QA SmartClass v4.2

- **Người thực hiện:** tmq3007
- **Phạm vi:** phân hệ SMART CLASS (quản lý lớp học dành cho giáo viên)

---

## I. TỔNG QUAN NHỮNG VIỆC ĐÃ LÀM

Trong giai đoạn vừa qua, phân hệ SMART CLASS được cải tiến theo 4 hướng chính:

1. **Làm ứng dụng chạy nhanh và mượt hơn:** mở trang gần như tức thì, danh sách học sinh/môn học/thời khóa biểu tải nhanh, cuộn và vuốt trên màn hình cảm ứng êm hơn, không còn nháy trắng khi đăng nhập.
2. **Hoàn thiện nghiệp vụ lớp học:** bổ sung quản lý danh mục môn học, cấu hình giờ học các tiết (mùa hè, mùa đông, giờ chuẩn), thời khóa biểu riêng cho từng lớp, tính năng "Vào dạy" nhanh ngay trên thời khóa biểu.
3. **Phân quyền rõ ràng theo vai trò:** Admin, Hiệu trưởng, Hiệu phó và Giáo viên mỗi người chỉ thấy đúng chức năng của mình; giáo viên chỉ thấy lớp mình phụ trách.
4. **Làm mới giao diện:** thiết kế lại các màn hình Khảo sát, Giám sát học sinh, Quản lý học sinh, Thời khóa biểu, thanh menu bên trái và thanh chế độ nổi; hiển thị đẹp hơn và thao tác chạm thuận tiện hơn trên màn hình tương tác trong lớp học.

Bên cạnh đó, mã nguồn của toàn bộ phân hệ được **dọn dẹp, chuẩn hóa** (khoảng 40 màn hình) để dễ bảo trì và ít lỗi hơn về sau.

### Bảng tóm tắt

| Nhóm công việc | Kết quả đạt được |
|---|---|
| Tốc độ & độ mượt | Chuyển trang tức thì; tải danh sách nhanh; không nháy trắng khi đăng nhập; vuốt cảm ứng mượt |
| Môn học & Thời khóa biểu | Quản lý môn học; chọn giờ học theo mùa; thời khóa biểu từng lớp; vào dạy nhanh |
| Học sinh & Nhóm | Giao diện quản lý mới; xem theo lớp của tôi / toàn trường; chia nhóm học sinh |
| Phân quyền | Mỗi vai trò chỉ thấy chức năng phù hợp; tự về trang mặc định theo vai trò |
| Giao diện | Làm mới Khảo sát, Giám sát, Menu, Thanh chế độ nổi, Nhắn tin |
| Chất lượng mã nguồn | Chuẩn hóa ~40 màn hình, thống nhất hộp thông báo, giảm mã trùng lặp |

---

## II. CHI TIẾT TỪNG HẠNG MỤC

### 1. Tăng tốc độ và độ mượt

**1.1. Chuyển trang nhanh**
- Ứng dụng tự **chuẩn bị sẵn các trang hay dùng** trong lúc máy rảnh, nên lần mở đầu tiên cũng hiện ngay.
- Khi bấm chuyển trang: trang hiện lên ngay lập tức, hiệu ứng chuyển cảnh chạy mượt, dữ liệu được tải ngầm phía sau nên màn hình không bị đứng.
- Các trang đã mở được giữ lại, quay lại trang cũ vẫn giữ nguyên trạng thái và mở rất nhanh.

**1.2. Tải danh sách nhanh hơn**
- Danh sách lớp, học sinh, môn học được **ghi nhớ trong bộ nhớ máy**, không phải đọc lại cơ sở dữ liệu mỗi lần mở; khi dữ liệu thay đổi thì tự cập nhật.
- Danh sách lớn được nạp một lần thay vì từng dòng, màn hình không bị giật.
- Danh sách học sinh chỉ vẽ phần đang nhìn thấy nên cuộn mượt kể cả khi lớp đông.
- Màn hình Thời khóa biểu không còn tải dữ liệu hai lần khi chuyển trang; màn hình Bài giảng tải ngầm, không làm đơ giao diện.
- Khi chọn lớp ở màn hình Học sinh, bảng "đang tải" hiện ngay rồi mới dựng sơ đồ lớp, nên thao tác bấm có phản hồi tức thì.
- Ảnh đại diện học sinh tải nhẹ hơn và không bị khóa file; nếu chưa có ảnh sẽ hiện biểu tượng thay thế.

**1.3. Khởi động và cuộn mượt**
- **Hết nháy trắng khi đăng nhập:** màn hình đăng nhập chỉ đóng lại sau khi màn hình chính đã hiện xong, chuyển cảnh liền mạch; màu nền đăng nhập được đổi cho đồng bộ với màn hình chính.
- **Thanh cuộn mới** mảnh, gọn, kiểu Windows 11, chỉ rõ lên khi di chuột.
- **Vuốt bằng ngón tay** để cuộn được trên toàn bộ ứng dụng, thuận tiện cho màn hình cảm ứng trong lớp.

---

### 2. Quản lý Môn học
- Giáo viên/quản trị có thể **thêm, sửa, xóa** môn học trong một cửa sổ riêng, có ô **tìm kiếm** nhanh.
- Mỗi môn có **biểu tượng và màu sắc** riêng: chọn từ bộ mẫu có sẵn hoặc tự nhập mã màu.
- Tên viết tắt của môn được **tự động gợi ý** khi nhập tên môn.
- Có nút **khôi phục danh mục mặc định** gồm 20 môn theo Chương trình giáo dục phổ thông 2018.
- Khi danh mục môn thay đổi, Thời khóa biểu **cập nhật ngay**, không cần mở lại.
- Trong màn hình Thời khóa biểu có sẵn nút **Quản lý môn** và các nút chọn môn nhanh.

### 3. Cấu hình giờ học các tiết
- Có **3 mẫu giờ học** sẵn: Mùa hè (sáng 7h00, chiều 13h30), Mùa đông (sáng 7h30, chiều 13h15), Giờ chuẩn 7h15 (sáng 7h15, chiều 13h30).
- Ứng dụng **tự gợi ý theo mùa**: từ tháng 11 đến tháng 2 là mùa đông, còn lại là mùa hè.
- Chỉ cần nhập giờ bắt đầu buổi sáng và buổi chiều, hệ thống **tự tính giờ cho 10 tiết** (5 sáng, 5 chiều, mỗi tiết 45 phút).
- Lưu xong, giờ trên Thời khóa biểu cập nhật ngay.

### 4. Thời khóa biểu
- **Mỗi lớp có thời khóa biểu riêng** gồm 28 tiết/tuần theo chương trình, có giáo viên và phòng học; các lớp không còn bị trùng thời khóa biểu như trước. Dữ liệu cũ bị trùng được tự động thay thế và đồng bộ sang phần Bài giảng.
- **Vừa với mọi kích thước màn hình:** màn hình lớn thì các tiết tự giãn đều lấp đầy; laptop nhỏ vẫn giữ chữ rõ, cuộn được; phần cuối không bị thanh công cụ nổi che.
- **Bố cục gọn, dễ nhìn hơn:** cột tiết và cột ngày được cân lại, chữ và khoảng cách vừa phải.
- **Ô tiết học rõ ràng:** có thanh màu theo môn, tên môn, phòng học, nhãn **"ĐANG HỌC"** ở tiết hiện tại và nhãn bài giảng đã gán.
- **Vào dạy nhanh:** ở tiết đang diễn ra có nút **"Vào dạy"**; bấm vào là hệ thống tự mở lớp, tự tìm bài giảng phù hợp môn học và vào giao diện giảng dạy.
- Có **sổ ghi chép tiết học**, các **thẻ thống kê** đầu trang và phần tổng hợp cuối trang.

### 5. Quản lý Học sinh và Nhóm
- **Màn hình Học sinh được thiết kế lại:** danh sách lớp bên trái, thông tin chi tiết bên phải; giao diện gọn và đồng nhất hơn.
- **Xem theo phạm vi:** giáo viên mặc định thấy **lớp mình phụ trách**; có nút chuyển sang **toàn trường** (dành cho Admin, Ban giám hiệu, hoặc giáo viên dạy thay).
- Nếu giáo viên chưa được phân lớp, hệ thống hiển thị toàn bộ lớp để không bị trống.
- Hiển thị và cập nhật **ảnh đại diện**, số thứ tự học sinh.
- **Màn hình Nhóm:** chia nhóm học sinh theo lớp, chọn lớp nhanh và không bị đơ.

### 6. Phân quyền theo vai trò
- **Admin:** toàn quyền.
- **Hiệu trưởng:** toàn bộ quyền của Hiệu phó, cộng thêm các chức năng quản lý chiến lược (báo cáo Bộ Giáo dục, bảng lương, học phí, tài sản, thông báo toàn trường).
- **Hiệu phó:** chuyên môn dạy học, duyệt đơn nghỉ phép, xử lý sự cố, tổ chuyên môn; không thấy bảng lương, báo cáo Bộ và các chức năng kỹ thuật.
- **Giáo viên và nhân viên:** không thấy các báo cáo cấp quản lý và chức năng kỹ thuật; chỉ giữ các tiện ích cá nhân cần thiết.
- Menu bên trái **tự ẩn/hiện** theo quyền: Bảng tổng quan, Danh sách giáo viên, nhóm Quản trị hệ thống, và nhóm **"Phê duyệt & Ban giám hiệu"** (mới, chỉ Ban giám hiệu/Admin thấy).
- Sau khi đăng nhập, mỗi người được **đưa thẳng về trang phù hợp với vai trò**; nếu cố vào chức năng không có quyền sẽ được chuyển về trang giảng dạy.
- Nút "Trang chủ" cũng đưa về đúng trang mặc định của từng vai trò.
- **Sửa lỗi hiển thị sai tên giáo viên:** tên và bài giảng nay lấy đúng theo người đang đăng nhập.

### 7. Làm mới giao diện
- **Menu bên trái:** biểu tượng căn giữa chuẩn, có thể **thu nhỏ/mở rộng**; khi thu nhỏ vẫn dễ thao tác.
- **Thanh chế độ nổi:**
  - Thu gọn/mở rộng mượt, có thể bấm bằng cả chuột lẫn **ngón tay**.
  - Ở chế độ bảng vẽ SMART TOUCH, thanh tự thu gọn và nằm ở **góc trên bên phải** để không che thanh công cụ vẽ ở phía dưới; ở chế độ SMART CLASS và DESKTOP thanh mở rộng và nằm phía dưới màn hình.
- **Màn hình Khảo sát (thiết kế lại hoàn toàn):**
  - Chia 2 cột: bên trái là phần tạo khảo sát, bên phải là kết quả trực tiếp từ học sinh.
  - **Khảo sát nhanh** bằng 4 nút lớn dễ chạm: *Hiểu rồi / Chưa hiểu / Hơi chậm / Quá dễ*.
  - **Khảo sát tùy chỉnh:** chọn mẫu câu hỏi đã lưu, nhập câu hỏi và chọn lớp nhận khảo sát.
  - Giao diện bo tròn, có đổ bóng, chiếm đầy màn hình.
- **Màn hình Giám sát học sinh:** giao diện tối dễ nhìn, khung chờ khi chưa có hình màn hình học sinh, **nhãn đỏ cảnh báo học sinh làm việc riêng**, chấm xanh báo học sinh đang trực tuyến.
- **Màn hình Nhắn tin (giáo viên và học sinh):** hiển thị mượt hơn khi danh sách trò chuyện dài.

### 8. Chuẩn hóa và dọn dẹp mã nguồn
- Khoảng **40 màn hình** của phân hệ được rà soát và chuẩn hóa cách viết, giảm mã lặp lại, giúp sửa lỗi và nâng cấp sau này nhanh hơn.
- **Hộp thông báo thống nhất:** mọi thông báo thông tin, cảnh báo, lỗi dùng chung một kiểu giao diện.
- Thêm cơ chế **chống bấm nút liên tiếp** gây thực hiện trùng.
- Các tệp quá dài được **tách nhỏ** theo chức năng (dò tìm thiết bị trong mạng, trình soạn bài giảng) để dễ quản lý.
