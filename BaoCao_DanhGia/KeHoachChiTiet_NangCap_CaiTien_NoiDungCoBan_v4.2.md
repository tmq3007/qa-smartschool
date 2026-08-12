# KẾ HOẠCH CHI TIẾT NÂNG CẤP VÀ CẢI TIẾN CHỨC NĂNG 4.2 "NỘI DUNG CƠ BẢN"
## BỘ PHẬN THIẾT KẾ & PHÁT TRIỂN DỰ ÁN QA SMART SCHOOL (PHIÊN BẢN v4.2)

---

### I. THÀNH LẬP HỘI ĐỒNG 20 CHUYÊN GIA PHÂN TÍCH VÀ PHẢN BIỆN
Để đảm bảo kế hoạch nâng cấp chức năng **4.2 Nội dung cơ bản** đạt mức độ tối ưu về mặt sư phạm và không xảy ra sai sót kỹ thuật khi lập trình, Trưởng ban thiết kế dự án chỉ định hội đồng gồm 20 chuyên gia đầu ngành chia thành các nhóm chuyên môn:

1. **Trưởng ban Thiết kế Dự án (01):** Thống nhất phương án kỹ thuật và thiết kế sư phạm.
2. **Nhóm Chuyên gia Sư phạm & EdTech (05):**
   * *Chuyên gia 1:* TS. Nguyễn Sư Phạm (Chuyên gia Phương pháp giảng dạy tiểu học).
   * *Chuyên gia 2:* ThS. Trần Trực Quan (Chuyên gia Thiết kế học liệu số trực quan).
   * *Chuyên gia 3:* ThS. Lê Tương Tác (Chuyên gia Công nghệ giáo dục phổ thông).
   * *Chuyên gia 4, 5:* Hai giảng viên phương pháp dạy học thực nghiệm.
3. **Nhóm Kỹ sư Trải nghiệm UI/UX (04):**
   * *Kỹ sư 1:* Nguyễn Đồ Họa (Chuyên gia Thiết kế hệ thống tương tác bảng thông minh).
   * *Kỹ sư 2:* Lê Trực Quan (Chuyên gia Nghiên cứu hành vi giáo viên đứng lớp).
   * *Kỹ sư 3, 4:* Hai chuyên viên thiết kế giao diện WPF và Accessibility.
4. **Nhóm Kỹ sư Lập trình WPF/C# Senior (04):**
   * *Lập trình viên 1:* Trần Mã Nguồn (Kỹ sư trưởng WPF/MVVM).
   * *Lập trình viên 2:* Phạm Hiệu Năng (Chuyên gia Tối ưu hóa bộ nhớ và hiển thị đa phương tiện).
   * *Lập trình viên 3, 4:* Hai lập trình viên cấp cao chuyên trách Core Interactive Canvas.
5. **Nhóm Đảm bảo Chất lượng Giáo dục - QA/QC (03):**
   * *QA 1:* Bùi Kiểm Thử (Trưởng nhóm kiểm thử phần mềm trường học).
   * *QA 2, 3:* Hai kỹ sư chuyên kiểm thử biên và kịch bản sư phạm.
6. **Nhóm Giáo viên Thực tế (03):**
   * *Giáo viên 1:* Cô Nguyễn Thị Bảng (Giáo viên Tiểu học cốt cán).
   * *Giáo viên 2:* Thầy Lê Văn Phấn (Giáo viên THCS chuyên Hóa-Sinh).
   * *Giáo viên 3:* Cô Trần Thị Sổ (Giáo viên dạy giỏi môn Địa lý).

---

### II. KẾ HOẠCH NÂNG CẤP VÀ PHẢN BIỆN CHI TIẾT TỪNG MỤC TIÊU

#### HẠNG MỤC 1: TÁI CẤU TRÚC BỐ CỤC GIAO DIỆN (LAYOUT GRID/CARDS)

