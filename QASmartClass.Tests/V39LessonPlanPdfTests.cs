using System;
using System.IO;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V39LessonPlanPdfTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;
        private readonly string _tempPdfPath;

        public V39LessonPlanPdfTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            if (System.Windows.Application.Current == null)
            {
                try { new System.Windows.Application(); } catch { }
            }
            AppServices.UIService = new MockUserInterfaceService();

            string guid = Guid.NewGuid().ToString("N");
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_v39_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_v39_{guid}.txt");
            _tempPdfPath = Path.Combine(AppPaths.RootDir, $"GiaoAn_Test_{guid}.pdf");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.36.0");
        }

        [Fact]
        public void TestExportLessonPlan_GeneratesValidPdfFile()
        {
            using (var db = new AppDbContext())
            {
                var pdfService = new PdfExportService(db);
                
                string subject = "Toán";
                string grade = "10";
                string title = "Phương trình bậc hai";
                string content = "I. Mục tiêu bài học\nNắm vững công thức nghiệm thu của phương trình bậc hai.\n\nII. Tiến trình\n1. Khởi động\n2. Hình thành kiến thức.";

                // Export to temp path
                string exportedPath = pdfService.ExportLessonPlan(subject, grade, title, content, _tempPdfPath);

                // Assertions
                Assert.Equal(_tempPdfPath, exportedPath);
                Assert.True(File.Exists(exportedPath), "Tệp PDF phải được tạo ra thực tế.");
                
                var fileInfo = new FileInfo(exportedPath);
                Assert.True(fileInfo.Length > 0, "Kích thước tệp PDF phải lớn hơn 0 byte.");
            }
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
                if (File.Exists(_tempVersionPath)) File.Delete(_tempVersionPath);
                if (File.Exists(_tempPdfPath)) File.Delete(_tempPdfPath);
            }
            catch { }
        }
    }
}
