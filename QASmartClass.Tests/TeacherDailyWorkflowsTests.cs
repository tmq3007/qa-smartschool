using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using QASmartClass.Staff.ViewModels;
using QASmartClass.LearningTools.Views.Thinking;
using Xunit;

namespace QASmartClass.Tests
{
    public class TeacherDailyWorkflowsTests
    {
        static TeacherDailyWorkflowsTests()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();
            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        private class MockSaveFileDialogService : ISaveFileDialogService
        {
            public string? TargetPath { get; set; }
            public bool ShowMessageCalled { get; set; }
            public bool StartProcessCalled { get; set; }
            public string? LastMessage { get; set; }

            public string? ShowSaveFileDialog(string defaultBaseName, string filter, string defaultExt)
            {
                return TargetPath;
            }

            public void ShowMessage(string message, string title, bool isError = false)
            {
                ShowMessageCalled = true;
                LastMessage = message;
            }

            public void StartProcess(string filePath)
            {
                StartProcessCalled = true;
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
            Exception? ex = null;
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

        [Fact]
        public async Task TestTeacherReportExport_PdfAndExcel_WorksCorrectly()
        {
            using (var db = new AppDbContext())
            {
                // Giả lập giáo viên đăng nhập
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_TEST_DAILY",
                    FullName = "Nguyễn Văn Dũng Test",
                    Role = "GV",
                    IsActive = true
                };
                StaffSession.Login(teacher);

                try
                {
                    var dialogService = new MockSaveFileDialogService();
                    var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestOutputs");
                    Directory.CreateDirectory(tempDir);
                    
                    var targetPdf = Path.Combine(tempDir, $"MoetTest_{Guid.NewGuid():N}.pdf");
                    var targetExcel = Path.Combine(tempDir, $"MoetTest_{Guid.NewGuid():N}.xlsx");

                    var vm = new MoetReportViewModel(dialogService, db);

                    // 1. Kiểm tra xuất PDF Mẫu 20
                    dialogService.TargetPath = targetPdf;
                    dialogService.ShowMessageCalled = false;
                    dialogService.LastMessage = null;
                    await vm.ExportMau20PdfCommand.ExecuteAsync(null);
                    if (dialogService.ShowMessageCalled && dialogService.LastMessage != null && dialogService.LastMessage.Contains("Lỗi"))
                    {
                        throw new Exception($"ExportMau20PdfCommand failed: {dialogService.LastMessage}");
                    }
                    Assert.True(File.Exists(targetPdf));
                    Assert.True(dialogService.ShowMessageCalled);
                    File.Delete(targetPdf);

                    // 2. Kiểm tra xuất Excel Mẫu 20
                    dialogService.TargetPath = targetExcel;
                    dialogService.ShowMessageCalled = false;
                    dialogService.LastMessage = null;
                    await vm.ExportMau20ExcelCommand.ExecuteAsync(null);
                    if (dialogService.ShowMessageCalled && dialogService.LastMessage != null && dialogService.LastMessage.Contains("Lỗi"))
                    {
                        throw new Exception($"ExportMau20ExcelCommand failed: {dialogService.LastMessage}");
                    }
                    Assert.True(File.Exists(targetExcel));
                    Assert.True(dialogService.ShowMessageCalled);
                    File.Delete(targetExcel);

                    // 3. Kiểm tra xuất PDF Mẫu 22
                    dialogService.TargetPath = targetPdf;
                    dialogService.ShowMessageCalled = false;
                    dialogService.LastMessage = null;
                    await vm.ExportMau22PdfCommand.ExecuteAsync(null);
                    if (dialogService.ShowMessageCalled && dialogService.LastMessage != null && dialogService.LastMessage.Contains("Lỗi"))
                    {
                        throw new Exception($"ExportMau22PdfCommand failed: {dialogService.LastMessage}");
                    }
                    Assert.True(File.Exists(targetPdf));
                    Assert.True(dialogService.ShowMessageCalled);
                    File.Delete(targetPdf);

                    // 4. Kiểm tra xuất Excel Mẫu 22
                    dialogService.TargetPath = targetExcel;
                    dialogService.ShowMessageCalled = false;
                    dialogService.LastMessage = null;
                    await vm.ExportMau22ExcelCommand.ExecuteAsync(null);
                    if (dialogService.ShowMessageCalled && dialogService.LastMessage != null && dialogService.LastMessage.Contains("Lỗi"))
                    {
                        throw new Exception($"ExportMau22ExcelCommand failed: {dialogService.LastMessage}");
                    }
                    Assert.True(File.Exists(targetExcel));
                    Assert.True(dialogService.ShowMessageCalled);
                    File.Delete(targetExcel);
                }
                finally
                {
                    StaffSession.Logout();
                }
            }
        }

        [Fact]
        public void TestMentalMathTool_DecimalSeparators_DynamicKeyIntercept()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Add missing static resources to prevent XamlParseException
                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                        System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandAccent"))
                        System.Windows.Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                        System.Windows.Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                }

                var tool = new MentalMathTool();
                var txtAnswer = (TextBox)tool.FindName("txtAnswer");
                Assert.NotNull(txtAnswer);

                // Giả lập gõ phím số chấm (Decimal key) khi ô trống
                txtAnswer.Text = "";
                txtAnswer.SelectionStart = 0;
                txtAnswer.SelectionLength = 0;

                string expectedDecSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

                // Triệu gọi trực tiếp sự kiện TextBox_PreviewKeyDown thông qua Reflection
                var method = typeof(MentalMathTool).GetMethod("TextBox_PreviewKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                // Sử dụng HwndSource làm MockPresentationSource
                using (var source = new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero))
                {
                    var keyEventArgs = new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        source,
                        0,
                        Key.Decimal)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent
                    };

                    method.Invoke(tool, new object[] { txtAnswer, keyEventArgs });

                    // Kiểm tra xem phím đã được xử lý và dấu thập phân được chèn vào
                    Assert.Equal(expectedDecSep, txtAnswer.Text);
                    Assert.True(keyEventArgs.Handled);

                    // Giả lập gõ thêm một dấu thập phân thứ hai khi đã có dấu thứ nhất
                    var keyEventArgs2 = new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        source,
                        0,
                        Key.Decimal)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent
                    };

                    method.Invoke(tool, new object[] { txtAnswer, keyEventArgs2 });

                    // Text vẫn phải là expectedDecSep và không bị lặp lại dấu chấm
                    Assert.Equal(expectedDecSep, txtAnswer.Text);
                    Assert.True(keyEventArgs2.Handled);
                }
            });
        }
    }
}
