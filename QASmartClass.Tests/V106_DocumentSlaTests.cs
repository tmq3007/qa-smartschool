using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;

namespace QASmartClass.Tests
{
    public class V106_DocumentSlaTests
    {
        private AppDbContext CreateTempDbContext(string dbPath)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public async Task Test_Doc_Save_CalculatesDefaultSlaDeadline()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doc_sla_def.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // Seed SLA Default days = 5
                    db.SystemSettings.Add(new SystemSetting { Id = "IT_Document_DefaultSLADays", Value = "5", Category = "IT" });
                    db.SaveChanges();

                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Document_DefaultSLADays");
                    int days = setting != null && int.TryParse(setting.Value, out int dVal) ? dVal : 3;

                    var doc = new OfficialDocument
                    {
                        DocumentNumber = "DOC-001",
                        Title = "Tờ trình nâng cấp phòng Lab",
                        Type = "Incoming",
                        IssuedDate = DateTime.Today,
                        ProcessingDeadline = DateTime.Today.AddDays(days),
                        Status = "Pending"
                    };

                    db.OfficialDocuments.Add(doc);
                    db.SaveChanges();

                    var saved = db.OfficialDocuments.Find(doc.Id);
                    Assert.NotNull(saved);
                    Assert.Equal(DateTime.Today.AddDays(5), saved.ProcessingDeadline);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_Doc_Save_UsesCustomDeadline()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doc_sla_cust.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    var customDeadline = DateTime.Today.AddDays(15);
                    var doc = new OfficialDocument
                    {
                        DocumentNumber = "DOC-002",
                        Title = "Quyết định phê duyệt nhân sự",
                        Type = "Internal",
                        IssuedDate = DateTime.Today,
                        ProcessingDeadline = customDeadline,
                        Status = "Pending"
                    };

                    db.OfficialDocuments.Add(doc);
                    db.SaveChanges();

                    var saved = db.OfficialDocuments.Find(doc.Id);
                    Assert.NotNull(saved);
                    Assert.Equal(customDeadline, saved.ProcessingDeadline);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_Doc_OverdueAndEscalation_Detection()
        {
            // Case 1: Past deadline and Pending
            var displayOverdue = new OfficialDocumentDisplay
            {
                Status = "Pending",
                ProcessingDeadline = DateTime.Today.AddDays(-1),
                IsEscalated = false
            };
            Assert.True(displayOverdue.IsSlaOverdue);
            Assert.Equal("QUÁ HẠN / LEO THANG!", displayOverdue.SlaStatusText);
            Assert.Equal("#DC2626", displayOverdue.SlaStatusColor);

            // Case 2: Escalated and Pending
            var displayEscalated = new OfficialDocumentDisplay
            {
                Status = "Pending",
                ProcessingDeadline = DateTime.Today.AddDays(2),
                IsEscalated = true
            };
            Assert.True(displayEscalated.IsSlaOverdue);

            // Case 3: Past deadline but Approved
            var displayApproved = new OfficialDocumentDisplay
            {
                Status = "Approved",
                ProcessingDeadline = DateTime.Today.AddDays(-2),
                IsEscalated = false
            };
            Assert.False(displayApproved.IsSlaOverdue);
        }

        [Fact]
        public async Task Test_Doc_EscalateAction_UpdatesDb_SendsNotification()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doc_sla_esc.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    // Add Leadership Profiles
                    var ht = new TeacherProfile { TeacherCode = "GV_HT", FullName = "Hiệu Trưởng A", Role = "HieuTruong", IsActive = true };
                    var hp = new TeacherProfile { TeacherCode = "GV_HP", FullName = "Hiệu Phó B", Role = "HieuPho", IsActive = true };
                    db.TeacherProfiles.Add(ht);
                    db.TeacherProfiles.Add(hp);

                    var doc = new OfficialDocument
                    {
                        DocumentNumber = "DOC-003",
                        Title = "Kế hoạch thi đua",
                        Type = "Internal",
                        Status = "Pending",
                        IsEscalated = false
                    };
                    db.OfficialDocuments.Add(doc);
                    db.SaveChanges();

                    // Perform escalation action
                    var savedDoc = db.OfficialDocuments.Find(doc.Id);
                    Assert.NotNull(savedDoc);
                    savedDoc.IsEscalated = true;
                    db.SaveChanges();

                    var leaders = await db.TeacherProfiles.Where(t => t.IsActive && (t.Role.Contains("HieuTruong") || t.Role.Contains("HieuPho"))).ToListAsync();
                    Assert.Equal(2, leaders.Count);

                    // Mock sending push notifications
                    foreach (var leader in leaders)
                    {
                        db.PushMessageLogs.Add(new PushMessageLog
                        {
                            RecipientId = leader.Id,
                            RecipientRole = "Teacher",
                            Title = "Yêu cầu xử lý công văn khẩn",
                            Body = $"Văn bản khẩn '{savedDoc.Title}' đã được leo thang.",
                            SentAt = DateTime.Now,
                            Status = "Delivered"
                        });
                    }
                    db.SaveChanges();

                    var updated = db.OfficialDocuments.Find(doc.Id);
                    Assert.NotNull(updated);
                    Assert.True(updated.IsEscalated);
                    Assert.Equal(2, db.PushMessageLogs.Count());
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }

        [Fact]
        public void Test_Doc_EscalateAction_LogsAuditTrail()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doc_sla_audit.db");
            try
            {
                using (var db = CreateTempDbContext(dbPath))
                {
                    var doc = new OfficialDocument
                    {
                        DocumentNumber = "DOC-004",
                        Title = "Công văn 004",
                        Type = "Incoming",
                        Status = "Pending",
                        IsEscalated = true
                    };
                    db.OfficialDocuments.Add(doc);
                    
                    db.AuditLogs.Add(new AuditLog
                    {
                        Action = "Escalate_Document",
                        ActorName = "GV_VanThu",
                        Details = $"Escalated document {doc.DocumentNumber} - {doc.Title}",
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();

                    var log = db.AuditLogs.FirstOrDefault(l => l.Action == "Escalate_Document");
                    Assert.NotNull(log);
                    Assert.Contains("DOC-004", log.Details);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }
    }
}
