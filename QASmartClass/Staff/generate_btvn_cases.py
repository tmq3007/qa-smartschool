import csv
import os
import sys

# Đảm bảo xuất file UTF-8-SIG để Excel hiển thị tiếng Việt chính xác
output_path = r"D:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\Staff\BTVN_1000_TestCases.csv"

# Tiêu đề cột
headers = [
    "Test_Case_Id",
    "Module",
    "Category",
    "Technical_Requirement",
    "Test_Scenario",
    "Preconditions",
    "Input_Data",
    "Steps",
    "Expected_Result",
    "Output_Data",
    "Checklist_Items",
    "Severity"
]

all_cases = []

# ==========================================
# MODULE 1: TEACHER GUI & LOCALIZATION (150 Cases)
# ==========================================
gui_elements_t = [
    ("guidePanel", "Bảng hướng dẫn sử dụng collapsible", "Visibility", "Nhấn nút '✕ Ẩn' và '💡 Hướng dẫn'"),
    ("filterAll", "Bộ lọc 'Tất cả'", "MouseLeftButtonDown / Tag='all'", "Nhấn vào tab lọc 'Tất cả'"),
    ("filterActive", "Bộ lọc '🟢 Đang mở'", "MouseLeftButtonDown / Tag='active'", "Nhấn vào tab lọc 'Đang mở'"),
    ("filterExpired", "Bộ lọc '⏰ Hết hạn'", "MouseLeftButtonDown / Tag='expired'", "Nhấn vào tab lọc 'Hết hạn'"),
    ("txtSubtitle", "Dòng mô tả phụ dưới tiêu đề chính", "TextBlock.Text", "Kiểm tra hiển thị text động theo số lượng bài tập"),
    ("txtStatTotal", "Hộp đếm tổng số bài tập", "TextBlock.Text / StatTotal", "Kiểm tra số lượng hiển thị"),
    ("txtStatActive", "Hộp đếm bài đang mở", "TextBlock.Text / StatActive", "Kiểm tra đếm bài còn hạn"),
    ("txtStatExpired", "Hộp đếm bài đã hết hạn", "TextBlock.Text / StatExpired", "Kiểm tra đếm bài quá hạn"),
    ("DownloadTemplateCommand", "Nút '📥 Tải mẫu Word'", "Button.Command", "Click nút tải mẫu"),
    ("ImportFromWordCommand", "Nút '📄 Nhập từ Word'", "Button.Command", "Click nút nhập file"),
    ("CreateHomeworkCommand", "Nút '➕ Giao BTVN mới'", "Button.Command", "Click nút tạo mới"),
    ("HomeworkItemViewModel.BgBorder", "Màu nền viền của Card bài tập", "WPF Border.Background", "Đọc danh sách hiển thị"),
    ("HomeworkItemViewModel.BorderBrush", "Màu viền ngoài của Card bài tập", "WPF Border.BorderBrush", "Kiểm tra viền"),
    ("HomeworkItemViewModel.AccentColor", "Thanh trạng thái bên trái Card bài tập", "Border.Background dọc", "Kiểm tra viền màu"),
    ("HomeworkItemViewModel.TimeInfo", "Dòng thông báo thời gian còn lại/đã quá hạn", "TextBlock.Text", "Hiển thị đếm ngược/quá hạn")
]

for i in range(150):
    element_key, element_name, req, action = gui_elements_t[i % len(gui_elements_t)]
    case_idx = i + 1
    tc_id = f"TC_T_GUI_{case_idx:03d}"
    
    # Tạo các ngữ cảnh kiểm thử giao diện chi tiết khác nhau (Độ phân giải, Font, Ngôn ngữ, Độ tương phản, DPI...)
    resolutions = ["1920x1080", "1366x768", "3840x2160 (4K)", "1024x768", "DPI 125%", "DPI 150%"]
    res = resolutions[i % len(resolutions)]
    
    scenario = f"Kiểm tra hiển thị {element_name} ({element_key}) ở độ phân giải {res}"
    if i % 5 == 1:
        scenario = f"Kiểm tra trạng thái MouseOver (Hover) và hiệu ứng mờ Opacity của {element_name}"
    elif i % 5 == 2:
        scenario = f"Kiểm tra tính bản địa hóa tiếng Việt có dấu, không lỗi font của nhãn {element_name}"
    elif i % 5 == 3:
        scenario = f"Kiểm tra thuộc tính hiển thị (Visibility) của {element_name} khi dữ liệu rỗng"
    elif i % 5 == 4:
        scenario = f"Kiểm tra độ tương phản màu sắc và khoảng cách Padding/Margin của {element_name} trên giao diện"

    steps = (
        f"1. Khởi động ứng dụng QA SmartClass ở chế độ Giáo viên.\n"
        f"2. Điều hướng tới mục 'IV. KIỂM TRA & BÀI TẬP' -> chọn '4.4 Bài tập về nhà'.\n"
        f"3. Thiết lập độ phân giải màn hình hoặc kích thước cửa sổ là {res}.\n"
        f"4. Thực hiện hành động: {action} và quan sát {element_name}."
    )
    
    expected = (
        f"Giao diện hiển thị đúng chuẩn thiết kế. {element_name} ({element_key}) không bị vỡ bố cục, không bị chồng đè text.\n"
        f"Màu sắc phối hợp hài hòa, phông chữ Segoe UI hiển thị sắc nét tiếng Việt.\n"
        f"Sự kiện tương tác hoạt động mượt mà."
    )
    
    input_data = f"Cấu hình giao diện màn hình: {res}, Cỡ chữ mặc định: 12, Thuộc tính Wpf: {req}"
    output_data = f"Trạng thái UI được cập nhật trên màn hình. Rendering thành công."
    
    checklist = (
        f"[ ] Text của {element_name} hiển thị chuẩn tiếng Việt không lỗi font.\n"
        f"[ ] Bố cục không bị tràn viền hoặc bị ẩn ở độ phân giải {res}.\n"
        f"[ ] Hiệu ứng Hover chuyển màu mượt mà theo đúng thuộc tính template."
    )
    
    severity = "Low" if i % 3 == 0 else "Medium"
    
    all_cases.append([
        tc_id, "Giáo viên", "GUI", f"HomeworkPage.xaml -> {element_key} ({req})",
        scenario, "Giáo viên đã đăng nhập và đang ở phân hệ Bài tập về nhà",
        input_data, steps, expected, output_data, checklist, severity
    ])

