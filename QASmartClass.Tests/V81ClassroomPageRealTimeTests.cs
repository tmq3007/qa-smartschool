using Xunit;
using System;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using QASmartClass.Classroom.Views;
using ModelStudent = QASmartClass.Classroom.Services.ConnectedStudent;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Tests
{
    public class V81ClassroomPageRealTimeTests
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

        [Fact]
        public void ClassroomPage_Initialization_ShouldLoadStudents()
        {
            RunOnStaThread(() =>
            {
                var page = new ClassroomPage();
                
                // Get private _students collection
                var sessionField = typeof(ClassroomPage).GetField("_sessionService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(sessionField);
                var sessionService = (ClassroomSessionService)sessionField.GetValue(page);
                Assert.NotNull(sessionService);
                var students = sessionService.ConnectedStudents;
                Assert.NotNull(students);
            });
        }

        [Fact]
        public void ClassroomPage_NetworkEvents_ShouldUpdateOnlineStatus()
        {
            RunOnStaThread(() =>
            {
                var page = new ClassroomPage();
                
                var sessionField = typeof(ClassroomPage).GetField("_sessionService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(sessionField);
                var sessionService = (ClassroomSessionService)sessionField.GetValue(page);
                Assert.NotNull(sessionService);
                var students = sessionService.ConnectedStudents;
                
                // Add a mock student to the collection
                var mockStudent = new ModelStudent
                {
                    Name = "Nguyen Van An",
                    StudentCode = "HS001",
                    PCName = "PC-01",
                    IPAddress = "192.168.1.101",
                    IsOnline = false
                };
                students.Add(mockStudent);
                
                // Simulate StudentConnected event using reflection
                var methodConnected = typeof(ClassroomSessionService).GetMethod("OnStudentConnected", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(methodConnected);
                
                var eventArgs = new StudentConnectedEventArgs
                {
                    StudentName = "Nguyen Van An",
                    StudentCode = "HS001",
                    PCName = "PC-01",
                    IPAddress = "192.168.1.101"
                };
                
                methodConnected.Invoke(sessionService, new object[] { null, eventArgs });
                Assert.True(mockStudent.IsOnline);
                
                // Simulate StudentDisconnected event
                var methodDisconnected = typeof(ClassroomSessionService).GetMethod("OnStudentDisconnected", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(methodDisconnected);
                
                methodDisconnected.Invoke(sessionService, new object[] { null, "HS001" });
                Assert.False(mockStudent.IsOnline);
            });
        }
    }
}
