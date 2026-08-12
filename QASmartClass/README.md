# QA SmartClass v4.0

![Build Status](https://github.com/company/QASmartClass/actions/workflows/build.yml/badge.svg)
![Coverage](https://img.shields.io/badge/Coverage-76%25-brightgreen)
![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)

QA SmartClass là hệ thống phòng học thông minh toàn diện, tích hợp bảng tương tác (Smart Touch), quản lý lớp học và hệ thống đánh giá theo chuẩn giáo dục hiện đại.

---

## 📋 Mục lục

1. [Yêu cầu hệ thống](#-yêu-cầu-hệ-thống)
2. [Cài đặt & Chạy](#-cài-đặt--chạy)
3. [Kiến trúc Module](#-kiến-trúc-module)
4. [Cơ sở dữ liệu](#-cơ-sở-dữ-liệu)
5. [Cấu hình](#-cấu-hình)
6. [Coding Standards](#-coding-standards)
7. [Changelog](#-changelog)

---

## 💻 Yêu cầu hệ thống

| Thành phần | Yêu cầu tối thiểu |
|---|---|
| **OS** | Windows 10 version 1903+ / Windows 11 |
| **Runtime** | .NET 8.0 Desktop Runtime |
| **SDK** (dev) | .NET 8.0 SDK |
| **RAM** | 4 GB (khuyến nghị 8 GB) |
| **Ổ đĩa** | 500 MB trống (cho DB + backups) |
| **Màn hình** | 1366x768+ (hỗ trợ smartboard cảm ứng) |
| **Mạng** | LAN cho chức năng TCP file transfer |

### Dependencies chính
- `Microsoft.EntityFrameworkCore.Sqlite` — ORM + SQLite
- `Serilog` — Structured logging
- `QuestPDF` — Export báo cáo PDF
- `CommunityToolkit.Mvvm` — MVVM pattern
- `HelixToolkit.Wpf` — 3D visualization (Compass tool)

---

## 🚀 Cài đặt & Chạy

### Cho nhà phát triển

```powershell
# 1. Clone repository
git clone https://github.com/company/QASmartClass.git
cd QASmartClass

# 2. Restore dependencies
dotnet restore

# 3. Build
dotnet build

# 4. Chạy ứng dụng
dotnet run
```

### Cho triển khai (Production)

```powershell
# Build bản Release
dotnet publish -c Release -r win-x64 --self-contained true

# File output tại: bin/Release/net8.0-windows/win-x64/publish/
```

---

## 🏗 Kiến trúc Module

```
QASmartClass/
├── Data/                     # DbContext, Models, Seeders
│   ├── AppDbContext.cs       # 25+ DbSets (SQLite)
│   ├── DatabaseSeeder.cs     # Seed dữ liệu mặc định
│   └── SampleDataSeeder.cs   # Seed dữ liệu demo
│
├── Services/                 # Business logic layer
│   ├── AppServices.cs        # Static service helper
│   ├── AppPaths.cs           # Đường dẫn file hệ thống
│   ├── LearningAnalyticsService.cs  # Phân tích học tập
│   ├── PdfExportService.cs   # Xuất PDF báo cáo
│   └── ...                   # 30+ services khác
│
├── Leadership/               # Module 1: Ban Giám Hiệu
│   ├── Views/                # Dashboard KPI, Báo cáo
│   └── ViewModels/
│
├── TeacherHub/               # Module 2: Giáo viên Bộ môn
│   ├── Views/                # Assignments, Grading, Dashboard
│   └── ViewModels/
│
├── HomeroomHub/              # Module 3: GV Chủ nhiệm
│   ├── Views/                # Điểm danh, Hạnh kiểm
│   └── ViewModels/
│
├── StudentClient/            # Module 4: Học sinh
│   ├── Views/                # Dashboard, Submit, Analytics, Quiz
│   ├── Models/               # AssignmentItem, etc.
│   └── Services/             # StudentIdentityService, FileTransfer, Network
│
├── Staff/                    # Module 6: Nhân viên
│   ├── Views/                # Sự cố, Cổng trường, Canteen
│   └── ViewModels/
│
├── WebAdmin/                 # Module 8: Admin (Web-based)
│   ├── index.html
│   └── dashboard.js
│
├── Classroom/                # Smart Touch core (Bảng tương tác)
│   ├── Views/
│   └── Services/
│
├── LearningTools/            # 90+ công cụ dạy học
│   ├── DS.cs                 # Design System constants
│   ├── UI.cs                 # UI Factory methods
│   └── CODING_STANDARDS.md   # Coding standards cho tools
│
└── Shared/                   # Shared components, converters
```

### Sơ đồ chủ thể

```
┌─────────────┐   ┌──────────────┐   ┌──────────────┐
│  Ban Giám    │   │  GV Bộ môn   │   │  GV Chủ      │
│  Hiệu (BGH) │   │  (TeacherHub)│   │  nhiệm (GVCN)│
│  Module 1 ✅ │   │  Module 2 ✅  │   │  Module 3 ✅  │
└──────┬───────┘   └──────┬───────┘   └──────┬───────┘
       │                  │                  │
       └──────────────────┼──────────────────┘
                          │
                   ┌──────┴───────┐
                   │   SQLite DB  │
                   │  AppDbContext │
                   └──────┬───────┘
                          │
       ┌──────────────────┼──────────────────┐
       │                  │                  │
┌──────┴───────┐   ┌──────┴───────┐   ┌──────┴───────┐
│  Học sinh    │   │  Phụ huynh   │   │  Nhân viên   │
│  (Student)   │   │  (Parent)    │   │  (Staff)     │
│  Module 4 ✅ │   │  Module 5 ⬜  │   │  Module 6 ✅  │
└──────────────┘   └──────────────┘   └──────────────┘
```

---

## 🗃 Cơ sở dữ liệu

**Engine:** SQLite (file-based)  
**ORM:** Entity Framework Core 8  
**Đường dẫn mặc định:** `%LOCALAPPDATA%/QASmartClass/smartclass.db`

### Các bảng chính (25+ tables)

| Bảng | Mô tả |
|---|---|
| `Students` | Danh sách học sinh (FullName, StudentCode, Avatar) |
| `Teachers` | Danh sách giáo viên |
| `ClassRosters` | Danh sách lớp học (ClassName, Subject, GradeLevel) |
| `ClassRosterStudents` | Quan hệ N-N HS ↔ Lớp |
| `Assignments` | Bài tập (Title, Deadline, RosterId) |
| `FileTransfers` | Lịch sử nộp file HS ↔ GV |
| `Quizzes` / `Questions` | Ngân hàng câu hỏi |
| `QuizResults` | Kết quả làm quiz |
| `LearningAnalytics` | Phân tích tiến trình học tập |
| `StudentAchievements` | Huy hiệu Gamification |
| `AttendanceRecords` | Điểm danh |
| `ConductRecords` | Hạnh kiểm (+/-) |
| `StudentGrades` | Bảng điểm theo loại |
| `GradeTypeMasters` | Cấu hình loại điểm (Miệng, 15p, 1T, HK) |
| `EventLogs` | Nhật ký sự kiện |
| `UsageLogs` | Telemetry |
| `MathQuizHistory` | Lịch sử luyện Toán |

### Migration

Database tự động migrate khi khởi động qua `DbMigrator.Migrate()`. Phiên bản hiện tại: `4.7.0`.

---

## ⚙ Cấu hình

### Đường dẫn quan trọng (AppPaths)

| Đường dẫn | Mô tả |
|---|---|
| `AppPaths.DatabaseFile` | File SQLite chính |
| `AppPaths.DocumentsDir` | Thư mục tài liệu (bài tập, file nộp) |
| `AppPaths.BackupsDir` | Thư mục backup DB |
| `AppPaths.StudentProfileFile` | Cache profile HS đang đăng nhập |

### Cổng mạng (Network Ports)

| Port | Chức năng |
|---|---|
| `29879` | TCP File Transfer (GV ↔ HS) |
| `29880` | Network Discovery (UDP broadcast) |
| `29881` | Student Network Client |

---

## 📝 Coding Standards

Mọi module UI mới **bắt buộc** phải tuân thủ:
- Sử dụng Design System: `DS.cs` cho font/colors/spacing, `UI.cs` cho factory methods
- **Font:** Segoe UI (KHÔNG dùng Consolas)
- **MVVM pattern** với CommunityToolkit.Mvvm
- Tham khảo chi tiết: [CODING_STANDARDS.md](LearningTools/CODING_STANDARDS.md)

---

## 🚀 Changelog

### v4.0.0 (2026-05-14)
*Bản phát hành lớn với nhiều cải tiến kiến trúc và nghiệp vụ giáo dục.*

- **Hệ thống đánh giá mới (Rubric System)**: Hỗ trợ giáo viên chấm điểm linh hoạt theo tiêu chí rubric chuẩn.
- **Learning Analytics**: Tích hợp phân tích phổ điểm, xu hướng học tập và đưa ra gợi ý môn học yếu.
- **Gamification**: Streak + Badge system cho học sinh.
- **Export PDF Báo cáo**: Hỗ trợ 4 mẫu báo cáo chuẩn (Attendance, Grades, Quiz Summary, System Usage) qua `QuestPDF`.
- **Dashboard Ban Giám Hiệu (BGH)**: Giao diện Admin tổng quan cho cấp quản lý trường học.
- **StudentIdentityService**: Service nhận diện HS dùng chung, loại bỏ code trùng lặp.
- **AppServices**: Static helper giảm coupling với Application.Current.
- **Chuẩn hóa Kiến trúc (MVVM)**: Chuyển đổi thành công sang `CommunityToolkit.Mvvm` cho các modules lõi.

### v3.2.0 (2025-11-20)
- Tích hợp Smart Touch v3
- Cập nhật giao diện bảng vẽ

---

## 📝 Tech Debt
Xem danh sách nợ kỹ thuật tại [docs/TECH_DEBT.md](docs/TECH_DEBT.md).