# ==========================================
# MODULE 2: TEACHER FUNCTIONAL & BUSINESS LOGIC (250 Cases)
# ==========================================
subjects = ["Toán", "Ngữ Văn", "Tiếng Anh", "Vật Lý", "Hóa Học", "Sinh Học", "Lịch Sử", "Địa Lý", "Tin Học", "Công Nghệ"]
for i in range(250):
    case_idx = i + 1
    tc_id = f"TC_T_FUN_{case_idx:03d}"
    
    # Chia nhỏ kịch bản nghiệp vụ: 1. Tạo bài (1-100), 2. Sửa bài (101-180), 3. Xóa bài (181-220), 4. Gửi lại (221-250)
    if case_idx <= 100:
        sub = subjects[i % len(subjects)]
        title_length = (i % 5) * 40 + 5 # 5, 45, 85, 125, 165 ký tự
        title_val = f"Bài tập môn {sub} số {case_idx} - " + "A" * title_length
        desc_val = f"Mô tả chi tiết bài tập {case_idx} môn {sub}. Yêu cầu hoàn thành đầy đủ."
        days_ahead = (i % 7) + 1
        deadline_date = f"DateTime.Today.AddDays({days_ahead})"
        time_str = ["08:00", "12:00", "17:00", "23:59"][i % 4]
        
        scenario = f"Kiểm tra Giao BTVN mới môn {sub} với tiêu đề dài {title_length} ký tự, hạn nộp lúc {time_str}"
        pre = "Giáo viên đang mở form 'Giao bài tập về nhà' bằng nút + Giao BTVN mới"
        input_data = f"Môn học: {sub}, Tiêu đề: '{title_val}', Nội dung: '{desc_val}', Hạn: {deadline_date} {time_str}, Đính kèm: None"
        steps = (
            f"1. Nhập vào ComboBox Môn học: {sub}.\n"
            f"2. Nhập vào TextBox Tiêu đề: '{title_val}'.\n"
            f"3. Nhập vào TextBox Nội dung: '{desc_val}'.\n"
            f"4. Chọn DatePicker Ngày hạn nộp: Today + {days_ahead} ngày.\n"
            f"5. Chọn ComboBox Giờ hạn nộp: {time_str}.\n"
            f"6. Nhấn nút 'Giao bài & Gửi HS'."
        )
        expected = (
            f"Hệ thống đóng dialog thành công. Hiển thị hộp thoại MessageBox 'Thành công' thông báo: 'Đã giao BTVN và gửi đến HS!'.\n"
            f"Bản ghi mới được ghi vào SQLite DB (EventLogs với EventType='HOMEWORK', Details dạng JSON của bài tập; "
            f"bảng Assignments tạo mới bản ghi với RosterId của lớp học hiện tại).\n"
            f"UI cập nhật thêm Card bài tập mới ở vị trí đầu danh sách. Số đếm StatTotal tăng 1."
        )
        output_data = f"EventLogId = sinh tự động; JSON Details ghi nhận đầy đủ thuộc tính; Bản ghi Assignment ID mới được tạo."
        checklist = (
            f"[ ] Bản ghi được tạo trong DB (bảng EventLogs và Assignments).\n"
            f"[ ] Card mới xuất hiện lập tức trên màn hình danh sách.\n"
            f"[ ] MessageBox hiển thị đúng nội dung: 'Đã giao BTVN và gửi đến HS!'."
        )
        severity = "High"
        
    elif case_idx <= 180:
        # Edit
        sub = subjects[i % len(subjects)]
        scenario = f"Kiểm tra Chỉnh sửa bài tập hiện có - Cập nhật Môn học thành {sub} và đổi tiêu đề"
        pre = "Danh sách đã có sẵn bài tập. Giáo viên nhấn nút ✏️ trên Card bài tập"
        input_data = f"ID bài tập cần sửa; Dữ liệu sửa mới: Môn học={sub}, Tiêu đề='Tiêu đề đã chỉnh sửa {case_idx}'"
        steps = (
            f"1. Nhấn nút ✏️ (Edit) trên Card bài tập thứ { (i%3)+1 }.\n"
            f"2. Chờ Dialog Chỉnh sửa hiện lên, đổi Môn học thành {sub}.\n"
            f"3. Đổi TextBox Tiêu đề thành 'Tiêu đề đã chỉnh sửa {case_idx}'.\n"
            f"4. Nhấn '💾 Cập nhật'."
        )
        expected = (
            f"Hộp thoại chỉnh sửa đóng lại. MessageBox thông báo cập nhật thành công hiển thị.\n"
            f"Dữ liệu được cập nhật trong DB SQLite (bản ghi EventLog được cập nhật chi tiết Details JSON với Timestamp hiện tại).\n"
            f"Card bài tập tương ứng hiển thị thông tin môn học và tiêu đề mới."
        )
        output_data = f"EventLog Details cập nhật; UI Render lại bài tập đã sửa."
        checklist = (
            f"[ ] EventLogs.Find(Id) trả về JSON đã cập nhật.\n"
            f"[ ] UI Card hiển thị Môn học mới là {sub}.\n"
            f"[ ] Không xuất hiện bài tập nhân bản thừa."
        )
        severity = "High"
        
    elif case_idx <= 220:
        # Delete
        scenario = f"Kiểm tra Xóa bài tập về nhà - Xác nhận xóa và kiểm tra gỡ bỏ khỏi cơ sở dữ liệu"
        pre = "Có ít nhất 1 bài tập trong danh sách hiển thị."
        input_data = f"Bấm nút 🗑️ trên Card bài tập. Lựa chọn xác nhận: Yes hoặc No."
        steps = (
            f"1. Nhấn nút 🗑️ (Xóa bài tập) trên Card bài tập.\n"
            f"2. Chờ hộp thoại xác nhận hiện ra hỏi 'Bạn có chắc chắn muốn xóa bài tập này?...'.\n"
            f"3. Nhấn 'Yes' để đồng ý xóa."
        )
        expected = (
            f"Hệ thống thực hiện xóa bản ghi EventLog khỏi Database (Remove log, SaveChanges).\n"
            f"Card bài tập lập tức biến mất khỏi UI danh sách. Các chỉ số thống kê (StatTotal, StatActive) giảm tương ứng.\n"
            f"Log ghi nhận: 'Homework deleted: [Tên bài tập]'."
        )
        output_data = f"Bản ghi bị xóa khỏi cơ sở dữ liệu. Log Serilog ghi nhận xóa."
        checklist = (
            f"[ ] Hộp thoại cảnh báo có hiển thị câu hỏi xác nhận và cảnh báo mất dữ liệu học sinh.\n"
            f"[ ] Bản ghi EventLog tương ứng không còn tồn tại trong SQLite DB.\n"
            f"[ ] Card bài tập biến mất khỏi UI và các số đếm tổng số bài cập nhật chính xác."
        )
        severity = "Critical"
        
    else:
        # Resend
        scenario = f"Kiểm tra Gửi lại bài tập (ResendCommand) cho học sinh online"
        pre = "Bài tập đã được giao trước đó. Hệ thống mạng của giáo viên đang ở trạng thái Broadcasting."
        input_data = f"Bấm nút 📤 (Gửi lại) trên Card bài tập"
        steps = (
            f"1. Định vị Card bài tập cần gửi.\n"
            f"2. Nhấn nút 📤 (Gửi lại cho HS).\n"
            f"3. Kiểm tra dòng lệnh TCP được gửi đi qua cổng mạng."
        )
        expected = (
            f"Hệ thống tuần tự hóa đối tượng bài tập thành JSON -> Base64.\n"
            f"Tạo lệnh command dạng 'CMD|HOMEWORK_JSON|[Base64]' và bắn ra socket TCP mạng LAN.\n"
            f"MessageBox hiển thị thông báo: '✅ Đã gửi lại BTVN cho HS!'.\n"
            f"Học sinh online nhận được thông báo nộp bài."
        )
        output_data = f"Chuỗi gửi qua mạng: CMD|HOMEWORK_JSON|[Base64_String]"
        checklist = (
            f"[ ] NetworkService.SendCommandAsync được gọi thành công.\n"
            f"[ ] Chuỗi Base64 giải mã ra đúng JSON của bài tập.\n"
            f"[ ] MessageBox hiển thị đúng tiêu đề: 'Thành công' và nội dung gửi thành công."
        )
        severity = "High"

    all_cases.append([
        tc_id, "Giáo viên", "Functional", "HomeworkViewModel.cs -> CRUD & Commands",
        scenario, pre, input_data, steps, expected, output_data, checklist, severity
    ])

