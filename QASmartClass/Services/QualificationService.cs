using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// DTO t?ng h?p tŕnh d? GV
    /// </summary>
    public class QualificationSummaryDto
    {
        public int TotalStaff { get; set; }
        public int BachelorCount { get; set; }
        public double BachelorPercent { get; set; }
        public int MasterCount { get; set; }
        public double MasterPercent { get; set; }
        public int DoctorateCount { get; set; }
        public double DoctoratePercent { get; set; }
        public int OtherCount { get; set; }
        public double OtherPercent { get; set; }
    }

    /// <summary>
    /// P4-WI04: Qu?n lư b?ng c?p / ch?ng ch? giáo viên
    /// </summary>
    public class QualificationService
    {
        private readonly AppDbContext _db;

        // Allowed file extensions for certificate uploads
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public QualificationService(AppDbContext db)
        {
            _db = db;
        }

        // --------------- QUALIFICATION CRUD ---------------

        /// <summary>Thêm b?ng c?p / ch?ng ch? m?i</summary>
        public TeacherQualification? AddQualification(TeacherQualification q)
        {
            try
            {
                // Validate StaffId
                var staffExists = _db.StaffProfiles.Any(s => s.Id == q.StaffId);
                if (!staffExists)
                {
                    Log.Warning("AddQualification: StaffId {StaffId} không t?n tại", q.StaffId);
                    return null;
                }

                // Validate file if provided
                if (!string.IsNullOrEmpty(q.FilePath) && !ValidateFile(q.FilePath))
                {
                    Log.Warning("AddQualification: File không h?p l?: {FilePath}", q.FilePath);
                    return null;
                }

                _db.TeacherQualifications.Add(q);
                _db.SaveChanges();
                Log.Information("AddQualification: Đã thêm b?ng c?p Id={Id} cho StaffId={StaffId}", q.Id, q.StaffId);
                return q;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AddQualification: Lỗi khi thêm bằng cấp cho StaffId={StaffId}", q.StaffId);
                return null;
            }
        }

        /// <summary>C?p nh?t b?ng c?p / ch?ng ch?</summary>
        public bool UpdateQualification(TeacherQualification q)
        {
            try
            {
                var existing = _db.TeacherQualifications.Find(q.Id);
                if (existing == null)
                {
                    Log.Warning("UpdateQualification: Không t́m th?y Id={Id}", q.Id);
                    return false;
                }

                // Validate StaffId
                var staffExists = _db.StaffProfiles.Any(s => s.Id == q.StaffId);
                if (!staffExists)
                {
                    Log.Warning("UpdateQualification: StaffId {StaffId} không t?n tại", q.StaffId);
                    return false;
                }

                // Validate file if changed
                if (!string.IsNullOrEmpty(q.FilePath) && q.FilePath != existing.FilePath && !ValidateFile(q.FilePath))
                {
                    Log.Warning("UpdateQualification: File không h?p l?: {FilePath}", q.FilePath);
                    return false;
                }

                existing.QualificationType = q.QualificationType;
                existing.InstitutionName = q.InstitutionName;
                existing.Major = q.Major;
                existing.IssuedDate = q.IssuedDate;
                existing.ExpiryDate = q.ExpiryDate;
                existing.CertificateNumber = q.CertificateNumber;
                existing.FilePath = q.FilePath;
                existing.Status = q.Status;

                _db.SaveChanges();
                Log.Information("UpdateQualification: Đã c?p nh?t Id={Id}", q.Id);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpdateQualification: Lỗi khi cập nhật Id={Id}", q.Id);
                return false;
            }
        }

        /// <summary>Xóa b?ng c?p / ch?ng ch?</summary>
        public bool DeleteQualification(int id)
        {
            try
            {
                var existing = _db.TeacherQualifications.Find(id);
                if (existing == null)
                {
                    Log.Warning("DeleteQualification: Không t́m th?y Id={Id}", id);
                    return false;
                }

                _db.TeacherQualifications.Remove(existing);
                _db.SaveChanges();
                Log.Information("DeleteQualification: Đã xóa Id={Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DeleteQualification: Lỗi khi xóa Id={Id}", id);
                return false;
            }
        }

        /// <summary>L?y danh sách b?ng c?p theo nhân viên</summary>
        public List<TeacherQualification> GetByStaff(int staffId)
        {
            try
            {
                return _db.TeacherQualifications
                    .Where(q => q.StaffId == staffId)
                    .OrderByDescending(q => q.IssuedDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetByStaff: Lỗi khi lấy bằng cấp StaffId={StaffId}", staffId);
                return new List<TeacherQualification>();
            }
        }

        /// <summary>L?y danh sách ch?ng ch? s?p h?t h?n trong N ngày tại</summary>
        public List<TeacherQualification> GetExpiringCertificates(int daysAhead)
        {
            try
            {
                var deadline = DateTime.Now.AddDays(daysAhead);
                return _db.TeacherQualifications
                    .Where(q => q.ExpiryDate != null
                             && q.ExpiryDate <= deadline
                             && q.ExpiryDate >= DateTime.Now
                             && q.Status == "Active")
                    .OrderBy(q => q.ExpiryDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetExpiringCertificates: Lỗi khi lấy chứng chỉ sắp hết hạn");
                return new List<TeacherQualification>();
            }
        }

        // --------------- TRAINING CRUD ---------------

        /// <summary>Thêm l?ch s? dào t?o / b?i du?ng</summary>
        public TrainingHistory? AddTraining(TrainingHistory t)
        {
            try
            {
                // Validate StaffId
                var staffExists = _db.StaffProfiles.Any(s => s.Id == t.StaffId);
                if (!staffExists)
                {
                    Log.Warning("AddTraining: StaffId {StaffId} không t?n tại", t.StaffId);
                    return null;
                }

                // Validate file if provided
                if (!string.IsNullOrEmpty(t.CertificatePath) && !ValidateFile(t.CertificatePath))
                {
                    Log.Warning("AddTraining: File không h?p l?: {FilePath}", t.CertificatePath);
                    return null;
                }

                _db.TrainingHistories.Add(t);
                _db.SaveChanges();
                Log.Information("AddTraining: Đã thêm dào t?o Id={Id} cho StaffId={StaffId}", t.Id, t.StaffId);
                return t;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AddTraining: Lỗi khi thêm đào tạo cho StaffId={StaffId}", t.StaffId);
                return null;
            }
        }

        /// <summary>L?y l?ch s? dào t?o theo nhân viên</summary>
        public List<TrainingHistory> GetTrainingsByStaff(int staffId)
        {
            try
            {
                return _db.TrainingHistories
                    .Where(t => t.StaffId == staffId)
                    .OrderByDescending(t => t.StartDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetTrainingsByStaff: Lỗi khi lấy đào tạo StaffId={StaffId}", staffId);
                return new List<TrainingHistory>();
            }
        }

        // --------------- SUMMARY / STATISTICS ---------------

        /// <summary>Th?ng kê % GV theo tŕnh d? (C? nhân, Th?c si, Ti?n si)</summary>
        public QualificationSummaryDto GetQualificationSummary()
        {
            try
            {
                // L?y t?t c? b?ng c?p Active, nhóm theo StaffId d? l?y tŕnh d? cao nh?t
                var qualifications = _db.TeacherQualifications
                    .Where(q => q.Status == "Active")
                    .ToList();

                // L?y tŕnh d? cao nh?t c?a m?i nhân viên
                var staffHighest = qualifications
                    .GroupBy(q => q.StaffId)
                    .Select(g => new
                    {
                        StaffId = g.Key,
                        HighestType = GetHighestQualification(g.Select(q => q.QualificationType).ToList())
                    })
                    .ToList();

                int total = staffHighest.Count;
                if (total == 0)
                {
                    return new QualificationSummaryDto { TotalStaff = 0 };
                }

                int bachelor = staffHighest.Count(s => s.HighestType == "C? nhân");
                int master = staffHighest.Count(s => s.HighestType == "Th?c si");
                int doctorate = staffHighest.Count(s => s.HighestType == "Ti?n si");
                int other = total - bachelor - master - doctorate;

                return new QualificationSummaryDto
                {
                    TotalStaff = total,
                    BachelorCount = bachelor,
                    BachelorPercent = Math.Round((double)bachelor / total * 100, 1),
                    MasterCount = master,
                    MasterPercent = Math.Round((double)master / total * 100, 1),
                    DoctorateCount = doctorate,
                    DoctoratePercent = Math.Round((double)doctorate / total * 100, 1),
                    OtherCount = other,
                    OtherPercent = Math.Round((double)other / total * 100, 1)
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetQualificationSummary: Lỗi khi thống kê trình độ");
                return new QualificationSummaryDto();
            }
        }

        // --------------- PRIVATE HELPERS ---------------

        /// <summary>Validate file upload: jpg/png/pdf, max 5MB</summary>
        private bool ValidateFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            if (!File.Exists(filePath))
            {
                Log.Warning("ValidateFile: File không t?n tại: {FilePath}", filePath);
                return false;
            }

            // Check extension
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                Log.Warning("ValidateFile: Extension không h?p l?: {Ext} (cho phép: jpg/png/pdf)", ext);
                return false;
            }

            // Check file size
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxFileSize)
            {
                Log.Warning("ValidateFile: File quá l?n: {Size} bytes (max 5MB)", fileInfo.Length);
                return false;
            }

            return true;
        }

        /// <summary>Xác d?nh tŕnh d? cao nh?t t? danh sách lo?i b?ng</summary>
        private string GetHighestQualification(List<string> types)
        {
            if (types.Contains("Ti?n si")) return "Ti?n si";
            if (types.Contains("Th?c si")) return "Th?c si";
            if (types.Contains("C? nhân")) return "C? nhân";
            return types.FirstOrDefault() ?? "Khác";
        }
    }
}

