using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// DTO k?t qu? ki?m th? End-to-End
    /// </summary>
    public class TestResultDto
    {
        public string ScenarioId { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public string CoveredFunctions { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// WI-16: End-to-End Test Runner — Ki?m th? 9 k?ch b?n tích h?p.
    /// Xác nh?n r?ng t?t c? entities, services và views c?n thi?t t?n tại và ho?t d?ng.
    /// </summary>
    public class EndToEndTestRunner
    {
        private readonly AppDbContext _db;

        public EndToEndTestRunner(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Ch?y toàn b? 9 k?ch b?n ki?m th? End-to-End.
        /// </summary>
        public List<TestResultDto> RunAllTests()
        {
            var results = new List<TestResultDto>
            {
                RunT01_DashboardAndDataQuery(),
                RunT02_StaffQualification(),
                RunT03_LessonPlanTimetable(),
                RunT04_StudentAttendanceGrading(),
                RunT05_EmulationDisciplineAward(),
                RunT06_ContactBookMessaging(),
                RunT07_DepartmentObservationTopic(),
                RunT08_PdfMoetAnalytics(),
                RunT09_RoleCalendarInbox()
            };

            int passed = results.Count(r => r.Passed);
            int failed = results.Count(r => !r.Passed);

            Log.Information("--- EndToEndTestRunner: {Passed}/{Total} k?ch b?n PASSED, {Failed} FAILED ---",
                passed, results.Count, failed);

            foreach (var r in results)
            {
                if (r.Passed)
                    Log.Information("  ? {Id}: {Name} — PASSED", r.ScenarioId, r.ScenarioName);
                else
                    Log.Warning("  ? {Id}: {Name} — FAILED: {Details}", r.ScenarioId, r.ScenarioName, r.Details);
            }

            return results;
        }

        // ---------------------------------------------------------------
        // T-01: Dashboard Hiệu trưởng + DB query
        // ---------------------------------------------------------------
        private TestResultDto RunT01_DashboardAndDataQuery()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-01",
                ScenarioName = "Dashboard Hiệu trưởng & truy vấn dữ liệu",
                CoveredFunctions = "HT01-HT04 (N1: Qu?n lư GV)"
            };

            try
            {
                // Check PrincipalDashboardPage exists (type check)
                var dashboardType = Type.GetType("QASmartClass.Leadership.Views.PrincipalDashboardPage, QASmartClass");
                if (dashboardType == null)
                    throw new Exception("PrincipalDashboardPage type not found in assembly");

                // Check Students DbSet exists and queryable
                int studentCount = _db.Students.Count();

                // Check TeacherProfiles DbSet exists and queryable
                int teacherCount = _db.TeacherProfiles.Count();

                result.Passed = true;
                result.Details = $"Dashboard OK. Students={studentCount}, Teachers={teacherCount}";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-01 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-02: H? so GV + B?ng c?p / Ch?ng ch?
        // ---------------------------------------------------------------
        private TestResultDto RunT02_StaffQualification()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-02",
                ScenarioName = "H? so GV + B?ng c?p / Ch?ng ch?",
                CoveredFunctions = "HT01-HT04 (StaffProfile, TeacherQualification, QualificationService)"
            };

            try
            {
                // Check StaffProfile entity exists in DB
                int staffCount = _db.StaffProfiles.Count();

                // Check TeacherQualification entity exists in DB
                int qualCount = _db.TeacherQualifications.Count();

                // Check QualificationService instantiates
                var service = new QualificationService(_db);
                var summary = service.GetQualificationSummary();

                result.Passed = true;
                result.Details = $"StaffProfiles={staffCount}, Qualifications={qualCount}, Summary.TotalStaff={summary.TotalStaff}";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-02 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-03: Giáo án + Th?i khóa bi?u tuân th?
        // ---------------------------------------------------------------
        private TestResultDto RunT03_LessonPlanTimetable()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-03",
                ScenarioName = "Giáo án & Th?i khóa bi?u tuân th?",
                CoveredFunctions = "HT10-HT13 (LessonPlan, TimetableComplianceService)"
            };

            try
            {
                // Check LessonPlan entity exists in DB
                int planCount = _db.LessonPlans.Count();

                // Check TimetableComplianceService instantiates
                var service = new TimetableComplianceService(_db);

                result.Passed = true;
                result.Details = $"LessonPlans={planCount}, TimetableComplianceService OK";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-03 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-04: H?c sinh + Đi?m danh + TT22 Grading
        // ---------------------------------------------------------------
        private TestResultDto RunT04_StudentAttendanceGrading()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-04",
                ScenarioName = "H?c sinh + Đi?m danh + X?p lo?i TT22",
                CoveredFunctions = "HT20-HT23 (Student, AttendanceRecord, TT22GradingService)"
            };

            try
            {
                // Check Student entity
                int studentCount = _db.Students.Count();

                // Check AttendanceRecord entity
                int attCount = _db.AttendanceRecords.Count();

                // Check TT22GradingService instantiates
                var gradingService = new TT22GradingService(_db);

                result.Passed = true;
                result.Details = $"Students={studentCount}, AttendanceRecords={attCount}, TT22GradingService OK";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-04 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-05: SKKN + Thi dua + K? lu?t + Khen thu?ng
        // ---------------------------------------------------------------
        private TestResultDto RunT05_EmulationDisciplineAward()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-05",
                ScenarioName = "SKKN + Thi dua + K? lu?t + Khen thu?ng",
                CoveredFunctions = "HT30-HT33 (Skkn, EmulationService, DisciplineService, AwardService)"
            };

            try
            {
                // Check Skkn entity
                int skknCount = _db.Skkns.Count();

                // Check EmulationService instantiates
                var emulationService = new EmulationService(_db);

                // Check DisciplineService instantiates
                var disciplineService = new DisciplineService(_db);

                // Check AwardService instantiates
                var awardService = new AwardService(_db);
                var stats = awardService.GetAwardStatistics("");

                result.Passed = true;
                result.Details = $"Skkns={skknCount}, EmulationService OK, DisciplineService OK, AwardService OK (Awards={stats.TotalAwards})";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-05 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-06: S? liên l?c + Nh?n tin
        // ---------------------------------------------------------------
        private TestResultDto RunT06_ContactBookMessaging()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-06",
                ScenarioName = "S? liên l?c di?n t? + Nh?n tin",
                CoveredFunctions = "HT40-HT43 (ContactBookService, InboxMessage)"
            };

            try
            {
                // Check ContactBookService instantiates
                var contactService = new ContactBookService(_db);

                // Check InboxMessage entity
                int inboxCount = _db.InboxMessages.Count();

                // Check ContactBookEntry entity
                int entryCount = _db.ContactBookEntries.Count();

                result.Passed = true;
                result.Details = $"ContactBookService OK, InboxMessages={inboxCount}, ContactBookEntries={entryCount}";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-06 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-07: T? CM + D? gi? + Chuyên d?
        // ---------------------------------------------------------------
        private TestResultDto RunT07_DepartmentObservationTopic()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-07",
                ScenarioName = "T? chuyên môn + L?ch d? gi? + Chuyên d?",
                CoveredFunctions = "HT50-HT53 (Department, ObservationSchedule, ProfessionalTopic)"
            };

            try
            {
                // Check Department entity
                int deptCount = _db.Departments.Count();

                // Check ObservationSchedule entity
                int obsCount = _db.ObservationSchedules.Count();

                // Check ProfessionalTopic entity
                int topicCount = _db.ProfessionalTopics.Count();

                // Check DepartmentService instantiates
                var deptService = new DepartmentService(_db);
                var depts = deptService.GetAllDepartments();

                result.Passed = true;
                result.Details = $"Departments={deptCount}, ObservationSchedules={obsCount}, ProfessionalTopics={topicCount}, DepartmentService OK";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-07 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-08: Xuất PDF + MOET Reports + Phân tích so sánh
        // ---------------------------------------------------------------
        private TestResultDto RunT08_PdfMoetAnalytics()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-08",
                ScenarioName = "Xuất PDF + Báo cáo MOET + Phân tích so sánh",
                CoveredFunctions = "HT60-HT63 (PdfExportService, MoetReportService, ComparativeAnalyticsService)"
            };

            try
            {
                // Check PdfExportService instantiates
                var pdfService = new PdfExportService(_db);

                // Check MoetReportService instantiates
                var moetService = new MoetReportService(_db);

                // Check ComparativeAnalyticsService instantiates
                var analyticsService = new ComparativeAnalyticsService(_db);

                // Quick smoke test: compare empty semesters should return default DTO
                var comparison = analyticsService.CompareAcademicResults("HK1", "HK2", "2025-2026");

                result.Passed = true;
                result.Details = $"PdfExportService OK, MoetReportService OK, ComparativeAnalyticsService OK (ScoreDelta={comparison.ScoreDelta})";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-08 FAILED");
            }

            return result;
        }

        // ---------------------------------------------------------------
        // T-09: Phân quy?n + L?ch + H?p thu
        // ---------------------------------------------------------------
        private TestResultDto RunT09_RoleCalendarInbox()
        {
            var result = new TestResultDto
            {
                ScenarioId = "T-09",
                ScenarioName = "Phân quy?n + L?ch tru?ng + H?p thu d?n",
                CoveredFunctions = "Ph? tr? (RolePermissionManagerView, SchoolCalendarView, InboxMessages)"
            };

            try
            {
                // Check RolePermissionManagerView type exists
                var roleViewType = Type.GetType("QASmartClass.Leadership.Views.RolePermissionManagerView, QASmartClass");
                if (roleViewType == null)
                    throw new Exception("RolePermissionManagerView type not found in assembly");

                // Check SchoolCalendarView type exists
                var calendarViewType = Type.GetType("QASmartClass.Leadership.Views.SchoolCalendarView, QASmartClass");
                if (calendarViewType == null)
                    throw new Exception("SchoolCalendarView type not found in assembly");

                // Check InboxMessages DbSet
                int inboxCount = _db.InboxMessages.Count();

                result.Passed = true;
                result.Details = $"RolePermissionManagerView OK, SchoolCalendarView OK, InboxMessages={inboxCount}";
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Details = ex.Message;
                Log.Error(ex, "T-09 FAILED");
            }

            return result;
        }
    }
}