# ==========================================
# MODULE 3: WORD IMPORT & PARSER TESTING (150 Cases)
# ==========================================
word_scenarios = [
    ("Đọc file mẫu chuẩn đầy đủ 5 trường thông tin", r"D:\Templates\MauChuan.docx", "Môn học: Toán\nTiêu đề: Kiểm tra số học\nNội dung: Làm bài tập 1,2 trang 5\nHạn nộp: 15/06/2026\nGhi chú: Không nộp muộn", "Tự động điền đầy đủ 5 trường vào Dialog nhập bài tập."),
    ("Đọc file Word không theo định dạng mẫu (chỉ có văn bản thuần túy)", r"D:\Templates\RawText.docx", "Văn bản bài tập tự do không có dấu hai chấm phân cách trường thông tin", "Dòng đầu làm Tiêu đề, các dòng còn lại làm Nội dung. Môn học mặc định: Toán, Hạn nộp mặc định: Today + 3 ngày."),
    ("Đọc file Word có trường hạn nộp bị để trống", r"D:\Templates\MissingDeadline.docx", "Môn học: Tin học\nTiêu đề: Lập trình Python\nNội dung: Viết script sinh testcase\nHạn nộp: \nGhi chú: Nhập tay hạn nộp", "Các trường khác điền bình thường, riêng Hạn nộp tự động gán mặc định Today + 3 ngày."),
    ("Đọc file Word có định dạng ngày tháng hạn nộp không hợp lệ (chuỗi chữ)", r"D:\Templates\InvalidDate.docx", "Môn học: Lý\nTiêu đề: Ôn tập Lý\nHạn nộp: Ngày mai nộp nha thầy\nNội dung: Trả lời câu hỏi 1-10", "Phân tích ngày thất bại (parsedDeadline = null). Ngày mặc định Today + 3 được gán."),
    ("Đọc file Word chứa bảng dữ liệu (Table)", r"D:\Templates\TableContent.docx", "Bảng chứa danh sách câu hỏi: Cột 1: STT, Cột 2: Câu hỏi", "Văn bản bảng được trích xuất cách nhau bằng dấu Tab và xuống dòng chuẩn xác."),
    ("Đọc file Word dung lượng lớn vượt giới hạn cho phép (12MB)", r"D:\Templates\LargeFile.docx", "File Word chứa nhiều hình ảnh, dung lượng 12MB", "Hệ thống ném ngoại lệ InvalidOperationException: 'File quá lớn (tối đa 10MB).'. Hiển thị MessageBox lỗi."),
    ("Đọc file sai định dạng (không phải đuôi mở rộng .docx)", r"D:\Templates\WrongExt.doc", "File word định dạng cũ (.doc)", "Hệ thống ném ngoại lệ: 'File không phải định dạng Word (.docx).'."),
    ("Đọc file không tồn tại trên đĩa cứng", r"D:\Templates\NotExist.docx", "Đường dẫn file ma", "Hệ thống ném ngoại lệ FileNotFoundException. Hiển thị thông báo lỗi.")
]

