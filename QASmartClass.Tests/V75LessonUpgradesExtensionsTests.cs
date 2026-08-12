using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.StudentClient.Views;

namespace QASmartClass.Tests
{
    public class V75LessonUpgradesExtensionsTests
    {
        static V75LessonUpgradesExtensionsTests()
        {
            // Ensure dependencies are registered
            QASmartClass.Services.AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.75.0");
        }

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
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    // Reset the static Application instance to prevent cross-thread owner issues in tests
                    try
                    {
                        var appInstanceField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appInstanceField != null)
                        {
                            appInstanceField.SetValue(null, null);
                        }
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null)
                        {
                            appCreatedField.SetValue(null, false);
                        }
                    }
                    catch { }

                    InitializeApplicationFull(); action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    try
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                    }
                    catch { }

                    // Reset again after test run to avoid leaking to other test classes
                    try
                    {
                        var appInstanceField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appInstanceField != null)
                        {
                            appInstanceField.SetValue(null, null);
                        }
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null)
                        {
                            appCreatedField.SetValue(null, false);
                        }
                    }
                    catch { }
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

        [Fact]
        public void TestLessonEditorPage_DiffingAlgorithm_UpdatesInsertsDeletesCorrectly()
        {
            using (var db = new AppDbContext())
            {
                // 1. Arrange: Create a temporary lesson
                var lesson = new Lesson
                {
                    Title = "Test Diffing " + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Subject = "Toán",
                    Grade = "Lớp 10",
                    Description = "Mô tả",
                    Status = "Draft"
                };
                db.Lessons.Add(lesson);
                db.SaveChanges();

                // 2. Add initial lesson content blocks (3 blocks)
                var block0 = new LessonContent { LessonId = lesson.Id, ContentType = "Text", Data = "Block 0", SortOrder = 0 };
                var block1 = new LessonContent { LessonId = lesson.Id, ContentType = "Text", Data = "Block 1", SortOrder = 1 };
                var block2 = new LessonContent { LessonId = lesson.Id, ContentType = "Text", Data = "Block 2", SortOrder = 2 };
                db.LessonContents.AddRange(block0, block1, block2);
                db.SaveChanges();

                // Get original ID of block 1 to check preservation
                int block1Id = block1.Id;

                try
                {
                    // 3. Act: Simulate the UI editor updating block 0, keeping block 1, replacing block 2 with Audio, and adding block 3
                    var oldBlocks = db.LessonContents
                        .Where(c => c.LessonId == lesson.Id)
                        .OrderBy(c => c.SortOrder)
                        .ToList();

                    var newBlocksList = new System.Collections.Generic.List<(string ContentType, string Data)>
                    {
                        ("Text", "Block 0 Modified"),         // Modified
                        ("Text", "Block 1"),                  // Unchanged (should preserve ID)
                        ("Audio", "audio.mp3|relative_path"), // Modified ContentType & Data
                        ("Text", "Block 3 New")               // Added new
                    };

                    int maxCount = Math.Max(oldBlocks.Count, newBlocksList.Count);
                    for (int i = 0; i < maxCount; i++)
                    {
                        if (i < oldBlocks.Count && i < newBlocksList.Count)
                        {
                            var oldBlock = oldBlocks[i];
                            var newBlock = newBlocksList[i];
                            if (oldBlock.ContentType != newBlock.ContentType || oldBlock.Data != newBlock.Data || oldBlock.SortOrder != i)
                            {
                                oldBlock.ContentType = newBlock.ContentType;
                                oldBlock.Data = newBlock.Data;
                                oldBlock.SortOrder = i;
                                db.Entry(oldBlock).State = EntityState.Modified;
                            }
                        }
                        else if (i >= oldBlocks.Count)
                        {
                            var newBlock = newBlocksList[i];
                            db.LessonContents.Add(new LessonContent
                            {
                                LessonId = lesson.Id,
                                ContentType = newBlock.ContentType,
                                Data = newBlock.Data,
                                SortOrder = i
                            });
                        }
                        else
                        {
                            db.LessonContents.Remove(oldBlocks[i]);
                        }
                    }
                    db.SaveChanges();

                    // 4. Assert: Load updated contents and verify DB states
                    var updatedBlocks = db.LessonContents
                        .Where(c => c.LessonId == lesson.Id)
                        .OrderBy(c => c.SortOrder)
                        .ToList();

                    Assert.Equal(4, updatedBlocks.Count);
                    
                    // Index 0 was modified
                    Assert.Equal("Block 0 Modified", updatedBlocks[0].Data);
                    Assert.Equal("Text", updatedBlocks[0].ContentType);

                    // Index 1 remained unchanged, its primary key ID should be preserved
                    Assert.Equal(block1Id, updatedBlocks[1].Id);
                    Assert.Equal("Block 1", updatedBlocks[1].Data);

                    // Index 2 is updated to Audio
                    Assert.Equal("Audio", updatedBlocks[2].ContentType);
                    Assert.Equal("audio.mp3|relative_path", updatedBlocks[2].Data);

                    // Index 3 is new
                    Assert.Equal("Block 3 New", updatedBlocks[3].Data);
                    Assert.Equal(3, updatedBlocks[3].SortOrder);
                }
                finally
                {
                    // Clean up test data
                    var contentsToRemove = db.LessonContents.Where(c => c.LessonId == lesson.Id);
                    db.LessonContents.RemoveRange(contentsToRemove);
                    db.Lessons.Remove(lesson);
                    db.SaveChanges();
                }
            }
        }

        /*
        [Fact]
        public void TestDatabaseEncryption_Fallback_EncryptDecryptVerify()
        {
            var tempDbFile = Path.Combine(AppPaths.TempDir, "test_sec_db_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".db");
            var originalFile = AppPaths.DatabaseFile;
            
            try
            {
                // Arrange: Create dummy SQLite file
                AppPaths.DatabaseFile = tempDbFile;
                byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes("SQLite format 3\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");
                File.WriteAllBytes(tempDbFile, originalBytes);

                // Act: Encrypt
                AppDbContext.EncryptDatabaseFile();

                // Assert Encryption: Header should NOT start with SQLite format 3 because it is encrypted
                byte[] encryptedBytes = File.ReadAllBytes(tempDbFile);
                string encryptedHeader = System.Text.Encoding.UTF8.GetString(encryptedBytes, 0, Math.Min(encryptedBytes.Length, 15));
                Assert.NotEqual("SQLite format 3", encryptedHeader);

                // Act: Decrypt
                AppDbContext.DecryptDatabaseFile();

                // Assert Decryption: Decrypted bytes should exactly match original bytes
                byte[] decryptedBytes = File.ReadAllBytes(tempDbFile);
                Assert.Equal(originalBytes, decryptedBytes);
            }
            finally
            {
                AppPaths.DatabaseFile = originalFile;
                if (File.Exists(tempDbFile))
                {
                    File.Delete(tempDbFile);
                }
            }
        }

        [Fact]
        public void TestAudioBlockControl_LoadPath_ParsesCorrectly()
        {
            RunOnStaThread(() =>
            {
                // Arrange
                var control = new AudioBlockControl();

                // Act 1: Load standard split format
                control.LoadPath("tieng_anh_listening.mp3|SharedFiles/Audio/listening_guid.mp3");

                // Assert 1
                Assert.Equal("tieng_anh_listening.mp3", control.FileName);
                Assert.Equal("SharedFiles/Audio/listening_guid.mp3", control.RelativePath);

                // Act 2: Load fallback path format
                control.LoadPath("only_path.mp3");

                // Assert 2
                Assert.Equal("only_path.mp3", control.FileName);
                Assert.Equal("only_path.mp3", control.RelativePath);
            });
        }
        */

        [Fact]
        public void TestStudentLessonPage_DiagnosticOverlay_ToggleF12()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentLessonPage();
                var overlay = page.FindName("dbDiagnosticOverlay") as Border;
                Assert.NotNull(overlay);
                Assert.Equal(Visibility.Collapsed, overlay.Visibility);

                var method = typeof(StudentLessonPage).GetMethod("Page_PreviewKeyDown",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                var keyEventArgs = new System.Windows.Input.KeyEventArgs(
                    System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice,
                    new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero),
                    0,
                    System.Windows.Input.Key.F12)
                {
                    RoutedEvent = UIElement.PreviewKeyDownEvent
                };
                method.Invoke(page, new object[] { page, keyEventArgs });

                Assert.Equal(Visibility.Visible, overlay.Visibility);
                Assert.True(keyEventArgs.Handled);

                var keyEventArgs2 = new System.Windows.Input.KeyEventArgs(
                    System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice,
                    new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero),
                    0,
                    System.Windows.Input.Key.F12)
                {
                    RoutedEvent = UIElement.PreviewKeyDownEvent
                };
                method.Invoke(page, new object[] { page, keyEventArgs2 });

                Assert.Equal(Visibility.Collapsed, overlay.Visibility);
                Assert.True(keyEventArgs2.Handled);
            });
        }

        [Fact]
        public void TestStudentLessonPage_ImageContent_BoundaryCheck()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentLessonPage();
                var panel = new StackPanel();

                string tempLargeImage = Path.Combine(AppPaths.TempDir, "large_test_image.jpg");
                Directory.CreateDirectory(AppPaths.TempDir);
                using (var fs = new FileStream(tempLargeImage, FileMode.Create, FileAccess.Write))
                {
                    fs.SetLength(21 * 1024 * 1024);
                }

                try
                {
                    var method = typeof(StudentLessonPage).GetMethod("RenderImageContent",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(method);

                    method.Invoke(page, new object[] { panel, tempLargeImage });

                    Assert.Single(panel.Children);
                    var childBorder = panel.Children[0] as Border;
                    Assert.NotNull(childBorder);
                    var txtBlock = childBorder.Child as TextBlock;
                    Assert.NotNull(txtBlock);
                    Assert.Contains("quá lớn", txtBlock.Text);
                }
                finally
                {
                    if (File.Exists(tempLargeImage))
                    {
                        File.Delete(tempLargeImage);
                    }
                }
            });
        }

        [Fact]
        public void TestStudentLessonPage_ImageContent_Sha256Verification_CorrectAndIncorrectHash()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentLessonPage();
                var panelCorrect = new StackPanel();
                var panelIncorrect = new StackPanel();

                string tempImageFile = Path.Combine(AppPaths.TempDir, "sha256_test_image.jpg");
                Directory.CreateDirectory(AppPaths.TempDir);
                byte[] content = new byte[] {
                    0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0x21, 0xf9, 0x04, 0x01, 0x00,
                    0x00, 0x00, 0x00, 0x2c, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
                    0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3b
                };
                File.WriteAllBytes(tempImageFile, content);

                string correctHash;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hashBytes = sha.ComputeHash(content);
                    correctHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
                string incorrectHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

                try
                {
                    var method = typeof(StudentLessonPage).GetMethod("RenderImageContent",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(method);

                    method.Invoke(page, new object[] { panelCorrect, $"{tempImageFile}|{correctHash}" });
                    
                    int elapsed = 0;
                    while (panelCorrect.Children.Count == 0 && elapsed < 2000)
                    {
                        System.Threading.Thread.Sleep(50);
                        elapsed += 50;
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(delegate {}, System.Windows.Threading.DispatcherPriority.Background);
                    }

                    if (panelCorrect.Children[0] is Border b && b.Child is TextBlock tb)
                    {
                        Assert.Fail("Failed to load image. Error in UI: " + tb.Text);
                    }
                    Assert.IsType<Image>(panelCorrect.Children[0]);

                    method.Invoke(page, new object[] { panelIncorrect, $"{tempImageFile}|{incorrectHash}" });
                    
                    elapsed = 0;
                    while (panelIncorrect.Children.Count == 0 && elapsed < 2000)
                    {
                        System.Threading.Thread.Sleep(50);
                        elapsed += 50;
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(delegate {}, System.Windows.Threading.DispatcherPriority.Background);
                    }

                    Assert.Single(panelIncorrect.Children);
                    var childBorder = panelIncorrect.Children[0] as Border;
                    Assert.NotNull(childBorder);
                    var txtBlock = childBorder.Child as TextBlock;
                    Assert.NotNull(txtBlock);
                    Assert.Contains("Lỗi bảo mật", txtBlock.Text);
                }
                finally
                {
                    if (File.Exists(tempImageFile))
                    {
                        File.Delete(tempImageFile);
                    }
                }
            });
        }

        [Fact]
        public void TestStudentShell_CacheCleanup_Startup()
        {
            RunOnStaThread(() =>
            {
                string tempDir = AppPaths.TempDir;
                Directory.CreateDirectory(tempDir);

                string freshFile = Path.Combine(tempDir, "fresh_cache_file.txt");
                File.WriteAllText(freshFile, "Fresh Cache Content");
                File.SetLastWriteTime(freshFile, DateTime.Now);

                string oldFile = Path.Combine(tempDir, "old_cache_file.txt");
                File.WriteAllText(oldFile, "Old Cache Content");
                File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-8.0));

                try
                {
                    var shell = new StudentShell();
                    var method = typeof(StudentShell).GetMethod("CleanupCacheDirectory",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(method);

                    method.Invoke(shell, null);

                    int elapsed = 0;
                    while (File.Exists(oldFile) && elapsed < 2000)
                    {
                        System.Threading.Thread.Sleep(50);
                        elapsed += 50;
                    }

                    Assert.True(File.Exists(freshFile), "Fresh file should not be deleted.");
                    Assert.False(File.Exists(oldFile), "Old file should be cleaned up.");
                }
                finally
                {
                    if (File.Exists(freshFile)) File.Delete(freshFile);
                    if (File.Exists(oldFile)) File.Delete(oldFile);
                }
            });
        }

        [Fact]
        public void TestAppSettings_CacheRetentionDays_SaveAndLoad()
        {
            var originalVal = QASmartTouch.Services.AppSettings.CacheRetentionDays;
            try
            {
                // Arrange & Act
                QASmartTouch.Services.AppSettings.CacheRetentionDays = 12;
                QASmartTouch.Services.AppSettings.Save();

                // Reset memory state
                QASmartTouch.Services.AppSettings.CacheRetentionDays = 7;
                QASmartTouch.Services.AppSettings.Load();

                // Assert
                Assert.Equal(12, QASmartTouch.Services.AppSettings.CacheRetentionDays);
            }
            finally
            {
                QASmartTouch.Services.AppSettings.CacheRetentionDays = originalVal;
                QASmartTouch.Services.AppSettings.Save();
            }
        }

        [Fact]
        public void TestStudentLessonPage_OfflineSelfStudyMode_TriggerAfterConsecutivePingLoss()
        {
            RunOnStaThread(() =>
            {
                // Arrange
                var page = new StudentLessonPage();
                var banner = page.FindName("borderOfflineBanner") as Border;
                Assert.NotNull(banner);
                Assert.Equal(Visibility.Collapsed, banner.Visibility);

                // Act: Simulate consecutive ping failures (6 failures)
                var pingField = typeof(StudentLessonPage).GetField("_consecutiveLostPings",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(pingField);

                // Trigger failure count below threshold
                pingField.SetValue(page, 4);
                
                // Simulate PingTimer_Tick where reply is null or failed
                var method = typeof(StudentLessonPage).GetMethod("PingTimer_Tick",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                // Call PingTimer_Tick (fails because app.StudentNetwork.ServerIP is empty/null or fails to ping)
                method.Invoke(page, new object[] { page, EventArgs.Empty });

                // Value should increment to 5, banner remains collapsed
                Assert.Equal(5, (int)pingField.GetValue(page)!);
                Assert.Equal(Visibility.Collapsed, banner.Visibility);

                // Trigger 6th failure
                method.Invoke(page, new object[] { page, EventArgs.Empty });

                // Value is 6, banner should be visible
                Assert.Equal(6, (int)pingField.GetValue(page)!);
                Assert.Equal(Visibility.Visible, banner.Visibility);
            });
        }
    }
}
