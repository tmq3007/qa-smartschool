# Kế hoạch Nâng cấp & Cải tiến Phân hệ Nhân viên & IT-Admin - Bản Cập nhật Cấu hình Master & Mở rộng Test Case
**Bộ quy chuẩn thiết kế sư phạm và ràng buộc kỹ thuật QA SmartClass v4.1**  
*Ngày cập nhật kế hoạch: 30 tháng 06 năm 2026*  

---

## ═══ MỤC TIÊU ═══
Hoàn thiện các chức năng quản trị còn thiếu dành cho nhân viên nhà trường, đặc biệt là nhân viên **IT-Admin** trên trang **Cấu hình Hệ thống (SystemSettingsView)** và trang **Sao lưu Dữ liệu (BackupRestoreView)**. Đảm bảo các cài đặt được thiết kế dưới dạng cấu hình cấp trường (Master Configurations) để nhà trường tự quyết định phương án vận hành dựa trên điều kiện thực tế, đồng thời mở rộng bộ checksheet kiểm thử bao phủ toàn bộ các kịch bản lỗi hệ thống.

---

## ═══ CÁC TÙY CHỌN CẤU HÌNH HỆ THỐNG MỚI (MASTER CONFIGURATIONS) ═══

IT-Admin có thể trực tiếp cấu hình các tham số vận hành hệ thống thông qua các Master settings được lưu trong bảng `SystemSettings`:

| ID Cấu hình (Key) | Mô tả cấu hình | Giá trị mặc định | Giá trị tùy chọn |
| :--- | :--- | :--- | :--- |
| `IT_Security_PwdHashLevel` | Chế độ xác thực và băm mật khẩu/PIN hệ thống | `"PBKDF2"` | `"PBKDF2"` (Chế độ băm an toàn cao)<br>`"PlaintextFallback"` (Cho phép mật khẩu thô cho máy chủ yếu) |
| `IT_Database_OverLimitAction` | Hành động khi kích thước cơ sở dữ liệu vượt quá giới hạn | `"WarnOnly"` | `"WarnOnly"` (Chỉ cảnh báo đỏ trên Dashboard)<br>`"AutoClean"` (Tự động kích hoạt dọn dẹp logs cũ hơn 30 ngày) |
| `IT_Backup_StorageTarget` | Phương thức đồng bộ và lưu trữ file sao lưu | `"LocalOnly"` | `"LocalOnly"` (Chỉ lưu file ZIP tại máy chủ LAN)<br>`"CloudSync"` (Đòng bộ song song lên cloud của Phòng/Sở) |

---

## ═══ CHI TIẾT KẾ HOẠCH NÂNG CẤP & CẢI TIẾN ═══

### 1. Quản trị trực quan Cấu hình Cổng Phụ huynh (Parent Portal Admin Panel)
*   **Yêu cầu:** Thêm một khu vực cấu hình chuyên biệt cho Cổng Phụ huynh trên giao diện `SystemSettingsView`.
*   **Các tham số tích hợp:**
    *   `ParentPortal_AuthSecureLevel`: ComboBox chọn `"Đơn giản (Chỉ SĐT)"` hoặc `"Bảo mật cao (Mật khẩu/PIN)"`.
    *   `ParentPortal_MessageRouting`: ComboBox chọn `"Định tuyến thông minh (GVCN)"` hoặc `"Hộp thư chung (GV)"`.
    *   `ParentPortal_AttendanceCase`: ComboBox chọn `"Không phân biệt hoa/thường"` hoặc `"Phân biệt tuyệt đối"`.

### 2. Cài đặt Màu chủ đạo tương tác thực tế (Interactive Theme Customization)
*   **Yêu cầu:** Chuyển đổi các vòng tròn màu Mock trong giao diện cũ thành nút bấm chọn màu hoạt động thực tế. Cho phép IT-Admin bấm chọn màu chủ đạo và cập nhật tức thì tài nguyên hệ thống (`Application.Current.Resources["PrimaryColor"]`), đổi giao diện ứng dụng thời gian thực mà không cần khởi động lại.

