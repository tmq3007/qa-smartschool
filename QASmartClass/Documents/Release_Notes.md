# 📋 Release Notes — QA SmartClass Principal Dashboard

**Phiên bản:** 4.0  
**Ngày phát hành:** 2026-05-22  
**Tổng Work Items:** 17 WIs across 4 Phases

---

## 🔴 Phase 1: CRITICAL — Nền tảng PDF & Dữ liệu thật

### WI-01: PdfExportService → QuestPDF ✅
- **Thay đổi:** Viết lại hoàn toàn PdfExportService từ text/CSV sang QuestPDF
- **Tính năng:** 7 phương thức xuất PDF chuyên nghiệp (Attendance, Grade, Quiz, Usage, Exam, Meeting, Homeroom)
- **File mới:** `Services/PdfTemplateHelper.cs` — Header trường, quốc hiệu, footer, table styles
- **File sửa:** `Services/PdfExportService.cs`

### WI-02: Dashboard Dữ liệu thật ✅
- **Thay đổi:** Xóa hoàn toàn `LoadFakeData()`, thay bằng `LoadRealData()` từ DB
- **Tính năng:** KPI thật (Students.Count, TeacherProfiles.Count, AttendanceRecords, LessonPlans)
- **File sửa:** `Leadership/Views/PrincipalDashboardPage.xaml.cs`

### WI-03: Module Kỷ luật GV/HS ✅
- **Thay đổi:** Mở rộng entity DisciplineRecord + tạo DisciplineService mới
- **Tính năng:** 4 loại vi phạm MOET, 4 mức kỷ luật, quy trình phê duyệt
- **File mới:** `Services/DisciplineService.cs`

### WI-04: Quản lý Bằng cấp / Chứng chỉ GV ✅
- **Thay đổi:** Thêm entity TeacherQualification + TrainingHistory
- **Tính năng:** CRUD bằng cấp, cảnh báo hết hạn, thống kê trình độ
- **File mới:** `Services/QualificationService.cs`

---

## 🟡 Phase 2: IMPORTANT — Tổ chuyên môn & Khen thưởng

### WI-05: Entity Tổ Chuyên Môn ✅
- **Thay đổi:** Thêm entity Department, DepartmentMember + DepartmentService
- **Tính năng:** Quản lý tổ CM master-detail, KPI tổ
- **File mới:** `Services/DepartmentService.cs`, `Leadership/Views/DepartmentManagementView.xaml`

### WI-06: Khen thưởng Workflow ✅
- **Thay đổi:** Entity AwardRecord + AwardService + UI quản lý
- **Tính năng:** Đề xuất → Duyệt → In giấy khen QuestPDF hàng loạt
- **File mới:** `Services/AwardService.cs`, `Leadership/Views/AwardManagementView.xaml`

### WI-07: Kiểm tra TKB Compliance ✅
- **Thay đổi:** Tạo TimetableComplianceService mới
- **Tính năng:** So khớp TKB vs thực tế, KPI tỷ lệ tuân thủ
- **File mới:** `Services/TimetableComplianceService.cs`

### WI-08: So sánh Dữ liệu qua Kỳ ✅
- **Thay đổi:** Tạo ComparativeAnalyticsService mới
- **Tính năng:** Delta/Trend giữa HK1↔HK2 (Academic, Attendance, Emulation)
- **File mới:** `Services/ComparativeAnalyticsService.cs`

### WI-09: Enforce Phân quyền ✅
- **Thay đổi:** AuthorizationGuard + try-catch protection cho services
- **Tính năng:** Role-based access control (GV/TT/HP/HT/Admin)

---

## 🟢 Phase 3: MOET COMPLIANCE — Biểu mẫu & Chuẩn Bộ GD

### WI-10: Sổ Liên Lạc Điện Tử ✅
- **Thay đổi:** Tạo ContactBookService + ContactBookView + ViewModel
- **Tính năng:** Auto GPA, ngày vắng, hạnh kiểm → GVCN nhận xét → Export PDF MOET
- **File mới:** `Services/ContactBookService.cs`, `HomeroomHub/Views/ContactBookView.xaml`

### WI-11: Biểu Mẫu MOET ✅
- **Thay đổi:** Viết lại MoetReportService từ CSV sang QuestPDF
- **Tính năng:** Mẫu 20-THCS (sĩ số), Mẫu 22-THCS (học lực+hạnh kiểm), Tổng kết (GV+HS)
- **File sửa:** `Services/MoetReportService.cs`, `Leadership/Views/ReportExportPage.xaml`

### WI-12: Lịch Dự Giờ Tổ CM ✅
- **Thay đổi:** Thêm TabControl + Calendar view vào ClassObservationView
- **Tính năng:** Lịch dự giờ tháng, filter theo tổ, "Hoàn thành" → Auto mở form
- **File sửa:** `Leadership/Views/ClassObservationView.xaml`

### WI-13: Quản Lý Chuyên Đề ✅
- **Thay đổi:** Tạo ProfessionalTopicView mới
- **Tính năng:** Timeline + Star rating (1-5) + Thống kê tổ/tháng
- **File mới:** `Leadership/Views/ProfessionalTopicView.xaml`

### WI-14: Rubric Dự Giờ MOET ✅
- **Thay đổi:** Mở rộng ClassObservation UI → 5 tiêu chí CV 5555/BGDĐT
- **Tính năng:** KHBD(25%)+TCHĐ(30%)+HĐHS(20%)+HTGV(15%)+KTĐG(10%) → Auto xếp loại
- **File sửa:** `Leadership/Views/ClassObservationView.xaml.cs`

---

## 🔵 Phase 4: POLISH & QA — Thi đua, Test, Tài liệu

### WI-15: Tổng Hợp Thi Đua HS ✅
- **Thay đổi:** Mở rộng EmulationService + EmulationBoardPage
- **Tính năng:**
  - CalcStudentEmulation: HSG/HSTT/HSTB/Chưa đạt
  - CalcClassEmulation: Lớp Xuất sắc/Tiên tiến/Hoàn thành
  - CompareEmulationBySemester: HK1↔HK2 delta
  - EmulationBoardPage: 4 tabs (Xếp hạng tháng, Thi đua HS, Thi đua Lớp, So sánh HK)
- **File sửa:** `Services/EmulationService.cs`, `Leadership/Views/EmulationBoardPage.xaml`

### WI-16: Test End-to-End ✅
- **Thay đổi:** Tạo EndToEndTestRunner service
- **Tính năng:** 9 kịch bản test tự động kiểm tra entities, services, integration
- **File mới:** `Services/EndToEndTestRunner.cs`

### WI-17: Documentation ✅
- **Thay đổi:** Tạo 3 tài liệu hướng dẫn
- **Files:**
  - `Documents/Principal_User_Guide.md` — Hướng dẫn 28 chức năng
  - `Documents/Admin_Setup_Guide.md` — Cấu hình hệ thống
  - `Documents/Release_Notes.md` — Changelog 4 Phase

---

## Thống kê tổng thể

| Metric | Giá trị |
|--------|---------|
| Tổng WI hoàn thành | **17/17** (100%) |
| Files mới tạo | ~25 files |
| Files sửa đổi | ~15 files |
| Build errors | **0** ✅ |
| Coverage chức năng | **100%** (28/28 HT codes + 4 phụ trợ) |
| Phases | **4/4** hoàn thành |