for i in range(150):
    case_idx = i + 1
    tc_id = f"TC_T_WRD_{case_idx:03d}"
    
    sc_info = word_scenarios[i % len(word_scenarios)]
    scenario = f"Kiểm tra import Word: {sc_info[0]} - Case {case_idx}"
    pre = "Giáo viên nhấn nút '📄 Nhập từ Word' và chọn file từ hộp thoại OpenFileDialog"
    input_data = f"Đường dẫn file: {sc_info[1]}, Nội dung giả lập: {sc_info[2]}"
    
    steps = (
        f"1. Nhấp nút '📄 Nhập từ Word'.\n"
        f"2. Tại OpenFileDialog, chọn file tại đường dẫn: {sc_info[1]}.\n"
        f"3. Quan sát các trường điền tự động trên dialog hiện lên."
    )
    
    expected = (
        f"Hệ thống gọi hàm ExtractTextFromWord để lấy text.\n"
        f"Gọi hàm ParseTemplateFields để bóc tách từ khóa.\n"
        f"Kết quả xử lý: {sc_info[3]}\n"
        f"Hiển thị Dialog nhập thông tin bài tập với dữ liệu trích xuất tương ứng."
    )
    
    output_data = "Đối tượng Dummy HomeworkItem được truyền vào ShowHomeworkDialog để bind lên giao diện."
    checklist = (
        f"[ ] Hàm ExtractTextFromWord bắt đúng ngoại lệ và trả ra thông báo lỗi thích hợp.\n"
        f"[ ] Việc bóc tách trường thông tin không phân biệt chữ hoa, chữ thường.\n"
        f"[ ] Nếu file có dung lượng lớn >10MB, chương trình hiển thị thông báo chặn lỗi ngay lập tức."
    )
    
    severity = "High" if "lớn" in sc_info[0] or "sai" in sc_info[0] else "Medium"
    
    all_cases.append([
        tc_id, "Giáo viên", "Word Parser", "HomeworkViewModel.cs -> ExtractTextFromWord & ParseTemplateFields",
        scenario, pre, input_data, steps, expected, output_data, checklist, severity
    ])

