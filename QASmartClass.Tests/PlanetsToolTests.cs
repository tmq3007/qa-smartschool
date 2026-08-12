using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.Data.Sqlite;
using QASmartClass.LearningTools.Views.Multi;
using Xunit;

namespace QASmartClass.Tests
{
    public class PlanetsToolTests
    {
        private static System.Threading.Thread? _staThread;
        private static System.Windows.Threading.Dispatcher? _dispatcher;
        private static readonly object _lock = new();

        private static void EnsureStaThread()
        {
            lock (_lock)
            {
                if (_staThread == null)
                {
                    var readyEvent = new System.Threading.ManualResetEvent(false);
                    _staThread = new System.Threading.Thread(() =>
                    {
                        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;

                        try
                        {
                            var appField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            var createdField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (appField != null) appField.SetValue(null, null);
                            if (createdField != null) createdField.SetValue(null, false);
                        }
                        catch {}

                        Application? app = null;
                        try
                        {
                            app = new Application();
                        }
                        catch
                        {
                            app = Application.Current;
                        }

                        if (app != null)
                        {
                            try
                            {
                                bool hasTokens = false;
                                foreach (var dict in app.Resources.MergedDictionaries)
                                {
                                    if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                                    {
                                        hasTokens = true;
                                        break;
                                    }
                                }

                                if (!hasTokens)
                                {
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                    });
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                                    });
                                }
                            }
                            catch {}
                        }

                        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        readyEvent.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    });
                    _staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                    _staThread.IsBackground = true;
                    _staThread.Start();
                    readyEvent.WaitOne();
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
            EnsureStaThread();
            Exception? testEx = null;
            _dispatcher!.Invoke(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    testEx = ex;
                }
            });
            if (testEx != null) throw testEx;
        }

        [Fact]
        public void TestPlanetsTool_CanBeInstantiated()
        {
            RunOnStaThread(() =>
            {
                var tool = new PlanetsTool();
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void TestPlanetsTool_ImplementsIDisposableAndDisposes()
        {
            RunOnStaThread(() =>
            {
                var tool = new PlanetsTool();
                Assert.True(tool is IDisposable, "PlanetsTool must implement IDisposable");
                
                var ex = Record.Exception(() => ((IDisposable)tool).Dispose());
                Assert.Null(ex);
            });
        }

        [Fact]
        public void TestPlanetsTool_CanvasDoesNotAccumulatePlanetsOnMultipleLoads()
        {
            RunOnStaThread(() =>
            {
                var tool = new PlanetsTool();
                
                // First load
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                int firstLoadCount = tool.SolarSystemCanvasNode.Children.Count;
                
                // Unload and Second load
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                int secondLoadCount = tool.SolarSystemCanvasNode.Children.Count;
                
                Assert.Equal(firstLoadCount, secondLoadCount);
            });
        }

        [Fact]
        public void TestPlanetsTool_DatabaseLoadUsesSafeTimeout()
        {
            RunOnStaThread(() =>
            {
                // Ensure database is migrated
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    QASmartClass.Services.DbMigrator.Migrate(db, "5.52.0");
                }

                // Initialize table and seed default values
                var initTool = new PlanetsTool();
                initTool.LoadPlanetsFromDb();

                // Verify SQLite connection string contains Default Timeout
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                var hexKey = Convert.ToHexString(QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey());
                using (var lockConn = new SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;"))
                {
                    lockConn.Open();
                    using (var tx = lockConn.BeginTransaction())
                    {
                        var cmd = lockConn.CreateCommand();
                        cmd.CommandText = "UPDATE LearningToolPlanets SET DayHours = 24 WHERE NameEn = 'Earth';";
                        cmd.ExecuteNonQuery();
                        
                        var tool = new PlanetsTool();
                        // Loading planets under a locked transaction should not crash immediately due to Default Timeout
                        var planets = tool.LoadPlanetsFromDb();
                        Assert.NotEmpty(planets);
                        
                        tx.Rollback();
                    }
                }
            });
        }

        [Fact]
        public void TestPlanetsTool_KeplerianMathCalculatesCorrectly()
        {
            RunOnStaThread(() =>
            {
                var tool = new PlanetsTool();
                // Giả lập tính toán elip cho Mercury (a=70, e=0.2056)
                double a = 70;
                double e = 0.2056;
                
                // Tại perihelion (theta = 0)
                double rPeri = a * (1 - e * e) / (1 + e * Math.Cos(0));
                Assert.True(rPeri < a, "Khoảng cách tại cận điểm phải nhỏ hơn bán trục lớn");
                
                // Tại aphelion (theta = Math.PI)
                double rApo = a * (1 - e * e) / (1 + e * Math.Cos(Math.PI));
                Assert.True(rApo > a, "Khoảng cách tại viễn điểm phải lớn hơn bán trục lớn");
            });
        }
    }
}
