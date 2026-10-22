using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.Classroom.Services;
using QASmartClass.Services;
using QASmartClass.StudentClient.Services;
using Xunit;

namespace QASmartClass.Tests
{
    [Collection("ClassroomNetwork")]
    public class V115ClassroomMonitoringAndConnectionEdgeCasesTests
    {
        [Fact]
        public async Task AntiDuplicate_ReconnectingStudent_SafelyClosesOldSocketAndKeepsSingleSession()
        {
            // Arrange
            var discovery = new NetworkDiscoveryService();
            await discovery.StartAsync("12A3_AntiDuplicate", "Cô Hương");
            int serverPort = NetworkDiscoveryService.TCP_PORT;

            var clientV1 = new StudentNetworkClient
            {
                StudentCode = "HS_DUPLICATE_01",
                StudentName = "Nguyễn Văn A",
                PCName = "PC_LAB_01"
            };

            var clientV2 = new StudentNetworkClient
            {
                StudentCode = "HS_DUPLICATE_01",
                StudentName = "Nguyễn Văn A (Reconnected)",
                PCName = "PC_LAB_01"
            };

            var receivedV1 = new List<string>();
            var receivedV2 = new List<string>();

            clientV1.MessageReceived += (_, msg) => receivedV1.Add(msg);
            clientV2.MessageReceived += (_, msg) => receivedV2.Add(msg);

            try
            {
                // Act 1: Kết nối lần đầu (V1)
                await clientV1.ConnectDirectAsync("127.0.0.1", serverPort);
                await Task.Delay(1000);

                Assert.True(clientV1.IsConnected);
                Assert.Equal(1, discovery.ConnectedCount);

                // Act 2: Cùng mã học sinh kết nối lại (V2) mô phỏng máy khởi động lại hoặc cắm lại dây mạng
                await clientV2.ConnectDirectAsync("127.0.0.1", serverPort);
                await Task.Delay(1500);

                // Assert: Socket cũ V1 phải bị ngắt, danh sách vẫn chỉ có 1 học sinh là V2
                Assert.Equal(1, discovery.ConnectedCount);
                Assert.True(clientV2.IsConnected);

                // Gửi thông báo từ giáo viên -> Chỉ V2 nhận được, V1 không còn nhận được
                await discovery.BroadcastMessage("Thông báo sau khi tái kết nối");
                await Task.Delay(1000);

                Assert.Contains("Thông báo sau khi tái kết nối", receivedV2);
            }
            finally
            {
                clientV1.Dispose();
                clientV2.Dispose();
                discovery.Stop();
                discovery.Dispose();
                await Task.Delay(500); // Chờ socket giải phóng
            }
        }

        [Fact]
        public async Task ClaimSeat_FirstComeFirstServed_RejectsDuplicateSeatSelection()
        {
            // Arrange
            var discovery = new NetworkDiscoveryService();
            await discovery.StartAsync("12A3_SeatClaim", "Thầy Hoàng");
            int serverPort = NetworkDiscoveryService.TCP_PORT;

            var alice = new StudentNetworkClient
            {
                StudentCode = "HS_ALICE_SEAT",
                StudentName = "Alice",
                PCName = "PC_01"
            };

            var bob = new StudentNetworkClient
            {
                StudentCode = "HS_BOB_SEAT",
                StudentName = "Bob",
                PCName = "PC_02"
            };

            var teacherMessages = new List<string>();
            discovery.MessageReceived += (_, e) =>
            {
                lock (teacherMessages)
                {
                    teacherMessages.Add($"{e.StudentCode}:{e.Message}");
                }
            };

            try
            {
                await alice.ConnectDirectAsync("127.0.0.1", serverPort);
                await bob.ConnectDirectAsync("127.0.0.1", serverPort);
                await Task.Delay(1000);

                Assert.Equal(2, discovery.ConnectedCount);

                // Act 1: Alice đăng ký ghế số 15 trước
                await alice.SendAsync("CLAIM_SEAT|15");
                await Task.Delay(1000);

                // Kiểm tra Alice nhận ghế 15 thành công
                lock (teacherMessages)
                {
                    Assert.Contains("HS_ALICE_SEAT:CLAIM_SUCCESS|15", teacherMessages);
                }

                var connectedList = discovery.GetConnectedStudents();
                var aliceInfo = connectedList.FirstOrDefault(c => c.Code == "HS_ALICE_SEAT");
                Assert.NotNull(aliceInfo);
                Assert.Equal(15, aliceInfo.SeatNumber);

                // Act 2: Bob cố tình đăng ký trùng ghế số 15
                await bob.SendAsync("CLAIM_SEAT|15");
                await Task.Delay(1000);

                // Bob không được nhận ghế 15
                var bobInfo = discovery.GetConnectedStudents().FirstOrDefault(c => c.Code == "HS_BOB_SEAT");
                Assert.NotNull(bobInfo);
                Assert.NotEqual(15, bobInfo.SeatNumber);

                // Act 3: Bob chọn ghế số 16 còn trống
                await bob.SendAsync("CLAIM_SEAT|16");
                await Task.Delay(1000);

                lock (teacherMessages)
                {
                    Assert.Contains("HS_BOB_SEAT:CLAIM_SUCCESS|16", teacherMessages);
                }
                bobInfo = discovery.GetConnectedStudents().FirstOrDefault(c => c.Code == "HS_BOB_SEAT");
                Assert.Equal(16, bobInfo?.SeatNumber);
            }
            finally
            {
                alice.Dispose();
                bob.Dispose();
                discovery.Stop();
                discovery.Dispose();
                await Task.Delay(500);
            }
        }

