# 📖 Hướng Dẫn Sử Dụng — Module Hiệu Trưởng (Principal Dashboard)

**Hệ thống:** QA SmartClass — Quản lý Trường học Thông minh  
**Phiên bản:** 4.0 — Hoàn thành 4 Phase nâng cấp  
**Ngày cập nhật:** 2026-05-22

---

## Mục lục

1. [N1: Quản lý Giáo viên (HT01-HT04)](#n1-quản-lý-giáo-viên)
2. [N2: Giảng dạy (HT10-HT13)](#n2-giảng-dạy)
3. [N3: Học sinh (HT20-HT23)](#n3-học-sinh)
4. [N4: Thi đua / Kỷ luật (HT30-HT33)](#n4-thi-đua--kỷ-luật)
5. [N5: Giáo viên Chủ nhiệm (HT40-HT43)](#n5-giáo-viên-chủ-nhiệm)
6. [N6: Tổ Chuyên môn (HT50-HT53)](#n6-tổ-chuyên-môn)
7. [N7: Báo cáo / Thống kê (HT60-HT63)](#n7-báo-cáo--thống-kê)
8. [Phụ trợ: Phân quyền, Lịch, Nhắn tin, Thông báo](#phụ-trợ)

---

## N1: Quản lý Giáo viên

| Mã | Chức năng | Mô tả | Navigation |
|----|-----------|-------|------------|
| HT01 | Danh sách GV | Xem danh sách tất cả GV với thông tin cơ bản (tên, chuyên môn, tổ) | Leadership → Staff |
| HT02 | Theo dõi hoạt động GV | Xem số giáo án, tiết dạy, nhiệm vụ hoàn thành của từng GV | Leadership → Staff → Chi tiết GV |
| HT03 | Hồ sơ – Bằng cấp – Chứng chỉ | Quản lý bằng cấp (ThS/TS), chứng chỉ (IELTS, tin học), cảnh báo hết hạn | Leadership → Staff → Bằng cấp (WI-04) |
| HT04 | Đánh giá, xếp loại GV | Đánh giá 360° với rubric, xếp loại GV theo tiêu chuẩn BGDĐT | Leadership → Evaluation 360° |

**Tính năng nổi bật:**
- QualificationService tự động cảnh báo chứng chỉ sắp hết hạn (30/60/90 ngày)
- Hồ sơ GV lưu trữ file scan PDF/JPG/PNG, tối đa 5MB/file
- KPI GV: Tỷ lệ hoàn thành nhiệm vụ, số giáo án, điểm dự giờ trung bình

---

## N2: Giảng dạy

| Mã | Chức năng | Mô tả | Navigation |
|----|-----------|-------|------------|
| HT10 | Tiến độ giảng dạy | Theo dõi tiến độ thực hiện chương trình giảng dạy theo tuần/tháng | Leadership → KPI Dashboard |
| HT11 | Kiểm tra giáo án | Duyệt giáo án (LessonPlan) do GV nộp: Approve/Reject/Revise | Leadership → Approval Queue |
| HT12 | Phân tích TKB | So khớp TKB với thực tế giảng dạy (WI-07: TimetableComplianceService) | Leadership → KPI → TKB Compliance |
| HT13 | Tổng hợp dữ liệu dạy học | Dashboard KPI tổng hợp: Số tiết, hiệu suất, xu hướng | Leadership → Principal Dashboard (WI-02) |

**Tính năng nổi bật:**
- TimetableComplianceService tự động so khớp TimetableEntries vs UsageLogs
- Hiển thị tỷ lệ tuân thủ TKB trực quan trên Dashboard
- Click vào KPI card → Xem chi tiết tiết dạy lệch giờ

---

## N3: Học sinh

| Mã | Chức năng | Mô tả | Navigation |
|----|-----------|-------|------------|
| HT20 | Sĩ số, chuyên cần | Thống kê sĩ số theo lớp/khối, tỷ lệ chuyên cần | Leadership → KPI Dashboard |
| HT21 | Kết quả học tập | Xem điểm TB, xếp loại học lực theo TT22 | Leadership → KPI → Academic |
| HT22 | HS đặc biệt | Cảnh báo HS có nguy cơ (điểm thấp, vắng nhiều, kỷ luật) | TeacherHub → Cảnh báo |
| HT23 | Rèn luyện – phẩm chất | Xếp loại hạnh kiểm theo thang điểm conduct | HomeroomHub → Conduct |

**Tính năng nổi bật:**
- TT22GradingService tính GPA chính xác theo Thông tư 22/2021/TT-BGDĐT
- Phân loại học lực: Giỏi (≥8.0) / Khá (≥6.5) / TB (≥5.0) / Yếu (≥3.5) / Kém (<3.5)
- Hạnh kiểm: Tốt (≥80) / Khá (≥65) / TB (≥50) / Yếu (<50)

---

## N4: Thi đua / Kỷ luật

| Mã | Chức năng | Mô tả | Navigation |
|----|-----------|-------|------------|
| HT30 | SKKN | Quản lý Sáng kiến Kinh nghiệm của GV | TeacherHub → SKKN |
| HT31 | Thi đua GV | Xếp hạng GV theo KPI hoạt động hàng tháng | Leadership → Emulation Board |
| HT32 | Thi đua HS | HSG/HSTT/HSTB theo học kỳ, so sánh HK1↔HK2 (WI-15) | Leadership → Emulation → Tab HS |
| HT33 | Kỷ luật | Quản lý kỷ luật GV/HS: 4 mức vi phạm MOET (WI-03) | Leadership → Discipline |

**Tính năng nổi bật:**
- WI-15: CalcStudentEmulation tự động xếp loại HSG/HSTT/HSTB/Chưa đạt
- WI-15: CalcClassEmulation xếp loại Lớp Xuất sắc/Tiên tiến/Hoàn thành
- WI-03: DisciplineService hỗ trợ 4 loại vi phạm MOET và 4 mức kỷ luật
- WI-06: AwardService quản lý khen thưởng và in giấy khen PDF hàng loạt

---

## N5: Giáo viên Chủ nhiệm

| Mã | Chức năng | Mô tả | Navigation |
|----|-----------|-------|------------|
| HT40 | Hoạt động GVCN | Quản lý hoạt động GVCN: sinh hoạt lớp, liên hệ PH | HomeroomHub |
| HT41 | Tình hình lớp học | Tổng quan lớp: sĩ số, điểm TB, chuyên cần, hạnh kiểm | HomeroomHub → Dashboard |
| HT42 | Giao tiếp PH | Nhắn tin với Phụ huynh qua hệ thống | HomeroomHub → Inbox |
| HT43 | Sổ liên lạc | Sổ liên lạc điện tử: tổng hợp điểm, hạnh kiểm, xuất PDF (WI-10) | HomeroomHub → Sổ Liên Lạc |

**Tính năng nổi bật:**
- WI-10: ContactBookService auto-populate GPA, ngày vắng, hạnh kiểm từ DB
- Xuất PDF sổ liên lạc chuẩn MOET (A4, bảng điểm, nhận xét GVCN)
- GVCN nhập nhận xét cá nhân → Lưu DB → Xuất PDF

---

## N6: Tổ Chuyên môn

| Mã | Chức năng | Mô tả | Navigation |
|----|-----------|-------|------------|
| HT50 | Tổ chuyên môn | Quản lý danh sách tổ CM, thành viên, tổ trưởng (WI-05) | Leadership → Department |
| HT51 | Kế hoạch CM tổ | Xem kế hoạch chuyên môn của từng tổ | Leadership → Department → KH |
| HT52 | Chuyên đề | Quản lý chuyên đề: tạo, đánh giá sao, thống kê (WI-13) | Leadership → Professional Topic |
| HT53 | Dự giờ | Lịch dự giờ + Phiếu đánh giá 5 tiêu chí CV 5555 (WI-12, WI-14) | Leadership → Class Observation |

**Tính năng nổi bật:**
- WI-12: Lịch dự giờ calendar monthly → Hoàn thành → Auto mở form đánh giá
- WI-13: ProfessionalTopicView timeline + star rating (1-5) + running average
- WI-14: Rubric 5 tiêu chí MOET: KHBD(25%), TCHĐ(30%), HĐHS(20%), HTGV(15%), KTĐG(10%)

---

## N7: Báo cáo / Thống kê

| Mã | Chức năng | Mô tả | Navigation |
|----|-----------|-------|------------|
| HT60 | Dashboard thống kê | KPI trực quan: Tổng HS, GV, Chuyên cần, Bài giảng mới (WI-02) | Leadership → Principal Dashboard |
| HT61 | Xuất báo cáo | PDF/Excel + 3 biểu mẫu MOET (Mẫu 20, 22, Tổng kết) (WI-01, WI-11) | Leadership → Report Export |
| HT62 | Phân tích xu hướng | So sánh HK1↔HK2: học lực, chuyên cần, thi đua (WI-08) | Leadership → KPI → Analytics |
| HT63 | Quản trị hệ thống | Backup/Restore, System Settings, Audit Log | Leadership → System |

**Tính năng nổi bật:**
- WI-01: PdfExportService (QuestPDF) xuất 7 loại báo cáo chuyên nghiệp
- WI-11: MoetReportService xuất 3 biểu mẫu MOET chuẩn (Mẫu 20, 22, Tổng kết)
- WI-08: ComparativeAnalyticsService so sánh delta và trend giữa các kỳ

---

## Phụ trợ

| Chức năng | Mô tả | Navigation |
|-----------|-------|------------|
| Phân quyền | Enforce vai trò (GV/TT/HP/HT/Admin) cho service và view (WI-09) | Leadership → Role Permission |
| Lịch công tác | Lịch sự kiện trường: hội nghị, thanh tra, ngày lễ | Leadership → School Calendar |
| Nhắn tin nội bộ | Hệ thống nhắn tin giữa GV, BGH, PH | TeacherHub → Inbox |
| Thông báo toàn trường | Gửi thông báo cho tất cả GV/HS/PH | Leadership → Broadcast |

---

## Hỗ trợ kỹ thuật

- **Database:** SQLite (auto-migrate qua DbMigrator.cs khi khởi động)
- **PDF output:** `Documents/QASmartClass/Reports/` 
- **Log file:** `Serilog` lưu tại thư mục ứng dụng
- **Liên hệ:** IT Support Team
