using Xunit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Classroom.Services;
using QASmartClass.StudentClient.Services;
using QASmartClass.StudentClient.Views;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_06 — HỘI ĐỒNG CHUYÊN GIA
    /// 
    /// Đánh giá các tính năng:
    ///   TC-01: Lệnh LOCK_SCREEN kích hoạt overlay khóa màn hình.
    ///   TC-02: Lệnh UNLOCK_SCREEN gỡ bỏ overlay.
    ///   TC-03: Lệnh LOCK (từ Policy Page) hiển thị overlay khóa đồng nhất.
    ///   TC-04: Lệnh UNLOCK (từ Policy Page) gỡ bỏ overlay đồng nhất.
    ///   TC-05: Lưu và gỡ bỏ lệnh QoS 1 trong danh sách chờ khi nhận ACK.
    ///   TC-06: Vòng lặp QoS 1 tự động resend lệnh khi không có ACK.
    ///   TC-07: Watchdog chủ động PING sau 12 giây im lặng và ngắt kết nối khi quá 30 giây.
    /// </summary>
    public class LOI_VID_06_ExpertVerificationTests
    {
        private void RunOnSTA(Action action)
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
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                try { InitializeApplicationFull(); action(); }
                catch (Exception ex) { threadEx = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        private void StopWatchdogTimer(StudentShell shell)
        {
            var timerField = typeof(StudentShell).GetField("_focusWatchdogTimer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (timerField != null)
            {
                var timer = timerField.GetValue(shell) as System.Windows.Threading.DispatcherTimer;
                timer?.Stop();
            }
        }

        [Fact]
        public void TC01_LockScreenCommand_TriggersOverlayAndState()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                StopWatchdogTimer(shell);

                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                handleMethod.Invoke(shell, new object[] { "CMD|LOCK_SCREEN|id=101" });

                var overlayField = typeof(StudentShell).GetField("_lockOverlay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField);
                var overlay = overlayField.GetValue(shell) as Grid;
                Assert.NotNull(overlay);

                var closeMethod = typeof(StudentShell).GetMethod("CloseScreenLockOverlay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(closeMethod);
                closeMethod.Invoke(shell, null);
            });
        }

        [Fact]
        public void TC02_UnlockScreenCommand_ClearsOverlayAndState()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                StopWatchdogTimer(shell);

                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                handleMethod.Invoke(shell, new object[] { "CMD|LOCK_SCREEN|id=101" });

                var overlayField = typeof(StudentShell).GetField("_lockOverlay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField.GetValue(shell));

                handleMethod.Invoke(shell, new object[] { "CMD|UNLOCK_SCREEN|id=102" });

                Assert.Null(overlayField.GetValue(shell));
            });
        }

        [Fact]
        public void TC03_LockCommand_TriggersOverlayAndState()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                StopWatchdogTimer(shell);

                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                handleMethod.Invoke(shell, new object[] { "CMD|LOCK|id=103" });

                var overlayField = typeof(StudentShell).GetField("_lockOverlay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField);
                var overlay = overlayField.GetValue(shell) as Grid;
                Assert.NotNull(overlay);

                var closeMethod = typeof(StudentShell).GetMethod("CloseScreenLockOverlay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(closeMethod);
                closeMethod.Invoke(shell, null);
            });
        }

        [Fact]
        public void TC04_UnlockCommand_ClearsOverlayAndState()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                StopWatchdogTimer(shell);

                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                handleMethod.Invoke(shell, new object[] { "CMD|LOCK|id=103" });

                var overlayField = typeof(StudentShell).GetField("_lockOverlay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField.GetValue(shell));

                handleMethod.Invoke(shell, new object[] { "CMD|UNLOCK|id=104" });

                Assert.Null(overlayField.GetValue(shell));
            });
        }

        [Fact]
        public async Task TC05_QosCommand_RegistersPending_AndRemovesOnAck()
        {
            var netService = new NetworkDiscoveryService();
            var clientsField = typeof(NetworkDiscoveryService).GetField("_clients",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(clientsField);
            var clients = clientsField.GetValue(netService) as ConcurrentDictionary<string, ClientInfo>;
            Assert.NotNull(clients);

            var fakeClient = new ClientInfo
            {
                Code = "HS999",
                Name = "Fake Student",
                PCName = "FAKE-PC",
                LastSeen = DateTime.Now
            };
            clients["HS999"] = fakeClient;

            await netService.SendCommandAsync("CMD|LOCK_SCREEN");

            var pendingField = typeof(NetworkDiscoveryService).GetField("_pendingQosCommands",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(pendingField);
            var pending = pendingField.GetValue(netService) as ConcurrentDictionary<string, PendingQosCommand>;
            Assert.NotNull(pending);
            
            Assert.NotEmpty(pending);
            var entry = pending.Values.FirstOrDefault(p => p.StudentCode == "HS999");
            Assert.NotNull(entry);
            Assert.Equal("HS999", entry.StudentCode);
            Assert.Contains("CMD|LOCK_SCREEN", entry.CommandText);

            var processMsg = typeof(NetworkDiscoveryService).GetMethod("ProcessStudentMessage",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(processMsg);

            string ackMsg = $"ACK|{entry.CommandId}|FAKE-PC|SUCCESS";
            processMsg.Invoke(netService, new object[] { fakeClient, ackMsg, null!, CancellationToken.None });

            Assert.Empty(pending);
        }

        [Fact]
        public async Task TC06_QosCommand_RetriesResending_WhenNoAck()
        {
            var netService = new NetworkDiscoveryService();
            var clientsField = typeof(NetworkDiscoveryService).GetField("_clients",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(clientsField);
            var clients = clientsField.GetValue(netService) as ConcurrentDictionary<string, ClientInfo>;
            Assert.NotNull(clients);

            int port = 39812;
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            var clientTask = TcpClientCreator(port);
            var serverTcp = await listener.AcceptTcpClientAsync();
            var clientTcp = await clientTask;

            var fakeClient = new ClientInfo
            {
                Code = "HS999",
                Name = "Fake Student",
                PCName = "FAKE-PC",
                LastSeen = DateTime.Now,
                TcpClient = serverTcp,
                Stream = serverTcp.GetStream()
            };
            clients["HS999"] = fakeClient;

            await netService.SendCommandAsync("CMD|LOCK_SCREEN");

            var pendingField = typeof(NetworkDiscoveryService).GetField("_pendingQosCommands",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(pendingField);
            var pending = pendingField.GetValue(netService) as ConcurrentDictionary<string, PendingQosCommand>;
            Assert.NotNull(pending);
            
            var entry = pending.Values.First();
            entry.NextRetryTime = DateTime.UtcNow.AddSeconds(-1);

            var retryMethod = typeof(NetworkDiscoveryService).GetMethod("RunQosRetryLoopAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(retryMethod);

            using var cts = new CancellationTokenSource();
            var retryTask = (Task)retryMethod.Invoke(netService, new object[] { cts.Token })!;
            
            await Task.Delay(1200);
            cts.Cancel();

            Assert.True(entry.RetryCount > 0, "RetryCount should be incremented");

            serverTcp.Close();
            clientTcp.Close();
            listener.Stop();
        }

        [Fact]
        public async Task TC07_ActiveHeartbeatPing_SendsPing_AndDetectsDisconnect()
        {
            var netService = new NetworkDiscoveryService();
            var clientsField = typeof(NetworkDiscoveryService).GetField("_clients",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(clientsField);
            var clients = clientsField.GetValue(netService) as ConcurrentDictionary<string, ClientInfo>;
            Assert.NotNull(clients);

            int port = 39813;
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();

            var clientTask = TcpClientCreator(port);
            var serverTcp = await listener.AcceptTcpClientAsync();
            var clientTcp = await clientTask;

            var fakeClient = new ClientInfo
            {
                Code = "HS999",
                Name = "Fake Student",
                PCName = "FAKE-PC",
                LastSeen = DateTime.Now.AddSeconds(-15),
                TcpClient = serverTcp,
                Stream = serverTcp.GetStream()
            };
            clients["HS999"] = fakeClient;

            var watchdogMethod = typeof(NetworkDiscoveryService).GetMethod("RunHeartbeatWatchdogAsync",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(watchdogMethod);

            using var cts = new CancellationTokenSource();
            var watchdogTask = (Task)watchdogMethod.Invoke(netService, new object[] { cts.Token })!;

            await Task.Delay(1200);

            var buffer = new byte[1024];
            var stream = clientTcp.GetStream();
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string receivedCmd = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            Assert.NotEmpty(receivedCmd);

            fakeClient.LastSeen = DateTime.Now.AddSeconds(-40);
            
            await Task.Delay(5200);
            cts.Cancel();

            Assert.False(serverTcp.Connected, "Client should be disconnected on heartbeat timeout");

            serverTcp.Close();
            clientTcp.Close();
            listener.Stop();
        }

        private async Task<TcpClient> TcpClientCreator(int port)
        {
            var tcp = new TcpClient();
            await tcp.ConnectAsync(IPAddress.Loopback, port);
            return tcp;
        }

        private static void PumpDispatcher()
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new System.Windows.Threading.DispatcherOperationCallback(
                    delegate(object? f)
                    {
                        ((System.Windows.Threading.DispatcherFrame)f!).Continue = false;
                        return null;
                    }), frame);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        [Fact]
        public void TC08_HttpPullFailures_TriggersRelease()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                StopWatchdogTimer(shell);

                // Mở overlay giả lập
                var overlayField = typeof(StudentShell).GetField("_broadcastOverlay",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField);
                var overlay = new Grid();
                overlayField.SetValue(shell, overlay);

                // Set up UI elements needed for close/notification
                var imgField = typeof(StudentShell).GetField("_broadcastImage",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(imgField);
                imgField.SetValue(shell, new Image());

                var updateMethod = typeof(StudentShell).GetMethod("UpdateScreenBroadcast",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(updateMethod);

                var failuresField = typeof(StudentShell).GetField("_consecutiveHttpPullFailures",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(failuresField);

                // Tạo file 0-byte tạm để File.Exists trả về true nhưng LoadBitmapFromFile thất bại
                string tempFile = Path.GetTempFileName();

                // Giả lập 5 lần pull thất bại
                for (int i = 0; i < 5; i++)
                {
                    updateMethod.Invoke(shell, new object[] { tempFile });
                    
                    // Pump dispatcher to allow the background task's Invoke to execute
                    for (int j = 0; j < 10; j++)
                    {
                        Thread.Sleep(20);
                        PumpDispatcher();
                    }
                }

                // Đợi task background chạy xong và pump dispatcher thêm
                for (int j = 0; j < 15; j++)
                {
                    Thread.Sleep(40);
                    PumpDispatcher();
                }
                
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch {}

                // Sau 5 lần hỏng, overlay phải được tự động giải phóng (bằng null)
                var currentOverlay = overlayField.GetValue(shell);
                Assert.Null(currentOverlay);
            });
        }

        [Fact]
        public void TC09_IsPortReachableAsync_HandlesStatusCorrectly()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var portCheckMethod = typeof(StudentShell).GetMethod("IsPortReachableAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(portCheckMethod);

                // Cổng rác không mở -> Trả về false
                var taskFail = (Task<bool>)portCheckMethod.Invoke(shell, new object[] { "127.0.0.1", 59812, 200 })!;
                bool resultFail = taskFail.GetAwaiter().GetResult();
                Assert.False(resultFail);

                // Cổng mở -> Trả về true
                int port = 59813;
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();

                var taskSuccess = (Task<bool>)portCheckMethod.Invoke(shell, new object[] { "127.0.0.1", port, 500 })!;
                bool resultSuccess = taskSuccess.GetAwaiter().GetResult();
                Assert.True(resultSuccess);

                listener.Stop();
            });
        }
    }
}
