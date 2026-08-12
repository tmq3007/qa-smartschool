using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    /// <summary>
    /// WI-03: Service quáº£n lÃ½ ká»· luáº­t GV/HS
    /// </summary>
    public class DisciplineService
    {
        private readonly AppDbContext _db;

        private static readonly string[] ValidViolationTypes = { "Nháº¹", "Trung bÃ¬nh", "Náº·ng", "Äáº·c biá»‡t nghiÃªm trá»ng" };
        private static readonly string[] ValidDisciplineLevels = { "Nháº¯c nhá»Ÿ", "Khiá»ƒn trÃ¡ch", "Cáº£nh cÃ¡o", "Buá»™c thÃ´i há»c" };

        public DisciplineService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>Láº¥y danh sÃ¡ch ká»· luáº­t theo há»c sinh</summary>
        public List<DisciplineRecord> GetByStudent(int studentId)
        {
            try
            {
                return _db.DisciplineRecords
                    .Where(d => d.StudentId == studentId)
                    .OrderByDescending(d => d.Date)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: GetByStudent failed for StudentId={StudentId}", studentId);
                return new List<DisciplineRecord>();
            }
        }

        /// <summary>Láº¥y danh sÃ¡ch ká»· luáº­t theo lá»›p vÃ  thÃ¡ng/nÄƒm</summary>
        public List<DisciplineRecord> GetByClass(string className, int month, int year)
        {
            try
            {
                return _db.DisciplineRecords
                    .Where(d => d.ClassName == className && d.Date.Month == month && d.Date.Year == year)
                    .OrderByDescending(d => d.Date)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: GetByClass failed for {ClassName} {Month}/{Year}", className, month, year);
                return new List<DisciplineRecord>();
            }
        }

        /// <summary>Láº¥y danh sÃ¡ch há»“ sÆ¡ chá» phÃª duyá»‡t</summary>
        public List<DisciplineRecord> GetPendingApproval()
        {
            try
            {
                return _db.DisciplineRecords
                    .Where(d => d.Status == "Pending")
                    .OrderBy(d => d.Date)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: GetPendingApproval failed");
                return new List<DisciplineRecord>();
            }
        }

        /// <summary>Táº¡o há»“ sÆ¡ ká»· luáº­t má»›i vá»›i validation</summary>
        public bool CreateRecord(int studentId, string className, string violationType, string disciplineLevel, string description, string createdBy)
        {
            try
            {
                // Validate studentId exists
                var student = _db.Students.Find(studentId);
                if (student == null)
                {
                    Log.Warning("DisciplineService: CreateRecord failed - StudentId={StudentId} not found", studentId);
                    return false;
                }

                // Validate violationType
                if (!ValidViolationTypes.Contains(violationType))
                {
                    Log.Warning("DisciplineService: CreateRecord failed - Invalid ViolationType={ViolationType}", violationType);
                    return false;
                }

                // Validate disciplineLevel
                if (!ValidDisciplineLevels.Contains(disciplineLevel))
                {
                    Log.Warning("DisciplineService: CreateRecord failed - Invalid DisciplineLevel={DisciplineLevel}", disciplineLevel);
                    return false;
                }

                var record = new DisciplineRecord
                {
                    StudentId = studentId,
                    StudentName = student.FullName,
                    ClassName = className,
                    Type = "Discipline",
                    ViolationType = violationType,
                    DisciplineLevel = disciplineLevel,
                    Reason = description,
                    ReportedBy = createdBy,
                    Date = DateTime.Now,
                    Status = "Draft"
                };

                _db.DisciplineRecords.Add(record);
                _db.SaveChanges();
                Log.Information("DisciplineService: Record created for Student={StudentName} by {CreatedBy}, ViolationType={ViolationType}", student.FullName, createdBy, violationType);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: CreateRecord failed");
                return false;
            }
        }

        /// <summary>PhÃª duyá»‡t há»“ sÆ¡ ká»· luáº­t (chá»‰ khi Status=Pending)</summary>
        public bool ApproveRecord(int recordId, string approverName)
        {
            try
            {
                AuthorizationGuard.EnsureRole(UserSessionService.Instance.Role, 
                    StatusConstants.TeacherRole.HieuTruong, 
                    StatusConstants.TeacherRole.HieuPho, 
                    StatusConstants.TeacherRole.Admin);

                var record = _db.DisciplineRecords.Find(recordId);
                if (record == null)
                {
                    Log.Warning("DisciplineService: ApproveRecord failed - RecordId={RecordId} not found", recordId);
                    return false;
                }

                if (record.Status != "Pending")
                {
                    Log.Warning("DisciplineService: ApproveRecord failed - RecordId={RecordId} Status={Status} (expected Pending)", recordId, record.Status);
                    return false;
                }

                record.Status = "Approved";
                record.ApprovedBy = approverName;
                _db.SaveChanges();
                AuditHelper.Log(_db, "APPROVE_DISCIPLINE", approverName, $"PhÃª duyá»‡t há»“ sÆ¡ ká»· luáº­t ID {recordId} cho há»c sinh {record.StudentName} (Lá»›p {record.ClassName})");
                Log.Information("DisciplineService: Record {RecordId} approved by {Approver}", recordId, approverName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: ApproveRecord failed for RecordId={RecordId}", recordId);
                throw; // rethrow the exception so it can be handled and shown by the UI/Controller
            }
        }

        /// <summary>Tá»« chá»‘i há»“ sÆ¡ ká»· luáº­t (chá»‰ khi Status=Pending)</summary>
        public bool RejectRecord(int recordId, string approverName, string reason)
        {
            try
            {
                AuthorizationGuard.EnsureRole(UserSessionService.Instance.Role, 
                    StatusConstants.TeacherRole.HieuTruong, 
                    StatusConstants.TeacherRole.HieuPho, 
                    StatusConstants.TeacherRole.Admin);

                var record = _db.DisciplineRecords.Find(recordId);
                if (record == null)
                {
                    Log.Warning("DisciplineService: RejectRecord failed - RecordId={RecordId} not found", recordId);
                    return false;
                }

                if (record.Status != "Pending")
                {
                    Log.Warning("DisciplineService: RejectRecord failed - RecordId={RecordId} Status={Status} (expected Pending)", recordId, record.Status);
                    return false;
                }

                record.Status = "Draft";
                record.ApprovedBy = string.Empty;
                record.Resolution = $"Tá»« chá»‘i bá»Ÿi {approverName}: {reason}";
                _db.SaveChanges();
                AuditHelper.Log(_db, "REJECT_DISCIPLINE", approverName, $"Tá»« chá»‘i há»“ sÆ¡ ká»· luáº­t ID {recordId} cho há»c sinh {record.StudentName}. LÃ½ do: {reason}");
                Log.Information("DisciplineService: Record {RecordId} rejected by {Approver}. Reason: {Reason}", recordId, approverName, reason);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: RejectRecord failed for RecordId={RecordId}", recordId);
                throw;
            }
        }

        /// <summary>Táº¡o báº£n ghi khen thÆ°á»Ÿng / ká»· luáº­t tá»« nháº­t kÃ½ lá»›p chá»§ nhiá»‡m</summary>
        public bool CreateHomeroomRecord(int studentId, string studentName, string className, string type, string reason, string reportedBy, string violationType = "", string disciplineLevel = "")
        {
            try
            {
                var record = new DisciplineRecord
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    ClassName = className,
                    Type = type,
                    Reason = reason,
                    ReportedBy = reportedBy,
                    Date = DateTime.Now,
                    Status = type == "Discipline" ? "Pending" : "Approved",
                    ViolationType = violationType,
                    DisciplineLevel = disciplineLevel
                };

                _db.DisciplineRecords.Add(record);
                _db.SaveChanges();
                Log.Information("DisciplineService: CreateHomeroomRecord succeeded. StudentId={StudentId}, Type={Type}, Status={Status}", studentId, type, record.Status);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: CreateHomeroomRecord failed");
                return false;
            }
        }

        /// <summary>Thá»‘ng kÃª ká»· luáº­t theo thÃ¡ng/nÄƒm</summary>
        public DisciplineStatsDto GetStats(int month, int year)
        {
            try
            {
                var records = _db.DisciplineRecords
                    .Where(d => d.Date.Month == month && d.Date.Year == year)
                    .ToList();

                return new DisciplineStatsDto
                {
                    Month = month,
                    Year = year,
                    TotalRecords = records.Count,
                    DraftCount = records.Count(r => r.Status == "Draft"),
                    PendingCount = records.Count(r => r.Status == "Pending"),
                    ApprovedCount = records.Count(r => r.Status == "Approved"),
                    ArchivedCount = records.Count(r => r.Status == "Archived"),
                    LightCount = records.Count(r => r.ViolationType == "Nháº¹"),
                    MediumCount = records.Count(r => r.ViolationType == "Trung bÃ¬nh"),
                    SevereCount = records.Count(r => r.ViolationType == "Náº·ng"),
                    CriticalCount = records.Count(r => r.ViolationType == "Äáº·c biá»‡t nghiÃªm trá»ng"),
                    TopClasses = records
                        .GroupBy(r => r.ClassName)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToDictionary(g => g.Key, g => g.Count())
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DisciplineService: GetStats failed for {Month}/{Year}", month, year);
                return new DisciplineStatsDto { Month = month, Year = year };
            }
        }
    }

    /// <summary>DTO thá»‘ng kÃª ká»· luáº­t</summary>
    public class DisciplineStatsDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalRecords { get; set; }
        public int DraftCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int ArchivedCount { get; set; }
        public int LightCount { get; set; }
        public int MediumCount { get; set; }
        public int SevereCount { get; set; }
        public int CriticalCount { get; set; }
        public Dictionary<string, int> TopClasses { get; set; } = new();
    }
}