# ==========================================
# MODULE 4: STUDENT GUI & LOCALIZATION (150 Cases)
# ==========================================
gui_elements_s = [
    ("assignmentsList", "Danh sách các bài tập giáo viên giao", "ItemsControl", "Kiểm tra render danh sách card bài tập"),
    ("tabReceived", "Tab chứa '📥 Tài liệu nhận'", "Button / _activeTab='received'", "Nhấn vào tab tài liệu nhận"),
    ("tabSubmitted", "Tab chứa '📤 Bài đã nộp'", "Button / _activeTab='submitted'", "Nhấn vào tab bài đã nộp"),
    ("tabEditor", "Tab 'Soạn bài' trực tiếp", "Button / _activeTab='editor'", "Nhấn vào tab soạn bài"),
    ("editorPanel", "Khung soạn thảo bài làm trực tiếp", "WPF Grid (Visibility)", "Chuyển trạng thái soạn bài"),
    ("txtEditorTitle", "TextBox đặt tên bài viết", "TextBox", "Kiểm tra nhập liệu tên file bài làm"),
    ("txtEditorContent", "Khung nhập nội dung bài viết", "TextBox (AcceptsReturn)", "Nhập nội dung bài"),
    ("txtWordCount", "Nhãn đếm số từ và ký tự thời gian thực", "TextBlock.Text", "Kiểm tra gõ ký tự"),
    ("progressBar", "Thanh tiến độ tải file nộp bài", "ProgressBar", "Theo dõi trong quá trình nộp file"),
    ("txtStatus", "Dòng trạng thái nộp bài dưới đáy trang", "TextBlock.Text", "Quan sát trạng thái nộp bài"),
    ("dropOverlay", "Khung lớp phủ kéo thả file", "Border (Visibility)", "Kéo file từ bên ngoài vào cửa sổ ứng dụng"),
    ("txtReceivedCount", "Số đếm tài liệu nhận", "TextBlock.Text", "Kiểm tra số lượng hiển thị"),
    ("txtSubmittedCount", "Số đếm bài đã nộp thành công", "TextBlock.Text", "Kiểm tra số lượng hiển thị"),
    ("txtPendingCount", "Số đếm bài nộp trạng thái chờ gửi", "TextBlock.Text", "Kiểm tra số lượng hiển thị")
]

for i in range(150):
    element_key, element_name, req, action = gui_elements_s[i % len(gui_elements_s)]
    case_idx = i + 1
    tc_id = f"TC_S_GUI_{case_idx:03d}"
    
    resolutions = ["1920x1080", "1366x768", "1280x800", "DPI 100%", "DPI 125%"]
    res = resolutions[i % len(resolutions)]
    
    scenario = f"Kiểm tra hiển thị {element_name} ({element_key}) phía học sinh ở cấu hình {res}"
    if i % 5 == 1:
        scenario = f"Kiểm tra màu sắc trạng thái hiển thị của {element_name} khi chọn tương tác: {action}"
    elif i % 5 == 2:
        scenario = f"Kiểm tra lỗi dịch thuật, tính nhất quán ngôn ngữ tiếng Việt của nhãn {element_name}"
    elif i % 5 == 3:
        scenario = f"Kiểm tra thuộc tính ẩn/hiện của {element_name} khi chuyển đổi tab"
    elif i % 5 == 4:
        scenario = f"Kiểm tra độ phản hồi giao diện của {element_name} dưới các hành vi tương tác nhanh"

    steps = (
        f"1. Khởi động ứng dụng QA SmartClass ở chế độ Học sinh.\n"
        f"2. Điều hướng tới menu 'Học tập' -> chọn 'Bài tập / Nộp bài' (giao diện nộp bài).\n"
        f"3. Thay đổi thiết lập màn hình thành {res}.\n"
        f"4. Thực hiện hành động: {action} và quan sát {element_name}."
    )
    
    expected = (
        f"Phần tử {element_name} hiển thị chuẩn thiết kế, không tràn dòng, phông chữ Segoe UI chuẩn tiếng Việt.\n"
        f"Màu sắc đúng bản thiết kế (ví dụ: Tab active chuyển màu nền sáng tương ứng).\n"
        f"Độ phân giải {res} không làm biến dạng giao diện."
    )
    
    input_data = f"Cấu hình hiển thị màn hình: {res}, WPF Control: {element_key}, Thuộc tính: {req}"
    output_data = "Giao diện render đồ họa chính xác. Trạng thái điều khiển hợp lệ."
    
    checklist = (
        f"[ ] Nhãn hiển thị tiếng Việt chính xác, không tràn khung.\n"
        f"[ ] Phản hồi chuyển trạng thái (nhấp chuột, hover) xảy ra dưới 100ms.\n"
        f"[ ] Không xuất hiện lỗi vẽ lại layout (UI rendering glitch)."
    )
    
    severity = "Low" if i % 2 == 0 else "Medium"
    
    all_cases.append([
        tc_id, "Học sinh", "GUI", f"StudentSubmitPage.xaml -> {element_key} ({req})",
        scenario, "Học sinh đã kết nối mạng và đang hiển thị trang nộp bài",
        input_data, steps, expected, output_data, checklist, severity
    ])

