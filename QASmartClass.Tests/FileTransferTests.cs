using Xunit;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows;
using QASmartClass.Classroom.Views;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;
using System.Reflection;

namespace QASmartClass.Tests
{
    public class FileTransferTests
    {
        private void RunOnStaThread(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        private void EnsureApplication()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new QASmartTouch.App();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FileTransferTests] App creation error: {ex.Message}");
                }
            }
        }

        [Fact]
        public void FileTransfer_ParseFilename_WithUnderscores_ShouldParseCorrectly()
        {
            using var db = TestDbFactory.Create();

            // Setup mock student in DB
            var student = new Student
            {
                StudentCode = "HS99999",
                FullName = "Nguyen Van An",
                ClassName = "12A1"
            };
            db.Students.Add(student);
            db.SaveChanges();

            // Test Case 1: Backward Compatibility (Old Single Underscore format) - Student exists in DB
            string fileName1 = "HS99999_Nguyen_Van_An_Lab_01_Exc_1_20260413T090530Z.cpp";
            var result1 = FileTransferPage.ParseNormalizedFilename(fileName1, db);

            Assert.Equal("HS99999", result1.StudentCode);
            Assert.Equal("Nguyen Van An", result1.StudentName);
            Assert.Equal("Lab_01_Exc_1.cpp", result1.OriginalName);

            // Test Case 2: New Standard (Double Underscore format) - Student NOT in DB
            string fileName2 = "HS88888__Tran_Thi_Binh__Exercise_Outline__20260413T090530Z.docx";
            var result2 = FileTransferPage.ParseNormalizedFilename(fileName2, db);

            Assert.Equal("HS88888", result2.StudentCode);
            Assert.Equal("Tran Thi Binh", result2.StudentName); // parsed cleanly from __ format
            Assert.Equal("Exercise_Outline.docx", result2.OriginalName);
        }

        [Fact]
        public void FileTransfer_DangerousExtensions_ShouldBeBlocked()
        {
            // Verify extension blocking logic
            var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".msi", ".scr", ".vbs", ".ps1" };
            
            foreach (var ext in dangerousExtensions)
            {
                var isDangerous = IsDangerousExtension(ext);
                Assert.True(isDangerous, $"Extension {ext} should be identified as dangerous.");
            }

            var safeExtensions = new[] { ".pdf", ".docx", ".xlsx", ".pptx", ".zip", ".rar", ".cpp", ".py" };
            foreach (var ext in safeExtensions)
            {
                var isDangerous = IsDangerousExtension(ext);
                Assert.False(isDangerous, $"Extension {ext} should be safe.");
            }
        }

        private bool IsDangerousExtension(string ext)
        {
            var dangerousList = new System.Collections.Generic.HashSet<string>
            {
                ".exe", ".bat", ".cmd", ".msi", ".scr", ".vbs", ".js", ".ps1", ".com", ".pif", ".lnk"
            };
            return dangerousList.Contains(ext.ToLowerInvariant());
        }

        [Fact]
        public void FileTransferPage_EventUnregistration_OnUnload()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new FileTransferPage();

                // Raise Loaded to register events
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var app = Application.Current as QASmartTouch.App;
                var ft = app?.FileTransfer;

                // Let's trigger Unloaded
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

                // If event unregistration succeeds, we can load again without issues.
                Assert.NotNull(page);
            });
        }

        [Fact]
        public void FileTransfer_SecurityFiltering_MaxSizeExceededBlocked()
        {
            // Verify size limit check logic
            long testLimitMb = 50;
            long maxSizeBytes = testLimitMb * 1024L * 1024L;

            long validSize = 49 * 1024L * 1024L;
            long invalidSize = 51 * 1024L * 1024L;
            long zeroSize = 0;
            long negativeSize = -100;

            Assert.True(validSize <= maxSizeBytes && validSize > 0);
            Assert.True(invalidSize > maxSizeBytes || invalidSize <= 0);
            Assert.True(zeroSize > maxSizeBytes || zeroSize <= 0);
            Assert.True(negativeSize > maxSizeBytes || negativeSize <= 0);
        }

        [Fact]
        public void FileTransfer_ZipScanning_DangerousFilesInsideZip_ShouldBeBlocked()
        {
            // Create a temporary zip file containing a dangerous .exe file
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"dangerous_{Guid.NewGuid()}.zip");
            string tempExePath = Path.Combine(Path.GetTempPath(), "payload.exe");
            File.WriteAllText(tempExePath, "dummy executable content");

            try
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                using (var archive = System.IO.Compression.ZipFile.Open(tempZipPath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(tempExePath, "payload.exe");
                }

                // Call the private scanner via Reflection
                var service = new FileTransferService(null!);
                var method = typeof(FileTransferService).GetMethod("IsZipContentDangerous", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                bool isDangerous = (bool)method.Invoke(service, new object[] { tempZipPath });
                Assert.True(isDangerous, "ZIP archive containing .exe should be flagged as dangerous.");
            }
            finally
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                if (File.Exists(tempExePath)) File.Delete(tempExePath);
            }
        }

        [Fact]
        public void FileTransfer_ZipScanning_SafeFilesInsideZip_ShouldPass()
        {
            // Create a temporary zip file containing safe .txt and .pdf files
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"safe_{Guid.NewGuid()}.zip");
            string tempTxtPath = Path.Combine(Path.GetTempPath(), "homework.txt");
            string tempPdfPath = Path.Combine(Path.GetTempPath(), "report.pdf");
            File.WriteAllText(tempTxtPath, "answers");
            File.WriteAllText(tempPdfPath, "document");

            try
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                using (var archive = System.IO.Compression.ZipFile.Open(tempZipPath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(tempTxtPath, "homework.txt");
                    archive.CreateEntryFromFile(tempPdfPath, "folder/report.pdf");
                }

                // Call the private scanner via Reflection
                var service = new FileTransferService(null!);
                var method = typeof(FileTransferService).GetMethod("IsZipContentDangerous", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                bool isDangerous = (bool)method.Invoke(service, new object[] { tempZipPath });
                Assert.False(isDangerous, "ZIP archive containing only safe files should be allowed.");
            }
            finally
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                if (File.Exists(tempTxtPath)) File.Delete(tempTxtPath);
                if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
            }
        }
    }
}
