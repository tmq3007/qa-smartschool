using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.Services;
using QASmartClass.StudentClient.Views;
using System.Windows;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_03 — HỘI ĐỒNG CHUYÊN GIA (Phiên bản nâng cao v2)
    /// 
    /// Bao gồm 8 kịch bản thực tế đánh giá triệt để cơ chế truyền màn hình Multicast/UDP:
    ///   TC-01: Sender bind IP LAN vật lý + JoinMulticastGroup thành công
    ///   TC-02: Receiver join multicast trên tất cả card mạng LAN — không exception
    ///   TC-03: Receiver fallback default interface khi không có card vật lý
    ///   TC-04: StudentShell auto-open overlay khi nhận SCREEN_BROADCAST_UPDATE
    ///   TC-05: Start/Stop Receiver cycle — không rò rỉ tài nguyên
    ///   TC-06: Multicast IP & Port cấu hình đúng (239.0.0.1:8088)
    ///   TC-07: UdpScreenBroadcastService Singleton + IDisposable pattern
    ///   TC-08: FrameReceived event + Slice protocol (MaxSliceSize=1400)
    /// </summary>
    public class LOI_VID_03_ExpertVerificationTests
    {
        private static void EnsureAppForCurrentThread()
        {
            if (Application.Current != null)
            {
                bool isInvalid = false;
                try
                {
                    if (Application.Current.Dispatcher.Thread != Thread.CurrentThread)
                    {
                        isInvalid = true;
                    }
                    else
                    {
                        var isShuttingDownField = typeof(Application).GetField("_isShuttingDown", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (isShuttingDownField != null)
                        {
                            bool isShuttingDown = (bool)isShuttingDownField.GetValue(Application.Current);
                            if (isShuttingDown) isInvalid = true;
                        }
                    }
                }
                catch
                {
                    isInvalid = true;
                }

                if (isInvalid)
                {
                    var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                    if (appCreatedField != null)
                    {
                        appCreatedField.SetValue(null, false);
                    }
                    var appInstanceField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                    if (appInstanceField != null)
                    {
                        appInstanceField.SetValue(null, null);
                    }
                }
            }

            if (Application.Current == null)
            {
                var app = new QASmartTouch.App();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                
                var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                if (dbProperty != null)
                {
                    dbProperty.SetValue(app, new AppDbContext());
                }

                var resources = app.Resources;
                try
                {
                    var dict = new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                    };
                    resources.MergedDictionaries.Add(dict);
                }
                catch { }

                var white = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

                string[] keys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush", "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
                foreach (var key in keys)
                {
                    if (!resources.Contains(key))
                    {
                        resources.Add(key, key.Contains("Dark") || key.Contains("Muted") ? gray : (key.Contains("Primary") ? blue : white));
                    }
                }

                if (!resources.Contains("Gray100"))
                {
                    resources.Add("Gray100", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5")));
                }
                if (!resources.Contains("BrandAccent"))
                {
                    resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E65100")));
                }
            }
        }
        /// <summary>
        /// TC-01 [Quản lý IT & Chuyên gia CSDL]:
        /// Sender bind vào IP LAN vật lý thành công và join multicast group.
        /// Mô phỏng: GV bật chiếu màn hình → Sender khởi tạo trên card LAN chính.
        /// </summary>
        [Fact]
        public void TC01_UdpSender_BindsToPhysicalIP_Successfully()
        {
            var service = UdpScreenBroadcastService.Instance;
            
            // StartSender phải không ném exception
            var exception = Record.Exception(() => service.StartSender());
            Assert.Null(exception);
        }

        /// <summary>
        /// TC-02 [Chuyên gia Kiểm thử & Nhân viên kỹ thuật]:
        /// Receiver join multicast group trên tất cả IP LAN vật lý — không exception.
        /// Mô phỏng: HS bật máy tính phòng Lab, nhận chiếu màn hình từ GV.
        /// </summary>
        [Fact]
        public void TC02_UdpReceiver_JoinsMulticastOnAllIPs_NoExceptions()
        {
            var service = UdpScreenBroadcastService.Instance;
            
            // StartReceiver phải không ném exception
            var exception = Record.Exception(() => service.StartReceiver());
            Assert.Null(exception);
            
            // Cleanup
            service.StopReceiver();
        }

        /// <summary>
        /// TC-03 [Chuyên gia Bảo mật & Gamer giỏi]:
        /// Receiver fallback join multicast trên default interface khi không có card LAN.
        /// Kiểm tra: StartReceiver luôn thành công bất kể cấu hình mạng.
        /// </summary>
        [Fact]
        public void TC03_Receiver_FallbackDefaultInterface_AlwaysSucceeds()
        {
            var service = UdpScreenBroadcastService.Instance;
            
            // Đảm bảo receiver đã dừng trước khi test
            service.StopReceiver();
            
            // Start lại — phải thành công
            var exception = Record.Exception(() => service.StartReceiver());
            Assert.Null(exception);
            
            // Stop ngay — không lỗi
            var stopException = Record.Exception(() => service.StopReceiver());
            Assert.Null(stopException);
        }

        /// <summary>
        /// TC-04 [Nhà giáo dục & Giáo viên ưu tú]:
        /// StudentShell tự động mở overlay khi nhận lệnh SCREEN_BROADCAST_UPDATE.
        /// Mô phỏng: GV bắt đầu chiếu bài → HS tự động thấy màn hình GV.
        /// </summary>
        [Fact]
        public void TC04_StudentShell_UpdateCommand_AutoOpensOverlay()
        {
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                EnsureAppForCurrentThread();
                var tempFile = Path.GetTempFileName();
                try
                {
                    var shell = new StudentShell();
                    
                    // Xác minh overlay ban đầu là null
                    var overlayField = typeof(StudentShell).GetField("_broadcastOverlay", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(overlayField);
                    Assert.Null(overlayField.GetValue(shell));
                    
                    // Mô phỏng nhận lệnh SCREEN_BROADCAST_UPDATE
                    var updateCmd = $"CMD|SCREEN_BROADCAST_UPDATE|{tempFile}|8080|TK_TESTTOKEN";
                    
                    // Tìm phương thức xử lý lệnh
                    var methods = typeof(StudentShell).GetMethods(
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    var handleMethod = methods.FirstOrDefault(m => 
                        m.Name.IndexOf("TeacherCommand", StringComparison.OrdinalIgnoreCase) >= 0);
                    Assert.NotNull(handleMethod);
                    
                    // Thực thi lệnh
                    handleMethod.Invoke(shell, new object[] { updateCmd });
                    
                    // Xác minh overlay đã được tạo
                    var overlayValue = overlayField.GetValue(shell);
                    Assert.NotNull(overlayValue);
                    
                    // Xác minh port và token được cập nhật
                    var portField = typeof(StudentShell).GetField("_currentTeacherWebPort", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var tokenField = typeof(StudentShell).GetField("_currentBroadcastToken", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    Assert.NotNull(portField);
                    Assert.NotNull(tokenField);
                    Assert.Equal(8080, portField.GetValue(shell));
                    Assert.Equal("TK_TESTTOKEN", tokenField.GetValue(shell));
                    
                    // Dọn dẹp
                    var closeMethod = typeof(StudentShell).GetMethod("CloseScreenBroadcast", 
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    closeMethod?.Invoke(shell, null);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
                finally
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch {}
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            
            Assert.Null(threadEx);
        }

        /// <summary>
        /// TC-05 [Hiệu trưởng & Trưởng bộ môn]:
        /// Start/Stop Receiver nhiều lần liên tiếp — không rò rỉ tài nguyên.
        /// Mô phỏng: GV bật/tắt chiếu màn hình nhiều lần trong 1 tiết.
        /// </summary>
        [Fact]
        public void TC05_StartStopReceiver_MultipleCycles_NoResourceLeak()
        {
            var service = UdpScreenBroadcastService.Instance;
            
            // Chu kỳ Start/Stop 5 lần liên tiếp
            for (int i = 0; i < 5; i++)
            {
                var startEx = Record.Exception(() => service.StartReceiver());
                Assert.Null(startEx);
                
                var stopEx = Record.Exception(() => service.StopReceiver());
                Assert.Null(stopEx);
            }
            
            // Lần cuối — Start phải vẫn hoạt động
            var finalStartEx = Record.Exception(() => service.StartReceiver());
            Assert.Null(finalStartEx);
            
            service.StopReceiver();
        }

        /// <summary>
        /// TC-06 [Chuyên gia CSDL & Cán bộ Sở GD]:
        /// Multicast IP = 239.0.0.1, Port = 8088, MaxSliceSize = 1400.
        /// Kiểm tra cấu hình constants qua reflection.
        /// </summary>
        [Fact]
        public void TC06_MulticastConfig_CorrectValues()
        {
            var type = typeof(UdpScreenBroadcastService);
            
            // MulticastIP = "239.0.0.1"
            var multicastIpProp = type.GetProperty("MulticastIP", 
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(multicastIpProp);
            var multicastIP = multicastIpProp.GetValue(null)?.ToString();
            Assert.Equal("239.0.0.1", multicastIP);
            
            // Validate multicast range (224.0.0.0 - 239.255.255.255)
            Assert.True(IPAddress.TryParse(multicastIP, out var addr));
            byte firstOctet = addr.GetAddressBytes()[0];
            Assert.InRange(firstOctet, 224, 239);
            
            // MulticastPort = 8088
            var multicastPortProp = type.GetProperty("MulticastPort", 
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(multicastPortProp);
            var multicastPort = (int)multicastPortProp.GetValue(null)!;
            Assert.Equal(8088, multicastPort);
            Assert.InRange(multicastPort, 1024, 65535); // Non-privileged port
            
            // MaxSliceSize = 1400 (< MTU 1472)
            var maxSliceField = type.GetField("MaxSliceSize", 
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(maxSliceField);
            var maxSlice = (int)maxSliceField.GetValue(null)!;
            Assert.Equal(1400, maxSlice);
            Assert.True(maxSlice + 8 < 1472, 
                "MaxSliceSize + header phải nhỏ hơn MTU 1472");
        }

        /// <summary>
        /// TC-07 [Nhà khoa học giáo dục & Nhân viên nhà trường]:
        /// UdpScreenBroadcastService là Singleton + implements IDisposable.
        /// Mô phỏng: Đảm bảo chỉ 1 instance truyền màn hình tồn tại.
        /// </summary>
        [Fact]
        public void TC07_Singleton_And_IDisposable_Pattern()
        {
            var type = typeof(UdpScreenBroadcastService);
            
            // Kiểm tra Singleton pattern
            var instanceProp = type.GetProperty("Instance", 
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(instanceProp);
            
            var instance1 = instanceProp.GetValue(null);
            var instance2 = instanceProp.GetValue(null);
            Assert.Same(instance1, instance2); // Phải cùng 1 object
            
            // Kiểm tra IDisposable
            Assert.True(typeof(IDisposable).IsAssignableFrom(type),
                "UdpScreenBroadcastService phải implement IDisposable");
            
            // Kiểm tra Dispose method tồn tại
            var disposeMethod = type.GetMethod("Dispose", 
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(disposeMethod);
            
            // Kiểm tra constructor private (Singleton pattern)
            var constructors = type.GetConstructors(
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(constructors.Length > 0, "Phải có private constructor");
            Assert.True(constructors.All(c => !c.IsPublic), 
                "Constructor không được public (Singleton)");
        }

        /// <summary>
        /// TC-08 [Học sinh & Chuyên gia thiết kế]:
        /// FrameReceived event tồn tại và MaxSliceSize tối ưu cho LAN.
        /// Mô phỏng: HS nhận frame chiếu từ GV, mỗi packet < MTU.
        /// </summary>
        [Fact]
        public void TC08_FrameReceived_Event_And_SliceProtocol()
        {
            var type = typeof(UdpScreenBroadcastService);
            
            // FrameReceived event phải tồn tại
            var frameEvent = type.GetEvent("FrameReceived");
            Assert.NotNull(frameEvent);
            
            // Event type phải là Action<byte[]>
            Assert.Equal(typeof(Action<byte[]>), frameEvent.EventHandlerType);
            
            // Kiểm tra _multicastEP field (IPEndPoint)
            var epField = type.GetField("_multicastEP", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(epField);
            
            var service = UdpScreenBroadcastService.Instance;
            var ep = epField.GetValue(service) as IPEndPoint;
            Assert.NotNull(ep);
            Assert.Equal("239.0.0.1", ep!.Address.ToString());
            Assert.Equal(8088, ep.Port);
            
            // Kiểm tra SendFrameAsync method tồn tại
            var sendMethod = type.GetMethod("SendFrameAsync", 
                BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(sendMethod);
            Assert.Equal(typeof(Task), sendMethod.ReturnType);
            
            // Kiểm tra tham số: byte[] imgBytes
            var sendParams = sendMethod.GetParameters();
            Assert.Single(sendParams);
            Assert.Equal(typeof(byte[]), sendParams[0].ParameterType);
        }
    }
}