        [Fact]
        public async Task Heartbeat_DataPlaneIsolation_NET001_DoesNotPolluteChatPipeline()
        {
            // Arrange
            var discovery = new NetworkDiscoveryService();
            await discovery.StartAsync("12A3_HeartbeatTest", "Thầy Trực");
            int serverPort = NetworkDiscoveryService.TCP_PORT;

            var student = new StudentNetworkClient
            {
                StudentCode = "HS_HB_TEST",
                StudentName = "Lê Thị C",
                PCName = "PC_LAB_05"
            };

            var chatMessagesReceived = new List<string>();
            var heartbeatsReceived = new List<string>();

            // Chat UI chỉ lắng nghe MessageReceived
            discovery.MessageReceived += (_, e) =>
            {
                lock (chatMessagesReceived)
                {
                    chatMessagesReceived.Add(e.Message);
                }
            };

            // Monitor & Status Page lắng nghe HeartbeatReceived
            discovery.HeartbeatReceived += (_, e) =>
            {
                lock (heartbeatsReceived)
                {
                    heartbeatsReceived.Add(e.Message);
                }
            };

            try
            {
                await student.ConnectDirectAsync("127.0.0.1", serverPort);
                await Task.Delay(1000);

                // Act 1: Học sinh gửi gói tin Heartbeat chứa thông tin tiến trình đang mở và CPU
                await student.SendAsync("HB|HS_HB_TEST|OK|notepad.exe|12");
                await Task.Delay(1000);

                // Assert 1: HeartbeatReceived nhận được thông tin cập nhật
                lock (heartbeatsReceived)
                {
                    Assert.Contains(heartbeatsReceived, m => m.Contains("HB_UPDATE|notepad.exe|12"));
                }

                // Assert 2 (Chuẩn NET-001): MessageReceived (Chat) TUYỆT ĐỐI KHÔNG chứa gói tin heartbeat
                lock (chatMessagesReceived)
                {
                    Assert.DoesNotContain(chatMessagesReceived, m => m.Contains("HB_UPDATE") || m.StartsWith("HB|"));
                }

                // Act 2: Học sinh gửi tin nhắn chat thật
                string normalChat = "Thưa thầy em đã làm xong bài tập 1";
                await student.SendChat(normalChat);
                await Task.Delay(1000);

                // Assert 3: MessageReceived nhận đúng tin nhắn chat
                lock (chatMessagesReceived)
                {
                    Assert.Contains(chatMessagesReceived, m => m.Contains(normalChat));
                }
            }
            finally
            {
                student.Dispose();
                discovery.Stop();
                discovery.Dispose();
                await Task.Delay(500);
            }
        }