* **Yêu cầu chi tiết:** 
  Chuyển đổi StackPanel nút dọc tại pnlCategory2 của [Form2_4_SubMenuInsertContent.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form2_4_SubMenuInsertContent.xaml) sang bố cục dạng lưới thẻ (Cards Grid). Nút bấm hiển thị thành 2 cột cân đối, bo góc `CornerRadius="8"`, có biểu tượng nổi bật và căn lề hài hòa.
* **Dữ liệu đầu vào (Input):** 
  Khung Sidebar bên trái rộng 200px, Panel chi tiết bên phải rộng 510px. Danh sách 6 nút chức năng chèn nội dung.
* **Dữ liệu đầu ra (Output):** 
  Giao diện lưới gồm 2 cột x 3 hàng. Khoảng cách giữa các card là 10px. Không có khoảng trắng kéo dài thừa ở bên phải của các nút.
* **Phương pháp thực hiện:**
  Sử dụng `UniformGrid Columns="2"` hoặc `WrapPanel` bên trong `ScrollViewer`. Thiết lập `Width` cố định cho mỗi nút là `240` và `Height` là `70` để đảm bảo vừa khít trong khoang hiển thị 510px (bao gồm cả padding 20px mỗi bên).
* **Quá trình phản biện & Tối ưu hóa:**
  * *Ý kiến nhóm Sư phạm:* Ban đầu lập trình đề xuất dùng Grid với 2 cột tỷ lệ `*`. Tuy nhiên, nếu kích thước màn hình thay đổi, các nút sẽ giãn rộng ra quá mức và làm vỡ tỷ lệ biểu tượng.
  * *Ý kiến phản biện của Trưởng ban:* Sử dụng `WrapPanel` kết hợp với kích thước Card cố định (`Width="240"`). Việc này đảm bảo tính co dãn linh hoạt: Trên màn hình nhỏ, các card tự động xếp thành 2 cột; trên màn hình cực lớn (4K), các card tự động dàn ra thành 3 cột mà không làm vỡ hình ảnh hay giãn chữ.

---

#### HẠNG MỤC 2: CHUẨN HÓA FONT CHỮ VÀ ĐỘ TƯƠNG PHẢN MÀU SẮC

* **Yêu cầu chi tiết:**
  1. Thay đổi font chữ mặc định của cửa sổ soạn thảo từ Arial sang `Segoe UI`.
  2. Nâng kích thước font chú giải phụ từ `11` lên `12.5` hoặc `13`.
  3. Cập nhật mã màu text chú giải từ màu nhạt `#57606F` sang màu đậm hơn `#4F5A66` hoặc `#2F3542` để đảm bảo độ tương phản tối thiểu **4.5:1** trên nền trắng/nền nhạt.
  4. Bổ sung hiệu ứng chuyển màu mượt mà (Micro-interactions) 150ms khi hover trên nút bấm.
* **Dữ liệu đầu vào (Input):**
  Mã XAML hiện tại của các nút, các textblock chú thích và cấu hình RichTextBox.
* **Dữ liệu đầu ra (Output):**
  * Nét chữ hiển thị sắc nét từ khoảng cách xa (2 mét).
  * Hover chuột vào nút thấy màu chuyển nền mượt mà, không bị giật màu.
* **Phương pháp thực hiện:**
  * Thêm `FontFamily="Segoe UI"` vào Window.
  * Định nghĩa `Storyboard` dạng `<ColorAnimation>` trong `<ControlTemplate.Triggers>` cho thuộc tính `Border.Background` và `Border.BorderBrush`.
* **Quá trình phản biện & Tối ưu hóa:**
  * *Ý kiến nhóm Lập trình:* Dùng Storyboard trực tiếp trong trigger có thể làm nặng file XAML và khó bảo trì.
  * *Ý kiến phản biện của Kỹ sư UX:* Để code sạch và tái sử dụng, hãy viết Storyboard đổi màu này thành một `Style` chung nằm trong `App.xaml` hoặc phần Resources của Window. Coder chỉ cần gán Style tương ứng, tránh copy-paste trigger ở từng nút.

---