### 3. Cảnh báo dung lượng Cơ sở dữ liệu và Cài đặt lịch sao lưu tự động
*   **Yêu cầu:** 
    *   Cho phép cấu hình giờ chạy sao lưu tự động hàng ngày (mặc định `23:00`) và ngưỡng dung lượng file cơ sở dữ liệu (ví dụ: `500MB`).
    *   Tự động kiểm tra dung lượng file SQLite (`smartclass.db`) tại thời điểm khởi động ứng dụng. 
    *   Nếu vượt quá ngưỡng cấu hình và cấu hình hành động là `"WarnOnly"`: hiển thị Banner cảnh báo đỏ nổi bật trên Dashboard của IT-Admin.
    *   Nếu vượt quá ngưỡng cấu hình và cấu hình hành động là `"AutoClean"`: tự động gọi `DataRetentionService` dọn dẹp dữ liệu để thu nhỏ file ngay lập tức.

---

## ═══ ĐỀ XUẤT THAY ĐỔI MÃ NGUỒN (PROPOSED CHANGES) ═══

### 1. Phân hệ Giao diện Quản trị Cấu hình (IT-Admin Settings)

#### [MODIFY] [SystemSettingsView.xaml](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/SystemSettingsView.xaml)
*   Tái thiết kế Grid hiển thị:
    *   Cột bên trái: Thêm một vùng chứa `Groupbox` mang tên **"Cấu hình Cổng Phụ huynh (Parent Portal)"**. Tích hợp 3 ComboBox lựa chọn cho các cài đặt bảo mật đăng nhập, định tuyến tin nhắn và điểm danh.
    *   Cột bên phải: Nâng cấp khu vực chọn **Theme Màu chủ đạo**:
        *   Thêm thuộc tính `MouseLeftButtonDown="ThemeColor_Click"` và thuộc tính `Cursor="Hand"` cho các vòng tròn màu.
        *   Bổ sung TextBox `txtThemeHex` để nhập mã màu Hex tùy chọn thủ công kèm nhãn hướng dẫn.
    *   Thêm các ComboBox/TextBox cho các cấu hình Master mới:
        *   `cmbPwdHashLevel` (PBKDF2 / PlaintextFallback).
        *   `cmbOverLimitAction` (WarnOnly / AutoClean).
        *   `cmbStorageTarget` (LocalOnly / CloudSync).
        *   TextBox `txtBackupTime` và `txtDbSizeLimit`.

#### [MODIFY] [SystemSettingsView.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Leadership/Views/SystemSettingsView.xaml.cs)
*   Nâng cấp hàm `LoadSettingsFromDb()` để nạp thêm các cài đặt:
    *   `ParentPortal_AuthSecureLevel`, `ParentPortal_MessageRouting`, `ParentPortal_AttendanceCase`.
    *   `IT_Security_PwdHashLevel`, `IT_Database_OverLimitAction`, `IT_Backup_StorageTarget`.
    *   `PrimaryColor` (nạp màu đang chọn lên TextBox Hex và làm nổi bật vòng tròn màu tương ứng).
    *   `BackupTime` (Giờ sao lưu), `DbSizeLimit` (Giới hạn CSDL).
*   Nâng cấp hàm `BtnSaveSettings_Click()`:
    *   Kiểm tra định dạng dữ liệu đầu vào (Validate):
        *   Mã màu Hex phải khớp định dạng: `^#[0-9A-Fa-f]{6}$` (ví dụ: `#1976D2`).
        *   Giờ sao lưu phải đúng định dạng `HH:mm` (từ `00:00` đến `23:59`).
        *   Giới hạn CSDL phải là số nguyên dương lớn hơn 10 (MB).
        *   Địa chỉ IP Address phải khớp mẫu kiểm tra IPv4.
    *   Lưu tất cả cấu hình hợp lệ vào DB và gọi `ApplyThemeFromDb()` để cập nhật trực quan toàn bộ hệ thống.
*   Thêm sự kiện `ThemeColor_Click` để tự động điền mã màu Hex vào TextBox khi người dùng bấm vào vòng tròn màu mẫu.

