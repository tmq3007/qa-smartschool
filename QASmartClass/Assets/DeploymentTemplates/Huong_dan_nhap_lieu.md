# 📋 HƯỚNG DẪN CHUẨN BỊ DỮ LIỆU ĐẦU VÀO TRIỂN KHAI (v4.1)

Tài liệu này cung cấp hướng dẫn chi tiết về các tệp dữ liệu đầu vào cần thiết để nhập (import) vào hệ thống **QA SmartClass** khi bắt đầu triển khai tại một trường học. Dữ liệu hiện tại đã được nâng cấp đồng bộ theo **Quy chuẩn v4.1**, hỗ trợ đọc tệp bảng tính Excel (.xlsx) trực tiếp, mã hóa bảo mật nâng cao và các kiểm tra ràng buộc nghiệp vụ sư phạm chặt chẽ.

---

## 🗺️ Tổng Quan Các File Dữ Liệu Đầu Vào

Hệ thống **QA SmartClass** quản lý dữ liệu thông qua các tệp tin Excel (.xlsx), CSV, Word (.docx) và JSON. Khi triển khai mới cho một trường học, bộ phận kỹ thuật hoặc quản trị viên nhà trường cần chuẩn bị **6 nhóm dữ liệu** sau:

| STT | Loại dữ liệu | Định dạng hỗ trợ | Thư mục dữ liệu mẫu | Giao Diện Nhập Liệu |
| :--- | :--- | :--- | :--- | :--- |
| 1 | **Học sinh toàn trường** | `.xlsx` / `.csv` | `Assets/DeploymentTemplates/danh_sach_hoc_sinh_toan_truong.csv` | Admin Dashboard $\rightarrow$ Học Sinh |
| 2 | **Học sinh theo Phòng Lab** | `.xlsx` / `.csv` | `Assets/DeploymentTemplates/danh_sach_hoc_sinh_phong_lab.csv` | Teacher Client $\rightarrow$ Thiết lập Phòng Lab |
| 3 | **Danh sách Giáo viên** | `.xlsx` / `.csv` | `Assets/DeploymentTemplates/danh_sach_giao_vien.csv` | Admin Dashboard $\rightarrow$ Giáo Viên |
| 4 | **Thời khóa biểu** | `.xlsx` / `.csv` | `Assets/DeploymentTemplates/thoi_khoa_bieu_mau.csv` | Teacher Client $\rightarrow$ Thời Khóa Biểu |
| 5 | **Bài giảng Word mẫu** | `.docx` | `Assets/DeploymentTemplates/bai_giang_mau.docx` | Teacher Client $\rightarrow$ Soạn Bài $\rightarrow$ Nhập Word |
| 6 | **Ngân hàng câu hỏi** | `.json` | `Assets/DeploymentTemplates/ngan_hang_cau_hoi_mau.json` | Teacher Client $\rightarrow$ Ngân Hàng Câu Hỏi |

---

## 📂 Chi Tiết Định Dạng & Ràng Buộc Kỹ Thuật (Chuẩn v4.1)

### 1. Danh sách Học sinh toàn trường (Global Students)
File này giúp quản trị viên đưa toàn bộ cơ sở dữ liệu học sinh của trường vào hệ thống trước khi gán lớp học cụ thể.

*   **Cột tiêu đề (Header):** `MãHS,HọTên,Lớp,Trường`
*   **Ràng buộc kỹ thuật v4.1:**
    *   **Loại bỏ trùng lặp:** Hệ thống so khớp theo `StudentCode` (Mã HS). Nếu trùng mã học sinh đã có trên hệ thống, dòng đó sẽ bị bỏ qua (không ghi đè trùng lặp).
    *   **Bảo mật mật khẩu:** Học sinh mới được thêm vào sẽ được cấp mật khẩu mặc định tự động có dạng `Hs@` ghép với 4 ký tự cuối của mã học sinh (ví dụ: học sinh có mã `HS001` sẽ có mật khẩu mặc định là `Hs@HS001`). Mật khẩu này được băm bằng thuật toán **HMACSHA512** và lưu dưới dạng chuỗi bảo mật `"salt:hash"` trong cơ sở dữ liệu.
*   **Thao tác nhập:** Vào Admin Dashboard $\rightarrow$ chọn mục **Học Sinh** $\rightarrow$ bấm **Import Excel** hoặc **Nhập CSV** $\rightarrow$ Chọn file dữ liệu mẫu đã chuẩn bị.

---

### 2. Danh sách Học sinh theo Phòng Lab & Sĩ số lớp (Class Roster Students)
Sử dụng tại phòng máy để thiết lập sơ đồ liên kết máy trạm của học sinh với địa chỉ IP nội bộ, phục vụ việc giám sát Split Screen và điều khiển tương tác.

*   **Cột tiêu đề (Header):** `Họ tên,Mã HS,Tên PC,Địa chỉ IP`
*   **Ràng buộc kỹ thuật v4.1:**
    *   **Sức chứa tối đa (Classroom Capacity Constraint):** Trước khi thêm học sinh vào một lớp học (`ClassRoster`), hệ thống sẽ đối chiếu tổng sĩ số sau khi nhập với giới hạn sức chứa tối đa của phòng học (`MaxStudents` trong cấu hình phòng học). Nếu vượt quá giới hạn sức chứa, toàn bộ tiến trình nhập sẽ tự động dừng lại, thực hiện **Rollback** dữ liệu để tránh lỗi bất đồng bộ và hiển thị thông báo lỗi rõ ràng: *"Không thể nhập dữ liệu. Tổng sĩ số sau khi nhập vượt quá giới hạn tối đa của phòng học"*.
