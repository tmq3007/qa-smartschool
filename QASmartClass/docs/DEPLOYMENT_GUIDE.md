# Hướng dẫn Triển khai QA SmartClass
> Phiên bản: v4.0 | Ngày: 14/05/2026  
> Đối tượng: IT trường học, Quản trị viên CNTT

---

## 📋 Mục lục

1. [Yêu cầu hệ thống](#1-yêu-cầu-hệ-thống)
2. [Cài đặt nhanh](#2-cài-đặt-nhanh)
3. [Cấu hình mạng](#3-cấu-hình-mạng)
4. [Vai trò người dùng](#4-vai-trò-người-dùng)
5. [Sao lưu & Khôi phục](#5-sao-lưu--khôi-phục)
6. [Cập nhật phần mềm](#6-cập-nhật-phần-mềm)
7. [Xử lý sự cố (FAQ)](#7-xử-lý-sự-cố-faq)
8. [Checklist triển khai](#8-checklist-triển-khai)

---

## 1. Yêu cầu hệ thống

### Máy Giáo viên (Server)
| Thành phần | Yêu cầu |
|---|---|
| OS | Windows 10 v1903+ / Windows 11 |
| RAM | 8 GB (tối thiểu 4 GB) |
| CPU | Intel i5 gen 8+ / AMD Ryzen 5+ |
| Ổ đĩa | 2 GB trống (cho app + DB + backups) |
| Màn hình | Smartboard cảm ứng hoặc monitor thường |
| .NET | .NET 8.0 Desktop Runtime |

### Máy Học sinh (Client)
| Thành phần | Yêu cầu |
|---|---|
| OS | Windows 10+ |
| RAM | 4 GB |
| Ổ đĩa | 500 MB |
| .NET | .NET 8.0 Desktop Runtime |

### Mạng
- **LAN/WiFi** cùng subnet (ví dụ: 192.168.1.x)
- **Ports cần mở:**
  - `29879` — TCP File Transfer
  - `29880` — UDP Network Discovery
  - `29881` — Student Network Client

---

## 2. Cài đặt nhanh

### Bước 1: Cài .NET Runtime
```powershell
# Tải và cài từ Microsoft
winget install Microsoft.DotNet.DesktopRuntime.8
```

### Bước 2: Cài QA SmartClass
1. Copy thư mục `QASmartClass/` vào `C:\Program Files\QASmartClass\`
2. Tạo shortcut từ `QASmartClass.exe` ra Desktop
3. Chạy lần đầu → chọn vai trò (Giáo viên / Học sinh / ...)

### Bước 3: Xác nhận hoạt động
1. Mở ứng dụng → Dashboard hiển thị bình thường
2. Kiểm tra thư mục DB: `%LOCALAPPDATA%\QASmartClass\smartclass.db` tồn tại
3. Kiểm tra log: `%LOCALAPPDATA%\QASmartClass\logs\` có file `.log`

### Bước 4: Nhập dữ liệu ban đầu (Excel/CSV v4.1)
*   Các tệp dữ liệu mẫu và hướng dẫn chuẩn bị chi tiết đã được đóng gói sẵn trong thư mục ứng dụng tại `Assets/DeploymentTemplates/`.
*   Hỗ trợ import trực tiếp từ tệp Excel `.xlsx` hoặc tệp CSV (tuân thủ chuẩn phân tách RFC 4180).
*   Hệ thống áp dụng băm mật khẩu bảo mật **HMACSHA512** (định dạng lưu trữ `"salt:hash"`) và tự động kiểm soát sĩ số không vượt quá sức chứa phòng máy (`MaxStudents`).
*   Giáo viên/Admin thực hiện nhập trực tiếp từ giao diện quản trị Admin hoặc màn hình Teacher Client.

---

## 3. Cấu hình mạng

### Mô hình kết nối
```
┌──────────────┐         LAN / WiFi         ┌──────────────┐
│  Máy GV      │◄──── TCP 29879 ────────────│  Máy HS 1    │
│  (Teacher)   │◄──── UDP 29880 (discover)──│  Máy HS 2    │
│  IP: .100    │◄──── TCP 29881 ────────────│  Máy HS ...  │
└──────────────┘                            └──────────────┘
```

### Cấu hình Firewall (máy GV)
```powershell
# Mở port cho QA SmartClass
netsh advfirewall firewall add rule name="QA SmartClass TCP" dir=in action=allow protocol=tcp localport=29879,29881
netsh advfirewall firewall add rule name="QA SmartClass UDP" dir=in action=allow protocol=udp localport=29880
```

### Kiểm tra kết nối
```powershell
# Từ máy HS → ping máy GV
ping 192.168.1.100

# Kiểm tra port
Test-NetConnection 192.168.1.100 -Port 29879
```

---

## 4. Vai trò người dùng

| Vai trò | Icon | Mô tả |
|---|---|---|
| **Giáo viên** | 👨‍🏫 | Quản lý lớp, soạn bài, kiểm tra, giao bài tập |
| **Học sinh** | 🎓 | Nhận bài, làm quiz, nộp bài, xem điểm |
| **Ban giám hiệu** | 🏫 | Dashboard KPI, báo cáo tổng thể |
| **IT Admin** | 🔧 | Cài đặt, mạng, sao lưu |
| **Nhân viên** | 📋 | Sự cố, cổng trường, canteen |
| **SmartTouch Only** | 🖊️ | Chỉ dùng bảng trắng tương tác |

**Cách đổi vai trò:** Mở Settings → Vai trò → Chọn vai trò mới → Restart

---

## 5. Sao lưu & Khôi phục

### Sao lưu tự động
- Hệ thống tự backup **mỗi lần khởi động** (1 bản/ngày)
- File backup: `%LOCALAPPDATA%\QASmartClass\backups\smartclass_backup_YYYYMMDD.db`
- Giữ lại tối đa **7 bản** gần nhất

### Sao lưu thủ công
Gọi `BackupService.CreateBackup("manual")` từ menu Admin hoặc:
```powershell
# Copy thủ công
Copy-Item "$env:LOCALAPPDATA\QASmartClass\smartclass.db" `
          "$env:LOCALAPPDATA\QASmartClass\backups\manual_backup.db"
```

### Khôi phục
1. Đóng ứng dụng hoàn toàn
2. Copy file backup vào vị trí DB chính:
```powershell
Copy-Item "backup_file.db" "$env:LOCALAPPDATA\QASmartClass\smartclass.db" -Force
```
3. Mở lại ứng dụng → Dữ liệu đã phục hồi

---

## 6. Cập nhật phần mềm

### Cập nhật thủ công
1. Tải bản mới từ nguồn phân phối
2. **Sao lưu DB** trước khi cập nhật
3. Ghi đè thư mục cài đặt (giữ lại thư mục `backups/`)
4. Chạy lại ứng dụng → DB tự migrate

### Kiểm tra version
- Xem trong log khi khởi động: `Database initialized: SQLite v4.7.0`
- Xem README.md hoặc About trong ứng dụng

---

## 7. Xử lý sự cố (FAQ)

### ❓ Học sinh không thấy bài tập
**Nguyên nhân:** HS chưa được thêm vào roster (danh sách lớp).  
**Giải pháp:** GV vào Classroom → Quản lý lớp → Thêm HS vào roster tương ứng.

### ❓ File nộp không đến máy GV
**Nguyên nhân:** Firewall chặn port 29879.  
**Giải pháp:**
1. Kiểm tra firewall (xem mục 3)
2. Kiểm tra cùng subnet
3. File sẽ được lưu local tại `Documents/Submissions/` nếu TCP fail

### ❓ Ứng dụng crash khi mở
**Nguyên nhân:** DB bị corrupt hoặc thiếu .NET Runtime.  
**Giải pháp:**
1. Kiểm tra .NET: `dotnet --version` (cần 8.0+)
2. Xem log: `%LOCALAPPDATA%\QASmartClass\logs\`
3. Restore backup: xem mục 5

### ❓ Biểu đồ Analytics trống
**Nguyên nhân:** Chưa có dữ liệu quiz/bài tập.  
**Giải pháp:** HS cần làm ít nhất 3 bài quiz để hệ thống phân tích xu hướng.

### ❓ Muốn reset toàn bộ dữ liệu
**Giải pháp:**
1. Đóng ứng dụng
2. Xóa file `smartclass.db`
3. Mở lại → DB mới với seed data mặc định

---

## 8. Checklist triển khai

### Trước khi triển khai
- [ ] Cài .NET 8.0 Desktop Runtime trên tất cả máy
- [ ] Mở firewall ports 29879, 29880, 29881
- [ ] Kiểm tra mạng LAN/WiFi cùng subnet
- [ ] Copy ứng dụng vào tất cả máy

### Sau khi triển khai
- [ ] Máy GV khởi động thành công (chọn vai trò Teacher)
- [ ] Máy HS kết nối được máy GV (network discovery)
- [ ] Gửi/nhận file hoạt động
- [ ] Quiz hoạt động
- [ ] Backup tự động chạy (kiểm tra thư mục backups)

### Hàng tuần
- [ ] Kiểm tra dung lượng DB (< 100 MB bình thường)
- [ ] Kiểm tra log lỗi
- [ ] Xác nhận backup đang chạy

---

*Tài liệu này được tạo bởi QA SmartSchool IT Team. Liên hệ hỗ trợ: it@qaschool.edu.vn*