### 2. Phân hệ Cảnh báo & Tự động sao lưu

#### [NEW] [DbSizeCheckService.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Services/DbSizeCheckService.cs)
*   Tạo lớp dịch vụ kiểm tra dung lượng SQLite:
    *   Đọc giới hạn dung lượng từ cấu hình `DbSizeLimit` (mặc định 500MB).
    *   Lấy kích thước vật lý của tệp `smartclass.db`.
    *   Nếu vượt quá giới hạn, trả về kết quả cảnh báo kèm dung lượng thực tế để giao diện hiển thị thông báo.

#### [MODIFY] [StaffDashboardWindow.xaml.cs](file:///d:/JOB/QA%20SmartClass%20-062026/QASmartClass/Staff/Views/StaffDashboardWindow.xaml.cs)
*   Trong hàm tải giao diện, gọi `DbSizeCheckService` kiểm tra kích thước CSDL.
*   Nếu vai trò là `"Admin"` và tệp tin vượt quá ngưỡng:
    *   Đọc `IT_Database_OverLimitAction`.
    *   Nếu là `"WarnOnly"`: hiển thị một Banner cảnh báo màu đỏ sư phạm nổi bật ở đầu trang Tổng quan: *"⚠️ Cảnh báo IT: Cơ sở dữ liệu hiện tại đạt [X] MB, vượt quá ngưỡng cho phép [Y] MB. Vui lòng chạy dọn dẹp dữ liệu hoặc sao lưu!"*.
    *   Nếu là `"AutoClean"`: Tự động chạy ngầm `DataRetentionService.CleanOldData(90, 30)` để xóa logs cũ thu nhỏ database, đồng thời ghi log thông báo đã dọn dẹp thành công.

---

## ═══ BỘ CHECKSHEET KIỂM THỬ TOÀN DIỆN (COMPREHENSIVE TEST CASES) ═══

Coder phải thực hiện kiểm thử và tích đầy đủ các kiểm tra sau để nghiệm thu:

### 1. Kiểm tra Cấu hình Cổng Phụ huynh (Parent Portal Settings)
*   `[ ]` **TC_ADMIN_PP_01 (Lưu cấu hình):** Thay đổi các giá trị của ComboBox Cổng Phụ huynh trên giao diện cài đặt và bấm Lưu. Xác nhận trong bảng `SystemSettings` của SQLite lưu đúng các key: `ParentPortal_AuthSecureLevel`, `ParentPortal_MessageRouting`, `ParentPortal_AttendanceCase`.
*   `[ ]` **TC_ADMIN_PP_02 (Đọc cấu hình):** Khởi động lại trang `SystemSettingsView`. Kiểm tra xem các ComboBox Cổng Phụ huynh có hiển thị chính xác các giá trị đã lưu hay không.

### 2. Kiểm tra Đổi Theme màu sắc (Theme Customization & Contrast Validation)
*   `[ ]` **TC_ADMIN_THEME_01 (Click bảng màu):** Bấm vào vòng tròn màu xanh lá cây trên bảng màu. Kết quả mong đợi: TextBox Hex tự động điền mã màu `#4CAF50`.
*   `[ ]` **TC_ADMIN_THEME_02 (Định dạng màu sai):** Nhập mã màu Hex không hợp lệ (ví dụ: `1976D2`, `#XYZ123`). Bấm lưu => Hiển thị lỗi cảnh báo định dạng màu sai, không ghi đè cấu hình cũ.
*   `[ ]` **TC_ADMIN_THEME_03 (Kiểm tra độ sáng phản cảm):** Nhập màu quá sáng (ví dụ: `#FFFFFF` hoặc `#FFFF55`). Bấm lưu => Hệ thống hiển thị cảnh báo từ chối áp dụng màu do độ tương phản thấp (vi phạm v4.1).
*   `[ ]` **TC_ADMIN_THEME_04 (Áp dụng trực tiếp):** Nhập màu `#9C27B0` (màu tím) hợp lệ và lưu. Xác nhận toàn bộ thanh menu chính và các tiêu đề của ứng dụng chuyển sang màu tím ngay lập tức mà không cần khởi động lại.