# ==========================================
# MODULE 5: STUDENT FUNCTIONAL LOGIC (200 Cases)
# ==========================================
for i in range(200):
    case_idx = i + 1
    tc_id = f"TC_S_FUN_{case_idx:03d}"
    
    # Chia nhỏ kịch bản nghiệp vụ học sinh: 1. Countdown timer (1-50), 2. Resubmit (51-90), 3. Editor (91-140), 4. Nộp bài (141-200)
    if case_idx <= 50:
        time_rem = (i % 5) # 0, 1h, 5h, 25h, 48h
        scenario = f"Kiểm tra cập nhật đồng hồ đếm ngược và màu sắc tương ứng mức thời gian hạn nộp còn {time_rem} giờ"
        pre = "Học sinh đang xem danh sách bài tập. Ứng dụng chạy bộ đếm thời gian DispatcherTimer 1 giây/lần."
        input_data = f"Hạn nộp bài tập được thiết lập sao cho thời gian còn lại là {time_rem} giờ."
        steps = (
            f"1. Truy cập danh sách bài tập.\n"
            f"2. Xác định bài tập có thời gian hạn nộp tương đương {time_rem} giờ.\n"
            f"3. Quan sát text đếm ngược (CountdownStr) và màu sắc nhãn (DeadlineColor)."
        )
        if time_rem == 0:
            exp_text = "⚠ QUÁ HẠN NỘP BÀI!"
            exp_color = "#C62828 (Đỏ)"
        elif time_rem == 1:
            exp_text = "Còn X phút Y giây"
            exp_color = "#C62828 (Đỏ)"
        elif time_rem < 24:
            exp_text = "Còn X giờ Y phút"
            exp_color = "#E65100 (Cam)"
        else:
            exp_text = "Còn X ngày Y giờ"
            exp_color = "#2E7D32 (Xanh lá)"
            
        expected = (
            f"Cứ sau mỗi giây, text đếm ngược tự động cập nhật giảm dần mà không cần tải lại trang.\n"
            f"Nội dung text hiển thị dạng: '{exp_text}'.\n"
            f"Màu sắc chữ chuyển sang màu: {exp_color}."
        )
        output_data = f"CountdownStr cập nhật qua PropertyChanged; DeadlineColor đổi brush màu."
        checklist = (
            f"[ ] CountdownStr cập nhật giảm giây liên tục theo chu kỳ 1s.\n"
            f"[ ] DeadlineColor chuyển đúng hệ màu HEX quy định trong code AssignmentItem.cs.\n"
            f"[ ] Khi thời gian âm (quá hạn) lập tức gán text quá hạn và tắt nút nộp nếu không cho phép nộp lại."
        )
        severity = "High"
        
    elif case_idx <= 90:
        allow = (i % 2 == 0)
        submitted = (i % 3 != 0)
        scenario = f"Kiểm tra nút bấm nộp bài trạng thái: Đã nộp={submitted}, Cho phép nộp lại={allow}"
        pre = "Học sinh có bài tập hiển thị trên danh sách."
        input_data = f"Cấu hình bài tập: IsSubmitted={submitted}, AllowResubmit={allow}"
        steps = (
            f"1. Xác định card bài tập được cấu hình đầu vào.\n"
            f"2. Quan sát Text hiển thị trên nút nộp bài (ButtonText).\n"
            f"3. Quan sát màu nền nút (ButtonBg).\n"
            f"4. Kiểm tra nút có thể click được không (IsEnabled)."
        )
        if not submitted:
            btn_text = "📤 Nộp bài"
            btn_color = "#1976D2 (Xanh dương)"
            enabled = "True"
        else:
            if allow:
                btn_text = "📝 Nộp lại"
                btn_color = "#F59E0B (Hổ phách)"
                enabled = "True"
            else:
                btn_text = "✅ Đã nộp"
                btn_color = "#4CAF50 (Xanh lá)"
                enabled = "False"
                
        expected = (
            f"Nhãn nút hiển thị chính xác là '{btn_text}'.\n"
            f"Màu nền nút chuyển tương ứng sang mã màu {btn_color}.\n"
            f"Trạng thái tương tác vật lý IsEnabled của nút nộp bài = {enabled}."
        )
        output_data = f"Trạng thái bind giao diện: ButtonText, ButtonBg, IsEnabled."
        checklist = (
            f"[ ] Nhãn hiển thị khớp hoàn toàn với cấu hình logic nộp/nộp lại.\n"
            f"[ ] Trạng thái bật/tắt (IsEnabled) hoạt động chính xác để tránh nộp đè khi không được phép.\n"
            f"[ ] Màu sắc trực quan phân biệt rõ ràng 3 trạng thái."
        )
        severity = "High"
        
    elif case_idx <= 140:
        content_len = (i % 5) * 50 # 0, 50, 100, 150, 200 từ
        title_val = f"BaiLamToan_Case_{case_idx}"
        scenario = f"Kiểm tra soạn bài làm trực tiếp (Inline Editor) độ dài {content_len} từ, tên bài là '{title_val}'"
        pre = "Học sinh chuyển sang tab 'Soạn bài' và nhập nội dung"
        input_data = f"Tiêu đề: '{title_val}', Nội dung bài viết có độ dài {content_len} từ."
        steps = (
            f"1. Chọn tab '📝 Soạn bài'.\n"
            f"2. Điền tên bài vào TextBox tiêu đề: '{title_val}'.\n"
            f"3. Điền văn bản nội dung bài làm có độ dài {content_len} từ vào TextBox nội dung.\n"
            f"4. Nhấn nút 'Nộp bài viết'."
        )
        if content_len == 0:
            expected = (
                "Hệ thống phát hiện nội dung rỗng hoặc văn bản mặc định.\n"
                "Hiển thị cảnh báo MessageBox: 'Vui lòng nhập nội dung bài làm!'.\n"
                "Ngăn chặn việc nộp bài và giữ nguyên giao diện soạn thảo."
            )
        else:
            expected = (
                f"Hệ thống đếm từ thời gian thực, cập nhật hiển thị chính xác ở nhãn txtWordCount.\n"
                f"Tạo file tạm thời định dạng .txt tại đường dẫn thư mục tạm của hệ thống: QASmartClass_Submit\\{title_val}_[date_time].txt.\n"
                f"Lưu nội dung bài viết vào file tạm này.\n"
                f"Gọi hàm SubmitFilesAsync để chuyển file đi. Chuyển hướng học sinh về tab 'Bài đã nộp' sau khi nộp thành công."
            )
        output_data = f"Tạo file .txt thành công trên đĩa. Gọi cơ chế gửi file đi."
        checklist = (
            f"[ ] Bộ đếm từ txtWordCount đếm chính xác số lượng từ phân cách bởi dấu khoảng trắng.\n"
            f"[ ] File tạm thời .txt được ghi thành công và không bị khóa tiến trình sau khi ghi.\n"
            f"[ ] Nội dung file .txt khớp chính xác 100% nội dung đã soạn thảo trên màn hình."
        )
        severity = "High"
        
    else:
        file_count = (i % 3) + 1 # 1, 2, 3 file nộp cùng lúc
        scenario = f"Kiểm tra nộp cùng lúc {file_count} file bài làm bằng nút 'Nộp file' qua OpenFileDialog"
        pre = "Học sinh click nút '📤 Nộp file' để chọn bài nộp"
        input_data = f"Chọn {file_count} file từ máy tính (ví dụ: BaiLam1.docx, HinhAnh.png)."
        steps = (
            f"1. Click nút '📤 Nộp file'.\n"
            f"2. Tại hộp thoại chọn file, tích chọn {file_count} file bài làm.\n"
            f"3. Click Open / OK để bắt đầu nộp."
        )
        expected = (
            f"Hệ thống gọi hàm SubmitFilesAsync truyền mảng chứa {file_count} đường dẫn file.\n"
            f"Thanh tiến độ progressBar nhảy tăng dần từ 0% đến 100%.\n"
            f"Nhãn txtStatus cập nhật nội dung tương ứng tên file đang nộp.\n"
            f"Khi hoàn tất, xuất hiện MessageBox thông báo 'Nộp thành công {file_count} file!'.\n"
            f"Bảng FileTransfers và EventLogs ghi nhận bản ghi nộp bài thành công của học sinh."
        )
        output_data = f"Bản ghi FileTransferRecord ghi nhận trạng thái 'Completed', EventLog ghi nhận 'FILE'."
        checklist = (
            f"[ ] Bản ghi trong bảng FileTransfers được tạo với Direction='StudentToTeacher'.\n"
            f"[ ] Trạng thái nộp bài được ghi nhận chính xác (Completed nếu gửi thành công).\n"
            f"[ ] ProgressBar hiển thị mượt mà không bị treo đơ giao diện (sử dụng async/await)."
        )
        severity = "Critical"

    all_cases.append([
        tc_id, "Học sinh", "Functional", "StudentSubmitPage.xaml.cs -> Submit Logic",
        scenario, pre, input_data, steps, expected, output_data, checklist, severity
    ])