*   **Thao tác nhập:** Vào Teacher Hub $\rightarrow$ chọn **Thiết lập mạng / Sơ đồ máy trạm (Workstations)** $\rightarrow$ bấm **Nhập danh sách máy (Import CSV/Excel)**.

---

### 3. Danh sách Giáo viên & Khử trùng dữ liệu (Teacher Profiles)
Khởi tạo hồ sơ cán bộ giảng dạy toàn trường và cấp tài khoản hoạt động ban đầu.

*   **Cột tiêu đề (Header):** `Họ tên,Học vị,Bộ môn,Trường,Số điện thoại,Email,Ghi chú`
*   **Ràng buộc kỹ thuật v4.1:**
    *   **Cơ chế khử trùng (Deduplication):** Trước khi thêm giáo viên mới, hệ thống tự động so khớp `Email` hoặc `Số điện thoại`. Nếu giáo viên đã tồn tại trong cơ sở dữ liệu, hệ thống sẽ tiến hành **Cập nhật (Update)** thông tin mới (như học vị, bộ môn, ghi chú) thay vì chèn dòng trùng lặp, bảo đảm tính nhất quán dữ liệu.
    *   **Mã hóa bảo mật:** Mật khẩu mặc định được tự động sinh và băm bảo mật bằng thuật toán **HMACSHA512** với định dạng lưu trữ `"salt:hash"`.
*   **Thao tác nhập:** Đăng nhập Admin $\rightarrow$ chọn **Hồ sơ giáo viên (Teachers)** $\rightarrow$ bấm **Nhập Excel/CSV**.

---

### 4. Thời khóa biểu của Lớp học (Class Timetable)
Cấu hình lịch học trong tuần cho phòng Lab/lớp học để tự động đồng bộ bài học phù hợp theo tiết dạy thực tế.

*   **Cột tiêu đề (Header):** `Thứ,Tiết,Môn,Phòng,Giáo viên,Ghi chú`
*   **Ràng buộc kỹ thuật v4.1:**
    *   **Phân tách cột an toàn (RFC 4180):** Thay thế hoàn toàn cơ chế chia cột bằng dấu phẩy thông thường. Chấp nhận các ô dữ liệu chứa dấu phẩy lồng trong dấu ngoặc kép (ví dụ: `"Toán, Lý, Hóa"` ở cột Ghi chú) mà không bị lệch cột.
    *   **Chuẩn hóa Thứ/Tiết dạy nghiêm ngặt:** Cột `Thứ` bắt buộc phải thuộc dải từ `Thứ 2` đến `Thứ 7` (hoặc các viết tắt chuẩn `t2` đến `t7`). Cột `Tiết` bắt buộc phải từ `Tiết 1` đến `Tiết 10` (hoặc từ `1` đến `10`). Các dòng không đúng chuẩn (ví dụ: `Tiết 11` hoặc sai chính tả) sẽ bị bỏ qua và ghi chi tiết cảnh báo (Warning log) ra bảng hiển thị lỗi trên UI để giáo viên sửa lại file.
*   **Thao tác nhập:** Vào Teacher Client $\rightarrow$ tab **Thời khóa biểu (Timetable)** $\rightarrow$ chọn lớp và bấm **Nhập TKB (Import Excel/CSV)**.

---

### 5. File giáo án Word bài giảng mẫu (Lesson Word Templates)
Phương thức soạn giáo án nhanh bằng Microsoft Word offline để giáo viên nạp nhanh cấu trúc bài học.

*   **Định dạng:** Microsoft Word (`.docx`).
*   **Quy định:** Soạn thảo nội dung đúng dưới các thẻ phân khu viết hoa như `[MỤC TIÊU BÀI HỌC]`, `[KIẾN THỨC TRỌNG TÂM]`,... sau đó lưu lại.
*   **Thao tác nhập:** Vào Teacher Client $\rightarrow$ phân hệ **Soạn bài giảng (Lesson Editor)** $\rightarrow$ bấm **Import Word** trên thanh công cụ.

---

### 6. Ngân hàng câu hỏi trắc nghiệm (Question Bank JSON)
Nạp ngân hàng câu hỏi kiểm tra có sẵn để tạo các đề thi nhanh trên hệ thống phòng Lab.

*   **Định dạng:** JSON (`.json`).
*   **Thao tác nhập:** Vào Teacher Client $\rightarrow$ phân hệ **Ngân hàng câu hỏi / Quản lý Quiz** $\rightarrow$ bấm **Export/Import ngân hàng** $\rightarrow$ chọn **Import (No)** $\rightarrow$ Chọn file JSON.

---

## 🛠️ Hướng dẫn cách chuẩn bị tệp tin bảng tính Excel (.xlsx)

Để chuẩn bị dữ liệu bằng Microsoft Excel, IT nhà trường và giáo viên thực hiện như sau:
1. Mở Excel, tạo các cột có tiêu đề (Header) chính xác theo quy định của từng phân hệ ở trên.
2. Điền dữ liệu vào các dòng bên dưới dòng tiêu đề (Dòng 1).
3. Đảm bảo không để dòng trống xen kẽ ở giữa vùng dữ liệu.
4. Chọn **Save As** $\rightarrow$ Lưu dưới định dạng **Excel Workbook (*.xlsx)** thông thường. Hệ thống sẽ tự động sử dụng thư viện EPPlus tích hợp để phân tích tệp một cách chính xác mà không đòi hỏi cài đặt MS Office trên máy tính đích.