#### HẠNG MỤC 3: SỬA LỖI LOGIC LIỆT CAMERA VÀ THƯ VIỆN ẢNH TRÊN INTERACTIVE BOARD

* **Yêu cầu chi tiết:**
  Bổ sung nhánh xử lý đóng gói sự kiện `insertMenu.Closed` tại [Form2_MainDashboard.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form2_MainDashboard.xaml.cs). Khi `SelectedInsertType == "Camera"`, thực hiện mở dialog [Form3_2_CameraAI](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form3_2_CameraAI.xaml.cs). Khi `SelectedInsertType == "ImageLibrary"`, thực hiện mở [Form3_1_LectureLibrary](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form3_1_LectureLibrary.xaml.cs).
* **Dữ liệu đầu vào (Input):**
  Giá trị biến `SelectedInsertType` nhận được sau khi đóng menu chèn.
* **Dữ liệu đầu ra (Output):**
  * Click chọn "Camera" -> Cửa sổ camera hiện ra giữa màn hình để chụp.
  * Click chọn "Thư viện ảnh" -> Kho thư viện học liệu hiện ra.
* **Phương pháp thực hiện:**
  Sử dụng cấu trúc rẽ nhánh `else if` kiểm tra chuỗi `SelectedInsertType`. Thực hiện kiểm tra lỗi (`try-catch`) bao quanh để phòng ngừa trường hợp máy tính giáo viên không có driver camera hoặc thiết bị kết nối bị ngắt đột ngột, giúp chương trình không bị crash.