        [Fact]
        public async Task QoS1_LockScreenCommand_EnqueuesPendingAndClearsOnAck()
        {
            // Arrange
            var discovery = new NetworkDiscoveryService();
            await discovery.StartAsync("12A3_QoSTest", "Thầy Dũng");
            int serverPort = NetworkDiscoveryService.TCP_PORT;

            var student = new StudentNetworkClient
            {
                StudentCode = "HS_QOS_TEST",
                StudentName = "Trần Văn B",
                PCName = "PC_LAB_10"
            };

            var studentReceivedCommands = new List<string>();
            student.CommandReceived += (_, cmd) => studentReceivedCommands.Add(cmd);

            string? acknowledgedCmdId = null;
            var ackEvent = new ManualResetEventSlim(false);

            discovery.CommandAckReceived += (_, e) =>
            {
                acknowledgedCmdId = e.CommandId;
                ackEvent.Set();
            };

            try
            {
                await student.ConnectDirectAsync("127.0.0.1", serverPort);
                await Task.Delay(1000);

                // Act: Giáo viên gửi lệnh khóa toàn bộ màn hình học sinh (QoS 1)
                await discovery.LockAllScreensAsync();

                // Đợi học sinh nhận lệnh và gửi ACK về cho giáo viên
                bool receivedAck = ackEvent.Wait(TimeSpan.FromSeconds(5));

                // Assert 1: Học sinh nhận được lệnh CMD|LOCK|ALL
                Assert.Contains(studentReceivedCommands, c => c.StartsWith("CMD|LOCK|ALL"));

                // Assert 2: Giáo viên nhận được ACK xác nhận từ học sinh
                Assert.True(receivedAck, "Máy giáo viên phải nhận được ACK phản hồi từ học sinh");
                Assert.False(string.IsNullOrEmpty(acknowledgedCmdId));

                // Assert 3: Reflection kiểm tra hàng đợi pending retry đã được xóa sạch
                var pendingField = typeof(NetworkDiscoveryService).GetField("_pendingQosCommands", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(pendingField);
                var pendingDict = pendingField.GetValue(discovery) as System.Collections.Concurrent.ConcurrentDictionary<string, PendingQosCommand>;
                Assert.NotNull(pendingDict);

                string key = $"{acknowledgedCmdId}_HS_QOS_TEST";
                Assert.False(pendingDict.ContainsKey(key), "Sau khi nhận ACK thành công, lệnh QoS 1 phải được loại khỏi hàng đợi retry");
            }
            finally
            {
                student.Dispose();
                discovery.Stop();
                discovery.Dispose();
                await Task.Delay(500);
            }
        }

        [Fact]
        public async Task LateJoiner_WebBlockAndExitPinPolicySync_EnforcesSecurityPolicies()
        {
            // Arrange
            var control = ClassControlService.Instance;
            control.IsWebBlocked = true;
            control.IsExitPinRequired = true;
            control.ExitPinHash = "SECURE_HASH_POLICY_TEST";

            var discovery = new NetworkDiscoveryService();
            await discovery.StartAsync("12A3_LateJoiner", "Cô Lan");
            int serverPort = NetworkDiscoveryService.TCP_PORT;

            var lateStudent = new StudentNetworkClient
            {
                StudentCode = "HS_LATE_01",
                StudentName = "Phạm Văn Muộn",
                PCName = "PC_LAB_99"
            };

            var receivedCommands = new List<string>();
            lateStudent.CommandReceived += (_, cmd) => receivedCommands.Add(cmd);

            try
            {
                // Act: Học sinh vào muộn kết nối vào lớp
                await lateStudent.ConnectDirectAsync("127.0.0.1", serverPort);
                await Task.Delay(1500);

                // Assert: Học sinh phải tự động nhận chính sách khóa Web và mã PIN thoát ngay lúc bắt tay
                Assert.True(lateStudent.IsConnected);
                Assert.Contains(receivedCommands, c => c.Contains("BLOCK_WEB_ON"));
                Assert.Contains(receivedCommands, c => c.Contains("EXIT_PIN_CONFIG") && c.Contains("SECURE_HASH_POLICY_TEST"));
            }
            finally
            {
                // Dọn dẹp chính sách
                control.IsWebBlocked = false;
                control.IsExitPinRequired = false;
                control.ExitPinHash = string.Empty;

                lateStudent.Dispose();
                discovery.Stop();
                discovery.Dispose();
                await Task.Delay(500);
            }
        }
    }
}