### 3. Kiểm tra các tham số bảo mật & Cảnh báo sao lưu (Master Security & Auto-Backup Checks)
*   `[ ]` **TC_ADMIN_SEC_01 (Cấu hình PBKDF2):** Đặt `IT_Security_PwdHashLevel` = `"PBKDF2"`. Thử tạo mật khẩu tài khoản nhân viên mới => Xác nhận mật khẩu được lưu dưới dạng băm phức tạp `pbkdf2:100000:...`.
*   `[ ]` **TC_ADMIN_SEC_02 (Plaintext Fallback):** Đặt `IT_Security_PwdHashLevel` = `"PlaintextFallback"`. Kiểm tra hệ thống có cho phép đăng nhập bằng các mật khẩu cũ chưa băm hay không.
*   `[ ]` **TC_ADMIN_BK_01 (Định dạng thời gian):** Nhập giờ sao lưu sai định dạng (ví dụ: `25:00`, `9:30 PM`, `abc`). Kết quả mong đợi: Hệ thống báo lỗi và từ chối lưu.
*   `[ ]` **TC_ADMIN_BK_02 (Ngưỡng CSDL âm):** Nhập giới hạn dung lượng CSDL là số âm hoặc bằng 0. Báo lỗi yêu cầu nhập số nguyên dương lớn hơn 10MB.
*   `[ ]` **TC_ADMIN_BK_03 (Hành động WarnOnly):** Cấu hình giới hạn CSDL = `2` MB, hành động = `"WarnOnly"`. Khởi động Staff Client bằng tài khoản IT-Admin. Kết quả mong đợi: Dashboard xuất hiện Banner cảnh báo màu đỏ hiển thị kích thước thực của CSDL lớn hơn 2MB. Đăng nhập bằng tài khoản không phải Admin => Banner cảnh báo không được phép hiển thị.
*   `[ ]` **TC_ADMIN_BK_04 (Hành động AutoClean):** Cấu hình giới hạn CSDL = `2` MB, hành động = `"AutoClean"`. Khởi động Staff Client. Kết quả mong đợi: Hệ thống chạy ngầm dọn dẹp dữ liệu cũ, ghi log `SystemSettings_AutoClean` thành công và không hiển thị Banner cảnh báo đỏ gây nhiễu thông tin cho quản trị viên.
*   `[ ]` **TC_ADMIN_BK_05 (Lưu trữ CloudSync):** Cấu hình `IT_Backup_StorageTarget` = `"CloudSync"`. Chạy chức năng sao lưu thủ công. Kết quả mong đợi: File ZIP được tạo ở thư mục LAN local đồng thời kích hoạt tác vụ ngầm mô phỏng đẩy file lên Cloud của Phòng/Sở.

---

## ═══ KẾ HOẠCH XÁC MINH (VERIFICATION PLAN) ═══

### Kiểm thử tự động (Automated Tests)
*   Bổ sung 4 unit tests trong `QASmartClass.Tests` để xác minh:
    1.  Hàm kiểm tra định dạng giờ sao lưu (`HH:mm`) và mã màu Hex (`^#[0-9A-Fa-f]{6}$`).
    2.  Thuật toán đo độ tương phản màu sắc để loại bỏ các mã màu quá sáng.
    3.  `DbSizeCheckService` trả về đúng giá trị dung lượng thực tế.
    4.  Logic kích hoạt tự động dọn dẹp khi `IT_Database_OverLimitAction` = `"AutoClean"`.
*   Lệnh chạy kiểm thử: `dotnet test QASmartClass.Tests`

### Kiểm thử thủ công (Manual Verification)
*   Nhập liệu thử các trường hợp dữ liệu sai quy chuẩn trên giao diện Cấu hình để đảm bảo các cảnh báo lỗi (Tooltip/Messagebox) bằng tiếng Việt hiển thị chính xác, rõ nghĩa và dễ hiểu đối với nhân viên nhà trường.