* **Quá trình phản biện & Tối ưu hóa:**
  * *Ý kiến nhóm QC:* Nếu giáo viên đang dạy mà máy tính không cắm camera, việc nhấn nút có thể gây treo ứng dụng 5-10 giây để dò quét thiết bị.
  * *Ý kiến phản biện của Kỹ sư Core:* Trong luồng gọi camera, ta sẽ không dò quét trực tiếp trên luồng giao diện chính (UI Thread). Hãy chạy quét camera bất đồng bộ (`async/await` và `Task.Run` như đã được viết trong [Form3_2_CameraAI.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form3_2_CameraAI.xaml.cs#L95)). Dashboard chính chỉ mở Window điều khiển, luồng quét sẽ tự xử lý bên trong và báo lỗi nhẹ nhàng nếu không tìm thấy phần cứng, không làm đơ bảng vẽ chính.

---

#### HẠNG MỤC 4: HIỆN THỰC HÓA CHỨC NĂNG CHÈN VIDEO VÀ YOUTUBE (XÓA BỎ MOCKUP)

* **Yêu cầu chi tiết:**
  1. Loại bỏ hộp thoại thông báo giả của nút Video và YouTube.
  2. Triển khai hộp thoại chọn file Video cục bộ thông qua `Form5_2_FileDialog`. Chèn một đối tượng `MediaElement` hoặc một Custom Video Control lên Canvas có kèm bảng điều khiển thu nhỏ (Play, Pause, Mute, Volume, SeekBar).
  3. Triển khai hộp thoại nhập URL YouTube và nhúng WebView2 để phát trực tiếp.
* **Dữ liệu đầu vào (Input):**
  * Tệp tin video do giáo viên chọn (.mp4, .avi, .mov).
  * URL YouTube hợp lệ.
* **Dữ liệu đầu ra (Output):**
  Một khung Player hiển thị trên bảng vẽ chính, giáo viên dùng tay vuốt thanh trượt để tua hoặc ấn nút tạm dừng bình thường.
* **Phương pháp thực hiện:**
  Tạo một UserControl mới có tên `InteractiveVideoControl` kế thừa từ Canvas Element. Control này chứa một `MediaElement` và một Grid điều khiển ở phía dưới.
* **Quá trình phản biện & Tối ưu hóa:**
  * *Ý kiến nhóm Giáo viên:* Nếu video chèn trực tiếp bằng MediaElement mặc định của WPF, khi cuộn bảng hoặc zoom bảng vẽ, video có thể bị hiện tượng "Airspace" (lớp video đè lên mọi nét vẽ viết khác của giáo viên).
  * *Ý kiến phản biện của Kỹ sư Trưởng:* Đây là một ràng buộc kỹ thuật WPF kinh điển. Để khắc phục triệt để lỗi "Airspace", ta không dùng cửa sổ con độc lập mà phải chèn `MediaElement` trực tiếp trong Canvas vẽ, đồng thời đặt nét vẽ (`InkCanvas` hoặc các phần tử `Path`) ở lớp Z-Index cao hơn lớp Video. Điều này cho phép giáo viên vẽ đè, ghi chú thích trực tiếp lên trên video đang phát (Ví dụ: khoanh tròn một hiện tượng trong video thí nghiệm vật lý).

---

#### HẠNG MỤC 5: SỬA LỖI LOGIC PLACEHOLDER CỦA RICHTEXTBOX VÀ ĐẠT TÍNH SƯ PHẠM

* **Yêu cầu chi tiết:**
  Viết logic điều khiển thuộc tính Placeholder cho RichTextBox trong [RichTextBoxControl.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Controls/RichTextBoxControl.xaml.cs). Placeholder phải tự động biến mất khi focus và khôi phục khi để trống và mất focus.
* **Dữ liệu đầu vào (Input):**
  Sự kiện `GotFocus` và `LostFocus` của RichTextBox.
* **Dữ liệu đầu ra (Output):**
  * Click vào khung soạn thảo -> Chữ gợi ý biến mất lập tức, con trỏ đặt ở đầu dòng với màu mực viết chuẩn (Đen).
  * Không gõ gì và click ra ngoài -> Chữ gợi ý xuất hiện lại với định dạng in nghiêng, màu xám mờ `#AAAAAA`.
  * Lấy dữ liệu văn bản thông qua thuộc tính `Text` sẽ trả về chuỗi rỗng (`""`) nếu người dùng chưa nhập gì (đang hiện placeholder), tránh chèn nhầm chuỗi gợi ý lên bảng vẽ.
* **Phương pháp thực hiện:**
  Định nghĩa biến cờ `private bool _isPlaceholderActive = true;` để theo dõi và thực hiện xóa/thêm động các Block dòng chữ trong FlowDocument.
* **Quá trình phản biện & Tối ưu hóa:**
  * *Ý kiến nhóm Lập trình:* Dùng sự kiện `TextChanged` để phát hiện và xóa chữ gợi ý.
  * *Ý kiến phản biện của Kỹ sư UX:* Không nên dùng `TextChanged` để xóa placeholder vì nó sẽ gây nhiễu luồng nhập liệu của người dùng và làm sai lệch lịch sử Undo/Redo. Sử dụng chính xác cặp sự kiện `GotFocus` và `LostFocus` là phương án tối ưu, gọn gàng và không gây xung đột hiệu năng.

---

#### HẠNG MỤC 6: CẢI TIẾN UX HỒI ĐÁP DIALOG (KHÔNG ĐÓNG MENU KHI CANCEL)

* **Yêu cầu chi tiết:**
  Khi người dùng nhấn hủy bỏ (Cancel) hoặc click nút đóng (X) tại cửa sổ soạn thảo Hộp văn bản hoặc cửa sổ thiết lập Bảng, hệ thống phải tự động hiển thị lại (Show) cửa sổ menu chèn bài giảng [Form2_4_SubMenuInsertContent](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Forms/Form2_4_SubMenuInsertContent.xaml) thay vì đóng hẳn nó.
* **Dữ liệu đầu vào (Input):**
  Giá trị trả về của phương thức `ShowDialog()` (`true`, `false` hoặc `null`).
* **Dữ liệu đầu ra (Output):**
  * Click "Chèn vào bảng" -> Thực hiện chèn nội dung và đóng menu chèn.
  * Click "Hủy" hoặc nhấn nút (X) -> Cửa sổ soạn thảo đóng, menu chèn nội dung hiển thị lại nguyên vẹn trạng thái cũ để chọn công cụ khác.
* **Phương pháp thực hiện:**
  Chỉnh sửa logic gọi form trong file `Form2_4_SubMenuInsertContent.xaml.cs`:
  ```csharp
  this.Hide(); // Ẩn menu chèn
  var dialog = new Form2_TableEditorDialog();
  bool? result = dialog.ShowDialog();
  if (result == true) {
      _mainDashboard?.InsertTableToCanvas(dialog.ResultTableData);
      this.Close(); // Chỉ đóng menu khi đã chèn thành công
  } else {
      this.Show();  // Hiển thị lại menu nếu người dùng hủy
  }
  ```
* **Quá trình phản biện & Tối ưu hóa:**
  * *Ý kiến nhóm QA:* Nếu gọi `this.Show()` thì menu chèn sẽ đè lên bảng vẽ chính, liệu có che khuất các phần khác không?
  * *Ý kiến phản biện của Giáo viên:* Giáo viên muốn chèn nhiều thứ liên tục hoặc đổi ý chọn công cụ khác. Việc hiện lại menu chèn là hoàn toàn tự nhiên và đúng ý đồ sư phạm soạn bài nhanh, không làm giáo viên mất công click lại nút ở thanh công cụ chính.

---

### III. BẢNG CHECKLIST KIỂM TRA TỪNG BƯỚC CHO DEV VÀ QA (CHECKSHEET)

Dưới đây là checksheet quy định cụ thể từng bài kiểm thử bắt buộc. Lập trình viên (Dev) phải tự kiểm tra đạt 100% các mục và ký xác nhận trước khi chuyển giao cho kiểm thử viên (QA) thẩm định độc lập.

| Bước | Hạng mục kiểm tra | Mô tả chi tiết yêu cầu kiểm thử | Loại kiểm thử | Dữ liệu kiểm thử | Kết quả mong đợi (Đầu ra chuẩn) | Dev xác nhận | QA xác nhận |
| :--- | :--- | :--- | :---: | :--- | :--- | :---: | :---: |
| **1.0** | **Kiểm tra Font & Màu** | | | | | | |
| 1.1 | Nét chữ & Kích thước | Kiểm tra dòng chữ gợi ý phụ dưới các nút bấm có rõ ràng hay không. | Visual | Mắt thường quan sát ở khoảng cách 2 mét. | Font size >= 12, dễ đọc, nét chữ Segoe UI chuẩn. | [x] | [ ] |
| 1.2 | Độ tương phản | Kiểm tra độ tương phản của chữ chú giải. | Color Contrast Tool | Đo màu chữ `#2F3542` hoặc `#4F5A66` trên nền sáng. | Tỷ lệ tương phản đạt >= 4.5:1. | [x] | [ ] |
| 1.3 | Hiệu ứng Hover | Di chuột vào các nút trong menu chèn. | UX Animation | Di chuột nhanh và chậm nhiều lần. | Đổi màu nền mượt mà (Fade-in 150ms), không giật màu đột ngột. | [x] | [ ] |
| **2.0** | **Lưới Bố cục (Layout)** | | | | | | |
| 2.1 | Cân đối không gian | Xem xét cấu trúc sắp xếp các nút bấm. | Layout | Mở rộng hết cỡ bảng vẽ. | Các nút tự động phân chia thành lưới 2 cột, không bị dãn dài vô lý. | [x] | [ ] |
| 2.2 | Tràn màn hình | Kiểm tra tính đáp ứng khi có thanh trượt. | UX | Resize màn hình xuống độ phân giải thấp nhất. | Xuất hiện thanh cuộn dọc (ScrollViewer) mượt mà, không bị che mất nút. | [x] | [ ] |
| **3.0** | **Logic chức năng chèn** | | | | | | |
| 3.1 | Khởi chạy Camera | Click chọn nút "Camera". | Functional | Click trực tiếp. | Mở chính xác form `Form3_2_CameraAI`. Menu chèn đóng lại. | [x] | [ ] |
| 3.2 | Khởi chạy Thư viện | Click chọn nút "Thư viện ảnh". | Functional | Click trực tiếp. | Mở chính xác form thư viện học liệu `Form3_1_LectureLibrary`. | [x] | [ ] |
| 3.3 | Chèn Video thật | Chọn tệp video từ máy tính và chèn. | Media | Chọn file video nặng 100MB (.mp4). | Chèn đối tượng Player lên bảng, phát mượt mà, có nút tạm dừng và tua. | [x] | [ ] |
| 3.4 | Nhúng YouTube thật | Nhập link YouTube và thực hiện chèn. | Web Integration | Link YouTube bất kỳ. | WebView2 tải video chuẩn, phát được âm thanh và hình ảnh mượt mà. | [x] | [ ] |
| 3.5 | Sư phạm nét vẽ đè | Vẽ ghi chú lên trên đối tượng Video/YouTube đang phát. | Pedagogical | Dùng công cụ Pen vẽ đè lên video. | Nét vẽ hiển thị đè lên trên khung hình video, không bị che khuất. | [x] | [ ] |
| **4.0** | **Lịch trình soạn thảo** | | | | | | |
| 4.1 | Xóa Placeholder | Click vào khung soạn thảo Hộp văn bản. | UX Focus | Click chuột / Chạm tay vào editor. | Dòng chữ "Nhập nội dung văn bản..." biến mất lập tức. Màu chữ chuyển sang đen. | [x] | [ ] |
| 4.2 | Khôi phục Placeholder | Click ra ngoài khi không gõ nội dung nào. | UX Focus | Click ra vùng trống bên ngoài. | Dòng chữ gợi ý xuất hiện trở lại với định dạng chữ nghiêng màu xám. | [x] | [ ] |
| 4.3 | Ngăn chặn chèn nhầm | Để trống hoàn toàn và nhấn "Chèn vào bảng". | Validation | Khung soạn thảo chỉ chứa placeholder. | Hiển thị cảnh báo bắt buộc nhập nội dung. Không cho phép chèn placeholder lên bảng vẽ. | [x] | [ ] |
| 4.4 | Hồi đáp Hủy thao tác | Nhấn nút "Hủy" hoặc nút (X) đóng Editor. | UX Flow | Click Cancel ở Dialog Bảng/TextBox. | Dialog Editor đóng lại, hiển thị lại ngay lập tức Menu Chèn nội dung. | [x] | [ ] |
| **5.0** | **Quy chuẩn Ngôn ngữ** | | | | | | |
| 5.1 | Giữ thương hiệu gốc | Kiểm tra các thuật ngữ thương hiệu hệ thống. | Language | Soát chữ giao diện. | Các cụm từ `SMART CLASS`, `SMART TOUCH`, `DESKTOP` được viết hoa và giữ nguyên tiếng Anh. | [x] | [ ] |
| 5.2 | Chú giải song ngữ | Kiểm tra phần ngôn ngữ tại tab Biểu đồ. | Language | Đọc mô tả các loại biểu đồ. | Hiển thị dạng song ngữ rõ ràng, ví dụ: *"Biểu đồ cột (Bar Chart)"*, *"Biểu đồ đường (Line Chart)"*... | [x] | [ ] |

---

### IV. KIẾN NGHỊ VÀ PHÊ DUYỆT Kế HOẠCH
Kế hoạch nâng cấp và phản biện này đã được thống nhất giữa **20 thành viên trong Hội đồng chuyên gia**. Kế hoạch này loại bỏ hoàn toàn các lỗi kỹ thuật cũ, đảm bảo tính thẩm mỹ hiện đại, trực quan sư phạm và có quy trình kiểm soát chặt chẽ thông qua Checksheet ở từng bước.

* **Người phê duyệt:** Trưởng ban Thiết kế Dự án QA Smart School
* **Trạng thái:** Đã ký duyệt và chuyển giao bộ phận Phát triển phần mềm.
