# 🔧 Admin Setup Guide — QA SmartClass

**Phiên bản:** 4.0  
**Ngày cập nhật:** 2026-05-22

---

## 1. Cài đặt ban đầu

### Yêu cầu hệ thống
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- Dung lượng tối thiểu: 200MB

### Database
Hệ thống sử dụng **SQLite** và tự động migrate khi khởi động:
- `DbMigrator.cs` chạy tự động trong `App.xaml.cs` → `OnStartup()`
- File DB: `qasmartclass.db` trong thư mục ứng dụng
- Không cần cài đặt SQL Server hay MySQL

### Khởi chạy
```
dotnet run
# hoặc
QASmartClass.exe
```

---

## 2. Cấu hình Tổ Chuyên Môn

### Tạo Tổ CM mới
1. Đăng nhập với vai trò **HieuTruong** hoặc **Admin**
2. Vào **Leadership → Department Management**
3. Click **"➕ Thêm Tổ"** → Nhập:
   - Tên tổ (VD: "Tổ Toán-Lý-Tin")
   - Tổ trưởng (chọn từ danh sách GV)
   - Mô tả

### Phân GV vào Tổ
1. Tại **Department Management** → Chọn Tổ
2. Click **"👤 Thêm thành viên"**
3. Chọn GV từ danh sách → Click **"Thêm"**

### Entity: Department
| Trường | Kiểu | Mô tả |
|--------|------|-------|
| Id | int (PK) | Auto-increment |
| Name | string | Tên tổ CM |
| HeadTeacherId | int? | FK → TeacherProfiles.Id |
| Description | string? | Mô tả |

---

## 3. Phân quyền vai trò

### Các vai trò hệ thống

| Vai trò | Mã | Quyền chính |
|---------|-----|-------------|
| Giáo viên | GV | Xem lớp, nộp giáo án, nhắn tin |
| Tổ trưởng | ToTruong | + Duyệt GA tổ, quản lý tổ CM |
| Hiệu phó | HieuPho | + Xem KPI, duyệt khen thưởng |
| Hiệu trưởng | HieuTruong | + Full Leadership Dashboard, xuất MOET |
| Quản trị viên | Admin | + System Settings, Backup/Restore |

### Cách phân quyền
1. **Leadership → Role Permission Manager**
2. Chọn GV → Thay đổi vai trò (dropdown)
3. Click **"Lưu"**

### Lưu ý
- `AuthorizationGuard` kiểm tra quyền trước khi thực thi service
- GV không thể truy cập Leadership Dashboard
- Chỉ HieuTruong + Admin mới xuất được biểu mẫu MOET

---

## 4. Import danh sách GV/HS

### Import GV (TeacherProfile)
1. Chuẩn bị file Excel (.xlsx) với các cột:
   - StaffId, FullName, Email, PhoneNumber, Subject, Title, Role
2. **Leadership → System Data Exporter → Import GV**
3. Chọn file → Preview → Confirm

### Import HS (Student)
1. Chuẩn bị file Excel (.xlsx) với các cột:
   - FullName, ClassName, Gender, Ethnicity, Status
2. **HomeroomHub → Import HS**
3. Chọn file → Map cột → Import

---

## 5. Cấu hình năm học, học kỳ

### ClassRoster (Sổ lớp)
Mỗi ClassRoster đại diện cho 1 lớp-môn-kỳ:

| Trường | Ý nghĩa |
|--------|---------|
| SchoolYear | "2025-2026" |
| Semester | "HK1" hoặc "HK2" |
| ClassName | "10A1", "11B3" |
| SubjectName | "Toán", "Vật lý" |
| IsActive | true/false |

### Tạo Roster mới
- Tự động tạo khi import danh sách HS vào lớp
- Hoặc thủ công: **TeacherHub → Quản lý lớp → Tạo Roster**

---

## 6. Backup / Restore

### Backup
1. **Leadership → System → Backup/Restore**
2. Click **"📦 Backup Now"**
3. File `.db` backup sẽ lưu tại `Documents/QASmartClass/Backups/`
4. Tên file: `backup_qasmartclass_20260522_160000.db`

### Restore
1. **Leadership → System → Backup/Restore**
2. Click **"📂 Restore"** → Chọn file backup
3. Confirm → Hệ thống sẽ restart

### Lịch backup tự động
- Hệ thống tự backup mỗi ngày lúc 23:00 (nếu đang chạy)
- Giữ tối đa 7 bản backup gần nhất

---

## 7. Cấu hình PDF Output

### Đường dẫn xuất file
Tất cả file PDF/Excel xuất ra lưu tại:
```
%USERPROFILE%\Documents\QASmartClass\Reports\
```

### QuestPDF License
- Sử dụng **Community License** (miễn phí cho education)
- Được set tự động trong `PdfTemplateHelper.cs`

---

## 8. Hướng dẫn cài đặt môi trường test (Dành cho Tester)

Tài liệu này hướng dẫn quy trình thiết lập môi trường kiểm thử (Testing Environment) cho nhóm QA/Tester để phục vụ kiểm tra các chức năng tương tác thời gian thực giữa máy Giáo viên (Teacher) và máy Học sinh (Student).

### Quy trình cài đặt
1. **Tải và giải nén:** Tải bộ cài phần mềm và giải nén vào thư mục mong muốn trên cả hai máy kiểm thử.
2. **Khởi chạy ứng dụng:** Mở thư mục đã giải nén, chạy file thực thi `.exe` và chờ phần mềm khởi động hoàn tất.
3. **Chế độ kiểm thử:** Tại màn hình đăng nhập, chọn chế độ **Thử nghiệm**.

### Cấu hình kết nối giữa các máy kiểm thử
Để kiểm thử các chức năng tương tác, cần thiết lập kết nối LAN giữa máy Giáo viên và máy Học sinh theo các bước sau:
*   **Máy thứ nhất:** Đăng nhập bằng tài khoản/chế độ **Giáo viên**.
*   **Máy thứ hai:** Đăng nhập bằng tài khoản/chế độ **Học sinh**.
*   **Thiết lập liên kết trên máy Học sinh:**
    1. Vào mục **Cài đặt** ở thanh điều hướng.
    2. Chọn **Mở khóa** và nhập mã PIN bảo mật để mở quyền cấu hình.
    3. Cập nhật địa chỉ IP máy chủ thành địa chỉ IP hiện tại của máy Giáo viên.
    4. Chọn **Kết nối** để thiết lập liên kết.

### Kết quả mong đợi
- Môi trường test được thiết lập thành công, kết nối hiển thị trạng thái xanh trực tuyến.
- Sẵn sàng phục vụ các kịch bản kiểm thử tương tác.

---

## Liên hệ hỗ trợ
- **Email:** support@qasmartschool.vn
- **Hotline:** 1900-xxxx
