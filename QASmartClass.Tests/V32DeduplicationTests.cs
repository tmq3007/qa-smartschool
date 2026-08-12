using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Xunit;
using QASmartClass.Classroom.Views;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    public class V32DeduplicationTests
    {
        [Fact]
        public void TestNormalizeName_TrimsAndNormalizesToNFC()
        {
            string nfcName = "Nguyễn Văn An";
            string nfdName = nfcName.Normalize(System.Text.NormalizationForm.FormD) + " \r\n";

            string result = MonitorPage.NormalizeName(nfdName);
            Assert.Equal(nfcName, result);
        }

        [Fact]
        public void TestIsNameMatch_MatchesVaryingSpacingAndNormalization()
        {
            string name1 = " Nguyễn Văn An\r\n";
            string name2 = "Nguyễn Văn An".Normalize(System.Text.NormalizationForm.FormD);

            bool isMatch = MonitorPage.IsNameMatch(name1, name2);
            Assert.True(isMatch);
        }

        [Fact]
        public void TestOnWebStudentConnected_PrioritizedMatchingAndDeduplication_WithStudentCode()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();

                var monitorPage = new MonitorPage();
                var allStudentsField = typeof(MonitorPage).GetField("_allStudents", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(allStudentsField);
                var allStudents = allStudentsField.GetValue(monitorPage) as ObservableCollection<ConnectedStudent>;
                Assert.NotNull(allStudents);

                allStudents.Clear();

                // Roster student has StudentCode
                var rosterStudent = new ConnectedStudent
                {
                    Name = "Nguyễn Văn An",
                    PCName = "PC-02",
                    StudentCode = "HS001",
                    IsOnline = false
                };
                allStudents.Add(rosterStudent);

                // Duplicate dynamic student
                var dynamicStudent = new ConnectedStudent
                {
                    Name = "Nguyễn Văn An",
                    PCName = "DESKTOP-FHD5M3R",
                    StudentCode = "HS001",
                    IsOnline = true
                };
                allStudents.Add(dynamicStudent);

                var e = new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Nguyễn Văn An",
                    StudentCode = "HS001",
                    PCName = "DESKTOP-FHD5M3R",
                    IPAddress = "192.168.1.15"
                };

                var onConnectedMethod = typeof(MonitorPage).GetMethod("OnWebStudentConnected", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(onConnectedMethod);
                onConnectedMethod.Invoke(monitorPage, new object[] { null, e });

                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                    () => { },
                    System.Windows.Threading.DispatcherPriority.Background);

                // Verification: Roster student should remain, dynamic removed
                Assert.Single(allStudents);
                var remaining = allStudents.First();
                Assert.Equal("DESKTOP-FHD5M3R", remaining.PCName);
                Assert.Equal("HS001", remaining.StudentCode);
                Assert.True(remaining.IsOnline);
            });
        }

        [Fact]
        public void TestOnWebStudentConnected_PrioritizedMatchingAndDeduplication_WithEmptyStudentCode()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();

                var monitorPage = new MonitorPage();
                var allStudentsField = typeof(MonitorPage).GetField("_allStudents", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(allStudentsField);
                var allStudents = allStudentsField.GetValue(monitorPage) as ObservableCollection<ConnectedStudent>;
                Assert.NotNull(allStudents);

                allStudents.Clear();

                // Roster student has empty StudentCode
                var rosterStudent = new ConnectedStudent
                {
                    Name = "Nguyễn Văn An",
                    PCName = "PC-02",
                    StudentCode = "", // empty
                    IsOnline = false
                };
                allStudents.Add(rosterStudent);

                // Duplicate dynamic student has StudentCode
                var dynamicStudent = new ConnectedStudent
                {
                    Name = "Nguyễn Văn An",
                    PCName = "DESKTOP-FHD5M3R",
                    StudentCode = "HS001",
                    IsOnline = true
                };
                allStudents.Add(dynamicStudent);

                var e = new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Nguye\u0301n Va\u0301n An ", // NFD name
                    StudentCode = "HS001",
                    PCName = "DESKTOP-FHD5M3R",
                    IPAddress = "192.168.1.15"
                };

                var onConnectedMethod = typeof(MonitorPage).GetMethod("OnWebStudentConnected", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(onConnectedMethod);
                onConnectedMethod.Invoke(monitorPage, new object[] { null, e });

                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                    () => { },
                    System.Windows.Threading.DispatcherPriority.Background);

                // Verification: Name match matches roster student, updates code, removes dynamic
                Assert.Single(allStudents);
                var remaining = allStudents.First();
                Assert.Same(rosterStudent, remaining);
                Assert.Equal("HS001", remaining.StudentCode);
                Assert.Equal("DESKTOP-FHD5M3R", remaining.PCName);
                Assert.True(remaining.IsOnline);
            });
        }

        private void InitializeWpfApplication()
        {
            if (Application.Current != null && (!(Application.Current is QASmartTouch.App) || Application.Current.Dispatcher.Thread != Thread.CurrentThread))
            {
                var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                if (appCreatedField != null)
                {
                    appCreatedField.SetValue(null, false);
                }
                var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                if (currentField != null)
                {
                    currentField.SetValue(null, null);
                }
            }

            if (Application.Current == null)
            {
                var app = new QASmartTouch.App();
                var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                if (dbProperty != null)
                {
                    dbProperty.SetValue(app, new AppDbContext());
                }
            }
            else if (Application.Current is QASmartTouch.App app)
            {
                var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                if (dbProperty != null)
                {
                    dbProperty.SetValue(app, new AppDbContext());
                }
            }

            // Register resources to avoid crash on static resource lookup
            var keys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush", "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
            foreach (var key in keys)
            {
                if (!Application.Current.Resources.Contains(key))
                {
                    Application.Current.Resources[key] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                }
            }

            // MonitorPage specifically needs GeomLock and GeomUnlock
            var geomKeys = new[] { "GeomLock", "GeomUnlock" };
            foreach (var key in geomKeys)
            {
                if (!Application.Current.Resources.Contains(key))
                {
                    Application.Current.Resources[key] = new System.Windows.Media.PathGeometry();
                }
            }
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
            var tcs = new TaskCompletionSource<bool>();
            var thread = new Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            tcs.Task.GetAwaiter().GetResult();
        }
    }
}