# ==========================================
# MODULE 6: NETWORK TCP & INTEGRATION SYNC (100 Cases)
# ==========================================
for i in range(100):
    case_idx = i + 1
    tc_id = f"TC_NET_{case_idx:03d}"
    
    # Các kịch bản mạng: 1. TCP Teacher Broadcast (1-40), 2. TCP Student Upload (41-70), 3. Offline Local Copy Fallback (71-100)
    if case_idx <= 40:
        scenario = f"Kiểm tra Giáo viên phát BTVN qua TCP mạng LAN - Tự động đồng bộ Command - Case {case_idx}"
        pre = "Hệ thống mạng LAN kết nối bình thường, NetworkService.IsBroadcasting = True"
        input_data = "Thông tin bài tập về nhà định dạng JSON -> Chuyển mã Base64"
        steps = (
            f"1. Giáo viên thực hiện giao bài tập mới.\n"
            f"2. Hệ thống gọi hàm RaiseLocalCommand và NetworkService.SendCommandAsync.\n"
            f"3. Bắt gói tin TCP gửi đi và xác minh cấu trúc chuỗi lệnh gửi."
        )
        expected = (
            f"Chuỗi lệnh gửi đi qua TCP socket bắt buộc phải khớp chuẩn định dạng: 'CMD|HOMEWORK_JSON|[Base64]'.\n"
            f"Base64 khi giải mã phải khôi phục chính xác 100% chuỗi JSON chứa các thuộc tính: Title, Description, Subject, Deadline của bài tập."
        )
        output_data = "Dòng lệnh TCP Command String được phát tới tất cả các địa chỉ IP học sinh trong lớp."
        checklist = (
            f"[ ] Dữ liệu Base64 không chứa ký tự xuống dòng gây lỗi phân tích dòng lệnh mạng.\n"
            f"[ ] JSON giải mã ra đúng JSON của bài tập.\n"
            f"[ ] Hàm gửi mạng được bọc trong try-catch không gây crash phần mềm giáo viên khi mất kết nối đột ngột."
        )
        severity = "High"
        
    elif case_idx <= 70:
        percent_val = (i % 10) * 10 + 10 # 10%, 20%, ..., 100%
        scenario = f"Kiểm tra đồng bộ tiến độ tải file nộp bài phía Học sinh qua TCP ở mức {percent_val}%"
        pre = "Học sinh đang tải file nộp lên máy giáo viên qua TCP."
        input_data = f"Sự kiện SubmitProgress bắn ra tiến độ đạt {percent_val}%"
        steps = (
            f"1. Học sinh nộp file bài làm dung lượng lớn để theo dõi tiến độ.\n"
            f"2. Hàm nộp gửi file và định kỳ kích hoạt sự kiện OnSubmitProgress với tham số percent={percent_val}.\n"
            f"3. Quan sát sự thay đổi giá trị trên thanh tiến độ progressBar và nhãn txtStatus."
        )
        expected = (
            f"Giá trị thuộc tính Value của progressBar cập nhật chính xác thành {percent_val}.\n"
            f"Văn bản trên nhãn txtStatus hiển thị dạng: '📤 Đang nộp... {percent_val}%'.\n"
            f"Giao diện phản hồi trực quan mượt mà."
        )
        output_data = f"progressBar.Value = {percent_val}; txtStatus.Text cập nhật."
        checklist = (
            f"[ ] Tiến độ nộp bài cập nhật đồng bộ thời gian thực theo sự kiện mạng LAN.\n"
            f"[ ] Tiến trình chạy ngầm (background thread) không khóa luồng UI chính.\n"
            f"[ ] Khi đạt 100% chuyển đổi trạng thái thành công lập tức."
        )
        severity = "Medium"
        
    else:
        scenario = f"Kiểm tra cơ chế dự phòng cục bộ (Local Fallback Copy) khi mất mạng LAN trong quá trình nộp bài"
        pre = "Học sinh nộp bài nhưng kết nối TCP mạng LAN bị lỗi (client.IsConnected = False)"
        input_data = "File bài làm: 'BaiTap_Hoa.pdf' nộp cho giáo viên"
        steps = (
            f"1. Ngắt kết nối mạng của máy học sinh.\n"
            f"2. Bấm nộp file 'BaiTap_Hoa.pdf'.\n"
            f"3. Hệ thống cố gắng gửi TCP thất bại.\n"
            f"4. Kiểm tra thư mục Documents\\Submissions và bảng FileTransfers."
        )
        expected = (
            f"Hệ thống phát hiện lỗi TCP, tự động nhảy vào khối xử lý dự phòng (Local Copy Fallback).\n"
            f"Thực hiện chuẩn hóa tên file: '[Mã_HS]_[Tên_HS]_BaiTap_Hoa.pdf'.\n"
            f"Sao chép file vào thư mục Documents\\Submissions\\.\n"
            f"Ghi nhận bản ghi FileTransferRecord vào SQLite DB với trạng thái Status='Pending' để chờ đồng bộ sau.\n"
            f"MessageBox hiển thị thông báo nộp chờ (hoặc nộp thành công cục bộ)."
        )
        output_data = "File được sao chép cục bộ với tên đã chuẩn hóa. Bản ghi SQLite ghi nhận trạng thái 'Pending'."
        checklist = (
            f"[ ] File được copy thành công vào thư mục Submissions cục bộ.\n"
            f"[ ] Tên file đầu ra được chuẩn hóa chính xác không chứa ký tự tiếng Việt có dấu phức tạp gây lỗi hệ điều hành.\n"
            f"[ ] Trạng thái trong DB lưu là 'Pending' chứ không phải 'Completed'."
        )
        severity = "Critical"

    all_cases.append([
        tc_id, "Tích hợp mạng", "Integration", "StudentSubmitPage.xaml.cs & Network Services",
        scenario, pre, input_data, steps, expected, output_data, checklist, severity
    ])

# ==========================================
# GHI DỮ LIỆU RA FILE CSV
# ==========================================
try:
    with open(output_path, mode="w", encoding="utf-8-sig", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(headers)
        writer.writerows(all_cases)
    print(f"SUCCESS: Generated {len(all_cases)} test cases in {output_path}")
    sys.exit(0)
except Exception as ex:
    print(f"ERROR: {str(ex)}")
    sys.exit(1)
