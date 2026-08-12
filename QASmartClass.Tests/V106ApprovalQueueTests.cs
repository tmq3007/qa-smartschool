using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using QASmartClass.Services;
using QASmartClass.Data;
using QASmartClass.Leadership.ViewModels;

namespace QASmartClass.Tests
{
    [Collection("Sequential")]
    public class V106ApprovalQueueTests
    {
        [Fact]
        public async Task Test_ApprovalQueue_ApproveLesson_WritesAuditLog_Test()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = tempDir;
            AppPaths.EnsureDirectories();
            DbEncryptionKeyManager.ClearCache();

            try
            {
                int lessonId;
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();

                    var lesson = new Lesson
                    {
                        Title = "Bài 1: Khái niệm hàm số",
                        Subject = "Toán",
                        TeacherName = "Nguyễn Văn A",
                        ClassName = "10A",
                        Status = "PendingApproval",
                        UpdatedAt = DateTime.Now
                    };
                    db.Lessons.Add(lesson);
                    db.SaveChanges();
                    lessonId = lesson.Id;
                }

                var vm = new ApprovalQueueViewModel();
                await vm.LoadPendingLessonsCommand.ExecuteAsync(null);

                Assert.Single(vm.PendingLessons);
                Assert.Equal("Bài 1: Khái niệm hàm số", vm.PendingLessons[0].Title);

                vm.SelectedLesson = vm.PendingLessons[0];
                vm.ReviewNotes = "Bài soạn tốt, duyệt giảng dạy.";
                await vm.ApproveLessonCommand.ExecuteAsync(null);

                using (var db = new AppDbContext())
                {
                    var l = db.Lessons.Find(lessonId);
                    Assert.NotNull(l);
                    Assert.Equal("Approved", l.Status);
                    Assert.Equal("Bài soạn tốt, duyệt giảng dạy.", l.ReviewNotes);

                    var audit = db.AuditLogs.FirstOrDefault(a => a.Action == "Lesson_Approved");
                    Assert.NotNull(audit);
                    Assert.Contains("Bài 1: Khái niệm hàm số", audit.Details);
                    Assert.Contains("Nguyễn Văn A", audit.Details);
                    Assert.Contains("Bài soạn tốt, duyệt giảng dạy.", audit.Details);
                }
            }
            finally
            {
                CleanTempDir(tempDir);
            }
        }

        [Fact]
        public async Task Test_ApprovalQueue_RejectLesson_RequiresNotes_Test()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = tempDir;
            AppPaths.EnsureDirectories();
            DbEncryptionKeyManager.ClearCache();

            try
            {
                int lessonId;
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();

                    var lesson = new Lesson
                    {
                        Title = "Bài 2: Hóa học vô cơ",
                        Subject = "Hóa học",
                        TeacherName = "Trần Thị B",
                        ClassName = "11B",
                        Status = "PendingApproval",
                        UpdatedAt = DateTime.Now
                    };
                    db.Lessons.Add(lesson);
                    db.SaveChanges();
                    lessonId = lesson.Id;
                }

                var vm = new ApprovalQueueViewModel();
                await vm.LoadPendingLessonsCommand.ExecuteAsync(null);

                vm.SelectedLesson = vm.PendingLessons[0];
                
                // 1. Reject without notes (should fail)
                vm.ReviewNotes = "";
                await vm.RejectLessonCommand.ExecuteAsync(null);

                Assert.Equal("Vui lòng nhập lý do từ chối vào ô Ghi chú.", vm.StatusMessage);

                using (var db = new AppDbContext())
                {
                    var l = db.Lessons.Find(lessonId);
                    Assert.Equal("PendingApproval", l.Status); // status remains unchanged
                }

                // 2. Reject with notes (should succeed)
                vm.ReviewNotes = "Thiếu phần bài tập vận dụng.";
                await vm.RejectLessonCommand.ExecuteAsync(null);

                using (var db = new AppDbContext())
                {
                    var l = db.Lessons.Find(lessonId);
                    Assert.Equal("Draft", l.Status); // status changed to Draft

                    var audit = db.AuditLogs.FirstOrDefault(a => a.Action == "Lesson_Rejected");
                    Assert.NotNull(audit);
                    Assert.Contains("Bài 2: Hóa học vô cơ", audit.Details);
                    Assert.Contains("Trần Thị B", audit.Details);
                    Assert.Contains("Thiếu phần bài tập vận dụng.", audit.Details);
                }
            }
            finally
            {
                CleanTempDir(tempDir);
            }
        }

        [Fact]
        public async Task Test_ApprovalQueue_Filter_ByTeacherOrSubject_Test()
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            AppPaths.DataDirOverride = tempDir;
            AppPaths.EnsureDirectories();
            DbEncryptionKeyManager.ClearCache();

            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();

                    db.Lessons.Add(new Lesson { Title = "Hàm số lượng giác", Subject = "Toán", TeacherName = "Nguyễn Văn A", ClassName = "11A", Status = "PendingApproval" });
                    db.Lessons.Add(new Lesson { Title = "Quang hình học", Subject = "Vật lý", TeacherName = "Nguyễn Văn A", ClassName = "11A", Status = "PendingApproval" });
                    db.Lessons.Add(new Lesson { Title = "Thì hiện tại hoàn thành", Subject = "Tiếng Anh", TeacherName = "Phạm Thị C", ClassName = "10B", Status = "PendingApproval" });
                    db.SaveChanges();
                }

                var vm = new ApprovalQueueViewModel();
                await vm.LoadPendingLessonsCommand.ExecuteAsync(null);

                Assert.Equal(3, vm.PendingLessons.Count);

                // Filter by Subject
                vm.SelectedSubject = "Toán";
                Assert.Single(vm.PendingLessons);
                Assert.Equal("Hàm số lượng giác", vm.PendingLessons[0].Title);

                // Reset Subject filter
                vm.SelectedSubject = "Tất cả";
                Assert.Equal(3, vm.PendingLessons.Count);

                // Filter by Search Text (Teacher name)
                vm.SearchText = "Nguyễn";
                Assert.Equal(2, vm.PendingLessons.Count);
                Assert.All(vm.PendingLessons, l => Assert.Equal("Nguyễn Văn A", l.TeacherName));

                // Filter by Search Text (Class name)
                vm.SearchText = "10B";
                Assert.Single(vm.PendingLessons);
                Assert.Equal("Thì hiện tại hoàn thành", vm.PendingLessons[0].Title);
            }
            finally
            {
                CleanTempDir(tempDir);
            }
        }

        private void CleanTempDir(string dir)
        {
            try
            {
                if (System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.Delete(dir, true);
                }
            }
            catch { }
            AppPaths.DataDirOverride = null;
            DbEncryptionKeyManager.ClearCache();
        }
    }
}
