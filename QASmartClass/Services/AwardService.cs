using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QASmartClass.Services
{
    public class AwardStatsDto
    {
        public int TotalAwards { get; set; }
        public int ApprovedAwards { get; set; }
        public int RejectedAwards { get; set; }
        public int PendingAwards { get; set; }
        public Dictionary<string, int> AwardsByType { get; set; } = new();
    }

    /// <summary>
    /// WI-06: Service quản lý Khen thưởng GV/HS/Lớp
    /// </summary>
    public class AwardService
    {
        private readonly AppDbContext _db;
        private static readonly string[] ValidAwardTypes = { "HSG", "HSTT", "GVDG", "LopTienTien", "GiayKhen", "BangKhen" };

        private static readonly byte[] DefaultKey = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x11 };
        private static readonly byte[] DefaultIV = new byte[] { 0x0F, 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00 };

        private static byte[]? _activeKey;
        private static byte[]? _activeIV;

        private static void InitializeKeys()
        {
            if (_activeKey != null && _activeIV != null) return;

            string secureConfigFile = System.IO.Path.Combine(AppPaths.SettingsDir, "secure_keys.json");
            try
            {
                if (System.IO.File.Exists(secureConfigFile))
                {
                    string json = System.IO.File.ReadAllText(secureConfigFile);
                    var config = System.Text.Json.JsonSerializer.Deserialize<SecureKeysConfig>(json);
                    if (config != null && !string.IsNullOrEmpty(config.EncryptedKey) && !string.IsNullOrEmpty(config.EncryptedIV))
                    {
                        byte[] encKey = Convert.FromBase64String(config.EncryptedKey);
                        byte[] encIV = Convert.FromBase64String(config.EncryptedIV);

                        _activeKey = System.Security.Cryptography.ProtectedData.Unprotect(encKey, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                        _activeIV = System.Security.Cryptography.ProtectedData.Unprotect(encIV, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("AwardService: Failed to decrypt secure keys using DPAPI, regenerating secure storage: {Err}", ex.Message);
            }

            // Fallback: Encrypt default keys using DPAPI and write to config
            try
            {
                byte[] encKey = System.Security.Cryptography.ProtectedData.Protect(DefaultKey, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                byte[] encIV = System.Security.Cryptography.ProtectedData.Protect(DefaultIV, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);

                var config = new SecureKeysConfig
                {
                    EncryptedKey = Convert.ToBase64String(encKey),
                    EncryptedIV = Convert.ToBase64String(encIV)
                };

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(secureConfigFile)!);

                if (System.IO.File.Exists(secureConfigFile))
                {
                    var attrs = System.IO.File.GetAttributes(secureConfigFile);
                    if ((attrs & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                    {
                        System.IO.File.SetAttributes(secureConfigFile, attrs & ~System.IO.FileAttributes.ReadOnly);
                    }
                }

                string json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(secureConfigFile, json);

                System.IO.File.SetAttributes(secureConfigFile, System.IO.FileAttributes.ReadOnly);

                _activeKey = DefaultKey;
                _activeIV = DefaultIV;
                Log.Information("AwardService: DPAPI-secured keys saved to {Path}", secureConfigFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: Failed to encrypt default keys using DPAPI");
                _activeKey = DefaultKey;
                _activeIV = DefaultIV;
            }
        }

        private class SecureKeysConfig
        {
            public string EncryptedKey { get; set; } = string.Empty;
            public string EncryptedIV { get; set; } = string.Empty;
        }

        public static byte[] EncryptBytes(byte[] input)
        {
            InitializeKeys();
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = _activeKey!;
                aes.IV = _activeIV!;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, aes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        cs.Write(input, 0, input.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        public static byte[] DecryptBytes(byte[] input)
        {
            InitializeKeys();
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = _activeKey!;
                aes.IV = _activeIV!;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, aes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        cs.Write(input, 0, input.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        public AwardService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public bool ProposeAward(string targetType, int targetId, string targetName, string awardType, string semester, string schoolYear, string proposedBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(targetType) || string.IsNullOrWhiteSpace(awardType)) return false;

                // Validate awardType
                if (!ValidAwardTypes.Contains(awardType))
                {
                    Log.Warning("AwardService: Invalid award type: {Type}", awardType);
                    return false;
                }

                // Không đề xuất trùng (cùng target, cùng loại, cùng kỳ, cùng năm học)
                var exists = _db.AwardRecords.Any(a => 
                    a.TargetType == targetType &&
                    a.TargetId == targetId &&
                    a.AwardType == awardType &&
                    a.Semester == semester &&
                    a.SchoolYear == schoolYear);
                
                if (exists)
                {
                    Log.Warning("AwardService: Award already proposed for target {TargetId} type {AwardType} semester {Semester}", targetId, awardType, semester);
                    return false;
                }

                var record = new AwardRecord
                {
                    TargetType = targetType,
                    TargetId = targetId,
                    TargetName = targetName ?? string.Empty,
                    AwardType = awardType,
                    Semester = semester ?? string.Empty,
                    SchoolYear = schoolYear ?? string.Empty,
                    ProposedBy = proposedBy ?? string.Empty,
                    ApprovedBy = string.Empty,
                    Status = "Proposed",
                    ProposedDate = DateTime.Now,
                    Notes = string.Empty
                };

                _db.AwardRecords.Add(record);
                _db.SaveChanges();
                Log.Information("AwardService: Proposed award for {TargetName}", targetName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: ProposeAward failed");
                return false;
            }
        }

        public bool ApproveAward(int recordId, string approverName)
        {
            try
            {
                AuthorizationGuard.EnsureRole(UserSessionService.Instance.Role, 
                    StatusConstants.TeacherRole.HieuTruong, 
                    StatusConstants.TeacherRole.Admin);

                var record = _db.AwardRecords.Find(recordId);
                if (record == null) return false;

                // Chỉ approved status Proposed
                if (record.Status != "Proposed")
                {
                    Log.Warning("AwardService: Only Proposed awards can be approved. Current status: {Status}", record.Status);
                    return false;
                }

                record.Status = "Approved";
                record.ApprovedBy = approverName ?? string.Empty;
                record.ApprovedDate = DateTime.Now;

                _db.SaveChanges();
                Log.Information("AwardService: Approved award {Id} by {Approver}", recordId, approverName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: ApproveAward failed");
                throw;
            }
        }

        public bool RejectAward(int recordId, string approverName, string reason)
        {
            try
            {
                AuthorizationGuard.EnsureRole(UserSessionService.Instance.Role, 
                    StatusConstants.TeacherRole.HieuTruong, 
                    StatusConstants.TeacherRole.Admin);

                var record = _db.AwardRecords.Find(recordId);
                if (record == null) return false;

                // Chỉ reject status Proposed
                if (record.Status != "Proposed")
                {
                    Log.Warning("AwardService: Only Proposed awards can be rejected. Current status: {Status}", record.Status);
                    return false;
                }

                record.Status = "Rejected";
                record.ApprovedBy = approverName ?? string.Empty;
                record.ApprovedDate = DateTime.Now;
                record.Notes = reason ?? string.Empty;

                _db.SaveChanges();
                Log.Information("AwardService: Rejected award {Id} by {Approver}. Reason: {Reason}", recordId, approverName, reason);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: RejectAward failed");
                throw;
            }
        }

        public List<AwardRecord> GetPendingAwards()
        {
            try
            {
                return _db.AwardRecords.Where(a => a.Status == "Proposed").OrderByDescending(a => a.ProposedDate).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: GetPendingAwards failed");
                return new List<AwardRecord>();
            }
        }

        public List<AwardRecord> GetApprovedAwards(string schoolYear)
        {
            try
            {
                return _db.AwardRecords
                    .Where(a => a.Status == "Approved" && (string.IsNullOrEmpty(schoolYear) || a.SchoolYear == schoolYear))
                    .OrderByDescending(a => a.ApprovedDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: GetApprovedAwards failed");
                return new List<AwardRecord>();
            }
        }

        public AwardStatsDto GetAwardStatistics(string schoolYear)
        {
            try
            {
                var query = _db.AwardRecords.AsQueryable();
                if (!string.IsNullOrEmpty(schoolYear))
                {
                    query = query.Where(a => a.SchoolYear == schoolYear);
                }

                var list = query.ToList();
                var stats = new AwardStatsDto
                {
                    TotalAwards = list.Count,
                    ApprovedAwards = list.Count(a => a.Status == "Approved"),
                    RejectedAwards = list.Count(a => a.Status == "Rejected"),
                    PendingAwards = list.Count(a => a.Status == "Proposed")
                };

                foreach (var group in list.GroupBy(a => a.AwardType))
                {
                    stats.AwardsByType[group.Key] = group.Count();
                }

                return stats;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: GetAwardStatistics failed");
                return new AwardStatsDto();
            }
        }

        public string PrintCertificate(int awardId)
        {
            try
            {
                var record = _db.AwardRecords.Find(awardId);
                if (record == null || record.Status != "Approved")
                {
                    Log.Warning("AwardService: Award not found or not approved");
                    return string.Empty;
                }

                string outputPath = PdfTemplateHelper.GetOutputPath("GiayKhen", record.TargetName);
                
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        
                        page.Content().Border(3).BorderColor(Colors.Amber.Medium).Padding(20).Column(col =>
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("SỞ GIÁO DỤC VÀ ĐÀO TẠO").FontSize(11).Bold();
                                    c.Item().AlignCenter().Text("TRƯỜNG THCS/THPT QA SMARTSCHOOL").FontSize(11).Bold();
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(11).Bold();
                                    c.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").FontSize(10).Italic();
                                });
                            });
                            
                            col.Item().PaddingTop(40).AlignCenter().Text("QUYẾT ĐỊNH KHEN THƯỞNG").FontSize(24).Bold().FontColor(Colors.Red.Darken2);
                            col.Item().AlignCenter().Text("HIỆU TRƯỞNG TRƯỜNG THCS/THPT QA SMARTSCHOOL").FontSize(14).Italic();
                            
                            col.Item().PaddingTop(30).AlignCenter().Text("TẶNG GIẤY KHEN").FontSize(28).Bold().FontColor(Colors.Amber.Darken3);
                            
                            string targetDesc = record.TargetType == "Student" ? "Học sinh" : (record.TargetType == "Teacher" ? "Giáo viên" : "Tập thể lớp");
                            col.Item().PaddingTop(20).AlignCenter().Text(text =>
                            {
                                text.Span("Cho: ").FontSize(14);
                                text.Span(record.TargetName).FontSize(18).Bold();
                            });

                            string awardDesc = QASmartClass.Leadership.Views.AwardManagementView.MapAwardTypeDesc(record.AwardType);
                            string reason = $"Đã có thành tích xuất sắc trong học tập và rèn luyện - Đạt danh hiệu {awardDesc} học kỳ {record.Semester} năm học {record.SchoolYear}";
                            col.Item().PaddingTop(15).AlignCenter().Text(reason).FontSize(14);
                            
                            col.Item().PaddingTop(40).Row(row =>
                            {
                                row.RelativeItem();
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().AlignCenter().Text($"Hà Nội, ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}").FontSize(10).Italic();
                                    c.Item().AlignCenter().Text("HIỆU TRƯỞNG").FontSize(12).Bold();
                                    c.Item().AlignCenter().Text("(Ký tên, đóng dấu)").FontSize(9).Italic();
                                    string encSigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "hieutruong_signature.enc");
                                    if (System.IO.File.Exists(encSigPath))
                                    {
                                        try
                                        {
                                            byte[] encBytes = System.IO.File.ReadAllBytes(encSigPath);
                                            byte[] decBytes = DecryptBytes(encBytes);
                                            c.Item().AlignCenter().Height(50).Image(decBytes);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex, "AwardService: Failed to decrypt signature image");
                                            c.Item().Height(50);
                                        }
                                    }
                                    else
                                    {
                                        c.Item().Height(50);
                                    }
                                    c.Item().AlignCenter().Text(record.ApprovedBy).FontSize(12).Bold();
                                });
                            });
                        });
                    });
                }).GeneratePdf(outputPath);

                // Cập nhật trạng thái thành Printed
                record.Status = "Printed";
                _db.SaveChanges();
                
                Log.Information("AwardService: Printed certificate to {Path}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AwardService: PrintCertificate failed");
                return string.Empty;
            }
        }
    }
}

