using Xunit;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using QASmartClass.StudentClient.Services;
using QASmartClass.StudentClient.Views;
using QASmartClass.Staff.ViewModels;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    public class V71UpgradeTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    action();
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
        public void StaffDashboardViewModel_ImplementsIDisposable_Verify()
        {
            var vm = new StaffDashboardViewModel();
            Assert.True(vm is IDisposable, "StaffDashboardViewModel phải kế thừa IDisposable");
        }

        [Fact]
        public void SecureProfileHelper_EncryptDecrypt_Verify()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var originalJson = "{\"StudentCode\":\"HS007\",\"StudentName\":\"Bond\",\"TeacherIP\":\"127.0.0.1\",\"RememberMe\":true}";
                
                // Ghi bảo mật
                SecureProfileHelper.WriteProfileText(tempFile, originalJson);
                
                // Đọc lại bảo mật
                var decryptedText = SecureProfileHelper.ReadProfileText(tempFile);
                
                Assert.Equal(originalJson, decryptedText);
                
                // Kiểm tra xem file ghi ra có đúng là nhị phân (bảo mật) không
                var bytes = File.ReadAllBytes(tempFile);
                Assert.NotEqual((byte)'{', bytes[0]);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void EventLog_ModelFields_Verify()
        {
            var log = new EventLog();
            log.MacAddress = "AA:BB:CC:DD:EE:FF";
            log.ClientIP = "192.168.1.50";
            
            Assert.Equal("AA:BB:CC:DD:EE:FF", log.MacAddress);
            Assert.Equal("192.168.1.50", log.ClientIP);
        }

        [Fact]
        public void StemTools_CreateWrapper_Verify()
        {
            RunOnStaThread(() =>
            {
                var control = QASmartClass.LearningTools.Views.LearningToolsHub.CreateToolControl("stem_tools");
                Assert.NotNull(control);
                Assert.IsType<QASmartClass.LearningTools.Views.StemToolsWrapper>(control);
            });
        }

        [Fact]
        public void StudentShell_GetStemToolsDisplayName_Verify()
        {
            var method = typeof(StudentShell).GetMethod("GetToolDisplayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(method);
            var result = method.Invoke(null, new object[] { "stem_tools" });
            Assert.Equal("Công cụ STEM", result);
        }
        [Fact]
        public void ConvertTcpToJson_BroadcastCommands_Verify()
        {
            var method = typeof(QASmartClass.Classroom.Services.WebSocketBridgeService).GetMethod("ConvertTcpToJson", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(method);
            
            // Screen broadcast start
            var tcpScreen = "CMD|SCREEN_BROADCAST_START|C:\\temp\\broadcast_0.jpg";
            var jsonScreen = (string)method.Invoke(null, new object[] { tcpScreen });
            Assert.Contains("/api/broadcast_image", jsonScreen);
            Assert.Contains("imageUrl", jsonScreen);
            
            // File broadcast
            var tcpFile = "CMD|FILE_BROADCAST|C:\\temp\\abc.pdf";
            var jsonFile = (string)method.Invoke(null, new object[] { tcpFile });
            Assert.Contains("/api/file_broadcast", jsonFile);
            Assert.Contains("fileUrl", jsonFile);
            Assert.Contains("fileName", jsonFile);
            Assert.Contains("fileType", jsonFile);
        }

        [Fact]
        public async Task SendToStudentAsync_FallbackMechanism_Verify()
        {
            var net = new QASmartClass.Classroom.Services.NetworkDiscoveryService();
            // Test lookup fallback does not throw exception and executes gracefully
            await net.SendToStudentAsync("NON_EXISTENT_PC", "CMD|SILENCE");
            Assert.True(true, "SendToStudentAsync fallback gracefully handled");
        }

        [Fact]
        public void StudentShell_HandleTeacherCommandPort_Verify()
        {
            RunOnStaThread(() =>
            {
                var shell = new StudentShell();
                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Assert.NotNull(handleMethod);
                
                // 1. Test SCREEN_BROADCAST_START with port
                handleMethod.Invoke(shell, new object[] { "CMD|SCREEN_BROADCAST_START|C:\\temp\\test.jpg|8085" });
                var portField = typeof(StudentShell).GetField("_currentTeacherWebPort", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(portField);
                Assert.Equal(8085, portField.GetValue(shell));

                // 2. Test SCREEN_BROADCAST_START with FORCE_WATCH and port
                handleMethod.Invoke(shell, new object[] { "CMD|SCREEN_BROADCAST_START|C:\\temp\\test.jpg|FORCE_WATCH|8090" });
                Assert.Equal(8090, portField.GetValue(shell));

                // 3. Test FILE_BROADCAST with port
                handleMethod.Invoke(shell, new object[] { "CMD|FILE_BROADCAST|C:\\temp\\file.pdf|8095" });
                Assert.Equal(8095, portField.GetValue(shell));
            });
        }

        [Fact]
        public void BroadcastPage_PrivacyFreezeState_Verify()
        {
            RunOnStaThread(() =>
            {
                var page = new QASmartClass.Classroom.Views.BroadcastPage();
                Assert.NotNull(page);

                var field = typeof(QASmartClass.Classroom.Views.BroadcastPage).GetField("_isBroadcastPausedDueToPrivacy",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(field);
                
                // Assert default is false
                Assert.False((bool)field.GetValue(page));

                // Set to true and check
                field.SetValue(page, true);
                Assert.True((bool)field.GetValue(page));
            });
        }
    }
}
